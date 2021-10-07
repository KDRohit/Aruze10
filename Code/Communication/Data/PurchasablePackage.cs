using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Com.HitItRich.Feature.VirtualPets;
using Com.Scheduler;
/**
PurchasablePackage

Contains a record of the purchasable packages.
This class supersedes both PurchaseableItems and PurchasableCreditsRaw.
*/
using Zynga.Zdk;
using Zynga.Payments.IAP;

public class PurchasablePackage : IResetGame
{
	// A dictionary of all purchasable items available, from global data.
	public static Dictionary<string, PurchasablePackage> all = new Dictionary<string, PurchasablePackage>();
	private static Dictionary<int, PurchasablePackage> allById = new Dictionary<int, PurchasablePackage>();

	public int id;						// Session-specific index number of this credit package.

	// Data Items:
	public string keyName;				// Key Name for this item. e.g. "mobile_ST0001"
	public int priceTier;				// The current price tier - approximately in USaD - for this item.
	public long creditsRewarded;		// The number of credits received by purchasing this item.
	public string passType;				// Pass type (for rich package only)
	public bool isActive;				// If this item is active and purchasable.

	// Generated data:
	public string priceLocalized;		// Exact price, in localized currency, as returned by the payment system.

	public string currencyCode;			// Currency code of the local device currency, as returned by the payment system.
	public string unlockableItemKey;	// Oddly-named property to hold the game key if this package is for unlocking a premium game.
	public PurchasableItem newZItem;

	private static bool initialized = false;
	private static bool CheckInitialized()
	{
		if (!initialized)
		{
			Debug.LogError("Attempting to read PurchasablePackage item before initialization.");
			return false;
		}

		return true;
	}

	private static System.Text.StringBuilder logBuilder;

	/**
	 * populateAll - creates a dictionary of purchasableItems from static data from the server.
	 * @param	items
	 */
	public static void populateAll(JSON[] items)
	{
		logBuilder = new System.Text.StringBuilder();
		logBuilder.Append("[log_name=purchasablePackageLog]");
		logBuilder.Append("[packages={");
		foreach (JSON item in items)
		{
			logBuilder.Append("{");
			new PurchasablePackage(item);
			logBuilder.Append("},");
		}
		logBuilder.Append("}]");
		initialized = true;

		if (Glb.serverLogPurchasablePackages)
		{
			Debug.Log("purchasable package log: " + logBuilder.ToString());
			Server.sendLogInfo("Economy_PopulatePackages", logBuilder.ToString());
		}

	}

	public PurchasablePackage(JSON item)
	{
		// Not error checking the de-referencing of this key data.
		// We are expecting the server has populated them.
		// This will null-reference on load if any properties were missing.
		id = item.getInt("id", -1);

		// Populate our set data about this item:
		keyName = item.getString("key_name", "");
		priceTier = item.getInt("dollar_cost", 0);
		creditsRewarded = item.getInt("package_coins", 0);
		passType = item.getString("pass_type", "");
		isActive = item.getBool("is_active", true);

//		vipPoints = item.getInt("vip_points", 0);
//		unlockableItemKey = item.getString("item_key_name", "");

		// PurchaseableItem must have a ZyngaEconomyItem for it to be available for sale:
#if UNITY_WSA_10_0 && NETFX_CORE
		newZItem = WindowEconomyManager.getZyngaEconomyItemByPackageName(keyName);
#else
		newZItem = NewEconomyManager.getZyngaEconomyItemByPackageName (keyName);
#endif

		priceLocalized = "N/A";
		currencyCode = "N/A";
		float cost;
		if (newZItem != null)
		{
			priceLocalized = newZItem.DisplayPrice;
			currencyCode = newZItem.Currency;

			if (Data.debugEconomy)
			{
				Debug.Log ("PurchaseablePackage - Added package: " + keyName + " SKU: " + newZItem.GameSkuId.ToString ());
			}
		}
		else
		{
			cost = ((float)priceTier) - 0.01f;
			if (cost > 0)
			{
				priceLocalized = string.Format("${0}", cost.ToString("n2"));
			}
			if (Data.debugEconomy)
			{
				Debug.LogWarning("PurchaseablePackage - Failed to find store item for: " + keyName);
			}
		}

		// Add to our index list:
		if (!all.ContainsKey(keyName))
		{
			all.Add(keyName, this);
		}
		else
		{
			all[keyName] = this;
		}

		if (!allById.ContainsKey(id))
		{
			allById.Add(id, this);
		}
		else
		{
			allById[id] = this;
		}
		logBuilder.Append("[package_name=" + keyName + "]");
		logBuilder.Append("[price_tier=" + priceTier.ToString() + "]");
		logBuilder.Append("[price_localized=" + priceLocalized + "]");
		logBuilder.Append("[currency_code=" + currencyCode + "]");
	}

