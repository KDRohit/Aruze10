using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Zynga.Core.Util;
using Object = UnityEngine.Object;
using Com.Scheduler;

/**
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
**/
namespace QuestForTheChest
{
	public class QFCMapDialog : DialogBase, IResetGame
	{
		const float DELAY_BEFORE_FIRST_POSITION_UPDATE = 0.8f;
		
		public enum DisplayMode
		{
			MAP,
			BONUS_GAME,
			START_INFO,
			REWARD,
			KEY_PROGRESS,
			KEYS_AWARD,
			KEY_CHEST_PRESENTATION,
			WAIT,
			CHEST,
			CHEST_LOST,
			MVP_WINNER,
			MVP_NON_WINNER
		}

		public DisplayMode currentMode { get; private set; }

		[SerializeField] private UISprite logoSprite;
		[SerializeField] private TextMeshPro timeRemainingLabel;
		[SerializeField] private ButtonHandler infoButton;
		[SerializeField] private SerializableDictionaryOfQFCNodeTypeToGameObject nodeTypeToNodePrefabs;
		[SerializeField] private SerializableDictionaryOfQFCPlayerIconTypeToGameObject playerIconTypeToIconPrefabs;
		[SerializeField] private QFCTeamProgressDisplay meterDisplay;
		[SerializeField] private GameObject portraitPrefab;
		[SerializeField] private UICenteredGrid homeTeamPortraitGrid;
		[SerializeField] private UICenteredGrid awayTeamPortraitGrid;
		[SerializeField] private Transform unusedPortraitParent;
		[SerializeField] private Transform chestParent;
		[SerializeField] private Transform overlayParent;
		[SerializeField] private QFCKeyAwardOverlay keyAwardOverlay;
		[SerializeField] private QFCMVPRewardsOverlay mvpAwardOverlay;
		[SerializeField] private QFCWinOverlay winChestOverlay;
		[SerializeField] private Animator notificationAnimator;
		[SerializeField] private TextMeshPro notificationBarLabel;
		[SerializeField] private float keyAwardDuration;

		[SerializeField] private GameObject nodesParent;
		[SerializeField] private Renderer background;
		[SerializeField] private Animator shroudAnimator;
		[SerializeField] private GameObject overlayShroud; //black shroud between map and overlay
		[SerializeField] private GameObject chestPrefab;

		[SerializeField] private GameObject rewardPrefab;
		[SerializeField] private GameObject keyPrefab;
		[SerializeField] private GameObject[] toggleVisibleObjects;
		
		private List<QFCPlayer> homeTeam;
		private List<QFCPlayer> awayTeam;
		private List<QFCBoardNodeObject> spawnedNodes;
		public Dictionary<string, QFCPlayerPortrait> zidsToPortraitsDict { get; private set; }
		private List<QFCPlayerPortrait> selectedOccupantPortraits;
		private List<ClickHandler> portraitButtons;
		private Queue<QFCPlayerPortrait> portraitPool;
		private UIAtlas themedAtlas;
		private bool? notificationVisible;

		private QuestForTheChestFeature qfcFeature;
		private int currentPlayerPosition = 0;

		private bool needReset = false;
		private bool skipAnimation = false;
		private float? callbackTimer;

		private QFCMiniGameOverlay currentMiniGame = null;
		private CommonAnimatedChest animatedChest = null;
		private static Queue<Dict> queuedEvents = new Queue<Dict>();

		private const string THEMED_BACKGROUND_PATH = "Features/Quest for the Chest/Themed Assets/{0}/Textures/Map Background {0}";
		private const string THEMED_ATLAS_PATH = "Features/Quest for the Chest/Themed Assets/{0}/Theme Atlas/Quest for the Chest Theme Atlas {0}";
		private const string WHEEL_GAME_PATH = "Features/Quest for the Chest/Prefabs/Bonus Games/Quest for the Chest Bonus Wheel Dialog";

		private const string SHROUD_INTRO_ANIMATION = "Intro";
		private const string SHROUD_OUTRO_ANIMATION = "Outro";
		private const string SHROUD_OFF_ANIMATION = "Off";
		private const string SHROUD_ON_ANIMATION = "On";
		
		private string bgMusic;
		private string playerIconInSound;
		private string playerIconOutSound;
		private string newKeySound = "QfcNewKey";
		private string playerIconMoveSound;
		private string playerCheckmarkSound;
		private string finalKeyIntroSound;
		private string finalKeyLockDisappear1Sound;
		private string finalKeyLockDisappear2Sound;
		private string finalKeyAwardSound;

		private const string NOTIF_BAR_LOCALIZATION_PREFIX = "qfc_map_message_";

		//static instance to determine if dialog is open or not (to show reward dialogs).
		private static QFCMapDialog _instance = null;
		public static bool showIntro = false;
		private static bool hasViewed = false;

		private static bool skipPendingNodeJumps = false;
		private bool hasPendingJumps = false;
		public static int pendingJumpsCount = 0;

		private int displayCount = 0; //Used for userflow tracking of how many states this dialog goes through
		private int progressRaceIndex = -1;

		public enum QFCBoardNodeType
		{
			START,
			MILESTONE,
			PROGRESS,
			MINIGAME
		}
		
		public enum QFCBoardPlayerIconType
		{
			CURRENT_PLAYER,
			HOME,
			AWAY,
		}
		
		public override void init()
		{
			StatsManager.Instance.LogCount("dialog", "ptr_v2", "dialog_board", "", "", "view");
			_instance = this;
			hasViewed = true;
			qfcFeature = QuestForTheChestFeature.instance;
			//Need to also setup themed elements (logo, background)
			initTeams();
			initBoard(qfcFeature.nodeData);
			initLabels();
			initMeters();
				
			if (!string.IsNullOrEmpty(ExperimentWrapper.QuestForTheChest.videoUrl))
			{
				infoButton.registerEventDelegate(infoButtonClicked);
			}
			else
			{
				infoButton.gameObject.SetActive(false); //Hide this button if no video is actually set
			}

			showIdleChest(false, queuedEvents.Count > 0);
			overlayShroud.SetActive(false);

			if (queuedEvents.Count > 0)
			{
				if(Scheduler.hasTaskWith("quest_for_the_chest_map"))
				{
					Scheduler.removeDialog("quest_for_the_chest_map");
				}
				dialogArgs = queuedEvents.Dequeue();
				checkDisplayMode();
			}
			else
			{
				currentMode = DisplayMode.MAP;
			}

			initSounds();

			AssetBundleManager.load(this, string.Format(THEMED_BACKGROUND_PATH, ExperimentWrapper.QuestForTheChest.theme), backgroundLoadSuccess, assetLoadFailed, isSkippingMapping:true, fileExtension:".png");
			AssetBundleManager.load(this, string.Format(THEMED_ATLAS_PATH, ExperimentWrapper.QuestForTheChest.theme), atlasLoadSuccess, assetLoadFailed, isSkippingMapping:true, fileExtension:".prefab");
			Audio.switchMusicKeyImmediate(bgMusic);

			//pause royal rush if it's active
			if (ExperimentWrapper.RoyalRush.isPausingInQFC && SlotBaseGame.instance != null && SlotBaseGame.instance.isRoyalRushGame && SlotBaseGame.instance.tokenBar != null)
			{
				RoyalRushCollectionModule rrMeter = SlotBaseGame.instance.tokenBar as RoyalRushCollectionModule;
				if (rrMeter != null && rrMeter.currentRushInfo.currentState == RoyalRushInfo.STATE.PAUSED)
				{
					rrMeter.pauseTimers();
				}
			}

			int startingHomeTeamKeyTotal = qfcFeature.getTeamKeyTotal(QFCTeams.HOME);
			int startingAwayTeamKeyTotal = qfcFeature.getTeamKeyTotal(QFCTeams.AWAY);
			
			Userflows.addExtraFieldToFlow(userflowKey, "home_team_start", startingHomeTeamKeyTotal.ToString());
			Userflows.addExtraFieldToFlow(userflowKey, "away_team_start", startingAwayTeamKeyTotal.ToString());

		}

		private void updateNotification(string text)
		{
			notificationBarLabel.text = text;
		}

		private void initSounds()
		{
			string soundSuffix = ExperimentWrapper.QuestForTheChest.theme;
			bgMusic = "QfcMusic" + soundSuffix;
			playerIconInSound = "QfcPlayerIconIn" + soundSuffix;
			playerIconOutSound = "QfcPlayerIconOut" + soundSuffix;
			playerIconMoveSound = "QfcPlayerIconMove" + soundSuffix;
			playerCheckmarkSound = "QfcPlayerCheckmark" + soundSuffix;
			finalKeyIntroSound = "QfcFinalKeyIntro" + soundSuffix;
			finalKeyLockDisappear1Sound = "QfcFinalKeyLockDisappear1" + soundSuffix;
			finalKeyLockDisappear2Sound = "QfcFinalKeyLockDisappear2" + soundSuffix;
			finalKeyAwardSound = "QfcFinalKeyAward" + soundSuffix;
		}

