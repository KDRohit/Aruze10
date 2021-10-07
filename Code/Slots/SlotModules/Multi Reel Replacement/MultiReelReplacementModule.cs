using UnityEngine;
using System.Collections;

public class MultiReelReplacementModule : SlotModule 
{
	[SerializeField] protected PresentOrderEnum presentType = PresentOrderEnum.OnReelsStop;

	protected StandardMutation featureMutation = null;

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
			if (mutation.type == "multi_reel_replacement" || mutation.type == SlotOutcome.REEVALUATION_TYPE_SPOTLIGHT)
			{
				featureMutation = mutation as StandardMutation;
				System.Array.Sort(featureMutation.reels);
				return true;
			}
			else if (mutation.type == "multi_reel_advanced_replacement")
			{
				featureMutation = mutation as StandardMutation;
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

		if (featureMutation.type == "multi_reel_advanced_replacement") // new mutation style
		{
			for (int i = 0; i < featureMutation.mutatedReels.Length; i++)
			{
				for (int j = 0; j < featureMutation.mutatedReels[i].Length; j++)
				{
					foreach (SlotSymbol symbol in reelGame.engine.getVisibleSymbolsAt(featureMutation.mutatedReels[i][j]))
					{	
						if (symbol.name != featureMutation.symbol)
						{
							symbol.mutateTo(featureMutation.symbol);
						}
					}
				}
			}
		}
		else
		{
			foreach (int reelID in featureMutation.reels)
			{
				foreach (SlotSymbol symbol in reelGame.engine.getVisibleSymbolsAt(reelID))
				{
					if (symbol.name != featureMutation.symbol)
					{
						symbol.mutateTo(featureMutation.symbol);
					}
				}
			}
		}
	}

	public override bool needsToExecutePreReelsStopSpinning()
	{
		foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
		{
			StandardMutation mutation = baseMutation as StandardMutation;
			if (mutation.type == "multi_reel_replacement" || mutation.type == SlotOutcome.REEVALUATION_TYPE_SPOTLIGHT)
			{
				featureMutation = mutation;
				System.Array.Sort(featureMutation.reels);
				return true && (presentType == PresentOrderEnum.DuringReelSpin);
			}
			else if (mutation.type == "multi_reel_advanced_replacement")
			{
				featureMutation = mutation;
				return true && (presentType == PresentOrderEnum.DuringReelSpin);
			}
		}

		return false;
	}
}
