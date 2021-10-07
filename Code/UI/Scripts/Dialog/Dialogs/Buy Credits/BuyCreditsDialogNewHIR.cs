using UnityEngine;
using System.Collections;
using TMPro;
using System.Collections.Generic;

/*
  Attached to the parent dialog to handle setting up the dialog and handling clicks.
*/

public class BuyCreditsDialogNewHIR : BuyCreditsDialog
{
	public TextMeshPro secureTransactionsLabel;
	public TextMeshPro vipPercentBonusLabel;

	public TextMeshPro saleTitleLabel;

	public MeshRenderer bannerRenderer;

	// Progressive Jackpot Inspector Variables
	public GameObject jackpotDaysParent;
	public TextMeshPro jackpotDaysJackpotLabel;
	public TextMeshPro jackpotDaysTimeRemainingLabel;
	public UIGrid optionsGrid;
	public GameObject optionTemplate;
	[SerializeField] private GameObject drawerOptionTemplate;

	public GameObject creditSweepstakesParent;
	public TextMeshPro creditSweepstakesAmountLabel;
	public TextMeshPro creditSweepstakesTimeLabel;
	public TextMeshPro creditSweepstakesDescLabel;
	
	// First Purchase Offer elements 
	public GameObject firstPurchaseOfferParent;
	public GameObject registeredPlayerParent;
	public GameObject nonRegisteredPlayerParent;
	public FacebookFriendInfo playerInfo;
	
	//Inflation Related Elements
	public GameObject bonusRibbon;
	public GameObject currentInflationIncreaseParent;
	public TextMeshPro currentInflationLabel;
	public GameObject nextInflationIncreaseParent;
	public TextMeshPro nextInflationLabel;
	
	//Elite Elements
	public GameObject eliteParent;

	private bool isOnSale = false;
	private bool loadedBackground = false;
	public bool didPressPurchase = false;
	
	private const float ANIMATION_DELAY = 0.15f;

    // VIP Status event elements
    public VIPIconHandler vipIcon; // Not directly part of the event, but will be manipulated by it.
    public GameObject vipStatusEventParent;
    public TextMeshPro vipStatusEventTimer;

    public SlideController slideController;
	public UIPanel optionsPanel;
	public TextMeshProMasker tmProMasker;
	public UIScrollBar scrollBar;

	public TextMeshPro moreCardsLabel;
	public TextMeshPro moreRareCardsLabel;

    private const int DEFAULT_OPTION_COUNT = 6;
	private const int MAX_OPTION_COUNT = 39;

	private bool headerIsSetup = false;
	private GameTimer textUpdateTimer = null;  // used to update text displaying the timer value, once a second

	private PurchasePerksCycler perksCycler;

