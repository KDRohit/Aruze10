using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Com.Scheduler;

public class RobustCampaign : ChallengeCampaign
{	
	public List<int> lastCompletedIndices;
	public int completedEventIndex = -1;
	public List<MissionReward> lastCompletedMissionRewards;
	public List<JSON> packDropData = new List<JSON>();

	// audio pack
	public string audioPackKey = "challenges_robust";
	public static DialogAudioPack audioPack = new DialogAudioPack();

	// =============================
	// DYNAMIC SOUNDS
	// =============================
	public string soundGoalComplete = "GoalOpen";
	public string soundChallengeComplete = "ChallengeCompleteOpen";
	public string soundFlash = "FlashBox";
	public string soundHiCollect = "HiliteCollect";
	public string soundCollect = "CollectButtonConfirm";
	public string soundCheckbox = "CheckBox";
	public string soundReplay = "GoalOpen";
	public string soundMusic = "Tune";
	public string soundClose = "XOut";
	public string soundToast = "ToastGoalComplete";
	
	public const string LAUNCH_DETAIL = "challenge";
	
	public bool isEUEChallenges
	{
		get
		{
			return CustomPlayerData.getBool("ftue_challenges", false);
		}
	}

	public override void init(JSON data)
	{
		base.init(data);
		campaignID = CampaignDirector.ROBUST_CHALLENGES;
		motdPrefix = "robust_challenges";

		audioPackKey = data.getString("audio", audioPackKey);

		if (!string.IsNullOrEmpty(audioPackKey))
		{
			audioPack = new DialogAudioPack(audioPackKey);
			audioPack.addAudio("GoalOpen", soundGoalComplete);
			audioPack.addAudio("ChallengeComplete", soundChallengeComplete);
			audioPack.addAudio("HiCollect", soundHiCollect);
			audioPack.addAudio("CollectButtonConfirm", soundCollect);
			audioPack.addAudio("FlashBox", soundFlash);
			audioPack.addAudio("Replay", soundReplay);
			audioPack.addAudio("ToastGoalComplete", soundToast);
			audioPack.addAudio("CheckBox", soundCheckbox);
			audioPack.addAudio(DialogAudioPack.MUSIC, soundMusic);
			audioPack.addAudio(DialogAudioPack.CLOSE, soundClose);
			
			// Make sure LobbyLoader.Awake() has been called to start downloading all necessary asset bundles for main lobby.
			// audioPack.preloadAudio() might get involve clip downloads which should have lower priority than bundles
			// for main lobby.
			// so that we can have LobbyLoader contruct main lobby faster for better loading performance
			if(LobbyLoader.instance != null)
			{
				RoutineRunner.instance.StartCoroutine(preloadAudio(audioPack));
			}
			// If LobbyLoader has not started yet, we register lobbyLoadEvent and do preloadAudio clip when lobby is ready
			else
			{
				LobbyLoader.lobbyLoadEvent -= onLobbyLoad;
				LobbyLoader.lobbyLoadEvent += onLobbyLoad;
			}
		}

		// check if we should disable the ftue challenges at this point
		if (variant != PlayerPrefsCache.GetString(CURRENT_CAMPAIGN_ID))
		{
			PlayerPrefsCache.SetString(CURRENT_CAMPAIGN_ID, variant);
			CustomPlayerData.setValue("ftue_challenges", false);
		}

		// if the user's spin count is still 0, we'll be doing ftue challenges
		if (GameExperience.totalSpinCount == 0)
		{
			CustomPlayerData.setValue("ftue_challenges", true);
		}
		
		// Make sure we sign up for pack drops from collectables
		Collectables.Instance.registerForPackDrop(onPackDropped, "challenges");
	}

	private void onLobbyLoad(Dict args = null)
	{
		if (audioPack != null)
		{
			RoutineRunner.instance.StartCoroutine(preloadAudio(audioPack));
		}
		// Unregister this event handler
		LobbyLoader.lobbyLoadEvent -= onLobbyLoad;
	}
	
	private IEnumerator preloadAudio(DialogAudioPack audioPack)
	{
		// Wait one more frame to make sure.
		yield return null;
		audioPack.preloadAudio();
	}
	
	public override void unregisterEvents()
	{
		base.unregisterEvents();
		
		// make sure we unrgister all events here
		LobbyLoader.lobbyLoadEvent -= onLobbyLoad;
	}
	
	public override void onCampaignLost(JSON response)
	{
		RobustChallengesEnded.processEndedData(response);
	}

	protected override void showCampaignComplete(List<JSON> completionJSON)
	{
		base.showCampaignComplete(completionJSON);
	
		int completedEventIndex = missions.Count - 1;

		List<int> typeIndexes = new List<int>();
		if (completionJSON != null)
		{
			for (int i = 0; i < completionJSON.Count; i++)
			{
				typeIndexes.AddRange(completionJSON[i].getIntArray("types"));
			}	
		}
		
		//D.AMOUNT ends up getting assigned to campaign.lastCompletedMissionRewards which gets iterated over
		//and each reward of type == RewardType.CREDITS gets added to coinsWon which gets passed
		//to SlotsPlayer.addFeatureCredits() in waitForFinalAnimationsAndRollups() when the dialog is closed
		long totalPendingCreditsAmount = getTotalCreditRewardForMission(completedEventIndex);
		if (totalPendingCreditsAmount > 0)
		{
			Server.handlePendingCreditsCreated("robustChallengeObjective", totalPendingCreditsAmount);
		}

		Dict completedArgs = Dict.create
			(
				D.INDEX, completedEventIndex,
				D.DATA, typeIndexes.ToArray(),
				D.AMOUNT, missions[completedEventIndex].rewards
			);

		//don't schedule this as we need to start the timer so the icon doesn't disappear
		RobustChallengesObjectivesDialog.showDialog("", completedArgs);
		
		Scheduler.addFunction(showIconAnim, Dict.create(D.MESSAGE, "goal_complete", D.DATA, false));
	}

