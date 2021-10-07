using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using Com.Rewardables;
using Zynga.Core.Util;

namespace QuestForTheChest
{
	public delegate void QFCDelegate(string zid, int value);

	public delegate void QFCRewardDelegate(List<QFCReward> rewards);

	public delegate void QFCRaceEndDelegate(string zid, bool didWin);

	public enum QFCTeams
	{
		AWAY,
		HOME
	}

	public class QFCReward
	{
		public QFCReward(string rewardType, long quantity)
		{
			type = rewardType;
			value = quantity;
		}
		
		public QFCReward(string rewardType, string packName)
		{
			type = rewardType;
			this.packName = packName;
		}
		
		public string type;
		public long value;
		public string packName;
	}

	public class QFCRaceData
	{
		public int index;
		public int homeKeyTotal;
		public int awayKeyTotal;
		public int[] homeFinalPositions;
		public int[] awayFinalPositions;

		public string toJSONString()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("{");
			sb.AppendLine("\t\"index\" : " + index + ",");
			sb.AppendLine("\t\"home_keys\" : " + homeKeyTotal + ",");
			sb.Append("\t\"away_keys\" : " + awayKeyTotal + "");
			if (homeFinalPositions != null || awayFinalPositions != null)
			{
				sb.AppendLine(",");
			}
			else
			{
				sb.AppendLine();
			}
			if (homeFinalPositions != null)
			{
				sb.AppendLine("\t\"home_positions\" : [");
				for(int i=0; i< homeFinalPositions.Length; i++)
				{
					if (i != 0)
					{
						sb.AppendLine(",");
					}
					sb.Append("\t\t" + homeFinalPositions[i]);
				}
				sb.AppendLine();
				sb.Append("\t]");
				if (awayFinalPositions != null)
				{
					sb.AppendLine(",");
				}
				else
				{
					sb.AppendLine();
				}
			}
			if (awayFinalPositions != null)
			{
				sb.AppendLine("\t\"away_positions\" : [");
				for(int i=0; i< awayFinalPositions.Length; i++)
				{
					if (i != 0)
					{
						sb.AppendLine(",");
					}
					sb.Append("\t\t" + awayFinalPositions[i]);
				}
				sb.AppendLine();
				sb.AppendLine("\t]");
			}

			sb.AppendLine("}");

			return sb.ToString();
		}

		public QFCRaceData()
		{
			index = 0;
			homeKeyTotal = 0;
			awayKeyTotal = 0;
			homeFinalPositions = null;
			awayFinalPositions = null;
		}

