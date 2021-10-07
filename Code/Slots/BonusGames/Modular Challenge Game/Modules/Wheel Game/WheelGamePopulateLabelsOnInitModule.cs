using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * A module for displaying the labelComponent of a wheel with something determined by the child class.
 */
public abstract class WheelGamePopulateLabelsOnInitModule : WheelGameModule
{
	[SerializeField] private List<LabelWrapperComponent> wheelLabels;
	[SerializeField] private bool makeLabelFontSizesEqual = true;
	[SerializeField] protected string labelFormat;

	// Enable round init override
	public override bool needsToExecuteOnRoundInit()
	{
		return true;
	}
	
	// Executes on round init & populate the wheel values
	public override void executeOnRoundInit(ModularWheelGameVariant roundParent, ModularWheel wheel)
	{
		base.executeOnRoundInit(roundParent, wheel);
		populatePanelLabels();
	}

	private void populatePanelLabels()
	{
		// generate an ordered outcome list from the wins & leftovers
		List<ModularChallengeGameOutcomeEntry> wheelEntryList = wheelRoundVariantParent.outcome.getAllWheelPaytableEntriesForRound(wheelRoundVariantParent.roundIndex);

		if (wheelEntryList.Count != wheelLabels.Count)
		{
			Debug.LogErrorFormat("wheelEntryList ({0}) from data != wheelLabels ({1}) set in prefab", wheelEntryList.Count, wheelLabels.Count);
		}

		for (int i = 0; i < wheelLabels.Count; i++)
		{
			if (wheelLabels[i] != null)
			{
				populateLabel(wheelLabels[i], wheelEntryList[i]);
			}
		}

		if (makeLabelFontSizesEqual)
		{
			CommonText.makeLabelFontSizesEqual(wheelLabels);
		}
	}

	protected abstract void populateLabel(LabelWrapperComponent label, ModularChallengeGameOutcomeEntry entry);

	public override bool needsToExecuteOnNumberOfWheelSlicesChanged(int newSize)
	{
		return true;
	}

	public override void executeOnNumberOfWheelSlicesChanged(int newSize)
	{
		while (newSize >= wheelLabels.Count)
		{
			wheelLabels.Add(null); // Add an element to the end of the list.
		}
		while (newSize < wheelLabels.Count)
		{
			wheelLabels.RemoveAt(wheelLabels.Count - 1); // Remove the last element.
		}
	}
}
