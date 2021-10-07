using UnityEngine;
using System.Collections;
using System;
using TMPro;

/**
StarterDialog

This dialog presents a new-ish user with a special offer.
**/
public class StarterDialog : DialogBase, IResetGame
{
	public TextMeshPro priceOldLabel;
	public TextMeshPro priceNewLabel;
	public TextMeshPro packCreditsLabel;
	public TextMeshPro timerLabel;
	public GameObject crossoutLine;
	public Renderer backgroundRenderer;
	public ClickHandler buyButton;
	public ClickHandler closeButton;
	[SerializeField] 
	public GameObject[] vfxObjects;

	public static GameTimerRange saleTimer = null;
	public static bool didPurchase = false; // A flag of status

	protected PurchasablePackage creditPackage = null;
	protected PurchasablePackage oldPricePackage = null;
	
	public static bool isActive
	{
		get
		{
			// Any conditions added here should also be added to the notActiveReason getter.
			return
				ExperimentWrapper.StarterPackEos.isInExperiment &&
				!ExperimentWrapper.LifecycleSales.isInExperiment && // cant be in lifecycles
				!didPurchase &&
				Packages.PaymentsManagerEnabled() &&
				saleTimer != null &&
				!saleTimer.isExpired;
		}
	}
	
	public static string notActiveReason
	{
		get
		{
			string reason = "";
			if (!ExperimentWrapper.StarterPackEos.isInExperiment)
			{
				reason += "Not in StarterPackEos experiment.\n";
			}
			if (didPurchase)
			{
				reason += "Already purchased.\n";
			}
			if (!NewEconomyManager.PurchasesEnabled)
			{
				reason += "Purchases not enabled in EconomyManager.\n";
			}
			if (saleTimer == null || saleTimer.isExpired)
			{
				reason += "Not within sale timeframe.\n";
			}
			return reason;
		}
	}

	private void setupVfxObjects()
	{
		if (vfxObjects == null || vfxObjects.Length == 0)
		{
			return;
		}
		
		switch (ExperimentWrapper.StarterPackEos.artPackage)
		{
			case "design4_00":
				vfxObjects[0].SetActive(true);
				break;
			case "design4_01":
				vfxObjects[1].SetActive(true);
				break;
			case "design4_02":
				vfxObjects[2].SetActive(true);
				break;
			case "design4_03":
				vfxObjects[3].SetActive(true);
				break;
		}
	}

	/// Initialization
	public override void init()
	{
		downloadedTextureToRenderer(backgroundRenderer, 0);
		// Configure our pricing availability based on the experiment we're in.
		priceOldLabel.text = "";
		priceNewLabel.text = "";
		packCreditsLabel.text = "";
		setCreditPackageData();
		MOTDFramework.markMotdSeen(dialogArgs);
		// Log that we've seen this dialog:
		StatsManager.Instance.LogCount("dialog", "starter_pack", "", "", GameState.currentStateName, "view");

		if (saleTimer != null)
		{
			timerLabel.text = saleTimer.timeRemainingFormatted;
		}

		economyTrackingName = "starter_pack";
		buyButton.registerEventDelegate(onClickBuy);
		closeButton.registerEventDelegate(onClickClose);
		setupVfxObjects();
	}

	protected virtual void setCreditPackageData()
	{
		string itemName = ExperimentWrapper.StarterPackEos.creditPackageName;
		if (!string.IsNullOrEmpty(itemName))
		{
			creditPackage = PurchasablePackage.find(itemName);
			if (creditPackage == null)
			{
				Debug.LogWarning("StarterDialog -- Cannot find PurchasablePackage: " + itemName);
			}
			else
			{
				int packagePricePoint = Convert.ToInt32(ExperimentWrapper.StarterPackEos.strikethroughAmount);	
				oldPricePackage = PurchasablePackage.findByPriceTier(packagePricePoint, true);
				if (!string.IsNullOrEmpty(ExperimentWrapper.StarterPackEos.packageOfferString))
				{
					priceNewLabel.text = Localize.textUpper(ExperimentWrapper.StarterPackEos.packageOfferString, creditPackage.getLocalizedPrice());
				}
				else
				{
					priceNewLabel.text = Localize.textUpper("now_{0}_no_break", creditPackage.getLocalizedPrice());
				}
				priceOldLabel.text = Localize.textUpper("was_{0}_no_break", oldPricePackage.getLocalizedPrice());
				if ((priceOldLabel.text == "") || (priceNewLabel.text == priceOldLabel.text))
				{
					crossoutLine.SetActive(false);
					priceOldLabel.gameObject.SetActive(false);
				}
				packCreditsLabel.text = CreditsEconomy.convertCredits(creditPackage.totalCredits(totalSaleBonus));
			}
		}
		else
		{
			Debug.LogError("StarterDialog -- itemName from EOS was null");
		}
	}
	
	protected virtual void Update()
	{
		AndroidUtil.checkBackButton(onClickClose, "dialog", "starter_pack", phylumName, "", "", "back");
		if (saleTimer != null)
		{
			timerLabel.text = saleTimer.timeRemainingFormatted;
		}
	}

	public virtual void onClickBuy(Dict args = null)
	{
		// Purchase the pack at the discount suggested.
		if (creditPackage != null)
		{
			creditPackage.makePurchase(totalSaleBonus);	// Will close the dialog if purchase is successful.
			StatsManager.Instance.LogCount("dialog", "starter_pack", "", creditPackage.keyName, "buy", "click");
		}
	}

	public virtual void onClickClose(Dict args = null)
	{
		if (creditPackage != null)
		{
			StatsManager.Instance.LogCount("dialog", "starter_pack", "", creditPackage.keyName, "buy", "click");
		}

		SlotBaseGame.logOutOfCoinsPurchaseStat(false);

		Dialog.close();
	}
			
	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup.
	}

	private int totalSaleBonus
	{
		get
		{
			int powerupBonus = 0;

			if (PowerupsManager.isPowerupsEnabled && PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_BUY_PAGE_KEY))
			{
				powerupBonus = BuyPageBonusPowerup.salePercent;
			}

			return ExperimentWrapper.StarterPackEos.bonusPercent + powerupBonus;
		}
	}
	
	private static string imagePath
	{
		get
		{
			if (ExperimentWrapper.StarterPackEos.artPackage != "")
			{
				return string.Format(StarterDialogHIR.IMAGE_PATH, ExperimentWrapper.StarterPackEos.artPackage);
			}
			else
			{
				Debug.LogError("artPackage string is empty!");
				return "";
			}
		}
	}

	public static string phylumName
	{
		get
		{
			return StarterDialogHIR.PHYLUM_NAME;
		}
	}

	// Called by BuyCreditsDialog.preloadDialogTextures().
    public static void preloadDialogTextures()
	{
		if (imagePath != "")
		{
			DisplayAsset.preloadTexture(imagePath, true);
		}
	}
	
	public static bool showDialog(string motdKey = "")
	{
		Dict args = Dict.create
		(
			D.IS_LOBBY_ONLY_DIALOG, GameState.isMainLobby,
			D.MOTD_KEY, motdKey
		);
		
		Dialog.instance.showDialogAfterDownloadingTextures("starter_pack_" + ExperimentWrapper.StarterPackEos.artPackage, imagePath, args, true, skipBundleMapping:true);
		return true;
	}

	public static void resetStaticClassData()
	{
		saleTimer = null;
		didPurchase = false;
	}
}
