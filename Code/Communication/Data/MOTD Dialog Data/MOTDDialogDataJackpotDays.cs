using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataJackpotDays : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			
			return ExperimentWrapper.BuyPageProgressive.jackpotDaysIsActive && 
			       !ExperimentWrapper.FirstPurchaseOffer.isInExperiment &&
			       ProgressiveJackpot.buyCreditsJackpot != null &&
			       ProgressiveJackpot.buyCreditsJackpot.pool > 0;
		}
	}

	public override bool show()
	{
		return JackpotDaysMOTD.showDialog();
	}

	new public static void resetStaticClassData()
	{
	}

}
