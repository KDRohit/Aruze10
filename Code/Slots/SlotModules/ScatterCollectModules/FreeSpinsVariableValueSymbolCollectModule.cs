using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Module for handling the variable value freespins SC results
 * Mutations exist for having SC symbols landing to award credits and additional freespins: symbol_landing_award_free_spins and symbol_landing_award_credits
 * This module processes those values and awards the spins and credits appropriately
 *
 * Author : Shaun Peoples <speoples@zynga.com>
 * First Use : Orig001
 */
public class FreeSpinsVariableValueSymbolCollectModule : SlotModule
{
	[SerializeField] private AnimationListController.AnimationInformationList animationsToPlayOnSymbolAddsFreeSpin;
	[SerializeField] private float delayAfterFreeSpinsAdded;

	[SerializeField] private List<string> creditAwardingSymbolsServerNames;
	[SerializeField] private List<string> spinAwardingSymbolsServerNames;

	[System.NonSerialized] private List<SlotSymbol> creditAwardingSymbols = new List<SlotSymbol>();
	[System.NonSerialized] private List<SlotSymbol> spinAwardingSymbols = new List<SlotSymbol>();

	[Header("RollUp Settings")]
	[SerializeField] private float rollupEachSpinTime = 0.0f;
	[SerializeField] private float postRollupWait;
	[SerializeField] private AudioListController.AudioInformationList rollupFanfareAudioList;
	[SerializeField] private bool shouldLoopSymbolAnimationsOnRollup = false;
	[SerializeField] private AnimatedParticleEffect rollupParticleEffect;

	[Header("Animated Particle Effects")]
	[SerializeField] private AnimatedParticleEffect trailFromFreeSpinSymbolToFreeSpinBox;
	[SerializeField] private AudioListController.AudioInformationList addFreeSpinsSounds;
	[SerializeField] private AnimatedParticleEffect trailFromCreditSymbolToWinBox;
	[SerializeField] private AudioListController.AudioInformationList creditTrailSounds;
	[SerializeField] private bool shouldResetTrailCompleteSoundsOnStart;
	
	private bool hasRollupFinished = false;
	private bool symbolsDonePlaying = true;
	
	private StandardMutation symbolLandingAwardCreditsMutation = null;
	private StandardMutation symbolLandingAwardFreeSpinsMutation = null;

