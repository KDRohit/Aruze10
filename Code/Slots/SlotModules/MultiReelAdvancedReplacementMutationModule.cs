using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Feature originally done for orig004 Freespins variant where a wild banner with a multiplier
appears over a reel on the first spin, and then remains there.  This module can be used
for any game that wants to use the server MultiReelAdvancedReplacementMutator and supports the locking
flag that can be enabled for that mutator.

Original Author: Scott Lepthien
Creation Date: September 18, 2020
*/
public class MultiReelAdvancedReplacementMutationModule : SlotModule
{
	[System.Serializable] private class WildBannerData
	{
		public int reelIndex = 0;
		public string symbolName = "";
		public AnimationListController.AnimationInformationList wildBannerTriggeredAnimations;
		public AnimationListController.AnimationInformationList wildBannerIdleAnimations;
	}
	
	[SerializeField] private List<WildBannerData> bannerAnimationData = new List<WildBannerData>();
	[SerializeField] private float timeBetweenBannerRevealAnims = 0.0f;
	[Tooltip("Determines if a tall symbol should be used instead of 1x1s to fill the reel.  The mutation from the server always sends a 1x1 symbol name, so a conversion has to be done for a tall symbol.")]
	[SerializeField] private bool isMutatingToTallSymbol = true;
	[Tooltip("Turn this on if instead of having symbol animations play for outcomes, you just want to use a looped overlay banner that is always animating.")]
	[SerializeField] private bool isLeavingBannerAnimationDuringOutcomes = false;
	private List<StandardMutation> wildBannerMutations = new List<StandardMutation>();
	
	private Dictionary<int, Dictionary<string, WildBannerData>> wildBannerLookupDictionary = new Dictionary<int, Dictionary<string, WildBannerData>>();

	public override void Awake()
	{
		base.Awake();

		foreach (WildBannerData banner in bannerAnimationData)
		{
			if (!wildBannerLookupDictionary.ContainsKey(banner.reelIndex))
			{
				wildBannerLookupDictionary.Add(banner.reelIndex, new Dictionary<string, WildBannerData>());
			}

			if (!wildBannerLookupDictionary[banner.reelIndex].ContainsKey(banner.symbolName))
			{
				wildBannerLookupDictionary[banner.reelIndex].Add(banner.symbolName, banner);
			}
			else
			{
				Debug.LogWarning("MultiReelAdvancedReplacementMutationModule.Awake() - Found duplicate banner for banner.reelIndex = " + banner.reelIndex + "; banner.symbolName = " + banner.symbolName + "; ignoring the duplicate.");
			}
		}
	}

	// executeOnPreSpin() section
	// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) before the reels spin
	public override bool needsToExecuteOnPreSpin()
	{
		// Check if the last spin had WD banner info it displayed
		return wildBannerMutations.Count > 0;
	}

