using UnityEngine;
using System.Collections;

/**
 * In certain cases, we want to move elements of the hierarchy out of a particular round for later use.
 * This module does so, for cases like progressive panels where they should stick around between rounds.
 */

public class ChallengeGameMigrateHierarchyModule : ChallengeGameModule 
{
	public Transform hierarchyToMove;
	public Transform targetParent;

	public override bool needsToExecuteOnRoundInit()
	{
		return true;
	}

	public override void executeOnRoundInit(ModularChallengeGameVariant round)
	{
		hierarchyToMove.SetParent(targetParent, true);
		base.executeOnRoundInit(round);
	}
}