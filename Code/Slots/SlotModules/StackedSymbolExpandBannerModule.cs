using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StackedSymbolExpandBannerModule : SlotModule
{
	[SerializeField] private string TW_SYMBOL_ARRIVES_SOUND_KEY = "tw_symbol_arrives";
	[SerializeField] private string TW_SYMBOL_LAND_SOUND_KEY = "tw_symbol_land";
	[SerializeField] private string SYMBOL_BANNER_REVEAL_KEY = "tw_symbol_reveal";
	[SerializeField] private float arivalSoundDelay = 0.0f;

	[System.Serializable]
	private class StackedSymbolData
	{
		public string symbolName;
		public string mutateSymbolName;
		public GameObject[] bannerPrefabByReel;
	}

	[SerializeField] private List<StackedSymbolData> stackedSymbolData;
	[SerializeField] private float revealAnimationDuration;
	[SerializeField] private bool shouldHideSymbolsWhileAnimatingReveal = false;

	private StackedSymbolData currentSymbolData;
	public List<TICoroutine> symbolExpandCoroutines = new List<TICoroutine>();

	public override bool needsToExecuteOnSpecificReelStop(SlotReel stoppingReel)
	{
		// check if a mutation should force us to skip this reel
		if (shouldSkipReelBecauseOfMutation(stoppingReel))
		{
			return false;
		}

		// check if symbols are all the same major all the way down
		string symbolName = stoppingReel.visibleSymbols[0].serverName;
		foreach (SlotSymbol slotSymbol in stoppingReel.visibleSymbols)
		{
			if (!slotSymbol.serverName.Equals(symbolName))
			{
				return false;
			}
		}

		foreach (StackedSymbolData symbolData in stackedSymbolData)
		{
			if (symbolData.symbolName.Equals(symbolName))
			{
				currentSymbolData = symbolData;
				break;
			}
		}

		return true;
	}

	public override IEnumerator executeOnSpecificReelStop(SlotReel stoppingReel)
	{
		Audio.playSoundMapOrSoundKey(TW_SYMBOL_LAND_SOUND_KEY);
		GameObject revealObject = currentSymbolData.bannerPrefabByReel[stoppingReel.reelID - 1];
		TICoroutine coroutine = StartCoroutine(mutateSymbolOnReel(stoppingReel, revealObject, currentSymbolData.mutateSymbolName));
		symbolExpandCoroutines.Add(coroutine);
		yield break;
	}

	private IEnumerator mutateSymbolOnReel(SlotReel stoppingReel, GameObject revealObject, string mutateSymbol)
	{
		revealObject.SetActive(true);
		Audio.playSoundMapOrSoundKey(SYMBOL_BANNER_REVEAL_KEY);
		Audio.playSoundMapOrSoundKeyWithDelay(TW_SYMBOL_ARRIVES_SOUND_KEY, arivalSoundDelay);
		if(shouldHideSymbolsWhileAnimatingReveal)
		{
			for(int i = 0; i < stoppingReel.visibleSymbols.Length; i++)
			{
				stoppingReel.visibleSymbols[i].gameObject.SetActive(false);
			}
		}
		yield return new TIWaitForSeconds(revealAnimationDuration);
		if(shouldHideSymbolsWhileAnimatingReveal)
		{
			for(int i = 0; i < stoppingReel.visibleSymbols.Length; i++)
			{
				stoppingReel.visibleSymbols[i].gameObject.SetActive(true);
			}
		}
		stoppingReel.visibleSymbols[0].mutateTo(mutateSymbol);
		revealObject.SetActive(false);
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return symbolExpandCoroutines.Count > 0;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(symbolExpandCoroutines));
		symbolExpandCoroutines.Clear();
	}

	// Some modules can cause problems with this one if they will change a symbol after the reels stop,
	// so we need to make sure a mutation isn't going to change a symbol on the reel before giving it the
	// ok to transform the symbol
	private bool shouldSkipReelBecauseOfMutation(SlotReel reel)
	{
		if (reelGame.mutationManager != null && reelGame.mutationManager.mutations != null && reelGame.mutationManager.mutations.Count > 0)
		{
			foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
			{
				StandardMutation currentMutation = baseMutation as StandardMutation;

				if (currentMutation != null && currentMutation.type == "matrix_cell_replacement")
				{
					foreach (KeyValuePair<int, int[]> mutationKvp in currentMutation.singleSymbolLocations)
					{
						int reelIndex = mutationKvp.Key - 1;
						if (reelIndex == reel.reelID - 1)
						{
							// need to skip expanding on this reel
							return true;
						}
					}
				}
				// NOTE : Add additional checks for other mutations types if they end up causing similar conflicts
			}
		}

		return false;
	}
}