	public void makeWheelDealPurchase(int bonusPercent)
	{
		makePurchase(bonusPercent, false, -1, "PopcornSalePackage");
	}

	// Wrapper around the original makePurchase call to make sure that when the user is in the network, we are not falsely advertising.
	public void makePurchase
	(
		int bonusPercent = 0,
		bool isMultiplierPurchase = false,
		int buyCreditsPagePackageIndex = -1,
		string packageClass = "",
		int saleBonusPercent = 0,
		string buffKey = "",
		string economyTrackingNameOverride = null,
		string economyTrackingVariantOverride = null,
		bool isLifecycleSale = false,
		string collectablePack = "",
		PurchaseFeatureData.Type purchaseType = PurchaseFeatureData.Type.NONE,
		int economyVersion = -1,
		int streakSalePackageIndex = -1,
		long seedValue = -1,
		string themeName = "",
		string lottoBlastKey = "",
		string bundleSaleId = "",
		RewardPurchaseOffer purchaseRewardable = null
	)
	{
		RoutineRunner.instance.StartCoroutine(makePurchaseCoroutine(
			bonusPercent,
			isMultiplierPurchase,
			buyCreditsPagePackageIndex,
			packageClass,
			saleBonusPercent,
			buffKey,
			economyTrackingNameOverride,
			economyTrackingVariantOverride,
			isLifecycleSale,
			collectablePack,
			purchaseType,
			economyVersion,
			streakSalePackageIndex,
			seedValue,
			themeName,
			lottoBlastKey,
			bundleSaleId,
			purchaseRewardable
		));
	}

