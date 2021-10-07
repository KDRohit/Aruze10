using UnityEngine;
using System.Collections;
using Com.Scheduler;
using TMPro;

public class NeedCreditsMultiDialog : DialogBase
{
	public NeedCreditsOption[] options;
	public Renderer backgroundRenderer;
	public VIPNewIcon vipIcon;
	public GameObject vipBonus;
	public GameObject watchToEarnUI;
	public TextMeshPro watchToEarnLabel;
	public Renderer watchToEarnBackgroundRenderer;

	private bool shouldCloseImmediately = false;
	private string coinPackagesStatsString = "";
	private string gameKey = "";
	private int packageIndexOffset = 0;		// allows us to skip a credit package
	
	public override void init()
	{
		downloadedTextureToRenderer(backgroundRenderer, 0);
		downloadedTextureToRenderer(watchToEarnBackgroundRenderer, 1);

		// make sure all options are active to start
		for (int i = 0; i < options.Length; i++)
		{
			options[i].gameObject.SetActive(true);
		}		

		initWatchToEarn();		

		vipIcon.setLevel(SlotsPlayer.instance.vipNewLevel);
		PurchaseFeatureData featureData = PurchaseFeatureData.OutOfCreditsThree;
		if (featureData != null && options.Length == featureData.creditPackages.Count)
		{
			// The number of packages need to match.
			for (int i = 0; i < options.Length; i++)
			{
				if (options[i].gameObject.activeSelf)		// watch to earn may have deactivated this option, only process active ones
				{
					int packageIndex = Mathf.Max(i + packageIndexOffset, 0);  // adjust index of which credit package to read from

					NeedCreditsOption option = options[i];
					option.init(featureData.creditPackages[packageIndex], this);
					coinPackagesStatsString += featureData.creditPackages[packageIndex].purchasePackage.keyName + ((i == options.Length -1) ? "" : ",");
				}
			}
		}
		else
		{
			shouldCloseImmediately = true;
		}
		StatsManager.Instance.LogCount("dialog", "out_of_coins", "", "", "", "view");
		
		// Only show the VIP bonus percent if there actually is any bonus.
		VIPLevel vipLevel = VIPLevel.find(SlotsPlayer.instance.vipNewLevel);
		vipBonus.SetActive(vipLevel != null && vipLevel.purchaseBonusPct > 0);
		gameKey = (GameState.game != null) ? GameState.game.keyName : "";

	}

	public void initWatchToEarn()
	{
		if (WatchToEarn.isEnabled && ExperimentWrapper.WatchToEarn.shouldShowOutOfCredits)
		{
			Audio.play("W2ECoinBoosterDialog");
			if (options.Length > 0)
			{
				// the first option wll be overlayed by watch to earn UI so hide it
				options[0].gameObject.SetActive(false);

				packageIndexOffset -= 1;   // account for hidden package
			}

			SafeSet.gameObjectActive(watchToEarnUI, true);

			SafeSet.labelText(watchToEarnLabel, CreditsEconomy.convertCredits(WatchToEarn.rewardAmount));

			coinPackagesStatsString += ",w2e";
		}
		else
		{
			SafeSet.gameObjectActive(watchToEarnUI, false);	
		}
	}

	public void Update()
	{
		AndroidUtil.checkBackButton(closeClicked);
	}
	
	protected override void onFadeInComplete()
	{
		// Sanity of gameKey checking because of Crittercism report.
		// https://app.crittercism.com/developers/crash-details/5616f9118d4d8c0a00d07cf0/5aaa1a6853529640c7ad2a83bb923a30be15786196afe753c3acfaa8
		string gameKey;
		if (GameState.game == null)
		{
			gameKey = "unknown";
		}
		else
		{
			gameKey = GameState.game.keyName;
		}
		
		StatsManager.Instance.LogCount("dialog", "out_of_coins", coinPackagesStatsString, gameKey, "", "view");
		base.onFadeInComplete();
		if (shouldCloseImmediately)
		{
			Debug.LogWarning("NeedCreditsMultiDialog -- Something went wrong, closing dialog.");
			// Something went wrong, so close the dialog.
			Dialog.close();
		}
	}
	
	public override void close()
	{
		// Do special cleanup here.
	}

	public void moreOptionsClicked()
	{
		StatsManager.Instance.LogCount("dialog", "out_of_coins", "", gameKey, "more_offers", "click");
		Dialog.close();
		BuyCreditsDialog.showDialog("", SchedulerPriority.PriorityType.IMMEDIATE);
		Audio.play("minimenuclose0");
	}

	public void closeClicked()
	{
	 	StatsManager.Instance.LogCount("dialog", "out_of_coins", coinPackagesStatsString, gameKey, "close", "click");
		Dialog.close();
		Audio.play("minimenuclose0");
	}

	public void watchToEarnClicked()
	{
		WatchToEarn.watchVideoClickHandler("out_of_coins", gameKey, "ooc");
		Dialog.close();
		Audio.play("minimenuclose0");
	}
	
	public void makePurchase(CreditPackage creditPackage)
	{
		StatsManager.Instance.LogCount("dialog", "out_of_coins", "", gameKey, "creditPackage.package.keyName", "click");
		creditPackage.purchasePackage.makePurchase(creditPackage.bonus);
	}
	
	public static void showDialog()
	{
		PurchaseFeatureData featureData = PurchaseFeatureData.OutOfCreditsThree;
		if (featureData != null)
		{
			string[] texturePaths = new string[]
			{
				"misc_dialogs/watch_to_earn/W2E_Coins_OOC.png"
			};			
			// Only show the dialog if the action is not null.
			Dialog.instance.showDialogAfterDownloadingTextures("need_credits_three", texturePaths);
		}
	}
		
}