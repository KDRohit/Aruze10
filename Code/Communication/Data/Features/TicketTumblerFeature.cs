using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TicketTumblerFeature : EventFeatureBase
{
	public static TicketTumblerFeature instance
	{
		get
		{
			return FeatureDirector.createOrGetFeature<TicketTumblerFeature>("ticket_tumbler");
		}
	}	
	public const string LOBBY_PREFAB_PATH = "Features/Ticket Tumbler/Prefabs/Lobby Option Ticket Tumbler";
	public const string STATUS_BUTTON_PATH = "Features/Ticket Tumbler/Prefabs/Ticket Tumbler Overlay Button";

	public int numTicketsForCollectablesPack;
	public long ticketPrizeAmount;
	public bool wasLotteryActiveAtLogIn;
	public bool waitingForEventData;
	public bool doNotShowUntilRestart;
	public JSON eventData;
	public JSON collectData;
	public JSON winnerData;
	public JSON completeData;
	public JSON progressData;
	public JSON ticketData;
	public JSON logInLotteryData;
	public int ticketCount;					// number of tickets player has this round
	public float meterProgress;				// progress until next ticket
	public int roundLength;					// total length of the round in minutes
	public int lotteryKey;					// key to use for server actions, changes on lottery info event
	public int previousLotteryKey;			// key to the drawing before the current one
	public GameTimerRange roundEventTimer;	// time remaining for this round

	public GameObject statusButtonPrefab;	// assetbundle prefab

	public int completedLotteryKey;
	public int winningLotteryKey;
	// Assets to load for when we need em.
	public GameObject lobbyButton = null;
	public GameObject toaster = null;

	public List<SocialMember> previousWinners;
	public long[] previousWinnersVipIndexes;
	public int lastTicketingKey;

	public const float BUNDLE_CHECK_INTERVAL = 1.0f;
	public const float BUNDLE_TIMEOUT = 600.0f;
	public const int SECONDS_LEFT_FOR_RED_ALERT = 20;
	public const string BUNDLE_NAME = "ticket_tumbler";

	private JSON packDropData;

	public long eventPrizeAmount
	{
		get
		{		
			if (eventData != null)
			{
				return eventData.getLong("prize", 0L);
			}

			return 0L;
		}
	}

	public static void checkInstance()
	{
		if (instance == null)
		{
			Debug.LogError("TicketTumblerFeature instance failed to create");
		}
	}

	private TicketTumblerStatusButton statusButton
	{
		get
		{
			if (SpinPanel.hir != null)
			{
				return SpinPanel.hir.ticketTumblerStatusButton;
			}

			return null;
		}
	}

	public TicketTumblerStatusButton attachStatusButtonInstance(GameObject anchor)
	{
		TicketTumblerStatusButton statusButton = null;

		if (anchor != null && statusButtonPrefab != null)
		{
			GameObject go = NGUITools.AddChild(anchor, statusButtonPrefab);
			if (go != null)
			{
				statusButton = go.GetComponent<TicketTumblerStatusButton>();
			}
		}

		return statusButton;
	}

	private void iconLoadSuccess(string assetPath, Object obj, Dict data = null)
	{
		switch (assetPath)
		{
			case LOBBY_PREFAB_PATH:
				// Load things one after the other.
				lobbyButton = obj as GameObject;
				break;
			case STATUS_BUTTON_PATH:
				statusButtonPrefab = obj as GameObject;
				break;
			default:
				Debug.LogError("Got an asset idk what to do with " + assetPath);
				break;
		}
	}

	public static void iconLoadFailure(string assetPath, Dict data = null)
	{
		Debug.LogError("Ticket Tumbler::iconconLoadFailure - Failed to load asset at: " + assetPath);
	}

	public void handleWagerChange()
	{
		if (isEnabled && GameState.game != null && !GameState.game.isMaxVoltageGame)
		{
			if (statusButton != null)
			{
				statusButton.onWagerChange();
			}
		}
	}

	public void onSpin()
	{
		if (isEnabled)
		{
			if (statusButton != null)
			{
				statusButton.onSpin();
			}
		}
	}

	public void stackTickets(List<GameObject> tickets, int numTix)
	{
		for (int i = 0; i < tickets.Count; i++)
		{
			tickets[i].SetActive(i < numTix);
		}
	}

	public static void resetStaticClassData()
	{
	}

	// gets called for first time when less than 20 seconds left on timer
	private void onRedAlert(Dict args = null, GameTimerRange originalTimer = null)	
	{
		if (isEnabled)
		{
			if (statusButton != null && roundEventTimer.timeRemaining <= SECONDS_LEFT_FOR_RED_ALERT)
			{
				statusButton.onRedAlert();
				if (roundEventTimer.timeRemaining > 1)
				{
					roundEventTimer.registerFunction(onRedAlert, args, roundEventTimer.timeRemaining - 1);
				}
			}
		}
	}

	private IEnumerator waitForOverLayInstance()
	{
		while (Overlay.instance == null || Overlay.instance.topHIR == null)
		{
			yield return null;
		}

		AssetBundleManager.load(LOBBY_PREFAB_PATH, iconLoadSuccess, iconLoadFailure);
		AssetBundleManager.load(STATUS_BUTTON_PATH, iconLoadSuccess, iconLoadFailure);

		yield return null;
	}

	private void ticketTumblerExpireHandler()	
	{
		if (ticketCount == 0)
		{
			hideStatusButton();
		}
	}

	private void hideCarousel()
	{
		CarouselData data;
		data = CarouselData.findActiveByAction("ticket_tumbler");
		if (data != null)
		{
			data.deactivate();
		}
	}

	private void showCarousel()
	{
		CarouselData data;
		data = CarouselData.findInactiveByAction("ticket_tumbler");
		if (data != null)
		{
			data.activate();
		}
	}

	private void hideStatusButton()
	{
		if (statusButton != null)
		{
			statusButton.deactivate();
		}
	}

	private  void roundExpireHandler(Dict args = null, GameTimerRange originalTimer = null)	
	{
		Audio.play("TimeOutTicketTumbler");
		int timerKey = 0;
		if (args != null)
		{
			timerKey = args.containsKey(D.DATA) ? (int)args[D.DATA] : 0;
		}

		if (TicketTumblerDialog.instance != null)
		{
			TicketTumblerDialog.instance.updateLabelsForExpiredTimer();
		}

		// this will return a complete event for the current lottery and a winner event if the player won, it will also return an info event for the next drawing
		postGetInfoAction();
	}

	public void initLoginData(JSON data)
	{
		logInLotteryData = data.getJSON("player.lottery");

		if (logInLotteryData == null)
		{
			wasLotteryActiveAtLogIn = false;
			return;
		}

		wasLotteryActiveAtLogIn = true;

		roundLength = Data.liveData.getInt("LOTTERY_DAY_LENGTH", 30);

		setCurrentLotteryInfo(logInLotteryData);

		RoutineRunner.instance.StartCoroutine(waitForOverLayInstance());

		if (!AssetBundleManager.isBundleCached(BUNDLE_NAME) && isEnabled)
		{
			AssetBundleManager.downloadAndCacheBundle(BUNDLE_NAME, true, true);
		}
	}

	public void onPackDropped(JSON data)
	{
		packDropData = data;
	}

	public void postGetInfoAction()
	{
		waitingForEventData = true;
		TicketTumblerAction.getInfo(lotteryKey.ToString());
	}

	private void setEventDataValid(JSON data)
	{
		waitingForEventData = false;
		eventData = data;

		previousWinners = makeWinnerList(data, out previousWinnersVipIndexes, "previous_winners", SocialMember.ScoreType.TICKET_TUMBLER_PREVIOUS_WIN);		// list that gets shown in intro state
	}

	public long[] makeTierPrizeList(JSON data, string listName)
	{
		List<JSON> srcList = data.deprecatedGetKeyedJSONList(listName);
		if (srcList.Count > 0)
		{
			long[] prizeList = new long[srcList.Count];
			for (int i = 0; i < srcList.Count; i++)
			{
				int newVIPLevel = srcList[i].getInt("vip", 0);
				if (newVIPLevel < prizeList.Length)
				{
					prizeList[newVIPLevel] = srcList[i].getLong("prize", 0L);
				}
			}

			return prizeList;
		}

		return null;
	}

	public List<SocialMember> makeWinnerList(JSON data, out long[] vipIndexes, string listName, SocialMember.ScoreType scoreType, int vipLevel = -1, bool addScore = true)
	{
		List<JSON> srcList = data.deprecatedGetKeyedJSONList(listName);
		List<SocialMember> winnerList = new List<SocialMember>();
		vipIndexes = new long[srcList.Count];
		for (int i = 0; i < srcList.Count; i++)
		{
			int newVIPLevel = srcList[i].getInt("vip", 0);

			if (vipLevel == -1 || vipLevel == newVIPLevel)
			{
				string fbid = srcList[i].getString("fbid", "");
				string zid = srcList[i].getString("zid", "");
				string name = srcList[i].getString("name", "");
				string photoURL = srcList[i].getString("photo_url", "");
				long achievementScore = srcList[i].getLong("achievement_score", -1);

				// precheck for valid id so we don't spam the error log with null entries that are only there for vip prize info
				if (CommonSocial.isValidId(fbid)|| CommonSocial.isValidId(zid))
				{				
					SocialMember member = CommonSocial.findOrCreate(fbid: fbid,
						zid: zid,
						firstName: name,
						lastName: "",
						imageUrl: photoURL,
						achievementScore: achievementScore);

					if (member != null)
					{
						if (addScore)
						{
							// Make sure we set the score and vip level every time as those can change.
							member.addScore(scoreType, srcList[i].getLong("prize", 0L));
						}

						if (newVIPLevel > member.vipLevel)
						{
							// Update the vip level if it says its greater.
							member.vipLevel = newVIPLevel;
						}
						if (newVIPLevel < vipIndexes.Length)
						{
							vipIndexes[i] = newVIPLevel;	// don't trust the one in ttlevl
						}
						winnerList.Add(member);
					}
				}
                else 
                {
                    winnerList.Add(null);       // null stub for tier level prize
                }
			}
		}

		return winnerList;
	}

#region feature_base_overrides
	protected override void initializeWithData(JSON data)
	{
		initLoginData(data);

		setTimestamps(Data.liveData.getInt("LOTTERY_DAY_START_TIME", 0),Data.liveData.getInt("LOTTERY_DAY_END_TIME", 0));
	}

	public override bool isEnabled
	{
		get
		{
			return (wasLotteryActiveAtLogIn && base.isEnabled && ExperimentWrapper.LotteryDayTuning.isInExperiment);
		}
	}

	protected override void registerEventDelegates()
	{
		Server.registerEventDelegate("lottery_info", handleLotteryInfoEvent, true);
		Server.registerEventDelegate("lottery_complete", handleLotteryCompleteEvent, true);
		Server.registerEventDelegate("lottery_winner", handleLotteryWinnerEvent, true);
		Server.registerEventDelegate("lottery_ticket", handleAddTicket, true);		// used by ticket machine in reel game
		Server.registerEventDelegate("lottery_progress", handleMeterProgress, true);  // used by ticket machine in reel game for meter progress
		
		// Register callback with collections. Eventually we'll want to port this over
		// to rewardables
		Collectables.Instance.registerForPackDrop(onTicketTumblerPackDrop , "lottery");
		
		onDisabledEvent += ticketTumblerExpireHandler;
	}

	protected override void clearEventDelegates()
	{
		onDisabledEvent -= ticketTumblerExpireHandler;
		Collectables.Instance.unRegisterForPackDrop(onTicketTumblerPackDrop , "lottery");
	}
	
#endregion
#region event_delegates

	public void onTicketTumblerPackDrop(JSON packData)
	{
		packDropData = packData;
	}
	
	public void handleLotteryInfoEvent(JSON data)  // make standard
	{
		if (data != null)
		{
			setCurrentLotteryInfo(data);

			if (statusButton != null)
			{
				statusButton.onGotTicket(false);
				statusButton.onEventProgress(false);
			}
		}
		else
		{
			Debug.LogError("Ticket Tumbler initEventData: json data is null");
		}
	}

	public void setCurrentLotteryInfo(JSON data)
	{
		if (Data.debugMode && DevGUIMenuTicketTumbler.pauseEvents)
		{
			return;
		}

		if (data != null)
		{
			previousLotteryKey = lotteryKey;
			lotteryKey = data.getInt("key", 0);	

			setEventDataValid(data);

			ticketCount = data.getInt("ticket_count", 0);
			meterProgress = data.getFloat("meter_progress", 0.0f);

			instance.numTicketsForCollectablesPack = data.getInt("num_tickets_for_pack", -1);

			if (!data.getBool("collectibles_eligibility", false))
			{
				instance.numTicketsForCollectablesPack = -1;
			}

			ticketPrizeAmount = data.getLong("total_ticket_prize", 0L);

			if (roundEventTimer == null)
			{
				roundEventTimer = new GameTimerRange(GameTimer.currentTime, GameTimer.currentTime + logInLotteryData.getInt("time_left", 0), true);
			}
			else
			{
				roundEventTimer.startTimers(GameTimer.currentTime, GameTimer.currentTime + data.getInt("time_left", 0));
			}

			Dict args = Dict.create(D.DATA, lotteryKey);
			roundEventTimer.registerFunction(roundExpireHandler, args);
			roundEventTimer.registerFunction(onRedAlert, args, SECONDS_LEFT_FOR_RED_ALERT);

			if (statusButton != null)
			{
				statusButton.onGotTicket(false);
				statusButton.onEventProgress(false);
			}
		}		
	}

	public void handleLotteryCompleteEvent(JSON data)
	{
		if (Data.debugMode && DevGUIMenuTicketTumbler.pauseEvents)
		{
			return;
		}

		if (completedLotteryKey == data.getInt("key", 0))
		{
			Debug.LogError("Duplicate lottery complete event, ignoring " + completedLotteryKey);
			return;
		}

		long ticketPrizeForEvent = data.getLong("total_ticket_prize", 0L);
		int ticketCountForEvent = data.getInt("ticket_count", 0);

		completeData = data;

		// update the key
		completedLotteryKey = data.getInt("key", 0);

		// check for winner, if this player is the winner then wait for winner event which has arrived in the same batch of events
		if (data.getBool("is_winner", false))
		{
			return;
		}

		winningLotteryKey = completedLotteryKey;	// this player did not win set the winningLotteryKey to the completed key so everything is in sync

		if (TicketTumblerDialog.instance != null && TicketTumblerDialog.instance.instanceTicketKey == completedLotteryKey)
		{
			TicketTumblerDialog.instance.setInstanceEventData(data);
			TicketTumblerDialog.instance.instancePrizeAmount = 0L;
			TicketTumblerDialog.instance.transitionToTicketingFromIntroState();
		}
		else
		{
			if (ticketCountForEvent > 0)
			{
				TicketTumblerDialog.showTicketingDialog(data, completedLotteryKey);
			}
			else
			{
				// BY: looked like this could happen before ToasterManager.Awake(), so we don't show this toaster
				if (ToasterManager.instance != null)
				{
					// get winner for their tier
					if (SlotsPlayer.instance != null && SlotsPlayer.instance.socialMember != null)
					{
						long[] toasterVipList = null;
						int displayVipLevel = data.getInt("vip", 0);

						List<SocialMember> tierWinners = makeWinnerList(data, out toasterVipList, "winners", SocialMember.ScoreType.TICKET_TUMBLER_PREVIOUS_WIN, displayVipLevel, false);
						long[] tierPrizes = makeTierPrizeList(data, "winners");

						Dict args = Dict.create(D.VALUE, tierPrizes != null && displayVipLevel < tierPrizes.Length ? tierPrizes[displayVipLevel] : 0);

						if (tierWinners != null && tierWinners.Count > 0 && tierWinners[0] != null)
						{
							ToasterManager.getPlayerDataAndAddToaster(tierWinners[0], ToasterType.TICKET_TUMBLER, args);
						}
					}
				}
			}
		}
	}

	public void handleLotteryWinnerEvent(JSON data)
	{
		if (Data.debugMode && DevGUIMenuTicketTumbler.pauseEvents)
		{
			return;
		}

		if (winningLotteryKey == data.getInt("key", 0))
		{
			Debug.LogError("got WINNER event we already handled key : " + winningLotteryKey);
			return;
		}

		winnerData = data;

		winningLotteryKey = data.getInt("key", 0);

		if (winningLotteryKey == 0)
		{
			if (TicketTumblerDialog.instance != null)
			{
				Dialog.close();
			}
			else
			{
				return;
			}
		}

		if (winningLotteryKey == lotteryKey || winningLotteryKey == completedLotteryKey || winningLotteryKey == previousLotteryKey)
		{
			if (TicketTumblerDialog.instance != null)
			{
				TicketTumblerDialog.instance.transitionToCollectionStateAsWinner(data);
			}
			else
			{
				TicketTumblerDialog.showWinnerDialog(data, false);
			}
		}
		else
		{
			// this is a previous win and they have come back into a new session
			TicketTumblerDialog.showWinnerDialog(data, true);
		}
	}

	private void handleAddTicket(JSON data)
	{
		ticketData = data;
		ticketCount = data.getInt("ticket_count", 0);
		meterProgress = data.getFloat("meter_progress", 0.0f);

		if (statusButton != null)
		{
			statusButton.onGotTicket();
		}

		StatsManager.Instance.LogCount(counterName:"ticket", kingdom:"lottery_day",
			phylum:ExperimentWrapper.LotteryDayTuning.scaleFactor.ToString(),
			klass:lotteryKey.ToString(),
			genus:ticketCount.ToString(),
			val:ticketCount);
	}

	private void handleMeterProgress(JSON data)
	{
		progressData = data;
		meterProgress = data.getFloat("meter_progress", 0.0f);
		if (meterProgress > .80f && statusButton != null)
		{
			statusButton.onEventProgress();
		}
	}

	#endregion


	
	public void statusButtonCheck()
	{
		if (featureTimer.isExpired)
		{
			hideStatusButton();
		}
	}

	public void dropPackCheck()
	{
		if (packDropData != null)
		{
			Collectables.claimPackDropNow(packDropData);
			packDropData = null;
		}
	} 
}
