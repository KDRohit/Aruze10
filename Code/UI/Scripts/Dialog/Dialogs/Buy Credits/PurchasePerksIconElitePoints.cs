using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PurchasePerksIconElitePoints : PurchasePerksIcon
{
	[SerializeField] private LabelWrapperComponent elitePointsLabel;
	[SerializeField] private LabelWrapperComponent elitePointsShadowLabel;
	
	private const string TEXT_LOCALIZATION = "elite_purchase_perk";
	public override void init(CreditPackage package, int index, bool isPurchased, RewardPurchaseOffer offer)
	{
		int points = package.purchasePackage.priceTier * EliteManager.elitePointsPerDollar;
		if (elitePointsLabel != null && elitePointsShadowLabel != null)
		{
			elitePointsLabel.text = Localize.text(TEXT_LOCALIZATION, CommonText.formatNumber(points));
			iconDimmer.cacheTextColors(elitePointsLabel.tmProLabel);
			iconDimmer.cacheTextColors(elitePointsShadowLabel.tmProLabel);
		}
	}
}
