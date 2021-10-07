using System.Collections;
using UnityEngine;
using Com.Scheduler;
using TMPro;

public class PostPurchaseChallengeDialog : DialogBase
{
	[SerializeField] private ObjectSwapper stateSwapper;
	[SerializeField] private ObjectSwapper coinStateSwapper;
	[SerializeField] private ObjectSwapper activeStateSwapper;
	[SerializeField] private TextMeshPro campaignTotalTimerLabel;
	[SerializeField] private TextMeshPro headerLabel;
	[SerializeField] private TextMeshPro subHeaderLabel;
	[SerializeField] private TextMeshPro buttonLabel;
	[SerializeField] private TextMeshPro durationLabel;
	[SerializeField] private TextMeshPro progressLabel;
	[SerializeField] private TextMeshPro creditsLabel;
	[SerializeField] private PostPurchaseChallengeProgressMeter progressMeter;
	[SerializeField] private GameObject itemContainer;
	[SerializeField] private GameObject buyMoreContainer;
	[SerializeField] private ButtonHandler spinButtonHandler;
	[SerializeField] private ButtonHandler buyButtonHandler;
	[SerializeField] private ClickHandler tapOpenClickHandler;
	[SerializeField] private BoxCollider itemContainerCollider;
	[SerializeField] private Animator collectCoinsAnim;

	[SerializeField] private AnimationListController.AnimationInformationList openAnimInfo;
	[SerializeField] private AnimationListController.AnimationInformationList burstAnimInfo;
	[SerializeField] private AnimationListController.AnimationInformationList coinWinTransitionAnimInfo;
	[SerializeField] private AnimationListController.AnimationInformationList coinLoseTransitionAnimInfo;

	// 
	//Dialog States
	private const string ACTIVE_STATE = "active";
	private const string INACTIVE_STATE = "inactive";
	private const string ACTIVE_IN_PROGRESS_STATE = "in_progress";
	private const string ACTIVE_WIN_STATE = "win";
	private const string ACTIVE_LOST_STATE = "lose";
	private const string ACTIVE_POST_COMPLETE_STATE = "post_complete";
	private const string COIN_WIN_STATE = "coin_stack_win";
	private const string COIN_LOSE_STATE = "coin_stack_lose";

	private bool showLostState;
	private PostPurchaseChallengeCampaign campaign;
	private PostPurchaseChallengeItem itemAnimator;
	private string soundDialogOpenInactive;
	private string soundDialogOpen;
	private string soundDialogClose;
	private string bgMusicDialogOpen;
	private string soundDialogWin;
	private string soundDialogWinCollect;
	private string soundTapToBreak;
	private string bgMusicDialogWin;
	
	public static void showDialog(
		SchedulerPriority.PriorityType priorityType = SchedulerPriority.PriorityType.HIGH,
		Dict args = null)
	{
		
		string campaignId = "";
		PostPurchaseChallengeCampaign campaign = PostPurchaseChallengeCampaign.getActivePostPurchaseChallengeCampaign();
		if (campaign != null)
		{
			campaignId = campaign.campaignID;
		}
		else if(args != null && args.containsKey(D.KEY))
		{
			campaignId = args[D.KEY].ToString();
		}

		if (!string.IsNullOrEmpty(campaignId))
		{
			Scheduler.addDialog(campaignId + "_dialog", args, priorityType);
		}
			
	}

