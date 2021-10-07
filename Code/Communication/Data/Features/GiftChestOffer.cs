using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class GiftChestOffer : FeatureBase
{
	public static GiftChestOffer instance
	{
		get
		{
			return FeatureDirector.createOrGetFeature<GiftChestOffer>("gift_chest_offer");
		}
	}

	public delegate void onPurchaseCallback();
	public event onPurchaseCallback onPurchaseSuccessEvent;
	public event onPurchaseCallback onPurchaseFailedEvent;

	public GameObject tooltipPrefab;
	public GameTimerRange cooldownTimer;
	// Just stops multiple purchases in the same session. If a user logs in again, we should re-get all relevant info.
	public bool purchased = false;

	public int views = 0; // We top out at some number of views.
	public int maxViews = 0;
	public int bonusPct = 0; // Whole number, like 10 or 100
	public string packageString = ""; // string to lookup our special deal
	public int bonusValue = 0;
	public long grandTotal = 0;
	public PurchasablePackage purchasePackage;

	private int cooldownTime = 0;
	public const string GIFT_CHEST_OFFER_TOOLTIP = "Features/Gift Chest Offer/Prefabs/Gift Chest Offer Tooltip";

	private void loadGiftChestButtonSuccess(string assetPath, Object obj, Dict data = null)
	{
		tooltipPrefab = obj as GameObject;
	}

	private void loadGiftChestButtonFail(string assetPath, Dict data = null)
	{
		Debug.LogError("GiftChestOffer::loabInboxButtonFail - Failed to download the inbox button");
	}

	public void refreshOfferData()
	{
		if (purchasePackage != null)
		{
			grandTotal = purchasePackage.totalCredits(bonusPct) * CreditsEconomy.economyMultiplier;
		}
	}

	//    gift_chest_offer: {
	//        coin_package: coin_package_10,
	//        bonus_pct: 1000,
	//        num_views: 3,
	//        max_views: 5
	//    }
	public void setupGiftChestOfferData(JSON data = null)
	{
		if (data != null)
		{
			views = data.getInt("num_views", 0);
			bonusPct = ExperimentWrapper.GiftChestOffer.bonusPercent;
			packageString = ExperimentWrapper.GiftChestOffer.coinPackage;
			maxViews = ExperimentWrapper.GiftChestOffer.maxViews;
			cooldownTime = ExperimentWrapper.GiftChestOffer.cooldown;
			purchasePackage = PurchasablePackage.find(packageString);
			refreshOfferData();

			AssetBundleManager.load(GIFT_CHEST_OFFER_TOOLTIP, loadGiftChestButtonSuccess, loadGiftChestButtonFail);
		}
	}

	public void onPurchaseSuccess()
	{
		// Hey we got there.
		views = maxViews;
		InboxIncentiveAction.closeOffer();

		if (onPurchaseSuccessEvent != null)
		{
			onPurchaseSuccessEvent();
		}
	}

	public void onPurchaseFailed()
	{
		purchased = false;
		if (onPurchaseFailedEvent != null)
		{
			onPurchaseFailedEvent();
		}
	}

	public void startCooldown()
	{
		if (cooldownTimer != null)
		{
			cooldownTimer.clearEvent();
		}

		cooldownTimer = GameTimerRange.createWithTimeRemaining(cooldownTime);
	}

	public bool canSurface
	{
		get
		{
			return views < maxViews &&
			       ExperimentWrapper.GiftChestOffer.isInExperiment &&
			       InboxInventory.findItemByCommand<InboxSpecialOfferCommand>() != null &&
			       !string.IsNullOrEmpty(packageString);
		}
	}
	
	public bool canShowTooltip
	{
		get 
		{
			return canSurface && (cooldownTimer == null || cooldownTimer.isExpired) && tooltipPrefab != null; 
		}
	}

	#region feature_base_overrides
	protected override void initializeWithData(JSON data)
	{
		// Init at game load.
		setupGiftChestOfferData(data.getJSON("gift_chest_offer"));
	}

	public override bool isEnabled
	{
		get
		{
			return base.isEnabled &&
				views < maxViews &&
				ExperimentWrapper.GiftChestOffer.isInExperiment &&
				!string.IsNullOrEmpty(packageString);
		}
	}

	public override void drawGuts()
	{
		GUILayout.BeginVertical();
		StringBuilder text = new StringBuilder();
		text.AppendLine("purchased: " + purchased.ToString());
		text.AppendLine("views: " + views.ToString());
		text.AppendLine("maxViews: " + maxViews.ToString());
		text.AppendLine("bonusPct: " + bonusPct.ToString());
		text.AppendLine("packageString: " + packageString.ToString());
		text.AppendLine("bonusValue: " + bonusValue.ToString());
		text.AppendLine("grandTotal: " + grandTotal.ToString());
		text.AppendLine("purchasePackage: " + (purchasePackage == null ? "Null" : purchasePackage.ToString()));
		text.AppendLine("cooldownTime: " + cooldownTime.ToString());
		GUILayout.TextArea(text.ToString());
		GUILayout.EndVertical();
	}
	#endregion
}
