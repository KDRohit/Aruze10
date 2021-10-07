using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Satc03 : MultiSlotBaseGame 
{
	private const int NUMBER_OF_SYMBOLS_TO_CACHE_AT_ONCE = 25;
	private const int NUMBER_OF_TALL_SYMBOLS_TO_CACHE_AT_ONCE = 20;
	//private string[] symbolsToCache = new string[]{"M1", "M2", "M3", "M4", "F4", "F5", "F6", "F7", "F8"};
	//private string[] tallSymbolsToCache = new string[]{"M1-3A", "M2-3A", "M3-3A", "M4-3A"};

	protected override IEnumerator finishLoading(JSON slotGameStartedData)
	{
		// Load in all of the symbols we might need for this game. We may need more WD's, but that's pretty unlikely.
		/**foreach (string symbolName in symbolsToCache)
		{
			yield return StartCoroutine(cacheSymbolsToPoolCoroutine(symbolName, 25, NUMBER_OF_SYMBOLS_TO_CACHE_AT_ONCE));
		}
		foreach (string symbolName in tallSymbolsToCache)
		{
			yield return StartCoroutine(cacheSymbolsToPoolCoroutine(symbolName, 20, NUMBER_OF_TALL_SYMBOLS_TO_CACHE_AT_ONCE));
		}
		yield return StartCoroutine(cacheSymbolsToPoolCoroutine("WD", 15, 15));
		yield return StartCoroutine(cacheSymbolsToPoolCoroutine("BN", 9, 9));*/
		yield return StartCoroutine(base.finishLoading(slotGameStartedData));
	}

	public override void setOutcome(SlotOutcome outcome)
	{
		base.setOutcome(outcome);
		mutationManager.setMutationsFromOutcome(_outcome.getJsonObject(), true);
	}
}
