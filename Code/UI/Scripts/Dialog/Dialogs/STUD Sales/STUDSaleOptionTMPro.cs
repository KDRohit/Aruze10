using UnityEngine;
using System.Collections;
using TMPro;
using System.Collections.Generic;
using Com.HitItRich.Feature.VirtualPets;

public class STUDSaleOptionTMPro : MonoBehaviour 
{
	protected const int COIN_SPACING = 40;	// Spacing amount between coin icon and left edge of credits amount label.
	
	public int index; // The package index for this SaleOption from left to right (ascending price order).
	
	public TextMeshPro bonusAmountLabel;		// Label representing the bonus percentage.
	public TextMeshPro totalCreditsLabel;		// Label representing the total number of credits (after applying bonus).
	public TextMeshPro vipPointsLabel;			// Label representing the number of VIP points the player will get after purchasing this package.
	public TextMeshPro vipPointsAmountLabel;	// Label representing the number of VIP points (without the VIP POINTS text.)
	public TextMeshPro priceLabel;				// Label representing the the price of this package.
	public TextMeshPro oldPriceLabel;			// Label representing the "previous" price of this package, as calculated by the sale bonus percent.
	public Transform coinIcon;					// Needs to be dynamically positioned next to the credits amount label;
	public ClickHandler buyButton;		// Button to purchase a package.
	public GameObject adjectiveLabelParent;		// Label representing the adjective("Best", "Better", "Good") value of the package. On by default
	public GameObject salePercentLabelParent;	// Parent object for the bonus amount labels
	public GameObject collectablesPackParent;

	protected PurchasablePackage creditPackage;
	protected int bonusPercentage = 0;
	protected int salePercentage = 0;
	protected STUDSale sale;
	protected bool isPackageValid = true;

	protected string collectablePackName = "";
	protected string petTreatKey = "";
	private PurchaseFeatureData.Type saleType = PurchaseFeatureData.Type.NONE;
	[SerializeField] private PurchasePerksPanel perksPanel;
	
	// Sets the package for this option, and updates the UI to represent this information.
	public virtual void setOption(CreditPackage dataPackage, STUDSale sale, List<PurchasePerksPanel.PerkType> perks = null, PurchasePerksCycler cycler = null)
	{
		if (dataPackage == null || dataPackage.purchasePackage == null || dataPackage.purchasePackage.newZItem == null)
		{
			isPackageValid = false;

			// Turn off gameObject so the user cannot purchase a broken item
			gameObject.SetActive(false);

			if (dataPackage == null)
			{
				Debug.LogError("dataPackage is null, STUD did not create this package");
			}
			else if (dataPackage.purchasePackage == null)
			{
				Debug.LogError("No Purchasable Package could be found");
			}
			else if (dataPackage.purchasePackage.newZItem == null)
			{
				Debug.LogError("The package items were null."); 
			}
		}
		else
		{
			// Setting the labels will be done by the sku-subclass.
			this.sale = sale;
			bonusPercentage = dataPackage.bonus;
			creditPackage = dataPackage.purchasePackage;
			salePercentage = dataPackage.getSaleBonus();
			collectablePackName = dataPackage.collectableDropKeyName;
			// Hook up the ButtonHandler for purchasing the package
			buyButton.registerEventDelegate(purchasePackage);
			if (VirtualPetsFeature.instance != null && VirtualPetsFeature.instance.isEnabled)
			{
				VirtualPetTreat treat = VirtualPetsFeature.instance.getTreatTypeForPackage(dataPackage.purchasePackage);
				petTreatKey = treat != null ? treat.keyName : "";
			}
			if (!ExperimentWrapper.BuyPageDrawer.isInExperiment)
			{
				if (Collectables.isActive())
				{
					if (!string.IsNullOrEmpty(collectablePackName) && collectablePackName != "nothing")
					{
						AssetBundleManager.load(this, "Features/Collections/Prefabs/Lobby & Buy Page/Popcorn Sale Pack",
							collectablesIconLoadedSuccess, collectablesIconLoadedFailed);
					}
				}
			}
			else
			{
				setupPerksPanel(dataPackage, perks, cycler);
			}
		}
	}

