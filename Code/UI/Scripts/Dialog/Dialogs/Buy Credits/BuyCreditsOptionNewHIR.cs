using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.HitItRich.Feature.VirtualPets;
using TMPro;
/**
Controls one of the options for buying credits.
This dialog also supports STUD-driven credits packages.
*/

public class BuyCreditsOptionNewHIR : BuyCreditsOptionVIP
{
	public AnimationListController.AnimationInformationList saleAnimations;

	public TextMeshPro baseCreditsLabel;		// The "Base Credits" label [the "previous value"]. but it just comes from STUD.

	public GameObject mostPopular;				// Object that holds the Most Popular indicator
	public GameObject bestValue;				// Object that holds the Best Offer indicator

	public TextMeshPro totalBonusLabel;

	// Inspector Links for the Sale badge.
	public bool doSaleBonusThrobbing;
	public GameObject saleStateParent;
	public GameObject saleBonusParent;
	public GameObject saleBonusPercentageParent;
	public GameObject saleBonusMultiplierParent;
	public TextMeshPro saleBonusPercentageLabel;
	public TextMeshPro saleBonusMultiplierLabel;
	public GameObject equalsSizer;

	public GameObject purchaseInfoParent;		// Parent object for all elements that are relevant to packages only [ie not w2e]
	public GameObject jackpotIcon;
	public GameObject collectablesAnchor;

	public GameObject watchToEarnOverlay;		// The parent object for the w2e elements.
	public TextMeshPro watchToEarnLabel;		// The label displaying how many credits the player earns when watching a video.
	
	
	[SerializeField] protected GameObject defaultStateOverlay;

	[SerializeField] protected ButtonHandler buyButtonHandler;
	[SerializeField] private GameObject overlayRoot;
	[SerializeField] private GameObject moreCardsPrefab;
	[SerializeField] private GameObject moreRareCardsPrefab;
	[SerializeField] private GameObject normalBGRoot;
	[SerializeField] private GameObject cardPackBGRoot;

	[SerializeField] protected GameObject postPurchaseChallengeOverlay;
	[SerializeField] protected TextMeshPro postPurchaseBaseCreditsLabel;
	[SerializeField] protected TextMeshPro postPurchaseBonusPercentLabel;
	[SerializeField] protected TextMeshPro postPurchaseVIPPointsLabel;
	[SerializeField] protected  UITexture postPurchaseIconTexture;

	[SerializeField] protected PurchasePerksPanel perksPanel;
	[SerializeField] private AdjustObjectColorsByFactor panelDimmer;
	[SerializeField] private ObjectSwapper coinStackSwapper;
	[SerializeField] private ObjectSwapper backgroundSwapper;
 
	protected bool isWatchToEarn = false;
	protected BuyCreditsDialogNewHIR dialog;
	protected PurchaseFeatureData featureData;

	protected long baseCreditsAmount = 0L;
	protected long totalCreditsAmount = 0L;
	private bool hasSaleAnimationFinished = false;

	private float badgeLargeSize = 0.0f;
	private const float EQUALS_HIDE_TWEEN_DURATION = 0.05f;
	private const float EQUALS_HIDE_TWEEN_SIZE = 0.001f;
	private const float BADGE_TWEEN_SMALL_SIZE = 0.7f;
	private const float THROB_TIMER_MINIMUM = 2.0f;
	private const float THROB_TIMER_MAXIMUM = 6.0f;
	private const float THROB_SIZE = 1.2f;
	private const float THROB_SPEED = 0.5f;
	private const float TOP_BADGE_INTRO_TWEEN_SIZE = 3f;
	private const float NORMAL_BADGE_INTRO_TWEEN_SIZE = 1.7f;
	private const float INTRO_TWEEN_DURATION = 0.3f;
	private const float ROLLUP_DURATION = 0.8f;
	private const float TOTAL_CREDITS_SALE_WIDTH = 700f;
	private const float SALE_CREDITS_FONT_SIZE_MAX = 70f;
	private const float PROMOTION_CYCLE_TIME = 3.0f;
	private GameTimer throbTimer;
	private GameTimer promotionTimer;
	private Queue<BuyCreditsCollectionTag> promotionTags;
	private BuyCreditsCollectionTag activePromotion;

