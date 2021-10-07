using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/* Pickem class for the Grandma pick game */

public class Gen10PickemWolfGame : PickingGame<PickemOutcome>
{
	public Animator winBoxAnimator;
	public Animator moonAnimator;
	public UILabel multiplierText;	// To be removed when prefabs are updated.
	public LabelWrapperComponent multiplierTextWrapperComponent;

	public LabelWrapper multiplierTextWrapper
	{
		get
		{
			if (_multiplierTextWrapper == null)
			{
				if (multiplierTextWrapperComponent != null)
				{
					_multiplierTextWrapper = multiplierTextWrapperComponent.labelWrapper;
				}
				else
				{
					_multiplierTextWrapper = new LabelWrapper(multiplierText);
				}
			}
			return _multiplierTextWrapper;
		}
	}
	private LabelWrapper _multiplierTextWrapper = null;
	
	private PickemPick pickemPick;
	private int multiplier = 1;
	private bool playPickVO = true;

	private const string INTRO_VO = "BonusIntroVOCoyote";
	private const string PICKME_SOUND = "HowlPickMe";
	private const string HOWL_WOLF = "HowlPickWolf";
	private const string HOWL_REVEAL_WOLF = "HowlRevealHowl";
	private const string MULTIPLIER_ADVANCE = "HowlAdvx";
	private const string PICK_BAD = "HowlRevealBad";
	private const string SUMMARY_VO = "BonusSummaryVOCoyote";
	private const string CANDLE_TRAVEL_SFX = "CandleTravelsSparklyWhoosh";
	private const string REVEAL_OTHERS = "reveal_others";
	private const string POOPER_VO = "HowlPickPooperVO";
	private const string HOWL_VO = "HowlPickVOCoyote";
	private const string TRAIL_SFX = "value_move";
	private const string MULTIPLIER_LANDED = "HowlCreditMultiplied";
	private const string FINAL_MULTIPLIER_VALUE = "Howl15X";

	private const string PICKME_ANIM = "Picking object_Pick me";
	private const string STILL_ANIM = "Picking object_Still";
	private const string OBJECT_REVEAL_ANIM = "Picking object_reveral number";
	private const string OBJECT_REVEAL_GAME_OVER_ANIM = "Picking object_reveral end";
	private const string OBJECT_REVEAL_GRAY_GO_ANIM = "Picking object_not Selected end";
	private const string OBJECT_REVEAL_GRAY_NUM_ANIM = "Picking object_not Selected number";
	private const string WINBOX_ANIM = "Win Box loop";
	private const string WINBOX_STILL = "Win Box still";
	private const string MOON_REVEAL = "moon_reveal";
	private const string MOON_LOOP = "moon_Loop";
	
	private const float PICKME_DELAY = 1.0f;
	private const float POST_PICKME_DELAY = 4.0f;
	private const float OBJECT_REVEAL_DELAY = 0.15f;
	private const float POST_REVEAL_ANIM_DELAY = 0.5f;
	private const float PRE_REVEAL_DELAY = 1.0f;
	private const float END_GAME_DELAY = 1.5f;
	private const float POST_OBJECT_SELECTED = 1.0f;
	private const float CANDLE_FLIGHT_TIME = 0.5f;
	private const float IGNITE_TIME = 0.8f;
	private const float POOPER_VO_DELAY = 0.8f;
	private const float SPARKLE_TRAIL_SCALE = 0.1f;
	private const float MULTIPLIER_TRAIL_SCALE = 0.2f;
	private const int MULTIPLIER_TRAIL_LAYER = 11;
	[SerializeField] private float SPARKLE_TRAIL_TIMING = 1.0f;
	[SerializeField] private float SPARKLE_HOLD_TIMING = 0.5f;
	[SerializeField] private float SPARKLE_EXPLOSION_DUR = 1.0f;
	private const float POST_MOON_REVEAL_DELAY = 1.5f;

	/// Handle initialization stuff for the game
	public override void init()
	{
		base.init();
		multiplierTextWrapper.text = Localize.text("{0}X", multiplier);
		Audio.play(INTRO_VO);
		winBoxAnimator.Play(WINBOX_STILL);
	}

	/// Triggered periodically to draw the users eye
	protected override IEnumerator pickMeAnimCallback()
	{		
		PickGameButtonData pickButton = getRandomPickMe();
			
		if (pickButton != null)
		{
			pickButton.animator.Play(PICKME_ANIM);
			Audio.play(PICKME_SOUND);
			
			yield return new WaitForSeconds(PICKME_DELAY);
			if (isButtonAvailableToSelect(pickButton))
			{
				pickButton.animator.Play(STILL_ANIM);
			}

			yield return new TIWaitForSeconds(POST_PICKME_DELAY);
		}
	}

