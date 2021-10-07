using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class VIPPostPurchaseDialog : DialogBase 
{
	// MUST BE IN TOP TO BOTTOM ORDER WHEN LINKED ON PREFAB
	// Order:
	//extra coins on purchases
	//sale bonus coins
	//vip bonus
	//total bonus
	public TextMeshPro[] bonusText;
	public TextMeshPro totalCoinsPurchasedTop;
	public TextMeshPro descriptionText;

	public Animator titleAnimator;

	public VIPNewIcon boostedIcon;
	public ButtonHandler closeButton;
	public GameObject saleLine;
	public GameObject vipBonusLine;

	// animation state consts
	private const string TITLE_ANIMATION = "PostPurchaseDialogLoop";

	// How fast all our stuff rolls up.
	private const float ROLLUP_TIME = 1.0f;
	private long creditsBeforePurchase;
	private bool isSaleActive = false;

	private static int noSaleRestingPosition = 20; // y position for the VIP bonus line when there are no sale coins
//	Dict args = Dict.create(
//		D.BONUS_CREDITS, (long)vipCredits,
//		D.TOTAL_CREDITS, (long)creditsAdded,
//		D.PACKAGE_KEY, packageKey,
//		D.VIP_POINTS, vipAdded,
//		D.DATA, data,
//		D.BASE_CREDITS, baseCredits,
//		D.BONUS_PERCENT, bonusPercent,
//		D.SALE_BONUS_PERCENT, saleBonusPercent,
//		D.VIP_BONUS_PERCENT, vipBonusPercent
//	);
	public override void init()
	{
		closeButton.registerEventDelegate(onClickClose);

		int vipPoints = (int)dialogArgs.getWithDefault(D.VIP_POINTS, 0);

		int bonusPercent = (int)dialogArgs.getWithDefault(D.BONUS_PERCENT, 0);
		int saleBonusPercent = (int)dialogArgs.getWithDefault(D.SALE_BONUS_PERCENT, 0);
		int vipBonusPercent = (int)dialogArgs.getWithDefault(D.VIP_BONUS_PERCENT, 0);
		// Convert to decimals so we don't have rounding issues
		System.Decimal bonusPercentage = (System.Decimal)bonusPercent / 100m;
		System.Decimal saleBonusPercentage = (System.Decimal)saleBonusPercent / 100m;
		System.Decimal vipBonusPercentage = (System.Decimal)vipBonusPercent / 100m;
		System.Decimal oldVIPBonusPercentage = (System.Decimal)VIPLevel.find(SlotsPlayer.instance.vipNewLevel).purchaseBonusPct / 100m;
		long workingTotal = (long)dialogArgs.getWithDefault(D.BASE_CREDITS, 0);
		long oldTotal = workingTotal;

		// Convert back and round correctly. 
		System.Decimal bonusCredits = System.Math.Round(bonusPercentage * workingTotal, System.MidpointRounding.AwayFromZero);
		workingTotal += System.Decimal.ToInt64(bonusCredits);
		oldTotal += System.Decimal.ToInt64(bonusCredits);

		// Used for comparison

		System.Decimal oldVIPBonus = System.Math.Round(oldVIPBonusPercentage * oldTotal, System.MidpointRounding.AwayFromZero);
		oldTotal += System.Decimal.ToInt64(oldVIPBonus);

		// What we actually got from VIP credits
		System.Decimal vipBonusCredits = System.Math.Round(vipBonusPercentage * workingTotal, System.MidpointRounding.AwayFromZero);
		workingTotal += System.Decimal.ToInt64(vipBonusCredits);

		System.Decimal oldSaleBonusCredits = System.Math.Round(saleBonusPercentage * oldTotal, System.MidpointRounding.AwayFromZero);
		System.Decimal saleBonusCredits = System.Math.Round(saleBonusPercentage * workingTotal, System.MidpointRounding.AwayFromZero);

		isSaleActive = saleBonusCredits != 0;

		if (!isSaleActive)
		{
			saleLine.SetActive(false);
			CommonTransform.setY(vipBonusLine.transform, 20);
		}

		System.Decimal totalBonusCredits = bonusCredits + saleBonusCredits + vipBonusCredits;
		System.Decimal toalUnboostedBonus = oldTotal + oldSaleBonusCredits;
		                                   
		descriptionText.text = Localize.text("vip_boost_purchase_description", "<color=#00ffffff>" + vipPoints + "</color>");
		boostedIcon.setLevel(VIPLevel.getEventAdjustedLevel());

		titleAnimator.Play(TITLE_ANIMATION);

		totalCoinsPurchasedTop.text = CreditsEconomy.convertCredits(SlotsPlayer.creditAmount);
			
		StartCoroutine(rollupAllBenefits((int)bonusCredits, (int)saleBonusCredits, (int)vipBonusCredits, (int)totalBonusCredits));

		JSON data = null;
		string eventId = "";

		data = dialogArgs.getWithDefault(D.DATA, null) as JSON;

		if (data != null)
		{
			eventId = data.getString("event", "");
		}

		if (eventId != "")
		{
			CreditAction.acceptPurchasedItem(eventId);
		}

		if (ExperimentWrapper.FirstPurchaseOffer.isInExperiment)
		{
			string packageKey = (string)dialogArgs.getWithDefault(D.PACKAGE_KEY, "");
			ExperimentWrapper.FirstPurchaseOffer.didPurchase = true;
			PurchasablePackage purchasedPackage = PurchasablePackage.find(packageKey);
			if (purchasedPackage != null)
			{
				StatsManager.Instance.LogCount(counterName: "dialog", 
					kingdom: "buy_page_v3", 
					phylum:"purchase", 
					klass: "first_purchase_offer",
					family: "First Purchase Offer",
					genus: purchasedPackage.getLocalizedPrice());
			}
		}
	}

	private IEnumerator rollupAllBenefits(int bonusCredits, int saleCredits, int vipCredits, int totalCredits)
	{
		int[] creditAmounts = new int[4]
		{
			bonusCredits,
			saleCredits,
			vipCredits,
			totalCredits
		};

		for (int i = 0; i < bonusText.Length; i++)
		{
			// If there are no sale credits, skip
			if (i == 1 && !isSaleActive)
			{
				i++;
			}

			StartCoroutine(SlotUtils.rollup(0, 
											creditAmounts[i] * CreditsEconomy.economyMultiplier, 
											bonusText[i], 
											playSound:false, 
											specificRollupTime:ROLLUP_TIME, 
											isCredit:false));
			
			yield return new WaitForSeconds(0.5f);
		}
			
		yield return null;
	}

	public void onClickClose(Dict args = null)
	{
		Dialog.close();
	}

	void Update()
	{
		AndroidUtil.checkBackButton(onClickClose);
	}

	public override void close()
	{

	}
}
