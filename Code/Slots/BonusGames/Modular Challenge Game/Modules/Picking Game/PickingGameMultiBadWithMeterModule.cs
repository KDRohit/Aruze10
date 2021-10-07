using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Module for a bad reveal which has a meter to track how many bad's you've found
*/
public class PickingGameMultiBadWithMeterModule : PickingGameBadPickModule 
{
	[SerializeField] private PickingGameMultiBadWithMeterModuleController multiBadModuleController;
	[SerializeField] private float TIME_BEFORE_HANDLING_METER_ANIMS = 0.15f; // add a small delay so that the reveal is going before the effects start to go off
	[SerializeField] private bool isCopyingNumBadRevealsFromPreviousRound = false;

	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		// perform the base reveal
		List<TICoroutine> runningCoroutines = new List<TICoroutine>();
		runningCoroutines.Add(StartCoroutine(base.executeOnItemClick(pickItem)));

		yield return new TIWaitForSeconds(TIME_BEFORE_HANDLING_METER_ANIMS);

		runningCoroutines.Add(StartCoroutine(multiBadModuleController.executeOnItemClick(pickItem, roundVariantParent.gameObject)));

		// wait for the base reveal and additional animations to finish
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(runningCoroutines));
	}

	// executeCopyDataFromModulesOfPrevRound() section
	// exectues when ModularChallengeGame.advanceRound() is called with a valid next round.  
	// This function allows the copying of data that needs to persist between rounds for a type of module.
	public override bool needsToExecuteCopyDataFromModulesOfPrevRound()
	{
		return isCopyingNumBadRevealsFromPreviousRound;
	}

	public override void executeCopyDataFromModulesOfPrevRound(List<ChallengeGameModule> modulesToCopyFrom)
	{
		if (modulesToCopyFrom.Count == 1)
		{
			PickingGameMultiBadWithMeterModule prevRoundModule = modulesToCopyFrom[0] as PickingGameMultiBadWithMeterModule;

			if (prevRoundModule != null)
			{
				multiBadModuleController.numBadSymbolsFound = prevRoundModule.multiBadModuleController.numBadSymbolsFound;
			}
			else
			{
				Debug.LogError("Something went wrong, unable to convert ChallengeGameModule to PickingGameMultiBadWithMeterModule!");
			}
		}
		else
		{
			if (modulesToCopyFrom.Count == 0)
			{
				Debug.LogWarning("Previous round didn't contain any PickingGameMultiBadWithMeterModule modules to copy from!");
			}
			else
			{
				Debug.LogWarning("Previous round contains multiple PickingGameMultiBadWithMeterModule modules, which probably isn't correct, and I don't know which to copy from!");
			}
		}
	}
}