	#region slotmodule overrides

	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		return shouldResetTrailCompleteSoundsOnStart;
	}

	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		if (shouldResetTrailCompleteSoundsOnStart)
		{
			addFreeSpinsSounds.resetCollection();
		}

		if (shouldResetTrailCompleteSoundsOnStart)
		{
			creditTrailSounds.resetCollection();
		}
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return true;
	}

	public override bool needsToExecutePreReelsStopSpinning()
	{
		return true;
	}

	public override IEnumerator executePreReelsStopSpinning()
	{
		updateReplacementSymbols();
		getApplicableMutations();
		yield break;
	}

	public override bool needsToExecuteAfterSymbolSetup(SlotSymbol symbol)
	{
		return symbolLandingAwardCreditsMutation != null;
	}

	public override void executeAfterSymbolSetup(SlotSymbol symbol)
	{
		foreach (StandardMutation.CreditOrSpinAwardingSymbol creditOrSpinAwardingSymbol in symbolLandingAwardCreditsMutation.symbolLandingAwardCreditSymbols)
		{
			if (symbol.serverName == creditOrSpinAwardingSymbol.symbolServerName)
			{
				LabelWrapperComponent symbolLabel = symbol.getDynamicLabel();

				if (symbolLabel != null)
				{
					symbolLabel.text = CreditsEconomy.multiplyAndFormatNumberAbbreviated(creditOrSpinAwardingSymbol.credits * reelGame.multiplier, shouldRoundUp: false);
				}
			}
		}
	}

	// When the reel stops, it's time to lock the symbols, handle awarded freespins,
	// and do a credit rollup if required.
	// We also allow resetting sound collections so they play from the beginning as they are
	// used in the handling of freespin awarding.
	public override IEnumerator executeOnReelsStoppedCallback()
	{
		if (symbolLandingAwardCreditsMutation == null && symbolLandingAwardFreeSpinsMutation == null)
		{
			yield break;
		}
		
		creditAwardingSymbols.Clear();
		spinAwardingSymbols.Clear();
		
		//make lists of symbols that will be used in the visual part of reward
		foreach (SlotSymbol symbol in reelGame.engine.getAllVisibleSymbols())
		{
			if (creditAwardingSymbolsServerNames.Contains(symbol.serverName))
			{
				creditAwardingSymbols.Add(symbol);
			}

			if (spinAwardingSymbolsServerNames.Contains(symbol.serverName))
			{
				spinAwardingSymbols.Add(symbol);
			}
		}

		yield return StartCoroutine(handleExtraSpinsAwarded());
		yield return StartCoroutine(handleSymbolCreditAward());
	}
	
	private void updateReplacementSymbols()
	{
		//need to set the replacement symbols from the outcome or we get the wrong symbols
		Dictionary<string, string> normalReplacementSymbolMap = new Dictionary<string, string>();

		reelGame.mutationManager.setMutationsFromOutcome(reelGame.outcome.getJsonObject());
		
		if (reelGame.mutationManager != null && reelGame.mutationManager.mutations != null)
		{
			for (int i = 0; i < reelGame.mutationManager.mutations.Count; i++)
			{
				MutationBase mutation = reelGame.mutationManager.mutations[i];
				StandardMutation replaceSymbolMutation = mutation as StandardMutation;

				if (mutation.type == "symbol_replace_multi" && replaceSymbolMutation != null)
				{
					foreach (KeyValuePair<string, string> normalReplaceInfo in replaceSymbolMutation.normalReplacementSymbolMap)
					{
						if (!normalReplacementSymbolMap.ContainsKey(normalReplaceInfo.Key))
						{
							normalReplacementSymbolMap.Add(normalReplaceInfo.Key, normalReplaceInfo.Value);
						}
					}
				}
			}
		}

		if (normalReplacementSymbolMap.Count > 0)
		{
			reelGame.engine.setReplacementSymbolMap(normalReplacementSymbolMap, null, isApplyingNow: true);
		}
	}

	// Get applicable mutations for this spin
	private bool getApplicableMutations()
	{
		symbolLandingAwardCreditsMutation = null;
		symbolLandingAwardFreeSpinsMutation = null;
		
		reelGame.mutationManager.setMutationsFromOutcome(reelGame.outcome.getJsonObject());

		if (reelGame.mutationManager != null && reelGame.mutationManager.mutations == null)
		{
			return false;
		}

		bool mutationsFound = false;
		foreach (MutationBase mutation in reelGame.mutationManager.mutations)
		{
			switch (mutation.type)
			{
				case "symbol_landing_award_credits":
					symbolLandingAwardCreditsMutation = mutation as StandardMutation;
					mutationsFound = true;
					break;
				case "symbol_landing_award_free_spins":
					symbolLandingAwardFreeSpinsMutation = mutation as StandardMutation;
					mutationsFound = true;
					break;
			}
		}

		return mutationsFound;
	}

	// Awards freespins, updates the number of symbols that are locked in position,
	// and tracks wager multiplier and credits awarded
	private IEnumerator handleExtraSpinsAwarded()
	{
		if (symbolLandingAwardFreeSpinsMutation == null)
		{
			yield break;
		}
		
		int totalNumberOfFreeSpinsAwarded = symbolLandingAwardFreeSpinsMutation.numberOfFreeSpinsAwarded;

		if (totalNumberOfFreeSpinsAwarded < 1)
		{
			yield break;
		}

		List<TICoroutine> visualEffectCoroutines = new List<TICoroutine>();
		foreach (SlotSymbol symbol in spinAwardingSymbols)
		{
			visualEffectCoroutines.Add(StartCoroutine(trailFromFreeSpinSymbolToFreeSpinBox.animateParticleEffect(symbol.transform)));
			visualEffectCoroutines.Add(StartCoroutine(symbol.playAndWaitForAnimateOutcome()));
		}
		
		visualEffectCoroutines.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(animationsToPlayOnSymbolAddsFreeSpin)));
		
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(visualEffectCoroutines));

		if (addFreeSpinsSounds != null)
		{
			yield return StartCoroutine(AudioListController.playListOfAudioInformation(addFreeSpinsSounds));
		}
		
		FreeSpinGame.instance.numberOfFreespinsRemaining += totalNumberOfFreeSpinsAwarded;

		if (delayAfterFreeSpinsAdded > 0)
		{
			yield return new TIWaitForSeconds(delayAfterFreeSpinsAdded);
		}
	}

	// Animates symbols and performs rolling up of credits if anything was won on the spin
	private IEnumerator handleSymbolCreditAward()
	{
		if (symbolLandingAwardCreditsMutation == null)
		{
			yield break;
		}
		
		long mutationsTotalCreditsAwarded = symbolLandingAwardCreditsMutation.creditsAwarded;

		if (mutationsTotalCreditsAwarded <= 0)
		{
			yield break;
		}

		hasRollupFinished = false;
		long creditsAwarded = mutationsTotalCreditsAwarded * reelGame.multiplier;
		
		List<TICoroutine> visualEffectCoroutines = new List<TICoroutine>();
		foreach (SlotSymbol symbol in creditAwardingSymbols)
		{
			visualEffectCoroutines.Add(StartCoroutine(symbol.playAndWaitForAnimateOutcome()));
			visualEffectCoroutines.Add(StartCoroutine(trailFromCreditSymbolToWinBox.animateParticleEffect(symbol.transform)));
		}

		if (visualEffectCoroutines.Count > 0)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(visualEffectCoroutines));
		}
		
		yield return StartCoroutine(rollupWinnings(creditsAwarded, rollupEachSpinTime));
		hasRollupFinished = true;

		while (!symbolsDonePlaying)
		{
			yield return null;
		}
	}

	#endregion
	
	#region rollup
	private IEnumerator rollupWinnings(long creditsAwarded, float rollupTime = 0.0f)
	{
		if (rollupFanfareAudioList != null)
		{
			AudioListController.playListOfAudioInformation(rollupFanfareAudioList);
		}

		if (rollupParticleEffect != null)
		{
			yield return StartCoroutine(rollupParticleEffect.animateParticleEffect());
		}
		
		//Wait for the rollup to finish animating
		yield return StartCoroutine(reelGame.rollupCredits(0,
			creditsAwarded,
			ReelGame.activeGame.onPayoutRollup,
			isPlayingRollupSounds: true,
			specificRollupTime: rollupTime,
			shouldSkipOnTouch: true,
			allowBigWin: false));
		
		yield return new TIWaitForSeconds(postRollupWait);

		if (rollupParticleEffect != null)
		{
			rollupParticleEffect.stopAllParticleEffects();
		}
	}
	#endregion
}
