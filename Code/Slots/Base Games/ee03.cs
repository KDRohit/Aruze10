using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ee03 game class. We need this for the special wild mutations we do from ice->fire
public class ee03 : SlotBaseGame
{

	private const float SMALL_MUTATE_WAIT = 0.75f;								// how long to wait before doing small symbol mutations
	private const float BIG_MUTATE_WAIT = 1.5f;									// how long to wait after doing large symbol mutations
	private const float ALL_MUTATIONS_WAIT = 1.75f;								// how long to wait before continuing (start timer when we start doing mutations)

	// Place to implement all of the custom overrides for this game.
	// when the reels stop, setup a couple of things and the launch the WD splitting co-routine 
	protected override void reelsStoppedCallback ()
	{
		StartCoroutine(splitOffscreenWilds());
	}
	
	// split up WD symbols that are part of a win but not wholly on screen
	// Then start the mutations
	private IEnumerator splitOffscreenWilds()
	{
		bool foundMutations = false;

		HashSet<SlotSymbol> winningSymbols = outcomeDisplayController.getSetOfWinningSymbols(outcome);

		foreach (SlotSymbol symbol in winningSymbols)
		{
			if (symbol.name.Contains("WD") && !symbol.isWhollyOnScreen)
			{
				foundMutations = true;
				symbol.splitSymbol();
			}
		}

		if (foundMutations) // wait a few frames for the split to be obvious
		{
			yield return null;
			yield return null;
			yield return null;
		}

		// after splitting symbols, now we can do the mutations
		doSpecialWildMutations(winningSymbols);
	}

	// mutate all WDs (ice) to their fire counterparts
	private void doSpecialWildMutations (HashSet<SlotSymbol> winningSymbols)
	{
		bool foundMutations = false;

		foreach (SlotSymbol symbol in winningSymbols)
		{
			if (symbol.name.Contains("WD-2A-2A"))
			{
				foundMutations = true;
				StartCoroutine(playAnimThenMutate(symbol,"WD_FIRE-2A-2A"));
			}
			else if (symbol.name.Contains("WD-4A-3A"))
			{
				foundMutations = true;
				StartCoroutine(playAnimThenMutate(symbol,"WD_FIRE-4A-3A"));
			}
			else if(symbol.name == "WD")
			{
				foundMutations = true;
				StartCoroutine(delayThenMutateSmallSymbol(symbol));
			}
		}

		if (foundMutations)
		{
			StartCoroutine(waitForMutationsThenCallback());
		}
		else
		{
			base.reelsStoppedCallback ();
		}
	}

	// wait a tiny bit before mutating the small symbols (so they line up with the big symbol mutations)
	private IEnumerator delayThenMutateSmallSymbol(SlotSymbol symbol)
	{
		yield return new TIWaitForSeconds(SMALL_MUTATE_WAIT);		
		symbol.mutateTo(symbol.name + "_FIRE");
	}

	// play the outcome animation for the big symbol, then mutate it to the fire version
	private IEnumerator playAnimThenMutate(SlotSymbol symbol, string mutateTo)
	{
		symbol.animator.playOutcome(symbol);

		yield return new TIWaitForSeconds(BIG_MUTATE_WAIT);
		symbol.mutateTo(mutateTo);
	}

	// wait for mutations to happen, then continue on with outcome displaying
	private IEnumerator waitForMutationsThenCallback()
	{
		yield return new TIWaitForSeconds(ALL_MUTATIONS_WAIT);
		base.reelsStoppedCallback ();
	}
}
