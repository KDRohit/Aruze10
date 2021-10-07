using UnityEngine;
using System.Collections;

public class StickyMutateMainLineModule : SlotModule 
{
	protected StandardMutation mutations;
	private StandardMutation stickyMutation;
	[SerializeField] private bool mutateOnFirstStop;

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return true;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		MutationManager mutationManager = reelGame.mutationManager;

		bool doMutate = false;

		if (mutationManager.mutations.Count > 0)
		{
			// Let's store the main mutation
			mutations = mutationManager.mutations[0] as StandardMutation;
			if (mutations.reveals != null && mutations.reveals.Count > 0)
			{
				// If there are reveals, then its our first mutation. Let's store it so we can mutate every spin.
				stickyMutation = mutationManager.mutations[0] as StandardMutation;
			}
			else
			{
				doMutate = true;
			}
		}

		if (stickyMutation != null && (doMutate || mutateOnFirstStop) )
		{
			yield return StartCoroutine(mutateMainLine());
		}
	}

	// The main line mutation happens here.	
	protected virtual IEnumerator mutateMainLine()
	{
		SlotReel[]reelArray = reelGame.engine.getReelArray();

		for (int i = 0; i < stickyMutation.triggerSymbolNames.GetLength(0); i++)
		{
			for (int j = 0; j < stickyMutation.triggerSymbolNames.GetLength(1); j++)
			{
				if (stickyMutation.triggerSymbolNames[i,j] != null && stickyMutation.triggerSymbolNames[i,j] != "")
				{
					SlotSymbol symbol = reelArray[i].visibleSymbolsBottomUp[j];
					symbol.mutateTo(stickyMutation.triggerSymbolNames[i,j]);
				}
			}
		}
		yield return null;
	}	

}
