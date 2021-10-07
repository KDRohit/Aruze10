using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class NudgingWildsModule : SlotModule 
{
	[SerializeField] private float nudgeDelay = 0.0f;
	[SerializeField] private float nudgeStep = 0.0f;
	[SerializeField] private List<AnimationListController.AnimationInformationList> preNudgeUpAnimations;
	[SerializeField] private List<AnimationListController.AnimationInformationList> preNudgeDownAnimations;

	private StandardMutation mutation = null;
	private List<TICoroutine> nudgeCoroutines;

	public override bool needsToExecutePreReelsStopSpinning()
	{	
		mutation = null;
		nudgeCoroutines = new List<TICoroutine>();

		if (reelGame.mutationManager != null && reelGame.mutationManager.mutations != null &&
			reelGame.mutationManager.mutations.Count > 0)
		{
			foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
			{
				StandardMutation currentMutation = baseMutation as StandardMutation;

				if (currentMutation.type == "nudging_wild")
				{
					mutation = currentMutation;
					break;
				}
			}
		}
		return false;
	}

	public override bool needsToExecuteOnSpecificReelStop(SlotReel stoppingReel)
	{
		return (mutation != null && !stoppingReel.isLocked);
	}

	public override IEnumerator executeOnSpecificReelStop(SlotReel stoppingReel)
	{
		if (System.Array.IndexOf(mutation.reels, stoppingReel.reelID - 1) > -1)
		{
			int index = getNudgingSymbolIndex(stoppingReel);

			if (index > -1)
			{
				int finalIndex = stoppingReel.numberOfTopBufferSymbols;
				int offset = finalIndex - index;

				nudgeCoroutines.Add(StartCoroutine(nudge(stoppingReel, offset)));
			}
			else
			{
				Debug.LogError("Expecting a " + mutation.symbol + " symbol but no such symbol found on reel " + stoppingReel.reelID + " the following symbol is excluded: " + mutation.excludeSymbolKey);
			}
		}

		yield return null;
	}

	// Get the target symbol index that should be nudged (returns -1 if a target symbol can't be found)
	private int getNudgingSymbolIndex(SlotReel stoppingReel)
	{
		int i = 0;
		
		// We only need to go to the bottom of the visible symbols (as anything past there shouldn't
		// be able to nudge (since the top of the large/mega symbol wouldn't be in the visible area)
		while (i < stoppingReel.numberOfTopBufferSymbols + stoppingReel.visibleSymbols.Length)
		{
			SlotSymbol currentSymbol = stoppingReel.symbolList[i];
			int symbolHeight = (int)currentSymbol.getWidthAndHeightOfSymbol().y;
		
			if (currentSymbol.shortServerName == mutation.symbol &&
				(string.IsNullOrEmpty(mutation.excludeSymbolKey) || currentSymbol.name != mutation.excludeSymbolKey))
			{
				// Adding another check here, to not overcomplicate the check above.
				// We need to make sure that this symbol actually overlaps the visible symbols
				// (it is possible that there might be multiple nudgeable symbols, but we only want
				// to care about ones that overlap the visible area)
				if (currentSymbol.isVisible(true, true))
				{
					return i;
				}
			}
			
			// Need to increment by the height of the symbol (since we don't need to check every symbol part)
			i += symbolHeight;
		}

		return -1;
	}

	private IEnumerator nudge(SlotReel reel, int offset)
	{
		float pos = 0.0f;

		if (offset > 0)
		{
			if (preNudgeDownAnimations.Count > reel.reelID - 1)
			{
				TICoroutine animCoroutine =
					StartCoroutine(AnimationListController.playListOfAnimationInformation(preNudgeDownAnimations[reel.reelID - 1]));

				nudgeCoroutines.Add(animCoroutine);
			}

			yield return new TIWaitForSeconds(nudgeDelay);

			// Handles the visual slide of the reel
			while (pos < offset)
			{
				reel.slideSymbols(pos);
				pos += nudgeStep;
				yield return null;

				// Handles the logical slide of the reel on each step (1 unit)
				while (pos > 1)
				{
					reel.advanceSymbols(SlotReel.ESpinDirection.Down);
					pos--;
					offset--;
				}
			}
		}
		else if (offset < 0)
		{
			if (preNudgeUpAnimations.Count > reel.reelID - 1)
			{
				TICoroutine animCoroutine =
					StartCoroutine(AnimationListController.playListOfAnimationInformation(preNudgeUpAnimations[reel.reelID - 1]));

				nudgeCoroutines.Add(animCoroutine);
			}

			yield return new TIWaitForSeconds(nudgeDelay);

			// Handles the visual slide of the reel
			while (pos > offset)
			{
				reel.slideSymbols(pos);
				pos -= nudgeStep;
				yield return null;

				// Handles the logical slide of the reel on each step (1 unit)
				while (pos < -1)
				{
					reel.advanceSymbols(SlotReel.ESpinDirection.Up);
					pos++;
					offset++;
				}
			}
		}

		// Manage floating point error
		reel.slideSymbols(0.0f);

		yield return null;
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return nudgeCoroutines.Count > 0;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(nudgeCoroutines));
	} 
}
