using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Module to populate a wheel with credit values, uses a basebonusoutcome instead of a wheel outcome
 */
public class WheelGameBaseOutcomeCreditsModule : WheelGameCreditsModule
{
	[SerializeField] private bool ingoreMultiplier = false;
	
	protected override List<ModularChallengeGameOutcomeEntry> getAllPickValuesForRound(int round)
	{
		List<ModularChallengeGameOutcomeEntry> allWheelItems = new List<ModularChallengeGameOutcomeEntry>();
		List<RoundPicks> allPicks = wheelRoundVariantParent.outcome.getNewBaseBonusRoundPicks();
		if (allPicks == null || allPicks.Count <= round)
		{
			return null;
		}

		for (int i = 0; i < allPicks[round].entryCount; i++)
		{
			allWheelItems.Add(new ModularChallengeGameOutcomeEntry(allPicks[round].entries[i]));
		}

		for (int i = 0; i < allPicks[round].revealCount; i++)
		{
			allWheelItems.Add(new ModularChallengeGameOutcomeEntry(allPicks[round].reveals[i]));
		}
		return allWheelItems;
	}
	
	protected override void assignLabels(List<ModularChallengeGameOutcomeEntry> filteredCreditList)
	{
		for (int i = 0; i < wheelLabels.Length; i++)
		{
			if (filteredCreditList.Count <= i)
			{
				Debug.LogError("filteredCreditList only has " + filteredCreditList.Count + " entries for " + wheelLabels.Length + " labels");
				break;
			}
			long credits = filteredCreditList[i].baseCredits;  //use base credits
			wheelLabels[i].text = formatCredits(credits);
		}
	}
	
	protected override List<ModularChallengeGameOutcomeEntry> filterOutcomeListByCredits(List<ModularChallengeGameOutcomeEntry> unfilteredItems)
	{
		List<ModularChallengeGameOutcomeEntry> results = new List<ModularChallengeGameOutcomeEntry>();
		foreach (ModularChallengeGameOutcomeEntry wheelOutcome in unfilteredItems)
		{
			long credits = ingoreMultiplier ? wheelOutcome.baseCredits : wheelOutcome.credits;
			if (credits > 0)  
			{
				results.Add(wheelOutcome);
			}
		}
		return results;
	}
}