	public override void setIndex(int newIndex, BuyCreditsDialog dialog)
	{
		//WatchToEarn.init(WatchToEarn.REWARD_VIDEO);
		this.dialog = dialog as BuyCreditsDialogNewHIR;
		index = newIndex;

		// The top badge grows to a larger size than the others.
		badgeLargeSize = (index == 5) ? TOP_BADGE_INTRO_TWEEN_SIZE : NORMAL_BADGE_INTRO_TWEEN_SIZE;

		promotionTags = new Queue<BuyCreditsCollectionTag>();

		jackpotIcon.SetActive(false);

		if (WatchToEarn.isEnabled && index == 0)
		{
			setupWatchToEarnState();
		}
		else
		{
			long vipPointsAmount = 0;
			string priceAmount = "";
			
			// This dialog used the STUD-driven credits packages.
			featureData = PurchaseFeatureData.BuyPage;
			
			if (featureData != null && featureData.creditPackages.Count > index && creditPackage == null)
			{
				creditPackage = featureData.creditPackages[index];
			}
			
			if (ExperimentWrapper.FirstPurchaseOffer.isInExperiment && index < ExperimentWrapper.FirstPurchaseOffer.firstPurchaseOffersList.Count)
			{
				FirstPurchaseOfferData currentOfferData = ExperimentWrapper.FirstPurchaseOffer.firstPurchaseOffersList[index];
				PurchasablePackage fallbackPackage = PurchasablePackage.find(currentOfferData.packageName);
				CreditPackage firstPurchaseCreditPackage = new CreditPackage(fallbackPackage, currentOfferData.bonusPercent, false); 
				firstPurchaseCreditPackage.saleBonusPercent = currentOfferData.salePercent;
				firstPurchaseCreditPackage.isBestValue = WatchToEarn.isEnabled ? currentOfferData.isW2eBestValue : currentOfferData.isBestValue;
				firstPurchaseCreditPackage.isMostPopular = WatchToEarn.isEnabled ? currentOfferData.isW2eMostPopular : currentOfferData.isMostPopular;
				//Set first purchase to use normal buy collectable since first purchase does not have collectable data in EOS
				firstPurchaseCreditPackage.collectableDropKeyName =
					creditPackage != null ? creditPackage.collectableDropKeyName : "";
				creditPackage = firstPurchaseCreditPackage;
			}
			

			if (creditPackage == null || Data.liveData.getBool("DISPLAY_ALL_PURCHASE_PACKAGES", false))
			{
				Debug.LogError("BuyCreditsOptionHIR -- STUDAction for New Buy Page is not setup correct, defaulting to fallback ZRT values...");
				// If the action is null, then we need to default to the fallback values.
				string[] packages = Data.liveData.getBool("DISPLAY_ALL_PURCHASE_PACKAGES", false) ? PurchasablePackage.getAllPackageKeyNames() : Glb.BUY_PAGE_DEFAULT_PACKAGES;
				
				if (packages == null || packages.Length <= index)
				{
					Debug.LogError("BuyCreditsOptionHIR -- The default Buy Page Packages are not set up properly, this is super duper bad...");
					return;
				}
				if (string.IsNullOrEmpty(packages[index]))
				{
					Debug.LogError("BuyCreditsOptionHIR -- The default package key is empty");
				}
				PurchasablePackage fallbackPackage = PurchasablePackage.find(packages[index]);
				if (fallbackPackage == null)
				{
					Debug.LogError("BuyCreditsOptionHIR -- The fallback Package " + packages[index] + "could not be found");
				}

				creditPackage = new CreditPackage(fallbackPackage, 0, false); 
			}

			long nonSaleTotalAmount = 1;
			if (creditPackage.purchasePackage != null)
			{
				priceAmount = creditPackage.purchasePackage.getLocalizedPrice();
				vipPointsAmount = creditPackage.purchasePackage.vipPoints();


				if (PurchaseFeatureData.isSaleActive)
				{
					// If we are in a sale, then use the sale bonus percent.
					totalCreditsAmount = creditPackage.purchasePackage.totalCredits(creditPackage.bonus, true,
						creditPackage.getSaleBonus(true));
				}
				else
				{
					// Otherwise, don't apply the bonus percent.
					totalCreditsAmount = creditPackage.purchasePackage.totalCredits(creditPackage.bonus, true);
				}

				baseCreditsAmount = creditPackage.purchasePackage.creditsBase();
				nonSaleTotalAmount = creditPackage.purchasePackage.totalCredits(creditPackage.bonus, true);
			}

			if (BuyPageCardEvent.instance.isEnabled)
			{
				// If the buy page a card event is active, then see if we have anything to turn on here.
				showCardEvent(creditPackage.activeEvent, creditPackage.eventText);
			}

			float nonSaleBonusFloat = (float)nonSaleTotalAmount / (float)baseCreditsAmount * 100f;
			int nonSaleBonusPercent = Mathf.FloorToInt(nonSaleBonusFloat - 100f);

			cost.text = priceAmount;

			if (vipBonus != null && vipSaleBonus != null)
			{
				vipBonus.text = vipSaleBonus.text =
					Localize.textUpper("plus_{0}_vip_pts", CommonText.formatNumber(vipPointsAmount));
			}

			setBaseCreditsLabel();
			if (totalBonusLabel != null)
			{
				totalBonusLabel.text = Localize.textUpper("{0}_percent", CommonText.formatNumber(nonSaleBonusPercent));
			}
			jackpotIcon.SetActive(false);

			if (creditPackage.getSaleBonus(true) > 0 && PurchaseFeatureData.isSaleActive)
			{
				setSaleLabels(nonSaleTotalAmount);
			}
			else
			{
				setNonSaleLabels(featureData);
			}

			bestValue.SetActive(creditPackage.isBestValue);
			mostPopular.SetActive(false);
			if (!bestValue.activeSelf)
			{
				// Only turn on the most popular icon if the best value option is not on.
				mostPopular.SetActive(creditPackage.isMostPopular);
			}

			PostPurchaseChallengeCampaign campaign = PostPurchaseChallengeCampaign.getActivePostPurchaseChallengeCampaign();
			if (campaign != null && campaign.isEarlyEndActive && perksPanel == null)
			{
				campaign.postPurchaseChallengeEnded += () =>
				{
					SafeSet.gameObjectActive(defaultStateOverlay, true);
					SafeSet.gameObjectActive(saleStateParent, true);
					SafeSet.gameObjectActive(postPurchaseChallengeOverlay, false);
				};
				
				int bonusAmount = campaign.getPostPurchaseChallengeBonus(index);
				if (bonusAmount > 0)
				{
					setupPostPurchaseChallenge(campaign, bonusAmount);
				}

				if (postPurchaseVIPPointsLabel != null)
				{
					postPurchaseVIPPointsLabel.text = Localize.textUpper("plus_{0}_vip_pts", CommonText.formatNumber(vipPointsAmount));
				}
			}
		}

		if (buyButtonHandler != null)
		{
			buyButtonHandler.registerEventDelegate(onClick);
		}

		if (coinStackSwapper != null)
		{
			coinStackSwapper.setState(index.ToString());
		}
	}

