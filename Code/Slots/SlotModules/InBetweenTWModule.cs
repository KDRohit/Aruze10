using UnityEngine;
using System.Collections;


/**
* InBetweenTWModule.cs
* Joel Gallant - 2017-09-06
* Originally created for ainsworth10 - Enchanted Island (Super Free Spins)
* When two matching symbols (TR2) land, all rows with or between symbols become wild
*/
public class InBetweenTWModule : SlotModule
{
	[SerializeField] private float delayBeforeMutation = 0.25f;
	
	[SerializeField] private ParticleTrailController transformEffectTrail;
	[SerializeField] private float trailTime = 1.0f;
	
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		for (int i = 0; i < reelGame.mutationManager.mutations.Count; i++)
		{
			MutationBase mutation = reelGame.mutationManager.mutations[i];
			if (mutation.type == "linking_wilds") // linking_wilds tag on server side
			{
				return true;
			}
		}
		
		// no inbetween mutation found
		return false;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		// find the appropriate mutation
		StandardMutation inBetweenMutation = null;
		for (int i = 0; i < reelGame.mutationManager.mutations.Count; i++)
		{
			MutationBase mutation = reelGame.mutationManager.mutations[i];
			if (mutation.type == "linking_wilds") // linking_wilds tag on server side
			{
				inBetweenMutation = mutation as StandardMutation;
			}
		}
		
		SlotReel[] reelArray = reelGame.engine.getReelArray();

		// find the minimum & maximum reel indexes to get the bounds of the effect
		int START_SYMBOL_REEL_INDEX = 0;
		int END_SYMBOL_REEL_INDEX = 0;
		for (int i = 0; i < inBetweenMutation.twMutatedSymbolList.Count; i++)
		{
			StandardMutation.ReplacementCell mutateCell = inBetweenMutation.twMutatedSymbolList[i];
			if (mutateCell.reelIndex < START_SYMBOL_REEL_INDEX)
			{
				START_SYMBOL_REEL_INDEX = mutateCell.reelIndex;
			}
			if (mutateCell.reelIndex > END_SYMBOL_REEL_INDEX)
			{
				END_SYMBOL_REEL_INDEX = mutateCell.reelIndex;
			}
		}


		// need to fly a compass between two symbols
		SlotSymbol startSymbol = null;
		SlotSymbol endSymbol = null;
		
		// outcome only returns the trigger symbol by name, not a full ReplacementCell
		// animate the symbols triggering the mutation
		for (int i = 0; i < inBetweenMutation.twTriggeredSymbolList.Count; i++)
		{
			StandardMutation.ReplacementCell triggerCell = inBetweenMutation.twTriggeredSymbolList[i];
			SlotSymbol triggerSymbol = reelArray[triggerCell.reelIndex].visibleSymbolsBottomUp[triggerCell.symbolIndex];
			triggerSymbol.animateOutcome();
			
			if (triggerSymbol.reel.reelID == START_SYMBOL_REEL_INDEX)
			{
				startSymbol = triggerSymbol;
			}
			else if (triggerSymbol.reel.reelID == END_SYMBOL_REEL_INDEX)
			{
				endSymbol = triggerSymbol;
			}
		}
		
		// fallback if twTriggerSymbolList does not contain full cell information
		if (startSymbol == null || endSymbol == null)
		{
			string TW_TRIGGER_SYMBOL = inBetweenMutation.triggerSymbolName;
			for (int i = 0; i < reelGame.engine.getVisibleSymbolsAt(START_SYMBOL_REEL_INDEX).Length; i++)
			{
				SlotSymbol reelSymbol = reelGame.engine.getVisibleSymbolsAt(START_SYMBOL_REEL_INDEX)[i];
				if (reelSymbol.shortServerName == TW_TRIGGER_SYMBOL)
				{
					startSymbol = reelSymbol;
				}
			}
			for (int i = 0; i < reelGame.engine.getVisibleSymbolsAt(END_SYMBOL_REEL_INDEX).Length; i++)
			{
				SlotSymbol reelSymbol = reelGame.engine.getVisibleSymbolsAt(END_SYMBOL_REEL_INDEX)[i];
				if (reelSymbol.shortServerName == TW_TRIGGER_SYMBOL)
				{
					endSymbol = reelSymbol;
				}
			}
		}

		// if we STILL don't have the required symbols, abort
		if (startSymbol == null || endSymbol == null)
		{
			Debug.LogError("Start and end trigger symbols not found - aborting InBetweenTWModule wild population!");
			yield break;
		}
		
		// spawn a compass trail at the symbol start location, flying to the end symbol
		StartCoroutine(transformEffectTrail.animateParticleTrail(startSymbol.transform.position, endSymbol.transform.position, transform));
		
		// slight delay in order to allow for the center of the trail effect to pass over slightly 
		if (delayBeforeMutation > 0.0f)
		{
			yield return new WaitForSeconds(delayBeforeMutation);			
		}
		
		// mutate columns between the trigger symbols based on the overall trail time.
		float columnDelayTime = trailTime / (END_SYMBOL_REEL_INDEX - START_SYMBOL_REEL_INDEX);
		for (int i = START_SYMBOL_REEL_INDEX; i <= END_SYMBOL_REEL_INDEX; i++)
		{
			// mutate the appropriate between symbols
			for (int j = 0; j < inBetweenMutation.twMutatedSymbolList.Count; j++)
			{
				StandardMutation.ReplacementCell mutateCell = inBetweenMutation.twMutatedSymbolList[j];
				// if this symbol is on the appropriate reel, mutate it
				if (mutateCell.reelIndex == i)
				{
					SlotSymbol mutateSymbol = reelArray[mutateCell.reelIndex].visibleSymbolsBottomUp[mutateCell.symbolIndex];
					mutateSymbol.mutateTo(mutateCell.replaceSymbol);					
				}
			}
			yield return new WaitForSeconds(columnDelayTime);
		}		
	}

}
