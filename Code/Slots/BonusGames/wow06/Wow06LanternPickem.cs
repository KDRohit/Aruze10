using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/* Pickem class for the Grandma pick game */

public class Wow06LanternPickem : PickingGame<PickemOutcome>
{
	public GameObject candle;
	public Animator candleAnimator;
	public Animator winBoxAnimator;
	public UILabel candleMultiplier;	// To be removed when prefabs are updated.
	public LabelWrapperComponent candleMultiplierWrapperComponent;

	public LabelWrapper candleMultiplierWrapper
	{
		get
		{
			if (_candleMultiplierWrapper == null)
			{
				if (candleMultiplierWrapperComponent != null)
				{
					_candleMultiplierWrapper = candleMultiplierWrapperComponent.labelWrapper;
				}
				else
				{
					_candleMultiplierWrapper = new LabelWrapper(candleMultiplier);
				}
			}
			return _candleMultiplierWrapper;
		}
	}
	private LabelWrapper _candleMultiplierWrapper = null;
	
	public UILabel candleMultiplierShadow;	// To be removed when prefabs are updated.
	public LabelWrapperComponent candleMultiplierShadowWrapperComponent;

	public LabelWrapper candleMultiplierShadowWrapper
	{
		get
		{
			if (_candleMultiplierShadowWrapper == null)
			{
				if (candleMultiplierShadowWrapperComponent != null)
				{
					_candleMultiplierShadowWrapper = candleMultiplierShadowWrapperComponent.labelWrapper;
				}
				else
				{
					_candleMultiplierShadowWrapper = new LabelWrapper(candleMultiplierShadow);
				}
			}
			return _candleMultiplierShadowWrapper;
		}
	}
	private LabelWrapper _candleMultiplierShadowWrapper = null;
	
	private PickemPick pickemPick;
	private int multiplier = 1;

	private const string INTRO_VO = "LanternIntroVO";
	private const string PICKME_SOUND = "rollover_sparkly";
	private const string LANTERN_SHIMMER = "LanternSparklyShimmer";
	private const string LANTERN_IGNITE = "LanternIgniteLantern";
	private const string LANTERN_FLOAT = "LanternFloatsSparklyImpactArpeggio";
	private const string LANTERN_STINGER = "LanternBurnsUpStinger";
	private const string FIREWORK_SPRAY = "FireworksSpray";
	private const string SUMMARY_VO = "LanternSummaryVO";
	private const string CANDLE_TRAVEL_SFX = "CandleTravelsSparklyWhoosh";
	private const string REVEAL_OTHERS = "reveal_others";

	private const string LANTERN_PICKME_ANIM = "PickingObject_lantern01_PickMe";
	private const string LANTERN_STILL_ANIM = "PickingObject_lantern01_Still";
	private const string LANTERN_REVEAL_ANIM = "PickingObject_lantern01_Reveal01";
	private const string LANTERN_FLY_ANIM = "PickingObject_lantern01_Reveal02";
	private const string LANTERN_REVEAL_GAME_OVER_ANIM = "PickingObject_lantern01_Reveal_GameOver";
	private const string LANTERN_REVEAL_GRAY_GO_ANIM = "PickingObject_lantern01_notSelectGameOver";
	private const string LANTERN_REVEAL_GRAY_NUM_ANIM = "PickingObject_lantern01_notSelectNumber";
	private const string WINBOX_ANIM = "winbox_anim";
	private const string WINBOX_STILL = "winbox_still";
	private const string CANDLE_FADE = "candle_fadeOut";
	private const string CANDLE_LOOP = "candle_loop";

	private const float PICKME_DELAY = 0.75f;
	private const float LANTERN_REVEAL_DELAY = 0.15f;
	private const float POST_FIRECRACKER_DELAY = 0.5f;
	private const float PRE_REVEAL_DELAY = 1.0f;
	private const float END_GAME_DELAY = 1.5f;
	private const float POST_LANTERN_SELECTED = 1.0f;
	private const float CANDLE_FLIGHT_TIME = 0.5f;
	private const float IGNITE_TIME = 0.1f;

	/// Handle initialization stuff for the game
	public override void init()
	{
		base.init();
		Audio.play(INTRO_VO);
		candleMultiplierWrapper.text = Localize.text("{0}X", multiplier.ToString());
		candleMultiplierShadowWrapper.text = Localize.text("{0}X", multiplier.ToString());
	}


