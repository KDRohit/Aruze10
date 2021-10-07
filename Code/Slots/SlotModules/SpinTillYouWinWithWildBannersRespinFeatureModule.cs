using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Feature originally done for wonka04 where reevaluation spins continue to happen
and wild banners fill in reels until the player gets a win.

Original Author: Scott Lepthien
Creation Date: October 25, 2017
*/
public class SpinTillYouWinWithWildBannersRespinFeatureModule : SlotModule 
{
	[System.Serializable] private class WildBannerData
	{
		public int reelIndex = 0;
		public AnimationListController.AnimationInformationList wildBannerTriggeredAnimations;
		public AnimationListController.AnimationInformationList wildBannerIdleAnimations;
	}

	[SerializeField] private List<WildBannerData> bannerAnimationData = new List<WildBannerData>();
	[SerializeField] private bool isHidingSymbolsUnderBanners = true;
	[SerializeField] private bool isSendingSeenBonusSummaryScreenResponse = true;
	private List<StandardMutation> wildBannerMutations = new List<StandardMutation>();
	private bool hasTriggeredBanners = false;

// executeOnPreSpin() section
// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) before the reels spin
	public override bool needsToExecuteOnPreSpin()
	{
		return hasTriggeredBanners;
	}

	public override IEnumerator executeOnPreSpin()
	{
		hasTriggeredBanners = false;

		// if the symbols under the banners were disabled, turn them back on
		if (isHidingSymbolsUnderBanners)
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
						for(int m = 0; m < visibleSymbolsOnReel.Length; m++)
						{
							if (isHidingSymbolsUnderBanners)
							{
								visibleSymbolsOnReel[m].gameObject.SetActive(true);
							}
						}
					}
				}
			}
		}

		wildBannerMutations.Clear();

		for (int i = 0; i < bannerAnimationData.Count; i++)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(bannerAnimationData[i].wildBannerIdleAnimations));
		}
	}

// executeOnReevaluationSpinStart() section
// functions in this section are accessed by ReelGame.startNextReevaluationSpin()
	public override bool needsToExecuteOnReevaluationSpinStart()
	{
		return true;
	}

	public override IEnumerator executeOnReevaluationSpinStart()
	{
		// Clear any mutaiton from the previous spin
		wildBannerMutations.Clear();

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
					wildBannerMutations.Add(mutation);
				}
			}
		}

		yield break;
	}

// executeOnReevaluationReelsSpinning() section
// Handles what executePreReelsStopSpinning() does, but during the reevaulation spins
// Called from ReelGame.startNextReevaluationSpin()
	public override bool needsToExecuteOnReevaluationPreReelsStopSpinning()
	{
		return wildBannerMutations.Count > 0;
	}
	
	public override IEnumerator executeOnReevaluationPreReelsStopSpinning()
	{
		hasTriggeredBanners = true;

		for (int mutationIndex = 0; mutationIndex < wildBannerMutations.Count; mutationIndex++)
		{
			StandardMutation currentMutation = wildBannerMutations[mutationIndex];
			for (int reelListIndex = 0; reelListIndex < currentMutation.mutatedReels.Length; reelListIndex++)
			{
				for (int reelEntryIndex = 0; reelEntryIndex < currentMutation.mutatedReels[reelListIndex].Length; reelEntryIndex++)
				{
					int currentReelIndex = currentMutation.mutatedReels[reelListIndex][reelEntryIndex];
					WildBannerData animationDataForReel = getWildBannerDataForReel(currentReelIndex);

					// play the animations
					yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(animationDataForReel.wildBannerTriggeredAnimations));
				}
			}
		}

		yield break;
	}

// executeOnReevaluationReelsStoppedCallback() section
// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReevaluationReelsStoppedCallback()
	{
		return wildBannerMutations.Count > 0;
	}

	public override IEnumerator executeOnReevaluationReelsStoppedCallback()
	{
		for (int mutationIndex = 0; mutationIndex < wildBannerMutations.Count; mutationIndex++)
		{
			StandardMutation currentMutation = wildBannerMutations[mutationIndex];
			for (int reelListIndex = 0; reelListIndex < currentMutation.mutatedReels.Length; reelListIndex++)
			{
				for (int reelEntryIndex = 0; reelEntryIndex < currentMutation.mutatedReels[reelListIndex].Length; reelEntryIndex++)
				{
					int currentReelIndex = currentMutation.mutatedReels[reelListIndex][reelEntryIndex];
					string symbolReplacementName = currentMutation.mutatedSymbols[reelListIndex];

					// Convert all symbols under the banners into wilds
					SlotSymbol[] visibleSymbolsOnReel = reelGame.engine.getVisibleSymbolsAt(currentReelIndex);
					for (int visibleSymbolIndex = 0; visibleSymbolIndex < visibleSymbolsOnReel.Length; visibleSymbolIndex++)
					{
						visibleSymbolsOnReel[visibleSymbolIndex].mutateTo(symbolReplacementName);
						visibleSymbolsOnReel[visibleSymbolIndex].skipAnimationsThisOutcome();

						if (isHidingSymbolsUnderBanners)
						{
							visibleSymbolsOnReel[visibleSymbolIndex].gameObject.SetActive(false);
						}
					}
				}
			}
		}

		// In wonka04 this feature sends a `pending_bonus_summary_id` which needs to be handled
		// otherwise the game will think that the feature wasn't shown to the player.
		if (isSendingSeenBonusSummaryScreenResponse)
		{
			BonusSummary.processBonusSummary();
		}

		yield break;
	}

	// Get the wild banner animation info for a specific reel (which will come from the mutation)
	private WildBannerData getWildBannerDataForReel(int reelIndex)
	{
		for (int i = 0; i < bannerAnimationData.Count; i++)
		{
			if (bannerAnimationData[i].reelIndex == reelIndex)
			{
				return bannerAnimationData[i];
			}
		}

		Debug.LogError("SpinTillYouWinWithWildBannersRespinFeatureModule.getWildBannerDataForReel() - Unable to find data for reelIndex = " + reelIndex + "; returning NULL!");
		return null;
	}
}
