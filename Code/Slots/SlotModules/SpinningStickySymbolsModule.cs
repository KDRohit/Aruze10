using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpinningStickySymbolsModule : SlotModule 
{
	private List<StandardMutation.ReplacementCell> replacementCells = new List<StandardMutation.ReplacementCell>();
	private StandardMutation.ReplacementCell currentCell;

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return true;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		if (reelGame.mutationManager != null && reelGame.mutationManager.mutations != null && reelGame.mutationManager.mutations.Count > 0)
		{
			foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
			{
				StandardMutation mutation = baseMutation as StandardMutation;

				if (mutation.type == "symbol_locking_with_mutating_symbols")
				{
					foreach (StandardMutation.ReplacementCell newCell in mutation.replacementCells)
					{
						replacementCells.Add(newCell);
					}
				}
			}
		}

		SlotOutcome nextOutcome = reelGame.peekNextOutcome();
		MutationManager	mutationManager = new MutationManager(false);
		if (nextOutcome != null)
		{
			mutationManager.setMutationsFromOutcome(nextOutcome.getJsonObject());
		}

		if (mutationManager != null && mutationManager.mutations != null && mutationManager.mutations.Count > 0)
		{
			foreach (MutationBase baseMutation in mutationManager.mutations)
			{
				StandardMutation mutation = baseMutation as StandardMutation;

				if (mutation.type == "symbol_locking_with_mutating_symbols")
				{
					foreach (StandardMutation.ReplacementCell removedCell in mutation.removedReplacementCells)
					{
						foreach (StandardMutation.ReplacementCell cell in replacementCells)
						{
							if (cell.reelIndex == removedCell.reelIndex && cell.symbolIndex == removedCell.symbolIndex)
							{
								replacementCells.Remove(cell);
								break;
							}
						}
					}

					foreach (StandardMutation.ReplacementCell mutatedCell in mutation.mutatedReplacementCells)
					{
						foreach (StandardMutation.ReplacementCell cell in replacementCells)
						{
							if (cell.reelIndex == mutatedCell.reelIndex && cell.symbolIndex == mutatedCell.symbolIndex)
							{
								cell.replaceSymbol = mutatedCell.replaceSymbol;
							}
						}
					}
				}
			}
		}

		yield break;
	}

	public override bool needsToExecuteAfterSymbolSetup(SlotSymbol symbol)
	{
		SlotReel[]reelArray = reelGame.engine.getReelArray();

		foreach (StandardMutation.ReplacementCell cell in replacementCells)
		{
            //
            // TEMP fix to swap top and bottom row ids for ainsworth04 - currently only game using this module.
            // 
            int position = reelGame.engine.getVisibleSymbolsAt(cell.reelIndex).Length - 1 - cell.symbolIndex;

            if (symbol.reel == reelGame.engine.getSlotReelAt(cell.reelIndex, position, 0))
			{
				currentCell = cell;
				return true;
			}
		}

		return false;
	}

	public override void executeAfterSymbolSetup(SlotSymbol symbol)
	{
		if (!symbol.name.Equals(currentCell.replaceSymbol))
		{
			string debugName = symbol.debugName;
			string debug = "Mutated " + debugName;

			symbol.mutateTo(currentCell.replaceSymbol);

			symbol.debugName = debugName;
			symbol.debug = debug;	
		}
	}
}