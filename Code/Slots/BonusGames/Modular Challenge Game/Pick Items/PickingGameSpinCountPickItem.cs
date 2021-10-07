using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickingGameSpinCountPickItem : PickingGameBasePickItemAccessor
{
	public LabelWrapperComponent spinCountLabel;
	public LabelWrapperComponent graySpinCountLabel;

	//Sets credit labels for revealed & leftovers
	public void setSpinCountLabel(int spinCount)
	{
		if (spinCountLabel != null)
		{
			spinCountLabel.text = "+" + CommonText.formatNumber(spinCount);
		}
		if (graySpinCountLabel != null)
		{
			graySpinCountLabel.text = "+" + CommonText.formatNumber(spinCount);
		}
	}
}
