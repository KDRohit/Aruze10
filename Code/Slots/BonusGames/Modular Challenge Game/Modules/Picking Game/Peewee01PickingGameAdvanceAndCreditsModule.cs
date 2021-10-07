using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Module to handle revealing peewee specific coreography. Mainly Revealing the leftovers before the rollup happens.
 */
public class Peewee01PickingGameAdvanceAndCreditsModule : PickingGameAdvanceModule 
{

	protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		// detect pick type & whether to handle with this module
		return pickData != null && pickData.canAdvance && pickData.credits > 0;
	}

	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentPickOutcome();

		// Set credits value
		PickingGameCreditPickItem creditsRevealItem = PickingGameCreditPickItem.getPickingGameCreditsPickItemForType(pickItem.gameObject, PickingGameCreditPickItem.CreditsPickItemType.Advance);
		creditsRevealItem.setCreditLabels(currentPick.credits);

		yield return StartCoroutine(base.executeOnItemClick(pickItem));

		// TODO: Make this only happen if it's the last pick of the round.
		// We are on our last pick, so we should show the leftovers.
		yield return StartCoroutine(pickingVariantParent.showLeftovers());

		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(ambientAnimationInformationOnReveal));

		// animate credit values if there are any 
		if (currentPick.credits > 0)
		{
			yield return StartCoroutine(rollupCredits(currentPick.credits));
		}
	}

	protected override IEnumerator playAmbientInformationOnReveal(List<TICoroutine> runningAnimations)
	{
		// We want to masively change the order that these clicks are happening.
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(runningAnimations));
	}

	public override IEnumerator executeOnRevealLeftover(PickingGameBasePickItem leftover)
	{
		// set the credit value from the leftovers
		ModularChallengeGameOutcomeEntry leftoverOutcome = pickingVariantParent.getCurrentLeftoverOutcome();

		PickingGameCreditPickItem creditsLeftOver = PickingGameCreditPickItem.getPickingGameCreditsPickItemForType(leftover.gameObject, PickingGameCreditPickItem.CreditsPickItemType.Advance);

		if (leftoverOutcome != null)
		{
			if (creditsLeftOver != null)
			{
				creditsLeftOver.setCreditLabels(leftoverOutcome.credits);
				creditsLeftOver.REVEAL_ANIMATION_GRAY = REVEAL_GRAY_ANIMATION_NAME;
			}
			else
			{
				Debug.LogError("PickingGameAdvanceAndCreditsModule.executeOnRevealLeftover() - leftover item didn't have an attached PickingGameCreditPickItem!");
			}
		}
			
		// play the associated leftover reveal sound
		Audio.play(Audio.soundMap(REVEAL_LEFTOVER_AUDIO));

		yield return StartCoroutine(base.executeOnRevealLeftover(leftover));
	}
}