	public void setIndex(int newIndex, BuyCreditsDialog dialog, List<PurchasePerksPanel.PerkType> cyclingPerks, PurchasePerksCycler perksCycler)
	{
		setIndex(newIndex, dialog);
		if (perksPanel != null)
		{
			if (WatchToEarn.isEnabled && newIndex == 0)
			{
				perksPanel.gameObject.SetActive(false); //Turn off the panel if W2E is on for this panel
			}
			else
			{
				perksPanel.init(index, creditPackage, dialog.dialogStatName, cyclingPerks, this.dialog.restoreNonClickedOptions, perksCycler, this, PurchaseFeatureData.Type.BUY_PAGE);
				perksPanel.openDrawerButton.registerEventDelegate(this.dialog.dimNonClickedOptions, Dict.create(D.INDEX, index));
			}
		}
	}

	protected void setupPostPurchaseChallenge(PostPurchaseChallengeCampaign campaign, int bonus)
	{
		SafeSet.gameObjectActive(defaultStateOverlay, false);
		SafeSet.gameObjectActive(saleStateParent, false);
		SafeSet.gameObjectActive(postPurchaseChallengeOverlay, true);
		
		if (!campaign.isLocked)
		{
			postPurchaseBonusPercentLabel.text = string.Format("<size=144%><#FFF400>Reset Timer</color></size>\nOn your {0}!", ExperimentWrapper.PostPurchaseChallenge.theme);
		}
		else
		{
			postPurchaseBonusPercentLabel.text = string.Format("<size=144%><#FFF400>Up to {0}%</color></size>\nMORE COINS!", bonus);
		}
		
		Material iconMaterial = new Material(postPurchaseIconTexture.material.shader);
		iconMaterial.mainTexture = campaign.icon;
		postPurchaseIconTexture.material = iconMaterial;
	}

