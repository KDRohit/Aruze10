using System.Collections.Generic;
using System.Text;
using Com.Scheduler;
using UnityEngine;

namespace PrizePop
{
	public class PrizePopFeature : EventFeatureBase
	{
		/* Assets */
		public const string IN_GAME_PREFAB_PATH = "Features/Prize Pop/Prefabs/Prize Pop In Game Panel Item";

		/* class ued to recrod prize pop rewards that have been claimed */
		[System.Serializable]
		public class PrizePopReward
		{
			public string type;
			public int value;
		}

		public class PrizePopPickData
		{
			public PrizePopPickData(BasePick pickItem, PrizePopReward rewardItem, int pickedObjectIndex)
			{
				pick = pickItem;
				reward = rewardItem;
				pickedIndex = pickedObjectIndex;
			}
			public BasePick pick;
			public PrizePopReward reward;
			public int pickedIndex = -1;
		}
		
		/* Events */
		private const string METER_EVENT = "prize_pop_meter_progress";
		private const string BONUS_GAME_PICKS_EVENT = "prize_pop_bonus_game_in_progress";
		private const string EXTRA_PICKS_GRANTED_EVENT = "prize_pop_extra_picks_granted"; //Happens when purchasing picks or revealing extra picks in pick game
		private const string ECONOMY_VERSION_UPDATED_EVENT = "prize_pop_economy_version_updated";
		private const string CURRENT_JACKPOT_EVENT = "prize_pop_current_jackpot";

		/* paytable details */
		private const string PAYTABLE_NAME = "prize_pop";
		private const string JACKPOT_GROUP = "JACKPOT";
		private const string EMPTY_GROUP = "EMPTY";
		
		/* Live Data */
		private const string METER_FILL_LIMIT_KEY = "PRIZE_POP_EARNED_PICKS_LIMIT";
		
		/* login data keys */
		public const string LoginDataKey = "prize_pop";
		private const string paytableKey = "prize_pop";

		
		private readonly Dictionary<int, List<PrizePopPickData>> selectedPicksByRound;
		private static bool logPackageError = true;
		
		
		public int currentPoints { get; private set; }
		public int currentRound { get; private set; }
		public int maximumPoints { get; private set; }
		public int minQualifyingWager { get; private set; }
		public string packageKey { get; private set; }
		public int totalRounds { get; private set; }
		public int maximumMeterFills { get; private set; }
		public int meterFillCount { get; private set; }
		public int extraPicks { get; private set; }
		public long currentJackpot { get; private set; }
		public int currentPackagePicks { get; private set; }
		public List<long> previousJackpots { get; private set; }
		private int convertedMeterToPicksCount = 0;

		public override bool isEnabled
		{
			get
			{
				return base.isEnabled && ExperimentWrapper.PrizePop.isInExperiment; 
			}
		}


		public int numPicksAvailable  
		{
			get
			{
				return meterFillCount + extraPicks;
			}	
		}

		private int economyVersion = -1; //Economy version used on the backend. Needs to be passed into the purchase refs dictionary so they know how many picks to award the player
		private bool manualBonusStart = false;
		public enum PrizePopOverlayType
		{
			NONE,
			KEEP_SPINNING,
			BUY_EXTRA_PICKS,
			EVENT_ENDED,
			EVENT_ENDING_SOON,
			NEW_STAGE
		}
		
		//static instance
		public static PrizePopFeature instance { get; private set; }

		private PrizePopFeature() 
		{
			selectedPicksByRound = new Dictionary<int, List<PrizePopPickData>>();
			currentRound = 0;
			meterFillCount = 0;
			extraPicks = 0;
			maximumMeterFills = 1;
			totalRounds = 0;
		}
		
		public static void instantiateFeature(JSON data)
		{
			if (instance != null)
			{
				instance.clearEventDelegates();
			}
			
			instance = new PrizePopFeature();
			instance.initFeature(data);
		}
		
