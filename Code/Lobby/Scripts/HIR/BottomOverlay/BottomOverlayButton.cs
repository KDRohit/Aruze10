using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BottomOverlayButton : MonoBehaviour, IResetGame
{
	public ButtonHandler clickHandler;
	[SerializeField] protected BottomOverlayButtonToolTipController toolTipController;
	[SerializeField] protected string featureKey;
	[SerializeField] private AdjustObjectColorsByFactor lockedColorAdjuster;
	[SerializeField] private AnimationListController.AnimationInformationList idleLockedAnimationList;
	[SerializeField] private AnimationListController.AnimationInformationList idleUnlockedAnimationList;

	// Enabling this will prevent the button from being added to the layout grid.
	// Use this for preventing clones from affecting the ui layout
	[SerializeField] private bool skipAddToOverlayGrid;

	public int sortIndex { get; protected set; }

	public static readonly List<BottomOverlayButton> globalList = new List<BottomOverlayButton>();
	
	protected bool hasViewedFeature = false;
	protected bool playingUnlockAnimation = false;
	protected FeatureUnlockData unlockData = null;

	protected virtual void Awake()
	{
		if (!globalList.Contains(this) && !skipAddToOverlayGrid)
		{
			globalList.Add(this);
		}
		getUnlockData();
	}

	protected virtual void getUnlockData()
	{
		if (ExperimentWrapper.EUEFeatureUnlocks.isInExperiment)
		{
			unlockData = EueFeatureUnlocks.getUnlockData(featureKey);
		}
	}

	protected virtual void OnDestroy()
	{
	}

	protected virtual void init()
	{
		if (clickHandler != null)
		{
			clickHandler.unregisterEventDelegate(onClick);
			clickHandler.registerEventDelegate(onClick);
		}
		
		hasViewedFeature = EueFeatureUnlocks.hasFeatureBeenSeen(featureKey);
	}

	protected virtual void onClick(Dict args = null)
	{
		//do stuff
	}

	protected virtual void initLevelLock(bool isBeingUnlocked)
	{
		int minLevel = EueFeatureUnlocks.getFeatureUnlockLevel(featureKey);
		string unlockType = unlockData != null ? unlockData.unlockType : EueFeatureUnlocks.getFeatureUnlockType(featureKey);

		switch (unlockType)
		{
			case "level":
				toolTipController.initLevelLock(minLevel, featureKey + "_unlock_level", minLevel);
				break;
			case "vip_level":
				toolTipController.initVipLock(minLevel, featureKey + "_unlock_level", VIPLevel.find(minLevel).name);
				break;
			case "pet_ftue":
				toolTipController.initMysteryLock(BottomOverlayButtonToolTipController.SPIN_TO_UNLOCK, null);
				break;
		}

		if (!isBeingUnlocked)
		{
			lockedColorAdjuster.multiplyColors();
			if (idleLockedAnimationList != null)
			{
				StartCoroutine(AnimationListController.playListOfAnimationInformation(idleLockedAnimationList));
			}
		}
	}

	protected void unlockFeature()
	{
		lockedColorAdjuster.restoreColors();
		if (idleUnlockedAnimationList != null && idleUnlockedAnimationList.Count > 0)
		{
			StartCoroutine(AnimationListController.playListOfAnimationInformation(idleUnlockedAnimationList));
		}
	}

	protected virtual void showLoadingTooltip(string dialogKey)
	{
		if (ExperimentWrapper.EUEFeatureUnlocks.isInExperiment)
		{
			Com.Scheduler.Scheduler.addTask(new BundleLoadingTask(Dict.create(D.OBJECT, toolTipController, D.KEY, dialogKey)), Com.Scheduler.SchedulerPriority.PriorityType.BLOCKING);
		}
	}

	protected void markFeatureSeen()
	{
		hasViewedFeature = true;
		toolTipController.toggleNewBadge(false);
		EueFeatureUnlocks.markFeatureSeen(featureKey);
	}

	protected void showUnlockAnimation()
	{
		playingUnlockAnimation = true;
		initLevelLock(true);
		unlockData.unlockAnimationSeen = true;
		Com.Scheduler.Scheduler.addTask(new FeatureUnlockTask(Dict.create(D.OBJECT, toolTipController, D.TITLE, "Unlock Animation Task")), Com.Scheduler.SchedulerPriority.PriorityType.BLOCKING);
	}

	protected virtual bool needsToShowUnlockAnimation()
	{
		return unlockData != null && unlockData.unlockedThisSession && !unlockData.unlockAnimationSeen;
	}

	protected virtual bool needsToForceShowFeature()
	{
		return unlockData != null && unlockData.isFeatureUnlocked() && !unlockData.unlockedThisSession && !unlockData.featureSeen;
	}

	public static void resetStaticClassData()
	{
		globalList.Clear();
	}

	protected void logLockedClick()
	{
		StatsManager.Instance.LogCount(
			counterName: "feature_lock",
			kingdom: featureKey,
			val:SlotsPlayer.instance.socialMember.experienceLevel
		);
	}
	
	protected void logComingSoonClick()
	{
		StatsManager.Instance.LogCount(
			counterName: "feature_coming_soon",
			kingdom: featureKey,
			val:SlotsPlayer.instance.socialMember.experienceLevel
		);
	}

	private void logFirstFeatureClick()
	{
		StatsManager.Instance.LogCount(
			counterName: "feature_unlock_icon_entry",
			kingdom: featureKey,
			val:SlotsPlayer.instance.socialMember.experienceLevel
		);
	}

	private void logForceEntryOnReload()
	{
		StatsManager.Instance.LogCount(
			counterName: "feature_unlock_reload_entry",
			kingdom: featureKey,
			val:SlotsPlayer.instance.socialMember.experienceLevel
		);
	}

	protected void logFirstTimeFeatureEntry(Dict args = null)
	{
		if (args != null && (bool)args.getWithDefault(D.OPTION, false))
		{
			logForceEntryOnReload();
		}
		else
		{
			logFirstFeatureClick();
		}
	}
}
