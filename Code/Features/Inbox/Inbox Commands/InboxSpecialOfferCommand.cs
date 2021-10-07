using UnityEngine;
using System.Collections;

public class InboxSpecialOfferCommand : InboxCommand
{
	public const string SPECIAL_OFFER = "collect_special_offer";

	public override void init(string action, string args)
	{
		base.init(action, args);
		GiftChestOffer.instance.onPurchaseSuccessEvent += onOfferPurchaseSuccessEvent;
	}

	private void onOfferPurchaseSuccessEvent()
	{
		InboxIncentiveAction.closeOffer();
		GiftChestOffer.instance.onPurchaseSuccessEvent -= onOfferPurchaseSuccessEvent;
	}

	/// <inheritdoc/>
	public override void execute(InboxItem inboxItem)
	{
		StatsManager.Instance.LogCount(
			counterName: "gift_chest",
			kingdom: "special_offer",
			phylum: GiftChestOffer.instance.purchasePackage.priceLocalized,
			family: "buy",
			genus: "click");

		int powerupBonus = PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_BUY_PAGE_KEY) ? BuyPageBonusPowerup.salePercent : 0;

		if (GiftChestOffer.instance.purchasePackage != null)
		{
			GiftChestOffer.instance.purchasePackage.makePurchase(
				bonusPercent: GiftChestOffer.instance.bonusPct + powerupBonus,
				packageClass: "GiftChestOffer");
		}
		else
		{
			Bugsnag.LeaveBreadcrumb("GiftChestDialgoOfferItemPanel.cs -- clickButton() -- failed to find a purchasable package, this is wierd.");
		}
	}

	/// <inheritdoc/>
	public override bool canExecute
	{
		get { return GiftChestOffer.instance != null && GiftChestOffer.instance.purchasePackage != null; }
	}

	/// <inheritdoc/>
	public override string actionName
	{
		get { return SPECIAL_OFFER; }
	}
}