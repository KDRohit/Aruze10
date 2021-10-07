using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

/*
 * Module for handling a slightly different way of doing TW symbol awards from what BaseTWModule does.
 * Needed for got01 basically this module handles playing animations on specific reels to present the
 * TW changes.
 *
 * Original Author: Scott Lepthien
 * Creation Date: 6/19/2019
 */
public class ReelAnimationTWModule : SlotModule
{
	[Header("Functionality Settings")]
	[Tooltip("Reel which has the TW symbol land that triggers this feature.")]
	[SerializeField] private int TRIGGER_REEL = 2; // Middle Reel
	[Tooltip("Delays the symbol mutations, allows you to sync the mutations to reelTriggerAnimations")]
	[SerializeField] private float TIME_BEFORE_MUTATING_SYMBOLS_ON_REEL = 0.0f;
	[Tooltip("Allows the symbol mutations to be staggered")]
	[SerializeField] private float TIME_BETWEEN_MUTATIONS = 0.0f;

	[Tooltip("Name of the symbol template animation that will play to show the symbol changing into a finalMutatedWdName symbol")]
	[SerializeField] private string symbolTransformName = "TWWD";
	[Tooltip("What symbol any mutated symbols will be converted to once the mutation is complete")]
	[SerializeField] private string finalMutatedWdName = "WD";

	[Header("Animation Settings")]
	[Tooltip("Array of animation data defining what each reel does when the feature triggers on it")]
	[SerializeField] private List<ReelAnimationData> reelAnimationDataArray;
	[Tooltip("Animations played before any of the reel features start")]
	[SerializeField] private AnimationListController.AnimationInformationList featureIntroAnims;
	[Tooltip("Animations played after all the reel features are done")]
	[SerializeField] private AnimationListController.AnimationInformationList featureOutroAnims;

	[Header("Audio Settings")]
	[Tooltip("Sounds played when the TW symbol that triggers the feature lands plays an anticipation")]
	[SerializeField] private AudioListController.AudioInformationList twSymbolLandAudioList; // Originally: "trigger_symbol" and "tw_symbol_vo"
	[Tooltip("Sounds played when the TW symbol plays an outcome animation as the feature is about to start")]
	[SerializeField] private AudioListController.AudioInformationList twSymbolAnimateAudioList; // Originally: "trigger_symbol_fanfare" and "tw_effect_land_vo"
	[Tooltip("Sounds played when a mutated symbol is converted")]
	[SerializeField] private AudioListController.AudioInformationList twSymbolMutateAudioList; // Originally: "trigger_symbol_effect"

	private bool isTriggerWildsSymbolLanded = false;
	private Dictionary<int, ReelAnimationData> reelAnimationDataDictionary = new Dictionary<int, ReelAnimationData>();
	private TICoroutine twAnticipationAnimCoroutine = null;

	public override void Awake()
	{
		base.Awake();

		for (int i = 0; i < reelAnimationDataArray.Count; i++)
		{
			ReelAnimationData currentAnimData = reelAnimationDataArray[i];
			if (!reelAnimationDataDictionary.ContainsKey(currentAnimData.reelIndex))
			{
				reelAnimationDataDictionary.Add(currentAnimData.reelIndex, currentAnimData);
			}
			else
			{
				Debug.LogWarning("ReelAnimationTWModule.Awake() - Found duplicate animation data for reelIndex = " + currentAnimData.reelIndex + "; ignoring the extra instance.");
			}
		}
	}

	public override bool needsToExecuteOnPreSpin()
	{
		return isTriggerWildsSymbolLanded;
	}

	public override IEnumerator executeOnPreSpin()
	{
		isTriggerWildsSymbolLanded = false;
		yield break;
	}

	public override bool needsToExecuteOnSpecificReelStop(SlotReel stoppedReel)
	{
		if (stoppedReel.reelID - 1 == TRIGGER_REEL)
		{
			string[] symbolsLanded = stoppedReel.getFinalReelStopsSymbolNames();

			for (int i = 0; i < symbolsLanded.Length; i++)
			{
				if (symbolsLanded[i].Contains("TW"))
				{
					if (reelGame.mutationManager.mutations.Count > 0)
					{
						isTriggerWildsSymbolLanded = true;
						return true;
					}
					else
					{
						Debug.LogError("ReelAnimationTWModule.needsToExecuteOnSpecificReelStop() - TW landed on TRIGGER_REEL = " + TRIGGER_REEL + "; but no mutation data was in the outcome, this shouldn't happen!");
					}
				}
			}
		}
		
		return false;
	}