	private IEnumerator makePurchaseCoroutine
	(
		int bonusPercent,
		bool isMultiplierPurchase,
		int buyCreditsPagePackageIndex,
		string packageClass,
		int saleBonusPercent,
		string buffKey,
		string economyTrackingNameOverride,
		string economyTrackingVariantOverride,
		bool isLifecycleSale = false,
		string collectablePack = "",
		PurchaseFeatureData.Type purchaseType = PurchaseFeatureData.Type.NONE,
		int economyVersion = -1,
		int streakSalePackageIndex = -1,
		long seedValue = -1,
		string themeName = "",
		string lottoBlastKey = "",
		string bundleSaleId = "",
		RewardPurchaseOffer purchaseRewardable = null
	)
	{
		string petTreatKeyName = "";
		string transactionName = "purchase-package-" + keyName;
		Userflows.flowStart(transactionName);

		if (LinkedVipProgram.instance.isEligible && LinkedVipProgram.instance.isConnected)
		{
			// If the client is connected to the linked vip network, then we want to get the sttaus before buying.
			NetworkAction.getVipStatus();
			// Wait for the network status request to finish.
			yield return RoutineRunner.instance.StartCoroutine(ServerAction.waitForActionBatch());

			// If the network level that came down, and the vip level of the player don't match.
			if (SlotsPlayer.instance.vipNewLevel < LinkedVipProgram.instance.previousNetworkLevel)
			{
				Debug.LogWarningFormat(
					"PurchasablePackage.cs -- makePurchase -- network_level doesn't match the client vip level, reloading dialog");
				// Someone on the network disconnected while the dialog is open, and their vip level changed.
				// Tell the user that this happened and close/reopen the dialog.
				DialogBase openDialog = Dialog.instance.currentDialog;
				GenericDialog.showDialog(
					Dict.create(
						D.TITLE, Localize.text("actions_error_title"),
						D.MESSAGE, Localize.text("vip_level_changed_offers"),
						D.OPTION, "ok",
						D.REASON, "purchasable-package-vip-level-change-error",
						D.CALLBACK, new DialogBase.AnswerDelegate((args) => { openDialog.init(); }
						)
					),
					SchedulerPriority.PriorityType.IMMEDIATE
				);
				// Don't continue here. Make the player touch the updated purchase button again.
				Userflows.flowEnd(transactionName, false, "package-vip-update");
				yield break;
			}
		}

		VirtualPetTreat petTreat = VirtualPetsFeature.instance != null && VirtualPetsFeature.instance.isEnabled
			? VirtualPetsFeature.instance.getTreatTypeForPackage(this)
			: null;
		if (petTreat != null)
		{
			petTreatKeyName = petTreat.keyName;
		}
		// Ensure payments are enabled for this player before proceeding:
		if (!SlotsPlayer.instance.allowedPayments)
		{
			showPurchaseNotAllowedDialog();
			Userflows.flowEnd(transactionName, false, "purchases-blocked");
			yield break;
		}

		if (newZItem != null)
		{
			Userflows.logStep("item-exists", transactionName);
#if UNITY_WSA_10_0 && NETFX_CORE
			WindowEconomyManager.startPurchase(newZItem, transactionName, bonusPercent, "", isMultiplierPurchase, buyCreditsPagePackageIndex, packageClass, saleBonusPercent);
#else
			NewEconomyManager.startPurchase (
				newZItem, 
				transactionName, 
				bonusPercent, 
				"", 
				isMultiplierPurchase, 
				buyCreditsPagePackageIndex, 
				packageClass, 
				saleBonusPercent, 
				economyTrackingNameOverride, 
				economyTrackingVariantOverride, 
				isLifecycleSale, 
				collectablePack, 
				purchaseType,
				economyVersion,
				streakSalePackageIndex,
				buffKey,
				seedValue,
				themeName,
				lottoBlastKey,
				petTreatKeyName,
				bundleSaleId,
				purchaseRewardable);
#endif
		}
		else
		{
#if UNITY_WSA_10_0 && NETFX_CORE
			Debug.LogErrorFormat("PurchaseablePackage Error - Cannot purchase, credits package not found: {0}", keyName );
			Userflows.flowEnd(transactionName, false, "missing-package");

			GenericDialog.showDialog(
				Dict.create(
					D.TITLE, Localize.text("error"),
					D.MESSAGE,  Localize.text("error_failed_to_send_actions"),
					D.REASON, "purchaseable-package-missing-package"
				),
				true
			);
#else
			Debug.LogWarning("PurchasablePackage.cs -- makePurchaseCoroutine -- newZItem was null at time of purchase, attempting to get it from catalog now");
			Userflows.logStep("populating-inventory", transactionName);
			NewEconomyManager.populateAllIAP();
			newZItem = NewEconomyManager.getZyngaEconomyItemByPackageName (keyName);

			if (newZItem != null)
			{
				Userflows.logStep("item-exists", transactionName);
				NewEconomyManager.startPurchase (
					newZItem, 
					transactionName, 
					bonusPercent, 
					"", 
					isMultiplierPurchase, 
					buyCreditsPagePackageIndex, 
					packageClass, 
					saleBonusPercent, 
					economyTrackingNameOverride, 
					economyTrackingVariantOverride, 
					isLifecycleSale, 
					collectablePack, 
					purchaseType, 
					economyVersion,
					streakSalePackageIndex,
					buffKey,
					seedValue,
					themeName,
					lottoBlastKey,
					petTreatKeyName,
					bundleSaleId,
					purchaseRewardable
					);
			}
			else
			{
				Debug.LogErrorFormat("PurchaseablePackage Error - Cannot purchase, package not found: {0}", keyName );
				Userflows.flowEnd(transactionName, false, "missing-package");

				GenericDialog.showDialog(
					Dict.create(
						D.TITLE, Localize.text("error"),
						D.MESSAGE,  Localize.text("error_failed_to_send_actions"),
						D.REASON, "purchaseable-package-missing-package"
					),
					SchedulerPriority.PriorityType.IMMEDIATE
				);
			}
#endif
		}
	}

	private void vipStatusCheckCallback(JSON response)
	{

	}

	// Return the base coins that we'd get from purchasing this item:
	public long creditsBase(bool isBuyPage = false)
	{
		if (isBuyPage)
		{
			return creditsRewarded * PurchaseFeatureData.findBuyCreditsMultiplier();
		}
		return creditsRewarded;
	}

