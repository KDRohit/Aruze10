using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Module to handle revealing bonus game pick during a picking round
 */
public class PickingGamePlayStackedNestedBonusAfterRevealModule : PickingGameModule
{
	[SerializeField] private string bonusGameName;
	
	[SerializeField] private BonusGamePresenter challengePresenter;
	[SerializeField] private ModularChallengeGame challengeGame;

	public override bool needsToExecuteOnAdvancePick()
	{
		ModularChallengeGameOutcomeEntry prevPick = pickingVariantParent.getPreviousPickOutcome();

		if (prevPick != null && prevPick.nestedBonusOutcome != null && prevPick.nestedBonusOutcome.getBonusGame() == bonusGameName)
		{
			return true;
		}

		return false;	
	}

	public override IEnumerator executeOnAdvancePick()
	{
		BonusGamePresenter parentPresenter = BonusGamePresenter.instance;
		challengePresenter.gameObject.SetActive(true);
		BonusGameManager.instance.swapToPassedInBonus(challengePresenter, false, false);
		challengePresenter.isReturningToBaseGameWhenDone = false;
		challengePresenter.init(isCheckingReelGameCarryOverValue:false);

		List<ModularChallengeGameOutcome> variantOutcomeList = new List<ModularChallengeGameOutcome>();
		ModularChallengeGameOutcome convertedOutcome = new ModularChallengeGameOutcome(pickingVariantParent.getPreviousPickOutcome().nestedBonusOutcome, false, pickingVariantParent.outcome.dynamicBaseCredits);
		// since each variant will use the same outcome we need to add as many outcomes as there are variants setup
		for (int m = 0; m < challengeGame.pickingRounds[0].roundVariants.Length; m++)
		{
			variantOutcomeList.Add(convertedOutcome);
		}

		challengeGame.addVariantOutcomeOverrideListForRound(0, variantOutcomeList);
		challengeGame.init();

		while (challengePresenter.isGameActive)
		{
			yield return null;
		}
		challengeGame.reset();
		BonusGamePresenter.instance = parentPresenter;
	}
}