	public override IEnumerator executeOnSpecificReelStop(SlotReel stoppedReel)
	{
		if (isTriggerWildsSymbolLanded)
		{
			yield return StartCoroutine(AudioListController.playListOfAudioInformation(twSymbolLandAudioList));
			SlotSymbol triggerWildsSymbol = getTWSymbol();
			twAnticipationAnimCoroutine = StartCoroutine(triggerWildsSymbol.playAndWaitForAnimateAnticipation());
		}

		yield return StartCoroutine(base.executeOnSpecificReelStop(stoppedReel));
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		if (reelGame.mutationManager.mutations.Count > 0 && isTriggerWildsSymbolLanded) //Only play our feature if we have mutations from the server and the TW symbol landed
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		// Make sure that the TW anticipation is done before continuing
		if (twAnticipationAnimCoroutine != null && !twAnticipationAnimCoroutine.isFinished)
		{
			yield return twAnticipationAnimCoroutine;
		}

		StandardMutation currentMutation = null;
		foreach (MutationBase mutation in reelGame.mutationManager.mutations)
		{
			currentMutation = mutation as StandardMutation;
			if (currentMutation.isTWmutation == true)
			{
				break;
			}
			else
			{
				currentMutation = null;
			}		
		}
		
		if (currentMutation == null || currentMutation.triggerSymbolNames == null || currentMutation.triggerSymbolNames.Length < 2)
		{
			Debug.LogError("The mutations came down from the server incorrectly. The backend is broken.");
			yield break;
		}
		
		// Play this to indicate that the feature is going to start
		SlotSymbol triggerWildsSymbol = getTWSymbol();
		yield return StartCoroutine(triggerWildsSymbol.playAndWaitForAnimateOutcome());
		
		// Play the intro anims here
		if (featureIntroAnims.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(featureIntroAnims));
		}

		// Play the outcome animation for the triggering symbol
		yield return StartCoroutine(AudioListController.playListOfAudioInformation(twSymbolAnimateAudioList));

		// Build data set of reels to the mutations that are happening on them
		ReelMutationTracker mutationTracker = new ReelMutationTracker();

		for (int i = 0; i < currentMutation.triggerSymbolNames.GetLength(0); i++)
		{
			for (int j = 0; j < currentMutation.triggerSymbolNames.GetLength(1); j++)
			{
				if (currentMutation.triggerSymbolNames[i,j] != null && currentMutation.triggerSymbolNames[i,j] != "")
				{
					mutationTracker.addMutationData(i, j, currentMutation.triggerSymbolNames[i,j]);
				}
			}
		}

		// Go through each reel and determine if we need to play mutation stuff on it
		SlotReel[] allReels = reelGame.engine.getReelArray();
		for (int i = 0; i < allReels.Length; i++)
		{
			int reelIndex = allReels[i].reelID - 1;
			if (mutationTracker.hasMutationsForReelIndex(reelIndex))
			{
				yield return StartCoroutine(playMutationEffectsOnReel(reelIndex, mutationTracker));
			}
		}
		