	public override void init()
	{
		campaign = PostPurchaseChallengeCampaign.getActivePostPurchaseChallengeCampaign();
		
		if (dialogArgs != null && dialogArgs.containsKey(D.DATA))
		{
			showLostState = (bool)dialogArgs[D.DATA];
		}
		
		if (spinButtonHandler != null)
		{
			spinButtonHandler.registerEventDelegate(onButtonClicked);
		}

		if (buyButtonHandler != null)
		{
			buyButtonHandler.registerEventDelegate(onBuyButtonClicked);
		}

		if (tapOpenClickHandler != null)
		{
			tapOpenClickHandler.registerEventDelegate(onTapToOpen);
		}

		itemContainerCollider.enabled = false;
		
		initSounds();
		
		if (showLostState)
		{
			stateSwapper.setState(ACTIVE_STATE);
			activeStateSwapper.setState(ACTIVE_LOST_STATE);
			coinStateSwapper.setState(COIN_LOSE_STATE);
			setLabels(ACTIVE_LOST_STATE);
			StatsPostPurchaseChallenge.logDialogView(ACTIVE_LOST_STATE);
			Audio.play(soundDialogOpen);
			Audio.switchMusicKeyImmediate(bgMusicDialogOpen);
			int progress = (int)dialogArgs[D.SCORE];
			progressMeter.updateProgress(progress);
			progressLabel.text = string.Format("{0}% full", progress);
			int amount = (int) dialogArgs[D.AMOUNT];
			creditsLabel.text = CreditsEconomy.convertCredits(amount);
			StartCoroutine(AnimationListController.playListOfAnimationInformation(openAnimInfo));
			AssetBundleManager.load(this, string.Format(PostPurchaseChallengeCampaign.ITEM_BASE_PATH, ExperimentWrapper.PostPurchaseChallenge.theme), assetLoadSuccess, assetLoadFailed);
		}
		else if (campaign != null)
		{
			if (campaign.isActive && !campaign.isLocked && campaign.runningTimeRemaining > 0)
			{
				stateSwapper.setState(ACTIVE_STATE);
				if (!campaign.isComplete)
				{
					activeStateSwapper.setState(ACTIVE_IN_PROGRESS_STATE);
					setLabels(ACTIVE_IN_PROGRESS_STATE);
					StatsPostPurchaseChallenge.logDialogView(ACTIVE_STATE);
					Audio.play(soundDialogOpen);
					Audio.switchMusicKeyImmediate(bgMusicDialogOpen);
				}
				else
				{
					activeStateSwapper.setState(ACTIVE_WIN_STATE);
					coinStateSwapper.setState(COIN_WIN_STATE);
					setLabels(ACTIVE_WIN_STATE);
					itemContainerCollider.enabled = true;
					StatsPostPurchaseChallenge.logDialogView(ACTIVE_WIN_STATE);
					StatsPostPurchaseChallenge.logMilestone("post_purchase_complete", campaign.duration - campaign.runningTimeRemaining);
					Audio.play(soundDialogWin);
					Audio.switchMusicKeyImmediate(bgMusicDialogWin);
				}
			}
			else
			{
				stateSwapper.setState(INACTIVE_STATE);
				setLabels(INACTIVE_STATE);
				campaignTotalTimerLabel.text = "Ends In ";
				campaign.timerRange.registerLabel(campaignTotalTimerLabel, GameTimerRange.TimeFormat.REMAINING_HMS_FORMAT, true);
				StatsPostPurchaseChallenge.logDialogView(INACTIVE_STATE);
				Audio.play(soundDialogOpenInactive);
			}

			StartCoroutine(AnimationListController.playListOfAnimationInformation(openAnimInfo));
			
			buyMoreContainer.SetActive(false);
			if (campaign.isRunning)
			{
				campaign.registerRunningTimeLabel(durationLabel, GameTimerRange.TimeFormat.REMAINING_HMS_FORMAT);
				if (campaign.runningTimeRemaining < PostPurchaseChallengeCampaign.REMINDER_DURATION && activeStateSwapper.getCurrentState() == ACTIVE_IN_PROGRESS_STATE)
				{
					buyMoreContainer.SetActive(true);
				}
			}

			int progress = 0;
			if (dialogArgs != null && dialogArgs.containsKey(D.SCORE))
			{
				progress = (int)dialogArgs[D.SCORE];
				progressMeter.updateProgress(progress);
			}
			else
			{
				progress = campaign.getProgressPercent();
				progressMeter.updateProgress(campaign);
			}
			
			progressLabel.text = string.Format("{0}% full", progress);

			if (campaign.currentMission != null)
			{
				creditsLabel.text = CreditsEconomy.convertCredits(campaign.currentMission.getCreditsReward);
			}
			
			AssetBundleManager.load(this, string.Format(PostPurchaseChallengeCampaign.ITEM_BASE_PATH, ExperimentWrapper.PostPurchaseChallenge.theme), assetLoadSuccess, assetLoadFailed);
		}
	}

