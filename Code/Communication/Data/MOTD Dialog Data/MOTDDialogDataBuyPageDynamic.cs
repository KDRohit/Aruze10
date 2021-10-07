using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataBuyPageDynamic : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return BuyPageDynamic.isEnabled;
		}
	}

	public override string noShowReason
	{
		get
		{
			string result = base.noShowReason;

			if (!BuyPageDynamic.isEnabled)
			{
				if (!ExperimentWrapper.DynamicBuyPageSurfacing.isInExperiment)
				{
					result += "Experiment is off.\n";
				}

				if (PurchaseFeatureData.isSaleActive)
				{
					result += "Buy Page Sale is active, so it should already pop.\n";
				}

				if (BuyPageDynamic.cooldownTimer == null)
				{
					result += "Cooldown Timer is null.\n";
				}
				else if (!BuyPageDynamic.cooldownTimer.isExpired)
				{
					// If the experiment is on, then the only reason it is off is the cooldown timer.
					result += "Still on Cooldown..\n";
				}

				if (ExperimentWrapper.FirstPurchaseOffer.isInExperiment)
				{
					result += "First Purchase Offer is enabled.\n";
				}
			}

			return result;
		}
	}

	public override bool show()
	{
		return BuyPageDynamic.show(keyName);
	}

	new public static void resetStaticClassData()
	{
	}
}