		private void updateNotification()
		{
			if (!notificationAnimator.gameObject.activeSelf)
			{
				notificationAnimator.gameObject.SetActive(true);
			}
			if (!notificationBarLabel.text.IsNullOrWhiteSpace() &&
			    (!notificationVisible.HasValue || !notificationVisible.Value))
			{
				notificationAnimator.Play("Intro");
				notificationVisible = true;
			}
			else if (!notificationVisible.HasValue && notificationBarLabel.text.IsNullOrWhiteSpace())
			{
				notificationVisible = false;
				notificationAnimator.Play("Off");
			}
			else if (notificationVisible.HasValue && notificationVisible.Value && notificationBarLabel.text.IsNullOrWhiteSpace())
			{
				notificationVisible = false;
				notificationAnimator.Play("Outro");
			}
		}

		private void checkDisplayMode()
		{
			currentMode = (DisplayMode)dialogArgs.getWithDefault(D.MODE, DisplayMode.MAP);
			if (needReset)
			{
				resetMapObjects(false);
				needReset = false;
			}
			if (notificationVisible.HasValue)
			{
				updateNotification(Localize.textOr(NOTIF_BAR_LOCALIZATION_PREFIX + currentMode.ToString().ToLower(),string.Empty));
			}
			if (currentMode == DisplayMode.BONUS_GAME)   // update CTA for replay message, but only display after player moves to start node
			{
				updateNotification(Localize.textOr(NOTIF_BAR_LOCALIZATION_PREFIX + currentMode.ToString().ToLower(),string.Empty));
			}
			
			switch (currentMode)
			{
				case DisplayMode.MAP:
					//do nothing
					break;

				case DisplayMode.BONUS_GAME:
					loadBonusGame();
					break;

				case DisplayMode.START_INFO:
					loadIntro();
					break;

				case DisplayMode.REWARD:
					loadReward();
					break;

				case DisplayMode.KEY_PROGRESS:
					loadKeys();
					break;

				case DisplayMode.KEYS_AWARD:
				case DisplayMode.MVP_WINNER:
				case DisplayMode.MVP_NON_WINNER:
					loadKeysAward(currentMode);
					break;

				case DisplayMode.KEY_CHEST_PRESENTATION:
					loadKeysChestPresentation();
					break;

				case DisplayMode.WAIT:
					waitForCallback();
					break;

				case DisplayMode.CHEST:
					loadChest();
					break;

				case DisplayMode.CHEST_LOST:
					loadChestLost();
					break;
			}
			
			Userflows.addExtraFieldToFlow(userflowKey, "display_" + displayCount, currentMode.ToString());
			displayCount++;
		}

		private void showIdleChest(bool forceShow, bool skipDropIn)
		{
			if (animatedChest == null)
			{
				animatedChest = NGUITools.AddChild(chestParent, chestPrefab).GetComponent<CommonAnimatedChest>();
			}
			animatedChest.playIdle(forceShow, skipDropIn);
		}

		private void showOpeningChest()
		{
			if (animatedChest == null)
			{
				animatedChest = NGUITools.AddChild(chestParent, chestPrefab).GetComponent<CommonAnimatedChest>();
			}
			Audio.play(finalKeyAwardSound);
			animatedChest.playOpenSequence();
		}

		private void initBoard(List<QFCBoardNode> nodeData)
		{
			if (!qfcFeature.isEnabled)
			{
				spawnedNodes = new List<QFCBoardNodeObject>(0);
				return;
			}

			HashSet<string> playerZids = new HashSet<string>();
			spawnedNodes = new List<QFCBoardNodeObject>(qfcFeature.nodeData.Count);
			for (int i = 0; i < nodeData.Count; i++)
			{
				GameObject instancedNode = null;
				if (i == 0)
				{
					//Start Node
					instancedNode = NGUITools.AddChild(nodesParent, nodeTypeToNodePrefabs[QFCBoardNodeType.START]);
				}
				else if (i == nodeData.Count - 1)
				{
					//Mini-game node
					instancedNode = NGUITools.AddChild(nodesParent, nodeTypeToNodePrefabs[QFCBoardNodeType.MINIGAME]);
				}
				else if (!string.IsNullOrEmpty(nodeData[i].storyLocalizationBody))
				{
					//Story/Milestone node
					instancedNode = NGUITools.AddChild(nodesParent, nodeTypeToNodePrefabs[QFCBoardNodeType.MILESTONE]);
				}
				else
				{
					//Just spawn a regular node
					instancedNode = NGUITools.AddChild(nodesParent, nodeTypeToNodePrefabs[QFCBoardNodeType.PROGRESS]);
				}

				if (instancedNode != null)
				{
					QFCBoardNodeObject newNode = instancedNode.GetComponent<QFCBoardNodeObject>();
					if (newNode != null)
					{
						newNode.transform.localPosition = nodeData[i].position;
						string homeTeamLeader = homeTeam != null && homeTeam.Count > 0 ? homeTeam[0].member.zId : "";
						string awayTeamLeader = awayTeam != null && awayTeam.Count > 0 ? awayTeam[0].member.zId : "";
						newNode.init(this, nodeData[i], homeTeamLeader, awayTeamLeader, playerIconTypeToIconPrefabs.dictionary, currentPlayerPosition >= i, playerZids);
						spawnedNodes.Add(newNode);
					}
				}
			}
		}

		//Setup various board labels
		private void initLabels()
		{
			if (qfcFeature.featureTimer != null)
			{
				qfcFeature.featureTimer.registerLabel(timeRemainingLabel);
			}
		}

		private void initMeters()
		{
			meterDisplay.init();
		}

		//Sets up the teams on the side of the board 
		private void initTeams()
		{
			//turn off portraits if
			if (!qfcFeature.isEnabled)
			{
				//instantiate items so we don't have invalid objects
				zidsToPortraitsDict = new Dictionary<string, QFCPlayerPortrait>();
				selectedOccupantPortraits = new List<QFCPlayerPortrait>();
				portraitButtons = new List<ClickHandler>();
				return;
			}

			homeTeam = qfcFeature.getTeamAsSortedList(QFCTeams.HOME);
			awayTeam = qfcFeature.getTeamAsSortedList(QFCTeams.AWAY);
			if (zidsToPortraitsDict == null)
			{
				zidsToPortraitsDict = new Dictionary<string, QFCPlayerPortrait>();
			}
			else
			{
				zidsToPortraitsDict.Clear();
			}

			if (selectedOccupantPortraits == null)
			{
				selectedOccupantPortraits = new List<QFCPlayerPortrait>();
			}
			else
			{
				selectedOccupantPortraits.Clear();
			}

			if (portraitButtons == null)
			{
				portraitButtons = new List<ClickHandler>();
			}
			else
			{
				portraitButtons.Clear();
			}

			for(int i = 0; i < homeTeam.Count; i++)
			{
				QFCPlayerPortrait homePlayerPortrait = null;
				if (!portraitPool.IsEmpty())
				{
					homePlayerPortrait = portraitPool.Dequeue();
					homePlayerPortrait.reset();
					GameObject newPortrait = homePlayerPortrait.gameObject;
					newPortrait.SetActive(true);
					newPortrait.transform.parent = homeTeamPortraitGrid.transform;
				}
				else
				{
					GameObject newPortrait = NGUITools.AddChild(homeTeamPortraitGrid.gameObject, portraitPrefab);
					homePlayerPortrait = newPortrait.GetComponent<QFCPlayerPortrait>();
				}

				QFCPlayer homePlayer = homeTeam[i];
				if (homePlayer.member.zId == SlotsPlayer.instance.socialMember.zId)
				{
					// Check if player have seen the current qfc.
					// Get the position that was last displayed, otherwise show the actual location from server
					if (!skipPendingNodeJumps)
					{
						if (pendingJumpsCount > 0)
						{
							hasPendingJumps = true;
							currentPlayerPosition = homePlayer.position;
						}
						else if (CustomPlayerData.getInt(CustomPlayerData.LAST_SEEN_QFC_COMPETITION_ID, 0) ==
						         qfcFeature.competitionId)
						{
							hasPendingJumps = true;
							currentPlayerPosition =
								CustomPlayerData.getInt(CustomPlayerData
									.QFC_PLAYER_LAST_SEEN_POSITION, -1);
							if (currentPlayerPosition < 0 || currentPlayerPosition == homePlayer.position)
							{
								hasPendingJumps = false;
							}
						}
						else
						{
							currentPlayerPosition = homePlayer.position;
						}
					}
					else
					{
						hasPendingJumps = false;
						skipPendingNodeJumps = false;
						pendingJumpsCount = 0;
						currentPlayerPosition = homePlayer.position;
					}
					
					homePlayerPortrait.init(QFCBoardPlayerIconType.CURRENT_PLAYER, homePlayer.member, homePlayer.keys, i == 0);
				}
				else
				{
					homePlayerPortrait.init(QFCBoardPlayerIconType.HOME, homePlayer.member, homePlayer.keys, i == 0);
				}
				zidsToPortraitsDict.Add(homePlayer.member.zId, homePlayerPortrait);
				portraitButtons.Add(homePlayerPortrait.profileInfo.profileHandler);
			}
			
			for(int i = 0; i < awayTeam.Count; i++)
			{
				QFCPlayerPortrait awayPlayerPortrait = null;
				if (!portraitPool.IsEmpty())
				{
					awayPlayerPortrait = portraitPool.Dequeue();
					awayPlayerPortrait.reset();
					GameObject newPortrait = awayPlayerPortrait.gameObject;
					newPortrait.SetActive(true);
					newPortrait.transform.parent = awayTeamPortraitGrid.transform;
				}
				else
				{
					GameObject newPortrait = NGUITools.AddChild(awayTeamPortraitGrid.gameObject, portraitPrefab);
					awayPlayerPortrait = newPortrait.GetComponent<QFCPlayerPortrait>();
				}

				QFCPlayer awayPlayer = awayTeam[i];
				awayPlayerPortrait.init(QFCBoardPlayerIconType.AWAY, awayPlayer.member, awayPlayer.keys, i == 0);
				zidsToPortraitsDict.Add(awayPlayer.member.zId, awayPlayerPortrait);
				portraitButtons.Add(awayPlayerPortrait.profileInfo.profileHandler);
			}
			
			awayTeamPortraitGrid.reposition();
			homeTeamPortraitGrid.reposition();
		}

