using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * A minor module that sets the value of the multiplier label to the multiplier of the reel game.
 */
public class ChallengeGameSetMultiplierLabelToReelGameValueOnRoundStartModule : ChallengeGameModule
{
	public override bool needsToExecuteOnRoundStart()
	{
		// Set this in the needToExecute so it gets changed in the same frame.
		if (roundVariantParent.multiplierLabel != null)
		{
			long multiplier = 1;
			if (ReelGame.activeGame != null)
			{
				multiplier = ReelGame.activeGame.relativeMultiplier;
			}
			roundVariantParent.multiplierLabel.text = Localize.text("{0}X", CommonText.formatNumber(multiplier));
		}
		return true;
	}
}
