using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Handles a game where the player has to find a certain number of advance icons in order to move to the next round
*/
public class PickingGameMultiAdvanceWithMeterModule : PickingGameAdvanceModule
{
	[SerializeField] private PickingGameMultiAdvanceWithMeterModuleController multiAdvanceModuleController;
	[SerializeField] private float TIME_BEFORE_HANDLING_METER_ANIMS = 0.15f; // add a small delay so that the reveal is going before the effects start to go off

	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		// perform the base reveal
		List<TICoroutine> runningCoroutines = new List<TICoroutine>();
		runningCoroutines.Add(StartCoroutine(base.executeOnItemClick(pickItem)));

		yield return new TIWaitForSeconds(TIME_BEFORE_HANDLING_METER_ANIMS);

		runningCoroutines.Add(StartCoroutine(multiAdvanceModuleController.executeOnItemClick(pickItem, roundVariantParent.gameObject)));

		// wait for the base reveal and additional animations to finish
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(runningCoroutines));
	}
}
