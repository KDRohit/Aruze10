using UnityEngine;
using System.Collections;

public class Aruze10SymbolOverlayReplacementModule : SlotModule
{
	[SerializeField] protected PresentOrderEnum presentType = PresentOrderEnum.OnReelsStop;
	protected StandardMutation featureMutation = null;

	
	public enum GameTypeEnum
	{
		BaseGame = 0,
		FreeSpinGame = 1
	}


	public enum PresentOrderEnum
	{
		DuringReelSpin = 0,
		OnReelsStop = 1
	}

	// executeOnReevaluationReelsStoppedCallback() section
	// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsStoppedCallback()
	{

		foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
		{
			StandardMutation mutation = baseMutation as StandardMutation;
			if (mutation.type == "symbol_overlay_replacement" || mutation.type == SlotOutcome.REEVALUATION_TYPE_SPOTLIGHT)
			{
				featureMutation = mutation as StandardMutation;
				System.Array.Sort(featureMutation.reels);
				return true;
			}

		}
		return false;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		
		if (featureMutation == null)
		{
			Debug.LogError("Trying to execute module on invalid data.");
			yield break;
		}
		else
		{
			foreach (StandardMutation.ReplacementCell RPCell in featureMutation.replacementCells)
			{
		
				foreach (SlotSymbol symbol in reelGame.engine.getVisibleSymbolsAt(RPCell.reelIndex-1))
				{
					if (symbol.visibleSymbolIndex == RPCell.symbolIndex-1)
					{
						symbol.mutateTo(RPCell.replaceSymbol);
					}
				}
			}
			reelGame.mutationManager.mutations.Clear();

		}

	}
	public override bool needsToExecutePreReelsStopSpinning()
	{
		foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
		{
			StandardMutation mutation = baseMutation as StandardMutation;
			if (mutation.type == "symbol_overlay_replacement" || mutation.type == SlotOutcome.REEVALUATION_TYPE_SPOTLIGHT)
			{
				featureMutation = mutation;
				System.Array.Sort(featureMutation.reels);
				return true && (presentType == PresentOrderEnum.DuringReelSpin);
			}
		}

		return false;
	}
}
