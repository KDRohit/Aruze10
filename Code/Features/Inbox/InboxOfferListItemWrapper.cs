using UnityEngine;
using System.Collections;

public class InboxOfferListItemWrapper : InboxListItemWrapper
{
	/// <inheritdoc/>
	public override void setup(InboxListItem listItem, InboxItem inboxItem)
	{
		base.setup(listItem, inboxItem);

		GiftChestOffer.instance.views++;
		GiftChestOffer.instance.refreshOfferData();

		setButton();

		string nameToUse = SlotsPlayer.instance.socialMember.firstName.ToLower() != "blank" ? SlotsPlayer.instance.socialMember.firstName : "Spinner";
		listItem.setMessageLabel(Localize.text("inbox_offer_description", nameToUse,  CommonText.formatNumber(GiftChestOffer.instance.grandTotal), GiftChestOffer.instance.purchasePackage.priceLocalized));

		GiftChestOffer.instance.onPurchaseSuccessEvent += onOfferPurchaseSuccessEvent;
	}

	private void onOfferPurchaseSuccessEvent()
	{
		InboxIncentiveAction.closeOffer();
		GiftChestOffer.instance.onPurchaseSuccessEvent -= onOfferPurchaseSuccessEvent;
	}

	/// <inheritdoc/>
	public override void action()
	{
		base.action();

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

		listItem.setButtonAnimation(InboxListItem.ItemAnimations.Outro);
	}

	/// <inheritdoc/>
	public override void dismiss()
	{
		base.dismiss();
		GiftChestOffer.instance.views = GiftChestOffer.instance.maxViews;
	}

	/// <inheritdoc/>
	protected override void setButton()
	{
		listItem.setButtonText(Localize.text("buy"));
	}
}