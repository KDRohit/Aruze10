using Com.HitItRich.Feature.BundleSale;
using UnityEngine;
using TMPro;


public class BuyCoinButtonManager : MonoBehaviour
{
	public GameObject basicBuyButton;
	public GameObject basicBuyButtonSpecial;

	public GameObject buyPageSaleButton;
	public GameObject buyPageSaleButtonSpecial;

	public TextMeshPro multiplierText;
	public TextMeshPro specialMultiplierText;

	public TextMeshPro buyPageTimerText;
	public TextMeshPro specialBuyPageTimerText;

	public GameObject splitButtons;
	public GameObject splitButtonsSpecial;
	public GameObject buttonEffects;
	public TextMeshPro timerTextDeal;
	public TextMeshPro timerTextDealSpecial;

	public OverlayTopHIRv2 overlay;

	private GameTimerRange buyButtonTimer;

	public int forceSaleMultiplierVal;
	public bool forceSplitDeal;
	public bool reset;

	public ButtonHandler dealButtonHandler;
	public ButtonHandler dealSpecialButtonHandler;
	public ButtonHandler splitBuyButtonHandler;
	public ButtonHandler splitBuyButtonSpecialHandler;
	public ButtonHandler buyButtonHandler;
	public ButtonHandler saleButtonHandler;
	public ButtonHandler saleSpecialButtonHandler;

	private ButtonSaleType saleType;
	private int currentSaleMultiplyer;

	private const int DEBUG_TIMER_DURATION = 30000;
	private const float DEFAULT_SALE_TEXT_X_VALUE = 105;
	private const float DEFAULT_SALE_TEXT_Y_VALUE = 23;
	private const int SALE_PERCENTAGE_DENOMINATOR = 100;

#if UNITY_EDITOR
	void Update()
	{
		if (forceSaleMultiplierVal > 1 || forceSplitDeal || reset)
		{
			setButtonType();
			forceSaleMultiplierVal = 0;
			forceSplitDeal = false;
			reset = false;
		}
	}
#endif

	private enum BuyCoinButtonType
	{
		BASIC = 0,
		MULTIPLIER,
		SPLIT
	}

	/*
	Special sales include Starter Pack, Popcorn Sales, Happy Hour, Payer Reactivation, VIP Sale, RDF event, Wheel Deal. 
	Scenario when Starter Pack sale AND another sale is active - show starter pack, because Starter Pack is the better deal
	*/
	private enum ButtonSaleType
	{
		NONE = 0,
		STARTER_PACK,
		POPCORN,
		HAPPY_HOUR,
		PAYER_REACTIVATE,
		VIP,
		WHEEL_DEAL,
		FLASH_SALE,
		STREAK_SALE,
		BUNDLE_SALE
	}

	void Awake()
	{
		dealButtonHandler.registerEventDelegate(dealButtonClicked);
		if (dealSpecialButtonHandler != null)
		{
			dealSpecialButtonHandler.registerEventDelegate(dealButtonClicked);
		}

		buyButtonHandler.registerEventDelegate(buyButtonClicked);
		splitBuyButtonHandler.registerEventDelegate(buyButtonClicked);
		if (splitBuyButtonSpecialHandler != null)
		{
			splitBuyButtonSpecialHandler.registerEventDelegate(buyButtonClicked);
		}

		saleButtonHandler.registerEventDelegate(buyButtonClicked);
		if (saleSpecialButtonHandler)
		{
			saleSpecialButtonHandler.registerEventDelegate(buyButtonClicked);
		}

		if (PowerupsManager.isPowerupsEnabled)
		{
			PowerupsManager.addEventHandler(onPowerupActivated);

			if (PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_BUY_PAGE_KEY))
			{
				onPowerupActivated(PowerupsManager.getActivePowerup(PowerupBase.POWER_UP_BUY_PAGE_KEY));
			}
		}