		public QFCRaceData(JSON data)
		{
			index = data.getInt("index", 0);
			homeKeyTotal = data.getInt("home_keys", 0);
			awayKeyTotal = data.getInt("away_keys", 0);
			homeFinalPositions = data.getIntArray("home_positions");
			awayFinalPositions = data.getIntArray("away_positions");
		}
	}

	public class QuestForTheChestFeature : EventFeatureBase
	{
		//constants
		public const float CONSUME_RACE_TIMEOUT = 5.0f;

		//asset paths
		public const string THEMED_ATLAS_PATH = "Features/Quest for the Chest/Themed Assets/{0}/Theme Atlas/Quest for the Chest Theme Atlas {0}";
		
		/* pending credit source */
		public const string QFC_REWARD_PENDING_CREDIT_SOURCE = "qfcReward";
		public const string QFC_REWARD_PENDING_CREDIT_BONUS_SOURCE = "qfcBonusOverlay";

		//Server Events
		private const string RACE_CHEST_EVENT = "qfc_race_chest";
		private const string CONTEST_COMPLETE_EVENT = "qfc_complete_contest";
		private const string RACE_INFO_EVENT = "qfc_race_info";
		private const string PLAYER_PROGRESS_EVENT = "qfc_player_progress";
		private const string NODE_REWARD_EVENT = "qfc_node_reward";
		private const string RACE_COMPLETE_EVENT = "qfc_race_complete";
		private const string TOKEN_REWARD_EVENT = "qfc_token_reward";
		private const string MINI_GAME_EVENT = "qfc_mini_game_outcome";
		private const string REWARDS_BUNDLE = "rewards_bundle";
		private const string QFC_MVP_REWARD = "qfc_mvp_reward";
		private const string QFC_MVP_REWARD_INFO = "qfc_mvp_reward_info";

		//server toaster events
		private const string TOASTER_ROUND_COMPLETE = "qfc_toaster_round_complete";
		private const string TOASTER_NEED_TOKENS = "qfc_toaster_need_tokens";
		
		//Live Data Keys
		private const string QFC_START_TIME_KEY = "QFC_START_TIME";
		private const string QFC_END_TIME_KEY = "QFC_END_TIME";
		
		//Prefab Paths
		private const string NODE_LOCATION_MAP_PREFAB_PATH = "Features/Quest for the Chest/Themed Assets/{0}/Prefabs/Map Node Coordinates Item {0}";

		//Timer Trigger
		private string toasterTimeTriggerData = "";
		
		//Keys to win Triggers
		private string toasterKeyTriggerData = "";
		private int keysTriggerIndex = -1;   // need X keys to win threshold flag
		private List<int> keyNumTriggerList = new List<int>();

		private bool isWaitingToShowToaster = false;
		private const float TOASTER_WAIT_TIME = 1.0f;

 		//team lead flag
		private bool cachedLead = false;

		//toaster cooldown
		public int toasterCooldown = 120; //show once every 2 mintues
		private long lastToasterTime = 0;

		public event QFCDelegate onPlayerAdvanceEvent;
		
		public event QFCDelegate onPlayerProgressToNonStoryNodeEvent;
		public event QFCDelegate onPlayerAwardTokenEvent;
		public event QFCRaceEndDelegate onRaceCompleteEvent;
		public event FeatureEventDelegate onNewRaceEvent;
		public event FeatureEventDelegate onRestartEvent;
		public event QFCRewardDelegate onChestAwardedEvent;
		public event QFCRewardDelegate onNodeRewardEvent;
		public event QFCRewardDelegate onMiniGameAwardedEvent;

		public event QFCRewardDelegate onKeyRewardEvent;
		
		//Number of tokens needed to win the chest
		public int requiredKeys { get; private set; }
		public int homeTeamId { get; private set; }
		public int awayTeamId { get; private set; }
		public int currentRound { get; private set; }

		public int totalNodes { get; private set; }

		public int competitionId { get; private set; }
		public int raceIndex { get; private set; }

		public Color rewardShroudColor { get; private set; }

		//home team is team on the left (always the users team)
		private List<SocialMember> homeTeam;

		//away team is the team on the right (always the opponents)
		private List<SocialMember> awayTeam;

		//current player event info indexed by zid
		private Dictionary<string, QFCPlayer> playerInfos = new Dictionary<string, QFCPlayer>();
		private Dictionary<int, QFCRaceData> raceInformation;

		//credit rewards index by node
		public List<QFCBoardNode> nodeData { get; private set; }
		public int currentConsumeRaceIndex { get; private set; }
		private bool raceComplete;
		private Dictionary<string, int> raceEventToIndexMap = new Dictionary<string, int>();
		private Queue<string> raceCompleteEventIds = new Queue<string>();
		private int winningKeyAmount;
		private Dictionary<string, long> rewardAmounts = new Dictionary<string, long>();
		private int authoritativeTimestamp;

		public static QuestForTheChestFeature instance
		{
			get
			{
				return FeatureDirector.createOrGetFeature<QuestForTheChestFeature>("qfc");
			}
		}

		public static void checkInstance()
		{
			if (instance == null)
			{
				Debug.LogError("QuestForTheChestFeature instance failed to create");
			}
		}

		public static Dictionary<long, long> convertAbsoluteCreditValues(JSON absoluteCoinRewardsJSON)
		{
			List<string> winIdKeys = absoluteCoinRewardsJSON.getKeyList();
			Dictionary<long, long> winIdToAbsouluteCreditValues = new Dictionary<long, long>();
			for (int i = 0; i < winIdKeys.Count; i++)
			{
				long winId = 0;
				if (long.TryParse(winIdKeys[i], out winId))
				{
					winIdToAbsouluteCreditValues.Add(winId, absoluteCoinRewardsJSON.getLong(winIdKeys[i], 0));
				}
			}

			return winIdToAbsouluteCreditValues;
		}

		public bool isEventActive()
		{
			return base.isEnabled && ExperimentWrapper.QuestForTheChest.isInExperiment;
		}

		public override bool isEnabled
		{
			get
			{
				return base.isEnabled && ExperimentWrapper.QuestForTheChest.isInExperiment && homeTeam != null && homeTeam.Contains(SlotsPlayer.instance.socialMember); 
			}
		}

		public bool isRaceComplete
		{
			get
			{
				return raceComplete;
			}
		}

		public string getInactiveReason()
		{
			StringBuilder noShowReason = new StringBuilder();
			if (!ExperimentWrapper.QuestForTheChest.isInExperiment)
			{
				noShowReason.AppendLine("Experiment isn't active");
			}

			if (featureTimer == null)
			{
				noShowReason.AppendLine("Event timer is null");
			}
			else
			{
				if (!featureTimer.startTimer.isExpired)
				{
					System.DateTime startDate = Common.convertFromUnixTimestampSeconds(Data.liveData.getInt(QuestForTheChestFeature.QFC_START_TIME_KEY, 0));
					noShowReason.AppendLine("Event timer isn't active: " + startDate);
				}

				if (featureTimer.endTimer.isExpired)
				{
					System.DateTime endDate = Common.convertFromUnixTimestampSeconds(Data.liveData.getInt(QuestForTheChestFeature.QFC_END_TIME_KEY, 0));
					noShowReason.AppendLine("Event timer is expired: " + endDate);
				}
			}

			if (homeTeam == null)
			{
				noShowReason.AppendLine("No home team data");
			}
			else if (!homeTeam.Contains(SlotsPlayer.instance.socialMember))
			{
				noShowReason.AppendLine("Current player not found in the home team");
			}

			return noShowReason.ToString();
		}

		protected override void initializeWithData(JSON data)
		{
			setTimestamps(Data.liveData.getInt(QFC_START_TIME_KEY, 0),Data.liveData.getInt(QFC_END_TIME_KEY, 0));
			raceComplete = false;
			authoritativeTimestamp = GameTimer.currentTime;
			currentConsumeRaceIndex = -1;

			if (Data.debugMode)
			{
				initOldRaceData();
			}

			if (data != null)
			{
				//parse data
				JSON questData = data.getJSON("qfc");

				if (questData != null)
				{
					onNewRace(questData);
				}
			}
		}

		protected override void registerEventDelegates()
		{
			Server.registerEventDelegate(RACE_CHEST_EVENT, onChestAwarded, true);
			Server.registerEventDelegate(CONTEST_COMPLETE_EVENT, onContestComplete, true);
			Server.registerEventDelegate(RACE_INFO_EVENT, onNewRace, true);
			Server.registerEventDelegate(PLAYER_PROGRESS_EVENT, onPlayerProgress, true);
			Server.registerEventDelegate(NODE_REWARD_EVENT, onNodeReward, true);
			Server.registerEventDelegate(RACE_COMPLETE_EVENT, onRaceComplete, true);
			Server.registerEventDelegate(TOKEN_REWARD_EVENT, onKeyReward, true);
			Server.registerEventDelegate(TOASTER_ROUND_COMPLETE, onRoundComplete, true);
			Server.registerEventDelegate(MINI_GAME_EVENT, onMiniGameAwarded, true);
			Server.registerEventDelegate(REWARDS_BUNDLE, onRewardReceipt, true);
			Server.registerEventDelegate(QFC_MVP_REWARD, onMVPReward, true);
			Server.registerEventDelegate(QFC_MVP_REWARD_INFO, onMVPRewardInfo, true);
		}

		private void onMVPReward(JSON data)
		{
			// key credits
			List<QFCReward> rewardList = parseJSONRewards(data, "token_rewards");
			// MVP rewards
			List<QFCReward> mvpRewardList = parseJSONRewards(data, "rewards");
			
			long tokenCredits = 0;
			long mvpCredits = 0;
			
			if (rewardList != null)
			{
				for (int i = 0; i < rewardList.Count; i++)
				{
					if (rewardList[i].type == RewardCoins.TYPE)
					{
						tokenCredits += rewardList[i].value;
					}
				}
			}
			if (mvpRewardList != null)
			{
				for (int i = 0; i < mvpRewardList.Count; i++)
				{
					if (mvpRewardList[i].type == RewardCoins.TYPE)
					{
						mvpCredits += mvpRewardList[i].value;
					}
				}
			}

			string eventId = data.getString("event", "");
			long totalCredits = tokenCredits + mvpCredits;
			if (totalCredits > 0)
			{
				rewardAmounts[eventId] = totalCredits;
				
				// Add pending credits to avoid desync.
				Server.handlePendingCreditsCreated(QFC_REWARD_PENDING_CREDIT_SOURCE, totalCredits);
			}
			if (!isEnabled)
			{
				if (!isEventActive())
				{
					// HIR-88448: Silently consume the reward.
					// This means the reward was granted after the event is completed, hence dont show the ui 
					consumeReward(eventId);
				}
				// else, Event is active, but not yet initialized: user needs to re-login to view the reward  
			}
			else
			{
				QFCMapDialog.showMVPWinAward(eventId, tokenCredits, getPlayerCloneByZid(SlotsPlayer.instance.socialMember.zId), mvpRewardList, rewardCollectCallback);
			}
		}

		private void onMVPRewardInfo(JSON data)
		{
			long totalCredits = 0;

			JSON mvpRewardsJson = data.getJSON("rewards");
			// key credits
			List<QFCReward> teamMvpRewardList = parseJSONRewards(mvpRewardsJson, "home_team");
			// MVP rewards
			List<QFCReward> opponentMvpRewardList = parseJSONRewards(mvpRewardsJson, "away_team");
			
			List<QFCReward> playerReward = parseJSONRewards(data, "token_rewards");
			
			if (playerReward != null)
			{
				for (int i = 0; i < playerReward.Count; i++)
				{
					if (playerReward[i].type == RewardCoins.TYPE)
					{
						totalCredits += playerReward[i].value;
					}
				}
			}
			
			string eventId = data.getString("event", "");
			if (totalCredits > 0)
			{
				rewardAmounts[eventId] = totalCredits;
				
				// Add pending credits to avoid desync.
				Server.handlePendingCreditsCreated(QFC_REWARD_PENDING_CREDIT_SOURCE, totalCredits);
			}
			
			if (!isEnabled)
			{
				if (!isEventActive())
				{
					// HIR-88448: Silently consume the reward.
					// This means the reward was granted after the event is completed, hence dont show the ui 
					consumeReward(eventId);
				}
				// else, Event is active, but not yet initialized: user needs to re-login to view the reward  
			}
			else
			{
				JSON mvpZids = data.getJSON("mvp_zids");
				QFCMapDialog.showNonMVPAward(eventId, totalCredits,  getPlayerCloneByZid(mvpZids.getString("home_team","")), teamMvpRewardList, getPlayerCloneByZid(mvpZids.getString("away_team","")),opponentMvpRewardList, rewardCollectCallback);
			}
		}

		protected override void clearEventDelegates()
		{
			Server.unregisterEventDelegate(RACE_CHEST_EVENT, onChestAwarded, true);
			Server.unregisterEventDelegate(CONTEST_COMPLETE_EVENT, onContestComplete, true);
			Server.unregisterEventDelegate(RACE_INFO_EVENT, onNewRace, true);
			Server.unregisterEventDelegate(PLAYER_PROGRESS_EVENT, onPlayerProgress, true);
			Server.unregisterEventDelegate(NODE_REWARD_EVENT, onNodeReward, true);
			Server.unregisterEventDelegate(RACE_COMPLETE_EVENT, onRaceComplete, true);
			Server.unregisterEventDelegate(TOKEN_REWARD_EVENT, onKeyReward, true);
			Server.unregisterEventDelegate(TOASTER_ROUND_COMPLETE, onRoundComplete, true);
			Server.unregisterEventDelegate(MINI_GAME_EVENT, onMiniGameAwarded, true);
			Server.unregisterEventDelegate(QFC_MVP_REWARD, onMVPReward, true);
			Server.unregisterEventDelegate(QFC_MVP_REWARD_INFO, onMVPRewardInfo, true);
		}

		//Server event sent when the current player wins tokens or advances nodes
		public void onPlayerProgress(JSON data)
		{
			//ignore progress events if the feature is not active
			if (!isEnabled)
			{
				return;
			}

			//check if this progress event is in the past
			int timestamp = data.getInt("creation_time", 0);
			if (timestamp < authoritativeTimestamp)
			{
				return;
			}

			int keysWon = data.getInt("tokens_won", 0);
			int newPlayerNode = data.getInt("new_node", 0);
			string zid = data.getString("zid", "-1");
			string eventId = data.getString("event_id", "");
			int currentRaceIndex = data.getInt("race_index", raceIndex);
			bool requestRaceInfo = currentRaceIndex != raceIndex;

			PreferencesBase prefs = SlotsPlayer.getPreferences();

			if (data.hasKey("lead"))
			{
				if (!string.IsNullOrEmpty(data.getString("lead", "", "")))
				{
					//show toaster for new lead, priority highest
					bool currentLead = data.getBool("lead", false);
					cachedLead = prefs.GetInt(Prefs.CACHED_TEAM_LEAD, 0) == 0 ? false : true;
					if (currentLead != cachedLead)
					{
						Dict args = Dict.create(D.OPTION, QFCToaster.TOASTER_SUB_TYPE.TEAM_LEAD, D.PLAYER, zid, D.VALUE, keysWon, D.KEY, currentLead);
						scheduleToaster(args);
						int cachedLeadInt = currentLead == false ? 0 : 1;
						prefs.SetInt(Prefs.CACHED_TEAM_LEAD, cachedLeadInt);
						prefs.Save();
					}
				}
			}

			if (playerInfos != null && playerInfos.ContainsKey(zid))
			{
				if (zid == SlotsPlayer.instance.socialMember.zId)
				{
					// current player jumping to story node
					if (!string.IsNullOrEmpty(nodeData[newPlayerNode].storyLocalizationBody))
					{
						QFCMapDialog.showKeys(eventId, keysWon, newPlayerNode, null, currentRaceIndex, topOfStack: false, waitTime: 3.0f);
					}
					else
					{	// current player jumping to non-story node
						if (onPlayerProgressToNonStoryNodeEvent != null)
						{
							onPlayerProgressToNonStoryNodeEvent.Invoke(zid, keysWon);
						}

						//Unpause the timer instantly since we're skipping the dialog and not interrupting the player's spins
						if (GameState.game != null)
						{
							RoyalRushAction.unPauseQFCEvent(GameState.game.keyName);
						}

						awardKeys(zid, keysWon);
						QFCMapDialog.pendingJumpsCount++;
						
						if (requestRaceInfo)
						{
							//Request current race info if we're getting progress for a different race index than we think we're on
							QFCAction.getCurrentRaceInformation(competitionId, currentRaceIndex);
						}
					}
				}
				else
				{	// progress by other players, no key animations
					awardKeys(zid, keysWon);
					int progressAmount = newPlayerNode - playerInfos[zid].position;
					advancePlayer(zid, progressAmount);
					if (currentRaceIndex != raceIndex)
					{
						//Request current race info if we're getting progress for a different race index than we think we're on
						QFCAction.getCurrentRaceInformation(competitionId, currentRaceIndex);
					}
				}

				if (isPlayerOnHomeTeam(zid) && !requestRaceInfo)
				{
					//show toaster for team needs x keys to win
					int keysNeedToWin = requiredKeys - getTeamKeyTotal(QFCTeams.HOME) - keysWon;   //evaluate keys needed after current key wins
					bool shouldPopKeyTriggerToaster = false;    
					if (keysNeedToWin > 0)
					{
						keysTriggerIndex = prefs.GetInt(Prefs.KEYS_TRIGGER_INDEX, -1);        //index points to the threshold in the trigger list that player will reach next

						//if player passed multiple threshold at once, we pop only one toaster which is triggered by the last threshold
						for (int i = 0; i < keyNumTriggerList.Count; i++)
						{
							if (keysNeedToWin <= keyNumTriggerList[i] && keysTriggerIndex < i)
							{
								shouldPopKeyTriggerToaster = true;
								keysTriggerIndex = i;
							}
						}
						if (shouldPopKeyTriggerToaster)
						{
							showKeysToWinToaster(keysNeedToWin);
							prefs.SetInt(Prefs.KEYS_TRIGGER_INDEX, keysTriggerIndex);
							prefs.Save();
						}
					}
					if(zid != SlotsPlayer.instance.socialMember.zId && shouldPopKeyTriggerToaster == false) 
					{
						//show toaster for token collect, priority lowest
						Dict args = Dict.create(D.OPTION, QFCToaster.TOASTER_SUB_TYPE.KEYS_AWARDED, D.PLAYER, zid, D.VALUE, keysWon);
						scheduleToaster(args);
					}
				}
			}

			if (Data.debugMode)
			{
				if (DevGUIMenuQuestForTheChest.playerProgressEvents == null)
				{
					DevGUIMenuQuestForTheChest.playerProgressEvents = new RollingList<JSON>(DevGUIMenuQuestForTheChest.maximumLogCount);
				}
				DevGUIMenuQuestForTheChest.playerProgressEvents.Add(data);
				DevGUIMenuQuestForTheChest.isDirty = true;
			}
		}

		private bool scheduleToaster(Dict args)
		{
			//prevent taoster from displaying if we get more than one at once or we're on cooldown
			if (isWaitingToShowToaster || !isToasterValidToSurface(args))
			{
				return false;
			}

			isWaitingToShowToaster = true;
			RoutineRunner.instance.StartCoroutine(showToasterRoutine(args));

			return true;
		}

		public void onToasterClose()
		{
			lastToasterTime = System.DateTime.UtcNow.Ticks;
			isWaitingToShowToaster = false;
		}

		private IEnumerator showToasterRoutine(Dict args)
		{
			yield return new WaitForSeconds(TOASTER_WAIT_TIME);
			ToasterManager.addToaster(ToasterType.QUEST_FOR_THE_CHEST, args);
		}

		private bool isToasterValidToSurface(Dict answerArgs)
		{
			QFCToaster.TOASTER_SUB_TYPE subType = (QFCToaster.TOASTER_SUB_TYPE)answerArgs.getWithDefault(D.OPTION, QFCToaster.TOASTER_SUB_TYPE.KEYS_AWARDED);
			switch (subType)
			{
				case QFCToaster.TOASTER_SUB_TYPE.KEYS_AWARDED:
					return 0 >= (lastToasterTime + (new System.TimeSpan(0, 0, toasterCooldown)).Ticks - System.DateTime.UtcNow.Ticks);

				default:
					return true;
			}
		}

		public void showKeysToWinToaster(int keysNeedToWin)
		{
			Dict args = Dict.create(D.OPTION, QFCToaster.TOASTER_SUB_TYPE.KEYS_TO_WIN, D.KEYS_NEED, keysNeedToWin);  
			scheduleToaster(args);
		}

		public void onConsumeRaceFailed()
		{
			currentConsumeRaceIndex = -1;
			if (playerInfos == null)
			{
				return;
			}

			foreach (QFCPlayer player in playerInfos.Values)
			{
				if (player == null)
				{
					continue;
				}
				player.keys = 0;
			}
		}

		public int awardKeys(string zid, int amount = 1)
		{
			QFCPlayer player = null;
			if (playerInfos.TryGetValue(zid, out player))
			{
				player.keys += amount;
				player.lastKeyTimestamp = GameTimer.currentTime;
			}
			else
			{
				Debug.LogError("Invalid player to award tokens: " + zid);
				return 0;
			}

			if (onPlayerAwardTokenEvent != null)
			{
				onPlayerAwardTokenEvent.Invoke(zid, player.keys);
			}

			return player.keys;
		}

		public int maxNode
		{
			get { return nodeData == null ? 0 : nodeData.Count - 1; }
		}

		public bool isPlayerOnHomeTeam(string zid)
		{
			if (homeTeam == null)
			{
				Debug.LogError("feature not initialized");
				return false;
			}

			for (int i = 0; i < homeTeam.Count; i++)
			{
				if (homeTeam[i].zId == zid)
				{
					return true;
				}
			}

			return false;
		}

		public QFCTeams getTeamForPlayer(string zid)
		{
			if (homeTeam == null)
			{
				Debug.LogError("Feature not initialized");
				return QFCTeams.AWAY;
			}

			for (int i = 0; i < homeTeam.Count; i++)
			{
				if (homeTeam[i].zId == zid)
				{
					return QFCTeams.HOME;
				}
			}

			return QFCTeams.AWAY;
		}

		public int getRoundForPlayer(string zid)
		{
			QFCPlayer teamPlayer = null;
			if (playerInfos.TryGetValue(zid, out teamPlayer))
			{
				return teamPlayer.round;
			}
			else
			{
				Debug.LogError("Invalid player");
				return 0;
			}
		}

		public int getPositionForPlayer(string zid)
		{
			QFCPlayer teamPlayer = null;
			if (playerInfos.TryGetValue(zid, out teamPlayer))
			{
				return teamPlayer.position;
			}
			else
			{
				Debug.LogError("Invalid player");
				return 0;
			}
		}

		public void udpatePlayerPosition(string zid, int newNode)
		{

			if (!playerInfos.ContainsKey(zid))
			{
				Debug.LogError("invalid player to move" + zid);
				return;
			}

			if (newNode >= nodeData.Count)
			{
				Debug.LogError("Invalid node");
				return;
			}

			int oldNode = playerInfos[zid].position;
			playerInfos[zid].position += newNode;

			if (oldNode > -1 && oldNode > newNode)
			{
				playerInfos[zid].round++;
			}

			nodeData[oldNode].occupiedBy.Remove(zid);
			nodeData[newNode].occupiedBy.Add(zid);

			//call advance event before finish event
			onPlayerAdvance(zid);
		}

		public int advancePlayer(string zid, int amount = 1)
		{
			QFCPlayer player = null;
			if (!playerInfos.TryGetValue(zid, out player))
			{
				Debug.LogError("invalid player to move" + zid);
				return 0;
			}
			else
			{
				int oldPosition = player.position;
				if (oldPosition > -1 && oldPosition < nodeData.Count)
				{
					if (nodeData[oldPosition] != null)
					{
						nodeData[oldPosition].occupiedBy.Remove(zid);
					}
					else
					{
						Debug.LogError("player " + player.name + " does not exist at old node: " + oldPosition);
					}
				}

				if (amount < 0) //If we're going backwards then force us to the start because we wrapped around the board
				{
					player.position = 0;
				}
				else
				{
					player.position += amount;
				}
			}

			//check boundary condition
			if (player.position > (totalNodes-1))
			{
				//we just completed a round,
				player.round++;
				player.position = 0;
			}

			if (player.position >= 0 && nodeData.Count > player.position && nodeData[player.position] != null)
			{
				nodeData[player.position].occupiedBy.Add(zid);
			}
			else
			{
				Debug.LogError("invalid node position");
			}

			//call advance event before finish event
			onPlayerAdvance(zid);

			//return position
			return player.position;
		}

		public QFCBoardNode getFirstBoardNode()
		{
			if (nodeData == null || nodeData.Count == 0)
			{
				return null;
			}

			return nodeData[0];
		}

		private void onPlayerAdvance(string zid)
		{
			//call event for listeners
			var handler = onPlayerAdvanceEvent;
			if (handler != null)
			{
				handler.Invoke(zid, playerInfos[zid].position);
			}
		}

		private void constructPlayerInfoForTeam(List<SocialMember> members, List<string> names, List<string> photos, List<int> positions, List<int> keys, List<int> timestamps)
		{
			if (members == null)
			{
				Debug.LogError("Invalid team");
				return;
			}
			
			for (int i = 0; i < members.Count; i++)
			{
				if (members[i] == null)
				{
					Debug.LogWarning("Invalid player in team list");
					continue;
				}
				QFCPlayer newPlayer = new QFCPlayer();
				newPlayer.position = (null == positions || positions.Count <= i) ? 0 : positions[i];
				newPlayer.keys = (null == keys || keys.Count <= i) ? 0 : keys[i];
				newPlayer.url = (null == photos || photos.Count <= i) ? "" : photos[i];
				newPlayer.name = (null == names || names.Count <= i) ? "" : names[i];
				newPlayer.lastKeyTimestamp = (null == timestamps || timestamps.Count <= i) ? 0 : timestamps[i];
				newPlayer.member = members[i];

				if (playerInfos == null)
				{
					playerInfos = new Dictionary<string, QFCPlayer>();
				}
				playerInfos[members[i].zId] = newPlayer;

				if (nodeData != null && nodeData.Count > newPlayer.position && newPlayer.position >= 0)
				{
					if (nodeData[newPlayer.position].occupiedBy == null)
					{
						nodeData[newPlayer.position].occupiedBy = new List<string>();
					}
					nodeData[newPlayer.position].occupiedBy.Add(members[i].zId);	
				}
				
			}
		}

		public void initTeams(List<SocialMember> home, List<string> homeNames, List<string> homePhotoURLs, List<int> homePositions,  List<int> homeTokenAmounts, List<int> homeTimestamps, List<SocialMember> away, List<string> awayNames, List<string> awayPhotoURLs, List<int> awayPositions, List<int> awayTokenAmounts, List<int> awayTimestamps)
		{
			//clear old team data
			playerInfos.Clear();

			homeTeam = home;
			awayTeam = away;

			constructPlayerInfoForTeam(home, homeNames, homePhotoURLs, homePositions, homeTokenAmounts, homeTimestamps);
			constructPlayerInfoForTeam(away, awayNames, awayPhotoURLs, awayPositions, awayTokenAmounts, awayTimestamps);
		}

		public void initNodes(JSON nodeRewards, QFCThemedStaticData themeData = null)
		{
			List<string> nodeJsonKeys = nodeRewards.getKeyList();
			if (nodeJsonKeys.Count > totalNodes)
			{
				Debug.LogWarning("more reward nodes than total nodes");
				totalNodes = nodeJsonKeys.Count + 1;
			}

			if (nodeData == null)
			{
				nodeData = new List<QFCBoardNode>(totalNodes);
			}
			else
			{
				nodeData.Clear();
			}
			
			QFCBoardNode prevNode = null;
			QFCBoardNode startingNode = new QFCBoardNode(0, 1);
			nodeData.Add(startingNode); //The starting node isn't setup in SCAT so its never part of the nodes json
			prevNode = startingNode;
			int storyIndex = 1;
			for (int i = 0; i < nodeJsonKeys.Count; i++)
			{
				float rewardAmount = nodeRewards.getFloat(nodeJsonKeys[i], 0);
				if (i == nodeJsonKeys.Count-1)
				{
					//The final node is the bonus game but doesn't have an associated rewardMultiplier
					//Forcing it to one so we recognize it needs to create a localization/texture paths
					rewardAmount = 1;
				}

				//TEMP - Assuming any node with a reward is a story node
				//This should be configurable some where and get included in the board JSON from the server
				//Passing in an invalid index for non-reward nodes so we skip creating localizations & asset paths
				QFCBoardNode newNode = new QFCBoardNode(rewardAmount > 0 ? storyIndex : -1, rewardAmount);
				nodeData.Add(newNode);
				if (prevNode != null)
				{
					prevNode.nextNode = newNode;
				}
				prevNode = newNode;
				
				if (rewardAmount > 0)
				{
					storyIndex++;
				}
			}
			
			if (themeData != null)
			{
				readThemeData(themeData);
			}
			else
			{
				parseNodeLocations();
			}

		}

		public void resetFeature(JSON data)
		{
			initializeWithData(data);
			onRestart();
		}

		private void onRestart()
		{
			var handler = onRestartEvent;
			if (handler != null)
			{
				handler.Invoke();
			}
		}

		private void consumeReward(string eventId)
		{
			if (!string.IsNullOrEmpty(eventId)) //prevents fake debug data from sending action
			{
				//add coins to wallet
				long coins = 0;
				if (rewardAmounts.TryGetValue(eventId, out coins))
				{
					//action to stop persistent server reward event.  Call this when we claim the reward
					SlotsPlayer.addFeatureCredits(coins, QFC_REWARD_PENDING_CREDIT_SOURCE);
					rewardAmounts.Remove(eventId);
				}

				QFCAction.claimReward(eventId);
			}
		}

		public void onChestAwarded(JSON data)
		{
			long teamCoinReward = data.getLong("team_coin_reward", 0);
			long normalizedReward = data.getLong("normalized_coin_reward", 0);
			int inflationFactor = data.getInt("inflation_factor", 0);
			int xpLevel = data.getInt("xp_level", 0);
			List<QFCReward> rewardList = null;
			JSON[] rewards = data.getJsonArray("rewards");
			if (rewards != null)
			{
				rewardList = new List<QFCReward>(rewards.Length + 1);
				for (int i = 0; i < rewards.Length; i++)
				{
					string type = rewards[i].getString("type", string.Empty);
					if (!type.IsNullOrWhiteSpace())
					{
						rewardList.Add(new QFCReward(type, rewards[i].getLong("value", 0)));
					}
					else
					{
						Debug.LogErrorFormat("No type sent in chest reward event for index {0}. Not awarding to the player", i);
					}
				}
			}
			else
			{
				rewardList = new List<QFCReward>(1);
			}

			rewardList.Add(new QFCReward("team_coin_reward", teamCoinReward));

			string eventId = data.getString("event", "");
			long totalCredits = 0;
			if (rewardList != null)
			{
				for (int i = 0; i < rewardList.Count; i++)
				{
					if (rewardList[i].type == RewardCoins.TYPE)
					{
						totalCredits += rewardList[i].value;
					}
				}
			}
			
			rewardAmounts[eventId] = totalCredits;
			
			// Add pending credits to avoid desync.
			Server.handlePendingCreditsCreated(QFC_REWARD_PENDING_CREDIT_SOURCE, totalCredits);
			
			var handler = onChestAwardedEvent;
			if (handler != null)
			{
				handler.Invoke(rewardList);
			}
			
			if (Data.debugMode)
			{
				if (DevGUIMenuQuestForTheChest.chestRewardEvents == null)
				{
					DevGUIMenuQuestForTheChest.chestRewardEvents = new RollingList<JSON>(DevGUIMenuQuestForTheChest.maximumLogCount);
				}
				DevGUIMenuQuestForTheChest.chestRewardEvents.Add(data);
				DevGUIMenuQuestForTheChest.isDirty = true;
			}

			if (!isEnabled) 
			{
				if (!isEventActive())
				{
					// HIR-88448: Silently consume the reward.
					// This means the chest reward was granted after the event is completed, hence dont show the ui 
					consumeReward(eventId);
				}
				// else, Event is active, but user needs to re-login to view the reward  
			}
			else
			{
				QFCMapDialog.showWinChest(eventId, teamCoinReward, totalCredits, xpLevel, inflationFactor, normalizedReward, rewardCollectCallback);
			}
		}

		public void onContestComplete(JSON data)
		{
			competitionId = data.getInt("start_time", 0);
			raceComplete = true;
		}

		protected void onRaceEnd(Dict args = null, GameTimerRange originalTimer = null)
		{
			//this function is called when the client side timer for the current race ends (server event should still come).
			//show toaster
			scheduleToaster(Dict.create(D.OPTION, QFCToaster.TOASTER_SUB_TYPE.CONTEST_ENDED));
		}

		private void getKeyTotals(JSON teamData, ref Dictionary<string, int> keyData)
		{
			if (keyData == null)
			{
				keyData = new Dictionary<string, int>();
			}

			List<string> zids = teamData.getKeyList();
			if (zids == null || zids.Count <= 0)
			{
				return;
			}

			int teamSize = zids.Count;
			for (int i = 0; i < teamSize; i++)
			{
				if (string.IsNullOrEmpty(zids[i]))
				{
					Debug.LogError("Invalid team member");
					continue;
				}

				keyData[zids[i]] = teamData.getInt(zids[i], 0);
			}
		}

		public void updateKeyData(int keyUnlockAmount, Dictionary<string, int> keyData)
		{
			requiredKeys = keyUnlockAmount;
			foreach(KeyValuePair<string, int> kvp in keyData)
			{
				QFCPlayer player = null;
				if (playerInfos.TryGetValue(kvp.Key, out player))
				{
					player.keys = kvp.Value;
				}
			}
		}

		//Event sent to all race contestants when one player completes the race
		public void onRaceComplete(JSON data)
		{
			int completeRaceIndex = data.getInt("race_index", -1);
			bool homeTeamWon = data.getBool("has_won", false);
			string winnerZid = data.getString("winner_zid", "");
			string eventId = data.getString("event", "");
			int keyUnlockAmount = data.getInt("required_keys", requiredKeys);
			winningKeyAmount = data.getInt("winning_keys_count", 1);
			raceCompleteEventIds.Enqueue(eventId);
			raceEventToIndexMap[eventId] = completeRaceIndex;

			JSON homeData = data.getJSON("home_team");
			JSON awayData = data.getJSON("away_team");
			Dictionary<string, int> keyData = null;
			if (homeData != null && awayData != null)
			{
				//init team keys
				getKeyTotals(homeData, ref keyData);
				getKeyTotals(awayData, ref keyData);
			}
			else
			{
				Debug.LogWarning("missing score details in race end event");
			}

			var handler = onRaceCompleteEvent;
			if (handler != null)
			{
				handler.Invoke(winnerZid, homeTeamWon);
			}

			//set race to complete
			raceComplete = true;

			if (Data.debugMode)
			{
				if (DevGUIMenuQuestForTheChest.raceCompleteEvents == null)
				{
					DevGUIMenuQuestForTheChest.raceCompleteEvents = new RollingList<JSON>(DevGUIMenuQuestForTheChest.maximumLogCount);
				}
				DevGUIMenuQuestForTheChest.raceCompleteEvents.Add(data);
				DevGUIMenuQuestForTheChest.isDirty = true;

				//if we did finish the current race, log it if we're in debug mode
				if (completeRaceIndex >= raceIndex)
				{
					QFCRaceData info = getCurrentRaceInfo();
					if (info != null)
					{
						if (raceInformation == null)
						{
							raceInformation = new Dictionary<int, QFCRaceData>();
						}
						raceInformation[completeRaceIndex] = info;

						saveCompetitionData();
					}
				}
			}

			
			if (!isEnabled)
			{
				if (!isEventActive())
				{
					consumeRaceComplete(eventId);
					// HIR-88448: Silently consume the event. Race complete event was sent after qfc event was ended.
				}
				// Ignore it for now, next login it will be processed
			}
			else
			{
				if (!homeTeamWon)
				{
					//show loser dialog
					QFCMapDialog.showRaceLost(eventId, winningKeyAmount, winnerZid, keyData, keyUnlockAmount, null);
				}
				else if (winnerZid != SlotsPlayer.instance.socialMember.zId) //Only show the final key win animation if someone else on your team gets the winning keys
				{
					QFCMapDialog.showHomeTeamChestWinner(winningKeyAmount, winnerZid, keyData, keyUnlockAmount, null, eventId);
				}
			}

		}

		public string getCompletedRaceId()
		{
			if (raceCompleteEventIds.IsEmpty())
			{
				return "";
			}

			return raceCompleteEventIds.Peek();
		}

		public QFCSpinPanelMessageBox spinPanelMessageBox
		{
			get
			{
				if (SpinPanel.hir != null && SpinPanel.hir.featureButtonHandler != null)
				{
					return SpinPanel.hir.featureButtonHandler.qfcSpinPanelMessageBox;
				}

				return null;
			}
		}

		public void handleWagerChange()
		{
			if (isEnabled && GameState.game != null)
			{
				if (spinPanelMessageBox != null)
				{
					spinPanelMessageBox.onWagerChange();
				}
			}
		}

		public void handleSpinClicked()
		{
			if (isEnabled && GameState.game != null)
			{
				if (spinPanelMessageBox != null)
				{
					spinPanelMessageBox.onSpinClicked();
				}
			}
		}

		public int getRaceIndexForCompleteEvent(string eventId)
		{
			int index = 0;
			if (raceEventToIndexMap.TryGetValue(eventId, out index))
			{
				return index;
			}

			return -1;
		}

		//call this when user clicks on the race complete dialog
		public void consumeRaceComplete(string eventId)
		{
			if (!string.IsNullOrEmpty(eventId))  //prevents actions being sent when input is faked
			{
				//this will cause a new race info event to occur
				QFCAction.completeRace(eventId);
			}

			string expectedEvent = raceCompleteEventIds.Dequeue();
			currentConsumeRaceIndex = getRaceIndexForCompleteEvent(eventId);

			if (Data.debugMode && expectedEvent != eventId)
			{
				Debug.LogError("Event Id doesn't match expected event ID");
			}
		}

		private List<QFCReward> parseJSONRewards(JSON data, string rewardStr)
		{
			List<JSON> rewards = new List<JSON>(data.getJsonArray(rewardStr));
			if (rewards.Count == 0)
			{
				// data was possibly not an array
				JSON reward = data.getJSON(rewardStr);
				if (reward != null)
				{
					rewards.Add(reward);
				}
			}

			List<QFCReward> rewardList = null;
			rewardList = new List<QFCReward>(rewards.Count);
			for (int i = 0; i < rewards.Count; i++)
			{
				string type = rewards[i].getString("type", string.Empty);
				switch (type)
				{
					case RewardCoins.TYPE:
					case RewardElitePassPoints.TYPE:
						rewardList.Add(new QFCReward(type, rewards[i].getLong("value", 0)));
						break;
					case RewardCardPack.TYPE:
						rewardList.Add(new QFCReward(type, rewards[i].getString("value", "")));
						break;
					default:
						if (!type.IsNullOrWhiteSpace())
						{
							rewardList.Add(new QFCReward(type, rewards[i].getLong("value", 0)));
						}
						else
						{
							Debug.LogErrorFormat("No type sent in reward event for index {0}. Not awarding to the player", i);
						}
						break;
				}
			}

			return rewardList;
		}

		//Event at the end of a race for the reward value of the leftover tokens conversion to credits
		public void onKeyReward(JSON data)
		{
			long totalCredits = 0;
			List<QFCReward> rewardList = parseJSONRewards(data, "rewards");
			if (rewardList != null)
			{
				for (int i = 0; i < rewardList.Count; i++)
				{
					if (rewardList[i].type == RewardCoins.TYPE)
					{
						totalCredits += rewardList[i].value;
					}
				}
			}

			var handler = onKeyRewardEvent;
			if (handler != null)
			{
				handler.Invoke(rewardList);
			}
			
			if (Data.debugMode)
			{
				if (DevGUIMenuQuestForTheChest.tokenRewardEvents == null)
				{
					DevGUIMenuQuestForTheChest.tokenRewardEvents = new RollingList<JSON>(DevGUIMenuQuestForTheChest.maximumLogCount);
				}
				DevGUIMenuQuestForTheChest.tokenRewardEvents.Add(data);
				DevGUIMenuQuestForTheChest.isDirty = true;
			}

			string eventId = data.getString("event", "");
			if (totalCredits > 0)
			{
				rewardAmounts[eventId] = totalCredits;
				
				// Add pending credits to avoid desync.
				Server.handlePendingCreditsCreated(QFC_REWARD_PENDING_CREDIT_SOURCE, totalCredits);
			}

			if (!isEnabled) 
			{
				if (!isEventActive())
				{
					// HIR-88448: Silently consume the reward.
					// This means the reward was granted after the event is completed, hence dont show the ui 
					consumeReward(eventId);
				}
				// else, Event is active, but not yet initialized: user needs to re-login to view the reward  
			}
			else
			{
				QFCMapDialog.showKeysAward(eventId, totalCredits, rewardCollectCallback);
			}
		}

		public void onNodeReward(JSON data)
		{
			if (instance == null)
			{
				Bugsnag.LeaveBreadcrumb("Can't award node when qfc feature is not initialized");
#if UNITY_EDITOR
				Debug.LogWarning("Can't award node when qfc feature not initialized");
#endif
				return;
			}

			int nodeIndex = data.getInt("node", 0);

			long totalCredits = 0;
			List<QFCReward> rewardList = parseJSONRewards(data, "rewards");
			if (rewardList != null)
			{
				for (int i = 0; i < rewardList.Count; i++)
				{
					if (rewardList[i].type == RewardCoins.TYPE)
					{
						totalCredits += rewardList[i].value;
					}
				}
			}

			if (Data.debugMode)
			{
				if (DevGUIMenuQuestForTheChest.nodeRewardEvents == null)
				{
					DevGUIMenuQuestForTheChest.nodeRewardEvents = new RollingList<JSON>(DevGUIMenuQuestForTheChest.maximumLogCount);
				}
				DevGUIMenuQuestForTheChest.nodeRewardEvents.Add(data);
				DevGUIMenuQuestForTheChest.isDirty = true;
			}

			var handler = onNodeRewardEvent;
			if (handler != null)
			{
				handler.Invoke(rewardList);
			}

			string eventId = data.getString("event", "");

			if (totalCredits > 0)
			{
				rewardAmounts[eventId] = totalCredits;
				
				// Add pending credits to avoid desync.
				Server.handlePendingCreditsCreated(QFC_REWARD_PENDING_CREDIT_SOURCE, totalCredits);
			}

			if (!isEnabled && isEventActive())
			{
				//event is active, but user is not a team yet, ignore this event until user is put on a team
				//do nothing, wait for user to login again.
			}
			else
			{
				QFCMapDialog.showReward(eventId, nodeIndex, rewardList, rewardCollectCallback);
			}

		}

		private void rewardCollectCallback(Dict args)
		{
			string eventId = (string)args.getWithDefault(D.EVENT_ID, "");

			string error = (string)args.getWithDefault(D.REASON, "");
			if (!string.IsNullOrEmpty(error))
			{
				Bugsnag.LeaveBreadcrumb("Could not show reward dialog: " + error);
			}
			consumeReward(eventId);
		}

		private QFCRaceData getCurrentRaceInfo()
		{
			if (homeTeam == null || awayTeam == null)
			{
				return null;
			}

			QFCRaceData raceData = new QFCRaceData
			{
				index = raceIndex,
				homeKeyTotal = getTeamKeyTotal(QFCTeams.HOME),
				awayKeyTotal = getTeamKeyTotal(QFCTeams.AWAY),
				homeFinalPositions = new int[homeTeam.Count],
				awayFinalPositions = new int[awayTeam.Count]
			};

			for (int i = 0; i < homeTeam.Count; i++)
			{
				SocialMember member = homeTeam[i];
				if (member == null)
				{
					continue;
				}
				raceData.homeFinalPositions[i] = getPositionForPlayer(member.zId);
			}

			for (int i = 0; i < awayTeam.Count; i++)
			{
				SocialMember member = awayTeam[i];
				if (member == null)
				{
					continue;
				}
				raceData.awayFinalPositions[i] = getPositionForPlayer(member.zId);
			}

			return raceData;
		}

		public void onNewRace(JSON data)
		{
			onNewRace(data, null, true);
		}
		
		public void onNewRace(JSON data, QFCThemedStaticData themeData, bool searchForSocialMembers)
		{
			if (data == null)
			{
				Bugsnag.LeaveBreadcrumb("QFC::onNewRace -- Invalid race data");
				return;
			}

			int startTime = data.getInt("start_time", 0);
			competitionId = startTime;
			int endTime = data.getInt("end_time", 0);

			//update timestamp if sent
			authoritativeTimestamp = data.getInt("creation_time", authoritativeTimestamp);
			
			setTimestamps(startTime,endTime);
			featureTimer.registerFunction(onRaceEnd);
			
			raceIndex = data.getInt("race_index", 0);
			totalNodes = data.getInt("total_nodes", 0);
			totalNodes += 1; //Starting Node isn't included in SCAT so this is one off
			requiredKeys = data.getInt("tokens_required", 0);

			//reset toaster triggers
			keysTriggerIndex = -1;
			PreferencesBase prefs = SlotsPlayer.getPreferences();
			prefs.SetInt(Prefs.KEYS_TRIGGER_INDEX, -1);
			
			if (featureTimer != null)
			{
				toasterTimeTriggerData = ExperimentWrapper.QuestForTheChest.toasterTimeTrigger; 
				if (!string.IsNullOrEmpty(toasterTimeTriggerData))
				{
					string[] toasterTimeTriggeStrings = toasterTimeTriggerData.Split(',');
					//triggers toaster contest ending in X events
					for (int i = 0; i < toasterTimeTriggeStrings.Length; i++)
					{
						float timerTrigger = 0.0f;  
						if(float.TryParse(toasterTimeTriggeStrings[i], out timerTrigger))
						{
							int timerTriggerSeconds = (int)(timerTrigger * 60 * 60);
							featureTimer.registerFunction(onContestEndingSoon,
								Dict.create(D.OPTION, QFCToaster.TOASTER_SUB_TYPE.CONTEST_ENDING, D.TIME_LEFT, timerTriggerSeconds),    
								timerTriggerSeconds);  
						} 
					}
				}
				else
				{
					Debug.Log("No EOS data for quest for the chest toaster time triggers.");
				}
			}

			//grab key trigger data from EOS
			toasterKeyTriggerData = ExperimentWrapper.QuestForTheChest.toasterKeyTrigger; 
			if (!string.IsNullOrEmpty(toasterKeyTriggerData))
			{
				string[] toasterKeyTriggeStrings = toasterKeyTriggerData.Split(',');
				for (int i = 0; i < toasterKeyTriggeStrings.Length; i++)
				{
					int keyNumTrigger = 0;
					if(int.TryParse(toasterKeyTriggeStrings[i], out keyNumTrigger))
					{
						//sanity check the value of the trigger against requiredTokens of this particular race
						//only append it when it makes sense
						//so the list is populated with healthy triggers for current race
						if(requiredKeys > keyNumTrigger)
						{
							keyNumTriggerList.Add(keyNumTrigger);
						}
					}
				}

				keyNumTriggerList.Sort();
				prefs.SetInt(Prefs.KEYS_TRIGGER_INDEX, keysTriggerIndex);
			}
			else
			{
				prefs.SetInt(Prefs.KEYS_TRIGGER_INDEX, keysTriggerIndex);
				Debug.Log("No EOS data for quest for the chest toaster keys triggers.");
			}

			//reset complete flag
			raceComplete = false;
			currentConsumeRaceIndex = -1;

			//reset team lead
			prefs.SetInt(Prefs.CACHED_TEAM_LEAD, 0);
			prefs.Save();

			JSON nodeRewardData = data.getJSON("node_rewards");
			if (nodeRewardData != null)
			{
				initNodes(nodeRewardData, themeData);
			}
			else if (nodeData != null && nodeData.Count > 0)
			{
				for (int i = 0; i < nodeData.Count; i++)
				{
					nodeData[i].occupiedBy.Clear();
				}
			}

			List<SocialMember> homeTeamMembers = null;
			List<string> homeNames = null;
			List<string> homePhotos = null;
			List<int> homePositions = null;
			List<int> homeTokens = null;
			List<int> homeTimestamps = null;

			JSON homeTeamJson = data.getJSON("home_team");
			if(!parseTeamJSON(homeTeamJson, searchForSocialMembers, out homeTeamMembers, out homeNames, out homePhotos, out homePositions, out homeTokens, out homeTimestamps))
			{
				Debug.LogError("Could not get home team");
			}

			List<SocialMember> awayTeamMembers = null;
			List<string> awayNames = null;
			List<string> awayPhotos = null;
			List<int> awayPositions = null;
			List<int> awayTokens = null;
			List<int> awayTimestamps = null;

			JSON awayTeamJson = data.getJSON("away_team");

			if(!parseTeamJSON(awayTeamJson, searchForSocialMembers, out awayTeamMembers, out awayNames, out awayPhotos, out awayPositions, out awayTokens, out awayTimestamps))
			{
				Debug.LogError("Could not get away team");
			}

			//initialize teams
			initTeams(homeTeamMembers, homeNames, homePhotos, homePositions, homeTokens, homeTimestamps, awayTeamMembers, awayNames, awayPhotos, awayPositions, awayTokens, awayTimestamps);

			//broadcast new race event
			var handler = onNewRaceEvent;
			if (handler != null)
			{
				handler.Invoke();
			}

			prefs.Save();

			if (Data.debugMode)
			{
				DevGUIMenuQuestForTheChest.isDirty = true;
			}

			QFCMapDialog.showNewRace();
		}

		private static bool parseTeamJSON(JSON teamData, bool searchForSocialMembers, out List<SocialMember> members, out List<string> names, out List<string> urls, out List<int> positions, out List<int> keys, out List<int> keyTimestamps)
		{
			if (teamData == null)
			{
				members = null;
				urls = null;
				positions = null;
				keys = null;
				keyTimestamps = null;
				names = null;
				return false;
			}

			List<string> zids = teamData.getKeyList();
			if (zids == null || zids.Count <= 0)
			{
				members = null;
				urls = null;
				positions = null;
				keys = null;
				keyTimestamps = null;
				names = null;
				return false;
			}

			int teamSize = zids.Count;
			members = new List<SocialMember>(teamSize);
			urls = new List<string>(teamSize);
			names = new List<string>(teamSize);
			positions = new List<int>(teamSize);
			keys = new List<int>(teamSize);
			keyTimestamps = new List<int>(teamSize);


			for (int i = 0; i < teamSize; i++)
			{
				if (string.IsNullOrEmpty(zids[i]))
				{
					Debug.LogError("Invalid team member");
					continue;
				}

				JSON playerData = teamData.getJSON(zids[i]);
				string photo = "";
				string playerName = "";
				if (playerData != null)
				{
					int currentNode = playerData.getInt("current_node", 0);
					positions.Add(currentNode);

					photo = playerData.getString("photo", "");
					urls.Add(photo);

					int currentTokens = playerData.getInt("tokens_earned", 0);
					keys.Add(currentTokens);

					int timestamp = playerData.getInt("last_token_timestamp", 0);
					keyTimestamps.Add(timestamp);

					playerName = playerData.getString("name", "");
					names.Add(playerName);
				}

				SocialMember teamMember = null;
				if (searchForSocialMembers)
				{
					teamMember = CommonSocial.findOrCreate("", zids[i], imageUrl: photo, firstName:playerName);
				}
				if (teamMember == null)
				{
					teamMember = new SocialMember("-1", zids[i]);
				}

				members.Add(teamMember);
			}
			return true;
		}

		private void saveCompetitionData()
		{

			//save last race into player prefs
			PreferencesBase prefs = SlotsPlayer.getPreferences();

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("{");
			sb.AppendLine("\t\"" + competitionId + "\" : [");

			if (raceInformation != null)
			{
				List<QFCRaceData> races = raceInformation.Values.ToList();
				for (int i = 0; i < races.Count; i++)
				{
					if (i != 0)
					{
						sb.AppendLine(",");
					}

					sb.AppendLine(races[i].toJSONString());
				}
			}

			sb.AppendLine("\t]");
			sb.AppendLine("}");

			prefs.SetString(Prefs.QFC_RACE_INFO, sb.ToString());
			prefs.Save();
		}

		private void initOldRaceData()
		{
			PreferencesBase prefs = SlotsPlayer.getPreferences();
			string data = prefs.GetString(Prefs.QFC_RACE_INFO, "");
			if (string.IsNullOrEmpty(data))
			{
				return;
			}

			JSON json = new JSON(data);
			List<string> keys = json.getKeyList();
			if (keys == null || keys.Count != 1)
			{
				return;
			}

			if (raceInformation == null)
			{
				raceInformation = new Dictionary<int, QFCRaceData>();
			}
			raceInformation.Clear();

			JSON[] races = json.getJsonArray(keys[0]);
			if (races != null)
			{
				for (int i = 0; i < races.Length; i++)
				{
					QFCRaceData raceItem = new QFCRaceData(races[i]);
					raceInformation[raceItem.index] = raceItem;
				}
			}
		}

		public QFCRaceData getOldRaceData(int index)
		{
			QFCRaceData data = null;
			if (raceInformation == null)
			{
				initOldRaceData();
			}

			//if we still don't have race data, we're not going to get it
			if (raceInformation == null)
			{
				return null;
			}

			//convert to actual race info index (This case can occur easily in a dev environment, or if the user switches devices)
			int currentIndex = 0;
			int infoIndex = -1;
			foreach (int key in raceInformation.Keys)
			{
				if (currentIndex == index)
				{
					infoIndex = key;
					break;
				}

				++currentIndex;
			}

			if (!raceInformation.TryGetValue(infoIndex, out data))
			{
				return null;
			}

			return data;
		}
		
		public int numSavedRaces
		{
			get
			{
				if (raceInformation == null)
				{
					initOldRaceData();
				}

				if (raceInformation == null)
				{
					return 0;
				}

				return raceInformation.Count;
			}
		}

		private void resetAllPlayers()
		{
			if (homeTeam == null || awayTeam == null)
			{
				Debug.LogWarning("Teams not initialized");
				return;
			}

			for (int i = 0; i < homeTeam.Count; i++)
			{
				QFCPlayer player = null;
				if (playerInfos.TryGetValue(homeTeam[i].zId, out player))
				{
					player.keys = 0;
				}
			}

			for (int i = 0; i < awayTeam.Count; i++)
			{
				QFCPlayer player = null;
				if (playerInfos.TryGetValue(awayTeam[i].zId, out player))
				{
					player.keys = 0;
				}
			}
			
			onRestart();
		}

		public int getCurrentUserKeyTotal()
		{
			QFCPlayer player = null;
			if (playerInfos.TryGetValue(SlotsPlayer.instance.socialMember.zId, out player))
			{
				return player.keys;
			}

			return 0;
		}

		private int getTeamKeyTotal(List<SocialMember> team)
		{
			if (team == null)
			{
				return 0;
			}

			int teamTotal = 0;
			for (int i = 0; i < team.Count; i++)
			{
				if (team[i] == null)
				{
					continue;
				}

				QFCPlayer teamPlayer = null;
				if (playerInfos.TryGetValue(team[i].zId, out teamPlayer))
				{
					teamTotal += teamPlayer.keys;
				}
			}

			return teamTotal;
		}

		public int getTeamKeyTotal(QFCTeams team)
		{
			switch (team)
			{
				case QFCTeams.AWAY:
					return getTeamKeyTotal(awayTeam);


				case QFCTeams.HOME:
					return getTeamKeyTotal(homeTeam);
				
				default:
					return 0;
			}
		}

		public List<SocialMember> getTeamMembersAsSocialMembers(QFCTeams team)
		{
			//return a new list that has the same elements as the home or away team, so that external code
			//cannot manipulate our teams
			switch (team)
			{
				case QFCTeams.AWAY:
					return awayTeam == null ? null : new List<SocialMember>(awayTeam);


				case QFCTeams.HOME:
					return homeTeam == null ? null : new List<SocialMember>(homeTeam);
			}
			
			return null;
		}

		public Dictionary<string, QFCPlayer> getTeamMembersAsPlayerDict(QFCTeams team)
		{
			Dictionary<string, QFCPlayer> teamToReturn = new Dictionary<string, QFCPlayer>();
			List<SocialMember> teamSocialMembers = getTeamMembersAsSocialMembers(team);
			if (null != teamSocialMembers)
			{
				for (int i = 0; i < teamSocialMembers.Count; i++)
				{
					QFCPlayer teamPlayer;
					if (playerInfos.TryGetValue(teamSocialMembers[i].zId, out teamPlayer))
					{
						teamToReturn.Add(teamSocialMembers[i].zId, teamPlayer);
					}
				}
			}
			return teamToReturn;
		}

		public void onRoundComplete(JSON data)
		{
			string zid = data.getString("zid", "-1");

			QFCPlayer teamPlayer = null;
			if (playerInfos.TryGetValue(zid, out teamPlayer))
			{
				teamPlayer.round++;
			}

			//show toaster
			Dict args = Dict.create(D.OPTION, QFCToaster.TOASTER_SUB_TYPE.ROUND_COMPLETE, D.PLAYER, zid);
			scheduleToaster(args);
		}

		public void onContestEndingSoon(Dict data = null, GameTimerRange sender = null)
		{ 
			Dict args = data;
			scheduleToaster(args);
		}

		public void parseNodeLocations()
		{
			AssetBundleManager.load(string.Format(NODE_LOCATION_MAP_PREFAB_PATH, ExperimentWrapper.QuestForTheChest.theme), assetBundleSuccess, assetBundleFailure, isSkippingMapping:true, fileExtension:".prefab");
		}
		
		private void assetBundleSuccess(string assetPath, Object obj, Dict data = null)
		{
			if (obj != null)
			{
				QFCThemedStaticData themeData = (obj as GameObject).GetComponent<QFCThemedStaticData>();
				readThemeData(themeData);
			}
		}

		private void readThemeData(QFCThemedStaticData themeData)
		{
			if (themeData == null)
			{
				Debug.LogWarning("Invalid theme data");
				return;
			}

			if (themeData.nodeLocations.Length != nodeData.Count)
			{
				Debug.LogWarningFormat( "Number of node transforms {0} & number of actual nodes in data {1} don't match", themeData.nodeLocations.Length, nodeData.Count);
				Bugsnag.LeaveBreadcrumb(string.Format("Uneven number of node locations ({0}) and nodes in data ({1})", themeData.nodeLocations.Length, nodeData.Count));
			}

			rewardShroudColor = themeData.rewardShroudColor;

			for (int i = 0; i < themeData.nodeLocations.Length; i++)
			{
				if (i < nodeData.Count)
				{
					nodeData[i].position = themeData.nodeLocations[i].localPosition;
				}
				else
				{
					return; //No point in continuing if we've already exceeded the date from the server
				}
			}
		}

		/// <summary>
		/// We use the clone of the object as there is a chance for the object to change between reward grant event and
		/// actual display of the event.
		/// Mainly the key count could change over time.
		/// </summary>
		/// <param name="zid"></param>
		/// <returns></returns>
		private QFCPlayer getPlayerCloneByZid(string zid)
		{
			QFCPlayer player;
			if (playerInfos.TryGetValue(zid, out player))
			{
				return player.getClone();
			}
			return null;
		}
		
		public List<QFCPlayer> getTeamAsSortedList(QFCTeams team)
		{
			Dictionary<string, QFCPlayer> teamDict = getTeamMembersAsPlayerDict(team);
			List<QFCPlayer> playerList = teamDict.Values.ToList();
			playerList.Sort(compareTeamMembers);
			return playerList;
		}

		private int compareTeamMembers(QFCPlayer playerA, QFCPlayer playerB)
		{
			int defaultSort = playerB.keys.CompareTo(playerA.keys);
			if (defaultSort != 0)
			{
				return defaultSort;
			}

			return playerA.lastKeyTimestamp.CompareTo(playerB.lastKeyTimestamp);
		}

		private void assetBundleFailure(string assetPath, Dict data = null)
		{
			Debug.LogError("DOWNLOAD FAILED: " + assetPath);
		}

		public void onMiniGameAwarded(JSON data)
		{
			// Add pending credits to avoid desync if mini game wins coins
			if (data != null)
			{
				JSON[] bonusGameOutcomeJsonArray = data.getJsonArray("outcomes");
				if (bonusGameOutcomeJsonArray.Length > 0)
				{
					JSON bonusGameOutcomeJson = bonusGameOutcomeJsonArray[0];
					JSON coinRewardsJSON = data.getJSON("absolute_coin_rewards");
					int keysReward = data.getInt("tokens", 0);
					SlotOutcome bonusOutcome = new SlotOutcome(bonusGameOutcomeJson);
					ModularChallengeGameOutcome challengeGameOutcome = new ModularChallengeGameOutcome(bonusOutcome);
					
					// If it is not a keyreward and coin rewards JSON is not null
					if (keysReward <= 0 && coinRewardsJSON != null)
					{
						Dictionary<long, long> absoluteCoinRewards = convertAbsoluteCreditValues(coinRewardsJSON);
						long winId = challengeGameOutcome.getRound(challengeGameOutcome.outcomeIndex).entries[0].winID;
						long totalCoins = 0;
						if (absoluteCoinRewards.TryGetValue(winId, out totalCoins))
						{
							// If coin reward is greater than 0, then add pending credits to avoid desync
							if (totalCoins > 0)
							{
								Server.handlePendingCreditsCreated(QFC_REWARD_PENDING_CREDIT_BONUS_SOURCE, totalCoins);
							}
						}
					}
				}
			}

			QFCMapDialog.showBonusGame(data);
			var handler = onMiniGameAwardedEvent;
			if (handler != null)
			{
				handler.Invoke(null);
			}
		}

		private void onRewardReceipt(JSON data)
		{
			if (data == null)
			{
				return;
			}

			string featureName = data.getString("feature_name", "");
			
			// If this reward bundle is for the loot box, Do not continue. The loot box will handle it in LootboxFeature.
			if(featureName.Equals(LootBoxFeature.LOOT_BOX, System.StringComparison.InvariantCultureIgnoreCase))
			{
				return;
			}
			
			JSON[] grantedEvents = data.getJsonArray("granted_events");

			if (grantedEvents != null)
			{
				int tokenAddedCount = 0;
				for (int i = 0; i < grantedEvents.Length; i++)
				{
					featureName = grantedEvents[i].getString("feature_name", "");
					switch (featureName)
					{
						case "qfc":
							Server.removePendingCredits(featureName);
							break;
					}

					switch (grantedEvents[i].getString("type", ""))
					{
						case "token_reward":
							tokenAddedCount += grantedEvents[i].getInt("added_value", 0);
							break;
						
						case "reward_granted":
							onRewardGranted(grantedEvents[i]);
							break;
					}
				}

				if (tokenAddedCount > 0)
				{
					QFCMapDialog.acknowledgeKeyReward(tokenAddedCount);
				}
			}
		}

		private void onRewardGranted(JSON grantedEvent)
		{
			JSON grantData = grantedEvent.getJSON("grant_data");
			// Only elite pass points need to be handled this way due to server limitation.
			// Card packs go directly to the RewardablesManager
			if (grantData.getString("reward_type", "") == "elite_pass_points") 
			{
				RewardablesManager.onRewardGranted(grantedEvent);
			}
		}
	}
}