		private void clearTeamPortraits()
		{
			int awayChildrenCount = awayTeamPortraitGrid.transform.childCount;
			int homeChildrenCount = homeTeamPortraitGrid.transform.childCount;
			int childrenCount = awayChildrenCount + homeChildrenCount;
			if (childrenCount > 0)
			{
				//remove objects
				if (portraitPool == null)
				{
					portraitPool = new Queue<QFCPlayerPortrait>(childrenCount);
				}
			}

			for (int i = awayChildrenCount - 1; i >= 0; i--)
			{
				GameObject obj = awayTeamPortraitGrid.transform.GetChild(i).gameObject;
				if (obj != null)
				{
					QFCPlayerPortrait portrait = obj.GetComponent<QFCPlayerPortrait>();
					obj.SetActive(false);
					obj.transform.parent = unusedPortraitParent;
					portraitPool.Enqueue(portrait);
				}
			}

			for (int i = homeChildrenCount - 1; i >= 0; i--)
			{
				GameObject obj = homeTeamPortraitGrid.transform.GetChild(i).gameObject;
				if (obj != null)
				{
					QFCPlayerPortrait portrait = obj.GetComponent<QFCPlayerPortrait>();
					obj.SetActive(false);
					obj.transform.parent = unusedPortraitParent;
					portraitPool.Enqueue(portrait);
				}
			}
		}

		private void clearMapNodes()
		{
			int nodeCount = spawnedNodes.Count;
			for(int i=nodeCount -1; i>= 0; --i)
			{
				int playerCount = spawnedNodes[i].playerContainer.childCount;
				for(int j=playerCount-1; j>=0; --j)
				{
					spawnedNodes[i].playerContainer.GetChild(j).gameObject.SetActive(false);
					Destroy(spawnedNodes[i].playerContainer.GetChild(j).gameObject);
				}
				spawnedNodes[i].gameObject.SetActive(false);
				Destroy(spawnedNodes[i].gameObject);
			}
			spawnedNodes.Clear();
		}

		private void resetMapObjects(bool showTeamMeters)
		{
			clearTeamPortraits();
			clearMapNodes();

			initTeams();
			initBoard(qfcFeature.nodeData);
			meterDisplay.reset(showTeamMeters);

			//bundle is cached so this won't take any time, and will set all the sprites correctly
			AssetBundleManager.load(this, string.Format(THEMED_ATLAS_PATH, ExperimentWrapper.QuestForTheChest.theme), atlasLoadSuccess, assetLoadFailed, isSkippingMapping:true, fileExtension:".prefab");
		}

		private void infoButtonClicked(Dict data = null)
		{
			//Video Dialog will hide the map/reshow it after being closed automatically
			VideoDialog.showDialog
			(
				ExperimentWrapper.QuestForTheChest.videoUrl,
				"", 
				"Play Now", 
				"quest_for_the_chest", 
				0, 
				0, 
				"", 
				ExperimentWrapper.QuestForTheChest.videoSummaryPath, 
				true, 
				"", 
				"",
				true
			);
		}

		// This is used to delay the dialog one frame to ensure all the events get queued up before it opens
		private static IEnumerator startDialog(bool topOfStack = false)
		{
			//wait one frame
			yield return null;

			showDialog(topOfStack);

		}

		/// Called by Dialog.close() - do not call directly.	
		public override void close()
		{
			int endHomeTeamKeyTotal = qfcFeature.getTeamKeyTotal(QFCTeams.HOME);
			int endAwayTeamKeyTotal = qfcFeature.getTeamKeyTotal(QFCTeams.AWAY);
			
			Userflows.addExtraFieldToFlow(userflowKey, "home_team_end", endHomeTeamKeyTotal.ToString());
			Userflows.addExtraFieldToFlow(userflowKey, "away_team_end", endAwayTeamKeyTotal.ToString());
			
			StopAllCoroutines();
			_instance = null;
			skipPendingNodeJumps = false;
			
			if (ExperimentWrapper.RoyalRush.isPausingInQFC && GameState.game != null)
			{
				string gameKey = GameState.game.keyName;
				RoyalRushInfo rushInfo = RoyalRushEvent.instance.getInfoByKey(gameKey);
				if (rushInfo != null && rushInfo.currentState == RoyalRushInfo.STATE.PAUSED)
				{
					RoyalRushAction.unPauseQFCEvent(gameKey);
				}
			}

			bool hadStuckCompleteEvent = false;
			string eventId = qfcFeature.getCompletedRaceId();
			while (!string.IsNullOrEmpty(eventId))
			{
				qfcFeature.consumeRaceComplete(eventId);
				eventId = qfcFeature.getCompletedRaceId();
				hadStuckCompleteEvent = true;
			}

			if (hadStuckCompleteEvent)
			{
				Userflows.addExtraFieldToFlow(userflowKey, "stuck_complete", "true");
			}

			//Do info request if we gained progress for an event we don't think we're in
			if (progressRaceIndex > -1 && progressRaceIndex != qfcFeature.raceIndex)
			{
				QFCAction.getCurrentRaceInformation(qfcFeature.competitionId, progressRaceIndex);
			}
		}

		public override void onCloseButtonClicked(Dict args = null)
		{
			StatsManager.Instance.LogCount("dialog", "ptr_v2", "dialog_board", "", "close", "click");
			base.onCloseButtonClicked(args);
		}

		public static bool hasBeenViewedThisSession()
		{
			return hasViewed;
		}

		public static bool isInstantiated()
		{
			return _instance != null;
		}

		private static void queueEvent(Dict args)
		{
			if (args == null)
			{
				Bugsnag.LeaveBreadcrumb("Invalid display event queued to qfc map");
				return;
			}

			queuedEvents.Enqueue(args);

			if (_instance != null && _instance.currentMode == DisplayMode.MAP)
			{
				_instance.newEventQueued();
			}
		}

		private static void insertEvent(Dict args)
		{
			if (args == null)
			{
				Bugsnag.LeaveBreadcrumb("Invalid display event inserted to qfc map");
				return;
			}

			if (queuedEvents.IsEmpty())
			{
				queueEvent(args);
			}
			else
			{
				//create new queue with start event
				Queue<Dict> newQueue = new Queue<Dict>();
				newQueue.Enqueue(args);

				//append existing events
				while (!queuedEvents.IsEmpty())
				{
					newQueue.Enqueue(queuedEvents.Dequeue());
				}

				//replace queue
				queuedEvents = newQueue;
			}
		}

		public static void showDialog(bool topOfStack = false, string motdKey = "")
		{
			if (showIntro)
			{
				showIntro = false;
				Dict args = Dict.create(D.MODE, DisplayMode.START_INFO);
				insertEvent(args);
			}
			Scheduler.addDialog("quest_for_the_chest_map", Dict.create(D.IS_TOP_OF_LIST, topOfStack, D.MODE, DisplayMode.MAP, D.MOTD_KEY, motdKey));
		}

		public static void showReward(string eventId, int node, List<QFCReward> rewardList, AnswerDelegate rewardCollectCallback, bool topOfStack = false)
		{
			if (!isStarted)
			{
				RoutineRunner.instance.StartCoroutine(startDialog(topOfStack));
			}
			skipPendingNodeJumps = true;
			Dict args = Dict.create(D.EVENT_ID, eventId, D.MODE, DisplayMode.REWARD, D.VALUES, rewardList, D.CLOSE, rewardCollectCallback, D.KEY, node);
			queueEvent(args);
		}

