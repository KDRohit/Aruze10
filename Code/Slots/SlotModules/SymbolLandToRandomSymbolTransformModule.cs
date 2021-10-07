//
// Transforms symbols on the reels based on a trigger symbol. This works with mutation type "transform_symbols_from_number_of_symbols_landed"
// and when the mutation is present it will transform the symbols specified in the mutation.
//
// If a trigger symbol lands on the reels, but no mutation is present, it will collect the trigger symbol and play a
// random animation from featureMissedAnimations.
//
// Author : Nick Saito <nsaito@zynga.com>
// Date : August 10, 2020
// Games : orig002
//

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SymbolLandToRandomSymbolTransformModule : SlotModule
{
#region serialized member variables

	[Tooltip("List of symbols names that can trigger the feature to attempt to transform symbols")]
	[SerializeField] private List<string> triggerSymbols;

	[Tooltip("Animations that are played when a trigger symbol lands and transforms the symbols")]
	[SerializeField] private List<FeatureAwardedAnimation> featureAwardedAnimations;

	[Tooltip("List of animation lists that are played at random when a trigger symbol lands but does not transform the symbols")]
	[SerializeField] private List<FeatureMissedAnimation> featureMissedAnimations;

	[Tooltip("Animate a particle effect from the landed trigger symbols to the feature animations")]
	[SerializeField] private AnimatedParticleEffect triggerSymbolCollectParticleEffect;

	[Tooltip("Add a delay when collecting trigger symbols")]
	[SerializeField] private float triggerSymbolCollectDelay;

	[Tooltip("Play a particle effect from feature animation to symbols that will be mutated")]
	[SerializeField] private AnimatedParticleEffect featureToSymbolMutateParticleEffect;

	[Tooltip("Add a delay between each particle trail when mutating symbols")]
	[SerializeField] private float featureToSymbolMutateDelay;

	[Tooltip("Time it takes for particle trail to arrive at a symbol to mutate")]
	[SerializeField] private float symbolMutateDelay;

	[Tooltip("Play a particle effect over mutating symbols")]
	[SerializeField] private AnimatedParticleEffect symbolMutateParticleEffect;

#endregion

#region private member variables

	private SlotReel[] slotReels;
	private List<TICoroutine> allCoroutines;

	// This is used to build up the final list of random animation lists depeding on the amount of trigger symbols landed.
	private List<FeatureMissedAnimation> featureMissedAnimationsComplete = new List<FeatureMissedAnimation>();

#endregion

#region slotmodule overrides

	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		return true;
	}

	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		allCoroutines = new List<TICoroutine>();
		slotReels = reelGame.engine.getAllSlotReels();
	}

	// This feature should when the reels stop unless there is a bonus game
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		if (reelGame.outcome.hasBonusGame())
		{
			return false;
		}

		return shouldExecuteFeature();
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		yield return StartCoroutine(executeFeature());
	}

	// If a bonus game was played we should collect the trigger symbols and
	// do the feature after the bonus game is complete.
	public override bool needsToExecuteOnBonusGameEnded()
	{
		return shouldExecuteFeature();
	}

	public override IEnumerator executeOnBonusGameEnded()
	{
		yield return StartCoroutine(executeFeature());
		SlotBaseGame.instance.doShowNonBonusOutcomes();
	}

	// This allows us to play the feature after coming back from the bonus game
	// and before the paylines start playing again. We manually call doShowNonBonusOutcomes
	// after the feature is completed in executeOnBonusGameEnded.
	public override bool needsToLetModuleTransitionBeforePaylines ()
	{
		return shouldExecuteFeature();
	}

#endregion

