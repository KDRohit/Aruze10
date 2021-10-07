using UnityEngine;
using System.Collections;

/* Module to handle increasing available picks when revealing a specific symbol, including credits*/
public class PickingGameIncreasePicksAndCreditsModule : PickingGameIncreasePicksModule
{
	[SerializeField] private bool USE_BASE_CREDITS = false;

	protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		// detect pick type & whether to handle with this module
		if ((pickData != null) && (pickData.additonalPicks > 0 || pickData.extraRound > 0) && (!pickData.canAdvance) && (pickData.credits > 0))
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

		PickingGameCreditPickItem creditsRevealItem = PickingGameCreditPickItem.getPickingGameCreditsPickItemForType(pickItem.gameObject, PickingGameCreditPickItem.CreditsPickItemType.IncreasePicks);

		// if there are credits, set the label values before revealing
		if (currentPick.credits > 0 && !USE_BASE_CREDITS)
		{
			// adjust with bonus multiplier if necessary
			creditsRevealItem.setCreditLabels(currentPick.credits);
		}
		else if(currentPick.baseCredits > 0 && USE_BASE_CREDITS)
		{
			creditsRevealItem.setCreditLabels(currentPick.baseCredits);
		}

		// set the increase value within the item and the reveal animation
		pickItem.REVEAL_ANIMATION = REVEAL_ANIMATION_NAME;
		yield return StartCoroutine(base.executeOnItemClick(pickItem));

		// animate credit values if there are any 
		if (currentPick.credits > 0 && !USE_BASE_CREDITS)
		{
			// rollup with extra animations included
			yield return StartCoroutine(base.rollupCredits(currentPick.credits));
		}
		else if (currentPick.baseCredits > 0 && USE_BASE_CREDITS)
		{
			yield return StartCoroutine(base.rollupCredits(currentPick.baseCredits));
		}
	}


	public override IEnumerator executeOnRevealPick(PickingGameBasePickItem pickItem)
	{
		yield return StartCoroutine(base.executeOnRevealPick(pickItem));
	}

	public override IEnumerator executeOnRevealLeftover(PickingGameBasePickItem leftover)
	{
		// set the pick quantity value from the leftovers
		ModularChallengeGameOutcomeEntry leftoverOutcome = pickingVariantParent.getCurrentLeftoverOutcome();
		if (leftoverOutcome != null)
		{
			PickingGameCreditPickItem creditsLeftoverItem = PickingGameCreditPickItem.getPickingGameCreditsPickItemForType(leftover.gameObject, PickingGameCreditPickItem.CreditsPickItemType.IncreasePicks);

			// if there are credits, set the label values before revealing
			if (leftoverOutcome.credits > 0 && !USE_BASE_CREDITS)
			{
				// adjust with bonus multiplier if necessary
				creditsLeftoverItem.setCreditLabels(leftoverOutcome.credits);
			}
			else if (leftoverOutcome.baseCredits > 0 && USE_BASE_CREDITS)
			{
				creditsLeftoverItem.setCreditLabels(leftoverOutcome.baseCredits);
			}
		}

		yield return StartCoroutine(base.executeOnRevealLeftover(leftover));
	}

}