	protected void showCardEvent(CreditPackage.CreditEvent activeEvent, string eventText)
	{
		switch (activeEvent)
		{
			case CreditPackage.CreditEvent.MORE_CARDS:
				showMoreCards(eventText, "more_cards");
				break;

			case CreditPackage.CreditEvent.MORE_RARE_CARDS:
				showMoreRareCards(eventText, "rare_cards");
				break;

			case CreditPackage.CreditEvent.NOTHING:
				showEmptyEvent();
				break;

		}
	}

	protected virtual void setBaseCreditsLabel()
	{
		string credits = CreditsEconomy.convertCredits(baseCreditsAmount);
		baseCreditsLabel.text = credits;
	}

	protected virtual void setSaleLabels(long nonSaleTotalAmount)
	{
		// If there is a sale currently active, and there is a non-zero bonus percent
		// Then show sale items and start the aniamtions.

		// HIR-60310 Updating the label to always show the <##>X version even when not a multiple of 100
		if (creditPackage.getSaleBonus(true) >= 100)
		{
			double mult = ((double)creditPackage.getSaleBonus(true) + 100) / 100;
			mult = System.Math.Truncate(mult * 10f) / 10f;
			saleBonusMultiplierLabel.text = Localize.textUpper("{0}X", mult);
		}
		else
		{
			saleBonusMultiplierLabel.text = Localize.textUpper("{0}%", creditPackage.getSaleBonus(true));
		}
		saleBonusMultiplierParent.SetActive(true);
		SafeSet.gameObjectActive(saleBonusPercentageParent, false);

		// Make the text bigger since we dont have any feature badges on the right.
		//totalSaleCredits.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, TOTAL_CREDITS_SALE_WIDTH);
		totalCredits.text = CreditsEconomy.convertCredits(nonSaleTotalAmount);
		//totalSaleCredits.fontSizeMax = SALE_CREDITS_FONT_SIZE_MAX;
	}

	protected virtual void setNonSaleLabels(PurchaseFeatureData featureData)
	{
		totalCredits.text = CreditsEconomy.convertCredits(totalCreditsAmount);
		// If the label would show 0% MORE! then we should turn it off....
		saleBonusParent.SetActive(false);
	}

	public void startAnimation()
	{
		if (creditPackage != null && creditPackage.getSaleBonus(true) > 0)
		{
			// Then we are on sale, and should start the animation.
			StartCoroutine(saleAnimationRoutine());
		}
	}

