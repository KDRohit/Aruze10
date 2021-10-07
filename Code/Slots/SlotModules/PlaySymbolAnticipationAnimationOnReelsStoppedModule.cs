using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This module allows you to play arbitrary animations on symbols on a reelstop regardless of outcome.
//	It can optionaly block respins until the animation is complete.
public class PlaySymbolAnticipationAnimationOnReelsStoppedModule : SlotModule 
{
	// Serializable dictionary for the class data kept for each symbol
	[System.Serializable] private class KeyValueIntToSymbolActionList : CommonDataStructures.SerializableKeyValuePair<int, List<SymbolAction>> {}
	[System.Serializable] private class SerializableDictionaryOfIntToSymbolActionList : CommonDataStructures.SerializableDictionary<KeyValueIntToSymbolActionList, int, List<SymbolAction>> {}
	[SerializeField] private SerializableDictionaryOfIntToSymbolActionList reelToSymbolAction;

	// Flag this as true if you don't want the game to keep spinning until all of the symbols are done
	//	animating.
	[SerializeField] private bool shouldBlockUntilAnimationsFinish = false;
	
	[SerializeField] private const string MUTATION_OUTCOME_TARGET = "trigger_replace_multi";

	// When a symbol is going to play its animation, search this list for sounds to play for that symbol
	[SerializeField] private List<SymbolAnticipationSoundData> symbolSounds;

	// Dictionary of animating symbols : possible mutation name (null if no mutation needed/found)
	private Dictionary<SlotSymbol,string> symbolsToAnim = new Dictionary<SlotSymbol, string>();
	
	// Return the appropriate list of symbol names based on the given reel.
	private List<SymbolAction> getReelSymbolList(SlotReel stoppingReel)
	{
		foreach (KeyValuePair<int, List<SymbolAction>> reel in reelToSymbolAction)
		{
			if (reel.Key == stoppingReel.reelID)
			{
				return reel.Value;
			}
		}
		return null;
	}

	// This function loops through all visible symbols on the various reels and caches any symbols we are concerned with for
	//	animation. It returns false if no symbols were found.
	private void cacheAnimatableSymbolsIfPresent()
	{
		// Go through and cache all the symbols in the reels we need to animate
		foreach (SlotReel stoppingReel in reelGame.engine.getAllSlotReels())
		{
			// Grab the corresponding reel list
			List<SymbolAction> reel = getReelSymbolList(stoppingReel);
			
			// Make sure we found a reel list
			if (reel != null)
			{
				// For each symbol in the current reel...
				for (int i = 0; i < stoppingReel.visibleSymbols.Length; i++)
				{
					SlotSymbol slotSymbol = stoppingReel.visibleSymbols[i];
					SymbolAction currentSymbol = reel.Find(symbol => symbol.name == slotSymbol.serverName);
					// Resolve the symbol's mutation if found on the reel
					if (currentSymbol != null) 
					{
						symbolsToAnim[slotSymbol] = getSymbolMutation(currentSymbol, stoppingReel.reelID - 1, stoppingReel.visibleSymbols.Length - 1 - i);
						if (string.IsNullOrEmpty(symbolsToAnim[slotSymbol]) && currentSymbol.mutationType == SymbolAction.MutationType.TRIGGER)
						{
							// If we're expecting a server mutation but weren't able to define a mutation symbol above, don't animate this symbol.
							symbolsToAnim.Remove(slotSymbol);
						}
					}
				}
			}
		}
	}

	private string getSymbolMutation(SymbolAction symbol, int row, int col)
	{
		switch (symbol.mutationType)
		{
			case SymbolAction.MutationType.OUTCOME:
				return symbol.name + SlotSymbol.OUTCOME_SYMBOL_POSTFIX;
			case SymbolAction.MutationType.TRIGGER:
				// Look in server response mutations to find how to mutate this trigger symbol
				foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
				{
					StandardMutation mutation = baseMutation as StandardMutation;
					if (mutation.type == MUTATION_OUTCOME_TARGET)
					{
						if (!string.IsNullOrEmpty(mutation.triggerSymbolNames[row,col]))
						{
							return mutation.triggerSymbolNames[row, col];
						}
					}
				}	
				break;
		}
		return null;
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{	
		cacheAnimatableSymbolsIfPresent();
		// return wether or not we cached any animating symbols
		return symbolsToAnim.Count > 0;
	}
	
	// This function fires once one of the animatable symbols is done
	public void animateDoneDelegate(SlotSymbol sender)
	{
		if (!string.IsNullOrEmpty(symbolsToAnim[sender]))
		{
			sender.mutateTo(symbolsToAnim[sender]);
		}
		// Remove the sender from the list of animatable symbols
		symbolsToAnim.Remove(sender);
	}
	
	public override IEnumerator executeOnReelsStoppedCallback()
	{
		SlotSymbol.AnimateDoneDelegate doneDelegateRef = null;
		
		// Check if we need to block, if so store a reference to the done delegate
		if (shouldBlockUntilAnimationsFinish)
		{
			doneDelegateRef = animateDoneDelegate;
		}
		
		// Fire off all the play functions in our cached symbol list
		foreach (SlotSymbol slotSymbol in symbolsToAnim.Keys)
		{
			slotSymbol.animateAnticipation(doneDelegateRef);
			if (symbolSounds != null)
			{
				// Play the sound for this symbol in supplied sound data list
				foreach(SymbolAnticipationSoundData soundData in symbolSounds)
				{
					if (soundData.name == slotSymbol.name && !soundData.symbolSoundFired)
					{
						StartCoroutine(AudioListController.playListOfAudioInformation(soundData.sounds));
						soundData.symbolSoundFired = true;
					}
				}
			}
		}

		// Reset fired flags on all the data
		foreach (SymbolAnticipationSoundData soundData in symbolSounds)
		{
			soundData.symbolSoundFired = false;
		}
		
		// If we are blocking, loop until the list is empty
		if (shouldBlockUntilAnimationsFinish)
		{
			while (symbolsToAnim.Count > 0)
			{
				yield return null;
			}
		}
	}

	[System.Serializable]
	private class SymbolAction
	{
		public string name;
		// Choose what kind of mutation you want to happen after the animation finishes
		public enum MutationType
		{
			NONE, 
			OUTCOME, // mutate to outcome version (symbolname + outcome prefix)
			TRIGGER // mutate to mutation trigger defined in server response 
		}
		public MutationType mutationType;
		public SymbolAction() {}
	}

	[System.Serializable]
	public class SymbolAnticipationSoundData
	{
		public string name = "";
		public AudioListController.AudioInformationList sounds;
		[HideInInspector] public bool symbolSoundFired; // avoids many symbols playing the same sound
	}
}
