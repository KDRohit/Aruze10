using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChallengeGameStopOutcomeDisplayOnStartRoundModule : ChallengeGameModule
{
	[Tooltip("By default, will stop on RoundInit")]
	[SerializeField] private bool stopOnRoundStart = false;

	public override bool needsToExecuteOnRoundInit()
	{
		return !stopOnRoundStart;
	}
		
	public override void executeOnRoundInit(ModularChallengeGameVariant round)
	{
		ReelGame.activeGame.outcomeDisplayController.clearOutcome();
	}
		
	public override bool needsToExecuteOnRoundStart()
	{
		return stopOnRoundStart;
	}
		
	public override IEnumerator executeOnRoundStart()
	{
		ReelGame.activeGame.outcomeDisplayController.clearOutcome();

		yield break;
	}
}