using UnityEngine;
using System.Collections;

public class STUDSaleOption : MonoBehaviour 
{	
	public int index; // The package index for this SaleOption from left to right (ascending price order).
	
	public UILabel bonusAmountLabel; // Label representing the bonus percentage. -  To be removed when prefabs are updated.
	public LabelWrapperComponent bonusAmountLabelWrapperComponent; // Label representing the bonus percentage.

	public LabelWrapper bonusAmountLabelWrapper
	{
		get
		{
			if (_bonusAmountLabelWrapper == null)
			{
				if (bonusAmountLabelWrapperComponent != null)
				{
					_bonusAmountLabelWrapper = bonusAmountLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_bonusAmountLabelWrapper = new LabelWrapper(bonusAmountLabel);
				}
			}
			return _bonusAmountLabelWrapper;
		}
	}
	private LabelWrapper _bonusAmountLabelWrapper = null;
	
	public UILabel totalCreditsLabel; // Label representing the total number of credits (after applying bonus). -  To be removed when prefabs are updated.
	public LabelWrapperComponent totalCreditsLabelWrapperComponent; // Label representing the total number of credits (after applying bonus).

	public LabelWrapper totalCreditsLabelWrapper
	{
		get
		{
			if (_totalCreditsLabelWrapper == null)
			{
				if (totalCreditsLabelWrapperComponent != null)
				{
					_totalCreditsLabelWrapper = totalCreditsLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_totalCreditsLabelWrapper = new LabelWrapper(totalCreditsLabel);
				}
			}
			return _totalCreditsLabelWrapper;
		}
	}
	private LabelWrapper _totalCreditsLabelWrapper = null;
	
	public UILabel vipPointsLabel; // Label representing the number of VIP points the player will get after purchasing this package. -  To be removed when prefabs are updated.
	public LabelWrapperComponent vipPointsLabelWrapperComponent; // Label representing the number of VIP points the player will get after purchasing this package.

	public LabelWrapper vipPointsLabelWrapper
	{
		get
		{
			if (_vipPointsLabelWrapper == null)
			{
				if (vipPointsLabelWrapperComponent != null)
				{
					_vipPointsLabelWrapper = vipPointsLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_vipPointsLabelWrapper = new LabelWrapper(vipPointsLabel);
				}
			}
			return _vipPointsLabelWrapper;
		}
	}
	private LabelWrapper _vipPointsLabelWrapper = null;
	
	public UILabel vipPointsAmountLabel; // Label representing the number of VIP points (without the VIP POINTS text.) -  To be removed when prefabs are updated.
	public LabelWrapperComponent vipPointsAmountLabelWrapperComponent; // Label representing the number of VIP points (without the VIP POINTS text.)

	public LabelWrapper vipPointsAmountLabelWrapper
	{
		get
		{
			if (_vipPointsAmountLabelWrapper == null)
			{
				if (vipPointsAmountLabelWrapperComponent != null)
				{
					_vipPointsAmountLabelWrapper = vipPointsAmountLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_vipPointsAmountLabelWrapper = new LabelWrapper(vipPointsAmountLabel);
				}
			}
			return _vipPointsAmountLabelWrapper;
		}
	}
	private LabelWrapper _vipPointsAmountLabelWrapper = null;
	
	public UILabel priceLabel; // Label representing the the price of this package. -  To be removed when prefabs are updated.
	public LabelWrapperComponent priceLabelWrapperComponent; // Label representing the the price of this package.

	public LabelWrapper priceLabelWrapper
	{
		get
		{
			if (_priceLabelWrapper == null)
			{
				if (priceLabelWrapperComponent != null)
				{
					_priceLabelWrapper = priceLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_priceLabelWrapper = new LabelWrapper(priceLabel);
				}
			}
			return _priceLabelWrapper;
		}
	}
	private LabelWrapper _priceLabelWrapper = null;
	
	
	private PurchasablePackage creditPackage;
	private int bonusPercentage = 0;
	private STUDSale sale;
	
	// Sets the package for this option, and updates the UI to represent this information.
	public void setOption(STUDAction.STUDCreditPackage studPackage, STUDSale sale)
	{
		if (studPackage == null || studPackage.package == null)
		{
			// Turn off gameObject so the user cannot purchase a broken item
			gameObject.SetActive(false);
			if (studPackage == null)
			{
				Debug.LogError("studPackage is null, STUD did not create this package");
			}
			else if (studPackage.package == null)
			{
				Debug.LogError("No Purchasable Package could be found");
			}
			else
			{
				Debug.LogError("No item in the economy could be found for " + studPackage.package.keyName);
			}
		}
		else
		{
			this.sale = sale;
			bonusPercentage = studPackage.bonus;
			creditPackage = studPackage.package;
			SafeSet.labelText(bonusAmountLabelWrapper, Localize.text("{0}_percent", bonusPercentage.ToString()));
			SafeSet.labelText(totalCreditsLabelWrapper, CreditsEconomy.convertCredits(creditPackage.totalCredits(bonusPercentage)));
			SafeSet.labelText(vipPointsAmountLabelWrapper, Localize.text("plus_{0}", CommonText.formatNumber(creditPackage.vipPoints())));
			SafeSet.labelText(vipPointsLabelWrapper, Localize.text("plus_{0}_vip_points", CommonText.formatNumber(creditPackage.vipPoints())));
			SafeSet.labelText(priceLabelWrapper, creditPackage.priceLocalized);
		}
	}
	
	// Purchase the package associated with this option.
	public void purchasePackage()
	{
		string offerName = string.Format("offer_{0}", (index+1));	// Stats are 1-index, packages are 0-indexed.
		StatsManager.Instance.LogCount("dialog", sale.kingdom, "", "", offerName, "click");
		creditPackage.makePurchase(bonusPercentage);	// Will close the dialog if purchase is successful.
	}

}