		public static void showRaceLost(string eventId, int numKeys, string winnerZid, Dictionary<string, int> keyData, int requiredKeys, AnswerDelegate onCompleteCallback, bool topOfStack = false)
		{
			if (!isStarted)
			{
				RoutineRunner.instance.StartCoroutine(startDialog(topOfStack));
			}
			Dict args1 = Dict.create(D.MODE, DisplayMode.KEY_CHEST_PRESENTATION, D.VALUE, numKeys, D.DATA, false, D.PLAYER, winnerZid, D.GAME_KEY, keyData, D.KEYS_NEED, requiredKeys);
			queueEvent(args1);

			Dict args2 = Dict.create(D.EVENT_ID, eventId, D.MODE, DisplayMode.CHEST_LOST, D.CLOSE, onCompleteCallback);
			queueEvent(args2);
		}

		public static void showHomeTeamChestWinner(int numKeys, string winnerZid, Dictionary<string, int> keyData, int requiredKeys, AnswerDelegate callback, string eventId, bool topOfStack = false)
		{
			if (!isStarted)
			{
				RoutineRunner.instance.StartCoroutine(startDialog(topOfStack));
			}
			Dict args = Dict.create(
				D.MODE, DisplayMode.KEY_CHEST_PRESENTATION,
				D.VALUE, numKeys,
				D.DATA, true,
				D.PLAYER, winnerZid,
				D.CLOSE, callback,
				D.EVENT_ID, eventId,
				D.GAME_KEY, keyData,
				D.KEYS_NEED, requiredKeys);
			queueEvent(args);
		}

		public static void showWinChest(string eventId, long teamWin, long userWin, int xpLevel, int inflationFactor, long normalizedWin, AnswerDelegate rewardCollectCallback, bool topOfStack = false)
		{
			if (!isStarted)
			{
				RoutineRunner.instance.StartCoroutine(startDialog(topOfStack));
			}
			Dict args = Dict.create(
				D.EVENT_ID, eventId,
				D.MODE, DisplayMode.CHEST,
				D.OPTION1, teamWin,
				D.OPTION2, userWin,
				D.RANK, xpLevel,
				D.SCORE, inflationFactor,
				D.PAYOUT_CREDITS, normalizedWin,
				D.CLOSE, rewardCollectCallback);
			queueEvent(args);
		}

		public static void showKeys(string eventId, int numKeys, int newNodeIndex, AnswerDelegate keyCollectCallback, int raceIndex, bool topOfStack = false, float waitTime = 0.0f)
		{
			if (!isStarted)
			{
				RoutineRunner.instance.StartCoroutine(startDialog(topOfStack));
			}

			skipPendingNodeJumps = true;
			Dict args = Dict.create(D.EVENT_ID, eventId, D.MODE, DisplayMode.KEY_PROGRESS, D.VALUE, numKeys, D.CLOSE, keyCollectCallback, D.SCORE, newNodeIndex, D.TIME, waitTime, D.INDEX, raceIndex);
			queueEvent(args);
		}

		public static void acknowledgeKeyReward(int numKeys)
		{
			if (_instance != null && _instance.isActiveAndEnabled)
			{
				_instance.StartCoroutine(_instance.playKeyAddAnimation(numKeys));
			}
			else
			{
				// just call award keys as the ui is already destroyed
				QuestForTheChestFeature.instance.awardKeys(SlotsPlayer.instance.socialMember.zId, numKeys);
			}
		}

		public static void showNewRace()
		{
			if (hasViewed)
			{
				skipPendingNodeJumps = true;
			}
			if (_instance != null)
			{
				if (_instance.currentMode != DisplayMode.MAP)
				{
					_instance.needReset = true;
				}
				else
				{
					_instance.resetMapObjects(true);
				}
			}

		}

		public static void showBonusGame(JSON bonusGameOutcome)
		{
			if (!isStarted)
			{
				//map dialog is not
				RoutineRunner.instance.StartCoroutine(startDialog());
			}
			skipPendingNodeJumps = true;
			Dict args = Dict.create(D.BONUS_GAME, bonusGameOutcome, D.MODE, DisplayMode.BONUS_GAME);
			queueEvent(args);
		}

		public static void showKeysAward(string eventId, long amount, AnswerDelegate rewardCollectCallback, bool topOfStack = false)
		{
			if (!isStarted)
			{
				RoutineRunner.instance.StartCoroutine(startDialog(topOfStack));
			}
			skipPendingNodeJumps = true;
			Dict args = Dict.create(D.EVENT_ID, eventId, D.MODE, DisplayMode.KEYS_AWARD, D.AMOUNT, amount, D.CLOSE, rewardCollectCallback);
			queueEvent(args);
		}
		
		public static void showMVPWinAward(string eventId, long amount, QFCPlayer player, List<QFCReward> mvpReward, AnswerDelegate rewardCollectCallback, bool topOfStack = false)
		{
			if (!isStarted)
			{
				RoutineRunner.instance.StartCoroutine(startDialog(topOfStack));
			}
			skipPendingNodeJumps = true;
			Dict args = Dict.create(D.EVENT_ID, eventId, D.MODE, DisplayMode.MVP_WINNER, D.AMOUNT, amount, D.PLAYER, player, D.OPTION, mvpReward, D.CLOSE, rewardCollectCallback);
			queueEvent(args);
		}
	
		public static void showNonMVPAward(string eventId, long amount, QFCPlayer teamMvp, List<QFCReward> teamMvpRewards, QFCPlayer opponentMvp, List<QFCReward> opponentMvpRewards, AnswerDelegate rewardCollectCallback, bool topOfStack = false)
		{
			if (!isStarted)
			{
				RoutineRunner.instance.StartCoroutine(startDialog(topOfStack));
			}
			skipPendingNodeJumps = true;
			Dict args = Dict.create(D.EVENT_ID, eventId, D.MODE, DisplayMode.MVP_NON_WINNER, D.AMOUNT, amount, D.PLAYER, teamMvp, D.PLAYER1, opponentMvp, D.OPTION, teamMvpRewards, D.OPTION1, opponentMvpRewards, D.CLOSE, rewardCollectCallback);
			queueEvent(args);
		}

		
		private void backgroundLoadSuccess(string assetPath, Object obj, Dict data = null)
		{
			background.material.mainTexture = obj as Texture2D;
			background.gameObject.SetActive(true);
		}

		private void atlasLoadSuccess(string assetPath, Object obj, Dict data = null)
		{
			themedAtlas = ((GameObject)obj).GetComponent<UIAtlas>();
			logoSprite.atlas = themedAtlas;
			logoSprite.spriteName = "Logo";

			for (int i = 0; i < spawnedNodes.Count; i++)
			{
				spawnedNodes[i].setSprite(themedAtlas);
			}
		}

		private void assetLoadFailed(string assetPath, Dict data = null)
		{
			if (this != null && closeButtonHandler != null && closeButtonHandler.gameObject != null)
			{
				closeButtonHandler.gameObject.SetActive(true); //Turn this on incase we failed trying to load an overlay
			}

			Bugsnag.LeaveBreadcrumb("QFC Themed Asset failed to load: " + assetPath);
#if UNITY_EDITOR
			Debug.LogWarning("QFC Themed Asset failed to load: " + assetPath);			
#endif
		}

		private void toggleOverlayShroud(bool enabled, bool immediate = false, bool shouldToggleButtons = true)
		{
			if (immediate)
			{
				shroudAnimator.Play(enabled ? SHROUD_ON_ANIMATION : SHROUD_OFF_ANIMATION);
			}
			//Only play the on/off animation if the shroud was previously in the opposite state
			else if (enabled && shroudAnimator.GetCurrentAnimatorStateInfo(0).IsName(SHROUD_OFF_ANIMATION) ||
					!enabled && (shroudAnimator.GetCurrentAnimatorStateInfo(0).IsName(SHROUD_INTRO_ANIMATION) || 
								shroudAnimator.GetCurrentAnimatorStateInfo(0).IsName(SHROUD_ON_ANIMATION)))
			{
				shroudAnimator.Play(enabled ? SHROUD_INTRO_ANIMATION : SHROUD_OUTRO_ANIMATION);
			}

			if (enabled)
			{
				for (int i = 0; i < spawnedNodes.Count; ++i)
				{
					spawnedNodes[i].flatten();
				}
			}
			else
			{
				//wait so the coins don't clip into the shroud hide animation
				StartCoroutine(unFlattenNodes(1.0f));
			}

			if (shouldToggleButtons)
			{
				toggleButtons(!enabled);
			}
		}

		protected override void Start()
		{
			base.Start();
			if (hasPendingJumps || pendingJumpsCount > 0)
			{
				StartCoroutine(forceRepositionPlayerOnLoad());
			}
		}

		private IEnumerator unFlattenNodes(float delay)
		{
			yield return new WaitForSeconds(delay);
			for (int i = 0; i < spawnedNodes.Count; ++i)
			{
				spawnedNodes[i].unFlatten();
			};
		}

