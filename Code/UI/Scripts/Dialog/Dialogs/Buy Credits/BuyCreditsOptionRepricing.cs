using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
/**
Controls one of the options for buying credits.
This dialog also supports STUD-driven credits packages.
*/

public class BuyCreditsOptionRepricing : BuyCreditsOptionNewHIR
{

	//Transform changes if this option should be displaying the values for a sale package
	private const float SALE_BASE_CREDITS_WIDTH = 368f;
	private const float SALE_BASE_CREDITS_HEIGHT = 72f;
	private Vector3 SALE_BASE_CREDITS_POSITION = new Vector3(-618f, 2.3f, 0f);

	private const float SALE_VIP_POINTS_LABEL_WIDTH = 316f;
	private const float SALE_VIP_POINTS_LABEL_HEIGHT = 50f;
	private Vector3 SALE_VIP_POINTS_LABEL_POSITION = new Vector3(-75.7f, -31f, -10f);

	private const int COLLECTABLES_TOTAL_CREDITS_WIDTH = 375; // Base size 490

	protected override void setBaseCreditsLabel()
	{
		string credits = CreditsEconomy.convertCredits(creditPackage.purchasePackage.totalCredits(creditPackage.bonus, true));
		baseCreditsLabel.text = credits;

		if (postPurchaseBaseCreditsLabel != null)
		{
			postPurchaseBaseCreditsLabel.text = credits;
		}

		if (baseSaleCredits != null)
		{
			baseSaleCredits.text = baseCreditsLabel.text;
		}
	}

	public override void setIndex(int newIndex, BuyCreditsDialog dialog)
	{
		base.setIndex(newIndex, dialog);

		if (perksPanel == null && Collectables.isActive() && creditPackage != null && !CommonText.IsNullOrWhiteSpace(creditPackage.collectableDropKeyName))
		{
			// This should add the surfacing to appropriate things.
			CollectablePackData buyPagePack = Collectables.Instance.findPack(creditPackage.collectableDropKeyName);
			
			if (buyPagePack != null && buyPagePack.constraints != null && buyPagePack.constraints.Length > 0)
			{
				string locKey = creditPackage.activeEvent == CreditPackage.CreditEvent.NOTHING ? "free!" : "bonus!";
				AssetBundleManager.load(this,
					Collectables.BUY_PAGE_SURFACING_PATH,
					onLoadBuyPageCollectablesObject,
					onBuyPageCollectablesObjectLoadFailure,
					Dict.create(
						D.DATA,buyPagePack.constraints[0],
						D.KEY, locKey)
				);
			}
		}
	}

	private void onLoadBuyPageCollectablesObject(string assetPath, System.Object obj, Dict data = null)
	{
		if (this != null)
		{
			GameObject iconObject = NGUITools.AddChild(collectablesAnchor, obj as GameObject);
			PackConstraint constraintData = (PackConstraint) data.getWithDefault(D.DATA, null);
			string headerTextKey = (string)data.getWithDefault(D.KEY, "");
			if (iconObject != null && constraintData != null)
			{
				CollectablesBuyPageIcon collectableIcon = iconObject.GetComponent<CollectablesBuyPageIcon>();
				if (collectableIcon != null)
				{
					bool isCardEventActive = false;

					if (featureData != null &&
						featureData.cardEvents != null &&
						featureData.cardEvents.Count > this.index)
					{
						isCardEventActive = (featureData.cardEvents[this.index].Key != CreditPackage.CreditEvent.NOTHING);
					}
					collectableIcon.init(constraintData, headerTextKey, isCardEventActive);
				}
			}
		}
	}


	private void onBuyPageCollectablesObjectLoadFailure(string assetPath, Dict data = null)
	{
		Debug.LogError("Failed to load surfacing item for buy page");
	}


	protected override void setSaleLabels(long nonSaleTotalAmount)
	{
		base.setSaleLabels(nonSaleTotalAmount);
		//baseCreditsLabel.rectTransform.localPosition = SALE_BASE_CREDITS_POSITION;
		//baseCreditsLabel.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, SALE_BASE_CREDITS_WIDTH);
		//baseCreditsLabel.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, SALE_BASE_CREDITS_HEIGHT);
		//vipBonus.rectTransform.localPosition = SALE_VIP_POINTS_LABEL_POSITION;
		//vipBonus.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, SALE_VIP_POINTS_LABEL_WIDTH);
		//vipBonus.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, SALE_VIP_POINTS_LABEL_HEIGHT);
	}

	protected override void setNonSaleLabels(PurchaseFeatureData featureData)
	{
		base.setNonSaleLabels(featureData);
		SafeSet.gameObjectActive(equalsSizer, false);
		SafeSet.gameObjectActive(saleBonusParent, false);
		SafeSet.gameObjectActive(baseSaleCredits.gameObject, false);
		SafeSet.gameObjectActive(totalCredits.gameObject, false);
		SafeSet.gameObjectActive(baseCreditsLabel.gameObject, true);


		if (vipBonus != null)
		{
			SafeSet.gameObjectActive(vipBonus.gameObject, true);
		}

		if (vipSaleBonus != null)
		{
			SafeSet.gameObjectActive(vipSaleBonus.gameObject, false);
		}
	}
}
