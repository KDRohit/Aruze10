using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using UnityEngine;
using TMPro;

public class LevelUpOverlay : MonoBehaviour
{
	[SerializeField] private Animator levelUpAnimator;

	[SerializeField] private GameObject starSequenceParent;
	[SerializeField] private GameObject summaryScreenParent;
	[SerializeField] private GameObject summaryScreenPrefab;

	[SerializeField] private ButtonHandler skipButton;

	[SerializeField] private AnimatedParticleEffect particleEffect;

	//Keep a running total fo these incase we get multiple level-ups together
	private long creditsAwarded = 0L;
	private int vipPointsAwarded = 0;
	private int newLevel = 0;

	private bool isIncreasingInflation = false;

	private LevelUpSummaryScreen summaryScreen;
	private bool isClosing = false;

	private string previousMusicKey;
	private RoyalRushCollectionModule rrMeter;
	
	public const string LEVEL_UP_OVERLAY_PATH = "Assets/Data/HIR/Bundles/Initialization/Features/Level Up V2/Level Up V2 Topbar Animation Parent";
	public const string LEVEL_UP_ANTICIPATION_PATH = "Assets/Data/HIR/Bundles/Initialization/Features/Level Up V2/Default Star Sizer Anticipation";

	public void startLevelUp()
	{
		if (Overlay.instance.topHIR.levelUpSequenceAnticipation != null)
		{
			Overlay.instance.topHIR.levelUpSequenceAnticipation.Play("Off");
		}
		
		if (ExperimentWrapper.RoyalRush.isPausingInLevelUps && SlotBaseGame.instance != null && SlotBaseGame.instance.isRoyalRushGame && SlotBaseGame.instance.tokenBar != null)
		{	
			rrMeter = SlotBaseGame.instance.tokenBar as RoyalRushCollectionModule;
			if (rrMeter != null && rrMeter.currentRushInfo.currentState == RoyalRushInfo.STATE.PAUSED)
			{
				rrMeter.pauseTimers();
			}
		}
		
		previousMusicKey = Audio.defaultMusicKey;
		skipButton.gameObject.SetActive(true);
		StartCoroutine(playLevelUpAnimations());
	}

	private IEnumerator playLevelUpAnimations()
	{
		skipButton.registerEventDelegate(skipClicked);

		bool showNextLevelTeaser = false;
		if (!isIncreasingInflation)
		{
			showNextLevelTeaser = SlotsPlayer.instance.nextBuyPageInflationFactor >
			                      SlotsPlayer.instance.currentBuyPageInflationFactor;
		}

		GameObject screenObject = NGUITools.AddChild(this.gameObject, summaryScreenPrefab);
		CommonGameObject.setLayerRecursively(screenObject, summaryScreenParent.layer);
		screenObject.transform.SetParent(summaryScreenParent.transform, false);
		
		summaryScreen = screenObject.GetComponent<LevelUpSummaryScreen>();
		summaryScreen.init(newLevel, creditsAwarded, vipPointsAwarded, showNextLevelTeaser, this);

		string animTypeName = isIncreasingInflation ? "special intro" : "normal intro";
		Audio.play("StarTravelLevelup");
		
		yield return new WaitForSeconds(0.1f); //Waiting briefly to allow anchors to setup to their correct position

		StartCoroutine(particleEffect.animateParticleEffect(particleEffect.translateStartTransform, particleEffect.translateEndTransform));
		TextMeshPro levelLabel = particleEffect.GetComponentInChildren<TextMeshPro>();
		if (levelLabel != null)
		{
			levelLabel.text = newLevel.ToString();
			levelLabel.gameObject.SetActive(true);
		}

		levelUpAnimator.Rebind();
		levelUpAnimator.Play(animTypeName);
		yield return new WaitForSeconds(1.2f);
		Audio.switchMusicKeyImmediate("FeatureBgLevelup");
		yield return new WaitForSeconds(1.3f);
		if (!isClosing)
		{
			summaryScreenParent.SetActive(true);
			summaryScreen.summaryAnimator.Play(animTypeName);
			yield return new WaitForSeconds(2.25f);
			starSequenceParent.SetActive(false);
			skipButton.gameObject.SetActive(false);
			if (ExperimentWrapper.RepriceLevelUpSequence.timeoutLength > 0)
			{
				GameTimerRange timeoutTimer = GameTimerRange.createWithTimeRemaining(ExperimentWrapper.RepriceLevelUpSequence.timeoutLength);
				timeoutTimer.registerFunction(summaryTimeout);
			}
		}
    }

	public void close()
	{
		isClosing = true;
		StartCoroutine(playOutroAnimations());
	}

	private IEnumerator playOutroAnimations()
	{
		SlotsPlayer.addCredits(creditsAwarded, "level up", false);
		Audio.play("CollectRewardTravelLevelup");
		Audio.playWithDelay("CollectRewardArriveLevelup", 2.5f);

		//wait for summary screen to finish animating
		float animTime = summaryScreen.animateOut();
		yield return new WaitForSeconds(animTime);
		
		if (SpinPanel.instance != null)
		{
			SpinPanel.instance.showFeatureUI(true);	
		}

		Overlay.instance.topHIR.levelUpSequence = null;

		//Check for dialogs since this was previously blocking the ToDo List
		if (GameState.game == null || (SlotBaseGame.instance != null && !SlotBaseGame.instance.isGameBusy))
		{
			Scheduler.run();
		}

		Destroy(this.gameObject);
		
		if (ExperimentWrapper.RoyalRush.isPausingInLevelUps && SlotBaseGame.instance != null && SlotBaseGame.instance.isRoyalRushGame)
		{
			if (rrMeter != null && rrMeter.currentRushInfo.currentState == RoyalRushInfo.STATE.PAUSED)
			{
				if (GameState.game != null)
				{
					RoyalRushAction.unPauseLevelUpEvent(GameState.game.keyName);
				}
			}
		}
		
		Audio.switchMusicKeyImmediate(previousMusicKey);
	}
	
	public void init (bool _isIncreasingInflation, int _newLevel)
    {
        if (!isIncreasingInflation)
        {
            isIncreasingInflation = _isIncreasingInflation;
        }

		ExperienceLevelData curLevel = ExperienceLevelData.find(_newLevel);

		if (curLevel == null)
		{
			// Hopefully this will NEVER happen.
			Debug.LogError("ExperienceLevelData not found for level " + _newLevel);
			return;
		}

		creditsAwarded += curLevel.bonusAmt + curLevel.levelUpBonusAmount;
		vipPointsAwarded += curLevel.bonusVIPPoints * curLevel.vipMultiplier;
		newLevel = _newLevel;
		
		StatsManager.Instance.LogCount("dialog", "level_up", newLevel.ToString(), "", "", "view", creditsAwarded);
    }

	private void skipClicked(Dict args = null)
	{
		args = Dict.create(D.OPTION, true);
		skipButton.gameObject.SetActive(false);
		starSequenceParent.SetActive(false);
		summaryScreenParent.SetActive(true);
		summaryScreen.closeClicked(args);
	}

	private void summaryTimeout(Dict args = null, GameTimerRange timerParent = null)
	{
		if (!isClosing)
		{
			summaryScreen.closeClicked();
		}
	}
}
