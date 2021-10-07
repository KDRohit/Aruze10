using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
A dev panel.
*/

public class DevGUIMenuSTUDSale : DevGUIMenu
{
	public override void drawGuts()
	{
		GUILayout.BeginVertical();
		GUILayout.BeginHorizontal();
		
		STUDAction popcornSale = STUDAction.findPopcornSale();
		if (popcornSale == null)
		{
			GUILayout.Button("No Popcorn Sale");
		}
		else if (GUILayout.Button("Popcorn Sale"))
		{
			STUDSaleDialog.showDialog(STUDSale.getSale(SaleType.POPCORN));
			DevGUI.isActive = false;
		}

		STUDAction happyHourSale = STUDAction.findHappyHourSale();
		if (happyHourSale == null)
		{
			GUILayout.Button("No Happy Hour Sale");
		}
		else if (GUILayout.Button("Happy Hour Sale"))
		{
			STUDSaleDialog.showDialog(STUDSale.getSale(SaleType.HAPPY_HOUR));
			DevGUI.isActive = false;
		}
		
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();

		STUDAction payerReactSale = STUDAction.findPayerReactivationSale();
		if (payerReactSale == null)
		{
			GUILayout.Button("No Payer Reactivation Sale");
		}
		else if (GUILayout.Button("Payer Reactivation Sale"))
		{
			STUDSaleDialog.showDialog(STUDSale.getSale(SaleType.PAYER_REACTIVATION));
			DevGUI.isActive = false;
		}
				
		STUDAction vipSale = STUDAction.findVipSale();
		if (vipSale == null)
		{
			GUILayout.Button("No VIP Sale");
		}
		else if (GUILayout.Button("VIP Sale"))
		{
			STUDSaleDialog.showDialog(STUDSale.getSale(SaleType.VIP));
			DevGUI.isActive = false;
		}
		
		GUILayout.EndHorizontal();

		PurchaseFeatureData data = null;
		
		data = PurchaseFeatureData.PopcornSale;
		if (data != null)
		{
			GUILayout.Label(data.type.ToString());
			indentedLabel(string.Format("Start Time: {0}", data.timerRange.startDate.ToString()));
			indentedLabel(string.Format("End Time: {0}", data.timerRange.endDate.ToString()));
			indentedLabel(string.Format("Image Path: {0}", data.imageFolderPath));
		}

		data = PurchaseFeatureData.PayerReactivationSale;
		if (data != null)
		{
			GUILayout.Label(data.type.ToString());
			indentedLabel(string.Format("Start Time: {0}", data.timerRange.startDate.ToString()));
			indentedLabel(string.Format("End Time: {0}", data.timerRange.endDate.ToString()));
			indentedLabel(string.Format("Image Path: {0}", data.imageFolderPath));
		}

		data = PurchaseFeatureData.VipSale;
		if (data != null)
		{
			GUILayout.Label(data.type.ToString());
			indentedLabel(string.Format("Start Time: {0}", data.timerRange.startDate.ToString()));
			indentedLabel(string.Format("End Time: {0}", data.timerRange.endDate.ToString()));
			indentedLabel(string.Format("Image Path: {0}", data.imageFolderPath));
		}

		data = PurchaseFeatureData.HappyHourSale;
		if (data != null)
		{
			GUILayout.Label(data.type.ToString());
			indentedLabel(string.Format("Start Time: {0}", data.timerRange.startDate.ToString()));
			indentedLabel(string.Format("End Time: {0}", data.timerRange.endDate.ToString()));
			indentedLabel(string.Format("Image Path: {0}", data.imageFolderPath));
		}
		GUILayout.EndVertical();
	}

	private void indentedLabel(string label, float space = 15f)
	{
		GUILayout.BeginHorizontal();
		GUILayout.Space(space);
		GUILayout.Label(label);
		GUILayout.EndHorizontal();
	}
	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}
