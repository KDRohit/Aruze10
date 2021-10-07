using UnityEngine;
using System;
using System.Collections;
using TMPro;

public class LobbyCarouselCardPane : MonoBehaviour
{
	public Renderer image;
	public Renderer imageGlow;
	public TextMeshPro timerPrefixLabel;
	public TextMeshPro timerLabel;
	public TextMeshPro buttonLabel;
	public GameObject timerContainer;
	public GameObject challengesNotepad;
	public GameObject imageParent;

	// beginner special fields
	public TextMeshPro coinLabel;
	public TextMeshPro strikeLabel;
	public TextMeshPro priceLabel;
	public TextMeshPro shadowLabel;
	public GameObject strikeObject; // contains field, and strike asset

	// "unique" feature card jackpot object and label
	public GameObject jackpot;
	public TextMeshPro jackpotLabel;

	public void setJackpotAmount(long amount)
	{
		SafeSet.gameObjectActive(jackpot, true);
		if (jackpotLabel != null)
		{
			jackpotLabel.text = CreditsEconomy.convertCredits(amount, true);
		}
	}

	public void setLifeCycleSales()
	{
		string itemName = ExperimentWrapper.LifecycleSales.creditPackageName;
		
		if (!string.IsNullOrEmpty(itemName))
		{
			PurchasablePackage creditPackage = PurchasablePackage.find(itemName);
			if (creditPackage != null)
			{
				int packagePricePoint = Convert.ToInt32(ExperimentWrapper.LifecycleSales.strikethroughAmount);
				PurchasablePackage oldPricePackage = PurchasablePackage.findByPriceTier(packagePricePoint, true);
				if (oldPricePackage != null)
				{
					strikeLabel.text = Localize.textUpper("was_{0}_no_break", oldPricePackage.getLocalizedPrice());
				}
				
				priceLabel.text = Localize.textUpper("now_{0}_no_break", creditPackage.getLocalizedPrice());
				
				if (oldPricePackage == null || strikeLabel.text == "" || priceLabel.text == strikeLabel.text)
				{
					strikeObject.SetActive(false);
				}
				
				coinLabel.text = CreditsEconomy.convertCredits(creditPackage.totalCredits(ExperimentWrapper.LifecycleSales.bonusPercent));

				if (shadowLabel != null)
				{
					shadowLabel.text = coinLabel.text;
				}
			}
		}
	}

	public void setBeginnerSpecialFields()
	{
		string itemName = ExperimentWrapper.StarterPackEos.creditPackageName;

		if (!string.IsNullOrEmpty(itemName))
		{
			PurchasablePackage creditPackage = PurchasablePackage.find(itemName);
			if (creditPackage != null)
			{
				int packagePricePoint = Convert.ToInt32(ExperimentWrapper.StarterPackEos.strikethroughAmount);
				PurchasablePackage oldPricePackage = PurchasablePackage.findByPriceTier(packagePricePoint, true);
				if (oldPricePackage != null)
				{
					strikeLabel.text = Localize.textUpper("was_{0}_no_break", oldPricePackage.getLocalizedPrice());
				}

				priceLabel.text = Localize.textUpper("now_{0}_no_break", creditPackage.getLocalizedPrice());

				if (oldPricePackage == null || strikeLabel.text == "" || priceLabel.text == strikeLabel.text)
				{
					strikeObject.SetActive(false);
				}

				coinLabel.text = CreditsEconomy.convertCredits(creditPackage.totalCredits(ExperimentWrapper.StarterPackEos.bonusPercent));

				if (shadowLabel != null)
				{
					shadowLabel.text = coinLabel.text;
				}
			}
		}
	}
}
