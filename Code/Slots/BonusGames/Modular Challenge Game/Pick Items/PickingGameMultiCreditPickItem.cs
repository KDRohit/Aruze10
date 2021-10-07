using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Class specific to credit reveals on pick items
 */
public class PickingGameMultiCreditPickItem : PickingGameCreditPickItem
{
	public List<LabelWrapperComponent> extraCreditLabels;
	public List<LabelWrapperComponent> extraGrayCreditLabels;

	//Sets credit labels for revealed & leftovers
	public override void setCreditLabels(long credits)
	{
		string creditText = CreditsEconomy.convertCredits(credits);

		// set all the extra credit labels
		foreach(LabelWrapperComponent label in extraCreditLabels)
		{
			label.text = creditText;
		}
		foreach(LabelWrapperComponent grayLabel in extraGrayCreditLabels)
		{
			grayLabel.text = creditText;
		}
	}
}