	/// Called when a button is pressed
	protected override IEnumerator pickemButtonPressedCoroutine(GameObject button)
	{
		inputEnabled = false;
		pickemPick = outcome.getNextEntry();

		int pickIndex = getButtonIndex(button);
		removeButtonFromSelectableList(getButtonUsingIndexAndRound(pickIndex));
		PickGameButtonData pickButtonData = getPickGameButton(pickIndex);

		if (pickemPick.isGameOver)
		{
			Audio.play(PICK_BAD);
			Audio.play(POOPER_VO, 1, 0, POOPER_VO_DELAY);

			pickButtonData.animator.Play(OBJECT_REVEAL_GAME_OVER_ANIM);

			pickButtonData.extraLabel.text = CreditsEconomy.convertCredits(pickemPick.credits);

			// After we start the firecracker display, wait before starting the rollup.
			yield return new TIWaitForSeconds(POST_REVEAL_ANIM_DELAY);

			yield return StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout + (pickemPick.credits)));
			BonusGamePresenter.instance.currentPayout += (pickemPick.credits);

			yield return new TIWaitForSeconds(PRE_REVEAL_DELAY);

			StartCoroutine(revealAllLanterns());
		}
		else
		{
			Audio.play(HOWL_WOLF);
			Audio.play(HOWL_REVEAL_WOLF);
			if (playPickVO)
			{
				Audio.play(HOWL_VO);
			}

			playPickVO = !playPickVO;

			pickButtonData.animator.Play(OBJECT_REVEAL_ANIM);
			pickButtonData.revealNumberLabel.text = CreditsEconomy.convertCredits(pickemPick.credits);

			yield return new TIWaitForSeconds(POST_OBJECT_SELECTED);

			Audio.play(TRAIL_SFX);

			SparkleTrailParams trailParams = new SparkleTrailParams();
			trailParams.startObject = multiplierTextWrapper.gameObject;
			trailParams.endObject = pickButtonData.animator.gameObject;
			trailParams.dur = SPARKLE_TRAIL_TIMING;
			trailParams.holdDur = SPARKLE_HOLD_TIMING;
			yield return StartCoroutine(animateSparkleTrail(trailParams));

			long currentWin = pickemPick.credits * multiplier;
			pickButtonData.revealNumberLabel.text = CreditsEconomy.convertCredits(currentWin);

			Audio.play(MULTIPLIER_LANDED);
			yield return StartCoroutine(animateSparkleExplosion(pickButtonData.animator.gameObject, SPARKLE_EXPLOSION_DUR));

			yield return new TIWaitForSeconds(IGNITE_TIME);

			winBoxAnimator.Play(WINBOX_ANIM);

			yield return StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout + currentWin));
			BonusGamePresenter.instance.currentPayout += currentWin;
			yield return null;
			winBoxAnimator.Play(WINBOX_STILL);

			multiplier++;
			multiplierTextWrapper.text = Localize.text("{0}X", multiplier);
			moonAnimator.Play(MOON_REVEAL);
			if (multiplier == 15)
			{
				Audio.play(FINAL_MULTIPLIER_VALUE);
			}
			else
			{
				Audio.play(MULTIPLIER_ADVANCE);
			}
			yield return new TIWaitForSeconds(POST_MOON_REVEAL_DELAY);
			moonAnimator.Play(MOON_LOOP);
			inputEnabled = true;
		}
		yield return null;
	}

	private IEnumerator revealAllLanterns()
	{
		yield return new TIWaitForSeconds(PRE_REVEAL_DELAY);
		
		pickemPick = outcome.getNextReveal();
		while (pickemPick != null)
		{
			if(!revealWait.isSkipping)
			{
				Audio.play(REVEAL_OTHERS);
			}
			GameObject button = grabNextButtonAndRemoveIt(currentStage);
			int pickIndex = getButtonIndex(button);
			PickGameButtonData pickButtonData = getPickGameButton(pickIndex);
			
			if (pickemPick.isGameOver)
			{
				pickButtonData.animator.Play(OBJECT_REVEAL_GRAY_GO_ANIM);
				pickButtonData.extraLabel.text = CreditsEconomy.convertCredits(pickemPick.credits);
			}
			else
			{
				pickButtonData.animator.Play(OBJECT_REVEAL_GRAY_NUM_ANIM);
				pickButtonData.extraLabel.text = CreditsEconomy.convertCredits(pickemPick.credits);
			}

			pickemPick = outcome.getNextReveal();
			yield return StartCoroutine(revealWait.wait(OBJECT_REVEAL_DELAY));
		}

		yield return new TIWaitForSeconds(END_GAME_DELAY);
		Audio.play(SUMMARY_VO);
		BonusGamePresenter.instance.gameEnded();
	}
	
}



