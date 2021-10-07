using UnityEngine;
using System.Collections;
using System;
using Com.Scheduler;
using TMPro;

/**
Lifecycle Dialog

This dialog presents a lapsed user, or a user who hasn't purchased in awhile with a discounted offer
**/
public class LifecycleDialog : DialogBase, IResetGame
{
	public TextMeshPro priceOldLabel;
	public TextMeshPro priceNewLabel;
	public TextMeshPro packCreditsLabel;
	public TextMeshPro packCreditsShadowLabel;
	public TextMeshPro timerLabel;
	public GameObject crossoutLine;
	public Renderer backgroundRenderer;
	public ClickHandler buyButton;
	public ClickHandler closeButton;
	public TextMeshPro expireWordLabel;

	public static GameTimerRange saleTimer = null;
	public static bool didPurchase = false; // A flag of status

	protected PurchasablePackage creditPackage = null;
	protected PurchasablePackage oldPricePackage = null;

	public const string IMAGE_PATH = "misc_dialogs/lifecycle_sales/{0}.png";
	public const string PHYLUM_NAME = "lapsed_payer_sale_dialog";
	
	public static bool isActive
	{
		get
		{
			// Any conditions added here should also be added to the notActiveReason getter.
			return
				ExperimentWrapper.LifecycleSales.isInExperiment &&
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
			if (!ExperimentWrapper.LifecycleSales.isInExperiment)
			{
				reason += "Not in LifecycleSales experiment.\n";
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

	/// Initialization
	public override void init()
	{
		downloadedTextureToRenderer(backgroundRenderer, 0);
		// Configure our pricing availability based on the experiment we're in.
		priceOldLabel.text = "";
		priceNewLabel.text = "";
		packCreditsLabel.text = "";
		packCreditsShadowLabel.text = "";
		setCreditPackageData();
		MOTDFramework.markMotdSeen(dialogArgs);

		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "lapsed_payer_sale_dialog",
			phylum: "lobby",
			klass: "",
			family: "auto_surface",
			genus: "view"
		);

		if (saleTimer != null)
		{
			timerLabel.text = saleTimer.timeRemainingFormatted;
		}

		economyTrackingName = "lapsed_payer_sale_dialog";
		buyButton.registerEventDelegate(onClickBuy);
		closeButton.registerEventDelegate(onClickClose);
	}

	protected virtual void setCreditPackageData()
	{
		string itemName = ExperimentWrapper.LifecycleSales.creditPackageName;
		if (!string.IsNullOrEmpty(itemName))
		{
			creditPackage = PurchasablePackage.find(itemName);
			if (creditPackage == null)
			{
				Debug.LogWarning("LifecycleDialog -- Cannot find PurchasablePackage: " + itemName);
			}
			else
			{
				int packagePricePoint = Convert.ToInt32(ExperimentWrapper.LifecycleSales.strikethroughAmount);	
				oldPricePackage = PurchasablePackage.findByPriceTier(packagePricePoint, true);
				priceOldLabel.text = Localize.textUpper("was_{0}_no_break", oldPricePackage.getLocalizedPrice());
				priceNewLabel.text = Localize.textUpper("now_{0}_no_break", creditPackage.getLocalizedPrice());
				if ((priceOldLabel.text == "") || (priceNewLabel.text == priceOldLabel.text))
				{
					crossoutLine.SetActive(false);
					priceOldLabel.gameObject.SetActive(false);
				}
				packCreditsLabel.text = CreditsEconomy.convertCredits(creditPackage.totalCredits(totalSaleBonus));
				packCreditsShadowLabel.text = packCreditsLabel.text;
			}
		}
		else
		{
			Debug.LogError("LifecycleDialog -- itemName from EOS was null");
		}
	}
	
	protected virtual void Update()
	{
		AndroidUtil.checkBackButton(onClickClose);
		if (saleTimer != null)
		{
			timerLabel.text = saleTimer.timeRemainingFormatted;
		}
		
		if (saleTimer == null || saleTimer.isExpired)
		{
			timerLabel.gameObject.SetActive(false);
			expireWordLabel.gameObject.SetActive(false);
		}
	}

	public virtual void onClickBuy(Dict args = null)
	{
		// Purchase the pack at the discount suggested.
		if (creditPackage != null)
		{
			creditPackage.makePurchase
			(
				bonusPercent: totalSaleBonus,
				isMultiplierPurchase: false,
				buyCreditsPagePackageIndex: -1,
				packageClass: "",
				saleBonusPercent: 0,
				buffKey: "",
				economyTrackingNameOverride: null,
				economyTrackingVariantOverride: null,
				isLifecycleSale: true
			);	// Will close the dialog if purchase is successful.
			StatsManager.Instance.LogCount("dialog", "lapsed_payer_sale_dialog", "", creditPackage.keyName, "buy", "click");
		}
	}

	public virtual void onClickClose(Dict args = null)
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "lapsed_payer_sale_dialog",
			phylum: "lobby",
			klass: "",
			family: "close",
			genus: "click"
		);
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

			return ExperimentWrapper.LifecycleSales.bonusPercent + powerupBonus;
		}
	}
	
	private static string imagePath
	{
		get
		{
			if (ExperimentWrapper.LifecycleSales.dialogImage != "")
			{
				return string.Format(IMAGE_PATH, ExperimentWrapper.LifecycleSales.dialogImage);
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
			return PHYLUM_NAME;
		}
	}

	// Called by BuyCreditsDialog.preloadDialogTextures().
    public static void preloadDialogTextures()
	{
		if (imagePath != "")
		{
			DisplayAsset.preloadTexture(imagePath);
		}
	}
	
	public static bool showDialog(string motdKey = "")
	{
		Dict args = Dict.create
		(
			D.IS_LOBBY_ONLY_DIALOG, GameState.isMainLobby,
			D.MOTD_KEY, motdKey
		);
		
		Dialog.instance.showDialogAfterDownloadingTextures("lifecycle_dialog", imagePath, args, true, SchedulerPriority.PriorityType.IMMEDIATE);
		return true;
	}

	public static void resetStaticClassData()
	{
		saleTimer = null;
		didPurchase = false;
	}
}