		public string getInactiveReason()
		{
			StringBuilder noShowReason = new StringBuilder();
			if (!ExperimentWrapper.PrizePop.isInExperiment)
			{
				noShowReason.AppendLine("Experiment isn't active");
			}

			if (featureTimer == null)
			{
				noShowReason.AppendLine("Event timer is null");
			}
			else if (!featureTimer.hasStarted)
			{
				noShowReason.AppendLine("Timer hasn't started yet");
			}
			else
			{
				if (featureTimer.startTimer != null)
				{
					if (!featureTimer.startTimer.isExpired)
					{
						noShowReason.AppendLine("Event timer isn't active yet: " + featureTimer.startTimer.timeRemainingFormatted );
					}	
				}

				if (featureTimer.endTimer != null)
				{
					if (featureTimer.endTimer.isExpired)
					{
						noShowReason.AppendLine("Event timer expired at: " + featureTimer.endDate.ToShortDateString());
					}	
				}
				else
				{
					noShowReason.AppendLine("No end timer");
				}
				
			}

			return noShowReason.ToString();
		}

		public bool isPickAvailable
		{
			get { return numPicksAvailable > 0; }
		}

		protected override void initializeWithData(JSON data)
		{
			if (data == null)
			{
				return;
			}
			
			//read live data
			if (Data.liveData != null)
			{
				maximumMeterFills = Data.liveData.getInt(METER_FILL_LIMIT_KEY, 1);	
			}
			else
			{
				maximumMeterFills = 1;
			}
			
			// Should be overridden to parse the JSON data for the feature here.
			// While only the relevant data block should be passed in here
			// LiveData setup should still happen here through accessing Data.liveData
			int endTime = ExperimentWrapper.PrizePop.endTime;
			int startTime = ExperimentWrapper.PrizePop.startTime;
			packageKey = data.getString("package_offer", "");
			minQualifyingWager = data.getInt("min_qualifying_wager", 0);
			currentPoints = data.getInt("meter_progress", 0);
			meterFillCount = data.getInt("meter_fills", 0);
			maximumPoints = data.getInt("meter_target", 0);
			extraPicks = data.getInt("extra_picks", 0);
			currentRound = data.getInt("round", 0);
			economyVersion = data.getInt("economy_version", -1);
			currentJackpot = data.getLong("current_jackpot", 0);
			currentJackpot = 0;
			currentPackagePicks = data.getInt("package_offer_picks", 0);

			JSON paytableJson = BonusGamePaytable.findPaytable(paytableKey);
			if (paytableJson != null)
			{
				JSON[] rounds = paytableJson.getJsonArray("rounds");
				totalRounds = rounds.Length;
			}
			
			JSON wonJackpots = data.getJSON("won_jackpots");
			previousJackpots = new List<long>(totalRounds);
			if (wonJackpots != null)
			{
				foreach (string jsonKey in wonJackpots.getKeyList())
				{
					previousJackpots.Add(wonJackpots.getLong(jsonKey, 0));
				}
			}

			JSON[] reveals = data.getJsonArray("reveals_seen");
			if (reveals != null)
			{
				for (int i = 0; i < reveals.Length; i++)
				{
					if (reveals[i] == null)
					{
						Debug.LogWarning("Invalid pick reveal");
						continue;
					}

					int objectIndex = reveals[i].getInt("object_index", 0);
					BasePick pickItem = new BasePick();
					PrizePopReward rewardItem = new PrizePopReward();
					JSON reward = reveals[i].getJSON("reward");
					if (reward != null)
					{
						rewardItem.type = reward.getString("type", "");
						rewardItem.value = reward.getInt("value", 0);	
					}

					addPick(pickItem, rewardItem, objectIndex);
				}
			}

			setTimestamps(startTime, endTime);
			featureTimer.registerFunction(onFeatureEnd);
			if (!isEndingSoon())
			{
				featureTimer.registerFunction(onFeatureEndingSoon, null, ExperimentWrapper.PrizePop.endingSoonTrigger * Common.SECONDS_PER_MINUTE);
			}

			//Enable Collections if it hasn't been started yet.
			//Prize Pop requires it to give out cards in pick game
			if (!Collectables.isActive() && 
			    Collectables.isEventTimerActive() && 
			    !string.IsNullOrEmpty(Collectables.currentAlbum))
			{
				Collectables.missingBundles.Clear();
				Collectables.Instance.startFeature();
			}
		}

