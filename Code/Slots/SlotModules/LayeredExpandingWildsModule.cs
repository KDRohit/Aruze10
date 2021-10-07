using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Module for showing animation and changing symbols wild when a mutation trigger occurs.
Originally made for ainsworth15 that as target areas on the reels that transform different
sections of the reel area wild depending on where a symbol is landed.

Creation Date: 9/27/2018
Original Author: Scott Lepthien
*/
public class LayeredExpandingWildsModule : SlotModule 
{
	private class SymbolReplacementData
	{
		public SymbolReplacementData(SlotSymbol symbol, string mutateTo)
		{
			symbolToMutate = symbol;
			symbolNameToMutateTo = mutateTo;
		}
	
		public SlotSymbol symbolToMutate;
		public string symbolNameToMutateTo;
	}
	
	[System.Serializable]
	private class TriggerSymbolSoundSet
	{
		public string setName = "";
		[Tooltip("Priority that sounds will override each other, higher overrides and plays instead of lower values")]
		public int priority = 0;
		public AudioListController.AudioInformationList triggerSymbolSoundList;
	}

	[System.Serializable]
	private class TriggerSymbolAnimData
	{
		public int reelID;
		public int layer = 0;
		public int visibleSymbolIndex;
		public AnimationListController.AnimationInformationList triggerSymbolAnimList;
		public string soundSetName = "";
	}

	private List<SymbolTriggerReelReplacementMutation> symbolTriggerReelReplacementMutations = new List<SymbolTriggerReelReplacementMutation>();
	private Dictionary<SlotSymbol, List<SymbolReplacementData>> mutationSymbolData = new Dictionary<SlotSymbol, List<SymbolReplacementData>>();

	[SerializeField] private List<TriggerSymbolAnimData> triggerSymbolAnimDataList = new List<TriggerSymbolAnimData>();
	[SerializeField] private AnimationListController.AnimationInformationList allReelsConvertedAnimList;
	[SerializeField] private string allReelsConvertedSoundSet = "";
	[SerializeField] private List<TriggerSymbolSoundSet> triggerSymolSoundSetsList = new List<TriggerSymbolSoundSet>();
	[SerializeField] private float DELAY_BEFORE_CONVERTING_SYMBOLS_TO_WD = 0.0f;
	
	// executeOnPreSpin() section
	// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) before the reels spin
	public override bool needsToExecuteOnPreSpin()
	{
		return symbolTriggerReelReplacementMutations.Count > 0;
	}

	public override IEnumerator executeOnPreSpin()
	{
		symbolTriggerReelReplacementMutations.Clear();
		mutationSymbolData.Clear();
		yield break;
	}
	
	// executeOnReelsStoppedCallback() section
	// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		MutationBase baseMutation;
		for (int i = 0; i < reelGame.mutationManager.mutations.Count; i++)
		{
			baseMutation = reelGame.mutationManager.mutations[i];
			SymbolTriggerReelReplacementMutation triggerReelReplaceMutaiton = baseMutation as SymbolTriggerReelReplacementMutation;
			if (triggerReelReplaceMutaiton != null)
			{
				symbolTriggerReelReplacementMutations.Add(triggerReelReplaceMutaiton);
			}
		}
	