	protected override void showMissionComplete(List<JSON> completionJSON)
	{
		base.showMissionComplete(completionJSON);
		Dictionary<int, List<int>> completedEvents = new Dictionary<int, List<int>>();

		if (completionJSON != null)
		{
			for (int i = 0; i < completionJSON.Count; i++)
			{
				int completedEventIndex = completionJSON[i].getInt("event_index", 0) - 1;
				
				// It's probably on replay and we just rolled over
				if (completedEventIndex == -1 && shouldRepeat)
				{
					completedEventIndex = missions.Count - 1;
				}
				
				
				List<int> typeIndexes = null;
				if (!completedEvents.TryGetValue(completedEventIndex, out typeIndexes))
				{
					typeIndexes = new List<int>();
					completedEvents.Add(completedEventIndex, typeIndexes);
				}
				typeIndexes.AddRange(completionJSON[i].getIntArray("types"));
			}
		}

		//for each completed mission do the following
		foreach (KeyValuePair<int, List<int>> kvp in completedEvents)
		{
			//D.AMOUNT ends up getting assigned to campaign.lastCompletedMissionRewards which gets iterated over
			//and each reward of type == RewardType.CREDITS gets added to coinsWon which gets passed
			//to SlotsPlayer.addFeatureCredits() in waitForFinalAnimationsAndRollups() when the dialog is closed
			long totalPendingCreditsAmount = getTotalCreditRewardForMission(kvp.Key);
			if (totalPendingCreditsAmount > 0)
			{
				Server.handlePendingCreditsCreated("robustChallengeObjective", totalPendingCreditsAmount);	
			}
		
			Dict completedArgs = Dict.create
			(
				D.INDEX, kvp.Key,
				D.DATA, kvp.Value.ToArray(),
				D.AMOUNT, missions[kvp.Key].rewards,
				D.MODE, true
			);
		
			RobustChallengesObjectivesDialog.showDialog("", completedArgs);
		
			//show the icon animation
			Scheduler.addFunction(showIconAnim, Dict.create(D.MESSAGE, "goal_complete", D.DATA, false));
		}
	}

	protected override void showTypeComplete(List<JSON> completionJson)
	{
		base.showTypeComplete(completionJson);
		if (completionJson != null)
		{
			for (int i = 0; i < completionJson.Count; i++)
			{
				int[] typeIndexes = completionJson[i].getIntArray("types");
				if (typeIndexes != null)
				{
					if (lastCompletedIndices == null)
					{
						lastCompletedIndices = new List<int>(typeIndexes);
					}
					else
					{
						lastCompletedIndices.AddRange(typeIndexes);
					}
				}
			}
		}
		
		// Show type complete message in black box.
		Scheduler.addFunction(showIconAnim, Dict.create(D.MESSAGE, "goal_complete", D.DATA, false));
	}

	protected override void showTypeReset(JSON resetJSON)
	{
		base.showTypeReset(resetJSON);

		// Show type reset message
		Scheduler.addFunction(showIconAnim, Dict.create(D.MESSAGE, "goal_reset", D.DATA, true));
	}

	protected virtual void showIconAnim(Dict args)
	{
		Scheduler.removeFunction(showIconAnim);
		if (SpinPanel.hir != null && SpinPanel.hir.featureButtonHandler != null)
		{
			SpinPanel.hir.featureButtonHandler.showRobustChallengesMessage(Localize.textUpper((string)args[D.MESSAGE]), (bool)args[D.DATA]);
		}
	}

	protected virtual void hideXinYIcon(Dict args)
	{
		if (SpinPanel.hir != null && SpinPanel.hir.featureButtonHandler != null)
		{
			SpinPanel.hir.featureButtonHandler.hideCoinsOverSpinIcon();
		}
	}

	protected override void showOverDialog()
	{
		RobustChallengesObjectivesDialog.showDialog();
	}

	public static bool hasActiveRobustCampaignInstance
	{
		get
		{
			return CampaignDirector.robust != null && CampaignDirector.robust.isActive;
		}
	}

	public static bool hasActiveEUEChallenges
	{
		get { return hasActiveRobustCampaignInstance && CampaignDirector.robust.isEUEChallenges; }
	}
	
	public void onPackDropped(JSON data)
	{
		packDropData.Add(data);
	}

	public void playAudio(string keyName)
	{
		if (audioPack != null)
		{
			if (!string.IsNullOrEmpty(audioPack.getAudioKey(keyName)))
			{
				Audio.playAudioFromURL(audioPack.getAudioKey(keyName));
			}
		}
	}
}