		private void onFeatureEnd(Dict args, GameTimerRange caller)
		{
			PrizePopOverlayStandaloneDialog.showDialog(PrizePopOverlayType.EVENT_ENDED);
		}
		
		private void onFeatureEndingSoon(Dict args, GameTimerRange caller)
		{
			if (PrizePopDialog.instance != null)
			{
				PrizePopDialog.instance.endingSoon();
			}

			if (PrizePopDialogOverlayBuyExtraPicks.instance != null)
			{
				PrizePopDialogOverlayBuyExtraPicks.instance.onEndingSoon();
			}
			
			InGameFeatureContainer.refreshDisplay(InGameFeatureContainer.PRIZE_POP_KEY);
		}

		protected override void registerEventDelegates()
		{
			Server.registerEventDelegate(METER_EVENT, onPointsGainedEvent, true);
			Server.registerEventDelegate(BONUS_GAME_PICKS_EVENT, onBonusStartEvent, true);
			Server.registerEventDelegate(EXTRA_PICKS_GRANTED_EVENT, onExtraPicksGranted, true);
			Server.registerEventDelegate(ECONOMY_VERSION_UPDATED_EVENT, onEconomyVersionUpdated, true);
			Server.registerEventDelegate(CURRENT_JACKPOT_EVENT, onGetCurrentJackpot, true);
		}

		protected override void clearEventDelegates()
		{
			Server.unregisterEventDelegate(METER_EVENT, onPointsGainedEvent, true);
			Server.unregisterEventDelegate(BONUS_GAME_PICKS_EVENT, onBonusStartEvent, true);
			Server.unregisterEventDelegate(EXTRA_PICKS_GRANTED_EVENT, onExtraPicksGranted, true);
			Server.unregisterEventDelegate(ECONOMY_VERSION_UPDATED_EVENT, onEconomyVersionUpdated, true);
			Server.unregisterEventDelegate(CURRENT_JACKPOT_EVENT, onGetCurrentJackpot, true);
		}

		private void onPointsGainedEvent(JSON data)
		{
			currentPoints = data.getInt("meter_progress", currentPoints);
			meterFillCount = data.getInt("meter_fills", meterFillCount);
			InGameFeatureContainer.refreshDisplay(InGameFeatureContainer.PRIZE_POP_KEY);
		}

		private void onBonusStartEvent(JSON data)
		{
			JSON[] picksData = data.getJsonArray("pending_reveals");
			List<PickemPick> picks = parsePicksFromEvent(picksData);
			showDialogWithBonusGame(picks, manualBonusStart);
			manualBonusStart = false;
			Decs.completeEvent(BONUS_GAME_PICKS_EVENT);
		}

		private void showDialogWithBonusGame(List<PickemPick> picks, bool manualStart)
		{
			PickemOutcome pickGameOutcome = new PickemOutcome(); //Pass this into the dialog to feed into the Modular Challenge Game
			pickGameOutcome.reveals = new List<PickemPick>();
			pickGameOutcome.entries = picks;
			bool hasJackpot = false;
			bool hasReward = false;
			for (int i = 0; i < pickGameOutcome.entries.Count; i++)
			{
				if (pickGameOutcome.entries[i].credits > 0 || !string.IsNullOrEmpty(pickGameOutcome.entries[i].cardPackKey) || pickGameOutcome.entries[i].prizePopPicks > 0)
				{
					if (pickGameOutcome.entries[i].isJackpot)
					{
						hasJackpot = true;
					}

					hasReward = true;
				}
			}

			ModularChallengeGameOutcome gameOutcome = new ModularChallengeGameOutcome(pickGameOutcome);
			
			if (currentJackpot == 0)
			{
				PrizePopAction.getCurrentJackpot();
			}
			
			PrizePopDialog.showDialog(manualStart, gameOutcome, hasJackpot, hasReward);
		}

