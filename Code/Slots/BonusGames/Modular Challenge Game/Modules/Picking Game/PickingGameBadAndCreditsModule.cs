using UnityEngine;
using System.Collections;

public class PickingGameBadAndCreditsModule : PickingGameBadPickModule 
{
	protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		// detect pick type & whether to handle with this module
		return pickData != null && pickData.isGameOver && pickData.credits > 0;
	}

	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentPickOutcome();

		// Set credits value
		PickingGameCreditPickItem creditsRevealItem = PickingGameCreditPickItem.getPickingGameCreditsPickItemForType(pickItem.gameObject, PickingGameCreditPickItem.CreditsPickItemType.Bad);
		
		if (creditsRevealItem != null)
		{
			creditsRevealItem.setCreditLabels(currentPick.credits);
		}
		
		yield return StartCoroutine(base.executeOnItemClick(pickItem));

		// animate credit values if there are any 
		if (currentPick.credits > 0)
		{
			yield return StartCoroutine(rollupCredits(currentPick.credits));
		}
	}

	public override IEnumerator executeOnRevealLeftover(PickingGameBasePickItem leftover)
	{
		// set the credit value from the leftovers
		ModularChallengeGameOutcomeEntry leftoverOutcome = pickingVariantParent.getCurrentLeftoverOutcome();

		PickingGameCreditPickItem creditsLeftOver = PickingGameCreditPickItem.getPickingGameCreditsPickItemForType(leftover.gameObject, PickingGameCreditPickItem.CreditsPickItemType.Bad);

		if (leftoverOutcome != null)
		{
			if (creditsLeftOver != null)
			{
				creditsLeftOver.setCreditLabels(leftoverOutcome.credits);
				leftover.REVEAL_ANIMATION_GRAY = REVEAL_GRAY_ANIMATION_NAME;
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
