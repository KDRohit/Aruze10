using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;

// This class handles data/events for the Royal Rush feature 

public class RoyalRushEvent : IResetGame
{
	const string BUNDLE_NAME = "royal_rush";
	// Lobby Card
	private GameObject _lobbyOptionRef;
	private bool loadReq = false;
	
	public GameObject lobbyOptionReference
	{
		get
		{
			if (!loadReq && IsActive())
			{
				loadReq = true;
				instance.loadCachedObjects();
			}
			return _lobbyOptionRef;
		}
	}

	public GameObject tooltipRef;

	public static bool IsActive()
	{	
		//this logic indicates that we have a bundle ready now and not one waiting for a reload
		//if it doesn't have a lazy lazy bundle ready for next session, then it doesn't have one that we could 
		//load RIGHT NOW either.
		return !AssetBundleManager.shouldLazyLoadBundle(BUNDLE_NAME); 
	}

	public List<RoyalRushInfo> rushInfoList = new List<RoyalRushInfo>();

	private const string LOBBY_OPTION_PATH = "features/royal rush/prefabs/lobby option royal rush";
	private const string NON_ANIMATED_TOOLTIP_ASSET_PATH = "features/royal rush/Prefabs/royal rush loading tooltip";
	private const string ANIMATED_TOOLTIP_ASSET_PATH = "features/royal rush/prefabs/royal rush lobby tooltip";

	private static RoyalRushEvent privateInstance;

	public static int minLevel = 10;
	public static int initialSprintTime = 0;
	public static int additionalTimeAmount = 0;
	public static int minTimeRequired = 0; // In seconds
	public static int rushInfoUpdateTime = 60;
	public static int contestEndingSoonTime = 20; //In minutes
	public static int scoreSubmitEnd = 60;
	public static float scoreSubmittedResponseTimeout = 5.0f;

	// Full update every 5 mins
	private const int ROYAL_RUSH_FULL_UPDATE_TIMING = 300;
	private static GameTimerRange fullUpdateTimer;

	//Don't allow spins if we're waiting for the sprint summary results to come back. 
	//Consequence could be starting the sprint before the previous one's results dialog has popped. 
	//This causes time loss for the player when the previous results dialog pops at the end of the spin and their new sprint has already started
	public static bool waitingForSprintSummary = false; 

	// private constructor so singleton works nice.
	private RoyalRushEvent()
	{
	}

#region info retreival 
	public static RoyalRushEvent instance
	{
		get
		{
			// We could experiment gate this part, but it feels risky. 
			if (privateInstance == null)
			{
				privateInstance = new RoyalRushEvent();
			}

			return privateInstance;
		}
	}

	public RoyalRushInfo getInfoByKey(string key)
	{
		if (rushInfoList != null)
		{
			for (int i = 0; i<rushInfoList.Count; i++)
			{
				if (rushInfoList[i].gameKey == key)
				{
					return rushInfoList[i];
				}
			}
		}

		// We can fail silently here since it's prone to bad lola configs.
		// Basically make sure the game is tagged in LoLa and the event is on
		return null;
	}
#endregion


	public static void init(JSON data)
	{
		if (!IsActive())
		{
			return;
		}

		fullUpdateTimer = new GameTimerRange(GameTimer.currentTime, GameTimer.currentTime + ROYAL_RUSH_FULL_UPDATE_TIMING);
		fullUpdateTimer.registerFunction(RoyalRushEvent.instance.onNeedUpdate);

		// Before we do this maybe make sure we clean up old ones?
		instance.rushInfoList = new List<RoyalRushInfo>();
		minLevel = Data.liveData.getInt("ROYAL_RUSH_MIN_LEVEL", 10);
		minTimeRequired = Data.liveData.getInt("ROYAL_RUSH_TIMER_END_START", 300);
		// Make sure there are no double reg shenanigans
		instance.unregisterPersistantEvents();
		instance.registerPersistentEvents();
		initialSprintTime = Data.liveData.getInt("ROYAL_RUSH_TIMER_INITIAL", 0);
		additionalTimeAmount = Data.liveData.getInt("ROYAL_RUSH_TIMER_ADD", 0);
		rushInfoUpdateTime = Data.liveData.getInt("ROYAL_RUSH_TIMER_UPDATE", 60);
		contestEndingSoonTime = Data.liveData.getInt("ROYAL_RUSH_CONTEST_ENDING_SOON_TIME", 20);
		scoreSubmitEnd = Data.liveData.getInt("ROYAL_RUSH_TIMER_END_SUBMIT", 60);
		scoreSubmittedResponseTimeout = Data.liveData.getFloat("ROYAL_RUSH_SPRINT_SUMMARY_TIMEOUT", 5.0f);
		if (data != null)
		{
			// Get the key list. We don't know how exatly it'll be key'd
			for (int i = 0; i < data.getKeyList().Count; i++)
			{
				string eventKey = data.getKeyList()[i];

				// Grab releveant info FOR that game
				JSON sprintInfo = data.getJSON(eventKey);

				// GO GO GO
				instance.rushInfoList.Add(new RoyalRushInfo(sprintInfo, false, eventKey));

				// Load the lobby option or any other relevant UI objects that should be available at like...all times.
				//instance.loadCachedObjects();
			}
		}
		else 
		{
			Debug.LogError("RoyalRushEvent::init - Data was null. We should abort feature setup here...");
		}
	}