	private IEnumerator saleAnimationRoutine()
	{
		if (equalsSizer != null)
		{
			if (!ExperimentWrapper.HyperEconomy.isShowingRepricedVisuals)
			{
				yield return tweenObjectAndForceSize(equalsSizer, EQUALS_HIDE_TWEEN_SIZE, EQUALS_HIDE_TWEEN_DURATION);
				equalsSizer.SetActive(false);
			}
		}
		else
		{
			Debug.LogWarning("BuyCreditsOptionNewHIR -- saleAnimationRoutine -- equalsSizer is null for some reason.");
		}

		if (saleBonusParent != null)
		{
			saleBonusParent.SetActive(true);
			StartCoroutine(AnimationListController.playListOfAnimationInformation(saleAnimations));
			// Turn on the badge object and tween it from tiny to large (with a bounce)
		}
		else
		{
			Debug.LogWarning("BuyCreditsOptionNewHIR -- saleAnimationRoutine -- saleBonusParent is null for some reason.");
		}

		if (totalCredits != null)
		{
			// Roll up the total credits
			yield return SlotUtils.rollup(start:baseCreditsAmount, end:totalCreditsAmount, tmPro: totalCredits, playSound:true, specificRollupTime: ROLLUP_DURATION,
				shouldBigWin: false);
			yield return new WaitForSeconds(ROLLUP_DURATION);
		}
		else
		{
			Debug.LogWarning("BuyCreditsOptionNewHIR -- saleAnimationRoutine -- totalCredits label is null for some reason.");
		}
		finalizeAnimation();
	}

	private IEnumerator tweenObjectAndForceSize(GameObject target, float scale, float duration)
	{
		Vector3 goalSize = Vector3.one * scale;
		iTween.ScaleTo(saleBonusParent, iTween.Hash("scale", goalSize, "time", duration, "easetype", iTween.EaseType.linear));
		yield return new WaitForSeconds(duration);
		if (saleBonusParent.transform.localScale != goalSize)
		{
			saleBonusParent.transform.localScale = goalSize;
		}
	}

	public void Update()
	{
		if (hasSaleAnimationFinished && creditPackage.getSaleBonus(true) > 0)
		{
			if (doSaleBonusThrobbing)
			{
				if (throbTimer == null)
				{
					throbTimer = new GameTimer(Random.Range(THROB_TIMER_MINIMUM, THROB_TIMER_MAXIMUM));
				}
				if (throbTimer.isExpired)
				{
					throbTimer = new GameTimer(Random.Range(THROB_TIMER_MINIMUM, THROB_TIMER_MAXIMUM));
					StartCoroutine(CommonEffects.throb(saleBonusParent, THROB_SIZE, THROB_SPEED));
				}
			}
		}

		if (promotionTimer == null || promotionTimer.isExpired)
		{
			if (promotionTags != null && promotionTags.Count > 0)
			{
				if (activePromotion == null)  //no current promotion showing
				{
					activePromotion = promotionTags.Dequeue();
					if (activePromotion != null)
					{
						activePromotion.gameObject.SetActive(true);
						if (promotionTimer == null)
						{
							activePromotion.show();
						}
						else
						{
							activePromotion.fadeIn();
						}

					}

					//Queue null entry again when it is not the first entry or we have to requeue it
					if(promotionTimer != null)
					{
						promotionTags.Enqueue(null);
					}
				}
				else
				{
					//fade out current promotion and re-add to end of queue
					activePromotion.fadeOut();
					promotionTags.Enqueue(activePromotion);

					//fade in next promotion
					activePromotion = promotionTags.Dequeue();
					if (activePromotion != null)
					{
						activePromotion.gameObject.SetActive(true);
						activePromotion.fadeIn();
					}

				}
			}
			promotionTimer = new GameTimer(PROMOTION_CYCLE_TIME);
		}
	}	