		private void toggleObjects(bool enabled)
		{
			if (toggleVisibleObjects != null)
			{
				for (int i = 0; i < toggleVisibleObjects.Length; i++)
				{
					toggleVisibleObjects[i].SetActive(enabled);
				}
			}

			//if we disable the object before the coroutine runs it puts the chest in a bad state
			//ensure chest is idle
			if (enabled)
			{
				showIdleChest(false, true);
							
				for (int i = 0; i <= currentPlayerPosition; i++)
				{
					if (i < spawnedNodes.Count && spawnedNodes[i] != null)
					{
						spawnedNodes[i].playIdleCompletedAnimation();
					}
				}

				foreach (KeyValuePair<string, QFCPlayerPortrait> kvp in zidsToPortraitsDict)
				{
					kvp.Value.playIdleBubbleAnimation();
				}
			}
		}

		private void toggleButtons(bool enabled)
		{
			closeButtonHandler.isEnabled = enabled;
			closeButtonHandler.gameObject.SetActive(enabled);
			infoButton.isEnabled = enabled;
			for (int i = 0; i < portraitButtons.Count; i++)
			{
				portraitButtons[i].isEnabled = enabled;
			}
		}

		private void waitForCallback()
		{
			float waitTime = (float)dialogArgs.getWithDefault(D.TIME, 1.0f);
			bool showShroud = (bool) dialogArgs.getWithDefault(D.ACTIVE, true);
			toggleOverlayShroud(showShroud);
			StartCoroutine(waitForTime(waitTime));
		}

		private IEnumerator waitForTime(float fTime)
		{
			yield return new WaitForSeconds(fTime);
			rewardCallback(null);
		}

		private IEnumerator delayToggleDialog(float fTime, bool enabled)
		{
			yield return null;
			NGUIExt.enableAllMouseInput();
			
			yield return new WaitForSeconds(fTime);
			if (!skipAnimation)
			{
				toggleDialog(enabled);
			}
			else
			{
				skipAnimation = false;
			}
		}

		private void toggleDialog(bool enabled)
		{
			toggleObjects(enabled);
			toggleOverlayShroud(enabled, true);
			showIdleChest(true, true); //force because deactivating animation breaks the idle state
			animateIn();
		}

		private void loadKeys()
		{
			progressRaceIndex = (int) dialogArgs.getWithDefault(D.INDEX, qfcFeature.raceIndex);
			string eventId = (string)dialogArgs.getWithDefault(D.EVENT_ID, "-1");
			float waitTime = (float) dialogArgs.getWithDefault(D.TIME, 0.0f);
			GameObject obj = NGUITools.AddChild(overlayParent, keyPrefab);
			if (obj != null)
			{
				toggleObjects(false);
				StartCoroutine(delayToggleDialog(waitTime, true));
				QFCKeyOverlay overlay = obj.GetComponent<QFCKeyOverlay>();
				if (overlay != null)
				{
					onFadeInComplete();
					int numKeys = (int)dialogArgs.getWithDefault(D.VALUE, 0);
					overlay.init(eventId, numKeys, startAdvanceAnimations, fastForwardPresentation);
					Audio.play(newKeySound + ExperimentWrapper.QuestForTheChest.theme);
				}
				else
				{
					Destroy(obj);
					AnswerDelegate answerCallback = (AnswerDelegate)dialogArgs.getWithDefault(D.CLOSE, null);
					answerCallback.Invoke(Dict.create(D.EVENT_ID, eventId, D.REASON, "Missing QFC reward dialog component on prefab"));
				}
			}
			else
			{
				AnswerDelegate answerCallback = (AnswerDelegate)dialogArgs.getWithDefault(D.CLOSE, null);
				answerCallback.Invoke(Dict.create(D.EVENT_ID, eventId, D.REASON, "Can't instantiate prefab"));
			}
		}

		private void loadChest()
		{
			toggleOverlayShroud(true); //shroud covers up chset
			string eventId = (string) dialogArgs.getWithDefault(D.EVENT_ID, "");
			long totalWin = (long) dialogArgs.getWithDefault(D.OPTION1, 0);
			long userWin = (long) dialogArgs.getWithDefault(D.OPTION2, 0);
			int xpLevel = (int) dialogArgs.getWithDefault(D.RANK, 0);
			int inflationFactor = (int) dialogArgs.getWithDefault(D.SCORE, 0);
			long normalizedWin = (long) dialogArgs.getWithDefault(D.PAYOUT_CREDITS, 0);
			StartCoroutine(chestWinPresentation(eventId, totalWin, userWin, xpLevel, inflationFactor, normalizedWin));
		}

		private void loadChestLost()
		{
			toggleOverlayShroud(true); //shroud covers up chset
			string eventId = (string) dialogArgs.getWithDefault(D.EVENT_ID, "");
			StartCoroutine(chestLosePresentation(eventId));
		}

		private IEnumerator chestLosePresentation(string eventId)
		{
			Audio.play(finalKeyLockDisappear1Sound);
			showIdleChest(false, true);
			meterDisplay.Unlock();
			updateNotification("");
			yield return new WaitForSeconds(1.25f);
			winChestOverlay.initLose(eventId, animatedChest, rewardCallback);
		}

		private IEnumerator chestWinPresentation(string eventId, long totalWin, long individualWin, int xpLevel, int inflationFactor, long normalizedReward)
		{
			Audio.play(finalKeyLockDisappear1Sound);
			showIdleChest(false, true);
			meterDisplay.Unlock();
			updateNotification("");
			yield return new WaitForSeconds(1.25f);
			showOpeningChest();
			yield return new WaitForSeconds(1.0f);
			winChestOverlay.init(eventId, totalWin, individualWin, xpLevel, inflationFactor, normalizedReward, animatedChest, rewardCallback);
		}

		private void loadKeysAward(DisplayMode displayMode)
		{
			toggleOverlayShroud(true);
			StartCoroutine(keyAwardPresentation(displayMode));
		}

		private IEnumerator keyAwardPresentation(DisplayMode displayMode)
		{
			//make sure we're in the idle state
			yield return StartCoroutine(meterDisplay.playIdle());

			//set the redeeming keys text
			meterDisplay.showMessage(Localize.text("Redeeming Keys..."));

			//consume race complete event if it exists
			string raceId = qfcFeature.getCompletedRaceId();
			int currentRaceIndex = qfcFeature.raceIndex;
			if (!string.IsNullOrEmpty(raceId))
			{
				qfcFeature.consumeRaceComplete(raceId);
			}

			//drop display
			yield return StartCoroutine(meterDisplay.dropKeysToZero(keyAwardDuration));

			//hide everything
			meterDisplay.clearMessage();
			Audio.play(finalKeyLockDisappear2Sound);
			yield return StartCoroutine(meterDisplay.playOutro());

			//yield until complete race finishes if we're waiting on a new race
			if (qfcFeature.isEnabled && !string.IsNullOrEmpty(raceId))
			{
				yield return StartCoroutine(verifyRaceConsumedEnumerator(currentRaceIndex));
			}

			long amount = (long) dialogArgs.getWithDefault(D.AMOUNT, 0);
			string eventId = (string) dialogArgs.getWithDefault(D.EVENT_ID, "");
			animatedChest.playChestOff();
			switch (displayMode)
			{
				case DisplayMode.KEYS_AWARD:
					// this flow is for single person qfc team
					keyAwardOverlay.init(eventId, amount, rewardCallback);
					break;
				case DisplayMode.MVP_WINNER:
					List<QFCReward> mvpRewards = dialogArgs.getWithDefault(D.OPTION, null) as List<QFCReward>;
					QFCPlayer player = (QFCPlayer) dialogArgs.getWithDefault(D.PLAYER, null);
					if (mvpRewards != null && player != null)
					{
						mvpAwardOverlay.initMVPView(eventId, player, amount, mvpRewards[0], rewardCallback, this);
					}
					break;
				case DisplayMode.MVP_NON_WINNER:
					List<QFCReward> teamMvpRewards = dialogArgs.getWithDefault(D.OPTION, null) as List<QFCReward>;
					List<QFCReward> opponentMvpRewards = dialogArgs.getWithDefault(D.OPTION1, null) as List<QFCReward>;
					QFCPlayer teamMVP = dialogArgs.getWithDefault(D.PLAYER, null) as QFCPlayer;
					QFCPlayer opponentMVP = dialogArgs.getWithDefault(D.PLAYER1, null) as QFCPlayer;
					mvpAwardOverlay.initNonMVPView(eventId, amount, teamMVP, opponentMVP, teamMvpRewards,
							opponentMvpRewards, rewardCallback, this);
					break;
			}
		}

		public static IEnumerator verifyRaceConsumed(int raceId)
		{
			if (_instance == null)
			{
				return null;
			}

			return _instance.verifyRaceConsumedEnumerator(raceId);
		}