	private void loadCachedObjects()
	{
		AssetBundleManager.load(this, LOBBY_OPTION_PATH, objectLoadSuccess, objectLoadFailure, isSkippingMapping:true, fileExtension:".prefab");
	}

	public void loadTooltip(GameObject parent, bool isFTUE)
	{
		Dict gameObjectToPass = Dict.create(D.DATA, parent);

		if (isFTUE)
		{
			AssetBundleManager.load(this, ANIMATED_TOOLTIP_ASSET_PATH, objectLoadSuccess, objectLoadFailure, gameObjectToPass, isSkippingMapping:true, fileExtension:".prefab");
		}
		else
		{
			AssetBundleManager.load(this, NON_ANIMATED_TOOLTIP_ASSET_PATH, objectLoadSuccess, objectLoadFailure, gameObjectToPass);
		}
	}

	private void objectLoadSuccess(string assetPath, Object obj, Dict data = null)
	{
		GameObject parent = null;

		if (data != null)
		{
			parent = data.getWithDefault(D.DATA, null) as GameObject;
		}
		string filePath = assetPath.Split('.')[0];
		switch (filePath)
		{
			case LOBBY_OPTION_PATH:
				_lobbyOptionRef = obj as GameObject;
				break;

			case ANIMATED_TOOLTIP_ASSET_PATH:
			case NON_ANIMATED_TOOLTIP_ASSET_PATH:
				tooltipRef = obj as GameObject;
				if (parent != null)
				{
					NGUITools.AddChild(parent, tooltipRef);
				}
				else
				{
					Debug.LogError("We were missing the parent for the Royal Rush tooltip");
				}
				break;

			default:
				Bugsnag.LeaveBreadcrumb("Loaded an object but we don't know where to assign it: " + filePath);
				break;
		}
	}

	private void objectLoadFailure(string assetPath, Dict data = null)
	{
		Debug.LogError("PartnerPowerupCampaign::partnerPowerIconLoadFailure - Failed to load asset at: " + assetPath);
	}

	public void playRoyalRushFTUE(Dict args)
	{
		// Yeah this is ass backwards but we have to do this since calling "goToPage" will delete our original refernce
		LobbyOptionButtonRoyalRush.ftueButton = null;
		MainLobby.hirV3.pageController.goToPage(LobbyOptionButtonRoyalRush.ftuePage, onArriveAtLobbyV3Page);
	}

	private void onAfterScroll()
	{
		// Remove event and go go go. 
		LobbyOptionButtonRoyalRush.ftueButton.loadTooltip(true);
		MOTDFramework.markMotdSeen(Dict.create(D.MOTD_KEY, "ftue_royal_rush"));
	}

	private void onArriveAtLobbyV3Page(GameObject page, int pageNumber)
	{
		if (MainLobby.hirV3 != null && MainLobby.hirV3.pageController != null)
		{
			MainLobby.hirV3.pageController.removeGoToPageCallback(onArriveAtLobbyV3Page);
		}

		if (LobbyOptionButtonRoyalRush.ftueButton != null)
		{ 
			LobbyOptionButtonRoyalRush.ftueButton.loadTooltip(true);
		}

		MOTDFramework.markMotdSeen(Dict.create(D.MOTD_KEY, "ftue_royal_rush"));
	}

#region server calls/callbacks