	// Calculates how many bonus coins we'll get from just the current VIP level.
	// Basically a convenience function for readability.
	public long bonusVIPCredits()
	{
		return bonusCredits(0);
	}

	
	// Calculates how many bonus coins we'll get for a specific level
	public long getVIPCreditsForLevel(int vipLevel)
	{
		VIPLevel levelToShow = VIPLevel.find(vipLevel);
		Decimal vipPercent = levelToShow.purchaseBonusPct / 100m;
		Decimal totalBonusRounded = Decimal.Round(creditsRewarded * vipPercent, MidpointRounding.AwayFromZero);
		long totalBonus = Decimal.ToInt64(totalBonusRounded);
		return totalBonus;
	}

	// Calculate how many bonus coins we'll get - applying the current VIP level to get the bonus percentage:
	public long bonusCredits(int bonusPercent, bool isBuyPage = false, int bonusSalePercent = 0)
	{
		if (SlotsPlayer.instance == null)
		{
			return 0;
		}

		VIPLevel myLevel = VIPLevel.find(SlotsPlayer.instance.vipNewLevel, "coin_purchases");

		if (myLevel == null)
		{
			return 0;
		}

		Decimal vipPercent = myLevel.purchaseBonusPct / 100m;
		Decimal salePercent = bonusSalePercent / 100m; // Convert the sale integer to a decimal.
		Decimal generalPercent = bonusPercent / 100m;
		Decimal inflationPercent = (Decimal)SlotsPlayer.instance.currentBuyPageInflationFactor;
		Decimal totalPercent =  ((1 + vipPercent) * (1 + salePercent) * (1 + generalPercent) * (inflationPercent) - 1);
		Decimal totalBonusRounded = Decimal.Round(creditsRewarded * totalPercent, MidpointRounding.AwayFromZero);
		long totalBonus = Decimal.ToInt64(totalBonusRounded);
		return totalBonus;
	}

	// Return the total coins that we're going to get from purchasing this item:
	public long totalCredits(int bonusPercent = 0, bool isBuyPage = false, int bonusSalePercent = 0)
	{
		return (creditsBase(isBuyPage) + bonusCredits(bonusPercent, isBuyPage, bonusSalePercent));
	}

	// Return how many VIP points this purchase will give the player.
	// For every dollar spent, we award 100 VIP points:
	const long VIP_POINTS_PER_DOLLAR = 100;
	public long vipPoints()
	{
		return priceTier * VIP_POINTS_PER_DOLLAR;
	}

	public string getLocalizedPrice()
	{
		return priceLocalized;
	}

	// returns rounded price with currency symbol in front and no decimal $9.99 becomes $10 for USD currency
	public string getRoundedPrice()
	{
		// tbd do for countries we care about, many can probably use roundSingleSymbolPrefix
		if (currencyCode == "USD")
		{
			return roundSingleSymbolPrefix();
		}

		return getLocalizedPrice();
	}

	// removes the 1 character currency symbol symbol in front and rounds the rest to a rounded up int
	// NOTE only works with strings that have comma's and decimal points in USD currency format
	private string roundSingleSymbolPrefix()
	{
		string origPrice = getLocalizedPrice();

		if (origPrice.Length > 1)
		{
			string frontSymbol = origPrice.Substring(0,1);
			string numString = origPrice.Substring(1,origPrice.Length-1);

			try
			{
				string roundedPrice = frontSymbol + CommonText.formatNumber(Math.Round(System.Double.Parse(numString), MidpointRounding.AwayFromZero));
				return (roundedPrice);
			}
			catch (System.Exception e)
			{
				Debug.LogErrorFormat("unable to parse price for rounding {0} exception {1}", numString, e.ToString());
			}
		}

		return (origPrice);
	}

