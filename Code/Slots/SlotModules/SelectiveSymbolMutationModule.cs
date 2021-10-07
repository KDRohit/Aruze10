using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Module for mutating selective symbols before outcome cycle display.
/// Currently has layer swapping to keep behavior consistent with OversizedSymbolDisplayModule.
/// </summary>

public class SelectiveSymbolMutationModule : SlotModule {

	public string mutateToAnimationPostfix;

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		reelGame.mutationManager.isLingering = false;

		if (reelGame.mutationManager != null && reelGame.mutationManager.mutations != null && reelGame.mutationManager.mutations.Count > 0)
		{
			foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
			{
				StandardMutation currentMutation = baseMutation as StandardMutation;
				if (currentMutation != null && currentMutation.singleSymbolLocations != null && currentMutation.singleSymbolLocations.Count > 0)
				{
					return true;
				}
			}
		}

		return false;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		List<SymbolMutation> mutations = new List<SymbolMutation>();

		foreach (MutationBase mutationBase in reelGame.mutationManager.mutations)
		{
			StandardMutation currentMutation = mutationBase as StandardMutation;
			if (currentMutation != null && currentMutation.singleSymbolLocations != null && currentMutation.singleSymbolLocations.Count > 0)
			{
				foreach(KeyValuePair<int, int[]> kvp in currentMutation.singleSymbolLocations)
				{
					foreach (int val in kvp.Value)
					{
						SymbolMutation newMutation = new SymbolMutation(kvp.Key, val, currentMutation.replaceSymbol);
						mutations.Add(newMutation);
					}
				}
			}
		}

		mutations = mutations.OrderBy(mut => mut.reelIndex).ThenBy(mut => mut.symbolIndex).ToList();

		if (Audio.canSoundBeMapped("symbol_wild_feature_fx"))
		{
			Audio.play(Audio.soundMap("symbol_wild_feature_fx"));
		}
			
		SlotReel[]reelArray = reelGame.engine.getReelArray();

		foreach (SymbolMutation mutation in mutations)
		{
			int reel = mutation.reelIndex - 1;

			SlotSymbol symbol = reelArray[reel].visibleSymbolsBottomUp[mutation.symbolIndex - 1];

			string name = symbol.name;

			CommonGameObject.setLayerRecursively(symbol.animator.gameObject, Layers.ID_SLOT_REELS);

			foreach (SpriteRenderer sr in symbol.gameObject.GetComponentsInChildren<SpriteRenderer>(true))
			{
				sr.gameObject.layer = Layers.ID_SLOT_REELS;
			}

			symbol.animator.gameObject.layer = Layers.ID_SLOT_FRAME;

			symbol.mutateTo(name + mutateToAnimationPostfix, null, true, true);

			symbol.mutateTo(mutation.mutateTo, (SlotSymbol sender) => 
				{
					sender.animator.gameObject.layer = Layers.ID_SLOT_REELS;
					foreach (SpriteRenderer sr in sender.gameObject.GetComponentsInChildren<SpriteRenderer>(true))
					{
						sr.gameObject.layer = Layers.ID_SLOT_FRAME;
					}
				}
			);

			yield return new WaitForSeconds(0.2f);
		}
		yield return new WaitForSeconds(2.0f);
	}

	private class SymbolMutation
	{
		public string mutateTo;
		public int reelIndex;
		public int symbolIndex;

		public SymbolMutation(int reel, int symbolPos, string mutateValue)
		{
			reelIndex = reel;
			symbolIndex = symbolPos;
			mutateTo = mutateValue;
		}
	}
}
