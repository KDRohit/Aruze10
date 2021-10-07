using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Module made to add the ability in legacy plop/tumble games to add a delay after the reels have plopped
if there isn't any win.  That way the next tumble/plop doesn't begin right away.
*/
public class WaitAfterNonWinFreespinTumbleSlotModule : SlotModule 
{
	[SerializeField] private float NON_WIN_WAIT_TIME = 1.0f;

	// we check for mutations on plopping finished
	public override bool needsToExecuteOnPloppingFinished()
	{
		return true;
	}

	// execute mutations. decide which outcome to use, then send off to doMutations()
	public override IEnumerator executeOnPloppingFinished(JSON currentTumbleOutcome, bool useTumble = false)
	{
		if (!reelGame.outcome.hasSubOutcomes())
		{
			// this outcome doesn't have any sub outcomes so nothing was won, so apply a delay
			// so the player can see what is on the reels
			if (NON_WIN_WAIT_TIME > 0.0f)
			{
				yield return new TIWaitForSeconds(NON_WIN_WAIT_TIME);
			}
		}
	}
}
