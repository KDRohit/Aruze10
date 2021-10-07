using UnityEngine;
using System.Collections;
using Com.Scheduler;
using TMPro;

public class ElitePassBottomButton : BottomOverlayButton
{
	[SerializeField] private TextMeshPro pointsLabel;
	[SerializeField] private UIMeterNGUI meter;
	[SerializeField] private GameObject meterParent;
	// =============================
	// CONST
	// =============================
	private const string POINTS_LOC = "elite_points_meter_{0}_{1}";
	private const string DAYS_LOC = "elite_days_left";

	protected override void Awake()
	{
		base.Awake();
		if (ExperimentWrapper.EUEFeatureUnlocks.isInExperiment)
		{
			sortIndex = 4;
		}
		else
		{
			sortIndex = 1;
		}
		init();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		if (EliteManager.expirationTimer != null)
		{
			EliteManager.expirationTimer.removeFunction(onExpired);
			EliteManager.expirationTimer.removeLabel(pointsLabel);
		}
		EliteManager.onElitePointsGranted -= handleElitePointsGranted;
	}

	protected override void init()
	{
		clickHandler.registerEventDelegate(onClick);
		EliteManager.onElitePointsGranted += handleElitePointsGranted;
		hasViewedFeature = (unlockData == null) || unlockData.featureSeen;
		updateState();
	}

	//Update the label and progress bar 
	private void updateState()
	{
		if (EliteManager.isLevelLocked)
		{
			meterParent.SetActive(false); //Turn this off since we don't have data for actual points needed
			initLevelLock(false);
			return;
		}

		if (!ExperimentWrapper.ElitePass.isInExperiment)
		{
			meterParent.SetActive(false); //Turn this off since we don't have data for actual points needed
			toolTipController.setLockedText("coming_soon");
		}
		
		if (needsToShowUnlockAnimation())
		{
			showUnlockAnimation();
		}
		else if (needsToForceShowFeature())
		{
			onClick(Dict.create(D.OPTION, true));
		}
		else
		{
			toolTipController.toggleNewBadge(!hasViewedFeature);
		}
		
		if (EliteManager.hasActivePass)
		{
			meter.setState(0,EliteManager.targetPoints);
			int timeRemainingInDays = EliteManager.timeRemainingInDays;
			if (timeRemainingInDays > 1)
			{
				pointsLabel.text = Localize.text(DAYS_LOC, timeRemainingInDays);
			}
			else
			{
				EliteManager.expirationTimer.registerLabel(pointsLabel, GameTimerRange.TimeFormat.REMAINING_HMS_FORMAT);
				EliteManager.expirationTimer.registerFunction(onExpired);
			}
		}
		else
		{
			meter.setState(EliteManager.points, EliteManager.targetPoints);
			pointsLabel.text = Localize.text(POINTS_LOC, EliteManager.points, + EliteManager.targetPoints);
		}
	}

	private void handleElitePointsGranted()
	{
		updateState();
	}

	protected void onExpired(Dict args = null, GameTimerRange originalTimer = null)
	{
		pointsLabel.text = Localize.text(POINTS_LOC, EliteManager.points, EliteManager.targetPoints);
		meter.setState(EliteManager.points, EliteManager.targetPoints);
	}

	protected override void onClick(Dict args = null)
	{
		if (EliteManager.isLevelLocked)
		{
			logLockedClick();
			StartCoroutine(toolTipController.playLockedTooltip());
		}
		else if (!ExperimentWrapper.ElitePass.isInExperiment)
		{
			logComingSoonClick();
			StartCoroutine(toolTipController.playLockedTooltip());
		}
		else
		{
			if (!hasViewedFeature)
			{
				logFirstTimeFeatureEntry(args);
				EliteManager.showVideo();
				markFeatureSeen();
			}
			
			showLoadingTooltip(EliteDialog.DIALOG_KEY);
			EliteDialog.showDialog();
			StatsElite.logEliteButtonClicked();
		}
	}
}