using UnityEngine;
using System.Collections;

/*
Picking games may have a max cards picked bonus.
For example, ghostubsters01 has 14 cards, where 3 of those cards are bad.
So if you get the max 14-3=11th card, then you get an 11+4=15X multiplier bonus.
*/	

public class PickingGameMultiplyCreditsWithMaxCardsPickedBonus : PickingGameMultiplyCreditsModule
{
	// Increase Multiplier
	[SerializeField] private string REACHED_MAX_MULTIPLIER_SOUND = "";
	protected override int getBonusMultiplier(ModularChallengeGameOutcomeEntry outcomeEntry)
	{
		int multiplierBonus = 0;
		
		int maxCardsPicked = roundVariantParent.outcome.maxCardsPicked;
		if (roundVariantParent.gameParent.getTotalPicksMade() + 1 == maxCardsPicked)
		{
			if (!string.IsNullOrEmpty(REACHED_MAX_MULTIPLIER_SOUND))
			{
				ADVANCE_MULTIPLIER_SOUND = REACHED_MAX_MULTIPLIER_SOUND;
			}
			multiplierBonus = roundVariantParent.outcome.multiplier;
		}
				
		return multiplierBonus;
	}
	
	protected override bool shouldIncreaseCurrentMultiplier(ModularChallengeGameOutcomeEntry outcomeEntry)
	{
		int maxCardsPicked = roundVariantParent.outcome.maxCardsPicked;
		
		if (roundVariantParent.gameParent.getTotalPicksMade() == maxCardsPicked)
		{
			// You got the max cards picked bonus, the game is over,
			// so don't increase the current multiplier again.
			return false;
		}
		
		return base.shouldIncreaseCurrentMultiplier(outcomeEntry);
	}

	// Allowing this to be overriden if a game has a special case where most 
	// times the rollup isn't blocking, but for the last rollup it needs to block
	protected override bool shouldWaitForRollupTotalCredits()
	{
		int maxCardsPicked = roundVariantParent.outcome.maxCardsPicked;
		
		if (roundVariantParent.gameParent.getTotalPicksMade() == maxCardsPicked)
		{
			// make sure the last 
			return true;
		}
		else
		{
			return SHOULD_WAIT_FOR_ROLLUP_TOTAL_CREDITS;
		}
	}

	// Allowing this to be overriden if a game has a special case where most 
	// times the rollup isn't blocking, but for the last rollup it needs to block
	protected override bool shouldWaitForRollupToEnd()
	{
		int maxCardsPicked = roundVariantParent.outcome.maxCardsPicked;
		
		if (roundVariantParent.gameParent.getTotalPicksMade() == maxCardsPicked)
		{
			// make sure the last 
			return true;
		}
		else
		{
			return SHOULD_WAIT_FOR_ROLLUP_TO_END;
		}
	}
}