	public override IEnumerator executeOnPreSpin()
	{
		List<TICoroutine> bannerAnimCoroutines = new List<TICoroutine>();
	
		// if the symbols under the banners were disabled, turn them back on
		if (isLeavingBannerAnimationDuringOutcomes)
		{
			for (int mutationIndex = 0; mutationIndex < wildBannerMutations.Count; mutationIndex++)
			{
				StandardMutation currentMutation = wildBannerMutations[mutationIndex];
				for (int reelListIndex = 0; reelListIndex < currentMutation.mutatedReels.Length; reelListIndex++)
				{
					for (int reelEntryIndex = 0; reelEntryIndex < currentMutation.mutatedReels[reelListIndex].Length; reelEntryIndex++)
					{
						int currentReelIndex = currentMutation.mutatedReels[reelListIndex][reelEntryIndex];

						SlotSymbol[] visibleSymbolsOnReel = reelGame.engine.getVisibleSymbolsAt(currentReelIndex);
						for (int m = 0; m < visibleSymbolsOnReel.Length; m++)
						{
							visibleSymbolsOnReel[m].gameObject.SetActive(true);
						}
					}
				}
			}
			
			for (int i = 0; i < bannerAnimationData.Count; i++)
			{
				if (bannerAnimationData[i].wildBannerIdleAnimations != null && bannerAnimationData[i].wildBannerIdleAnimations.Count > 0)
				{
					bannerAnimCoroutines.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(bannerAnimationData[i].wildBannerIdleAnimations)));
				}
			}
		}
	
		wildBannerMutations.Clear();

		if (bannerAnimCoroutines.Count > 0)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(bannerAnimCoroutines));
		}
	}

	// executePreReelsStopSpinning() section
	// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) before the reels stop spinning (after the outcome has been set)
	public override bool needsToExecutePreReelsStopSpinning()
	{
		// Grab the current mutations and see if we have one that triggers wild banners
		if (reelGame.mutationManager != null 
			&& reelGame.mutationManager.mutations != null 
			&& reelGame.mutationManager.mutations.Count > 0)
		{
			for (int mutationIndex = 0; mutationIndex < reelGame.mutationManager.mutations.Count; mutationIndex++)
			{
				MutationBase baseMutation = reelGame.mutationManager.mutations[mutationIndex];
				if (baseMutation.type == "multi_reel_advanced_replacement")
				{
					StandardMutation mutation = baseMutation as StandardMutation;
					if (mutation.mutatedReels != null && mutation.mutatedReels.Length > 0)
					{
						wildBannerMutations.Add(mutation);
					}
				}
			}
		}
	
		return wildBannerMutations.Count > 0;
	}
	
	public override IEnumerator executePreReelsStopSpinning()
	{
		List<TICoroutine> bannerAnimCoroutines = new List<TICoroutine>();
	
		for (int mutationIndex = 0; mutationIndex < wildBannerMutations.Count; mutationIndex++)
		{
			StandardMutation currentMutation = wildBannerMutations[mutationIndex];
			for (int reelListIndex = 0; reelListIndex < currentMutation.mutatedReels.Length; reelListIndex++)
			{
				for (int reelEntryIndex = 0; reelEntryIndex < currentMutation.mutatedReels[reelListIndex].Length; reelEntryIndex++)
				{
					int currentReelIndex = currentMutation.mutatedReels[reelListIndex][reelEntryIndex];
					string symbolReplacementName = currentMutation.mutatedSymbols[reelListIndex];
					WildBannerData animationDataForReel = getWildBannerDataForReel(currentReelIndex, symbolReplacementName);

					// play the animations
					if (animationDataForReel != null && animationDataForReel.wildBannerTriggeredAnimations.Count > 0)
					{
						bannerAnimCoroutines.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(animationDataForReel.wildBannerTriggeredAnimations)));
						if (timeBetweenBannerRevealAnims > 0.0f)
						{
							yield return new TIWaitForSeconds(timeBetweenBannerRevealAnims);
						}
					}
				}
			}
		}

		if (bannerAnimCoroutines.Count > 0)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(bannerAnimCoroutines));
		}
	}
	
	// executeOnReelsStoppedCallback() section
	// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return wildBannerMutations.Count > 0;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		List<TICoroutine> bannerAnimCoroutines = new List<TICoroutine>();
	
		for (int mutationIndex = 0; mutationIndex < wildBannerMutations.Count; mutationIndex++)
		{
			StandardMutation currentMutation = wildBannerMutations[mutationIndex];
			for (int reelListIndex = 0; reelListIndex < currentMutation.mutatedReels.Length; reelListIndex++)
			{
				for (int reelEntryIndex = 0; reelEntryIndex < currentMutation.mutatedReels[reelListIndex].Length; reelEntryIndex++)
				{
					int currentReelIndex = currentMutation.mutatedReels[reelListIndex][reelEntryIndex];
					string symbolReplacementName = currentMutation.mutatedSymbols[reelListIndex];
					bool isLockingReel = currentMutation.isLockingMutatedReel[reelListIndex];

					if (isLockingReel)
					{
						SlotReel reel = reelGame.engine.getSlotReelAt(currentReelIndex);
						reel.isLocked = true;
					}

					// Convert all symbols under the banners into the mutation replacement symbol
					SlotSymbol[] visibleSymbolsOnReel = reelGame.engine.getVisibleSymbolsAt(currentReelIndex);

					if (isMutatingToTallSymbol)
					{
						string tallSymbolName = SlotSymbol.constructNameFromDimensions(symbolReplacementName, 1, visibleSymbolsOnReel.Length);
						visibleSymbolsOnReel[0].mutateTo(tallSymbolName, skipAnimation:true);
					}
					
					for (int visibleSymbolIndex = 0; visibleSymbolIndex < visibleSymbolsOnReel.Length; visibleSymbolIndex++)
					{
						// If we aren't mutating to a tall symbol, we'll mutate each symbol individually.
						if (!isMutatingToTallSymbol)
						{
							visibleSymbolsOnReel[visibleSymbolIndex].mutateTo(symbolReplacementName, skipAnimation:true);
						}

						if (isLeavingBannerAnimationDuringOutcomes)
						{
							visibleSymbolsOnReel[visibleSymbolIndex].skipAnimationsThisOutcome();
							visibleSymbolsOnReel[visibleSymbolIndex].gameObject.SetActive(false);
						}
					}

					if (!isLeavingBannerAnimationDuringOutcomes)
					{
						// We should switch the banner animation back to idle here, since we are going to just leave the symbols behind that are under it
						WildBannerData animationDataForReel = getWildBannerDataForReel(currentReelIndex, symbolReplacementName);
						if (animationDataForReel != null && animationDataForReel.wildBannerIdleAnimations.Count > 0)
						{
							bannerAnimCoroutines.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(animationDataForReel.wildBannerIdleAnimations)));
						}
					}
				}
			}
		}
	
		if (bannerAnimCoroutines.Count > 0)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(bannerAnimCoroutines));
		}
	}
	
	// Get the wild banner animation info for a specific reel (which will come from the mutation)
	private WildBannerData getWildBannerDataForReel(int reelIndex, string symbolReplacementName)
	{
		if (wildBannerLookupDictionary.ContainsKey(reelIndex))
		{
			if (wildBannerLookupDictionary[reelIndex].ContainsKey(symbolReplacementName))
			{
				return wildBannerLookupDictionary[reelIndex][symbolReplacementName];
			}
		}

		return null;
	}
}
