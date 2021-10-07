using System;
using System.Collections;
using System.Collections.ObjectModel;
using UnityEngine;

using Zynga.Payments.IAP.Impl;
using Zynga.Payments.IAP;


public class DevGUIMenuIAP : DevGUIMenu
{

	public override void drawGuts()
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label($"initialized: {NewEconomyManager.Initialized}");
		GUILayout.Label($"loaded: {NewEconomyManager.FirstLoad}");
		GUILayout.Label($"purchases enabled: {NewEconomyManager.PurchasesEnabled}");
		GUILayout.Label($"purchase in progress: {NewEconomyManager.isPurchaseInProgress}");
		GUILayout.EndHorizontal();

		InAppPurchaser iap = NewEconomyManager.Instance.InAppPurchase as InAppPurchaser;
		if (iap != null)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label("IAP Purchaser");
			StoreCommon store = iap.Store as StoreCommon;
			GUILayout.Label($"store: {store?.GetType()}");
			GUILayout.EndHorizontal();

			if (store != null)
			{
				ReadOnlyDictionary<string, PurchasableItem> productsByCode = iap.PurchasableItemsByProductCode;
				GUILayout.BeginVertical();
				GUILayout.Label("Products");
				foreach (string productCode in productsByCode.Keys)
				{
					GUILayout.BeginHorizontal();
					GUILayout.Label($"{productCode}");
					if (GUILayout.Button("Purchase"))
					{
						PurchasableItem item = productsByCode[productCode];
						NewEconomyManager.startPurchase(item, "test");
					}
					GUILayout.EndHorizontal();
				}
				GUILayout.EndVertical();
			}
		}
	}
}