		private void onEconomyVersionUpdated(JSON data)
		{
			maximumPoints = data.getInt("meter_target", maximumPoints);
			minQualifyingWager = data.getInt("min_qualifying_wager", minQualifyingWager);
			packageKey = data.getString("package_offer", packageKey);
			currentPackagePicks = data.getInt("package_offer_picks", currentPackagePicks);
			currentJackpot = data.getLong("current_jackpot", 0);
			economyVersion = data.getInt("economy_version", -1);
		}

		private void onGetCurrentJackpot(JSON data)
		{
			currentJackpot = data.getLong("current_jackpot", 0);
			if (PrizePopDialog.instance != null)
			{
				PrizePopDialog.instance.updateJackpotAmount();
			}
		}

		private List<PickemPick> parsePicksFromEvent(JSON[] pendingRevealsData)
		{
			List<PickemPick> picks = new List<PickemPick>();
			for (int i = 0; i < pendingRevealsData.Length; i++)
			{
				JSON winInfo = pendingRevealsData[i].getJSON("win_outcome");
				PickemPick pick = new PickemPick();
				pick.credits = winInfo.getLong("credits", 0);
				pick.groupId = winInfo.getString("group", "");
				pick.isJackpot = pick.groupId == "JACKPOT";
				JSON rewardInfo = pendingRevealsData[i].getJSON("reward");
				if (rewardInfo != null)
				{
					pick.parseRewardableInfo(rewardInfo);
					if (pick.prizePopPicks > 0)
					{
						pick.groupId = ""; //These picks are part of the EMPTY group for some reason
					}
				}
				
				picks.Add(pick);
			}

			return picks;
		}
		
		public bool showVideo(bool autoPopped)
		{
			StatsPrizePop.logShowVideo(autoPopped);
			return VideoDialog.showDialog(
				ExperimentWrapper.PrizePop.videoUrl, 
				actionLabel : "Great!", 
				summaryScreenImage: ExperimentWrapper.PrizePop.videoSummaryPath, 
				autoPopped: autoPopped,
				motdKey:autoPopped ? "prize_pop_video" : "",
				topOfList: !autoPopped,
				statName:"prize_pop_feature_video"
			);
		}

		private void onExtraPicksGranted(JSON data)
		{
			int picksAvailable = data.getInt("picks_granted", 0);
			extraPicks += picksAvailable;

			JSON[] additionalPicksData = data.getJsonArray("pending_reveals");
			List<PickemPick> additionalPicks = parsePicksFromEvent(additionalPicksData);
			if (PrizePopDialog.instance != null)
			{
				PrizePopDialog.instance.addNewPicks(additionalPicks);
			}
			else if (additionalPicks.Count > 0)
			{
				showDialogWithBonusGame(additionalPicks, false);
			}

			InGameFeatureContainer.refreshDisplay(InGameFeatureContainer.PRIZE_POP_KEY);
		}
		
		public bool isQualifyingBet(long wager)
		{
			return wager >= minQualifyingWager;
		}


		public void clearPointsAndMeterFill()
		{
			currentPoints = 0;
			meterFillCount = 0;
			InGameFeatureContainer.refreshDisplay(InGameFeatureContainer.PRIZE_POP_KEY);
		}

		private void reducePickCount()
		{
			if (convertedMeterToPicksCount > 0)
			{
				--convertedMeterToPicksCount;
			}
			else if (meterFillCount > 0)
			{
				--meterFillCount;	
			}
			else if (extraPicks > 0)
			{
				--extraPicks;
			}
			else
			{
				Debug.LogError("made pick with invalid data");
			}
		}

		private void addPick(BasePick selectedPick, PrizePopReward reward, int pickedIndex)
		{
			List<PrizePopPickData> picks = null;
			if (!selectedPicksByRound.TryGetValue(currentRound, out picks))
			{
				picks = new List<PrizePopPickData>();
				selectedPicksByRound[currentRound] = picks;
			}
			
			picks.Add(new PrizePopPickData(selectedPick, reward, pickedIndex));
		}

