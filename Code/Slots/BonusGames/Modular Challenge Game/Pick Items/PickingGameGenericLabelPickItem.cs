using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Class to handle a reveal with a generic text label that can be set.  This will hopefully
 * be a bit more resuable rather than making a new type for each thing we want to reveal
 * if all we need is a label to set text on.
 *
 * Original Author: Scott Lepthien
 * Creation Date: 1/28/2021
 */
public class PickingGameGenericLabelPickItem : PickingGameBasePickItemAccessor
{
	[SerializeField] private LabelWrapperComponent genericLabel;

	// Sets the generic label text (can use MultiLabelWrapperComponent if you want to set more than one label at once) 
	public void setGenericLabel(string labelText)
	{
		if (genericLabel != null)
		{
			genericLabel.text = labelText;
		}
	}
}
