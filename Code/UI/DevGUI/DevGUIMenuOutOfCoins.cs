using System.Collections;
using System.Collections.Generic;
using Com.HitItRich.Feature.OOCRebound;
using UnityEngine;

public class DevGUIMenuOutOfCoins : DevGUIMenu 
{
	public static bool outOfCoinsOnNextSpin = false;

	public override void drawGuts()
	{
		GUILayout.Label("Out Of Coins");

		GUILayout.Label("hir_ooc_buy_page experiment enabled = " + ExperimentWrapper.OutOfCoinsBuyPage.isEnabled);
		if (ExperimentWrapper.OutOfCoinsBuyPage.isEnabled)
		{
			GUILayout.Label("hir_ooc_buy_page experiment variant = " + ExperimentWrapper.OutOfCoinsBuyPage.variantName);
		}
		
		GUILayout.Label("hir_special_ooc experiment enabled = " + ExperimentWrapper.SpecialOutOfCoins.experimentData.isEnabled);
		if (ExperimentWrapper.SpecialOutOfCoins.experimentData.isEnabled)
		{
			bool canCollect = OOCReboundFeature.isAvailableForCollect;
			GUILayout.Label("OOC Rebound available: " + canCollect);
			if (!canCollect)
			{
				GUILayout.Label(OOCReboundFeature.notAvailableReason);
			}
		}

		GUILayout.Label(" ");

		outOfCoinsOnNextSpin = GUILayout.Toggle(outOfCoinsOnNextSpin, "Trigger Out Of Coins flow on next spin.");

		if (GUILayout.Button("Show Intermediary Dialog"))
		{
			NeedCredtisNotifyDialog.showDialog();
		}

#if !ZYNGA_PRODUCTION
		if (ExperimentWrapper.SpecialOutOfCoins.experimentData.isEnabled)
		{
			if (GUILayout.Button("Show OOC Rebound Dialog"))
			{
				OutOfCoinsReboundDialog.showDialog(null, null);
			}
			if (GUILayout.Button("Trigger Special OOC on next spin"))
			{
				OOCAction.devTriggerNextSpin();
				Glb.resetGame("dev menu");
			}
		}
#endif

		drawPriorityList();
	}

	public void drawPriorityList()
	{
		GUILayout.Label(" ");
		GUILayout.Label("-------------------------- Priority List ---------------------------");
		GUILayout.Label("Experiment out_of_coins_sale_priority : " + ExperimentWrapper.OutOfCoinsPriority.isInExperiment);

		if (ExperimentWrapper.OutOfCoinsBuyPage.showIntermediaryDialog)
		{
			GUILayout.Label("--You will see the intermediary dialog and then....");
		}
		else
		{
			GUILayout.Label("--You will NOT see the intermediary dialog and directly see ");
		}


		GUILayout.Label("Priority #0 Watch To Earn Offer :  " + WatchToEarn.isEnabled);
		if (WatchToEarn.isEnabled)
		{
			GUILayout.Label("--You will be taken to the watch to earn offer.");
			return;
		}
		else
		{
			GUILayout.Label("--No Watch to Earn offer.");
		}

		GUILayout.Label("Priority #1 First Purchase Offer :  " + ExperimentWrapper.FirstPurchaseOffer.isInExperiment);
		if (ExperimentWrapper.FirstPurchaseOffer.isInExperiment)
		{
			GUILayout.Label("--You will be taken to the buy page for first purchase offer when running out of coins");
			return;
		}
		else
		{
			GUILayout.Label("--No First Purchase offer.");
		}

		GUILayout.Label("Priority #2 PurchaseFeatureData Offer");
		if (PurchaseFeatureData.isSaleActive && ExperimentWrapper.OutOfCoinsPriority.isInExperiment)
		{
			GUILayout.Label("--PurchaseFeatureData sale is active out_of_coins_sale_priority is true, you should see buy page with PurchaseFeature offer.");
			return;	
		}
		else
		{
			GUILayout.Label("--No PurchaseFeatureData offer.");
		}

		GUILayout.Label("Priority #3 StarterDialog Offer");
		if (StarterDialog.isActive) 
		{
			GUILayout.Label("-StarterDialog sale is active, you should see StarterDialog offer.");
			return;	
		}
		else
		{
			GUILayout.Label("-No StarterDialog offer.");
		}

		GUILayout.Label("Priority #4 OutOfCoinsBuyPage experiment");
		if (ExperimentWrapper.OutOfCoinsBuyPage.isInExperiment) 
		{
			GUILayout.Label("-ooc_buy_page isInExperiment is active, you should see ooc buy page.");
			return;	
		}
		else
		{
			GUILayout.Label("-Not in ooc_buy_page experiment you will fall through to legacy ooc motd.");
		}
	}

	new public static void resetStaticClassData()
	{
		outOfCoinsOnNextSpin = false;
	}
}