		// Play outro anims here
		if (featureOutroAnims.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(featureOutroAnims));
		}
	}

	private IEnumerator playMutationEffectsOnReel(int reelIndex, ReelMutationTracker mutationTracker)
	{
		// Get the anim data for this reel
		ReelAnimationData currentReelAnimData = null;
		if (reelAnimationDataDictionary.ContainsKey(reelIndex))
		{
			currentReelAnimData = reelAnimationDataDictionary[reelIndex];
		}

		List<TICoroutine> reelFeatureCoroutines = new List<TICoroutine>();
		if (currentReelAnimData != null && currentReelAnimData.reelTriggerAnimations.Count > 0)
		{
			// Play an animation for this reel being converted (in got02 for instance this is a dragon breathing fire onto the reel)
			reelFeatureCoroutines.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(currentReelAnimData.reelTriggerAnimations)));
		}
		
		// Add a pause if required before mutating the symbols since we are allowing them to overlap in case
		// we want to have the symbols change while the trigger animation is going
		if (TIME_BEFORE_MUTATING_SYMBOLS_ON_REEL > 0.0f)
		{
			yield return new TIWaitForSeconds(TIME_BEFORE_MUTATING_SYMBOLS_ON_REEL);
		}
		
		List<MutationPositionData> mutationsOnReel;
		if (currentReelAnimData == null || !currentReelAnimData.isMutatingFromReelTopToBottom)
		{
			mutationsOnReel = mutationTracker.getMutationDataForReelBottomToTop(reelIndex);
		}
		else
		{
			mutationsOnReel = mutationTracker.getMutationDataForReelTopToBottom(reelIndex);
		}

		// Convert the mutated symbols
		for (int i = 0; i < mutationsOnReel.Count; i++)
		{
			reelFeatureCoroutines.Add(StartCoroutine(convertSymbolToTW(mutationsOnReel[i])));
			// If this isn't the last symbol then allow the staggering time
			if (i < mutationsOnReel.Count - 1 && TIME_BETWEEN_MUTATIONS > 0.0f)
			{
				yield return new TIWaitForSeconds(TIME_BETWEEN_MUTATIONS);
			}
		}

		yield return StartCoroutine(Common.waitForCoroutinesToEnd(reelFeatureCoroutines));
		
		// Play reel feature ending animations if defined
		if (currentReelAnimData != null && currentReelAnimData.reelFeatureEndedAnimations.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(currentReelAnimData.reelFeatureEndedAnimations));
		}
	}

	private IEnumerator convertSymbolToTW(MutationPositionData mutationData)
	{
		SlotReel[] reelArray = reelGame.engine.getReelArray();
		SlotSymbol targetSymbol = reelArray[mutationData.reelIndex].visibleSymbolsBottomUp[mutationData.symbolIndex];
		
		targetSymbol.mutateTo(symbolTransformName, null, false, true);
		
		yield return StartCoroutine(AudioListController.playListOfAudioInformation(twSymbolMutateAudioList));
		yield return StartCoroutine(targetSymbol.playAndWaitForAnimateOutcome());

		targetSymbol.mutateTo(finalMutatedWdName, null, false, true);
	}

	// Get the TW symbol which is causing the feature to occur
	private SlotSymbol getTWSymbol()
	{
		SlotSymbol triggerWildSymbol = null;
		reelGame.engine.getSlotReelAt(TRIGGER_REEL).refreshVisibleSymbols();
		SlotSymbol[] visibleSymbols = reelGame.engine.getSlotReelAt(TRIGGER_REEL).visibleSymbols;
		foreach (SlotSymbol symbol in visibleSymbols)
		{
			if (symbol.name.Contains("TW") && symbol.isWildSymbol)
			{				
				triggerWildSymbol = symbol;
				break;
			}
		}
		
		return triggerWildSymbol;
	}

	// Animation information linked to a given reel for when the feature occurs on that reel
	[System.Serializable]
	private class ReelAnimationData
	{
		public int reelIndex;
		[Tooltip("Animations played along with the symbols being mutated.  Set TIME_BEFORE_MUTATING_SYMBOLS_ON_REEL to be the length of these animations if you want the symbols to convert after these anims are done")]
		public AnimationListController.AnimationInformationList reelTriggerAnimations;
		[Tooltip("Animations played after all the symbols are converted and reelTriggerAnimations are done.  Use this if you need to stop something like a looping animation from reelTriggerAnimations.")]
		public AnimationListController.AnimationInformationList reelFeatureEndedAnimations;
		[Tooltip("Allows each reel to define what direction the symbols will mutate in when the feature triggers.")]
		public bool isMutatingFromReelTopToBottom = false;
	}

	// Class used to track the mutation information and link and sort the mutations for each reel
	private class ReelMutationTracker
	{
		private List<int> reelsWithMutations = new List<int>();
		private Dictionary<int, List<MutationPositionData>> mutationsByReel = new Dictionary<int, List<MutationPositionData>>();

		public void addMutationData(int reelIndex, int symbolIndex, string mutatedSymbolName)
		{
			if (!reelsWithMutations.Contains(reelIndex))
			{
				reelsWithMutations.Add(reelIndex);
				mutationsByReel.Add(reelIndex, new List<MutationPositionData>());
			}
			
			mutationsByReel[reelIndex].Add(new MutationPositionData(reelIndex, symbolIndex, mutatedSymbolName));
		}

		public bool hasMutationsForReelIndex(int reelIndex)
		{
			return reelsWithMutations.Contains(reelIndex);
		}

		public List<MutationPositionData> getMutationDataForReelBottomToTop(int reelIndex)
		{
			if (mutationsByReel.ContainsKey(reelIndex))
			{
				List<MutationPositionData> listForReel = mutationsByReel[reelIndex];
				// Just do a default sort to make sure the list is indeed sorted
				listForReel.Sort();
				return listForReel;
			}
			else
			{
				Debug.LogError("ReelAnimationTWModule.ReelMutationTracker.getMutationDataForReelBottomToTop() - No mutations are stored for reelIndex = " + reelIndex);
				return null;
			}
		}

		public List<MutationPositionData> getMutationDataForReelTopToBottom(int reelIndex)
		{
			if (mutationsByReel.ContainsKey(reelIndex))
			{
				List<MutationPositionData> listForReel = mutationsByReel[reelIndex];
				// Just do a default sort to make sure the list is indeed sorted
				listForReel.Sort();
				// And now reverse it
				listForReel.Reverse();
				return listForReel;
			}
			else
			{
				Debug.LogError("ReelAnimationTWModule.ReelMutationTracker.getMutationDataForReelTopToBottom() - No mutations are stored for reelIndex = " + reelIndex);
				return null;
			}
		}

		public void clear()
		{
			reelsWithMutations.Clear();
			mutationsByReel.Clear();
		}
	}

	// Class to store and sort the mutation information
	private class MutationPositionData : IComparable<MutationPositionData>
	{
		public MutationPositionData(int reelIndex, int symbolIndex, string mutatedSymbolName)
		{
			this.reelIndex = reelIndex;
			this.symbolIndex = symbolIndex;
			this.mutatedSymbolName = mutatedSymbolName;
		}
		
		public int CompareTo(MutationPositionData value)
		{
			return this.symbolIndex.CompareTo(value.symbolIndex);
		}
		
		public int reelIndex;
		public int symbolIndex;
		public string mutatedSymbolName;
	}
}
