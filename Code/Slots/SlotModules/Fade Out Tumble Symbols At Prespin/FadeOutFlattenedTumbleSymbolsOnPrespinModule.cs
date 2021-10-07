using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FadeOutFlattenedTumbleSymbolsOnPrespinModule : SlotModule
{
	/*
	 * Module to handle fading out symbols in a Tumble Game. 
	 * The fade happens when the SPIN button is pressed and requires the game to be using flattened symbols to ensure proper fading
	*/

	[SerializeField] private float MAX_FADE_TIME = 1.0f;
	private bool waitingForResponse = true;
	private bool finishingFade = false;

	public override bool needsToExecuteOnBeginRollback(SlotReel reel)
	{
		return true;
	}

	public override IEnumerator executeOnBeginRollback(SlotReel reel)
	{
		waitingForResponse = true;
		finishingFade = true;
		List<SlotSymbol> activeSymbols = reel.symbolList;
		List<TICoroutine> runningCoroutines = new List<TICoroutine>();
		for (int i = 0; i < activeSymbols.Count; i++)
		{
			if (!activeSymbols[i].isFlattenedSymbol)
			{
				activeSymbols[i].mutateToFlattenedVersion();
			}
			runningCoroutines.Add(StartCoroutine(activeSymbols[i].fadeOutSymbolCoroutine(MAX_FADE_TIME)));
		}

		float elapsedTime = 0.0f;

		while (waitingForResponse && elapsedTime < MAX_FADE_TIME) //Keep fading for our max duration unless we've gotten an outcome back from the server
		{
			elapsedTime += Time.deltaTime;
			yield return null;
		}

		if (elapsedTime < MAX_FADE_TIME) //If we got an outcome from the server but are in the middle of fading, lets manually stop everything so we can start spinning immediately
		{
			for (int i = 0; i < activeSymbols.Count; i++)
			{
				activeSymbols[i].animator.haltFade();
				StopCoroutine(runningCoroutines[i]); //Need to stop the fadeOutSymbolCoroutine thats attached to this module along with the ones in the symbol animator
			}
		}
		finishingFade = false;
	}

	public override bool needsToExecutePreReelsStopSpinning()
	{
		return true;
	}

	public override IEnumerator executePreReelsStopSpinning()
	{
		waitingForResponse = false; //If our reels are ready to stop then we've already set the outcome
		while (finishingFade) //Don't release this coroutine until all of our fade stuff started on the beginRollback hook
		{
			yield return null;
		}
	}

// executeOnReelsSpinning() section
// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) immediately after the reels start spinning
	public override bool needsToExecuteOnReelsSpinning()
	{
		return reelGame.isFreeSpinGame() || (reelGame.isDoingFreespinsInBasegame());
	}
	
	public override IEnumerator executeOnReelsSpinning()
	{
		// force the game to delay during freespins so the symbols do actually fade between spins
		yield return new TIWaitForSeconds(MAX_FADE_TIME);
	}
}
