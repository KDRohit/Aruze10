using UnityEngine;
using System.Collections;

public class DoSomethingXpromoV2 : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		string xPromoKey = ExperimentWrapper.MobileToMobileXPromo.getArtCampaign();
		StatsManager.Instance.LogCount(counterName:"lobby",
			kingdom: "xpromo",
			phylum: "hir_xpromo_creative_v2",
			klass: xPromoKey,
			genus: "click");
		MobileXpromo.showXpromo(MobileXpromo.SurfacingPoint.NONE);
		
	}
	
	public override bool getIsValidToSurface(string parameter)
	{
		if (!MobileXpromo.isEnabled())
		{
			return false;
		}
		
		int totalCount = CustomPlayerData.getInt(CustomPlayerData.XPROMO_ART_VIEW_COUNT, 0);
		bool isAtMaxViews = totalCount >= ExperimentWrapper.MobileToMobileXPromo.dialogMaxViewToSwap;


		return !isAtMaxViews;
		
	}
}