		if (BundleSaleFeature.instance != null && BundleSaleFeature.instance.canShow())
		{
			onBundleSaleActivated();
		}
	}

	private void onBundleSaleActivated()
	{
		setButtonType();
	}

	private void onPowerupActivated(PowerupBase powerup)
	{
		if (powerup.name == PowerupBase.POWER_UP_BUY_PAGE_KEY)
		{
			setButtonType();
			powerup.runningTimer.registerFunction(onPowerupExpired);
		}
	}

	private void onPowerupExpired(Dict args = null, GameTimerRange originalTimer = null)
	{
		setButtonType();
	}

	private void dealButtonClicked(Dict args)
	{
		if (SlotBaseGame.instance != null && SlotBaseGame.instance.hasAutoSpinsRemaining)
		{
			return;
		}

		StatsManager.Instance.LogCount("top_nav", "buy_coins_button", "deal", "", "", "click");

		switch (saleType)
		{
			case ButtonSaleType.NONE:
				overlay.clickBuyCredits();
				break;
			case ButtonSaleType.STREAK_SALE:
				StreakSaleDialog.showDialog();
				break;
			case ButtonSaleType.POPCORN:
			case ButtonSaleType.STARTER_PACK:
			case ButtonSaleType.HAPPY_HOUR:
			case ButtonSaleType.PAYER_REACTIVATE:
			case ButtonSaleType.VIP:
			case ButtonSaleType.WHEEL_DEAL:
			case ButtonSaleType.FLASH_SALE:
			case ButtonSaleType.BUNDLE_SALE:
				DoSomething.now(getSaleAction(saleType));
				break;
		}
	}

	private string getSaleAction(ButtonSaleType saleType)
	{
		switch (saleType)
		{
			case ButtonSaleType.STARTER_PACK:
				return ("starter_pack");
			case ButtonSaleType.POPCORN:
				return ("popcorn_sale");
			case ButtonSaleType.HAPPY_HOUR:
				return ("happy_hour_sale");
			case ButtonSaleType.PAYER_REACTIVATE:
				return ("payer_reactivation_sale");
			case ButtonSaleType.VIP:
				return ("vip_sale");
			case ButtonSaleType.WHEEL_DEAL:
				return ("wheel_deal");
			case ButtonSaleType.FLASH_SALE:
				return ("flash_sale");
			case ButtonSaleType.STREAK_SALE:
				return ("streak_sale");
			case ButtonSaleType.BUNDLE_SALE:
				return ("bundle_sale");
		}

		return "";
	}

	protected void buyButtonClicked(Dict args)
	{
		string phylum = PurchaseFeatureData.isSaleActive ? "buy_page_sale" : "buy_page";
		StatsManager.Instance.LogCount("top_nav", "buy_coins_button", phylum, "", "", "click");

		overlay.clickBuyCredits();
	}

	private void setupSpecialSaleInfo()
	{
		buyButtonTimer = null;
		saleType = ButtonSaleType.NONE;
		currentSaleMultiplyer = 0;

		if (StreakSaleManager.streakSaleActive)
		{
			saleType = ButtonSaleType.STREAK_SALE;
			buyButtonTimer = null;
		}
		else if (!doSaleSetup(ButtonSaleType.STARTER_PACK, ref buyButtonTimer) && 
		         !doSaleSetup(ButtonSaleType.BUNDLE_SALE, ref buyButtonTimer) &&
		    !doSaleSetup(ButtonSaleType.POPCORN, ref buyButtonTimer) &&
		    !doSaleSetup(ButtonSaleType.HAPPY_HOUR, ref buyButtonTimer) &&
		    !doSaleSetup(ButtonSaleType.PAYER_REACTIVATE, ref buyButtonTimer) &&
		    !doSaleSetup(ButtonSaleType.VIP, ref buyButtonTimer) &&
		    !doSaleSetup(ButtonSaleType.WHEEL_DEAL, ref buyButtonTimer) &&
		    FlashSaleManager.flashSaleIsActive)
		{
			saleType = ButtonSaleType.FLASH_SALE;
			validateSaleType();
		}
	}

	private GameTimerRange getActiveSaleTimerByType(ButtonSaleType saleType)
	{
		switch (saleType)
		{
			case ButtonSaleType.VIP:
				return getActiveVIPSaleSaleTimer();
			case ButtonSaleType.POPCORN:
				return getActivePopcornSaleTimer();
			case ButtonSaleType.HAPPY_HOUR:
				return getActiveHappyHourSaleTimer();
			case ButtonSaleType.WHEEL_DEAL:
				return getActiveWheelDealTimer();
			case ButtonSaleType.STARTER_PACK:
				return getActiveStarterSaleTimer();
			case ButtonSaleType.PAYER_REACTIVATE:
				return getActivePayerActivationSaleTimer();
			case ButtonSaleType.BUNDLE_SALE:
				return getActiveBundleSaleTimer();
			
			default:
				return null;
		}
	}

	private bool doSaleSetup(ButtonSaleType buttonSaleType, ref GameTimerRange timer)
	{	
		GameTimerRange saleTimer = getActiveSaleTimerByType(buttonSaleType);
		if (saleTimer != null)
		{
			timer = saleTimer;
			saleType = buttonSaleType;
			validateSaleType();
			return true;
		}
		return false;
	}

	private void validateSaleType()
	{
		string actionId = getSaleAction(saleType);

		bool canSurface = actionId.Length > 0 && DoSomething.getIsValidToSurface(actionId);

		//Debug.Log("checking " + actionId + "for sale type of " + saleType + " is valid " + canSurface);

		switch (saleType)
		{
			case ButtonSaleType.STARTER_PACK:
				// do something for startperback depends on art type so we have to just check the dialog
				canSurface = StarterDialog.isActive;
				break;
		}

		if (!canSurface)
		{
			saleType = ButtonSaleType.NONE;
			buyButtonTimer = null;
		}
	}

	private void postPurchaseCampaignCallback()
	{
		setButtonType();
	}

	public void setButtonType()
	{
		basicBuyButton.SetActive(false);
		basicBuyButtonSpecial.SetActive(false);
		multiplierText.gameObject.SetActive(false);
		if (specialMultiplierText != null)
		{
			specialMultiplierText.gameObject.SetActive(false);
		}
		buyPageSaleButton.SetActive(false);
		buyPageSaleButtonSpecial.SetActive(false);
		splitButtons.SetActive(false);
		splitButtonsSpecial.SetActive(false);
		ObjectSwapper swapper = splitButtons.GetComponent<ObjectSwapper>();
		if (swapper != null)
		{
			swapper.setState("default");
		}

		bool isSaleActive = PurchaseFeatureData.isSaleActive;    //  buy page sale

		setupSpecialSaleInfo();

		SafeSet.gameObjectActive(buttonEffects, PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_BUY_PAGE_KEY));
		PostPurchaseChallengeCampaign campaign = PostPurchaseChallengeCampaign.getActivePostPurchaseChallengeCampaign();



		if (swapper != null && campaign != null && (campaign.isEarlyEndActive || (!campaign.isLocked && campaign.runningTimeRemaining > 0)))
		{
			splitButtons.SetActive(true);
			swapper.setState("post_purchase_challenge");
			campaign.postPurchaseChallengeEnded += postPurchaseCampaignCallback;
		}
		else if (buyButtonTimer != null) // we have special sale going, so do split button look
		{
			if (PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_BUY_PAGE_KEY))
			{
				splitButtonsSpecial.SetActive(true);
				buyButtonTimer.registerLabel(timerTextDealSpecial);
				buyButtonTimer.registerFunction(onTimerExpire);
			}
			else
			{
				splitButtons.SetActive(true);
				buyButtonTimer.registerLabel(timerTextDeal);
				buyButtonTimer.registerFunction(onTimerExpire);
			}
		}
		else if (StreakSaleManager.streakSaleActive)
		{
			splitButtons.SetActive(true);
			buyButtonTimer = StreakSaleManager.endTimer;
			buyButtonTimer.registerLabel(timerTextDeal);
		}
		else if (isSaleActive || forceSaleMultiplierVal > 1)
		{
			// buy page sale is happening
			int saleMultiplier = ExperimentWrapper.FirstPurchaseOffer.isInExperiment ? ExperimentWrapper.FirstPurchaseOffer.bestSalePercent : PurchaseFeatureData.findBuyCreditsSalePercentage();
			int origSaleMultipler = saleMultiplier;
			bool displayAsPercent = false;

			if (saleMultiplier >= 100)
			{
				saleMultiplier = (saleMultiplier / SALE_PERCENTAGE_DENOMINATOR) + 1;
			}
			else
			{
				displayAsPercent = true;
			}

			if (PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_BUY_PAGE_KEY))
			{
				origSaleMultipler += BuyPageBonusPowerup.SALE_PERCENT;

				if (!displayAsPercent)
				{
					saleMultiplier += BuyPageBonusPowerup.SALE_PERCENT / SALE_PERCENTAGE_DENOMINATOR;
				}
			}

			buyPageSaleButton.SetActive(true);
			basicBuyButton.SetActive(false);
			basicBuyButtonSpecial.SetActive(false);
			buyPageSaleButtonSpecial.SetActive(false);

			if (PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_BUY_PAGE_KEY))
			{
				buyPageSaleButton.SetActive(false);
				buyPageSaleButtonSpecial.SetActive(true);
			}

			if (forceSaleMultiplierVal > 1)
			{
				buyButtonTimer = new GameTimerRange(GameTimer.currentTime, GameTimer.currentTime + DEBUG_TIMER_DURATION);
			}
			else
			{
				PurchaseFeatureData featureData = PurchaseFeatureData.BuyPage;

				//Check for the powerup timer first 
				if ((buyButtonTimer == null || buyButtonTimer.isExpired) && PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_BUY_PAGE_KEY))
				{
					buyButtonTimer = PowerupsManager.getActivePowerup(PowerupBase.POWER_UP_BUY_PAGE_KEY).runningTimer;
				}
				else if (featureData != null)
				{
					buyButtonTimer = featureData.timerRange;
				}

			}

			if (buyButtonTimer != null && buyButtonTimer.timeRemaining > 0)
			{
				buyPageTimerText.gameObject.SetActive(true);
				specialBuyPageTimerText.gameObject.SetActive(true);
				buyButtonTimer.registerLabel(buyPageTimerText);
				buyButtonTimer.registerLabel(specialBuyPageTimerText);
			}
			else //For some sales like First Purchase Offer, there is no timer
			{
				buyPageTimerText.gameObject.SetActive(false);
				specialBuyPageTimerText.gameObject.SetActive(false);
			}

			if (forceSaleMultiplierVal > 1)
			{
				saleMultiplier = forceSaleMultiplierVal;
			}

			if (!displayAsPercent)
			{
				multiplierText.text = saleMultiplier + "X";
			}
			else
			{
				multiplierText.text = origSaleMultipler + "%";
			}

			multiplierText.gameObject.SetActive(true);

			if (specialMultiplierText != null)
			{
				specialMultiplierText.gameObject.SetActive(true);
				specialMultiplierText.text = multiplierText.text;
			}
		}
		else if (FlashSaleManager.flashSaleIsReadyToPop)
		{
			//Flash sale is the lowest priority sale. We're using this if-else chain to prevent showing the flash sale if any other sales are using the buyCoinsButton.

			clearBuyButtonTimerLabels();

			splitButtons.SetActive(true);

			FlashSaleManager.startSale();
			setupSpecialSaleInfo(); //Make sure saleType gets set to flash sale. Without this extra call, the wrong dialog will appear on clicking the deal button after another sale ends and the flash sale begins in the same session.
		}
		else
		{
			basicBuyButton.SetActive(true);
		}

	}

	public void clearBuyButtonTimerLabels()
	{
		if (buyButtonTimer != null)
		{
			buyButtonTimer.clearLabels();
		}
	}

	private void onTimerExpire(Dict args = null, GameTimerRange sender = null)
	{
		setButtonType();
	}

	private GameTimerRange safeGetTimerRange(PurchaseFeatureData featureData)
	{
		if (featureData != null)
		{
			GameTimerRange range = featureData.timerRange;
			if (range != null)
			{
				if (range.isActive)
				{
					return (range);
				}
			}
		}
		return null;
	}

	private GameTimerRange getActivePayerActivationSaleTimer()
	{
		return (safeGetTimerRange(PurchaseFeatureData.PayerReactivationSale));
	}

	private GameTimerRange getActiveVIPSaleSaleTimer()
	{
		return (safeGetTimerRange(PurchaseFeatureData.VipSale));
	}	

	private GameTimerRange getActiveHappyHourSaleTimer()
	{
		return (safeGetTimerRange(PurchaseFeatureData.HappyHourSale));
	}	

	private GameTimerRange getActivePopcornSaleTimer()
	{
		PowerupBase powerup = PowerupsManager.getActivePowerup(PowerupBase.POWER_UP_BUY_PAGE_KEY);
		if (powerup != null)
		{
			return powerup.runningTimer;
		}
		
		return (safeGetTimerRange(PurchaseFeatureData.PopcornSale));
	}

	private GameTimerRange getActiveStarterSaleTimer()
	{
		return StarterDialog.isActive ? StarterDialog.saleTimer : null;
	}

	private GameTimerRange getActiveWheelDealTimer()
	{
		if (LuckyDealDialog.eventTimer != null && !LuckyDealDialog.eventTimer.isExpired)
		{
			return LuckyDealDialog.eventTimer;
		}

		return null;
	}

	private GameTimerRange getActiveBundleSaleTimer()
	{
		if (BundleSaleFeature.instance != null && BundleSaleFeature.instance.canShow() && BundleSaleFeature.instance.isTimerVisible && BundleSaleFeature.instance.getSaleTimer() != null)
		{
			return BundleSaleFeature.instance.getSaleTimer();
		}

		return null;
	}

	public void updateTimerTextDeal(string newText)
	{
		timerTextDeal.text = newText;
	}

	public void OnDestroy()
	{
		PowerupsManager.removeEventHandler(onPowerupActivated);
	}
}