		private IEnumerator verifyRaceConsumedEnumerator(int raceId)
		{
			if (callbackTimer.HasValue || !QuestForTheChestFeature.instance.isEnabled)
			{
				//only allow one instance and don't run this for cases where we don't expect a return
				yield break;
			}

			callbackTimer = 0f;
			while (qfcFeature.currentConsumeRaceIndex >=0 && callbackTimer.Value < QuestForTheChestFeature.CONSUME_RACE_TIMEOUT)
			{
				yield return null;
			}

			if (callbackTimer.Value >= QuestForTheChestFeature.CONSUME_RACE_TIMEOUT)
			{
				//zero out all keys
				qfcFeature.onConsumeRaceFailed();

				//set reset flag
				needReset = true;

				//send get info event to quietly update while user is spinning
				QFCAction.getCurrentRaceInformation(qfcFeature.competitionId, qfcFeature.raceIndex+1);

				//log an error to verify we didn't get a new race info
				Debug.LogError("Server never returned from consume race for race id: " + raceId + " , skipping ahead");
			}
			callbackTimer = null;
		}

		private void loadKeysChestPresentation()
		{
			//make sure keys are set to correct values
			Dictionary<string, int> keyData = (Dictionary<string, int>) dialogArgs.getWithDefault(D.GAME_KEY, null);
			if (keyData != null)
			{
				int requiredKeys = (int) dialogArgs.getWithDefault(D.KEYS_NEED, 0);
				qfcFeature.updateKeyData(requiredKeys, keyData);
				resetKeyTotals(false);
			}

			//make sure chest is displayed
			showIdleChest(false, true);

			//make sure meters are displayed
			meterDisplay.showTeamMeters();

			GameObject obj = NGUITools.AddChild(overlayParent, keyPrefab);
			if (obj != null)
			{
				QFCKeyOverlay overlay = obj.GetComponent<QFCKeyOverlay>();
				if (overlay != null)
				{
					toggleOverlayShroud(true);
					int numKeys = (int)dialogArgs.getWithDefault(D.VALUE, 0);
					bool homeTeamWin = (bool) dialogArgs.getWithDefault(D.DATA, true);
					string winnerZid = (string) dialogArgs.getWithDefault(D.PLAYER, "");
					if (!string.IsNullOrEmpty(winnerZid))
					{
						Dictionary<string, QFCPlayer> players = QuestForTheChestFeature.instance.getTeamMembersAsPlayerDict(homeTeamWin ? QFCTeams.HOME : QFCTeams.AWAY);
						if (players.ContainsKey(winnerZid))
						{
							meterDisplay.showMessage(Localize.text("qfc_{0}_found_winning_key", players[winnerZid].name));
						}
					}
					
					Audio.play(finalKeyIntroSound);
					string eventId = (string) dialogArgs.getWithDefault(D.EVENT_ID, "");
					overlay.initRaceComplete(homeTeamWin, numKeys, rewardCallback, eventId);
				}
				else
				{
					Destroy(obj);
					AnswerDelegate answerCallback = (AnswerDelegate)dialogArgs.getWithDefault(D.CLOSE, null);
					answerCallback.Invoke(Dict.create(D.REASON, "Missing QFC key dialog component on prefab"));
				}
			}
			else
			{
				AnswerDelegate answerCallback = (AnswerDelegate)dialogArgs.getWithDefault(D.CLOSE, null);
				answerCallback.Invoke(Dict.create(D.REASON, "Can't instantiate prefab"));
			}
		}

		private void loadIntro()
		{
			string eventId = (string)dialogArgs.getWithDefault(D.EVENT_ID, "-1");
			GameObject obj = NGUITools.AddChild(overlayParent, rewardPrefab);
			if (obj != null)
			{
				QFCRewardOverlay overlay = obj.GetComponent<QFCRewardOverlay>();
				if (overlay != null)
				{
					overlayShroud.SetActive(true);
					toggleOverlayShroud(true);
					List<QFCReward> itemList = (List<QFCReward>) dialogArgs.getWithDefault(D.VALUES, null);
					overlay.initIntro(rewardCallback);
				}
				else
				{
					Destroy(obj);
					AnswerDelegate answerCallback = (AnswerDelegate)dialogArgs.getWithDefault(D.CLOSE, null);
					answerCallback.Invoke(Dict.create(D.EVENT_ID, eventId, D.REASON, "Missing QFC reward dialog component on prefab"));
				}
			}
			else
			{
				AnswerDelegate answerCallback = (AnswerDelegate)dialogArgs.getWithDefault(D.CLOSE, null);
				answerCallback.Invoke(Dict.create(D.EVENT_ID, eventId, D.REASON, "Can't instantiate prefab"));
			}
		}

		private void loadReward()
		{
			string eventId = (string)dialogArgs.getWithDefault(D.EVENT_ID, "-1");
			GameObject obj = NGUITools.AddChild(overlayParent, rewardPrefab);
			if (obj != null)
			{
				QFCRewardOverlay overlay = obj.GetComponent<QFCRewardOverlay>();
				if (overlay != null)
				{
					overlayShroud.SetActive(true);
					toggleOverlayShroud(true);
					List<QFCReward> itemList = (List<QFCReward>) dialogArgs.getWithDefault(D.VALUES, null);
					overlay.init(eventId, currentPlayerPosition, itemList, rewardCallback);
				}
				else
				{
					Destroy(obj);
					AnswerDelegate answerCallback = (AnswerDelegate)dialogArgs.getWithDefault(D.CLOSE, null);
					answerCallback.Invoke(Dict.create(D.EVENT_ID, eventId, D.REASON, "Missing QFC reward dialog component on prefab"));
				}
			}
			else
			{
				AnswerDelegate answerCallback = (AnswerDelegate)dialogArgs.getWithDefault(D.CLOSE, null);
				answerCallback.Invoke(Dict.create(D.EVENT_ID, eventId, D.REASON, "Can't instantiate prefab"));
			}
		}

		private void Update()
		{
			if (callbackTimer.HasValue)
			{
				callbackTimer += Time.deltaTime;
			}
		}

		private void startAdvanceAnimations(Dict args)
		{
			overlayShroud.SetActive(false);
			toggleButtons(false);
			StartCoroutine(playRewardAnimations(args));
		}

		private void fastForwardPresentation(Dict args)
		{
			StatsManager.Instance.LogCount("dialog", "ptr_v2", "dialog_board", "", "skip", "click");

			//Don't show the map if we're going to auto-close the dialog
			bool closeInstantlyIfLastEvent = (bool) args.getWithDefault(D.OPTION, false);
			bool autoClosing = closeInstantlyIfLastEvent && queuedEvents.Count == 0;
			int keysToAdd = (int)dialogArgs.getWithDefault(D.VALUE, 0);
			int newKeysTotal = 0;
			
			if (keysToAdd > 0)
			{
				newKeysTotal = qfcFeature.awardKeys(SlotsPlayer.instance.socialMember.zId, keysToAdd);
			}
			
			if (!autoClosing)
			{
				//Enable the map so we can show the board for the next reward presentation
				toggleObjects(enabled);
				showIdleChest(true, true);
				show();

				if (keysToAdd > 0)
				{
					QFCPlayerPortrait currentPlayerPortrait = null;
					if (zidsToPortraitsDict.TryGetValue(SlotsPlayer.instance.socialMember.zId, out currentPlayerPortrait))
					{
						StartCoroutine(currentPlayerPortrait.updateKeysBubble(newKeysTotal));
					}

					updatePlayerPortraitLocation();
				}
				
				bool isRestarting = (bool) args.getWithDefault(D.NEW_LEVEL, false);
				StartCoroutine(updatePlayerBoardPosition(false, isRestarting));
			}
			else
			{
				//Just progress the player without moving any gameObject pieces around since the dialog will be closing
				int progress = (int)dialogArgs.getWithDefault(D.SCORE, 0);
				qfcFeature.advancePlayer(SlotsPlayer.instance.socialMember.zId, progress);
			}

			rewardCallback(args);
		}

		private void toggleStaticOverlayShroud(Dict args)
		{
			bool enabled = (bool)args.getWithDefault(D.OPTION, false);
			overlayShroud.SetActive(enabled);
		}
		
		private void addKeysToPlayer()
		{
			StartCoroutine(playKeyAddAnimation((int)dialogArgs.getWithDefault(D.VALUE, 0)));
		}

		private IEnumerator playKeyAddAnimation(int keysToAdd)
		{
			//Add the keys to the meter and player portrait
			int newKeysTotal = qfcFeature.awardKeys(SlotsPlayer.instance.socialMember.zId, keysToAdd);
			meterDisplay.updateKeyTotals();
			yield return new WaitForSeconds(0.5f);  //delay for meter to reach star burst stage in the animation
			//need try value in case this we get a reward event after the qfc event has ended
			QFCPlayerPortrait currentPlayerPortrait = null;
			if (zidsToPortraitsDict.TryGetValue(SlotsPlayer.instance.socialMember.zId, out currentPlayerPortrait))
			{
				yield return  StartCoroutine(currentPlayerPortrait.updateKeysBubble(newKeysTotal));
				currentPlayerPortrait.playPortraitUpdateAnimation();
			}
		}

