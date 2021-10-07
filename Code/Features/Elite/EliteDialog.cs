using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Com.Scheduler;
using TMPro;


public class EliteDialog : DialogBase
{
	[SerializeField] private ObjectSwapper tabContentSwapper;
	[SerializeField] private ObjectSwapper rewardTabButtonSwapper;
	[SerializeField] private ObjectSwapper pointsTabButtonSwapper;
	[SerializeField] private ObjectSwapper topContentSwapper;
	[SerializeField] private ObjectSwapper secondaryMeterSwapper;
	[SerializeField] private TextMeshPro titleLabel;
	[SerializeField] private TextMeshPro lockedPointsLabel;
	[SerializeField] private TextMeshPro unlockingPointsLabel;
	[SerializeField] private TextMeshPro unlockedPointsLabel;
	[SerializeField] private TextMeshPro lockedDurationLabel;
	[SerializeField] private TextMeshPro unlockedDurationLabel;
	[SerializeField] private TextMeshPro unlockedMoreDurationLabel;
	[SerializeField] private UIMeterNGUI progressMeter;
	[SerializeField] private UIMeterNGUI secondaryProgressMeter;
	[SerializeField] private ButtonHandler rewardsButton;
	[SerializeField] private ButtonHandler pointsButton;
	[SerializeField] private ButtonHandler infoButton;
	[SerializeField] private UICenteredGrid rewardsGrid;
	[SerializeField] private UIGrid pointsGrid;
	[SerializeField] private GameObject rewardsListItemPrefab;
	[SerializeField] private GameObject pointsListItemPrefab;
	[SerializeField] private AnimationListController.AnimationInformationList lockAnimInfo;
	[SerializeField] private AnimationListController.AnimationInformationList unlockAnimInfo;
	[SerializeField] private AnimationListController.AnimationInformationList unlockedAnimInfo;
	[SerializeField] private SlideController slideController;
	

	private const string LOCKED_STATE = "locked";
	private const string UNLOCKED_STATE = "unlocked";
	private const string REWARDS_TAB_STATE = "rewards_tab_state";
	private const string POINTS_TAB_STATE = "points_tab_state";
	private const string METER_OVERFLOW_POINTS = "overflow_coins_meter_fill"; 
	private const string TAB_ON = "on";
	private const string TAB_OFF = "off";
	private const int MIN_REWARDS = 3;
	private const string DIALOG_OPEN = "DialogueOpenElite";
	private bool displayEliteGoldDialogOnClose = false;
	
	public const string DIALOG_KEY = "elite_main_dialog";

	public delegate void onActionButtonClickDelegate();
	
	public override void init()
	{
		registerHandlers();
		showRewardsTab();

		if (!EliteManager.hasActivePass)
		{
			setState(LOCKED_STATE);
		}
		else
		{
			setState(UNLOCKED_STATE);
		}
		displayEliteGoldDialogOnClose = (bool)dialogArgs.getWithDefault(D.CUSTOM_INPUT, false);
		Audio.play(DIALOG_OPEN);
	}

	public override void close()
	{
		if (displayEliteGoldDialogOnClose)
		{
			displayEliteGoldDialogOnClose = false;
			RichPassUpgradeToGoldDialog.showDialog("gold_game", SchedulerPriority.PriorityType.IMMEDIATE,true);

		}
	}

	private void registerHandlers()
	{
		rewardsButton.registerEventDelegate(onRewardsButtonPressed);
		pointsButton.registerEventDelegate(onPointsButtonPressed);
		infoButton.registerEventDelegate(infoButtonPressed);
	}

	private void setState(string state)
	{
		int durationInDays = EliteManager.passDuration / Common.SECONDS_PER_DAY;
		switch (state)
		{
			case LOCKED_STATE:
				topContentSwapper.setState(LOCKED_STATE);
				titleLabel.text = Localize.text("elite_title_locked");
				lockedPointsLabel.text = string.Format("{0}/{1} Points", CommonText.formatNumber(EliteManager.points), 
					CommonText.formatNumber(EliteManager.targetPoints));
				lockedDurationLabel.text = string.Format("{0} Days Access", durationInDays);
				progressMeter.setState(EliteManager.points, EliteManager.targetPoints);
				StartCoroutine(AnimationListController.playListOfAnimationInformation(lockAnimInfo));
				break;
			case UNLOCKED_STATE:
				setupUnlockedTimers();
				string formattedPoints = CommonText.formatNumber(EliteManager.points);
				string formattedTotal = CommonText.formatNumber(EliteManager.targetPoints);
				titleLabel.text = Localize.text("elite_title_unlocked");
				lockedPointsLabel.text = string.Format("{0}/{1} Points", formattedPoints, formattedTotal);
				if (EliteManager.passes == 1)
				{
					unlockedPointsLabel.text = string.Format("{0}/{1} Points", formattedPoints, formattedTotal);
					secondaryProgressMeter.setState(EliteManager.points, EliteManager.targetPoints);
				}
				else
				{
					unlockedPointsLabel.text = string.Format("{0} Points", formattedPoints);
					secondaryProgressMeter.setState(EliteManager.targetPoints, EliteManager.targetPoints);
					secondaryMeterSwapper.setState(METER_OVERFLOW_POINTS);
				}
				
				if (EliteManager.showEliteUnlocked)
				{
					//we want to show the points filled while unlocking
					unlockingPointsLabel.text = string.Format("{0}/{1} Points", formattedTotal, formattedTotal);
					//This needs to be shown while doing the unlock animation
					lockedDurationLabel.text = string.Format("{0} Days Access", durationInDays);
					StartCoroutine(playUnlockRoutine());
					EliteManager.showEliteUnlocked = false;
				}
				else
				{
					StartCoroutine(AnimationListController.playListOfAnimationInformation(unlockedAnimInfo));
					topContentSwapper.setState(UNLOCKED_STATE);
				}
				break;
		}
	}

