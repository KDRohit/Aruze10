using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

public class LevelUpSummaryDialog : DialogBase
{	
	[SerializeField] private GameObject summaryScreenPrefab;
	[SerializeField] private UIAnchor summaryScreenAnchor;

	private LevelUpSummaryScreen summaryScreen;

	private const float normalAnimLength = 1.833f;
	private const float specialAnimLength = 2.150f;

	public const float normalOutroAnimLength = 3.500f;
	public const float specialOutroAnimLength = 3.500f;

	public override void init()
	{
		int currentLevel = SlotsPlayer.instance.socialMember.experienceLevel;
		ExperienceLevelData curLevel = ExperienceLevelData.find(currentLevel);

		if (curLevel == null)
		{
			// Hopefully this will NEVER happen.
			Debug.LogError("ExperienceLevelData not found for level " + currentLevel);
			return;
		}

		long creditsAwarded = curLevel.bonusAmt + curLevel.levelUpBonusAmount;
		int vipPointsAwarded = curLevel.bonusVIPPoints * curLevel.vipMultiplier;

		bool isIncreasingInflation = SlotsPlayer.instance.currentBuyPageInflationPercentIncrease > 0;
		bool showNextLevelTeaser = false;
		if (!isIncreasingInflation)
		{
			showNextLevelTeaser = SlotsPlayer.instance.nextBuyPageInflationFactor > SlotsPlayer.instance.currentBuyPageInflationFactor;
		}

		GameObject screenObject = NGUITools.AddChild(sizer.gameObject, summaryScreenPrefab);
		summaryScreen = screenObject.GetComponent<LevelUpSummaryScreen>();
		summaryScreen.init(currentLevel, creditsAwarded, vipPointsAwarded, showNextLevelTeaser, null, this);

	}

	public override float animateIn()
	{
		bool animate = (bool)dialogArgs.getWithDefault(D.OPTION, true);
		bool isIncreasingInflation = SlotsPlayer.instance.currentBuyPageInflationPercentIncrease > 0;
		if (animate)
		{
			//animaiton is controlled in the init function (called in the same frame as this)
			return isIncreasingInflation ? specialAnimLength : normalAnimLength;
		}
		else
		{
			//fast forward intro animatinon to end state
			summaryScreen.summaryAnimator.Play(isIncreasingInflation ? "special intro" : "normal intro", -1, 1.0f);

			//play base dialog animation
			return base.animateIn();
		}

	}

	public override float animateOut()
	{
		if (summaryScreen != null)
		{
			bool animate = (bool)dialogArgs.getWithDefault(D.OPTION, true);
			if (animate)
			{
				return summaryScreen.animateOut();
			}
			else
			{
				return base.animateOut();
			}

		}

		return base.animateOut();
	}

	protected override void onFadeInComplete ()
	{
		base.onFadeInComplete ();
		for (int i = 0; i < summaryScreen.anchors.Length; i++)
		{			
			if (summaryScreen.anchors[i].gameObject.activeInHierarchy)
			{
				summaryScreenAnchor.dependentAnchors.Add(summaryScreen.anchors[i]);
			}
			
		}
		summaryScreenAnchor.enabled = true;
	}

	/// Show the flying coin and rollup before closing.
	public override void close()
	{

	}

	public static void showDialog()
	{
		Scheduler.addDialog("level_up_summary", Dict.create(D.OPTION, false));
	}
}