	/// Initialization
	public override void init()
	{
		dialogStatName = "buy_page_v5"; // Set the new stat name for this dialog.
		base.init();

		saleTitleLabel.text = currentSaleTitle;
		secureTransactionsLabel.text = Localize.textUpper("secure_transactions_through_{0}", Glb.storeName);

		if (ExperimentWrapper.BuyPageHyperlink.isInExperiment && ExperimentWrapper.BuyPage.hasCardPackDropsConfigured)
		{
			secureTransactionsLabel.text += ExperimentWrapper.BuyPageHyperlink.getLinkForBuyPage();
		}

		// Hide secureTransactionsLabel for DotCom build
		if (Data.webPlatform.IsDotCom)
		{
			secureTransactionsLabel.gameObject.SetActive(false);
		}

		vipIcon.shouldSetToPlayerLevel = false;
		moreCardsLabel.gameObject.SetActive(false);
		moreRareCardsLabel.gameObject.SetActive(false);

	    int maxBonusPercentage = PurchaseFeatureData.findBuyCreditsSalePercentage();

	    createOptionsGrid();
	    initOffers();
		initCoinSweepstakesObjects();
		initJackpotObjects(maxBonusPercentage);
		initBanner();
		initVIPObjects();


		if (ExperimentWrapper.FirstPurchaseOffer.isInExperiment)
		{
			economyTrackingName = "first_purchase_offer";
		}
		else if (maxBonusPercentage > 0)
		{
			economyTrackingName = "buy_page_sale";
		}
		else
		{
			economyTrackingName = "buy_page";
		}

		EosAction.notifyBuypageView();
		MOTDFramework.markMotdSeen(dialogArgs);

		if (PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_BUY_PAGE_KEY))
		{
			PowerupsManager.getActivePowerup(PowerupBase.POWER_UP_BUY_PAGE_KEY).runningTimer.registerFunction(onPowerupExpired);
		}
	}

	/**
	 * if the buy page powerup expires, close this dialog, and reopen it so all the values are updated
	 */
	private void onPowerupExpired(Dict args = null, GameTimerRange originalTimer = null)
	{
		Dialog.close(this);
		showDialog((string) dialogArgs.getWithDefault(D.MOTD_KEY, ""));
	}

	private void initOffers()
	{
		SafeSet.gameObjectActive(firstPurchaseOfferParent, false);

		// First purchase offer has priority over all other sales.
		if (ExperimentWrapper.FirstPurchaseOffer.isInExperiment)
		{
			firstPurchaseOfferParent.SetActive(true);
			isOnSale = true;

			// We don't want the VIP banner in the top left.
			vipBonusParent.SetActive(false);
			vipStatusEventParent.SetActive(false);

			// Check if anonymous
			if (SlotsPlayer.isSocialFriendsEnabled)
			{
				// Show the personalized version.
				registeredPlayerParent.SetActive(true);
				nonRegisteredPlayerParent.SetActive(false);

				if (playerInfo != null && SlotsPlayer.instance != null)
				{
					playerInfo.member = SlotsPlayer.instance.socialMember;
				}
			}
			else
			{
				// Show the default version.
				nonRegisteredPlayerParent.SetActive(true);
				registeredPlayerParent.SetActive(false);
			}
		}
		else
		{
			if (PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_BUY_PAGE_KEY))
			{
				isOnSale = true;
			}

			vipStatusEventParent.SetActive(VIPStatusBoostEvent.isEnabled() && !SlotsPlayer.instance.isMaxVipLevel);
			if (VIPStatusBoostEvent.isEnabled())
			{
				VIPStatusBoostEvent.featureTimer.registerLabel(vipStatusEventTimer);
			}
		}
	}

	private void initBanner()
	{
		downloadedTextureToRenderer(bannerRenderer, 0);

		if (bannerRenderer.material.mainTexture == null)
		{
			bannerRenderer.gameObject.SetActive(false);
		}
		else
		{
			loadedBackground = true;
		}

		if (BuyPageCardEvent.instance.shouldShowHeader)
		{
			switch (BuyPageCardEvent.instance.cardEvent)
			{
				case CreditPackage.CreditEvent.MORE_CARDS:
					setMoreCardsHeader();
					break;
				case CreditPackage.CreditEvent.MORE_RARE_CARDS:
					setMoreRareCardsHeader();
					break;
			}
		}
		else
		{
			turnOffCardEventHeaders();
		}
	}

	private void turnOffCardEventHeaders()
	{
		if (moreRareCardsLabel != null && moreRareCardsLabel.gameObject != null)
		{
			moreRareCardsLabel.gameObject.SetActive(false);
		}

		if (moreCardsLabel != null && moreCardsLabel.gameObject != null)
		{
			moreCardsLabel.gameObject.SetActive(false);
		}		
	}
	
	private void setMoreCardsHeader()
	{
		if (headerIsSetup)
		{
			return;
		}
		if (moreRareCardsLabel != null && moreRareCardsLabel.gameObject != null)
		{
			moreRareCardsLabel.gameObject.SetActive(false);
		}

		if (moreCardsLabel != null && moreCardsLabel.gameObject != null)
		{
			moreCardsLabel.gameObject.SetActive(true);
			moreCardsLabel.text = BuyPageCardEvent.instance.getBuyCreditsHeaderTextMoreCards();
		}
		headerIsSetup = true;
	}

	private void setMoreRareCardsHeader()
	{
		if (headerIsSetup)
		{
			return;
		}

		if (moreCardsLabel != null && moreCardsLabel.gameObject != null)
		{
			moreCardsLabel.gameObject.SetActive(false);
		}

		if (moreRareCardsLabel != null && moreRareCardsLabel.gameObject != null)
		{
			moreRareCardsLabel.gameObject.SetActive(true);
			moreRareCardsLabel.text = BuyPageCardEvent.instance.getBuyCreditsHeaderTextMoreRareCards();
		}
		headerIsSetup = true;
	}

	private void createOptionsGrid()
	{
		int optionCount = Data.liveData.getBool("DISPLAY_ALL_PURCHASE_PACKAGES", false) ? MAX_OPTION_COUNT : DEFAULT_OPTION_COUNT;
		optionsGrid.maxPerLine = optionCount;

		if (ExperimentWrapper.BuyPageDrawer.isInExperiment)
		{
			List<PurchasePerksPanel.PerkType> cyclingPerks = ExperimentWrapper.FirstPurchaseOffer.isInExperiment ? 
				PurchasePerksPanel.getEligiblePerks(ExperimentWrapper.FirstPurchaseOffer.firstPurchaseOffersList,PurchaseFeatureData.BuyPage.creditPackages) : 
				PurchasePerksPanel.getEligiblePerks(PurchaseFeatureData.BuyPage);
			
			perksCycler = new PurchasePerksCycler(ExperimentWrapper.BuyPageDrawer.delays, Mathf.Min(ExperimentWrapper.BuyPageDrawer.maxItemsToRotate, cyclingPerks.Count));
			for (int i = 0; i < optionCount; i++)
			{
				GameObject go = NGUITools.AddChild(optionsGrid.gameObject, drawerOptionTemplate);
				go.name = string.Format("{0} Buy Option", options.Length - i);
				options[i] = go.GetComponent<BuyCreditsOptionNewHIR>();// as BuyCreditsOptionVIP;
				options[i].setIndex(i, this, cyclingPerks, perksCycler);
				tmProMasker.addObjectArrayToList(options[i].getTextMeshPros());
			}
			
			perksCycler.startCycling();
		}
		else
		{
			for (int i = 0; i < optionCount; i++)
			{
				GameObject go = NGUITools.AddChild(optionsGrid.gameObject, optionTemplate);
				go.name = string.Format("{0} Buy Option", options.Length - i);
				options[i] = go.GetComponent<BuyCreditsOptionNewHIR>();// as BuyCreditsOptionVIP;
				options[i].setIndex(i, this);
				tmProMasker.addObjectArrayToList(options[i].getTextMeshPros());
			}
		}


		if (optionCount <= DEFAULT_OPTION_COUNT)
		{
			slideController.enabled = false;
			slideController.topBound = 0;
			slideController.bottomBound = 0;
			scrollBar.enabled = false;
			scrollBar.gameObject.SetActive(false);
			optionsPanel.clipping = UIDrawCall.Clipping.None;
		}
		else
		{
			// Make sure alpha clipping is on if we are using the controller.
			optionsPanel.clipping = UIDrawCall.Clipping.HardClip;
		}
		// Don't need the template anymore.
		Destroy(optionTemplate);
		Destroy(drawerOptionTemplate);
		optionsGrid.repositionNow = true;
	}

	private bool initCoinSweepstakesObjects()
	{
		bool creditSweepstakesActive = !ExperimentWrapper.FirstPurchaseOffer.isInExperiment &&
			                               !PurchaseFeatureData.isSaleActive &&
			                               !shouldShowCollectablesBanner() &&
			                               CreditSweepstakes.isActive;
		creditSweepstakesParent.SetActive(creditSweepstakesActive);
		if (creditSweepstakesActive)
		{
			textUpdateTimer = new GameTimer(1.0f);  // update once/sec
			creditSweepstakesAmountLabel.text = CreditsEconomy.convertCredits(CreditSweepstakes.payout, true);
			updateTimerLabelText();
			creditSweepstakesDescLabel.text = Localize.text("coinsw_buy_page_subtext_{0}", CommonText.formatNumber(CreditSweepstakes.winnerCount));
		}
		return creditSweepstakesActive;
	}

	private void updateTimerLabelText()
	{
		creditSweepstakesTimeLabel.text = CreditSweepstakes.timeRange.timeRemainingFormatted;
	}


	private bool initJackpotObjects(int maxBonusPercentage)
	{
		bool isJackpotDaysProgressive = false;
		if (maxBonusPercentage > 0)
		{
			// Do 2x sale stuff.
			hasSeenSale = true;
			isOnSale = true;
			// Widen the options and move them up.
			optionsGrid.Reposition();
			// Turn off the progressive parent.
			jackpotDaysParent.SetActive(false);
		}
		else
		{
			// Only show progressive if there is a jackpot, and there is no sale.
			int progressiveJackpotTimeRemaining = SlotsPlayer.instance.jackpotDaysTimeRemaining.timeRemaining;
			bool isProgressive = ProgressiveJackpot.buyCreditsJackpot != null;
			bool hasJackpotOption = false;
			for (int i = 0; i < options.Length; i++)
			{
				// we can have null options now for debugging
				if (options[i] == null) { break; }

				BuyCreditsOptionNewHIR option = options[i];
				if (option.setupJackpotIcon(isProgressive))
				{
					hasJackpotOption = true;
				}
			}

			// Only turn on the jackpot overlay information if there is a valid option for it.
			isProgressive = isProgressive && hasJackpotOption;
			isJackpotDaysProgressive = isProgressive && ExperimentWrapper.BuyPageProgressive.jackpotDaysIsActive;
			if (isProgressive)
			{
				if (progressiveJackpotTimeRemaining > 0)
				{
					SlotsPlayer.instance.jackpotDaysTimeRemaining.registerLabel(jackpotDaysTimeRemainingLabel);
					ProgressiveJackpot.buyCreditsJackpot.registerLabel(jackpotDaysJackpotLabel);
					StatsManager.Instance.LogCount("dialog", "buy_page_v3", "", "jackpot_days", "view", "view");
				}
			}

			jackpotDaysParent.SetActive(isJackpotDaysProgressive);
		}

		return isJackpotDaysProgressive;
	}

	private void initVIPObjects()
	{
		vipIcon.setLevel(SlotsPlayer.instance.vipNewLevel, "coin_purchases");
		VIPLevel level = VIPLevel.find(SlotsPlayer.instance.vipNewLevel, "coin_purchases");
		
		//If the VIP boost powerup is active,show the vip boosted banner and timer 
		if (PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_VIP_BOOSTS_KEY))
		{
			vipBonusParent.SetActive(true);
			vipStatusEventParent.SetActive(!SlotsPlayer.instance.isMaxVipLevel);
			VIPStatusBoostEvent.featureTimer.registerLabel(vipStatusEventTimer);
		}
		else if (EliteManager.isActive && EliteManager.hasActivePass)
		{
			eliteParent.SetActive(true);
		}
		else if (SlotsPlayer.instance.currentBuyPageInflationPercentIncrease > 0)
		{
			currentInflationIncreaseParent.SetActive(true);
			currentInflationLabel.text = string.Format("{0}%", SlotsPlayer.instance.currentBuyPageInflationPercentIncrease);
		}
		else if (SlotsPlayer.instance.nextBuyPageInflationPercentIncrease > 0)
		{
			nextInflationIncreaseParent.SetActive(true);
			nextInflationLabel.text = string.Format("{0}%", SlotsPlayer.instance.nextBuyPageInflationPercentIncrease);
		}
		else if (level.purchaseBonusPct > 0)
		{
			vipBonusParent.SetActive(true);
			setVIPBonusLabel(level);
		}
		else
		{
			bonusRibbon.SetActive(false);
		}
	}


	protected override void onFadeInComplete()
	{
		base.onFadeInComplete();
		if (isOnSale)
		{
			StartCoroutine(playStaggeredAnimations());
		}
	}

	private IEnumerator playStaggeredAnimations()
	{
		
		// Start animation stuff.
		for (int i = options.Length - 1; i >= 0 ; i--)
		{
			if (options[i] != null)
			{
				BuyCreditsOptionNewHIR option = options[i];
				option.startAnimation();
				yield return new WaitForSeconds(ANIMATION_DELAY);
			}
		}
	}
	
	void Update()
	{
		if(!loadedBackground && bannerRenderer.material != null && bannerRenderer.material.mainTexture != null)
		{
			bannerRenderer.gameObject.SetActive(true);
			loadedBackground = true;
		}

		if (textUpdateTimer != null && textUpdateTimer.isExpired)
		{
			if (CreditSweepstakes.timeRange != null)
			{
				updateTimerLabelText();
			}
			textUpdateTimer.startTimer(1.0f);  // update once/sec
		}
		AndroidUtil.checkBackButton(onCloseButtonClicked, "dialog", dialogStatName, "back", StatsManager.getGameKey(), "", "click");
	}

	/// Called by Dialog.close() - do not call directly.
	public override void close()
	{
		// Do special cleanup. Downloaded textures are automatically destroyed by Dialog class.

		// Check for if we want the extra popcorn surfacing on.
		if (!ExperimentWrapper.FirstPurchaseOffer.isInExperiment && !StarterDialog.isActive && !didPressPurchase)
		{
			STUDSale popcornSale = STUDSale.getSale(SaleType.POPCORN);
			if (popcornSale != null && popcornSale.isActive)
			{
				STUDSaleDialog.showDialog(popcornSale);
			}
		}

		if (PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_BUY_PAGE_KEY))
		{
			PowerupsManager.getActivePowerup(PowerupBase.POWER_UP_BUY_PAGE_KEY).runningTimer.removeFunction(onPowerupExpired);
		}
	}

	public void dimNonClickedOptions(Dict args)
	{
		if (perksCycler != null)
		{
			perksCycler.pauseCycling();
		}
		
		int clickedIndex = (int) args.getWithDefault(D.INDEX, -1);

		if (clickedIndex >= 0)
		{
			for (int i = 0; i < options.Length; i++)
			{
				if (i == clickedIndex || options[i] == null)
				{
					continue;
				}

				options[i].dimPanel();
			}
		}
	}

	public void restoreNonClickedOptions(Dict args)
	{
		perksCycler.startCycling();
		int clickedIndex = (int) args.getWithDefault(D.INDEX, -1);
		if (clickedIndex >= 0)
		{
			for (int i = 0; i < options.Length; i++)
			{
				if (i == clickedIndex || options[i] == null)
				{
					continue;
				}

				options[i].restorePanel();
			}
		}
	}

	protected virtual void setVIPBonusLabel(VIPLevel level)
	{
		vipPercentBonusLabel.text = Localize.text("plus_{0}_percent_more", CommonText.formatNumber(level.purchaseBonusPct));
	}

	public static new void resetStaticClassData()
	{
		hasSeenSale = false;
	}
}