	private void onNeedUpdate(Dict args = null, GameTimerRange parentTimer = null)
	{
		if (ExperimentWrapper.RoyalRush.isInExperiment)
		{
			RoyalRushAction.getUpdate();
			fullUpdateTimer = new GameTimerRange(GameTimer.currentTime, GameTimer.currentTime + ROYAL_RUSH_FULL_UPDATE_TIMING);
			fullUpdateTimer.registerFunction(onNeedUpdate);
		}
	}

	public void getAllRushInfos()
	{
		RoyalRushAction.getUpdate();
	}

	// Loop through and find the matching royalRushInfo for these.
	// Once we do, call the event which may trigger a state change among other things
	// Anything hooked into that event should get the call though and adjust accordingly.
	private void onGetRushInfo(JSON data = null)
	{
		data = data.getJSON("royal_rush");

		for (int i = 0; i < data.getKeyList().Count; i++)
		{
			string rushKey = data.getKeyList()[i];

			// Grab releveant info FOR that game
			JSON sprintInfo = data.getJSON(rushKey);
			string gameKey = sprintInfo.getString("game_key","");
			RoyalRushInfo info = getInfoByKey(gameKey);

			// If this isn't new info
			if (info != null)
			{
				info.onGetRushInfo(sprintInfo);

				// In case we run a 1 hour event after say, a 2 hour event.
				info.rushKey = rushKey;
			}
			else
			{
				// Otherwise add it to the list.
				rushInfoList.Add(new RoyalRushInfo(sprintInfo, false, rushKey));
			}
		}
	}

	private void onEndRoyalRush(JSON data = null)
	{
		data = data.getJSON("royal_rush");

		if (data != null)
		{
			for (int i = 0; i < data.getKeyList().Count; i++)
			{
				string rushKey = data.getKeyList()[i];
				// Grab releveant info FOR that game
				if (!string.IsNullOrEmpty(rushKey))
				{
					JSON sprintInfo = data.getJSON(rushKey);

					if (sprintInfo != null)
					{
						string gameKey = sprintInfo.getString("game_key","");
						
						RoyalRushInfo info = getInfoByKey(gameKey);
						if (info != null)
						{
							info.onGetRushEnd(sprintInfo);
						}
						else
						{
							Debug.LogError("RoyalRushEvent::onEndRoyalRush - Could not find royal rush info for " + gameKey);
						}
					}
					else
					{
						Debug.LogError("RoyalRushEvent::onEndRoyalRush - Sprint info missing");
					}
				}
			}
		}
		else
		{
			Debug.LogError("RoyalRushEvent::onEndRoyalRush - Missing royal rush data when we ended royal rush!!");
		}
	}

	public void onCompleteRoyalRushEvent(JSON data = null)
	{
		string eventID = data.getString("event", "");
		Server.registerEventDelegate("royal_rush_credits", onGetCredits);
		if (!string.IsNullOrEmpty(eventID))
		{
			RoyalRushInfo winnerInfo = new RoyalRushInfo(data, true);

			// Claim whatever now.
			RoyalRushAction.completeEvent(eventID);

			// Create rush info
			Dict args = Dict.create(D.DATA, winnerInfo, D.OPTION, "Event Over");
			RoyalRushStandingsDialog.showDialog(args);
		}
		else
		{
			Debug.LogError("onCompleteRoyalRushEvent - eventID was missing!");
		}
	}

	private void onGetCredits(JSON data = null)
	{
		// Debugging only.
		//Debug.LogError("Success " + data);
	}

	private void registerPersistentEvents()
	{
		Server.registerEventDelegate("royal_rush_info", onGetRushInfo, true);
		Server.registerEventDelegate("royal_rush_ended", onEndRoyalRush, true);
	}

	private void unregisterPersistantEvents()
	{
		Server.unregisterEventDelegate("royal_rush_info", onGetRushInfo);
		Server.unregisterEventDelegate("royal_rush_ended", onEndRoyalRush);
	}

	public static void onLoadBundleRequest()
	{
		AssetBundleManager.downloadAndCacheBundle("royal_rush", true, true, blockingLoadingScreen:false);
	}

	public static void resetStaticClassData()
	{
		minLevel = 10;
		initialSprintTime = 0;
		additionalTimeAmount = 0;
		rushInfoUpdateTime = 60;
		minTimeRequired = 0;
		contestEndingSoonTime = 20;
		scoreSubmitEnd = 60;
		scoreSubmittedResponseTimeout = 5.0f;
		fullUpdateTimer = null;
		waitingForSprintSummary = false;
		privateInstance = null;
	}

#endregion

}

