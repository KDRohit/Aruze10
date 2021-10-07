using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using TMPro;


public class STUDSaleOptionTMProHIR : STUDSaleOptionTMPro
{
	public GameObject linkedVIPLogo;
	public override void setOption(CreditPackage dataPackage, STUDSale sale, List<PurchasePerksPanel.PerkType> perks = null, PurchasePerksCycler cycler = null)
	{
		base.setOption(dataPackage, sale, perks, cycler);

		if (isPackageValid)
		{
			setLabels(bonusPercentage, creditPackage.totalCredits(bonusPercentage));
		}
		
		SafeSet.gameObjectActive(linkedVIPLogo, LinkedVipProgram.instance.shouldSurfaceBranding);
	}

	protected override void logPurchase()
	{
		if (sale.saleType == SaleType.POPCORN)
		{
			StatsManager.Instance.LogCount("dialog", sale.kingdom, creditPackage.keyName, sale.saleName, "buy", "click");
		}
		else
		{
			base.logPurchase();
		}



	}
}