		return symbolTriggerReelReplacementMutations.Count > 0;
	}

	// Get the animation information
	private TriggerSymbolAnimData getTriggerSymbolAnimDataForTriggerSymbol(SlotSymbol triggerSymbol)
	{
		for (int i = 0; i < triggerSymbolAnimDataList.Count; i++)
		{
			TriggerSymbolAnimData currentData = triggerSymbolAnimDataList[i];
			if (triggerSymbol.reel.reelID == currentData.reelID
				&& triggerSymbol.reel.layer == currentData.layer
				&& triggerSymbol.visibleSymbolIndex == currentData.visibleSymbolIndex)
			{
				return currentData;
			}
		}

		Debug.LogWarning("LayeredExpandingWildsModule.getTriggerSymbolAnimDataForTriggerSymbol() - Unable to find match for: "
						+ "triggerSymbol.reel.reelID = " + triggerSymbol.reel.reelID
						+ "; triggerSymbol.reel.layer = " + triggerSymbol.reel.layer
						+ "; triggerSymbol.visibleSymbolIndexBottomUp = " + triggerSymbol.visibleSymbolIndexBottomUp
						+ "; returning NULL!");
		return null;
	}

	private IEnumerator playAnimationsForTriggerSymbols(bool isEveryVisibleSymbolCovered)
	{
		List<TICoroutine> animCoroutines = new List<TICoroutine>();

		TriggerSymbolSoundSet soundSetToPlay = null;

		// if all visible symbols are covered we will only play a special animation that covers the entire reel area
		if (isEveryVisibleSymbolCovered)
		{
			animCoroutines.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(allReelsConvertedAnimList)));
			soundSetToPlay = getTriggerSymbolSoundSetForSetName(allReelsConvertedSoundSet);
		}
		else
		{
			// otherwise we will play the anim associated with each trigger symbol location
			foreach (KeyValuePair<SlotSymbol, List<SymbolReplacementData>> kvp in mutationSymbolData)
			{
				TriggerSymbolAnimData triggerSymbolAnimData = getTriggerSymbolAnimDataForTriggerSymbol(kvp.Key);
				if (triggerSymbolAnimData != null)
				{
					animCoroutines.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(triggerSymbolAnimData.triggerSymbolAnimList)));
					
					// Determine what sound set will be the final one we play along with these animations
					TriggerSymbolSoundSet currentSoundSet = getTriggerSymbolSoundSetForSetName(triggerSymbolAnimData.soundSetName);
					if (currentSoundSet != null && currentSoundSet.triggerSymbolSoundList.Count > 0)
					{
						if (soundSetToPlay == null || soundSetToPlay.priority < currentSoundSet.priority)
						{
							soundSetToPlay = currentSoundSet;
						}
					}
				}
			}
		}
		
		// Play the audio list we determined we should be playing right now
		if (soundSetToPlay != null && soundSetToPlay.triggerSymbolSoundList.Count > 0)
		{
			animCoroutines.Add(StartCoroutine(AudioListController.playListOfAudioInformation(soundSetToPlay.triggerSymbolSoundList)));
		}

		// Wait a certain amount of time before converting the symbols over, this way they can be converted while
		// covered by the animations above
		if (DELAY_BEFORE_CONVERTING_SYMBOLS_TO_WD > 0.0f)
		{
			yield return new TIWaitForSeconds(DELAY_BEFORE_CONVERTING_SYMBOLS_TO_WD);
		}

		foreach (KeyValuePair<SlotSymbol, List<SymbolReplacementData>> kvp in mutationSymbolData)
		{
			for (int i = 0; i < kvp.Value.Count; i++)
			{
				SymbolReplacementData currentReplacement = kvp.Value[i];
				currentReplacement.symbolToMutate.mutateTo(currentReplacement.symbolNameToMutateTo, null, false, true);
			}
		}

		if (animCoroutines.Count > 0)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(animCoroutines));
		}
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		int totalSymbolsConvertedToWilds = 0;

		// Gather the unique trigger symbols (since each part of the game can respond to the same trigger symbol)
		HashSet<SlotSymbol> uniqueTriggerSymbols = new HashSet<SlotSymbol>();
		for (int i = 0; i < symbolTriggerReelReplacementMutations.Count; i++)
		{
			SymbolTriggerReelReplacementMutation mutationData = symbolTriggerReelReplacementMutations[i];
			SymbolTriggerReelReplacementMutation.ResultInfo result;
			for (int k = 0; k < mutationData.results.Count; k++)
			{
				result = mutationData.results[k];
				SymbolTriggerReelReplacementMutation.TriggerSymbolInfo triggerSymbolData = result.triggerSymbol;
				SlotReel triggerSymbolReel = reelGame.engine.getSlotReelAt(triggerSymbolData.reel, -1, triggerSymbolData.layer);
				SlotSymbol symbol = triggerSymbolReel.visibleSymbolsBottomUp[triggerSymbolData.pos];
				
				// Check if we need to make an entry for this trigger symbol	
				if (!mutationSymbolData.ContainsKey(symbol))
				{
					mutationSymbolData.Add(symbol, new List<SymbolReplacementData>());
				}

				// Associate the replacement data with the trigger symbol
				SymbolTriggerReelReplacementMutation.ReplacementSymbolInfo replacementData;
				for (int m = 0; m < result.replacedSymbolList.Count; m++)
				{
					replacementData = result.replacedSymbolList[m];
					SlotReel replaceSymbolReel = reelGame.engine.getSlotReelAt(replacementData.reel, -1, replacementData.layer);
					SlotSymbol replaceSymbol = replaceSymbolReel.visibleSymbolsBottomUp[replacementData.pos];
					mutationSymbolData[symbol].Add(new SymbolReplacementData(replaceSymbol, replacementData.toSymbolName));
					totalSymbolsConvertedToWilds++;
				}
			}
		}

		// Determine if the whole reel area will be converted, and if so we'll play a special animation
		bool isEveryVisibleSymbolCovered = false;

		if (reelGame.engine is MultiSlotEngine)
		{
			// For MultiSlotEngine we need to consider every section of the game, so we'll just
			// grab all of the reels and use the total number of symbols to determine how many
			// symbols need to be mutated for the entire reel area to be converted
			int alreadyVisibleWds = 0;
			int totalNumberOfVisibleSymbols = 0;
			SlotReel[] allReels = reelGame.engine.getAllSlotReels();
			for (int i = 0; i < allReels.Length; i++)
			{
				SlotSymbol[] visibleSymbols = allReels[i].visibleSymbols;

				for (int k = 0; k < visibleSymbols.Length; k++)
				{
					if (visibleSymbols[k] != null && visibleSymbols[k].isWildSymbol)
					{
						alreadyVisibleWds++;
					}
				}
				totalNumberOfVisibleSymbols += allReels[i].reelData.visibleSymbols;
			}

			isEveryVisibleSymbolCovered = ((totalSymbolsConvertedToWilds + alreadyVisibleWds) == totalNumberOfVisibleSymbols);
		}
		else
		{
			// We need to take into account symbols that will not be converted because they are already WDs
			List<SlotSymbol> allVisibleSymbols = reelGame.engine.getAllVisibleSymbols();
			int alreadyVisibleWds = 0;

			for (int i = 0; i < allVisibleSymbols.Count; i++)
			{
				if (allVisibleSymbols[i].isWildSymbol)
				{
					alreadyVisibleWds++;
				}
			}

			isEveryVisibleSymbolCovered = ((totalSymbolsConvertedToWilds + alreadyVisibleWds) == allVisibleSymbols.Count);
		}

		// Play animation for revealing buffalo banner of correct size and converting symbols to WD
		yield return StartCoroutine(playAnimationsForTriggerSymbols(isEveryVisibleSymbolCovered));
	}
	
	// Find the trigger symbol sound set data for the passed name
	private TriggerSymbolSoundSet getTriggerSymbolSoundSetForSetName(string setName)
	{
		for (int i = 0; i < triggerSymolSoundSetsList.Count; i++)
		{
			TriggerSymbolSoundSet currentSoundSet = triggerSymolSoundSetsList[i];
			if (currentSoundSet.setName == setName)
			{
				return currentSoundSet;
			}
		}

		return null;
	}
}
