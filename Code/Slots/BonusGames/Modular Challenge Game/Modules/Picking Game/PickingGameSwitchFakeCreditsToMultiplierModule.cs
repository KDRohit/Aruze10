using UnityEngine;
using System.Collections;

//This class is to set up set following sequence
//	1) Reveal fake credit amount like a credit pick
//	2) Play sound and roll up fake credits
//	3) Play special VO ("Oops thats not right")
//	4) Play reveal multiplier animation 
//	5) Play special VO ("There thar's better")
//  6) Rollup credits to actual winnings total
//
//If you'd like reference this is currently used in Harvey01's final round 

public class PickingGameSwitchFakeCreditsToMultiplierModule : PickingGameRevealModule
{
	[SerializeField] private long FAKE_CREDITS_AMOUNT = 100;
	[SerializeField] private string REVEAL_FAKE_CREDITS_ANIMATION_NAME = "credit";
	[SerializeField] private string REVEAL_FAKE_CREDITS_AUDIO = "pickem_special1_pick2";
	[SerializeField] private string REVEAL_FAKE_CREDITS_VO_AUDIO = "pickem_special1_vo_pick2";
	[SerializeField] private float REVEAL_FAKE_CREDITS_VO_DELAY = 0.2f; // delay before playing the VO clip
	[SerializeField] private float TIME_TO_DELAY_BETWEEN_REVEALS = 1.0f; // delay before playing the VO clip
	[SerializeField] private string REVEAL_MULTIPLIER_ANIMATION_NAME = "2x";
	[SerializeField] private string REVEAL_GRAY_MULTIPLIER_ANIMATION_NAME = "grey 2x";
	[SerializeField] private string REVEAL_MULTIPLIER_AUDIO = "pickem_special1_pick3";
	[SerializeField] private string REVEAL_MULTIPLIER_VO_AUDIO = "pickem_special1_vo_pick3";
	[SerializeField] private float REVEAL_MULTIPLIER_VO_DELAY = 0.2f; // delay before playing the VO clip
	[SerializeField] private string REVEAL_LEFTOVER_AUDIO = "reveal_not_chosen";

	[SerializeField] private Animator rollupAnimator;
	[SerializeField] private string ROLLUP_ANIMATION_NAME = "on";
	[SerializeField] private string ROLLUP_ANIMATION_IDLE = "off";

    protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		if (pickData.multiplier == 1)
		{
			return true;
		}
		else
		{
			return false;
		}
	}
	
	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentPickOutcome();
		
		//Play the fake credits reveal audio
		Audio.play(Audio.soundMap(REVEAL_FAKE_CREDITS_AUDIO));
		if (!string.IsNullOrEmpty(REVEAL_FAKE_CREDITS_VO_AUDIO))
		{
			// play the associated audio voiceover
			Audio.playWithDelay(Audio.soundMap(REVEAL_FAKE_CREDITS_VO_AUDIO), REVEAL_FAKE_CREDITS_VO_DELAY);
		}
		pickItem.REVEAL_ANIMATION = REVEAL_FAKE_CREDITS_ANIMATION_NAME;

		//Reveal the fake credit amount
		PickingGameCreditPickItem creditsRevealItem = pickItem.gameObject.GetComponent<PickingGameCreditPickItem>();
		creditsRevealItem.setCreditLabels(FAKE_CREDITS_AMOUNT);
		yield return StartCoroutine(base.executeOnItemClick(pickItem));

		// play custom animation for rollup
		if (rollupAnimator != null)
		{
			rollupAnimator.Play(ROLLUP_ANIMATION_NAME);
		}

		// Roll up the fake amount
		yield return StartCoroutine(rollupCredits(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout + FAKE_CREDITS_AMOUNT, false));		

		// play custom animation for after rollup
		if (rollupAnimator != null)
		{
			rollupAnimator.Play(ROLLUP_ANIMATION_IDLE);
		}

		//Time to wait for the joke/swap
		yield return new TIWaitForSeconds(TIME_TO_DELAY_BETWEEN_REVEALS);

		//Play the reveal audio for the multiplier
		Audio.play(Audio.soundMap(REVEAL_MULTIPLIER_AUDIO));
		if (!string.IsNullOrEmpty(REVEAL_MULTIPLIER_VO_AUDIO))
		{
			// play the associated audio voiceover
			Audio.playWithDelay(Audio.soundMap(REVEAL_MULTIPLIER_VO_AUDIO), REVEAL_MULTIPLIER_VO_DELAY);
		}

		//Set the reveal animation to the multiplier animation
		pickItem.REVEAL_ANIMATION = REVEAL_MULTIPLIER_ANIMATION_NAME;
		//Replay the reveal animation 
		yield return StartCoroutine(pickItem.revealPick(currentPick));

		// play custom animation for rollup
		if (rollupAnimator != null)
		{
			rollupAnimator.Play(ROLLUP_ANIMATION_NAME);
		}

		// animate credit values with multiplier
		yield return StartCoroutine(rollupCredits(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout * (currentPick.multiplier + 1)));

		// play custom animation for after rollup
		if (rollupAnimator != null)
		{
			rollupAnimator.Play(ROLLUP_ANIMATION_IDLE);
		}
	}

	public override IEnumerator executeOnRevealLeftover(PickingGameBasePickItem leftover)
	{
		leftover.REVEAL_ANIMATION_GRAY = REVEAL_GRAY_MULTIPLIER_ANIMATION_NAME;
		// play the associated leftover reveal sound
		Audio.play(Audio.soundMap(REVEAL_LEFTOVER_AUDIO));
		yield return StartCoroutine(base.executeOnRevealLeftover(leftover));
	}
}
