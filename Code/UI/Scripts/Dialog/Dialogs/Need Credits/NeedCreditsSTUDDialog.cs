using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
*/

public class NeedCreditsSTUDDialog : DialogBase
{
	public Transform creditsParent;
	public TextMeshPro creditsLabel;
	public TextMeshPro oldPriceLabel;
	public TextMeshPro newPriceLabel;
	public GameObject crossedOutLine;
	public TextMeshPro vipCreditsBonus;
	public TextMeshPro vipPoints;
	public VIPIconHandler vipIconHandler;
	public Renderer gameTexture;
	public GameObject vipOfferInfo;

	public ImageButtonHandler closeButton;
	public ImageButtonHandler buyButton;

	// Properties for Watch To Earn version.
	public GameObject w2eOfferInfo;
	public TextMeshPro w2eRewardLabel;
	
	public TextMeshPro buttonLabel; // Label for the shared button at the bottom of the dialog.
	
	protected PurchaseFeatureData featureData = null;
    protected CreditPackage creditPackage = null;

	/// Initialization
	public override void init()
	{
		closeButton.registerEventDelegate(closeClicked);
		buyButton.registerEventDelegate(buyClicked);
		
		// Override in subclasses.
		featureData = PurchaseFeatureData.OutOfCredits;
		economyTrackingName = "out_of_coins";
		downloadedTextureToRenderer(gameTexture, 0);
		buttonLabel.text = Localize.textUpper("buy");
		PurchasablePackage fallbackPackage = PurchasablePackage.find(Glb.NEED_CREDITS_DEFAULT_PACKAGE);		
		
		string vipCreditsText = "";
		string vipNewPriceText = "";
		string vipOldPriceText = "";
		string vipCreditsBonusText = "";
		string vipPointsText = "";

		if (featureData == null && fallbackPackage == null)
		{
			Debug.LogError("NeedCreditsSTUDDialog: Couldn't find out of credits STUD action! And no default package found.");
		}
		else if (WatchToEarn.isEnabled && w2eOfferInfo != null)
		{
			vipOfferInfo.SetActive(false);
			w2eOfferInfo.SetActive(true);
			w2eRewardLabel.text = Localize.textUpper("purchased_details{0}", CreditsEconomy.convertCredits(WatchToEarn.rewardAmount));
			buttonLabel.text = Localize.textUpper("watch");
		}
		else
		{	
			if (featureData == null)
			{
				// If we are here, then there is no STUDAction and we need to resort to the fallback package.
				creditPackage = new CreditPackage(fallbackPackage, 0, false);
			}
			else if (featureData.creditPackages != null && featureData.creditPackages.Count > 0)
			{
				creditPackage = featureData.creditPackages[0];
			}
			
			if (creditPackage != null)
			{
				vipCreditsText = CreditsEconomy.convertCredits(creditPackage.purchasePackage.totalCredits(creditPackage.bonus));
				vipNewPriceText = creditPackage.purchasePackage.getLocalizedPrice();	// This is pre-localized.
				vipOldPriceText = creditPackage.purchasePackage.getOriginalLocalizedPrice(100 + creditPackage.bonus);	// Get the localized price if it is available.

				long vipBonusCredits = creditPackage.purchasePackage.bonusVIPCredits();
				if (vipBonusCredits > 0)
				{
					vipCreditsBonusText = Localize.textUpper("plus_{0}_credits", CreditsEconomy.convertCredits(vipBonusCredits));
				}
				else
				{
					vipCreditsBonusText = Localize.text("vip_no_credits_bonus");
				}
				vipPointsText = Localize.textUpper("plus_{0}_points", CommonText.formatNumber(creditPackage.purchasePackage.vipPoints()));
			}
			
		    vipIconHandler.setToPlayerLevel();

			creditsLabel.text = vipCreditsText;
			newPriceLabel.text = vipNewPriceText;
			oldPriceLabel.text = vipOldPriceText;
			SafeSet.labelText(vipCreditsBonus, vipCreditsBonusText);
			SafeSet.labelText(vipPoints, vipPointsText);
			
			// Position the coin and amount label so that it's always horizontally centered.
			creditsLabel.ForceMeshUpdate();	// Force the bounds to be updated immediately after text changes.
			float width = creditsLabel.transform.localPosition.x + creditsLabel.bounds.size.x;
			CommonTransform.setX(creditsParent, width * -0.5f);

			if (string.IsNullOrEmpty(vipOldPriceText) || vipNewPriceText.Equals(vipOldPriceText))
			{
				crossedOutLine.SetActive(false);
				oldPriceLabel.gameObject.SetActive(false);
				// Horizontally center the price too.
				CommonTransform.setX(newPriceLabel.transform, 0.0f);
			}
		}
		Audio.play("minimenuopen0");
	}

