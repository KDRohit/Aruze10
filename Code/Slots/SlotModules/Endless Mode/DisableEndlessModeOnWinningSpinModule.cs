using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Module that can disable endless mode on a winning spin.  Useful for game types that use
 * the concept of spin till you win.  Where the game is endless until a win occurs.
 *
 * Original Author: Scott Lepthien
 * Creation Date: September 21, 2020
 */
public class DisableEndlessModeOnWinningSpinModule : SlotModule
{
	// executeOnReelsStoppedCallback() section
	// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return reelGame.endlessMode && reelGame.getSubOutcomeCount() > 0;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		reelGame.endlessMode = false;
		reelGame.numberOfFreespinsRemaining = 0;
		yield break;
	}
}
