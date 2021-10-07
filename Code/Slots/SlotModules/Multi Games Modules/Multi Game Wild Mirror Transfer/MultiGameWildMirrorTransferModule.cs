using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
* MultiGameWildMirrorTransferModule.cs
* This module handles the wild mirror mutation in Multi games like batman01
* WDs are transferred from one reel set to another in a fashion that resembles a mirror reflection of the reel sets
* Author: Nick Reynolds & Stephen Arredondo
*/

public class MultiGameWildMirrorTransferModule : SlotModule 
{
	private const string WD_SYMBOL_VO_KEY = "TW_symbol_vo";

	protected bool transferringWD = false;
	protected int numberOfTransfers = 0;
	[SerializeField] protected float TIME_BETWEEN_WD_TRANSFERS = 0.0f;
	protected List<SlotSymbol> symbolsToMutate = new List<SlotSymbol>();

	public override bool needsToExecuteOnSpecificReelStop(SlotReel stoppedReel)
	{
		foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
		{
			PaperFoldMutation mut = baseMutation as PaperFoldMutation;
			if (mut != null && mut.paperFolds != null && mut.paperFolds.Count > 0)
			{
				foreach (PaperFoldMutation.PaperFold paperfold in mut.paperFolds)
				{
					if ((paperfold.fromReelID == stoppedReel.reelID-1 && paperfold.fromGame == stoppedReel.layer) 
						|| (paperfold.toReelID == stoppedReel.reelID-1 && paperfold.toGame == stoppedReel.layer))
					{
						return true;
					}
				}
			}
		}
		
		return false;
	}
	
	public override IEnumerator executeOnSpecificReelStop(SlotReel stoppedReel)
	{
		// Alter the debug symbol names now so we don't have a symbol mismatch exception
		foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
		{
			PaperFoldMutation mut = baseMutation as PaperFoldMutation;
			if (mut != null && mut.paperFolds != null && mut.paperFolds.Count > 0)
			{
				if (Audio.canSoundBeMapped(WD_SYMBOL_VO_KEY))
				{
					Audio.play(Audio.soundMap(WD_SYMBOL_VO_KEY));
				}
				foreach (PaperFoldMutation.PaperFold mutation in mut.paperFolds)
				{
					if (mutation.fromReelID == stoppedReel.reelID-1 && stoppedReel.layer == mutation.fromGame) //We only want to transfer the wilds from this specific reel and layer
					{
						SlotSymbol[] toReelsSymbols = reelGame.engine.getVisibleSymbolsAt(mutation.toReelID, mutation.toGame);
						SlotSymbol symbolToMutate = toReelsSymbols[toReelsSymbols.Length - mutation.rowID - 1];
						string[] finalSymbols = reelGame.engine.getSlotReelAt(mutation.toReelID, -1, mutation.toGame).getFinalReelStopsSymbolNames();
						string finalSymbolName = finalSymbols[mutation.rowID];
						if(!SlotSymbol.isWildSymbolFromName(finalSymbolName)) //Only transfer symbols if there isn't already a WD on the opposite side
						{
							Vector3 targetPosition = reelGame.engine.getReelRootsAt (mutation.toReelID, mutation.rowID, mutation.toGame).transform.position;
							targetPosition = new Vector3 (targetPosition.x, targetPosition.y + (mutation.rowID * reelGame.symbolVerticalSpacingWorld), targetPosition.z);

							// changing this name early so we don't cause a mismatch exception
							symbolToMutate.debugName = "WD";

							SlotSymbol[] fromReelssymbols = reelGame.engine.getVisibleSymbolsAt(mutation.fromReelID, mutation.fromGame);
							SlotSymbol landedWD = fromReelssymbols[fromReelssymbols.Length - mutation.rowID - 1];
							StartCoroutine(doWildsTransfer(landedWD, symbolToMutate, targetPosition));

							yield return new TIWaitForSeconds(TIME_BETWEEN_WD_TRANSFERS);
						}
						else
						{
							--numberOfTransfers;
						}
					}
					else if (mutation.toReelID == stoppedReel.reelID-1 && stoppedReel.layer == mutation.toGame) //If the stopped reel has stuff we need to mutate then lets add it to the list
					{
						SlotSymbol[] toReelsSymbols = reelGame.engine.getVisibleSymbolsAt(mutation.toReelID, mutation.toGame);
						SlotSymbol symbolToMutate = toReelsSymbols[toReelsSymbols.Length - mutation.rowID - 1];
						symbolsToMutate.Add(symbolToMutate);
					}
				}
			}
		}
		
		yield break;
	}
	
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
		{
			PaperFoldMutation mut = baseMutation as PaperFoldMutation;
			if (mut != null && mut.paperFolds != null && mut.paperFolds.Count > 0)
			{
				return true;
			}
		}

		return false;
	}
	
	public override IEnumerator executeOnReelsStoppedCallback()
	{
		foreach (SlotSymbol symbol in symbolsToMutate)
		{
			if (symbol.canBeSplit() && symbol.isTallSymbolPart) //If we're transferring to a large symbol, break it apart and regrab the new 1x1 symbol we want to mutate
			{
				symbol.splitSymbol();
			}
			symbol.mutateTo("WD");
		}
		symbolsToMutate.Clear();
		yield return null;
	}

	protected virtual IEnumerator doWildsTransfer(SlotSymbol fromSymbol, SlotSymbol toSymbol, Vector3 targetPosition)
	{
		yield return null;
	}

	public override bool needsToExecutePreReelsStopSpinning ()
	{
		foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
		{
			PaperFoldMutation mut = baseMutation as PaperFoldMutation;
			if (mut != null && mut.paperFolds != null && mut.paperFolds.Count > 0)
			{
				return true;
			}
		}
		return false;
	}

	public override IEnumerator executePreReelsStopSpinning ()
	{
		foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
		{
			PaperFoldMutation mut = baseMutation as PaperFoldMutation;
			if(mut.paperFolds.Count > 0 && numberOfTransfers == 0)
			{
				numberOfTransfers = mut.paperFolds.Count;
			}
		}
		yield break;
	}
}