	public bool setupJackpotIcon(bool isProgressive)
	{
		bool jackpotActive = isProgressive &&
		                     ProgressiveJackpot.buyCreditsJackpot != null &&
		                     creditPackage != null &&
		                     creditPackage.isJackpotEligible &&
		                     ExperimentWrapper.BuyPageProgressive.jackpotDaysIsActive;

		//default to off, the promotion cycle code will activate
		SafeSet.gameObjectActive(jackpotIcon, false);

		if (jackpotActive)
		{
			BuyCreditsCollectionTag tag = jackpotIcon.GetComponent<BuyCreditsCollectionTag>();
			if (tag != null)
			{
				promotionTags.Enqueue(tag);
				return true;
			}
		}
		else if (ExperimentWrapper.BuyPageProgressive.jackpotDaysIsActive)
		{
			promotionTags.Enqueue(null);
		}

		return false;
	}

	private void finalizeAnimation()
	{
		// Force everything to be the corect size/label.
		saleBonusParent.transform.localScale = Vector3.one;
		totalCredits.text = CreditsEconomy.convertCredits(totalCreditsAmount);
		hasSaleAnimationFinished = true;
	}

	protected void setupWatchToEarnState()
	{
		isWatchToEarn = true;

		watchToEarnLabel.text = Localize.text("watch_video_for_credits_{0}", CreditsEconomy.convertCredits(WatchToEarn.rewardAmount));
		cost.text = Localize.textUpper("free");
		purchaseInfoParent.SetActive(false);
		SafeSet.gameObjectActive(watchToEarnOverlay, true);
	}	

	protected void onClick(Dict args = null)
	{
		if (WatchToEarn.isEnabled && isWatchToEarn)
		{
			Debug.Log("In w2e click buy page");
			StatsManager.Instance.LogCount("dialog", dialog.dialogStatName, "watch_Ad", StatsManager.getGameTheme(), StatsManager.getGameName(), "click");
			WatchToEarn.watchVideo("buypage", true);
			
			PostPurchaseChallengeCampaign campaign = PostPurchaseChallengeCampaign.getActivePostPurchaseChallengeCampaign();
			if (campaign != null)
			{
				campaign.updatePostPurchaseChallengeMaxBonus();
			}
			// Might as well just close the dialog here, since it's not a normal purchase.
			Dialog.close(dialog);
		}
		else
		{
			Debug.Log("In else w2e click buy page");
			string packageKey = "";
			bool isJackpotEligible = false;
			if (creditPackage != null)
			{
				int saleBonusPercent = (PurchaseFeatureData.isSaleActive ? creditPackage.getSaleBonus(true) : 0);

				string packageClass = "";
				PurchaseFeatureData featureData = PurchaseFeatureData.BuyPage;
				
				OverlayTopHIRv2 overlay = OverlayTopHIRv2.instance as OverlayTopHIRv2;
				bool showLottoBlast = ExperimentWrapper.LevelLotto.isInExperiment 
				                      && FeatureOrchestrator.Orchestrator.activeFeaturesToDisplay.Contains(ExperimentWrapper.LevelLotto.experimentName);
				string buffKey = showLottoBlast ? ExperimentWrapper.LevelLotto.buffKeyname : "";

				if (creditPackage.purchasePackage != null)
				{
					PurchaseFeatureData.Type purchaseType = featureData != null ? featureData.type : PurchaseFeatureData.Type.NONE;
					creditPackage.purchasePackage.makePurchase(creditPackage.bonus, false, index, packageClass, saleBonusPercent, collectablePack:creditPackage.collectableDropKeyName, purchaseType:purchaseType, buffKey:buffKey);

					packageKey = creditPackage.purchasePackage.keyName;
					isJackpotEligible = creditPackage.isJackpotEligible;
					dialog.didPressPurchase = true; // Tell the dialog that we are making a purchase.
				}
				else
				{
					Debug.LogErrorFormat("BuyCreditsOptionNewHIR.cs -- OnClick -- creditPackage.packge is null, this is very bad.");
				}
			}

#if UNITY_EDITOR
			PostPurchaseChallengeCampaign postPurchaseChallengeCampaign = PostPurchaseChallengeCampaign.getActivePostPurchaseChallengeCampaign();
			if (postPurchaseChallengeCampaign != null && postPurchaseChallengeCampaign.getPostPurchaseChallengeBonus(index) > 0)
			{ 
				ServerAction action = new ServerAction(ActionPriority.HIGH, "purchase_challenge");
			}
#endif
			
			if (packageKey != "")
			{
				StatsManager.Instance.LogCount(
					"dialog",
					dialog.dialogStatName,
					packageKey,
					StatsManager.getGameTheme(),
					StatsManager.getGameName(),
					"click"
				);
				if (ExperimentWrapper.FirstPurchaseOffer.isInExperiment)
				{
					StatsManager.Instance.LogCount(counterName: "dialog", 
						kingdom: "buy_page_v3", 
						phylum: packageKey, 
						klass: "first_purchase_offer",
						genus: "click");
				}

				if (ExperimentWrapper.BuyPageProgressive.jackpotDaysIsActive)
				{
					if (isJackpotEligible)
					{
						StatsManager.Instance.LogCount("dialog", "buy_page_v3", packageKey, "jackpot_days", "purchase_jackpot", "click");
					}
					else
					{
						StatsManager.Instance.LogCount("dialog", "buy_page_v3", packageKey, "jackpot_days", "purchase_regular", "click");
					}
				}
			}
		}
	}

