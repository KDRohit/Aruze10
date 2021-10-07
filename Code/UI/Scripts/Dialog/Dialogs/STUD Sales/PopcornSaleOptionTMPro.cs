using UnityEngine;
using System.Collections.Generic;

public class PopcornSaleOptionTMPro : STUDSaleOptionTMPro
{
	private int saleBonusPercent = 0;

	public override void setOption(CreditPackage dataPackage, STUDSale sale, List<PurchasePerksPanel.PerkType> perks = null, PurchasePerksCycler cycler = null)
	{
		base.setOption(dataPackage, sale, perks, cycler);

		saleBonusPercent = dataPackage.getSaleBonus();

		if (isPackageValid)
		{
			if (Data.liveData.getBool("NEW_POPCORN_PERCENT_AND_DISPLAY", false))
			{
				long totalCredits = creditPackage.totalCredits(bonusPercentage, false, saleBonusPercent);
				setLabels(saleBonusPercent, totalCredits);
			}
			else
			{
				setLabels(bonusPercentage, creditPackage.totalCredits(bonusPercentage));
			}
		}
	}

	// NGUI button callback.
	// Purchase the package associated with this option.
	public override void purchasePackage(Dict args = null)
	{
		if (Data.liveData.getBool("NEW_POPCORN_PERCENT_AND_DISPLAY", false))
		{
			logPurchase();
			creditPackage.makePurchase(bonusPercentage, false, -1, "", saleBonusPercent, collectablePack:collectablePackName, purchaseType:sale.featureData.type);	// Will close the dialog if purchase is successful.
		}
		else
		{
			base.purchasePackage();
		}
	}
}
