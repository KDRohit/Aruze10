using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This module allows you to play arbitrary animations on symbols on a reelstop regardless of outcome.
// It can optionaly block respins until the animation is complete.
// This module is basically functionally the same as PlaySymbolAnticipationAnimationOnReelsStoppedModule, but adds sounds that trigger with the animation calls
// First used for shania01 FreeSpins
// Original Author: Scott Lepthien
public class PlaySymbolAnticipationAnimationWithSoundOnReelsStoppedModule : SlotModule 
{
	[SerializeField] List<SymbolAnticipationData> symbolAnticipationData; 			// A list of data which pairs reel ids to info about symbols it cares about finding and animating as well as the sound to play at the same time
	[SerializeField] private bool shouldBlockUntilAnimationsFinish = false; 		// Flag this as true if you don't want the game to keep spinning until all of the symbols are done animating.
	[SerializeField] private bool shouldMutateToOutcomeOnFinish = false; 			// Flag this if you want the symbol to mutate to its outcome version once finished.
	[SerializeField] private bool playEachAnticipationSoundOnlyOncePerSpin = true; 	// Turn this off if you want the anticipation sounds to get louder if more than one triggers when the reels stop
	
	private Dictionary<int, List<SymbolAnticipationData>> symbolAnticipationDataMap = new Dictionary<int, List<SymbolAnticipationData>>(); // A dictionary to lookup based on reel what symbols will trigger anticipations when the reels stop
	private List<SlotSymbol> animatableSymbols = new List<SlotSymbol>(); // This is a list of currently displayed animatable symbols.
	private List<SymbolAnticipationData> animatableSymbolAnticipationData = new List<SymbolAnticipationData>();
	private HashSet<string> playedSymbolAnticipationSounds = new HashSet<string>(); // Tracks what symbol anticipation sounds have already played on this spin, used to skip playing duplicates if playEachAnticipationSoundOnlyOncePerSpin is true

	public override void Awake()
	{
		base.Awake();

		foreach (SymbolAnticipationData anticipationData in symbolAnticipationData)
		{
			for (int i = 0; i < anticipationData.reelsIdsAffected.Count; i++)
			{
				int reelId = anticipationData.reelsIdsAffected[i];

				if (symbolAnticipationDataMap.ContainsKey(reelId))
				{
					symbolAnticipationDataMap[reelId].Add(anticipationData);
				}
				else
				{
					symbolAnticipationDataMap.Add(reelId, new List<SymbolAnticipationData>());
					symbolAnticipationDataMap[reelId].Add(anticipationData);
				}
			}
		}
	}
	
	// This function loops through all visible symbols on the various reels and caches any symbols we are concerned with for
	//	animation. It returns false if no symbols were found.
	private void cacheAnimatableSymbolsIfPresent()
	{
		// Go through and cache all the symbols in the reels we need to animate
		foreach (SlotReel stoppingReel in reelGame.engine.getAllSlotReels())
		{
			// Grab the corresponding reel list
			List<SymbolAnticipationData> reelListOfSymbolAnticipationData = null;

			if (symbolAnticipationDataMap.ContainsKey(stoppingReel.reelID))
			{
				reelListOfSymbolAnticipationData = symbolAnticipationDataMap[stoppingReel.reelID];
			}
			
			// Make sure we found a reel list
			if (reelListOfSymbolAnticipationData != null)
			{
				// For each symbol in the current reel...
				for (int i = 0; i < stoppingReel.visibleSymbols.Length; i++)
				{
					SlotSymbol slotSymbol = stoppingReel.visibleSymbols[i];
					
					// Make sure the symbol is in the reel and play it
					foreach (SymbolAnticipationData anticipationData in reelListOfSymbolAnticipationData)
					{
						if (slotSymbol.serverName == anticipationData.symbolName)
						{
							animatableSymbols.Add(slotSymbol);
							animatableSymbolAnticipationData.Add(anticipationData);
						}
					}
				}
			}
		}
	}
	
	public override bool needsToExecuteOnReelsStoppedCallback()
	{	
		cacheAnimatableSymbolsIfPresent();
		// return wether or not we cached any animating symbols
		return animatableSymbols.Count > 0;
	}
	
	// This function fires once one of the animatable symbols is done
	public void animateDoneDelegate(SlotSymbol sender)
	{
		if (shouldMutateToOutcomeOnFinish)
		{
			sender.mutateTo(sender.serverName + SlotSymbol.OUTCOME_SYMBOL_POSTFIX, null, true, true);
		}
		
		// Remove the sender from the list of animatable symbols
		animatableSymbols.Remove(sender);
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
		for (int i = 0; i < animatableSymbols.Count; i++)
		{
			SlotSymbol slotSymbol = animatableSymbols[i];

			SymbolAnticipationData anticipationData = animatableSymbolAnticipationData[i];

			// make sure we have an anticipation sound to play along with the animation before trying to play it
			if (!string.IsNullOrEmpty(anticipationData.anticipationSound))
			{
				if (!playEachAnticipationSoundOnlyOncePerSpin || !playedSymbolAnticipationSounds.Contains(anticipationData.anticipationSound))
				{
					Audio.playSoundMapOrSoundKey(anticipationData.anticipationSound);

					// handle tracking to make sure we only play sounds one time each if playEachAnticipationSoundOnlyOncePerSpin is enabled
					if (!playedSymbolAnticipationSounds.Contains(anticipationData.anticipationSound))
					{
						playedSymbolAnticipationSounds.Add(anticipationData.anticipationSound);
					}
				}
			}

			slotSymbol.animateAnticipation(doneDelegateRef);
		}

		animatableSymbolAnticipationData.Clear();
		playedSymbolAnticipationSounds.Clear();
		
		// If we are blocking, loop until the list is empty
		if (shouldBlockUntilAnimationsFinish)
		{
			while (animatableSymbols.Count > 0)
			{
				yield return null;
			}
		}
	}

	[System.Serializable]
	public class SymbolAnticipationData
	{
		public List<int> reelsIdsAffected = new List<int>();
		public string symbolName = "";
		public string anticipationSound = "";
	}
}