	public override TextMeshPro[] getTextMeshPros()
	{
		return new TextMeshPro[] {
			totalCredits,
			cost,
			vipBonus,
			baseSaleCredits,
			vipSaleBonus,
			baseCreditsLabel,
			totalBonusLabel,
			saleBonusPercentageLabel,
			saleBonusMultiplierLabel,
			watchToEarnLabel};
	}


	private void showMoreCards(string text1, string text2)
	{
		//change the background
		normalBGRoot.SetActive(false);
		cardPackBGRoot.SetActive(true);

		//add the tag
		if (moreCardsPrefab != null)
		{
			GameObject promoOverlay = CommonGameObject.instantiate(moreCardsPrefab, overlayRoot.transform) as GameObject;
			if (promoOverlay != null)
			{
				promoOverlay.SetActive(true);
				BuyCreditsCollectionTag tag = promoOverlay.GetComponent<BuyCreditsCollectionTag>();
				if (tag != null)
				{
					tag.setText(text1, text2);
					tag.gameObject.SetActive(false);
					promotionTags.Enqueue(tag);
				}
			}

		}
	}

	private void showEmptyEvent()
	{
		//add an empty item to the promotion queue
		promotionTags.Enqueue(null);
	}

	private void showMoreRareCards(string text1, string text2)
	{
		//change the background
		normalBGRoot.SetActive(false);
		cardPackBGRoot.SetActive(true);

		//add the tag
		if (moreRareCardsPrefab != null)
		{
			GameObject promoOverlay = CommonGameObject.instantiate(moreRareCardsPrefab, overlayRoot.transform) as GameObject;
			if (promoOverlay != null)
			{
				promoOverlay.SetActive(true);
				BuyCreditsCollectionTag tag = promoOverlay.GetComponent<BuyCreditsCollectionTag>();
				if (tag != null)
				{
					if (string.IsNullOrEmpty(text1))
					{
						text1 = "More";
					}
					tag.setText(text1, text2);
					tag.gameObject.SetActive(false);
					promotionTags.Enqueue(tag);
				}
			}
		}
	}

	public void onDrawerOpen()
	{
		backgroundSwapper.setState("selected");
	}

	public void onDrawerClose()
	{
		backgroundSwapper.setState("unselected");
	}

	public void dimPanel()
	{
		panelDimmer.multiplyColors();
		perksPanel.dimCurrentIcon();
	}

	public void restorePanel()
	{
		panelDimmer.restoreColors();
		perksPanel.restoreCurrentIcon();
	}
}
