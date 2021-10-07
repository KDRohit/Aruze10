using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class RobustChallengesObjectivesDialog : DialogBase
{
	private const string DEFAULT_IMAGE_PATH = "challenges/bigger_better_challenge_neutral.png";

	[SerializeField] private TextMeshPro titleLabel;
	[SerializeField] private TextMeshPro descriptionLabel;
	[SerializeField] private TextMeshPro timerLabel;
	[SerializeField] private TextMeshPro awardLabel;
	[SerializeField] private Renderer backgroundRenderer;
	[SerializeField] private RobustChallengesTypeBlock sampleBlock;
	[SerializeField] private UICenteredGrid objectivesGrid;
	[SerializeField] private UICenteredGrid inProgressRewardGrid;

	[SerializeField] private Animator objectivesOutroAnimator;
	[SerializeField] private GameObject completeParent;
	[SerializeField] private GameObject objectivesParent;
	[SerializeField] private GameObject flashObject;
	[SerializeField] private UICenteredGrid completedRewardGrid;
	[SerializeField] private ButtonHandler collectButton;
	[SerializeField] private GameObject singleRewardParent;
	[SerializeField] private GameObject multipleRewardsParent;

	[Header("Jackpot bar (for Lootbox)")] 
	[SerializeField] private rewardBar rewardBar;

	private bool showingRewards = false;

	private List<RobustChallengesTypeBlock> allObjectives = new List<RobustChallengesTypeBlock>();
	private List<RobustChallengesRewardIcon> allRewards = new List<RobustChallengesRewardIcon>();

	private long coinsWon = 0L;
	private static string tempMotdKey;

	private const string REWARD_PREFAB_PATH = "Features/Robust Challenges V2/Prefabs/Instanced Prefabs/Rewards/Robust Challenges V2 {0} Reward Icon";
	private const string REWARD_ITEM_PREFAB_PATH = "Features/Robust Challenges V2/Prefabs/Instanced Prefabs/Robust Challenges V2 Reward Item";
	private const string ZAP_OBJECTIVE_BUTTON_FORMAT = "RobustDialogObjectiveButton_{0}";

	public override void init()
	{
		campaign.playAudio("GoalOpen");

		downloadedTextureToRenderer(backgroundRenderer, 0);
		showingRewards = (bool)dialogArgs.getWithDefault(D.MODE, false);

		setLabels();
		setTypeBlocks();
		setRewards();

		if (showingRewards)
		{
			campaign.playAudio(DialogAudioPack.MUSIC);
			campaign.playAudio("ChallengeComplete");
			StartCoroutine(playChallengeCompleteAnimations());
			StatsManager.Instance.LogCount("dialog", "robust_challenges_complete", ExperimentWrapper.RobustChallengesEos.variantName, GameState.game != null ? GameState.game.keyName : "", (campaign.completedEventIndex + 1).ToString(), "view");
		}
		else
		{
			campaign.playAudio("GoalComplete");

			if (campaign.lastCompletedIndices != null)
			{
				StartCoroutine(checkOffRecentlyCompletedChallenges());
			}
			StatsManager.Instance.LogCount("dialog", "robust_challenges_motd", CampaignDirector.robust.variant, GameState.game != null ? GameState.game.keyName : "", (campaign.currentEventIndex + 1).ToString(), "view");
		}

		if (dialogArgs.containsKey(D.MOTD_KEY))
		{
			MOTDFramework.markMotdSeen(dialogArgs);
		}
	}

	private IEnumerator checkOffRecentlyCompletedChallenges()
	{
		for (int i = 0; i < campaign.lastCompletedIndices.Count; i++)
		{
			if (i > 0)
			{
				yield return new WaitForSeconds(0.05f);
			}
			int index = campaign.lastCompletedIndices[i];
			RobustChallengesTypeBlock lastCompletedObjective = allObjectives[index];
			lastCompletedObjective.animator.Play("checkmark_intro");
			campaign.playAudio("CheckBox");
		}

		campaign.lastCompletedIndices = null;
	}

	private IEnumerator playChallengeCompleteAnimations()
	{
		SafeSet.gameObjectActive(closeButtonHandler.gameObject, false);

		if (campaign == null || campaign.lastCompletedIndices == null || campaign.missions == null)
		{
			Debug.LogError("RobustChallengesObjectivesDialog::playChallengeCompleteAnimations - Missing vital data for showing the challenge complete animations ");
			Dialog.immediateClose(this);
			yield break;
		}
		
		bool areIndiciesSafe = campaign.missions.Count >  campaign.completedEventIndex && allObjectives.Count >= campaign.lastCompletedIndices.Count;
		
		if (areIndiciesSafe)
		{
			DialogData dialogData = campaign.missions[campaign.completedEventIndex].getDialogByState(ChallengeCampaign.COMPLETE);

			if (dialogData != null)
			{
				for (int i = 0; i < campaign.lastCompletedIndices.Count; i++)
				{
					int index = campaign.lastCompletedIndices[i];
					RobustChallengesTypeBlock lastCompletedObjective = allObjectives[index];
					lastCompletedObjective.animator.Play("checkmark_intro");
				}

				yield return new WaitForSeconds(1.0f);

				for (int i = 0; i < allObjectives.Count; i++)
				{
					if (i > 0)
					{
						yield return new WaitForSeconds(0.15f); //Slight stagger on the cards outro
					}

					allObjectives[i].animator.Play("card_outro");
				}

				if (objectivesOutroAnimator != null)
				{
					objectivesOutroAnimator.Play("outro");
				}
				
				yield return new WaitForSeconds(1.0f);

				SafeSet.gameObjectActive(objectivesParent, false);
				SafeSet.gameObjectActive(flashObject, true);

				campaign.playAudio("FlashBox");

				yield return new WaitForSeconds(0.5f);

				if (backgroundRenderer != null)
				{
					downloadedTextureToRenderer(backgroundRenderer, 1);
				}

				if (titleLabel != null && dialogData.titleText != null)
				{
					titleLabel.text = dialogData.titleText.ToUpper();
				}

				if (descriptionLabel != null && dialogData.description != null)
				{
					descriptionLabel.text = dialogData.description;
				}

				yield return new WaitForSeconds(0.5f);

				SafeSet.gameObjectActive(completeParent, true);

				yield return new WaitForSeconds(1.167f);
			}
			else
			{
				Debug.LogError("Missing Dialog Data for playChallengeCompleteAnimations");
			}
		}
		else
		{
			SafeSet.gameObjectActive(objectivesParent, false);
			SafeSet.gameObjectActive(flashObject, false);
			SafeSet.gameObjectActive(completeParent, true);
			Debug.LogError("RobustChallengesObjectivesDialog::playChallengeCompleteAnimations - Unsafe indicies");
		}
		
		closeButtonHandler.gameObject.SetActive(true);
		collectButton.registerEventDelegate(onCloseButtonClicked);
	}

	protected virtual void Update()
	{
		AndroidUtil.checkBackButton(onCloseButtonClicked);
	}

	// Set dialog title and description
	private void setLabels()
	{
		if (campaign == null || campaign.missions == null)
		{
			return;
		}
		
		string state = campaign.state;
		
		if (showingRewards && campaign.missions.Count > campaign.completedEventIndex && campaign.completedEventIndex >= 0)
		{
			DialogData dialogData = campaign.missions[campaign.completedEventIndex].getDialogByState(ChallengeCampaign.IN_PROGRESS);

			if (dialogData != null)
			{
				if (dialogData.titleText != null)
				{
					titleLabel.text = dialogData.titleText.ToUpper();
				}

				if (dialogData.description != null)
				{
					descriptionLabel.text = dialogData.description;
				}
			}

			state = ChallengeCampaign.COMPLETE;
		}
		else if (campaign.currentMission != null)
		{
			titleLabel.text = campaign.currentMission.getDialogByState(campaign.state).titleText.ToUpper();
			descriptionLabel.text = campaign.currentMission.getDialogByState(campaign.state).description;
		}
        
		switch (state)
		{
			case ChallengeCampaign.IN_PROGRESS:
				timerLabel.text = Localize.text("ends_in");
				if (campaign.timerRange != null)
				{
					campaign.timerRange.registerLabel(timerLabel, keepCurrentText:true);
				}
				break;
			case ChallengeCampaign.COMPLETE:
				timerLabel.text = Localize.textUpper(ChallengeCampaign.challengeCompleteLocalization);
				break;
			case ChallengeCampaign.INCOMPLETE:
				timerLabel.text = Localize.textUpper("challenge_ended");
				break;
			default:
				break;
		}
	}

	private void setTypeBlocks()
	{
		// Instantiate type blocks.
		List<Objective> objectivesToShow = null;

		if (showingRewards)
		{
			if (campaign.missions.Count > campaign.completedEventIndex && campaign.completedEventIndex >= 0)
			{
				objectivesToShow = campaign.missions[campaign.completedEventIndex].objectives;
			}
		}
		else
		{
			objectivesToShow = campaign.currentMission.objectives;
		}

		if (objectivesToShow != null)
		{
			int typeCount = objectivesToShow.Count;
			int gameImageOffset = showingRewards ? 2 : 1;

			if (typeCount > 3)
			{
				Debug.LogErrorFormat("RobustChallengesObjectivesDialog.cs -- setTypeBlocks() -- More than three objectives, this is not supported by UI and will look bad. ZAP will also not function properly");
			}
			for (int i = 0; i < typeCount; i++)
			{
				Objective objective = objectivesToShow[i];
				GameObject typeBlockObject = NGUITools.AddChild(objectivesGrid.gameObject, sampleBlock.gameObject);
				RobustChallengesTypeBlock typeBlock = typeBlockObject.GetComponent<RobustChallengesTypeBlock>();
				downloadedTextureToRenderer(typeBlock.gameIconRenderer, i + gameImageOffset);
				bool isFinalCompletedObjective =
					(campaign.lastCompletedIndices != null && campaign.lastCompletedIndices.Contains(i));
				typeBlock.init(objective, isFinalCompletedObjective);
				allObjectives.Add(typeBlock);
			}
		}

		objectivesGrid.reposition();
	}

	private void setRewards()
	{
		List<MissionReward> rewardsToShow = new List<MissionReward>();
		if (showingRewards)
		{
			if (campaign.lastCompletedMissionRewards != null)
			{
				rewardsToShow = campaign.lastCompletedMissionRewards;
			}
		}
		else
		{
			if (campaign.currentMission.rewards != null)
			{
				rewardsToShow = campaign.currentMission.rewards;
			}
		}

		SafeSet.gameObjectActive(multipleRewardsParent, rewardsToShow.Count > 1);
		SafeSet.gameObjectActive(singleRewardParent, rewardsToShow.Count == 1);

		int skippedRewards = 0;
		for (int i = 0; i < rewardsToShow.Count; i++)
		{
			MissionReward missionReward = rewardsToShow[i];
			if (missionReward.isCardPackReward() && !Collectables.isActive())
			{
				skippedRewards++;
				SafeSet.gameObjectActive(multipleRewardsParent, rewardsToShow.Count - skippedRewards > 1);
				SafeSet.gameObjectActive(singleRewardParent, rewardsToShow.Count - skippedRewards == 1);
				continue;
			}

			// Show / Hide loot box - The reward could be a loot box, or could be coins
			displayLootBox(missionReward);

			if (showingRewards)
			{
				if (missionReward.type == MissionReward.RewardType.CREDITS)
				{
					long missionCoinsWon = missionReward.amount;
					if (missionCoinsWon > 0)
					{
						coinsWon += missionCoinsWon;
					}
				}
			}

			string missionRewardPrefabName = getPrefabName(missionReward.type);

			if (missionReward.isCardPackReward())
			{
				missionRewardPrefabName = "card_packs"; //Use the same prefab for various reward types that still just award card packs
			}
			string prefabPath = string.Format(REWARD_PREFAB_PATH, missionRewardPrefabName);

			AssetBundleManager.load(this, prefabPath, rewardPrefabLoadSuccess, rewardPrefabLoadFailed, Dict.create(D.DATA, missionReward));

			if (showingRewards)
			{
				if (missionReward.type == MissionReward.RewardType.LOOT_BOX)
				{
					prefabPath = REWARD_ITEM_PREFAB_PATH;
				}
				else
				{
					prefabPath += " Completed";
				}
				AssetBundleManager.load(this, prefabPath, rewardPrefabLoadSuccess, rewardPrefabLoadFailed, Dict.create(D.DATA, missionReward, D.OPTION, true));
				campaign.playAudio("HiCollect");
			}
		}
	}

	private string getPrefabName(ChallengeReward.RewardType type)
	{
		switch (type)
		{
			case ChallengeReward.RewardType.XP:
				return "xp";
			
			case ChallengeReward.RewardType.CHEST:
				return "chest";
			
			case ChallengeReward.RewardType.LEVELS:
				return "level";
			
			case ChallengeReward.RewardType.CARD_PACKS:
				return "card_packs";
			
			case ChallengeReward.RewardType.VIP_POINTS:
				return "vip_point";
			
			case ChallengeReward.RewardType.CREDITS:
				return "credits";
			
			case ChallengeReward.RewardType.SLOTVENTURE_CARD_PACK:
				return "slotventures_card_packs";
			
			case ChallengeReward.RewardType.GAME_UNLOCK:
				return "game_unlock";
			
			default:
				return "";
		}
	}

	private void rewardPrefabLoadSuccess(string assetPath, Object obj, Dict data = null)
	{
		if (this == null)
		{
			return;
		}
		
		bool isCompletedPrefab = (bool)data.getWithDefault(D.OPTION, false);
		UICenteredGrid currentGrid = !isCompletedPrefab ? inProgressRewardGrid : completedRewardGrid;
		GameObject rewardObject = NGUITools.AddChild(currentGrid.gameObject, obj as GameObject);
		RobustChallengesRewardIcon rewardIcon = rewardObject.GetComponent<RobustChallengesRewardIcon>();
		MissionReward reward = (MissionReward)data.getWithDefault(D.DATA, null);

		if (rewardIcon != null)
		{
			if (isCompletedPrefab)
			{
				allRewards.Add(rewardIcon);
			}
			
			rewardIcon.init(reward);
		}

		rewardBar rewardBar = rewardObject.GetComponent<rewardBar>();
		if (rewardBar != null)
		{
			rewardBar.showJackPot(reward);
		}

		currentGrid.reposition();
	}

	private void rewardPrefabLoadFailed(string assetPath, Dict data = null)
	{
		Bugsnag.LeaveBreadcrumb("Failed to load reward prefab at: " + assetPath);
	}
	
	public override void onCloseButtonClicked(Dict args = null)
	{
		if (showingRewards)
		{
			closeButtonHandler.clearAllDelegates();
			collectButton.clearAllDelegates();
			StatsManager.Instance.LogCount("dialog", "robust_challenges_complete", CampaignDirector.robust.variant, GameState.game != null ? GameState.game.keyName : "", (campaign.completedEventIndex + 1).ToString(), "click");
			float waitTime = 0.0f;
			for (int i = 0; i < allRewards.Count; i++)
			{
				waitTime = Mathf.Max(allRewards[i].onCollect(), waitTime);
			}
			StartCoroutine(waitForFinalAnimationsAndRollups(waitTime));

			campaign.playAudio("CollectButtonConfirm");
		}
		else
		{
			StatsManager.Instance.LogCount("dialog", "robust_challenges_motd", CampaignDirector.robust.variant, GameState.game != null ? GameState.game.keyName : "", (campaign.currentEventIndex + 1).ToString(), "close");
			Dialog.close();
		}
	}

	private IEnumerator waitForFinalAnimationsAndRollups(float animationWaitTime)
	{
		// coinsWon might be 0 if reward type is not credit
		if (coinsWon > 0)
		{
			SlotsPlayer.addFeatureCredits(coinsWon, "robustChallengeObjective");
			yield return new WaitForSeconds(animationWaitTime);
		}

		Dialog.close();
	}

	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		campaign.playAudio(DialogAudioPack.CLOSE);
		
		campaign.timerRange.removeLabel(timerLabel);
		lootboxRewardCheck();
		dropPackCheck();

		if (showingRewards)
		{
			if (campaign.completedEventIndex != campaign.missions.Count-1)
			{
				RobustChallengesObjectivesDialog.showDialog(); //Queue up the dialog to show the new challenges
			}
			else if (campaign.canRestart())
			{
				RobustChallengesPlayAgainDialog.showDialog();
			}
			else if (campaign.isEUEChallenges)
			{
				CustomPlayerData.setValue("ftue_challenges", false);
				RateMe.checkAndPrompt(RateMe.RateMeTrigger.MISC, true);
			}
			
			campaign.lastCompletedMissionRewards = null;
			campaign.lastCompletedIndices = null;
			campaign.completedEventIndex = -1;
		}
	}

	private static string inProgressBackgroundImagePath
	{
		// If the path is emtpty, return the default image path.
		get
		{
			string path = null;
			DialogData dialogData = campaign.completedEventIndex >= 0 ? campaign.missions[campaign.completedEventIndex].getDialogByState(ChallengeCampaign.IN_PROGRESS) : campaign.currentMission.getDialogByState(campaign.state);
			if (dialogData != null)
			{
				path = dialogData.backgroundImageURL;
			}
			if (string.IsNullOrEmpty(path))
			{
				path = DEFAULT_IMAGE_PATH;
			}
			else
			{
				path = "challenges/" + path;
			}
			return path;
		}
	}

	private static string completeBackgroundImagePath
	{
		// If the path is emtpty, return the default image path.
		get
		{
			string path = null;
			DialogData dialogData = campaign.missions[campaign.completedEventIndex].getDialogByState(ChallengeCampaign.COMPLETE);
			if (dialogData != null)
			{
				path = dialogData.backgroundImageURL;
			}
			if (string.IsNullOrEmpty(path))
			{
				path = DEFAULT_IMAGE_PATH;
			}
			else
			{
				path = "challenges/" + path;
			}
			return path;
		}
	}

	// Fetch latest progress data before showing up the dialog.
	public static bool showDialog(string motdKey = "", Dict completedArgs = null)
	{
		if (campaign != null && !campaign.isEnabled)
		{
			Debug.LogError("Robust Challenges data isn't enabled!");
			return false;
		}
		
		tempMotdKey = motdKey;

		if (completedArgs != null)
		{
			IEnumerable<int> lastCompletedIndices = (int[])completedArgs.getWithDefault(D.DATA, null);
			if (lastCompletedIndices != null)
			{
				campaign.lastCompletedIndices = new List<int>(lastCompletedIndices);
			}

			campaign.completedEventIndex = (int)completedArgs.getWithDefault(D.INDEX, -1);
			campaign.lastCompletedMissionRewards = (List<MissionReward>)completedArgs.getWithDefault(D.AMOUNT, null);
			showDialogHelper(false);
		}
		else
		{
			showDialogHelper();
		}
	
		return true;
	}

	public static void onProgressUpdate(JSON response = null)
	{
		if (campaign == null || campaign.currentMission == null || campaign.currentMission.rewards == null)
		{
			return;
		}
		Bugsnag.LeaveBreadcrumb("RobustChallengesMOTDshowDialog - Campaign found! Gathering data");

		List<string> urls = new List<string>();
		List<string> gameOptionImages = new List<string>();
		urls.Add(inProgressBackgroundImagePath);
		bool showingRewardsDialog = campaign.completedEventIndex >= 0;

		if (showingRewardsDialog)
		{
			urls.Add(completeBackgroundImagePath);
		}

		bool isTopOfList = true;
		List<Objective> objectivesToShow = showingRewardsDialog ? campaign.missions[campaign.completedEventIndex].objectives : campaign.currentMission.objectives;
		for (int i = 0; i < objectivesToShow.Count; i++)
		{
			LobbyGame gameInfo = null;
			string gameKey = objectivesToShow[i].game;
			if (!string.IsNullOrEmpty(gameKey))
			{
				gameInfo = LobbyGame.find(gameKey);
				if (gameInfo != null)
				{
					gameOptionImages.Add(SlotResourceMap.getLobbyImagePath(gameInfo.groupInfo.keyName, gameInfo.keyName));
				}
				else
				{
					Debug.LogError("Game " + gameKey + " doesn't exist!");
					return;
				}
			}
			else if (objectivesToShow[i].type == Objective.PACKS_COLLECTED || objectivesToShow[i].type == Objective.CARDS_COLLECTED)
			{
				gameOptionImages.Add("robust_challenges/collections pack challenge card 1x1");
				if (campaign.lastCompletedIndices != null && campaign.lastCompletedIndices.Contains(i))
				{
					isTopOfList = false; //Don't force the challenges dialog to the top if we will be showing a card pack being won for an objective
				}
			}
			else if (Objective.addGameOptionImage(objectivesToShow[i], gameOptionImages))
			{
				//Objective.addGameOptionImage will load a new path into gameOptionImages if a match was found, otherwise it will return false.
			}
			else
			{
				gameOptionImages.Add("robust_challenges/generic hir win challenge card 1x1");
			}
		}
		List<MissionReward> rewardsToShow = showingRewardsDialog ? campaign.missions[campaign.completedEventIndex].rewards : campaign.currentMission.rewards;
		// Add game icons in unlock reward into download list.
		for (int i = 0; i < rewardsToShow.Count; i++)
		{
			MissionReward reward = rewardsToShow[i];
			if (reward.type == MissionReward.RewardType.GAME_UNLOCK)
			{
				string gameKey = reward.game;
				LobbyGame gameInfo = null;

				if (!string.IsNullOrEmpty(gameKey))
				{
					// grab the lobby info for this game
					gameInfo = LobbyGame.find(gameKey);
					if (gameInfo != null)
					{
						gameOptionImages.Add(SlotResourceMap.getLobbyImagePath(gameInfo.groupInfo.keyName, gameInfo.keyName));
					}
					else
					{
						Debug.LogError("Game " + gameKey + " doesn't exist!");
						return;
					}
				}
				else
				{
					Debug.LogError("Robust Challenges: " + gameKey + "doesn't exist!!!");
					return;
				}
			}
		}
		
		Bugsnag.LeaveBreadcrumb("RobustChallengesMOTDshowDialog - Got all of the reward data! Showing MOTD");

		if (!string.IsNullOrEmpty(tempMotdKey))
		{
			Dialog.instance.showDialogAfterDownloadingTextures("robust_challenges_motd", urls.ToArray(), Dict.create(D.MOTD_KEY, tempMotdKey, D.MODE, showingRewardsDialog), nonMappedBundledTextures:gameOptionImages.ToArray());
		}
		else
		{
			isTopOfList = isTopOfList && response == null;
			Dialog.instance.showDialogAfterDownloadingTextures("robust_challenges_motd", urls.ToArray(), Dict.create(D.MODE, showingRewardsDialog, D.IS_TOP_OF_LIST, isTopOfList), nonMappedBundledTextures:gameOptionImages.ToArray());
		}
	}

	public static void showDialogHelper(bool updateProgressBeforeSurface = true)
	{
		if (campaign == null)
		{
			// Do not show this if the campaign isn't ready
			Debug.LogError("RobustRichesMOTD::showDialogHelper - Campaign was null");
			return;
		}

		if (updateProgressBeforeSurface)
		{
			// Request to update progress data.
			CampaignDirector.getProgress(campaign.campaignID, onProgressUpdate);
		}
		else
		{
			onProgressUpdate();
		}
	}

	public void lootboxRewardCheck()
	{
		// Display Loot box reward dialog if we have one
		if (LootBoxFeature.instance != null)
		{
			LootBoxFeature.instance.showLootBoxRewardDialog(LootBoxFeature.SOURCE_CHALLENGE);
		}
	}
    
	public void dropPackCheck()
	{
		if (campaign != null && campaign.packDropData.Count > 0)
		{
			for (int i = 0; i < campaign.packDropData.Count; i++)
			{
				Collectables.claimPackDropNow(campaign.packDropData[i]);
			}
		    
			campaign.packDropData.Clear();
		}
	}

	private void displayLootBox(MissionReward missionReward)
	{
		if (missionReward == null)
		{
			return;
		}
		
		bool isLootBoxReward = (missionReward.type == MissionReward.RewardType.LOOT_BOX);
		// Show/Hide loot box specific award label
		if (awardLabel != null)
		{
			awardLabel.gameObject.SetActive(isLootBoxReward);
		}

		if (isLootBoxReward && rewardBar != null)
		{
			rewardBar.showJackPot(missionReward);
		}
	}
	
	/*=========================================================================================
	GETTERS
	=========================================================================================*/
	
	// Shortcut getter.
    public static RobustCampaign campaign
    {
        get
        {
			return CampaignDirector.robust;
        }
    }
}
