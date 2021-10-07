using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * A slot module to handle the swapping from normal to independent reels and back again when
 * a feature triggers.  The normal reels are assumed to be on layer 0 and the independent reels on layer 1.
 * This class is tended for games with features like got01.  This base module can be reused for
 * other feature modules that also want to swap the reels.
 * Original Author: Scott Lepthien
 * Date Created: 2/22/2019
 */

public class SwapNormalToIndependentReelTypesOnReevalModule : SwapNormalToIndependentReelTypesBaseModule
{
	// executeOnPreSpin() section
	// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) before the reels spin
	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnPreSpin()
	{
		yield return StartCoroutine(activateNormalReels());
	}

	// executeOnReevaluationPreSpin() section
	// function in a very similar way to the normal PreSpin hook, but hooks into ReelGame.startNextReevaluationSpin()
	// and triggers before the reels begin spinning
	public override bool needsToExecuteOnReevaluationPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnReevaluationPreSpin()
	{
		yield return StartCoroutine(activateIndependentReels());
	}
}