	/// Triggered periodically to draw the users eye
	protected override IEnumerator pickMeAnimCallback()
	{		
		PickGameButtonData pickButton = getRandomPickMe();
			
		if (pickButton != null)
		{
			pickButton.animator.Play(LANTERN_PICKME_ANIM);
			Audio.play(PICKME_SOUND);
			
			yield return new WaitForSeconds(PICKME_DELAY);
			if (isButtonAvailableToSelect(pickButton))
			{
				pickButton.animator.Play(LANTERN_STILL_ANIM);
			}
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
			Audio.play(LANTERN_STINGER);
			Audio.play(FIREWORK_SPRAY, 1, 0, 0.8f);

			pickButtonData.animator.Play(LANTERN_REVEAL_GAME_OVER_ANIM);

			pickButtonData.multiplierLabel.text = CreditsEconomy.convertCredits(pickemPick.credits);
			pickButtonData.multiplierOutlineLabel.text = CreditsEconomy.convertCredits(pickemPick.credits);

			// After we start the firecracker display, wait before starting the rollup.
			yield return new TIWaitForSeconds(POST_FIRECRACKER_DELAY);

			yield return StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout + (pickemPick.credits)));
			BonusGamePresenter.instance.currentPayout += (pickemPick.credits);

			yield return new TIWaitForSeconds(PRE_REVEAL_DELAY);

			StartCoroutine(revealAllLanterns());
		}
		else
		{
			Audio.play(LANTERN_SHIMMER);
			pickButtonData.animator.gameObject.transform.position = pickButtonData.animator.gameObject.transform.position - new Vector3(0, 0, 0.05f);

			pickButtonData.animator.Play(LANTERN_REVEAL_ANIM);
			pickButtonData.revealNumberLabel.text = CreditsEconomy.convertCredits(pickemPick.credits);
			pickButtonData.revealNumberOutlineLabel.text = CreditsEconomy.convertCredits(pickemPick.credits);

			yield return new TIWaitForSeconds(POST_LANTERN_SELECTED);

			// We revealed, now fly the candle up to the lantern.
			Vector3 originalPosition = candle.transform.position;

			iTween.MoveTo(candle, iTween.Hash("position", pickButtonData.animator.gameObject.transform.position - new Vector3(0, 0.25f, -.28f), "time", CANDLE_FLIGHT_TIME, "islocal", false, "easetype", iTween.EaseType.linear));
			Audio.play(CANDLE_TRAVEL_SFX);

			yield return new TIWaitForSeconds(CANDLE_FLIGHT_TIME);

			// Now fade the candle away.
			candleAnimator.Play(CANDLE_FADE);

			long currentWin = pickemPick.credits * multiplier;
			pickButtonData.revealNumberLabel.text = CreditsEconomy.convertCredits(currentWin);
			pickButtonData.revealNumberOutlineLabel.text = CreditsEconomy.convertCredits(currentWin);
			Audio.play(LANTERN_IGNITE);

			yield return new TIWaitForSeconds(IGNITE_TIME);

			// Then make the lantern fly away.
			pickButtonData.animator.Play(LANTERN_FLY_ANIM);
			Audio.play(LANTERN_FLOAT);

			winBoxAnimator.Play(WINBOX_ANIM);

			yield return StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout + currentWin));
			BonusGamePresenter.instance.currentPayout += currentWin;

			// Make the candle return at the bottom, and start the sequence over. Yielding null here so the candle might not pop in place.
			candle.transform.position = originalPosition;
			yield return null;

			candleAnimator.Play(CANDLE_LOOP);
			winBoxAnimator.Play(WINBOX_STILL);

			// Check if all pick entries have been exhausted without running into a BAD pick (which has GameOver flag)
			if (outcome.entryCount == 0)
			{
				StartCoroutine(revealAllLanterns());
			}
			else
			{
				multiplier++;

				candleMultiplierWrapper.text = Localize.text("{0}X", multiplier.ToString());
				candleMultiplierShadowWrapper.text = Localize.text("{0}X", multiplier.ToString());

				inputEnabled = true;
			}
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
				pickButtonData.animator.Play(LANTERN_REVEAL_GRAY_GO_ANIM);
				pickButtonData.multiplierLabel.text = CreditsEconomy.convertCredits(pickemPick.credits);
				pickButtonData.multiplierOutlineLabel.text = CreditsEconomy.convertCredits(pickemPick.credits);
				pickButtonData.extraLabel.text = CreditsEconomy.convertCredits(pickemPick.credits);
				pickButtonData.extraOutlineLabel.text = CreditsEconomy.convertCredits(pickemPick.credits);
			}
			else
			{
				pickButtonData.animator.Play(LANTERN_REVEAL_GRAY_NUM_ANIM);
				pickButtonData.multiplierLabel.text = CreditsEconomy.convertCredits(pickemPick.credits);
				pickButtonData.multiplierOutlineLabel.text = CreditsEconomy.convertCredits(pickemPick.credits);
				pickButtonData.extraLabel.text = CreditsEconomy.convertCredits(pickemPick.credits);
				pickButtonData.extraOutlineLabel.text = CreditsEconomy.convertCredits(pickemPick.credits);
			}

			pickemPick = outcome.getNextReveal();
			yield return StartCoroutine(revealWait.wait(LANTERN_REVEAL_DELAY));
		}

		yield return new TIWaitForSeconds(END_GAME_DELAY);
		Audio.play(SUMMARY_VO);
		BonusGamePresenter.instance.gameEnded();
	}
	
}



