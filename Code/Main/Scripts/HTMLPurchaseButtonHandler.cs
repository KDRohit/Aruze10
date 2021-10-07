using UnityEngine;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/*
This script is attached to our HIR "Persistent Object" and receives HTML-based purchase messages, 
to support HTML-based purchase buttons in our HIR WebGL client. Does no harm to other platforms.
*/

public class HTMLPurchaseButtonHandler : TICoroutineMonoBehaviour
{
	// "purchasePopcornSalePackage" is referenced via reflection by the Unity messaging system
	public void purchasePopcornSalePackage(string jsonString)
	{
		// Abort if things are too early...
		if (!NewEconomyManager.Initialized)
		{
			Debug.LogWarning("HTMLPurchaseButtonHandler.purchase: NewEconomyManager not initialized; aborting");
			return;
		}

		// Parse variables from JSON string
		JSON jsonObj = new JSON(jsonString);
		string itemCode = jsonObj.getString("itemCode", "");
		int bonusPercent = jsonObj.getInt("bonusPercent", 0);
		string economyName = jsonObj.getString("economyName", "");
		string variantName = jsonObj.getString("variantName", "");

		Debug.Log("HTMLPurchaseButtonHandler json=" + jsonString);
		Debug.Log("HTMLPurchaseButtonHandler itemCode=" + itemCode + " bonusPercent=" + bonusPercent + " economyName=" + economyName + " variantName=" + variantName);

		// Look for package by key, Returns null if no package or packages not yet initializsed...
		PurchasablePackage package = PurchasablePackage.find(itemCode);
		if (package != null)
		{
			package.makePurchase(bonusPercent, economyTrackingNameOverride: economyName, economyTrackingVariantOverride: variantName, purchaseType: PurchaseFeatureData.Type.ONE_CLICK_BUY); 
		}
		else
		{
			Debug.LogWarning("HTMLPurchaseButtonHandler - Aborting; Couldn't find package keyname: " + itemCode);
		}
	}


	// Simple test function that creates mock data...
	public void testPurchasePopcornSalePackage()
	{
		// Mock data...
		var dict = new Dictionary<string,object>();
		dict["itemCode"] = "coin_package_5";
		dict["bonusPercent"] = 110;
		dict["economyName"] = "one_click_buy";
		dict["variantName"] = "dummy_variant";

		string jsonString = JSON.createJsonString("", dict);
		purchasePopcornSalePackage(jsonString);
	}

}
