using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataFirstPurchaseOffer : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return ExperimentWrapper.FirstPurchaseOffer.isInExperiment;
		}
	}

	public override bool show()
	{
		return FirstPurchaseOfferMOTD.showDialog();
	}

	new public static void resetStaticClassData(){}
}
