using UnityEngine;
using System.Collections.Generic;
using Com.Scheduler;
using Com.HitItRich.EUE;
using Zynga.Core.Util;

public class SlotventuresChallengeCampaign : ChallengeLobbyCampaign
{
	public const string CAMPAIGN_ID = "challenge_slotventures";
	public bool canShowCompleteAnimation = false;

	private const int CONCLUSION_Z = 400;
	private const int PROGRESS_Z = 200;
	private const int OFFSCREEN_PROGRESS_PANEL_Y = -740;
	private List<JSON> packDrops = new List<JSON>();
	private bool isReadyForEUE = false;

	public override void init(JSON data)
	{
		base.init(data);

		// These are the intervals in minutes that the "ending soon" toaster will show up.
		int[] intervals = { 30, 15, 5};

		// Clear the subtimers and re-add them soon after
		timerRange.clearSubtimers();
		for (int i = 0; i < intervals.Length; i++)
		{
			timerRange.registerFunction(showEndingSoonToaster, null, intervals[i] * Common.SECONDS_PER_MINUTE);
		}
		unlockLevel = ExperimentWrapper.Slotventures.levelLock;

		setupThemeData();
		Collectables.Instance.registerForPackDrop(addPackDropData, "slotventures");

		isReadyForEUE = true;
	}

	public static void setupThemeData()
	{
		IPreferences prefs = SlotsPlayer.getPreferences();
		if (Data.debugMode && !string.IsNullOrEmpty(prefs.GetString(DebugPrefs.SLOTVENTURE_THEME_OVERRIDE, "")))
		{
			SlotventuresLobby.setAssetData();
			SlotventuresLobby.assetData.themeName = prefs.GetString(DebugPrefs.SLOTVENTURE_THEME_OVERRIDE, "GreatestHits");
			SlotventuresLobby.setupThemedAssetPaths();
			SlotventuresLobby.setupAudioMap();
		}
		else if (Data.liveData != null)
		{
			SlotventuresLobby.setAssetData();

			if (string.IsNullOrEmpty(SlotventuresLobby.assetData.themeName))
			{
				if (ExperimentWrapper.Slotventures.isEUE)
				{
					SlotventuresLobby.assetData.themeName = Data.liveData.getString("EUE_SLOTVENTURES_THEME", "").ToLower();
				}
				else
				{
					SlotventuresLobby.assetData.themeName = Data.liveData.getString("SLOTVENTURES_THEME", "").ToLower();
				}
			}
			SlotventuresLobby.setupThemedAssetPaths();
			SlotventuresLobby.setupAudioMap();
		}
		else
		{
			Debug.LogError("SlotventuresChallengeCampaign::init - Live data was null or the theme key was missing.");
		}
	}

	public override void onProgressUpdate(JSON response)
	{
		base.onProgressUpdate(response);

		if (isReadyForEUE && EUEManager.isComplete && ExperimentWrapper.Slotventures.isEUE)
		{
			isReadyForEUE = false;

			Scheduler.addTask(new SlotventuresEUETask());
		}
	}

	public override long currentJackpot
	{
		get
		{
			for (int i = 0; i < missions[missions.Count - 1].rewards.Count; i++)
			{
				if (missions[missions.Count - 1].rewards[i].type == MissionReward.RewardType.CREDITS)
				{
					return missions[missions.Count - 1].rewards[i].amount;
				}
			}

			ChallengeLobbyCampaign current = CampaignDirector.find(SlotventuresChallengeCampaign.CAMPAIGN_ID) as ChallengeLobbyCampaign;
			Debug.LogError("Didn't find a credit jackpot for campaign: " + (current == null ? "invalid" : current.campaignID));
			return 0;
		}
	}

	public override long nextJackpot
	{
		get
		{
			long baseAmount = currentJackpot;
			ChallengeLobbyCampaign current = CampaignDirector.find(SlotventuresChallengeCampaign.CAMPAIGN_ID) as ChallengeLobbyCampaign;
			return baseAmount + System.Convert.ToInt64(baseAmount * (current == null ? 0 : current.replayRewardRatio));
		}
	}

	protected override void showMissionComplete(List<JSON> completionJSON)
	{
		base.showMissionComplete(completionJSON);
		if (completionJSON != null)
		{
			for (int i = 0; i < completionJSON.Count; i++)
			{
				int completedEventIndex = completionJSON[i].getInt("event_index", 1) - 1;	
				if (completedEventIndex < 0 || completedEventIndex > missions.Count)
				{

					continue;
				}

				if (missions[completedEventIndex] != null)
				{
					long reward = missions[completedEventIndex].getCreditsReward;
					if (reward > 0)
					{
						Server.handlePendingCreditsCreated(SlotventuresLobby.CREDIT_SOURCE, reward);	
					}	
				}

			}
		}

	}

	protected override void showCampaignComplete(List<JSON> completionJSON)
	{
		base.showCampaignComplete(completionJSON);
		canShowCompleteAnimation = true;
	}