	protected override void onFadeInComplete()
	{
		base.onFadeInComplete();
		if ((featureData == null && creditPackage == null) || 
			((creditPackage == null) &&
			!WatchToEarn.isEnabled))
		{
			// Something didn't work right, so close this dialog ASAP.
			// An error about what didn't work has already been logged in init().
			Dialog.close();
		}
		else
		{
			if (GameState.game != null) // don't crash when testing in the editor.
			{
				logViewStats();
			}
		}
	}
	
	void Update()
	{
		AndroidUtil.checkBackButton(closeClicked, "dialog", "android_dialog", "ooc", "", "", "back");
	}

	// This method was somehow generating an NRE, so I added excessive null checking.
	// https://app.crittercism.com/developers/crash-details/525d7eb9d0d8f716a9000006/c49df51040e1986bf353b29539662ce42e2b052bfee6975349eaa4b7
	protected virtual void logViewStats()
	{
		string gameKey;
		if (GameState.game == null || GameState.game.keyName == null)
		{
			gameKey = "game_not_found";
		}
		else
		{
			gameKey = GameState.game.keyName;
		}
		string creditPackageString = "";
		if (creditPackage == null || creditPackage.purchasePackage == null || creditPackage.purchasePackage.keyName == null)
		{
			creditPackageString = "package_not_found";
		}
		else
		{
			creditPackageString = creditPackage.purchasePackage.keyName;
		}
		if (StatsManager.Instance != null)
		{
			StatsManager.Instance.LogCount("dialog", "out_of_coins", creditPackageString, gameKey, "", "view");
		}
	}
	
    protected virtual void logPurchaseStats()
	{
		string gameKey = (GameState.game == null) ? "game_not_found" : GameState.game.keyName;
		string creditPackageString = ((creditPackage == null) || (creditPackage.purchasePackage == null)) ? "package_not_found": creditPackage.purchasePackage.keyName;
		StatsManager.Instance.LogCount("dialog", "out_of_coins", "", gameKey, creditPackageString, "click");
	}

	// Not a click delegate but called from the option class.
	protected virtual void buyClicked(Dict args = null)
	{
		if (creditPackage != null)
		{
			creditPackage.purchasePackage.makePurchase(creditPackage.bonus);
			logPurchaseStats();
		}
	}

	// Close Button delegate
	public virtual void closeClicked(Dict args = null)
	{
		Dialog.close();

		if (MobileXpromo.shouldShow(MobileXpromo.SurfacingPoint.OOC))
		{
			MobileXpromo.showXpromo(MobileXpromo.SurfacingPoint.OOC);
		}
		else if (ExperimentWrapper.ZadeXPromo.isInExperiment)
		{
			//TODO: Girish
			//ZADEAdManager.Instance.RequestAd(ZADEAdManager.ZADE_OOC_SLOT_NAME, onZadeAdLoaded, onZadeAdLoadedError); 
		}
		if (GameState.game != null) // don't crash when testing in the editor.
		{
			StatsManager.Instance.LogCount("dialog", "out_of_coins", "", GameState.game.keyName, "close", "click");
			SlotBaseGame.logOutOfCoinsPurchaseStat(false);
		}
	}
	
	/// Called by Dialog.close() - do not call directly.
	public override void close()
	{
		// Do special cleanup.
	}
	
	public static void showDialog()
	{
		if (ExperimentWrapper.NeedCreditsThreeOptions.isInExperiment && PurchaseFeatureData.OutOfCreditsThree != null)
		{
			// If we are in the experiment, and the package is set up, then use the three package setup.
			NeedCreditsMultiDialog.showDialog();
		}
		else
		{
			// Otherwise call NeedCreditsSTUDDialog and it will use the fallback packages.
			if (GameState.isMainLobby) // don't crash when testing in the lobby.
			{
				GenericDialog.showDialog(
					Dict.create(
						D.TITLE, "Ooops",
						D.MESSAGE, "You must be in a game (not the lobby) to test this dialog.",
						D.REASON, "need-credits-stud-dialog-not-in-game"
					)
				);
				return;
			}

			List<string> filenames = new List<string>()
			{
				SlotResourceMap.getLobbyImagePath(GameState.game.groupInfo.keyName, GameState.game.keyName)
			};
			
			Dialog.instance.showDialogAfterDownloadingTextures("need_credits_stud", nonMappedBundledTextures:filenames.ToArray());
		}
	}
}