		public void advanceToNextRound()
		{
			selectedPicksByRound[currentRound].Clear(); //Clear this in case we hit this round in this session again
			currentRound++;
			if (currentRound >= totalRounds)
			{
				currentRound = 0;
				previousJackpots.Clear();
			}
		}
		

		public void incrementPickCount()
		{
			++extraPicks;
		}

		public int maxStorablePoints
		{
			get { return maximumPoints * maximumMeterFills; }
		}

		public void incrementPoints(int value)
		{
			if (meterFillCount >= maximumMeterFills)
			{
				Debug.LogWarning("Can't add more points, at maxium value");
				return;
			}
			if (value > 0)
			{
				currentPoints += value;
				while (currentPoints > maxStorablePoints)
				{
					currentPoints -= maxStorablePoints;
					meterFillCount++;
					if (meterFillCount >= maximumMeterFills)
					{
						currentPoints = 0;
						break;
					}
				}
				InGameFeatureContainer.refreshDisplay(InGameFeatureContainer.PRIZE_POP_KEY);
			}
			else
			{
				Debug.LogWarning("Negative meter fill amount is invalid");
			}
		}

		public void makePick(int pickIndex, bool isDebugPick = false)
		{
			if (numPicksAvailable <= 0)
			{
				Debug.LogWarning("Unable to make a pick without an available pick");
				//return;
			}
			
			reducePickCount();
			addPick(null, null, pickIndex);

			if (!isDebugPick)
			{
				PrizePopAction.spendPick(pickIndex);	
			}

			InGameFeatureContainer.refreshDisplay(InGameFeatureContainer.PRIZE_POP_KEY);
		}

		public void startBonusGame(bool isDebugGame, bool manualStart)
		{
			if (meterFillCount <= 0)
			{
				return;
			}

			extraPicks += meterFillCount - 1; //If we filled the meter multiple times these are converted to extra picks.
			convertedMeterToPicksCount = 1; //First pick will be the first one from filling the meter 
			meterFillCount = 0; //Meter gets drained as soon as bonus starts
			manualBonusStart = manualStart;

			if (!isDebugGame)
			{
				Scheduler.addTask(new PrizePopStartBonusTask(), SchedulerPriority.PriorityType.BLOCKING);
				PrizePopAction.startBonusGame();
			}

		}

		public List<PrizePopPickData> getCurrentRoundPicks()
		{
			List<PrizePopPickData> seenPicks;
			if (selectedPicksByRound.TryGetValue(currentRound, out seenPicks))
			{
				return selectedPicksByRound[currentRound];
			}

			return new List<PrizePopPickData>();
		}
		
		public void purchaseExtraPick()
		{
			PurchasablePackage package = getCurrentPackage();
			if (package != null)
			{
				logPurchase();
				package.makePurchase(0,false, -1, "PrizePopPackage", economyTrackingNameOverride:"PrizePop", economyVersion:economyVersion, purchaseType:PurchaseFeatureData.Type.PRIZE_POP);
			}
			else if (logPackageError)
			{
				logPackageError = false;
				Debug.LogError("No prize pop package available");
			}
		}
		
		public PurchasablePackage getCurrentPackage()
		{
			PurchaseFeatureData data = PurchaseFeatureData.PrizePop;

			if (data != null && data.prizePopPackages != null && data.prizePopPackages.Count > 0)
			{
				for (int i = 0; i < data.prizePopPackages.Count; i++)
				{
					if (data.prizePopPackages[i].purchasePackage != null && data.prizePopPackages[i].purchasePackage.keyName == packageKey)
					{
						return data.prizePopPackages[i].purchasePackage;
					}
				}
			}
			return null;
		}

		public bool isEndingSoon()
		{
			return featureTimer.timeRemaining / Common.SECONDS_PER_MINUTE < ExperimentWrapper.PrizePop.endingSoonTrigger;
		}
	
		private static void logPurchase()
		{
			//Currently no stats are being logged.
		}
		
		public static void resetStaticClassData()
		{
			instance = null;
			logPackageError = true;
		}
	}
	
	
}