	// On Android, only localized prices are available for the actual purchase cost.
	// This is a problem if we want to find the original cost of an item, because that item doesn't have a localized price.
	// So what we do is search for *another* item in the game that has the desired sale price - then return the (localized) price of that item.
	public string getOriginalLocalizedPrice(int bonusPercent)
	{
/*		if ((originalPriceTier == currentPriceTier) || (originalPriceTier == 0))
		{
			return priceLocalized;
		}

		// We now need the original price for this item.
		// If we have access to an item that has that original price, use it:
		PurchasablePackage itemPriced = PurchasablePackage.findByPriceTier(originalPriceTier);
		if (itemPriced != null)
		{
			return itemPriced.localizedPrice;
		}

		return doublePrice(priceLocalized);

		/* Michael C -- since we double deafult to doubling the price for "was", removing this.
		// We don't have a localized original price - make one:
		float originalCost = (((float)originalPriceTier) / 10f) - .01f;
		if (originalCost > 0)
		{
			return string.Format("US${0}", originalCost.ToString("n2"));
		}

		return "";
		*/
		return modifyPrice(priceLocalized, bonusPercent);
	}

	// We do not have mobile packages for each of the "original" sale prices that we want to show in game
	// becuase of the maximum allowed package size of $99.99, but sometimes we run a sale on something that
	// "was $199.99" and we are putting it on sale. getOriginalLocalizedPrice will work if the double price is
	// under $99, but if it is not, then we need to do some magic to get a number.
	public static string modifyPrice(string price, int bonus)
	{
		if (bonus <= 0)
		{
			return price;
		}

		string workingPrice = price;
		char[] chars = workingPrice.ToCharArray();
		Dictionary<int, char> symbols = new Dictionary<int, char>();

		string prefix = "";
		string suffix = "";

		// Grab the prefix of the price.
		for(int i = 0; i < chars.Length; i++)
		{
			if (Char.IsNumber(chars[i]))
			{
				//Searching for a number value, once we have hit a number, was assume that anything we hit before that is the prefix.
				if (i == 0)
				{
					// In this case, a number is the first character in the string, so there is no prefix.
					break;
				}
				prefix = workingPrice.Substring(0, i);
				workingPrice = workingPrice.Substring(i);
				break;
			}
		}

		// Grab the suffix of the price.
		for(int i = chars.Length - 1; i >= 0; i--)
		{
			//Searching for a number value, once we have hit a number, was assume that anything we hit before that is the suffix.
			if (Char.IsNumber(chars[i]))
			{
				if (i == chars.Length - 1)
				{
					// In this case, a number is the last character in the string, so there is no suffix.
					break;
				}
				suffix = workingPrice.Substring(i + 1);
				workingPrice = workingPrice.Substring(0, i + 1);
				break;
			}
		}

		chars = workingPrice.ToCharArray();
		// We have the characters now, so reset this string to be reused during symbol removal.
		workingPrice = "";

		// Grab the remaining symbols and store their locations.
		for (int i = chars.Length - 1; i >= 0; i--)
		{
			// We want to store symbols by their position from the end so that if the string size changes we can easily adjust.
			int indexFromEnd = chars.Length - 1 - i;
			if (!Char.IsNumber(chars[i]))
			{
				symbols.Add(indexFromEnd, chars[i]);		
			}
			else
			{
				workingPrice = workingPrice.Insert(0, chars[i].ToString());
			}
		}

		int priceInt = Convert.ToInt32(workingPrice);
		float bonusMultiplier = (float)((100f + bonus)/100f);
		priceInt = Mathf.FloorToInt(priceInt * bonusMultiplier);
		string doubledString = priceInt.ToString();


		// Add the symbols back in.
		foreach (KeyValuePair<int, char> pair in symbols)
		{
			// Insert the symbol at the correct position, used the new string length to adjust position if necessary.
			int index = doubledString.Length - pair.Key;
			doubledString = doubledString.Insert(index, pair.Value.ToString());
		}

		return prefix + doubledString + suffix;
	}

	// Accessors for finding a specific purchasable item.

	// Finds a PurchasablePackage by the primary key (long name):
	public static PurchasablePackage find(string id)
	{
		if (!CheckInitialized())
		{
			return null;
		}

		if (all.ContainsKey(id))
		{
			return all[id];
		}
		return null;
	}

	// Finds a PurchasablePackage by the internal ID:
	public static PurchasablePackage findByID(int id)
	{
		if (!CheckInitialized())
		{
			return null;
		}

		if (allById.ContainsKey(id))
		{
			return allById[id];
		}
		return null;
	}

