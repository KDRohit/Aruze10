using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using QuestForTheChest;

/**
 * A module for displaying the labelComponent of a wheel with Quest for the Chest keys.
 * Used in bonus games where the wheel slice has a QFC key award
 */
public class WheelGameGrantQfcKeysModule : WheelGameModule
{
	public LabelWrapperComponent[] wheelLabels;
	[SerializeField] protected bool shouldDisplayVerticalCredits = true;
	[SerializeField] protected bool shouldDisplayAbbreviatedCredits = false;

	private int keysToGrant = 0;
	
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
		// generate an ordered outcome list from the wins & leftovers
		List<ModularChallengeGameOutcomeEntry> fullWheelEntryList = wheelRoundVariantParent.outcome.getAllWheelPaytableEntriesForRound(wheelRoundVariantParent.roundIndex);

		// filter out the qfc key results only
		List<ModularChallengeGameOutcomeEntry> filteredQfcKeysList = new List<ModularChallengeGameOutcomeEntry>();
		foreach (ModularChallengeGameOutcomeEntry wheelOutcome in fullWheelEntryList)
		{
			if (wheelOutcome.qfcKeys > 0)
			{
				filteredQfcKeysList.Add(wheelOutcome);
			}
		}

		// iterate & populate
		for (int i = 0; i < wheelLabels.Length; i++)
		{
			if (wheelLabels[i] == null)
			{
				Debug.LogWarning("Wheel Label isn't set up: " + i);
				continue;
			}

			if (filteredQfcKeysList != null && filteredQfcKeysList.Count > i && filteredQfcKeysList[i] != null)
			{
				wheelLabels[i].text = formatKeys(filteredQfcKeysList[i].qfcKeys);
			}
		}
	}

	private string formatKeys(int keys)
	{
		string creditText = "";
		//Determine how the text looks
		if (shouldDisplayAbbreviatedCredits)
		{
			creditText = CommonText.formatNumberAbbreviated(keys);
		}
		else
		{
			creditText = keys.ToString();
		}
		creditText = shouldDisplayVerticalCredits ? CommonText.makeVertical(creditText) : creditText;
		return creditText;
	}
	
	public override bool needsToExecuteOnSpinComplete()
	{
		keysToGrant = wheelRoundVariantParent.outcome.getRound(wheelRoundVariantParent.roundIndex).entries[0].qfcKeys;
		return keysToGrant > 0;
	}
	
	// Update the winnings label if we've won keys from this outcome
	public override IEnumerator executeOnSpinComplete()
	{
		// delay if we need to wait for the wheel_stop sound to finish
		
		if (keysToGrant > 0)
		{
			QuestForTheChestFeature.instance.awardKeys(SlotsPlayer.instance.socialMember.zId, keysToGrant);
		}
		yield break;
	}
}