		private IEnumerator playRewardAnimations(Dict args)
		{
			//Add the keys to the meter and player portrait
			int keysToAdd = (int)dialogArgs.getWithDefault(D.VALUE, 0);

			yield return StartCoroutine(playKeyAddAnimation(keysToAdd));

			bool isRestarting = (bool)args.getWithDefault(D.NEW_LEVEL, false);
			
			updatePlayerPortraitLocation();
			yield return new WaitForSeconds(1.0f); //delay for player update to finish
			toggleOverlayShroud(false, false, false);
			yield return new WaitForSeconds(DELAY_BEFORE_FIRST_POSITION_UPDATE); //Slight delay before moving the player pip


			yield return StartCoroutine(updatePlayerBoardPosition(true, isRestarting));
			
			rewardCallback(args);
		}

		private IEnumerator updatePlayerBoardPosition(bool doTween, bool isRestarting)
		{
			if (spawnedNodes == null || spawnedNodes.Count == 0 || qfcFeature == null)
			{
				Debug.LogError("Trying to update qfc board position on an invalid game board");
				yield break;
			}
			
			int newNodeIndex = 0;
			if (hasPendingJumps) 
			{
				for (int i = 0; i < homeTeam.Count; i++)
				{
					if (homeTeam[i].member == null)
					{
						continue;
					}
					if (homeTeam[i].member.zId == SlotsPlayer.instance.socialMember.zId)
					{
						newNodeIndex = homeTeam[i].position + pendingJumpsCount;
						if (newNodeIndex > spawnedNodes.Count - 1)
						{
							newNodeIndex = 0;
						}

						break;
					}
				}
			}
			else
			{
				if (dialogArgs == null)
				{
					Debug.LogError("Launched update position with incorrect dialog arguments");
					newNodeIndex = 0;
				}
				else
				{
					newNodeIndex = (int) dialogArgs.getWithDefault(D.SCORE, 0);
				}
				
			}

			int previousNodeIndex = 0;

			while (currentPlayerPosition != newNodeIndex) //currentPlayerPosition>0 && 
			{
				previousNodeIndex = currentPlayerPosition;
				if (currentPlayerPosition == spawnedNodes.Count - 1 && !isRestarting && !hasPendingJumps)
				{
					yield break;
				}
				
				// if this was launch flow, then dont call advance player
				// ie skipped Jumps are 0
				if (pendingJumpsCount == 0 && hasPendingJumps)
				{
					currentPlayerPosition++;
					if (currentPlayerPosition > spawnedNodes.Count - 1)
					{
						currentPlayerPosition = 0;
					}
				}
				else
				{
					currentPlayerPosition = qfcFeature.advancePlayer(SlotsPlayer.instance.socialMember.zId);
				}

				int nextNodeIndex = currentPlayerPosition;


				QFCBoardNodeObject currentNode = spawnedNodes[previousNodeIndex];
				QFCBoardNodeObject nextNode = spawnedNodes[nextNodeIndex];

				QFCBoardPlayerIconObject playerPipIcon = currentNode != null ? currentNode.currentPlayerIcon : null;
				if (playerPipIcon != null)
				{
					if (playerPipIcon.gameObject != null && nextNode != null && nextNode.playerContainer != null) 
					{
						if (doTween)
						{
							iTween.MoveTo(playerPipIcon.gameObject, iTween.Hash("position", nextNode.playerContainer, "islocal", false, "time", 0.75f,"easetype", iTween.EaseType.linear));
							Audio.play(playerIconMoveSound);
							yield return StartCoroutine(CommonAnimation.playAnimAndWait(playerPipIcon.animator, playerPipIcon.moveAnimation));
							Audio.play(playerCheckmarkSound);
						}
						else
						{
							playerPipIcon.gameObject.transform.position = nextNode.playerContainer.position;
						}	
					}

					if (currentNode != null)
					{
						currentNode.currentPlayerIcon = null;
						currentNode.playCompletedOffAnimation();	
					}
					
					if (nextNode != null)
					{
						nextNode.currentPlayerIcon = playerPipIcon;
						IEnumerator animIEnum = nextNode.playCompletedAnimation();
						if (animIEnum != null)
						{
							yield return StartCoroutine(animIEnum);
						}
					}
				}
			}
			CustomPlayerData.setValue(CustomPlayerData.QFC_PLAYER_LAST_SEEN_POSITION, currentPlayerPosition);
			hasPendingJumps = false;
			pendingJumpsCount = 0;
		}

		IEnumerator forceRepositionPlayerOnLoad()
		{
			if (pendingJumpsCount == 0)
			{
				int actualPlayerPosition = 0;
				for (int i = 0; i < homeTeam.Count; i++)
				{
					if (homeTeam[i].member.zId == SlotsPlayer.instance.socialMember.zId)
					{
						actualPlayerPosition = homeTeam[i].position;
						break;
					}
				}

				QFCBoardNodeObject actualPosition = spawnedNodes[actualPlayerPosition];
				QFCBoardNodeObject playerViewedPosition = spawnedNodes[currentPlayerPosition];

				QFCBoardPlayerIconObject playerPipIcon = actualPosition.currentPlayerIcon;
				if (playerPipIcon != null)
				{
					playerPipIcon.gameObject.transform.position = playerViewedPosition.playerContainer.position;
					playerViewedPosition.currentPlayerIcon = playerPipIcon;
					actualPosition.currentPlayerIcon = null;
				}
			}
			yield return new WaitForSeconds(DELAY_BEFORE_FIRST_POSITION_UPDATE);
			StartCoroutine(updatePlayerBoardPosition(true, false));
		}

		private void rewardCallback(Dict args)
		{
			AnswerDelegate answerCallback = (AnswerDelegate)dialogArgs.getWithDefault(D.CLOSE, null);
			if (answerCallback != null)
			{
				answerCallback.Invoke(args);
			}

			bool closeInstantlyIfLastEvent = (bool) args.getWithDefault(D.OPTION, false);
			//kill any displayed message
			meterDisplay.clearMessage();

			//turn off reward dialog shroud
			overlayShroud.SetActive(false);

			Dict newArgs = null;
			while (queuedEvents.Count > 0)
			{
				Dict queuedArgs = queuedEvents.Dequeue();
				if ((DisplayMode)queuedArgs.getWithDefault(D.MODE, DisplayMode.MAP) != DisplayMode.MAP)
				{
					newArgs = queuedArgs;
					break;
				}
			}
			
			if (newArgs != null)
			{
				//set dialog args to new value, re-run display mode check
				dialogArgs = newArgs;
				checkDisplayMode();
			}
			else if (!closeInstantlyIfLastEvent && QuestForTheChestFeature.instance != null && QuestForTheChestFeature.instance.isEnabled)
			{
				resetToMapMode();
				// in case the animations were skipped, there could be some nodes in incorrect state.
				forceNodesToCorrectStates();
			}
			else
			{
				StartCoroutine(waitForPendingActionsAndCloseDialog());
			}
		}

		private void forceNodesToCorrectStates()
		{
			if (currentPlayerPosition > 0 && currentPlayerPosition < spawnedNodes.Count)
			{
				if (!spawnedNodes[currentPlayerPosition].isComplete)
				{
					// play completed anim only if player has not seen it already
					spawnedNodes[currentPlayerPosition].swapToCompletedSprite();
					IEnumerator playCoinAnim = spawnedNodes[currentPlayerPosition].playCompletedAnimation();
					if (playCoinAnim != null)
					{
						StartCoroutine(playCoinAnim);
					}
				}
			}

			bool isRestarting = currentPlayerPosition == 0;
			int lastNodeIndex = isRestarting ? spawnedNodes.Count : currentPlayerPosition;

			for (int i = 0; i < lastNodeIndex && i < spawnedNodes.Count; i++)
			{
				if (!isRestarting)
				{
					spawnedNodes[i].swapToCompletedSprite();	
				}
				spawnedNodes[i].playCompletedOffAnimation();
			}
		}

		// when closing dialog from callbacks, we can use this to ensure all pending actions are cleared before closing.
		IEnumerator waitForPendingActionsAndCloseDialog()
		{
			while (Server.waitingForActionsResponse)
			{
				yield return null;
			}
			Dialog.close();
		}

