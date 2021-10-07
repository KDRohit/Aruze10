using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public enum SaleType
{
	POPCORN,
	HAPPY_HOUR,
	VIP,
	PAYER_REACTIVATION,
	NONE // This should never be used.
}

public class STUDSale
{
	public SaleType saleType;
	public string dialogTypeKey = "";
	public PurchaseFeatureData featureData;
	public bool isOnCooldown = false;
	public string kingdom = "";
	public string saleName = "";
	
	private string customDataKey = "";
	private static Dictionary<SaleType, STUDSale> sales;
	
	// Ordered List of the SaleTypes. (Used for the old MOTD system with sorting the top sales)
	private static SaleType[] prioritizedTypeList
	{ 
		get
		{
			return new SaleType[] {
				SaleType.PAYER_REACTIVATION,
				SaleType.VIP,
				SaleType.POPCORN,
				SaleType.HAPPY_HOUR
			};
		}
	}
	
	// Returns whether or not this sale should show a dialog.
	public bool isActive
	{
		get
		{
			bool hasValidPackages = false; //Turn of the sale if we find a broken package
			if (featureData != null)
			{
				hasValidPackages = true;
				for (int i = 0; i < featureData.creditPackages.Count; i++)
				{
					if (featureData.creditPackages[i] == null)
					{
						hasValidPackages = false;
						Debug.LogErrorFormat("Not activating sale {0} because is has a null credit package", saleType.ToString());
						break;
					}
					else if (featureData.creditPackages[i].purchasePackage == null)
					{
						hasValidPackages = false;
						Debug.LogErrorFormat("Not activating sale {0} because a credit package has a null purchase package", saleType.ToString());
						break;
					}
					else if (featureData.creditPackages[i].purchasePackage.newZItem == null)
					{
						hasValidPackages = false;
						Debug.LogErrorFormat("Not activating sale {0} because purchase package {1} has a null ZItem", saleType.ToString(), featureData.creditPackages[i].purchasePackage.keyName);
						break;
					}
				}
			}
			return
				featureData != null &&
				featureData.timerRange.isActive &&
				hasValidPackages;
		}
	}

	public void markShown()
	{
		CustomPlayerData.setValue(customDataKey, true);
	}
	
	// Constructor
	public STUDSale(SaleType type)
	{
		switch (type)
		{
			case SaleType.HAPPY_HOUR:
				dialogTypeKey = "happy_hour_sale";
				featureData = PurchaseFeatureData.HappyHourSale;
				kingdom = "happy_hour";
				isOnCooldown = false;
				saleType = type;
				customDataKey = CustomPlayerData.STUD_SALE_VIEWED_HAPPY_HOUR;
				break;
			case SaleType.POPCORN:
				dialogTypeKey = "popcorn_sale";
				featureData = PurchaseFeatureData.PopcornSale;
				
				if (ExperimentWrapper.PopcornVariantTest.isInExperiment)
				{
				    kingdom = "popcorn_new";
					saleName = ExperimentWrapper.PopcornVariantTest.theme;
				}
				else
				{
					kingdom = "popcorn_refresh";
				}
				
				isOnCooldown = false;
				saleType = type;
				customDataKey = CustomPlayerData.STUD_SALE_VIEWED_POPCORN;
				break;
			case SaleType.PAYER_REACTIVATION:
				dialogTypeKey = "payer_reactivation_sale";
				featureData = PurchaseFeatureData.PayerReactivationSale;
				kingdom = "payer_reactivation";
				customDataKey = CustomPlayerData.STUD_SALE_VIEWED_PAYER_REACTIVATION;
				isOnCooldown = CustomPlayerData.getBool(customDataKey, false);
				saleType = type;
				break;
			case SaleType.VIP:
				dialogTypeKey = "vip_sale";
				featureData = PurchaseFeatureData.VipSale;
				kingdom = "vip_sale";
				isOnCooldown = false;
				saleType = type;
				customDataKey = CustomPlayerData.STUD_SALE_VIEWED_VIP_SALE;
				break;
			default:
				featureData = null;
				dialogTypeKey = "";
				kingdom = "";
				isOnCooldown = false;
				saleType = type;
				break;
		}
		
		if (featureData != null)
		{
			if (kingdom != "popcorn_new")
			{
				// If we are in the popcorn_new variant test, then we already have the saleName from Eos above.
				string [] tokens = featureData.imageFolderPath.Split('/');
				if (tokens.Length > 0)
				{
					saleName = tokens[tokens.Length -1];
				}
			}

		}
		sales.Add(type, this);
	}

	// Create all the STUDSales based on the STUD data.
	// Thus this should be called AFTER all the STUDActions have been processed.
	public static void populateAll()
	{
		sales = new Dictionary<SaleType, STUDSale>();
		foreach (SaleType type in prioritizedTypeList)
		{
			new STUDSale(type);
		}
	}
	
	public static bool isSaleActive(SaleType type)
	{
		STUDSale sale = getSale(type);
		return (sale != null && sale.isActive);
	}
	
	// Returns the STUDSale for a given type.
	// This needs to be null-checked by whatever uses it.
	public static STUDSale getSale(SaleType type)
	{
		if (sales != null && sales.ContainsKey(type))
		{
			return sales[type];
		}
		else
		{
			return null;
		}
	}

	// Returns the STUDSale for a given type, but checks whether the sale
	// is active before returning in.
	public static STUDSale getActiveSale(SaleType type)
	{
		if (sales != null &&
			sales.ContainsKey(type) &&
			sales[type] != null &&
			sales[type].isActive)
		{
			return sales[type];
		}
		else
		{
			return null;
		}		
	}

	// Returns the STUDSale for the given action string.
	public static STUDSale getSaleByAction(string action)
	{
		if (string.IsNullOrEmpty(action))
		{
			Debug.LogError("Trying to get a STUDSale with a null/empty action string");
			return null;
		}
		SaleType type = SaleType.NONE;
		switch (action)
		{
			case "happy_hour_sale":
				type = SaleType.HAPPY_HOUR;
				break;
			case "payer_reactivation_sale":
				type = SaleType.PAYER_REACTIVATION;
				break;
			case "popcorn_sale":
				type = SaleType.POPCORN;
				break;
			case "vip_sale":
				type = SaleType.VIP;
				break;
		}
		return getSale(type);
	}
		
	// Returns the Top available STUD Sale
	public static STUDSale getTopSale()
	{
		if (prioritizedTypeList != null)
		{
			for (int i = 0; i < prioritizedTypeList.Length; i++) // Iterating to maintain ordering.
			{
				STUDSale sale = getSale(prioritizedTypeList[i]);
				if (sale != null && sale.isActive && !sale.isOnCooldown)
				{
					return sale;
				}
			}
		}
		return null;
	}

}