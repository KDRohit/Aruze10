using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;


public class STUDSaleDialogHIR : STUDSaleDialog
{
	public TextMeshPro moreInfoText;

	public override void init()
	{
		base.init();

		if (ExperimentWrapper.BuyPageHyperlink.isInExperiment && ExperimentWrapper.PopcornSale.hasCardPackDropsConfigured)
		{
			moreInfoText.text = ExperimentWrapper.BuyPageHyperlink.getLinkForPopcornSale();
		}
	}

	protected override void setPackages(List<CreditPackage> packages)
	{
		base.setPackages(packages);	// Also sets creditPackagesLogString, but we need it empty for this override.
		creditPackagesLogString = "";
	}
	
	protected override void rejectButtonClicked()
	{
		if (sale.saleType == SaleType.POPCORN)
		{
			// If we are in a popcorn sale, then we want special new stats.
		    logCloseStats(sale.saleName, "click");
		}
		else
		{
			logCloseStats("", "no");
		}

		base.rejectButtonClicked();
	}
}