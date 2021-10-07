using System.Collections.Generic;
using UnityEngine;

// Module to set the value of a label to be the initial starting count of freespins when
// freespins start. Used to set the header value in bettie02 "# freespins 2x"
//
// Author : Nick Saito <nsaito@zynga.com>
// Date : Sept 29th, 2019
//
// games : bettie02
public class InitFreespinCountLabelsSlotModule : SlotModule
{
	[SerializeField] private LabelWrapperComponent freespinCountLabel;

	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		return true;
	}

	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		// safely populate the spin panel labels
		populateLabels(freespinCountLabel, BonusSpinPanel.instance.spinCountLabel.text);
	}

	private void populateLabels(LabelWrapperComponent labelWrapper, string labelText)
	{
			if (labelWrapper != null && labelText != null)
			{
				labelWrapper.text = labelText;
			}
	}
}