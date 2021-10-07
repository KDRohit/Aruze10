using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
	Override the initial multiplier value from SCAT.
	Technically, ghostbusters01 has initial multiplier 0, but it's convenient to start at 1.
*/

public class ChallengeGameOverrideInitialMultiplierValueOnRoundStartModule : ChallengeGameModule
{
	[SerializeField] int initialMultiplierOverride = 1;
	
	public override bool needsToExecuteOnRoundStart()
	{
		return true;
	}
		
	public override IEnumerator executeOnRoundStart()
	{
		roundVariantParent.gameParent.currentMultiplier = initialMultiplierOverride;
		roundVariantParent.refreshMultiplierLabel();
		
		yield break;
	}
}
