using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SymbolExpansionReelReplacementModule : SlotModule 
{
	[SerializeField] private float POST_REEL_EXPAND_DELAY;
	protected int lastReel = -1;
// executeOnReelsStoppedCallback() section
// functions in this section are accessed by reelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		// Go through the mutations from the spin and see if there is one for this type of mutation.
		if (reelGame.mutationManager != null && reelGame.mutationManager.mutations != null)
		{
			foreach (MutationBase mutation in reelGame.mutationManager.mutations)
			{
				if (mutation.type == "symbol_expansion_reel_replacement")
				{
					return true;
				}
			}
		}
		else
		{
			Debug.LogError("Mutation manager not properly set up.");
		}
		return false;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		StandardMutation symbolExpansionMutation = null;
		if (reelGame.mutationManager != null && reelGame.mutationManager.mutations != null)
		{
			foreach (MutationBase mutation in reelGame.mutationManager.mutations)
			{
				if (mutation.type == "symbol_expansion_reel_replacement")
				{
					symbolExpansionMutation = mutation as StandardMutation;
					break;
				}
			}
		}

		if (symbolExpansionMutation != null)
		{
			yield return StartCoroutine(expandReelFromMutation(symbolExpansionMutation));
		}
		else
		{
			Debug.LogError("No symbol_expansion_reel_replacement type found for module");
		}
	}

	protected virtual IEnumerator expandReelFromMutation(StandardMutation mutation)
	{
		if (mutation == null)
		{
			Debug.LogError("No mutations sent to expand reel with.");
			yield break;
		}
		foreach (int reelID in mutation.reels)
		{
			if (reelID != 4)
			{
				lastReel = reelID;
			}
		}
		foreach (int reelID in mutation.reels)
		{
			yield return StartCoroutine(expandReelAt(reelID));
			yield return new TIWaitForSeconds(POST_REEL_EXPAND_DELAY);
		}
		
		lastReel = -1;
	}

	protected virtual IEnumerator expandReelAt(int reelID)
	{
		foreach (SlotSymbol symbol in reelGame.engine.getVisibleSymbolsAt(reelID))
		{
			symbol.mutateTo("WD");
		}
		yield break;
	}

}