	private void setupPerksPanel(CreditPackage package, List<PurchasePerksPanel.PerkType> perks, PurchasePerksCycler cycler)
	{
		vipPointsLabel.gameObject.SetActive(false);
		perksPanel.gameObject.SetActive(true);
		perksPanel.init(-1, package, sale.dialogTypeKey, perks, perksCycler:cycler, purchaseType:PurchaseFeatureData.Type.POPCORN_SALE);
	}

	protected virtual void setLabels(int saleBonusPercent, long totalCredits)
	{
		SafeSet.labelText(bonusAmountLabel, Localize.text("{0}_percent", CommonText.formatNumber(saleBonusPercent)));
		SafeSet.labelText(totalCreditsLabel, CreditsEconomy.convertCredits(totalCredits));
		SafeSet.labelText(priceLabel, creditPackage.priceLocalized);
		SafeSet.labelText(oldPriceLabel, creditPackage.getOriginalLocalizedPrice(saleBonusPercent));

		if (!ExperimentWrapper.BuyPageDrawer.isInExperiment)
		{
			SafeSet.labelText(vipPointsAmountLabel, Localize.text("plus_{0}", CommonText.formatNumber(creditPackage.vipPoints())));
			SafeSet.labelText(vipPointsLabel, Localize.text("plus_{0}_vip_points", "<#FFFFFF>" + CommonText.formatNumber(creditPackage.vipPoints()) + "</color>"));
		}

		if (coinIcon != null && totalCreditsLabel != null)
		{
			totalCreditsLabel.ForceMeshUpdate();	// Force the bounds to be updated immediately after text changes.
			CommonTransform.setX(coinIcon, -(totalCreditsLabel.bounds.size.x * 0.5f + COIN_SPACING));
		}

		//Check our experiment to see if we should show the actual sale percent number or the adjectives
		SafeSet.gameObjectActive(salePercentLabelParent, ExperimentWrapper.SaleBubbleVisuals.isInExperiment);
		SafeSet.gameObjectActive(adjectiveLabelParent, !ExperimentWrapper.SaleBubbleVisuals.isInExperiment);
	}
	
	// NGUI button callback.
	// Purchase the package associated with this option.
	public virtual void purchasePackage(Dict args = null)
	{
		logPurchase();
		creditPackage.makePurchase(bonusPercentage, collectablePack:collectablePackName, purchaseType:sale.featureData.type);	// Will close the dialog if purchase is successful.
	}

	protected virtual void logPurchase()
	{
		string offerName = string.Format("offer_{0}", (index+1));	// Stats are 1-index, packages are 0-indexed.
		StatsManager.Instance.LogCount("dialog", sale.kingdom, "", "", offerName, "click");		
	}

	private void collectablesIconLoadedSuccess(string assetPath, Object obj, Dict data = null)
	{
		if (this == null)
		{
			return;
		}
		
		if (collectablesPackParent != null && Dialog.isSpecifiedDialogShowing("popcorn_sale"))
		{
			GameObject colIcon = NGUITools.AddChild(collectablesPackParent, obj as GameObject);
			if (colIcon != null)
			{
				CollectablesBuyPageIcon icon = colIcon.GetComponent<CollectablesBuyPageIcon>();

				if (icon != null)
				{
					CollectablePackData buyPagePack = Collectables.Instance.findPack(collectablePackName);
					if (buyPagePack != null && buyPagePack.constraints != null && buyPagePack.constraints.Length > 0)
					{
						icon.init(buyPagePack.constraints[0], "free!");
					}
				}
			}
		}
	}

	private void collectablesIconLoadedFailed(string assetPath, Dict data = null)
	{
		Debug.LogError("Failed to load the collectable popcorn object");
	}
}
