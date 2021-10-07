using UnityEngine;
using Com.HitItRich.Feature.BundleSale;

public class DevGUIMenuBundleSale: DevGUIMenu
{
    public override void drawGuts()
    {
       	bool featureEnabled = BundleSaleFeature.instance?.isEnabled ?? false;


		GUIStyle redStyle = new GUIStyle();
		redStyle.normal.textColor = Color.red;
		
		GUIStyle greenStyle = new GUIStyle();
		greenStyle.normal.textColor = Color.green;
		
		GUILayout.BeginVertical();	
		
		
		GUILayout.Label("Feature Enabled: " + featureEnabled);
		if (featureEnabled)
		{
			GUILayout.BeginHorizontal();

			GUILayout.BeginVertical();

			GUILayout.Label("Cool Down Time: " + BundleSaleFeature.instance.coolDown);
			GUILayout.Label("Purchases Limit: " + BundleSaleFeature.instance.purchaseLimit);
			GUILayout.Label("Purchases Remaining: " + BundleSaleFeature.instance.purchaseRemaining);
			GUILayout.Label("Package Key: " + BundleSaleFeature.instance.coinPackageKey);
			GUILayout.Label("Can Show Purchase " + BundleSaleFeature.instance.canShow());
			GUILayout.Label("Current Cooldown: " + BundleSaleFeature.instance.getCooldownRemaining());
			GUILayout.Label("Current Buff Timer: " + BundleSaleFeature.instance.getBuffTimeRemaining());

			GUILayout.EndVertical();

			GUILayout.BeginVertical();

#if !ZYNGA_PRODUCTION
			GUILayout.Label("Bundle Sale Items:");
			if (BundleSaleFeature.instance.itemsInBundle != null)
			{
				foreach (BundleSaleFeature.BundleItem item in BundleSaleFeature.instance.itemsInBundle)
				{
					if (item == null)
					{
						continue;
					}

					GUILayout.BeginHorizontal();

					GUILayout.Label(
						"Item: " + item.getTitle() + ", type: " + item.buffType + ", duration: " + item.buffDuration,
						item.active ? greenStyle : redStyle);

					GUILayout.EndHorizontal();
				}
			}
			
			
			if (GUILayout.Button("Purchase"))
			{
				BundleSaleFeature.instance.devMockPurchase();
			}
			if (GUILayout.Button("Reset Purchase Limit"))
			{
				BundleSaleFeature.instance.devResetPurchaseLimt();
			}
			if (GUILayout.Button("Reset Buffs"))
			{
				BundleSaleActions.devBuffReset();
			}
			if (GUILayout.Button("Reset Cooldown"))
			{
				BundleSaleFeature.instance.devResetCooldown();
			}
			if (GUILayout.Button("End Buff Timer in 10 seconds"))
			{
				BundleSaleFeature.instance.devEndBuffTimerInSeconds(10);
			}
			if (GUILayout.Button("Sale Timer in 10 seconds"))
			{
				BundleSaleFeature.instance.devEndSaleTimerInSeconds(10);
			}
			if (GUILayout.Button("Show Dialog"))
			{
				BundleSaleFeature.instance.showDialog();
			}
#endif
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
			
		}
		GUILayout.EndVertical();
    }
}
