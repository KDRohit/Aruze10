using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
	Display the initial multiplier as a different multiplier (eg "1X" instead of "0X").
	Used in ghostbusters01.
*/

public class ChallengeGameOverrideInitialMultiplierLabelOnInitModule : ChallengeGameModule
{
	[SerializeField] int initialMultiplierDisplayOverride = 1;
	
	public override bool needsToExecuteOnRoundInit()
	{
		return true;
	}
		
	public override void executeOnRoundInit(ModularChallengeGameVariant round)
	{
		round.multiplierLabel.text = Localize.text("{0}X", initialMultiplierDisplayOverride);
	}
}