		/// <summary>
		/// Upates the player portrait in the homeTeamPortraitsGrid on collecting a key
		/// </summary>
		private void updatePlayerPortraitLocation()
		{
			if (homeTeam == null)
			{
				return;
			}
			
			int oldPlayerIndex = 0;
			for (int i = 0; i < homeTeam.Count; i++)
			{
				if (homeTeam[i].member.zId == SlotsPlayer.instance.socialMember.zId)
				{					
					oldPlayerIndex = i;
					break;
				}
			}
			
			//if the player was already at the top no need to update position
			if (oldPlayerIndex != 0)
			{
				int newPlayerIndex = oldPlayerIndex;
				for (int i = 0; i < oldPlayerIndex; i++)
				{
					if (homeTeam[i].keys < homeTeam[oldPlayerIndex].keys)
					{
						newPlayerIndex = i;
						break;
					}
				}

				//update position only if the new index is different
				if (newPlayerIndex != oldPlayerIndex)
				{
					Transform player = homeTeamPortraitGrid.transform.GetChild(oldPlayerIndex);
					player.SetSiblingIndex(newPlayerIndex);
					//If the player is now the new leader make sure to toggle the team leader crown
					if (newPlayerIndex == 0)
					{
						QFCPlayerPortrait playerPortrait = player.GetComponent<QFCPlayerPortrait>();
						if (playerPortrait != null)
						{
							playerPortrait.ToggleTeamLeader(true);
						}

						Transform previousLeaderPortraitTransform = homeTeamPortraitGrid.transform.GetChild(newPlayerIndex + 1);
						QFCPlayerPortrait prevLeaderPortrait = previousLeaderPortraitTransform.GetComponent<QFCPlayerPortrait>();
						if (prevLeaderPortrait != null)
						{
							prevLeaderPortrait.ToggleTeamLeader(false);
							
							if (currentPlayerPosition >= 0 && currentPlayerPosition < spawnedNodes.Count && spawnedNodes[currentPlayerPosition].currentPlayerIcon != null)
							{
								spawnedNodes[currentPlayerPosition].currentPlayerIcon.setToLeaderSprite();
							}

							int oldLeaderPosition = homeTeam[0].position;
							if (oldLeaderPosition >= 0 && oldLeaderPosition < spawnedNodes.Count && spawnedNodes[oldLeaderPosition].homeTeamIcon != null)
							{
								spawnedNodes[homeTeam[0].position].homeTeamIcon.setToDefaultSprite();
							}
						}

						homeTeamPortraitGrid.reposition();
					}
				}
			}
		}

		private void resetToMapMode()
		{
			toggleOverlayShroud(false);
			showIdleChest(false, false);
			if (needReset)
			{
				needReset = false;
				resetMapObjects(false);
			}
			meterDisplay.gameObject.SetActive(true); //ensure meter display is active so coroutine in showmeteres runs
			meterDisplay.showTeamMeters();
			overlayShroud.SetActive(false);
			currentMode = DisplayMode.MAP;
			updateNotification();
		}

		private void resetKeyTotals(bool showTeamMeters)
		{
			clearTeamPortraits();
			initTeams();
			
			meterDisplay.reset(showTeamMeters);
		}

		private void loadBonusGame()
		{
			JSON bonusGameEvent = (JSON)dialogArgs.getWithDefault(D.BONUS_GAME, null);
			if (bonusGameEvent != null)
			{
				toggleButtons(false);
				JSON[] bonusGameOutcomeJsonArray = bonusGameEvent.getJsonArray("outcomes");
				if (bonusGameOutcomeJsonArray != null && bonusGameOutcomeJsonArray.Length > 0)
				{
					JSON bonusGameOutcomeJson = bonusGameOutcomeJsonArray[0];
					JSON absoluteCoinRewards = bonusGameEvent.getJSON("absolute_coin_rewards");
					int keysReward = bonusGameEvent.getInt("tokens", 0);
					SlotOutcome bonusOutcome = new SlotOutcome(bonusGameOutcomeJson);
					ModularChallengeGameOutcome.ModularChallengeGameOutcomeTypeEnum outcomeType =
						BonusGamePaytable.getPaytableOutcomeType(bonusOutcome.getBonusGamePayTableName());

					//Assuming different bonus games will need different prefabs, so load that here based on the bonus game type
					switch (outcomeType)
					{
						case ModularChallengeGameOutcome.ModularChallengeGameOutcomeTypeEnum.WHEEL_OUTCOME_TYPE:
							AssetBundleManager.load(this, WHEEL_GAME_PATH, bonusGameLoadSuccess, assetLoadFailed,
								Dict.create(D.BONUS_GAME, bonusOutcome, D.DATA, absoluteCoinRewards, D.KEY,
									keysReward));
							break;
						default:
							Debug.LogWarningFormat("Bonus game type {0} currently isn't supported in QFC", outcomeType);
							rewardCallback(null);
							break;
					}

					resetMapNodes();
				}
				else
				{
					Debug.LogError("QFC Bonus Game is in unexpected format");
					rewardCallback(null);
				}
			}
		}

		public void resetMapNodes()
		{
			for (int i = 0; i < spawnedNodes.Count; i++)
			{
				spawnedNodes[i].resetState();
			}
		}

		private void bonusGameLoadSuccess(string assetPath, Object obj, Dict data = null)
		{
			if (this == null) //Should be getting caught prior to this function being called but adding this check for verification
			{
				Debug.LogError("QFC Map SuccessCallback called on null object");
				return;
			}

			bool success = false;
			if (data != null)
			{
				SlotOutcome bonusGameOutcome = (SlotOutcome) data.getWithDefault(D.BONUS_GAME, null);
				int keysReward = (int)data.getWithDefault(D.KEY, 0);
				JSON absoluteCoinRewards = (JSON) data.getWithDefault(D.DATA, null);
				if (bonusGameOutcome != null && overlayParent != null && obj != null && absoluteCoinRewards != null)
				{
					GameObject miniGameObject = NGUITools.AddChild(overlayParent, obj as GameObject);
					if (miniGameObject != null)
					{
						currentMiniGame = miniGameObject.GetComponent<QFCMiniGameOverlay>();
						if (currentMiniGame != null)
						{
							int nodeIndex = qfcFeature.nodeData != null ? qfcFeature.nodeData.Count - 1 : -1;
							currentMiniGame.init(nodeIndex, bonusGameOutcome, absoluteCoinRewards, keysReward, toggleStaticOverlayShroud, startAdvanceAnimations); //Need to pass in actual Node index and not just assume the final node
							toggleOverlayShroud(true);
							SafeSet.gameObjectActive(overlayShroud, true);
							success = true;
						}
					}
				}
			}

			if (!success) //If anything above fails, jump to the outroCallback to keep the dialog going along
			{
				startAdvanceAnimations(Dict.create(D.OPTION, true, D.NEW_LEVEL, true));
			}
		}

		public void onNodeClicked(Dict data = null)
		{
			dismissSelectedPortraits();
			List<string> occupiedByList = (List<string>)data.getWithDefault(D.DATA, null);
			if (occupiedByList != null)
			{
				for (int i = 0; i < occupiedByList.Count; i++)
				{
					QFCPlayerPortrait occupantPortrait = null;
					if (zidsToPortraitsDict.TryGetValue(occupiedByList[i], out occupantPortrait))
					{
						occupantPortrait.playClickedAnimation();
						selectedOccupantPortraits.Add(occupantPortrait);
					}
				}

				Audio.play(playerIconInSound);
			}
		}

		public void dismissSelectedPortraits()
		{
			if (selectedOccupantPortraits.Count > 0)
			{
				Audio.play(playerIconOutSound);
				for (int i = 0; i < selectedOccupantPortraits.Count; i++)
				{
					selectedOccupantPortraits[i].playDismissedAnimation();
				}

				selectedOccupantPortraits.Clear();
			}
		}

		//Handle swapping display modes while the dialog is open if we're on the map display
		//Edge-case if a chest/reward event comes in while the map is already open
		private void newEventQueued()
		{
			if (queuedEvents.Count > 0)
			{
				dialogArgs = queuedEvents.Dequeue();
				checkDisplayMode();
			}
		}

		protected override void onHide()
		{
			dismissSelectedPortraits();
		}

		private static bool isStarted
		{
			get { return _instance != null || queuedEvents.Count > 0 || Scheduler.hasTaskWith("quest_for_the_chest_map");  }
		}

		public static void resetStaticClassData()
		{
			queuedEvents = new Queue<Dict>();
			hasViewed = false;
		}
		
		//Private classes for board prefabs serialization
		[System.Serializable] private class KeyValuePairOfQFCNodeTypeToGameObject : CommonDataStructures.SerializableKeyValuePair<QFCBoardNodeType, GameObject> {}
		[System.Serializable] private class SerializableDictionaryOfQFCNodeTypeToGameObject : CommonDataStructures.SerializableDictionary<KeyValuePairOfQFCNodeTypeToGameObject, QFCBoardNodeType, GameObject> {}
		[System.Serializable] private class KeyValuePairOfQFCPlayerIconTypeToGameObject : CommonDataStructures.SerializableKeyValuePair<QFCBoardPlayerIconType, GameObject> {}
		[System.Serializable] private class SerializableDictionaryOfQFCPlayerIconTypeToGameObject : CommonDataStructures.SerializableDictionary<KeyValuePairOfQFCPlayerIconTypeToGameObject, QFCBoardPlayerIconType, GameObject> {}
	}
}