	// This will kick off the lobby forced flow for finishing a challenge, but we'll need to cache didCompleteAll
	// so we know that we should play/enable the big animation?
	protected override void showChallengesDialog(ChallengeLobbyCampaign campaign, Mission mission, JSON data, bool didCompleteAll)
	{
		// The final mission is the final presentation
		if (!didCompleteAll)
		{
			ChallengeLobbyObjectivesDialog.showDialog(campaign, mission, data, true, didCompleteAll);
		}
	}

	private void showEndingSoonToaster(Dict args = null, GameTimerRange sender = null)
	{
		if (SlotventuresLobby.toasterPrefabEnding == null)
		{
			AssetBundleManager.load(SlotventuresLobby.TOASTER_PATH_ENDING_SOON, loadToasterSuccess, SlotventuresLobby.bundleLoadFailure);
			return;
		}

		if (timerRange.timeRemaining <= 0)
		{
			return;
		}

		GameObject objectToAttachTo = null;
		if (this == null || state == ChallengeLobbyCampaign.COMPLETE || currentEventIndex >= missions.Count)
		{
			return;
		}

		if (SpinPanel.hir != null)
		{
			objectToAttachTo = SpinPanel.hir.topEdge.gameObject;
		}
		else if (SlotventuresLobby.instance != null)
		{
			SlotventuresLobby lobbyInstance = SlotventuresLobby.instance as SlotventuresLobby;
			objectToAttachTo = lobbyInstance.toasterAnchor;

			// Move progress panel out of the way
			iTween.MoveTo(lobbyInstance.progressPanel.gameObject, iTween.Hash("y", OFFSCREEN_PROGRESS_PANEL_Y, "z", PROGRESS_Z, "time", 1, "islocal", true, "easetype", iTween.EaseType.linear));
		}

		if (objectToAttachTo != null)
		{
			NGUITools.AddChild(objectToAttachTo, SlotventuresLobby.toasterPrefabEnding);
		}
	}

	private void loadToasterSuccess(string path, Object obj, Dict args)
	{
		GameObject endingSoonToaster = obj as GameObject;
		if (endingSoonToaster != null)
		{
			SlotventuresLobby.toasterPrefabEnding = endingSoonToaster;
			showEndingSoonToaster();
		}
	}

	public override bool isGameUnlocked(string gameKey)
	{
		Mission mission = findWithGame(gameKey);
		int missionIndex = missions.IndexOf(mission);
		LobbyGame game = LobbyGame.find(gameKey);
		if (missionIndex > 0 && missionIndex <= currentEventIndex)
		{
			return true;
		}
		else if (game != null && 
				(game.xp.isSkuGameUnlock || 
				 game.xp.isPermanentUnlock || 
				 (UnlockAllGamesFeature.instance != null && UnlockAllGamesFeature.instance.isEnabled)))
		{
			return true;
		}

		return false;
	}

	protected override void showOverDialog()
	{
		base.showOverDialog();

		SlotventuresMOTD.showDialog("", SlotventuresMOTD.DialogState.EVENT_ENDED);
	}

	public override void restart()
	{
		base.restart();
		canShowCompleteAnimation = false;
	}

	public void addPackDropData(JSON data)
	{
		packDrops.Add(data);
	}

	public bool dropPackCheck()
	{
		bool showingPacks = false;
		if (packDrops != null && packDrops.Count > 0)
		{
			showingPacks = true;
			for (int i = 0; i < packDrops.Count; i++)
			{
				Collectables.claimPackDropNow(packDrops[i]);
			}

			packDrops.Clear();
		}

		return showingPacks;
	}

	public static void loadBundles()
	{
		if (!AssetBundleManager.isBundleCached(SlotventuresLobby.COMMON_BUNDLE_NAME))
		{
			AssetBundleManager.downloadAndCacheBundle(SlotventuresLobby.COMMON_BUNDLE_NAME, false, true, true);
		}

		if (!AssetBundleManager.isBundleCached(SlotventuresLobby.COMMON_BUNDLE_NAME_SOUNDS))
		{
			AssetBundleManager.downloadAndCacheBundle(SlotventuresLobby.COMMON_BUNDLE_NAME_SOUNDS, false, true, true);
		}

		if (!AssetBundleManager.isBundleCached(SlotventuresLobby.THEMED_BUNDLE_NAME))
		{
			AssetBundleManager.downloadAndCacheBundle(SlotventuresLobby.THEMED_BUNDLE_NAME, false, true, true);
		}
	}

	public static void showVideo()
	{
		VideoDialog.showDialog(
				ExperimentWrapper.Slotventures.videoUrl, 
				"", 
				"Play Now!", 
				summaryScreenImage: ExperimentWrapper.Slotventures.videoSummaryPath, 
				autoPopped: false,
				statName: "SV_eue"
				);
	}
}
