using UnityEngine;
using System.Collections;

/**
 * Class for multiplier revealing pick items
 */
public class PickingGameMultiplierPickItem : PickingGameBasePickItemAccessor
{
	public LabelWrapperComponent multiplierLabel;
	public LabelWrapperComponent grayMultiplierLabel;

	//Sets credit labels for revealed & leftovers
	public void setMultiplierLabel(long multiplier)
	{
		if (multiplierLabel != null)
		{
			multiplierLabel.text = Localize.text("{0}X", multiplier);
		}
		if (grayMultiplierLabel != null)
		{
			grayMultiplierLabel.text = Localize.text("{0}X", multiplier);
		}
	}
}
