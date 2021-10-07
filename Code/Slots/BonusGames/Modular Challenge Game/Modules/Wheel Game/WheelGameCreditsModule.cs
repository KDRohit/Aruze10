using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Module to populate a wheel with credit values
 */
public class WheelGameCreditsModule : WheelGameModule
{
	public LabelWrapperComponent[] wheelLabels;
	[SerializeField] protected float delayBeforeRollup = 0.0f; // May need to delay the rollup start slightly so that the wheel_stop sound isn't aborted
	[SerializeField] protected Animator wheelWedgeCelebrationAnimator = null;
	[SerializeField] protected string WHEEL_WEDGE_CELEBRATION_ANIMATION_NAME = "anim";
	[SerializeField] protected bool shouldDisplayVerticalCredits = true;
	[SerializeField] protected bool shouldDisplayAbbreviatedCredits = false;

	// Enable round init override
	public override bool needsToExecuteOnRoundInit()
	{
		return true;
	}

	// Executes on round init & populate the wheel values
	public override void executeOnRoundInit(ModularWheelGameVariant roundParent, ModularWheel wheel)
	{
		base.executeOnRoundInit(roundParent, wheel);
		populateWheelSequence();
	}

	// Returns an ordered outcome list from the wins and leftovers
	protected virtual List<ModularChallengeGameOutcomeEntry> getAllPickValuesForRound(int round)
	{
		 return wheelRoundVariantParent.outcome.getAllWheelPaytableEntriesForRound(round);
	}
	
	// Populates the wheel sequence from the outcomes available
	private void populateWheelSequence()
	{
		//get all entries
		List<ModularChallengeGameOutcomeEntry> fullWheelEntryList = getAllPickValuesForRound(wheelRoundVariantParent.roundIndex);
		// filter out the credit results only
		List<ModularChallengeGameOutcomeEntry>filteredCreditList = filterOutcomeListByCredits(fullWheelEntryList);
		// iterate & populate
		assignLabels(filteredCreditList);
	}

	protected virtual List<ModularChallengeGameOutcomeEntry> filterOutcomeListByCredits(List<ModularChallengeGameOutcomeEntry> unfilteredItems)
	{
		List<ModularChallengeGameOutcomeEntry> results = new List<ModularChallengeGameOutcomeEntry>();
		foreach (ModularChallengeGameOutcomeEntry wheelOutcome in unfilteredItems)
		{
			if (wheelOutcome.credits > 0)
			{
				results.Add(wheelOutcome);
			}
		}
		return results;
	}

	protected virtual void assignLabels(List<ModularChallengeGameOutcomeEntry> filteredCreditList)
	{
		for (int i = 0; i < wheelLabels.Length; i++)
		{
			if (filteredCreditList.Count <= i)
			{
				Debug.LogError("filteredCreditList only has " + filteredCreditList.Count + " entries for " + wheelLabels.Length + " labels");
				break;
			}
			long credits = filteredCreditList[i].credits;
			wheelLabels[i].text = formatCredits(credits);
		}
	}

	protected string formatCredits(long credits)
	{
		string creditText = "";
		//Determine how the text looks
		if (shouldDisplayAbbreviatedCredits)
		{
			creditText = CommonText.formatNumberAbbreviated(CreditsEconomy.multipliedCredits(credits));
		}
		else
		{
			creditText = CreditsEconomy.convertCredits(credits, false);
		}
		creditText = shouldDisplayVerticalCredits ? CommonText.makeVertical(creditText) : creditText;
		return creditText;
	}

	// Enable spin complete callback
	public override bool needsToExecuteOnSpinComplete()
	{
		return true;
	}
	
	// Update the winnings label if we've won credits from this outcome
	public override IEnumerator executeOnSpinComplete()
	{
		// play the wedge celebration
		if (wheelWedgeCelebrationAnimator != null)
		{
			wheelWedgeCelebrationAnimator.Play(WHEEL_WEDGE_CELEBRATION_ANIMATION_NAME);
		}

		// delay if we need to wait for the wheel_stop sound to finish
		if (delayBeforeRollup != 0.0f)
		{
			yield return new TIWaitForSeconds(delayBeforeRollup);
		}

		long creditsWon = wheelRoundVariantParent.outcome.getRound(wheelRoundVariantParent.roundIndex).entries[0].credits;
		if (creditsWon > 0)
		{
			wheelRoundVariantParent.addCredits(creditsWon);
			yield return StartCoroutine(wheelRoundVariantParent.animateScore(0, creditsWon));
		}
	}
}
