using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
A dev panel.
*/

public class DevGUIMenuPurchaseFeatureData : DevGUIMenu
{
    private PurchaseFeatureData.Type type;
    private PurchaseFeatureData data = null;
	
	public override void drawGuts()
	{
		Color originalColor = GUI.color;
		GUILayout.BeginHorizontal();
		// Left Side
		GUILayout.BeginVertical();
		foreach(PurchaseFeatureData.Type curType in System.Enum.GetValues(typeof(PurchaseFeatureData.Type)))
		{
			// Make a button for each feature type.
			if (GUILayout.Button(curType.ToString()))
			{
				selectType(curType);
			}
		}
		GUILayout.EndVertical();

		// Right Side
		GUILayout.BeginVertical();
		if (data != null)
		{
			GUILayout.Label(string.Format("Should Read EOS: {0}", PurchaseFeatureData.shouldReadEos));
			GUILayout.Label(type.ToString());
			if (PurchaseFeatureData.allEos.ContainsKey(type) && PurchaseFeatureData.shouldReadEos)
			{
				GUILayout.Label("EOS is available and used.");
			}
			else if (PurchaseFeatureData.allStud.ContainsKey(type))
			{
				GUILayout.Label("Using STUD.");
			}
			else
			{
				GUILayout.Label("Not using STUD or EOS...");
			}

			for (int i = 0; i < data.creditPackages.Count; i++)
			{
				GUILayout.BeginHorizontal();				
				CreditPackage cp = data.creditPackages[i];
				GUI.color = Color.red;
				GUILayout.BeginVertical();
				GUILayout.Label(string.Format("Package {0} -- {1}", i, cp.purchasePackage.keyName));
				GUILayout.EndVertical();
				GUILayout.BeginVertical();
				GUI.color = Color.blue;
				GUILayout.Label(string.Format("Bonus: {0}", cp.bonus));
				GUILayout.Label(string.Format("Sale Bonus: {0}", cp.saleBonusPercent));
				GUILayout.Label(string.Format("Event: {0}", cp.activeEvent.ToString()));
				GUILayout.Label(string.Format("Event Text: {0}", cp.eventText));
				GUILayout.EndVertical();
				GUILayout.EndHorizontal();
				GUILayout.Space(10f);
			}
			GUI.color = originalColor;			

		    GUILayout.Label("Image: " + data.imageFolderPath);
			if (data.timerRange != null)
			{
				GUILayout.Label("Start: " + data.timerRange.startDateFormatted);
				GUILayout.Label("End: " + data.timerRange.endDateFormatted);
				GUILayout.Label("Is Active: " + data.timerRange.isActive.ToString());
			}
			else
			{
				GUILayout.Label("No Timer");
			}

			GUILayout.Label("Card Events: ");
			if (data.cardEvents != null && data.cardEvents.Count > 0)
			{
				foreach(KeyValuePair<CreditPackage.CreditEvent, string> pair in data.cardEvents)
				{
					GUILayout.Label(pair.Key.ToString() + "::" + pair.Value);
				}
			}
		}
		else
		{
			GUILayout.Label("Either no Purchase Feature Data exists, or you have to select a value.");
		}
		GUILayout.EndVertical();
		GUILayout.EndHorizontal();
		GUI.color = originalColor;
	}

	private void selectType(PurchaseFeatureData.Type type)
	{
		this.type = type;
	    this.data = PurchaseFeatureData.find(type);
	}
	
	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}
