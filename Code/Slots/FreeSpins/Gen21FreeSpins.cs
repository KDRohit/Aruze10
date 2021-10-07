using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
The gen21 freespins class.
 */ 
public class Gen21FreeSpins : TumbleFreeSpinGame 
{
	public string retriggerSound;
	public float retriggerSoundDelay = 0f;

	public override IEnumerator showUpdatedSpins(int numberOfSpins)
	{
		int prevNumberOfSpins = numberOfFreespinsRemaining;

		if(!string.IsNullOrEmpty(retriggerSound))
		{
			Audio.playSoundMapOrSoundKeyWithDelay(retriggerSound, retriggerSoundDelay);
		}
		
		yield return StartCoroutine(base.showUpdatedSpins(numberOfSpins));

		// base.showUpdatedSpins does not update autoSpins becauase free spin animation does not have a AnimatorFreespinEffect
		// or a FreeSpinEffect component. So do it ourselves and check against prevNumberOfSpins in case one of these components ever gets added
		// and base.showUpdatedSpins(numberOfSpins  actually adds the spins
		// grant free spin modules do not work with tumble games either
		if (numberOfFreespinsRemaining == prevNumberOfSpins) 
		{
			numberOfFreespinsRemaining += numberOfSpins;
		}

		yield return null;
	}


	protected override void beginFreeSpinMusic()
 	{
 		// Even though we came from a transition, we still want to play this sound.
 		// The VO is handled in the base game animateAllBonusSymbols during the transition.
 		Audio.switchMusicKeyImmediate(Audio.soundMap("freespin"), 0.0f);
 	}

	protected override IEnumerator prespin()
	{	
		if (isGameUsingOptimizedFlattenedSymbols && visibleSymbolClone != null)
		{
			SlotReel[] reelArray = engine.getReelArray();

			// go through the  symbols and make sure they are flattened for fading
			for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
			{
				SlotSymbol[] visibleSymbols = reelArray[reelIndex].visibleSymbols;

				for (int symbolIndex = 0; symbolIndex < visibleSymbols.Length; symbolIndex++)
				{
					flattenSymbolWithRestore(visibleSymbolClone[reelIndex][reelArray[reelIndex].visibleSymbols.Length-symbolIndex-1]);
				}
			}
		}

		yield return StartCoroutine(base.prespin());

	}	

	protected override IEnumerator plopNewSymbols()
	{
		// check which symbols need to be unflattened before animating in a winning payline
		// it needs to be done before any tumble movement happens since the flatten will reset the symbol
		// to its original position
		if (isGameUsingOptimizedFlattenedSymbols)
		{
			unFlattenWinningSymbols();
			unflattenBonusSymbols();
		}

		yield return StartCoroutine(base.plopNewSymbols());

	}

	/// reelsStoppedCallback - called when all reels have come to a stop.
	protected override void reelsStoppedCallback()
	{
		// check which symbols need to be unflattened before animating in a winning payline
		// it needs to be done before any tumble movement happens since the flatten will reset the symbol
		// to its original position
		if (isGameUsingOptimizedFlattenedSymbols)
		{
			unFlattenWinningSymbols();
			unflattenBonusSymbols();
		}

		StartCoroutine(plopSymbols());
	}

	private void flattenSymbolWithRestore(SlotSymbol symbol)
	{
		if (!symbol.isFlattenedSymbol)
		{
			Vector3 originalPosition = symbol.transform.position;
			symbol.mutateToFlattenedVersion(null, false, true, false);
			symbol.transform.position = originalPosition;  // restore position changed by mutation
		}
	}

	private void unFlattenWithRestore(SlotSymbol symbol)
	{
		if (symbol.isFlattenedSymbol)
		{
			Vector3 originalPosition = symbol.transform.position;
			symbol.mutateTo(symbol.serverName, null, false, true);
			symbol.transform.position = originalPosition;  // restore position changed by mutation
		}
	}	

	protected void unflattenBonusSymbols()
	{
		SlotReel[] reelArray = engine.getReelArray();

		// check for bonus symbols to unflatten since they always animate winning payline or not
		for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
		{
			SlotSymbol[] visibleSymbols = reelArray[reelIndex].visibleSymbols;
			for (int symbolIndex = 0; symbolIndex < visibleSymbols.Length; symbolIndex++)
			{
				SlotSymbol symbol = visibleSymbols[symbolIndex];	
				if (symbol.isBonusSymbol && symbol.isFlattenedSymbol)
				{
					unFlattenWithRestore(symbol);
				}
			}
		}

	}

	private void unFlattenWinningSymbols()
	{
		HashSet<SlotSymbol> winningSymbols = outcomeDisplayController.getSetOfWinningSymbols(_outcome);

		foreach (SlotSymbol symbol in winningSymbols)
		{
			if (symbol.isMajor || symbol.isWildSymbol)   // only majors  and wilds animate in gen21
			{
				unFlattenWithRestore(symbol);
			}
		}		
	}		
 	
}
