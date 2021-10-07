using UnityEngine;
using System.Collections;

/**
 * Module to handle revealing Advacne outcomes during a picking round
 * Also includes awarding extra picks
 */
public class PickingGameAdvanceAndAdditionalPicksAndCreditsModule : PickingGameAdvanceAndAdditionalPicksModule
{
	protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		// detect pick type & whether to handle with this module
		return pickData != null && pickData.canAdvance && pickData.additonalPicks > 0 && pickData.credits > 0;
	}


	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentPickOutcome();

		// Set credits value
		PickingGameCreditPickItem creditsRevealItem = PickingGameCreditPickItem.getPickingGameCreditsPickItemForType(pickItem.gameObject, PickingGameCreditPickItem.CreditsPickItemType.Advance);
		creditsRevealItem.setCreditLabels(currentPick.credits);

		// perform the reveal
		yield return StartCoroutine(base.executeOnItemClick(pickItem));

		// animate credit values if there are any 
		yield return StartCoroutine(rollupCredits(currentPick.credits));

	}
}