	private void initSounds()
	{
		string soundSuffix = ExperimentWrapper.PostPurchaseChallenge.theme;
		bgMusicDialogOpen = "IdlePPC" + soundSuffix;
		bgMusicDialogWin = "WinBgPPC" + soundSuffix;
		soundDialogOpenInactive = "MotdPPC" + soundSuffix;
		soundDialogOpen = "DialogueOpenPPC" + soundSuffix;
		soundDialogClose = "ButtonSubmitPPCCommon";
		soundDialogWin = "DialogueWinOpenPPC" + soundSuffix;
		soundDialogWinCollect = "CollectPPC" + soundSuffix;
		soundTapToBreak = "TapToBreakPPC" + soundSuffix;
	}

	private void onButtonClicked(Dict args = null)
	{
		//Close the dialog for now
		if (stateSwapper.getCurrentState() == INACTIVE_STATE)
		{
			BuyCreditsDialog.showDialog(priority:SchedulerPriority.PriorityType.IMMEDIATE);
			StatsPostPurchaseChallenge.logDialogClick("close");
			Audio.play("ButtonConfirm");
			Dialog.close(this);
		}
		else if (activeStateSwapper.getCurrentState() == ACTIVE_POST_COMPLETE_STATE)
		{
			spinButtonHandler.unregisterEventDelegate(onButtonClicked);
			
			if (showLostState)
			{
				int amount = (int) dialogArgs[D.AMOUNT];
				PostPurchaseChallengeCampaign.pendingLostData = null;
				RobustChallengesAction.sendLostSeenResponse(dialogArgs[D.EVENT_ID].ToString());
				SlotsPlayer.addFeatureCredits(amount, "postPurchaseLost_" + type);
				Audio.play(soundDialogClose);
			}
			else
			{
				foreach (MissionReward reward in campaign.currentMission.rewards)
				{
					reward.collect("postPurchase_" + reward.type);
				}
				Audio.play(soundDialogWinCollect);
			}
			
			StatsPostPurchaseChallenge.logDialogClick("collect_reward");
			if (campaign != null)
			{
				RobustChallengesAction.getCampaignRestartData(campaign.campaignID, onCampaignUpdate);
			}
			StartCoroutine(collectReward());
		}
		else
		{
			StatsPostPurchaseChallenge.logDialogClick("close");
			Audio.play(soundDialogClose);
			Dialog.close();
		}
	}

	private IEnumerator collectReward()
	{
		collectCoinsAnim.gameObject.SetActive(true);
		yield return StartCoroutine(CommonAnimation.playAnimAndWait(collectCoinsAnim, "explosion"));
		Dialog.close();
	}

	private void onCampaignUpdate(JSON data)
	{
		JSON clientData = data.getJSON("clientData");
		campaign.winAndReset(clientData);
	}

	private void onBuyButtonClicked(Dict args = null)
	{
		Dialog.close();
		OverlayTopHIRv2.instance.clickBuyCredits();
	}

	private void onTapToOpen(Dict args = null)
	{
		if (itemAnimator != null && activeStateSwapper.getCurrentState() == ACTIVE_WIN_STATE)
		{
			Audio.play(soundTapToBreak);
			Audio.switchMusicKeyImmediate(bgMusicDialogWin);
			StartCoroutine(playWinSequence());
		}
	}

	private IEnumerator playWinSequence()
	{
		yield return StartCoroutine(itemAnimator.playWinSequence());
		StartCoroutine(showCoinSequence());
	}

