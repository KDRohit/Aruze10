using System;
using System.Collections;
using System.Collections.Generic;
using FeatureOrchestrator;
using UnityEngine;

/*
 * Base class for Boardgame specific modules
 */
public class BoardGameModule : PickingGameRevealModule
{
	[Serializable]
	public class BoardGameTokenData
	{
		public BoardTokenType type;
		public GameObject tokenPrefab;
	}
	public enum BoardTokenType  
	{
		Bell = 0,
		Cherry = 1,
		Horseshoe = 2
	}
	
	protected ModularBoardGameVariant boardGameVariantParent;

	// Overrides HAVE TO call base.executeOnRoundInit!
	public override void executeOnRoundInit(ModularChallengeGameVariant round)
	{
		base.executeOnRoundInit(round);
		boardGameVariantParent = round as ModularBoardGameVariant;
	}

	public virtual bool needsToExecuteOnDataUpdate()
	{
		return false;
	}

	public virtual void executeOnDataUpdate(PickByPickClaimableBonusGameOutcome data)
	{
		
	}

	public virtual bool needsToExecuteOnBoardLoop()
	{
		return false;
	}
	
	public virtual IEnumerator executeOnBoardLoop()
	{
		yield break;
	}
	
	public virtual bool needsToExecuteOnBoardComplete()
	{
		return false;
	}
	
	public virtual IEnumerator executeOnBoardComplete()
	{
		yield break;
	}
}