#region helper methods

	private IEnumerator playFeatureAnimations(string replaceSymbol)
	{
		foreach (FeatureAwardedAnimation featureAwardedAnimation in featureAwardedAnimations)
		{
			if (featureAwardedAnimation.animationName == replaceSymbol)
			{
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(featureAwardedAnimation.animationInformationList));
			}
		}
	}

	private IEnumerator playAnimatedParticleEffects()
	{
		foreach (SlotReel slotReel in slotReels)
		{
			foreach (SlotSymbol slotSymbol in slotReel.visibleSymbols)
			{
				if (triggerSymbols.Contains(slotSymbol.serverName))
				{
					yield return StartCoroutine(triggerSymbolCollectParticleEffect.animateParticleEffect(slotSymbol.transform));

					if (triggerSymbolCollectDelay > 0)
					{
						yield return new WaitForSeconds(triggerSymbolCollectDelay);
					}
				}
			}
		}
	}

	// Run through the replacement cells and get the SlotSymbol there. Send a particle effect
	// to the symbol, and then mutate it to the new replacement symbol.
	private IEnumerator mutateSymbols(List<StandardMutation.ReplacementCell> symbolsToMutate)
	{
		foreach (StandardMutation.ReplacementCell replacementCell in symbolsToMutate)
		{
			// get the symbol to mutate
			SlotSymbol slotSymbol = getSlotSymbolFromReplacementCell(replacementCell);

			// send a particle effect to the symbol
			allCoroutines.Add(StartCoroutine(featureToSymbolMutateParticleEffect.animateParticleEffect(null, slotSymbol.transform)));
			allCoroutines.Add(StartCoroutine(mutateSymbol(replacementCell)));

			if (featureToSymbolMutateDelay > 0)
			{
				yield return new WaitForSeconds(featureToSymbolMutateDelay);
			}
		}
	}

	private IEnumerator mutateSymbol(StandardMutation.ReplacementCell replacementCell)
	{
		if (symbolMutateDelay > 0)
		{
			yield return new WaitForSeconds(symbolMutateDelay);
		}

		SlotSymbol slotSymbol = getSlotSymbolFromReplacementCell(replacementCell);
		allCoroutines.Add(StartCoroutine(symbolMutateParticleEffect.animateParticleEffect(slotSymbol.transform)));
		slotSymbol.mutateTo(replacementCell.replaceSymbol);
	}

	private SlotSymbol getSlotSymbolFromReplacementCell(StandardMutation.ReplacementCell replacementCell)
	{
		return slotReels[replacementCell.reelIndex].visibleSymbolsBottomUp[replacementCell.symbolIndex];
	}

	// Check if a trigger symbol landed on the reels
	private bool shouldExecuteFeature()
	{
		foreach (SlotReel slotReel in slotReels)
		{
			foreach (SlotSymbol slotSymbol in slotReel.visibleSymbols)
			{
				if (triggerSymbols.Contains(slotSymbol.serverName))
				{
					return true;
				}
			}
		}

		return false;
	}

	public IEnumerator executeFeature()
	{
		allCoroutines.Clear();

		// play animated particle effect from trigger symbols to the feature target.
		yield return StartCoroutine(playAnimatedParticleEffects());

		bool isFeatureTriggered = false;

		// check the mutations to see if the feature is actually triggered
		if (reelGame.mutationManager != null && reelGame.mutationManager.mutations != null)
		{
			foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
			{
				StandardMutation mutation = baseMutation as StandardMutation;

				if (mutation.type == "transform_symbols_from_number_of_symbols_landed")
				{
					isFeatureTriggered = true;

					foreach (string replaceSymbol in mutation.symbolReplacementCells.Keys)
					{
						yield return StartCoroutine(playFeatureAnimations(replaceSymbol));
						allCoroutines.Add(StartCoroutine(mutateSymbols(mutation.symbolReplacementCells[replaceSymbol])));
					}
				}
			}
		}

		if (!isFeatureTriggered)
		{
			// play random failed feature animation
			allCoroutines.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(getRandomAnimationList())));
		}

		yield return StartCoroutine(Common.waitForCoroutinesToEnd(allCoroutines));
	}

	private AnimationListController.AnimationInformationList getRandomAnimationList()
	{
		buildRandomAnimationList();
		int failedAnimationIndex = UnityEngine.Random.Range(0, featureMissedAnimationsComplete.Count);
		return featureMissedAnimationsComplete[failedAnimationIndex].animations;
	}

	private void buildRandomAnimationList()
	{
		featureMissedAnimationsComplete.Clear();
		int numTriggerSymbols = getNumberOfTriggerSymbols();
		foreach (FeatureMissedAnimation featureMissedAnimation in featureMissedAnimations)
		{
			if (numTriggerSymbols >= featureMissedAnimation.minTriggers && numTriggerSymbols <= featureMissedAnimation.maxTriggers)
			{
				featureMissedAnimationsComplete.Add(featureMissedAnimation);
			}
		}
	}

	private int getNumberOfTriggerSymbols()
	{
		int numTriggerSymbols = 0;
		foreach (SlotReel slotReel in slotReels)
		{
			foreach (SlotSymbol slotSymbol in slotReel.visibleSymbols)
			{
				if (triggerSymbols.Contains(slotSymbol.serverName))
				{
					numTriggerSymbols++;
				}
			}
		}

		return numTriggerSymbols;
	}

#endregion

#region data classes

	// animations to play when a symbol is transformed
	[Serializable]
	struct FeatureAwardedAnimation
	{
		[Tooltip("Symbol or name that is used for this animation")]
		public string animationName;

		[Tooltip("Animation to play when feature is activated")]
		public AnimationListController.AnimationInformationList animationInformationList;
	}

	[Serializable]
	struct FeatureMissedAnimation
	{
		public string name;
		[Tooltip("min number of triggers that need to land trigger this animation")]
		public int minTriggers;

		[Tooltip("max number of triggers that can to land and trigger this animation")]
		public int maxTriggers;

		[Tooltip("feature missed animation list")]
		public AnimationListController.AnimationInformationList animations;
	}

#endregion
}

