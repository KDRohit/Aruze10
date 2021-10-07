using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * MutateTumble2xModule.cs
 * Author: Nick Reynolds
 * Handles the feature in Tumble Games where a TW symbol causes 2x multipliers to be attached to specified symbols.
 */ 
public class MutateTumbleNxModule : SlotModule 
{

	// inspector variables
	[SerializeField] protected GameObject multiplierPrefab; // prefab to attach to symbols
	[SerializeField] protected Vector3 multiplierPrefabOffset; // local position of prefab after being attached
	
	// timing constants
	[SerializeField] protected float EXTRA_MUTATION_WAIT_TIME; // Time to wait after attaching, enough time for user to see.... stuff
	[SerializeField] protected float MUTATION_PREFAB_ATTACH_INTERVAL_TIME; // Time to wait between starting each attachment process
	[SerializeField] protected float PRE_MUTATION_WAIT_TIME; // Time to after tumble wait before starting anything
	
	protected Dictionary<SlotSymbol, GameObject> multiplierDict = new Dictionary<SlotSymbol, GameObject>();
	protected GameObject twSymbolAttachment; // hold onto this one if we have it. We'll need a reference to it to handle mutations of the TW symbol.
	
	// executeOnPreSpin() section
	// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) before the reels spin
	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnPreSpin()
	{
		destroyAttachedMultipliers();
		yield break;
	}
	
	// we check for mutations on plopping finished
	public override bool needsToExecuteOnPloppingFinished()
	{
		return true;
	}

	// execute mutations. decide which outcome to use, then send off to doMutations()
	public override IEnumerator executeOnPloppingFinished(JSON currentTumbleOutcome, bool useTumble = false)
	{
		if (!useTumble)
		{
			reelGame.mutationManager.setMutationsFromOutcome(reelGame.outcome.getJsonObject());
		}
		else
		{
			reelGame.mutationManager.setMutationsFromOutcome(currentTumbleOutcome);
		}
		yield return StartCoroutine(doMutations());
	}

	
	// This function handles the 2x mutation
	public IEnumerator doMutations()
	{
		if (reelGame.mutationManager.mutations.Count == 0 || !isTWSymbolShowing())
		{
			// No mutations, so do nothing special.
			yield break;
		}
		else
		{
			StandardMutation currentMutation = reelGame.mutationManager.mutations[0] as StandardMutation;
			
			if (currentMutation == null || currentMutation.triggerSymbolNames == null || currentMutation.triggerSymbolNames.Length < 2)
			{
				Debug.LogError("The mutations came down from the server incorrectly. The backend is broken.");
				yield break;
			}
			
			// Introducing a minor delay to the animation to allow for audio to complete.
			if (currentMutation.triggerSymbolNames.GetLength(0) > 0)
			{
				yield return new TIWaitForSeconds(PRE_MUTATION_WAIT_TIME);
			}
			
			yield return StartCoroutine(doChoreographyBeforeAttaching());
			
			for (int i = 0; i < currentMutation.triggerSymbolNames.GetLength(0); i++)
			{
				for (int j = 0; j < currentMutation.triggerSymbolNames.GetLength(1); j++)
				{
					if (currentMutation.triggerSymbolNames[i,j] != null && currentMutation.triggerSymbolNames[i,j] != "")
					{
						yield return StartCoroutine(attachMultiplier(i, j));
					}
				}
			}
		}
		
		yield return new TIWaitForSeconds(EXTRA_MUTATION_WAIT_TIME);
	}

	// do anything special (sounds, animations, etc) before we start attaching
	protected virtual IEnumerator doChoreographyBeforeAttaching()
	{
		yield break;
	}

	// instantiate the multiplier prefab and attach it to the symbol specified by the indecies given
	protected virtual IEnumerator attachMultiplier(int i, int j)
	{
		SlotSymbol symbol = reelGame.engine.getVisibleSymbolClone()[i][j];

		// Make sure the symbol doesn't already have a multiplier attached to it
		// (which might happen if you get the WD spreader on multiple tumbles)
		if (!multiplierDict.ContainsKey(symbol))
		{
			GameObject multiplierAttachment = CommonGameObject.instantiate(multiplierPrefab) as GameObject;
			symbol.animator.addObjectToAnimatorObject(multiplierAttachment);
			multiplierAttachment.transform.localPosition = multiplierPrefabOffset;
			multiplierDict.Add(symbol, multiplierAttachment);

			if (symbol.name.Contains("TW"))
			{
				twSymbolAttachment = multiplierAttachment;
			}
			symbol.name = symbol.name + "-2X";

			yield return new TIWaitForSeconds(MUTATION_PREFAB_ATTACH_INTERVAL_TIME);
		}
	}

	// it's possible that an outcome has mutations associated with it, but the TW symbol hasn't tumbled down yet
	private bool isTWSymbolShowing()
	{
		SlotReel[] reelArray = reelGame.engine.getReelArray();

		for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
		{
			for (int rowIndex = 0; rowIndex < reelArray[reelIndex].visibleSymbols.Length; rowIndex++)
			{
				if(reelGame.engine.getVisibleSymbolClone()[reelIndex][rowIndex].name == "TW")
				{
					return true;
				}
			}
		}
		return false;
	}

	protected SlotSymbol getTWSymbol()
	{
		SlotReel[] reelArray = reelGame.engine.getReelArray();

		for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
		{
			for (int rowIndex = 0; rowIndex < reelArray[reelIndex].visibleSymbols.Length; rowIndex++)
			{
				if(reelGame.engine.getVisibleSymbolClone()[reelIndex][rowIndex].name == "TW")
				{
					return reelGame.engine.getVisibleSymbolClone()[reelIndex][rowIndex];
				}
			}
		}
		return null;
	}

	// we have some cleanup to do on game end
	public override bool needsToExecuteOnFreespinGameEnd()
	{
		return true;
	}

	// cleanup any leftover multiplier prefabs
	public override IEnumerator executeOnFreespinGameEnd()
	{
		destroyAttachedMultipliers();
		yield break;
	}
	
	private void destroyAttachedMultipliers() 
	{
		foreach (KeyValuePair<SlotSymbol, GameObject> kvp in multiplierDict)
		{
			// Check if the rock was already destroyed due to being part of a tumble payout
			if (kvp.Value != null)
			{
				kvp.Key.animator.removeObjectFromSymbol(kvp.Value);
				Destroy(kvp.Value);
			}
		}

		multiplierDict.Clear();
	}
}