	private void setupUnlockedTimers()
	{
		int timeRemainingInDays = EliteManager.timeRemainingInDays;
		if (timeRemainingInDays > 1)
		{
			unlockedDurationLabel.text = Localize.text("elite_days_access", timeRemainingInDays);
		}
		else
		{
			EliteManager.expirationTimer.registerLabel(unlockedDurationLabel, GameTimerRange.TimeFormat.REMAINING_HMS_FORMAT);
		}

		if (EliteManager.passes == 1)
		{
			unlockedMoreDurationLabel.text = Localize.text("elite_dialog_more_access", EliteManager.passDuration/Common.SECONDS_PER_DAY);
		}
		else
		{
			unlockedMoreDurationLabel.text = Localize.text("elite_dialog_extra_points");
		}
	}

	private void onRewardsButtonPressed(Dict args = null)
	{
		showRewardsTab();
	}
	
	private void onPointsButtonPressed(Dict args = null)
	{
		showPointsTab();
	}
	
	private void actionButtonCallback()
	{
		Dialog.close(this);
	}

	private void infoButtonPressed(Dict data = null)
	{
		DoSomething.now("elite_ftue:question_mark");
	}

	private IEnumerator playUnlockRoutine()
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(unlockAnimInfo));
		topContentSwapper.setState(UNLOCKED_STATE);
		if (EliteManager.passes == 1)
		{
			secondaryProgressMeter.setState(EliteManager.points, EliteManager.targetPoints);
		}
		else
		{
			secondaryProgressMeter.setState(EliteManager.targetPoints, EliteManager.targetPoints);
			secondaryMeterSwapper.setState(METER_OVERFLOW_POINTS);
		}
	}
	
	private void showRewardsTab()
	{
		Audio.play("ButtonConfirm");
		StatsElite.logOpenTab("elite_rewards");
		
		rewardTabButtonSwapper.setState(TAB_ON);
		pointsTabButtonSwapper.setState(TAB_OFF);
		tabContentSwapper.setState(REWARDS_TAB_STATE);
		if (rewardsGrid.transform.childCount == 0)
		{
		    
		    for (int i = 0; i < 3; i++)
			{
				GameObject rewardGO = NGUITools.AddChild(rewardsGrid.transform, rewardsListItemPrefab);
				if (rewardGO != null)
				{
					EliteRewardListItem reward = rewardGO.GetComponent<EliteRewardListItem>();
					reward.setup(i, actionButtonCallback);
				}
			}

			rewardsGrid.reposition();
		}

		if (rewardsGrid.transform.childCount <= MIN_REWARDS)
		{
			slideController.enabled = false;
			slideController.toggleScrollBar(false);			
		}
	}

	private void showPointsTab()
	{
		Audio.play("ButtonConfirm");
		StatsElite.logOpenTab("elite_points");
		
		pointsTabButtonSwapper.setState(TAB_ON);
		rewardTabButtonSwapper.setState(TAB_OFF);
		tabContentSwapper.setState(POINTS_TAB_STATE);

		if (pointsGrid.transform.childCount == 0)
		{
			addPointsItem(ElitePointsDisplayType.Coins);
			addPointsItem(ElitePointsDisplayType.RichPass);
			addPointsItem(ElitePointsDisplayType.Spins);
			
			pointsGrid.repositionNow = true;
		}
		
		if (pointsGrid.transform.childCount <= MIN_REWARDS)
		{
			slideController.enabled = false;
			slideController.scrollBar.enabled = false;
		}
	}

	private void addPointsItem(ElitePointsDisplayType itemType)
	{
		if (itemType == ElitePointsDisplayType.RichPass &&
		    (CampaignDirector.richPass == null || !CampaignDirector.richPass.isActive))
		{
			return;
		}
		
		GameObject pointsGO = NGUITools.AddChild(pointsGrid.transform, pointsListItemPrefab);
		if (pointsGO != null)
		{
			ElitePointsListItem pointsItem = pointsGO.GetComponent<ElitePointsListItem>();
			pointsItem.setup(itemType, actionButtonCallback);
		}
	}
	
	
	/*=========================================================================================
	SHOW DIALOG CALL
	=========================================================================================*/
	public static void showDialog(Dict args = null, SchedulerPriority.PriorityType priority = SchedulerPriority.PriorityType.HIGH)
	{
		Scheduler.addDialog(DIALOG_KEY, args, priority);
	}
}
