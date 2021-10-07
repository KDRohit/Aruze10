//
// Populates a list of labels in a wheel game with game data from the possible win array
// for each wheel slice.
//
// Author : Shaun Peoples <speoples@zynga.com>
// Date : Sept 9th, 2020
// Games : orig002
//

using System.Collections;
using UnityEngine;

public class WheelGamePopulateLabelsWithEntryDataOnInitModule : WheelGamePopulateLabelsOnInitModule
{
	[SerializeField] bool shouldDisplayVertically;
	[SerializeField] private EntryDataDisplayType entryDataDisplayType;
	[SerializeField] private LabelWrapperComponent winLabel;

	enum EntryDataDisplayType
	{
		Spins
	}

	protected override void populateLabel(LabelWrapperComponent label, ModularChallengeGameOutcomeEntry entry)
	{
		if (label == null)
		{
			return;
		}

		string labelText = "";
		switch (entryDataDisplayType)
		{
			case EntryDataDisplayType.Spins:
				if (!string.IsNullOrEmpty(labelFormat))
				{
					labelText = string.Format(labelFormat, CommonText.formatNumber(entry.spins));
				}
				else
				{
					labelText = CommonText.formatNumber(entry.spins);
				}
				break;
		}

		label.text = shouldDisplayVertically ? CommonText.makeVertical(labelText) : labelText;
	}

	public override bool needsToExecuteOnSpinComplete()
	{
		return true;
	}

	public override IEnumerator executeOnSpinComplete()
	{
		if (winLabel == null)
		{
			yield break;
		}

		switch (entryDataDisplayType)
		{
			case EntryDataDisplayType.Spins:
				foreach (ModularChallengeGameOutcomeEntry entry in wheelRoundVariantParent.outcome.getCurrentRound().entries)
				{
					if (entry.spins > 0)
					{
						winLabel.text = CommonText.formatNumber(entry.spins);
						yield break;
					}
				}

				break;
			default:
				winLabel.text = "";
				break;
		}
	}
}