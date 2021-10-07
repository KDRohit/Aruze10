using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Class: CreditPackage
Author: mchristensencalvin@zynga.com
Description: Wrapper class to seperate the logic of STUDAction.STUDCreditPackage into a standalone
	class to handle reading the package data from whatever source we want to be using.
*/
public class CreditPackage
{
	public enum CreditEvent
	{
		NOTHING,
		MORE_CARDS,
		MORE_RARE_CARDS
	}

	public PurchasablePackage purchasePackage;
	public long baseCoins = 0;
	public int bonus = 0;
	public int saleMultiplier = 1;
	public int saleBonusPercent = 0;
	public bool isJackpotEligible = false;
	public bool isBestValue = false;
	public bool isMostPopular = false;
	public string perkPackage = "";
	public string collectableDropKeyName = "";
	public bool qualifiesForCardPack = false;
	public CreditEvent activeEvent;
	public string eventText;

	public CreditPackage(STUDAction.STUDCreditPackage studPackage)
	{
		this.purchasePackage = studPackage.package;
		this.baseCoins = studPackage.baseCoins;
		this.bonus = studPackage.bonus;
		this.saleMultiplier = studPackage.saleMultiplier;
		this.saleBonusPercent = studPackage.saleBonusPercent;
		this.isJackpotEligible = studPackage.isJackpotEligible;
		this.isBestValue = studPackage.isBestValue;
		this.isMostPopular = studPackage.isMostPopular;
		this.perkPackage = studPackage.perkPackage;
		this.qualifiesForCardPack = false; //not available via stud

	}

	public CreditPackage(string packageName, PurchaseExperiment data, CreditEvent cardEvent = CreditEvent.NOTHING, string eventStringData = "")
	{
		string creditPackageName = data.getCreditPackageName(packageName);
		bonus = data.getPackageValue(packageName,"_bonus", 0);
		baseCoins = data.getPackageValue(packageName, "_base_coins", 0L);
		saleMultiplier = data.getPackageValue(packageName, "_sale_multiplier", 1);
		saleBonusPercent = data.getPackageValue(packageName, "_sale_bonus_pct", 0);
		isJackpotEligible = ExperimentWrapper.BuyPageProgressive.isInExperiment && data.getPackageValue(packageName, "_qualifies_for_buypage_pjp", false);
		isBestValue = data.getPackageValue(packageName, "_best_value", false);
		isMostPopular = data.getPackageValue(packageName, "_most_popular", false);
		perkPackage = data.getPackageValue(packageName, "_buff_key", "");
		collectableDropKeyName = data.getPackageValue(packageName, "_collectible_pack", "");
		qualifiesForCardPack = data.getPackageValue(packageName, "_qualifies_for_collectible_pack", false);
		activeEvent = cardEvent;
		eventText = eventStringData;

		if (collectableDropKeyName == "nothing")
		{
			collectableDropKeyName = "";
		}
		purchasePackage = PurchasablePackage.find(creditPackageName);
		if (purchasePackage == null)
		{
			Debug.LogErrorFormat("CreditPackage.cs -- constructor -- failed to find a package for name: {0}", creditPackageName);
		}
	}

	public int getSaleBonus(bool isBuyPage = false)
	{
		if (PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_BUY_PAGE_KEY))
		{
			if (isBuyPage && PurchaseFeatureData.isActiveFromPowerup)
			{
				return BuyPageBonusPowerup.salePercent;
			}
			return saleBonusPercent + BuyPageBonusPowerup.salePercent;
		}

		return saleBonusPercent;
	}

	public CreditPackage(PurchasablePackage purchasePackage, int bonus, bool isJackpotEligible)
	{
		this.purchasePackage = purchasePackage;
		this.bonus = bonus;
		this.isJackpotEligible = isJackpotEligible;
	}

	public override string ToString()
	{
		return string.Format(
			"CreditPackage:[package_key:{0},bonus_percentage:{1},sale_multiplier:{2},sale_bonus_pct:{3},base_coins:{4},is_jackpot:{5},is_best_value:{6},is_most_popular:{7},buff_key:{8}]",
			purchasePackage.keyName.ToString(), bonus.ToString(), saleMultiplier.ToString(),
			saleBonusPercent.ToString(), baseCoins.ToString(),
			isJackpotEligible.ToString(), isBestValue.ToString(), isMostPopular.ToString(), perkPackage.ToString());
	}
}