	private IEnumerator playLostSequence()
	{
		yield return StartCoroutine(itemAnimator.playLoseSequence());

		int amount = (int)dialogArgs[D.AMOUNT];
		if (amount > 0)
		{
			activeStateSwapper.setState(ACTIVE_POST_COMPLETE_STATE);
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(coinLoseTransitionAnimInfo));
		}
		else
		{
			RobustChallengesAction.sendLostSeenResponse(dialogArgs[D.EVENT_ID].ToString());
			if (campaign != null)
			{
				RobustChallengesAction.getCampaignRestartData(campaign.campaignID, onCampaignUpdate);
			}
			PostPurchaseChallengeCampaign.pendingLostData = null;
			Dialog.close(this);
		}
	}

	private IEnumerator showCoinSequence()
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(burstAnimInfo));
		activeStateSwapper.setState(ACTIVE_POST_COMPLETE_STATE);
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(coinWinTransitionAnimInfo));
	}

	private void setLabels(string state)
	{
		string theme = ExperimentWrapper.PostPurchaseChallenge.theme.ToLower();
		switch (state)
		{
			case INACTIVE_STATE:
				headerLabel.text = Localize.text("post_purchase_challenge_title", campaign.getPostPurchaseChallengeMaxBonus());
				subHeaderLabel.text = Localize.text(string.Format("post_purchase_challenge_{0}_inactive",theme));
				buttonLabel.text = Localize.text("shop_now");
				break;
			case ACTIVE_IN_PROGRESS_STATE:
				headerLabel.text = Localize.text(string.Format("post_purchase_challenge_{0}_active",theme));
				subHeaderLabel.text = "";
				buttonLabel.text = Localize.text("post_purchase_challenge_spin");
				break;
			case ACTIVE_WIN_STATE:
				headerLabel.text = Localize.text(string.Format("post_purchase_challenge_{0}_complete", theme));
				subHeaderLabel.text = "";
				buttonLabel.text = Localize.text("collect");
				break;
			case ACTIVE_LOST_STATE:
				headerLabel.text = Localize.text("post_purchase_challenge_lose");
				subHeaderLabel.text = "";
				buttonLabel.text = Localize.text("collect");
				break;
		}
	}

	private void assetLoadSuccess(string assetPath, Object obj, Dict data = null)
	{
		GameObject themedItemObject = NGUITools.AddChild(itemContainer, obj as GameObject);
		if (themedItemObject != null)
		{
			itemAnimator = themedItemObject.GetComponent<PostPurchaseChallengeItem>();
			if (itemAnimator != null) 
			{
				if (stateSwapper.getCurrentState() == INACTIVE_STATE)
				{
					itemAnimator.playIdleAnimation();
					return;
				}
				
				if (activeStateSwapper.getCurrentState() == ACTIVE_WIN_STATE)
				{
					itemAnimator.playPreWinAnimation();
				}
				else if(activeStateSwapper.getCurrentState() == ACTIVE_LOST_STATE)
				{
					StartCoroutine(playLostSequence());
				}
				else
				{
					itemAnimator.playIdleAnimation();
				}
			}
		}
	}

	private void assetLoadFailed(string assetPath, Dict data = null)
	{
		Bugsnag.LeaveBreadcrumb("Post Purchase Challenge Themed Asset failed to load: " + assetPath);
#if UNITY_EDITOR
		Debug.LogWarning("Post Purchase Challenge Themed Asset failed to load: " + assetPath);			
#endif
		//This is to handle an edge case wherein the player lost in the previous challenge and has some pending lost data
		//to be seen and reward to be collected but the player is in the holdout/control variant now.
		//Because of this the theme cannot be loaded and the dialog gets stuck since its dependent on some animations
		if (showLostState)
		{
			RobustChallengesAction.sendLostSeenResponse(dialogArgs[D.EVENT_ID].ToString());
			int amount = (int) dialogArgs[D.AMOUNT];
			if (amount > 0)
			{
				SlotsPlayer.addFeatureCredits(amount, "challengeLoadFail_" + type);
			}
			PostPurchaseChallengeCampaign.pendingLostData = null;
		}
		Dialog.close(this);
	}

	public override void close()
	{
		
	}

	public override void onCloseButtonClicked(Dict args = null)
	{
		StatsPostPurchaseChallenge.logDialogClick("close");
		if (stateSwapper.getCurrentState() == INACTIVE_STATE)
		{
			Audio.play("ButtonConfirm");
		}
		else
		{
			Audio.play(soundDialogClose);
		}
		base.onCloseButtonClicked();
	}

	public bool canBeForcedToClose()
	{
		string currState = activeStateSwapper.getCurrentState();
		
		// This class supports multiple types of post purchase challenge dialogs.  lost or win dialogs should not be
		// forced to close since they are adding coins (mission complete rewards or consolation rewards).
		// if they are closed before adding coins, there could be desync issues.
		return !currState.Equals(ACTIVE_LOST_STATE) && !currState.Equals(ACTIVE_WIN_STATE) &&
		        !currState.Equals(ACTIVE_POST_COMPLETE_STATE);
	}
}
