using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * A module for displaying the labelComponent of a wheel with credit values.
 * Overrides entry.credits amount
 * Used in bonus games where the wheel slice credit amounts are dynamic and not stored in the bonus paytable (ex: quest for the chest) 
 */
public class WheelGameGrantAbsoluteCreditsModule : WheelGameModule
{
	public LabelWrapperComponent[] wheelCreditLabels;
	[SerializeField] private float delayBeforeRollup = 0.0f; // May need to delay the rollup start slightly so that the wheel_stop sound isn't aborted
	[SerializeField] private bool shouldDisplayVerticalCredits = true;
	[SerializeField] private bool shouldDisplayAbbreviatedCredits = false;

	private List<long> filteredCreditsList = new List<long>();
	private long creditsWon = 0;

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
	
	// Populates the wheel sequence from the outcomes available
	private void populateWheelSequence()
	{
		List<ModularChallengeGameOutcomeEntry> fullWheelEntryList = wheelRoundVariantParent.outcome.getAllWheelPaytableEntriesForRound(wheelRoundVariantParent.roundIndex);

		foreach (ModularChallengeGameOutcomeEntry wheelOutcome in fullWheelEntryList)
		{
			if (QuestForTheChest.QFCMiniGameOverlay.instance != null)
			{
				long absoluteCreditsAmount = QuestForTheChest.QFCMiniGameOverlay.instance.getAbsoulteCreditsValue(wheelOutcome.winID);
				if (absoluteCreditsAmount > 0)
				{
					filteredCreditsList.Add(absoluteCreditsAmount);
				}
			}
			else if (wheelOutcome.credits > 0)
			{
				filteredCreditsList.Add(wheelOutcome.credits);
			}
		}

		// iterate & populate
		for (int i = 0; i < wheelCreditLabels.Length; i++)
		{
			wheelCreditLabels[i].text = formatCredits(filteredCreditsList[i]);
		}
	}

	private string formatCredits(long credits)
	{
		string creditText = "";
		//Determine how the text looks
		if (shouldDisplayAbbreviatedCredits)
		{
			creditText = CommonText.formatNumberAbbreviated(CreditsEconomy.multipliedCredits(credits), 2, false);
		}
		else
		{
			creditText = CreditsEconomy.convertCredits(credits, false);
		}
		creditText = shouldDisplayVerticalCredits ? CommonText.makeVertical(creditText) : creditText;
		return creditText;
	}
	
	public override bool needsToExecuteOnSpinComplete()
	{
		creditsWon = wheelRoundVariantParent.outcome.getRound(wheelRoundVariantParent.roundIndex).entries[0].credits;
		if (QuestForTheChest.QFCMiniGameOverlay.instance != null)
		{
			creditsWon = QuestForTheChest.QFCMiniGameOverlay.instance.getAbsoulteCreditsValue(wheelRoundVariantParent.outcome.getRound(wheelRoundVariantParent.roundIndex).entries[0].winID);
		}
		return creditsWon > 0;
	}
	
	// Update the winnings label if we've won credits from this outcome
	public override IEnumerator executeOnSpinComplete()
	{
		// delay if we need to wait for the wheel_stop sound to finish
		if (delayBeforeRollup != 0.0f)
		{
			yield return new TIWaitForSeconds(delayBeforeRollup);
		}

		if (creditsWon > 0)
		{
			wheelRoundVariantParent.addCredits(creditsWon);
			yield return StartCoroutine(wheelRoundVariantParent.animateScore(0, creditsWon));
		}
	}
}
