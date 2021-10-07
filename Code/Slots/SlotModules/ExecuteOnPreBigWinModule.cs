using System.Collections;
using UnityEngine;

//
// This module can be used to add a delay before a big win plays.
// Future use could add AnimationInformation list or AnimatedParticleEffect.
//
// Author : Nick Saito <nsaito@zynga.com>
// Date : July 22, 2020
// Games : gen95
//

public class ExecuteOnPreBigWinModule : SlotModule
{
	[SerializeField] private bool requireMultiplier;
	[SerializeField] private float preBigWinDelay;

	public override bool needsToExecuteOnPreBigWin()
	{
		if (requireMultiplier)
		{
			return reelGame.outcomeDisplayController.multiplier > 1;
		}

		return true;
	}

	public override IEnumerator executeOnPreBigWin()
	{
		if (preBigWinDelay > 0)
		{
			yield return new WaitForSeconds(preBigWinDelay);
		}
	}
}
