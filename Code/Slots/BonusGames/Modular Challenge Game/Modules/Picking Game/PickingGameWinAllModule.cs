using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PickingGameWinAllModule : PickingGameRevealModule
{
	[SerializeField] private AnimationListController.AnimationInformationList winAllAnimationInformationOnReveal;
	[SerializeField] private string WIN_ALL_ANIMATION_NAME = "win all";
	[SerializeField] private string WIN_ALL_GRAY_ANIMATION_NAME = "grey win all";
	[SerializeField] private string WIN_ALL_REVEAL_AUDIO = "pickem_reveal_jackpot_1";
	[SerializeField] private string WIN_ALL_AUDIO = "pickem_special1_pick";
	[SerializeField] private string WIN_ALL_VO_AUDIO = "pickem_special1_vo_pick";
	[SerializeField] private float WIN_ALL_REVEAL_DELAY = 0.5f;
	[SerializeField] private string REVEAL_ANIMATION_NAME = "revealCredit";
	[SerializeField] private string REVEAL_GRAY_ANIMATION_NAME = "revealCreditGray";
	[SerializeField] private string CREDITS_PICK_AUDIO = "pickem_reveal_credits_1";
	[SerializeField] private string REVEAL_AUDIO = "pickem_credits_pick";
	[SerializeField] private string REVEAL_VO_AUDIO = "pickem_credits_vo_pick";
	[SerializeField] private float REVEAL_VO_DELAY = 0.2f; // delay before playing the VO clip
	[SerializeField] private string REVEAL_LEFTOVER_AUDIO = "reveal_not_chosen";
	[SerializeField] private float PRE_ROLLUP_WAIT = 0.0f; //delay after the reveals before we rollup on a "WIN ALL" win

	[SerializeField] private Animator rollupAnimator;
	[SerializeField] private string ROLLUP_ANIMATION_NAME = "ui_win_loop";
	[SerializeField] private string ROLLUP_ANIMATION_IDLE = "ui_hold";

	[SerializeField] private bool playSparkleTrailToWinboxBeforeRollup = false;
	
	private ModularChallengeGameOutcomeEntry winAllPick;
	private long winAllCredits = 0;

	protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		if (pickData == null)
		{			
			return false;
		}

		if (((pickData.credits > 0) && (pickData.pickemGroupId == "") && (pickData.additonalPicks == 0) && !pickData.isGameOver) || pickData.isCollectAll)
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
		
		if (currentPick.isCollectAll)
		{
			// play the associated reveal sound
			Audio.play(Audio.soundMap(WIN_ALL_AUDIO));
			if (!string.IsNullOrEmpty(WIN_ALL_REVEAL_AUDIO))
			{
				Audio.play(Audio.soundMap(WIN_ALL_REVEAL_AUDIO));
			}
			if (!string.IsNullOrEmpty(WIN_ALL_VO_AUDIO))
			{
				// play the associated audio voiceover
				Audio.playWithDelay(Audio.soundMap(WIN_ALL_VO_AUDIO), REVEAL_VO_DELAY);
			}			
			pickItem.REVEAL_ANIMATION = WIN_ALL_ANIMATION_NAME;
			winAllPick = currentPick;
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(winAllAnimationInformationOnReveal));
		}
		else
		{
			// play the associated reveal sound
			Audio.play(Audio.soundMap(REVEAL_AUDIO));
			if (!string.IsNullOrEmpty(CREDITS_PICK_AUDIO))
			{
				// play the associated audio voiceover
				Audio.play(Audio.soundMap(CREDITS_PICK_AUDIO));
			}
			if (!string.IsNullOrEmpty(REVEAL_VO_AUDIO))
			{
				// play the associated audio voiceover
				Audio.playWithDelay(Audio.soundMap(REVEAL_VO_AUDIO), REVEAL_VO_DELAY);
			}
			pickItem.REVEAL_ANIMATION = REVEAL_ANIMATION_NAME;
		}

		PickingGameCreditPickItem creditsRevealItem = PickingGameCreditPickItem.getPickingGameCreditsPickItemForType(pickItem.gameObject, PickingGameCreditPickItem.CreditsPickItemType.Default);
		creditsRevealItem.setCreditLabels(currentPick.credits);
		yield return StartCoroutine(base.executeOnItemClick(pickItem));

		if (playSparkleTrailToWinboxBeforeRollup)
		{
			ParticleTrailController particleTrailController = ParticleTrailController.getParticleTrailControllerForType(pickItem.gameObject, ParticleTrailController.ParticleTrailControllerType.Default);

			if (particleTrailController != null)
			{
				yield return StartCoroutine(particleTrailController.animateParticleTrail(rollupAnimator.gameObject.transform.position, roundVariantParent.gameObject.transform));
			}	
		}

		// animate credit values
		if (winAllPick == null)
		{
			// play custom animation for rollup
			if (rollupAnimator != null)
			{
				rollupAnimator.Play(ROLLUP_ANIMATION_NAME);
			}

			//Animate Credits
			yield return StartCoroutine(base.rollupCredits(currentPick.credits));

			// play custom animation for after rollup
			if (rollupAnimator != null)
			{
				rollupAnimator.Play(ROLLUP_ANIMATION_IDLE);
			}
		}
	}

	public override bool needsToExecuteOnRevealRoundEnd()
	{
		return (winAllPick != null);
	}

	public override IEnumerator executeOnRevealRoundEnd(List<PickingGameBasePickItem> leftovers)
	{
		foreach (PickingGameBasePickItem leftover in leftovers)
		{
			ModularChallengeGameOutcomeEntry leftoverOutcome = pickingVariantParent.getCurrentLeftoverOutcome();
			PickingGameCreditPickItem creditsLeftOver = PickingGameCreditPickItem.getPickingGameCreditsPickItemForType(leftover.gameObject, PickingGameCreditPickItem.CreditsPickItemType.Default);
			if (leftoverOutcome != null && !leftoverOutcome.isGameOver && creditsLeftOver != null)
			{
				//Set the left over animation to the win all animation			
				creditsLeftOver.REVEAL_ANIMATION_GRAY = REVEAL_ANIMATION_NAME;
				winAllCredits += leftoverOutcome.credits;
				creditsLeftOver.setCreditLabels(leftoverOutcome.credits);
				//Play the associated credit reveal sound
				Audio.play(Audio.soundMap(pickingVariantParent.revealAudioKey));
				//Reveal the credit item
				StartCoroutine(leftover.revealLeftover(leftoverOutcome));
				//Set item as reveal so it wont be used when displaying the rest
				leftover.isRevealed = true;

				//Remove this outcome so we don't reveal it with the leftovers
				pickingVariantParent.consumeCurrentLeftoverOutcome();
			}
			else
			{
				//Make sure we get the next outcome if we didnt use our current one
				pickingVariantParent.advanceLeftover();
			}
		}

		//Reset the leftover index
		pickingVariantParent.resetLeftover();

		yield return new TIWaitForSeconds(PRE_ROLLUP_WAIT);
		// play custom animation for rollup
		if (rollupAnimator != null)
		{
			rollupAnimator.Play(ROLLUP_ANIMATION_NAME);
		}

		//Animate Credits
		if (winAllPick.credits == 0)
		{
			yield return StartCoroutine(base.rollupCredits(winAllCredits));
		}
		else
		{
			yield return StartCoroutine(base.rollupCredits(winAllPick.credits));
		}

		// play custom animation for after rollup
		if (rollupAnimator != null)
		{
			rollupAnimator.Play(ROLLUP_ANIMATION_IDLE);
		}

		//Brief wait to not feel rushed
		yield return new TIWaitForSeconds(WIN_ALL_REVEAL_DELAY);
		yield return StartCoroutine(base.executeOnRevealRoundEnd(leftovers));
	}

	public override IEnumerator executeOnRevealLeftover(PickingGameBasePickItem leftover)
	{
		// set the credit value from the leftovers
		ModularChallengeGameOutcomeEntry leftoverOutcome = pickingVariantParent.getCurrentLeftoverOutcome();

		PickingGameCreditPickItem creditsLeftOver = PickingGameCreditPickItem.getPickingGameCreditsPickItemForType(leftover.gameObject, PickingGameCreditPickItem.CreditsPickItemType.Default);

		if (leftoverOutcome.isCollectAll)
		{
			leftover.REVEAL_ANIMATION_GRAY = WIN_ALL_GRAY_ANIMATION_NAME;
		}
		else
		{
			creditsLeftOver.REVEAL_ANIMATION_GRAY = REVEAL_GRAY_ANIMATION_NAME;
		}

		if (leftoverOutcome != null)
		{
			if (creditsLeftOver != null)
			{
				creditsLeftOver.setCreditLabels(leftoverOutcome.credits);
			}
			else
			{
				Debug.LogError("PickingGameCreditsModule.executeOnRevealLeftover() - leftover item didn't have an attached PickingGameCreditPickItem!");
			}
		}

		// play the associated leftover reveal sound
		Audio.play(Audio.soundMap(REVEAL_LEFTOVER_AUDIO));

		yield return StartCoroutine(base.executeOnRevealLeftover(leftover));
	}	
}
