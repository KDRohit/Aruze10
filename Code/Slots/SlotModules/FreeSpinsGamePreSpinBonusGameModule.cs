using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Class that runs and blocks the freespins starting until an initial bonus game is complete.
First game which will use this is gen75 Tinker Treasure for a wheel game portal intro.

Creation Date: 2/9/2018
Original Author: Scott Lepthien
*/
public class FreeSpinsGamePreSpinBonusGameModule : SlotModule 
{
	[SerializeField] private ModularChallengeGame challengeGame;    // Challenge game triggered when the freespins game starts
	[SerializeField] private AnimationListController.AnimationInformationList preBonusAnimations;
	[SerializeField] private AnimationListController.AnimationInformationList postBonusAnimations;
	[SerializeField] private bool isCancelingLoopedMusicOnModuleStart = false;
	[SerializeField] private bool useSimplePickemOutcome = false;

	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		return true;
	}
	
	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		if (isCancelingLoopedMusicOnModuleStart)
		{
			Audio.switchMusicKeyImmediate("");
		}
	
		if (preBonusAnimations != null && preBonusAnimations.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(preBonusAnimations));
		}

		if (challengeGame != null)
		{
			List<ModularChallengeGameOutcome> variantOutcomeList = new List<ModularChallengeGameOutcome>();

			// since each variant will use the same outcome we need to add as many outcomes as there are variants setup
			for (int m = 0; m < challengeGame.pickingRounds[0].roundVariants.Length; m++)
			{
				variantOutcomeList.Add(createOutcomeForBonus());
			}

			challengeGame.addVariantOutcomeOverrideListForRound(0, variantOutcomeList);
			challengeGame.init();
			challengeGame.gameObject.SetActive(true);

			// wait till this challenge game feature is over before continuing
			while (!challengeGame.hasBonusGameEnded)
			{
				yield return null;
			}

			challengeGame.reset();
		}

		if (postBonusAnimations != null && postBonusAnimations.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(postBonusAnimations));
		}

		// turn the spin panel back on, even if we are going to slide it in, we need to have it on when we do that
		SpinPanel.instance.showPanel(SpinPanel.Type.FREE_SPINS);
	}

	protected virtual ModularChallengeGameOutcome createOutcomeForBonus()
	{
		if (useSimplePickemOutcome)
		{
			return createOutcomeForSimplePickemBonus();
		}

		// @note : Not really sure how this would work in a game that had data, since we are basically just faking the outcome for gen75 Tinker Treasure
		NewBaseBonusGameOutcome bonusOutcome = new NewBaseBonusGameOutcome(BonusGameManager.currentBonusGameOutcome, isUsingBaseGameMultiplier: true);
		// Convert our outcome to ModularChallengeGameOutcome
		ModularChallengeGameOutcome modularBonusOutcome = new ModularChallengeGameOutcome(bonusOutcome);

		return modularBonusOutcome;
	}
	
	protected virtual ModularChallengeGameOutcome createOutcomeForSimplePickemBonus()
	{
		SimplePickemOutcome simplePickemOutcome = new SimplePickemOutcome(BonusGameManager.currentBonusGameOutcome);
		ModularChallengeGameOutcome modularBonusOutcome = new ModularChallengeGameOutcome(simplePickemOutcome);
		return modularBonusOutcome;
	}
}