	public static PurchasablePackage findByPriceTier(int priceTier, bool shouldFindClosest = false)
	{
		if (!CheckInitialized())
		{
			return null;
		}

		foreach (PurchasablePackage item in all.Values)
		{
			if ((item.newZItem != null) && (item.priceTier == priceTier)) {
				return item;
			}
		}

		if (shouldFindClosest)
		{
			// If we haven't found one to return, lets go off and find the closest one.
			PurchasablePackage closePackage = null;
			if (closePackage == null)
			{
				// If we didn't find one at that price tier, then search for one that is greater than that price point.
				foreach (PurchasablePackage p in all.Values)
				{
					if (p.priceTier >= priceTier &&
						(closePackage == null || (closePackage.priceTier > p.priceTier)))
					{
						closePackage = p;
					}
				}
			}
			return closePackage;
		}
		return null;
	}

	// The ids sent down from server do not match the item ids
	// So this tries to look up the correct item for tracking credited purchases
	public static float findUSDByAmount(int packageCoins)
	{
		if (!CheckInitialized())
		{
			return 0.0f;
		}

		foreach (PurchasablePackage item in all.Values)
		{
			if ((item.newZItem != null) && (item.creditsRewarded == packageCoins)) {
				return (float)item.newZItem.PriceAmount;
			}
		}
		return 0.0f;
	}

	// Used for Testing as server actions will not fire in Editor but fake Purchases will
	public static PurchasablePackage getNewBySku(int sku)
	{
		if (!CheckInitialized())
		{
			return null;
		}

		foreach (PurchasablePackage item in all.Values)
		{
			if (item.newZItem != null) {
				if (item.newZItem.GameSkuId == sku) {
					return item;
				}
			}
		}
		Debug.LogError("Could not find reference to PurchasablePackage item: " + sku); // This is bad, probably a data misconfiguration.
		return null;
	}

	public static void showPurchaseNotAllowedDialog()
	{
		Debug.LogError("Player is prohibited from purchasing.");
		GenericDialog.showDialog(
			Dict.create(
				D.TITLE, Localize.toTitle(Localize.textOr("access_denied", "Access Denied")),
				D.MESSAGE, Localize.textOr("access_denied_purchase_cs","This account is currently blocked from making purchases. Please contact customer support.")
					+ "\n\nZID " + SlotsPlayer.instance.socialMember.zId,
				D.OPTION1, Localize.textOr("help_support_button", "Support"),
				D.OPTION2, Localize.textOr("ok", "Ok"),
				D.REASON, "purchaseable-package-player-prohibited",
				D.CALLBACK, new DialogBase.AnswerDelegate( (args) => {
					if ((string)args[D.ANSWER] == "1")
					{
						Common.openSupportUrl(Glb.HELP_LINK_SUPPORT);
					}
				} )
			),
			SchedulerPriority.PriorityType.BLOCKING
		);
	}

	public static string[] getAllPackageKeyNames()
	{
		string[] keyNames = new string[all.Count];
		int index = 0;
		foreach (KeyValuePair<string, PurchasablePackage> kvp in all)
		{
			keyNames[index] = kvp.Key;
			index++;
		}
		return keyNames;
	}

	public static bool isValidPackage(string key)
	{
		return all != null && all.ContainsKey(key);
	}
	
	//Generic function for figuring out how much any package amount could payout with various different bonus percents 
	public static long getTotalPackageAmount(long baseAmount, int bonusPercentInt = 0, int salePercentInt = 0, int vipPercentInt = 0)
	{
		Decimal vipPercent = vipPercentInt / 100m;
		Decimal salePercent = salePercentInt / 100m;
		Decimal generalPercent = bonusPercentInt / 100m;
		Decimal inflationPercent = (Decimal)SlotsPlayer.instance.currentBuyPageInflationFactor;
		Decimal totalPercent =  ((1 + vipPercent) * (1 + salePercent) * (1 + generalPercent) * (inflationPercent) - 1);
		Decimal totalBonusRounded = Decimal.Round(baseAmount * totalPercent, MidpointRounding.AwayFromZero);
		long totalBonus = Decimal.ToInt64(totalBonusRounded);
		return totalBonus + baseAmount;
	}

	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		all = new Dictionary<string, PurchasablePackage>();
		allById = new Dictionary<int, PurchasablePackage>();
	}
}
