using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

public class LuckyDealDialog : DialogBase 
{

	public static bool wasWheelDealActiveAtLogIn;
	public static bool waitingForEventData;
	private static bool didInitData;
	private static bool waitingForCoinData;
	public static bool doNotShowUntilRestart;
	public static JSON eventData;	
	private static int freeCoins;
	public static JSON freeCoinData;
	public static GameTimerRange eventTimer;
	public static string errMessage = "";
	public Animator mainAnimator;
	public AnimationListController.AnimationInformationList animationList;
	public AnimationListController.AnimationInformationList creditsRecievedAnims;

	public ButtonHandler exitButton;
	public ButtonHandler confirmButton;
	public ButtonHandler collectButton;
	public ButtonHandler collectCloseButton;
	public ButtonHandler maybeLaterButton;

	public List<LuckyDealReel> reels = new List<LuckyDealReel>();
	public TextMeshPro 	creditsAwardedCollect;
	public TextMeshPro 	creditsAwardedConfirmAndShare;
	
	public GameObject dollarSlashGroup;
	public GameObject newPriceGroup;
	public GameObject freeGroup;
	public GameObject vipGroup;

	public TextMeshPro 	origPriceLabel;
	public TextMeshPro 	percentOffLabel;
	public TextMeshPro 	discountLabel;
	public TextMeshPro 	buttonLabel;
	public TextMeshPro 	vipTierLabel;
	public TextMeshPro 	timerLabel;

	public float preSpinDelay = 10.0f;
	public float postSpinDelay = 3.0f;
	public float coinTravelTime = 0.5f;

	public int bonusPercent;

	// flying coin animation anchors
	public GameObject coinStart;
	public GameObject coinEnd;

	private PurchasablePackage dealPackage;

	private string state;
	private int[] reelStops;

	private JSON collectData;

	private bool skipToBanner;
	private static bool eventHasBeenSpunThisSession;
	SkippableWait skipWait = new SkippableWait();

	public override void init()
	{
		economyTrackingName = "wheel_deal";

		registerButtonHandlers();

		processDialogArgs();

		if (state == "INTRO")
		{
			if (eventData != null)
			{
				StartCoroutine(manageEventDataSequence());
			}
			
		}
		else if (state == "COLLECT")
		{
			StartCoroutine(manageCollectSequence());					
		}
	}

	public IEnumerator manageCollectSequence()
	{
		if (freeCoins > 0)
		{
			setCreditLabels(freeCoins);			
		}
		else if (collectData != null)
		{
			if (dealPackage != null)
			{
				StatsManager.Instance.LogCount(counterName: "wheel_deal", kingdom: "dialog", phylum:"purchase_confirm_page", klass: ExperimentWrapper.WheelDeal.keyName, family: dealPackage.keyName, genus: bonusPercent.ToString());			
			}			
			setCreditLabels(collectData.getInt("credits", 0));
		}
		else
		{
			setCreditLabels(0);			
		}
			
		startCollectBanner();

		yield return new WaitForSeconds(1.0f);

		StartCoroutine(coinEffect());

		yield return null;
	}

	private IEnumerator coinEffect()
	{
		CoinScriptUpdated coin = CoinScriptUpdated.create(
			sizer,
			coinStart.transform.position,
			new Vector3(0, 0, 0)
		);

		Vector2 destination = NGUIExt.localPositionOfPosition(sizer, coinEnd.transform.position);

		while (coin != null)
		{
			Vector3 prevDest = NGUIExt.localPositionOfPosition(sizer, coin.transform.position);;
			yield return StartCoroutine(coin.flyTo(destination, coinTravelTime));
			destination = prevDest;
		}
	}		

	public IEnumerator waitAndStart()
	{
		yield return new WaitForSeconds(1.0f);
		StartCoroutine(manageEventDataSequence());
	}

	// requires event data
	public IEnumerator manageEventDataSequence()
	{
		// yield so that touch input gets a chance to clear its taps, such as from the carousel card, so we don't skip the spin
		// yes we have to wait 2 frames worth
		yield return null;
		yield return null;

		adjustForSpinViewState();

		initPurchaseData();

		initReels(eventData);

		setSavingsInfo();

		skipToBanner = false;
		skipWait.reset();

		activateSavingsInfo();

		if (state == "INTRO")
		{
			StatsManager.Instance.LogCount(counterName: "wheel_deal", kingdom: "dialog", phylum:"spin_page", klass: ExperimentWrapper.WheelDeal.keyName, family: "", genus: "");			
			// start up the intro animations
			StartCoroutine(AnimationListController.playListOfAnimationInformation(animationList));
		
			// spin the wheel reels
			yield return StartCoroutine(skipWait.wait(preSpinDelay));

			startSpinning();

			while (areReelsSpinning() && !skipToBanner)
			{
				skipToBanner = (TouchInput.didTap || skipToBanner);
				yield return null;
			}

			yield return StartCoroutine(skipWait.wait(postSpinDelay));

			if (areReelsSpinning())
			{
				foreach (LuckyDealReel reel in reels)
				{
					reel.lockInSymbols();
				}				
			}
		}
		else
		{
			if (dealPackage != null)
			{
				StatsManager.Instance.LogCount(counterName: "wheel_deal", kingdom: "dialog", phylum:"purchase_page", klass: ExperimentWrapper.WheelDeal.keyName, family: dealPackage.keyName, genus: bonusPercent.ToString());			
			}
			StartCoroutine(playReelStopAnimations());
		}

		// recored that player has seen the spin
		PlayerPrefsCache.SetInt(Prefs.HAS_SPUN_LUCKY_DEAL, 1);
		eventHasBeenSpunThisSession = true;

	}

	private void adjustForSpinViewState()
	{
		// check if they get to see the spin
		if ((eventData.getBool("spin", false) || PlayerPrefsCache.GetInt(Prefs.HAS_SPUN_LUCKY_DEAL, 0) == 0) && !eventHasBeenSpunThisSession)
		{
			PlayerPrefsCache.SetInt(Prefs.HAS_SPUN_LUCKY_DEAL, 0);
		}
		else if (PlayerPrefsCache.GetInt(Prefs.HAS_SPUN_LUCKY_DEAL, 0) == 1)
		{
			state = "PURCHASE";
		}
	}

	private void activateSavingsInfo()
	{
		SafeSet.gameObjectActive(dollarSlashGroup, true);

		bool isDeal = freeCoins == 0;

		// Active if Deal
		SafeSet.gameObjectActive(newPriceGroup, isDeal);		
		SafeSet.gameObjectActive(maybeLaterButton.gameObject, isDeal);
		SafeSet.gameObjectActive(vipGroup, isDeal);

		// active if Free win
		SafeSet.gameObjectActive(freeGroup, !isDeal);


		if (isDeal)
		{
			SafeSet.labelText(buttonLabel, Localize.text("buy_now"));
			VIPLevel myLevel = VIPLevel.find(SlotsPlayer.instance.vipNewLevel);
			if (myLevel != null && myLevel.purchaseBonusPct > 0)
			{
				SafeSet.labelText(vipTierLabel, Localize.text("lucky_deal_vip_level_{0}", myLevel.purchaseBonusPct, myLevel.name));
			}
			else
			{
				SafeSet.gameObjectActive(vipGroup, false);
			}		
		}
		else
		{
			SafeSet.labelText(buttonLabel,  Localize.text("collect_amp_share"));
		}		
	}

	private void processDialogArgs()
	{
		state = dialogArgs != null && dialogArgs.containsKey(D.DATA) ? dialogArgs[D.DATA] as string : state = "INTRO";

		if (state == "COLLECT")
		{
			collectData = dialogArgs.getWithDefault(D.CUSTOM_INPUT, null) as JSON;
		}
		else
		{
			collectData = null;
		}
	}

	private void registerButtonHandlers()
	{
		exitButton.registerEventDelegate(onClickClose);
		maybeLaterButton.registerEventDelegate(onClickClose);
		confirmButton.registerEventDelegate(onClickConfirm);
		collectButton.registerEventDelegate(onClickCollect);
		collectCloseButton.registerEventDelegate(onClickCollect);
	}

	private void initPurchaseData()
	{
		bonusPercent = eventData.getInt("bonus_percent", 0);

		if (freeCoinData == null)
		{
			freeCoins = 0;
		}

		dealPackage = PurchasablePackage.find(eventData.getString("coin_package", ""));
		if (freeCoins > 0)
		{
			setCreditLabels(freeCoins);			
		}
		else if (dealPackage != null)
		{
			setCreditLabels(dealPackage.totalCredits(bonusPercent, false));
		}
		else
		{
			Debug.LogError("No credit package found! " + eventData.getString("coin_package", ""));
		}
	}

	private IEnumerator playReelStopAnimations()
	{
		foreach (LuckyDealReel reel in reels)
		{
			yield return StartCoroutine(reel.playStopAnimations());
		}
	}	

	private void startSpinning()
	{
		foreach (LuckyDealReel reel in reels)
		{
			reel.startSpinning();
		}
	}

	private bool areReelsSpinning()
	{
		foreach (LuckyDealReel reel in reels)
		{
			if (reel.isSpinning)
			{
				return true;
			}
		}

		return false;
	}

	private void setCreditLabels(long amount)
	{
		SafeSet.labelText(creditsAwardedCollect, CreditsEconomy.convertCredits(amount));
		SafeSet.labelText(creditsAwardedConfirmAndShare, CreditsEconomy.convertCredits(amount));
	}

	private void setSavingsInfo()
	{
		SafeSet.labelText(origPriceLabel, reels[0].stopText);

		if (freeCoins == 0)
		{
			SafeSet.labelText(percentOffLabel, reels[1].stopText);
			if (dealPackage != null)
			{
				SafeSet.labelText(discountLabel, dealPackage.getRoundedPrice());
			}
			else
			{
				SafeSet.labelText(discountLabel, "0");
			}
		}

		updateTimer();

	}

	private void updateTimer()
	{
		if (timerLabel != null)
		{
			timerLabel.text =  Localize.text("deal_ends_in_{0}", eventTimer.timeRemainingFormatted);
		}
	}

	public static void initLoginData(JSON data)
	{
		doNotShowUntilRestart = false;

		wasWheelDealActiveAtLogIn = data.getBool("wheel_deal", false);

		eventTimer = new GameTimerRange(0,0, false);

		if (wasWheelDealActiveAtLogIn)
		{
			getEventAction();
		}
	}

	public static void registerEventDelegates()
	{
		Server.registerEventDelegate("wheel_deal_info", initEventData, true);
		Server.registerEventDelegate("wheel_deal_coins", initFreeCoinsData, true);
	}

	public static void getEventAction()
	{
		didInitData = false;
		waitingForEventData = true;
		waitingForCoinData = true;
		eventData = null;
		freeCoinData = null;
		eventHasBeenSpunThisSession = false;
		LuckyDealAction.getDeal();
	}

	public static void initEventData(JSON data)
	{
		didInitData = true;

		if (data != null)
		{
			// check if free coins expected, if so dont mark data as valid until we have it
			if (isFreeCoinWinnerEvent(data) && waitingForCoinData)
			{
				RoutineRunner.instance.StartCoroutine(waitForFreeCoinData(data));
			}
			else
			{
				setEventDataValid(data);
			}
		}
		else
		{
			Debug.LogError("LuckyDealDialog initEventData: json data is null");
		}
	}

	public static IEnumerator waitForFreeCoinData(JSON data)
	{
		while (waitingForCoinData)
		{
			yield return null;
		}

		setEventDataValid(data);
	}

	private static void setEventDataValid(JSON data)
	{
		waitingForEventData = false;
		eventData = data;

		eventTimer.startTimers(data.getInt("start_date", 0), data.getInt("end_date", 1));
		eventTimer.registerFunction(luckyDealExpireHandler);

		if (freeCoins > 0)
		{
			doNotShowUntilRestart = true;   // no matter what they will not see this spin again ever
			hideCarousel();
		}
		else
		{
			showCarousel();
		}		
	}

	private static void luckyDealExpireHandler(Dict args = null, GameTimerRange originalTimer = null)	
	{
		Debug.Log("Lucky Deal Timer has expired");
		doNotShowUntilRestart = true;
		hideCarousel();
		eventTimer.removeFunction(luckyDealExpireHandler);
	}

	private static void hideCarousel()
	{
		CarouselData data;
		data = CarouselData.findActiveByAction("wheel_deal");
		if (data != null)
		{
			data.deactivate();
		}

		if (OverlayTopHIR.instance != null)
		{
			OverlayTopHIR.instance.setupSaleNotification();
		}		
	}

	private static void showCarousel()
	{

		CarouselData data;
		data = CarouselData.findInactiveByAction("wheel_deal");
		if (data != null)
		{
			data.activate();
		}			
		if (OverlayTopHIR.instance != null)
		{
			OverlayTopHIR.instance.setupSaleNotification();
		}		
	}	

	public static void initFreeCoinsData(JSON data)
	{
		waitingForCoinData = false;
		freeCoinData = data;

		if (data != null)
		{
			freeCoins = freeCoinData.getInt("coins", 0);
		}
	}

	private static bool isFreeCoinWinnerEvent(JSON data)
	{
		int[] stops = data.getIntArray("reel_stops");
		JSON[] strips = data.getJsonArray("reel_strips");
		string[] symbols = null;

		if (strips.Length > 1)
		{
			symbols = strips[1].getStringArray("symbols");
		}

		if (symbols != null && symbols.Length > 1)
		{
			return (symbols[stops[1]].Contains("100"));
		}
	
		return false;
	}		

	private void initReels(JSON data)
	{
		int reelIndex = 0;

		reelStops = data.getIntArray("reel_stops");

		foreach (JSON reelStrip in data.getJsonArray("reel_strips"))
		{
			if (reelIndex < reels.Count)
			{
				reels[reelIndex].init(reelStrip, reelStops[reelIndex]);
				if (state == "PURCHASE")
				{
					reels[reelIndex].lockInSymbols();		// put in final position since we already saw the spin
				}
			}
			else
			{
				Debug.LogError("We have more JSON reelstrips than reels! reelIndex : " + reelIndex);
			}
			reelIndex++;				
		}

		if (freeCoins > 0)
		{
			// swap reel stop sound out for extra blingy version
			AnimationListController.changeSoundName(reels[1].reelStopAnimations, "Wheel2StopsNormalWheelDeal", "Wheel2StopsFreeWheelDeal");
		}
	}

	public void onClickConfirm(Dict args = null)
	{
#if UNITY_EDITOR
		if (DevGUIMenuLuckyDeal.useFakeServerPurchase)
		{
			collectData = new JSON(DevGUIMenuLuckyDeal.fakePurchaseJSON);
			purchaseSucceeded(collectData, PurchaseFeatureData.Type.NONE);
		}		
		else if (UnityEngine.Input.GetKey(KeyCode.LeftControl) || UnityEngine.Input.GetKey(KeyCode.RightControl))
		{
			// hack for quick iteration of animaitons during dev
			PlayerPrefsCache.SetInt(Prefs.HAS_SPUN_LUCKY_DEAL, 0);
			state = "INTRO";
			StartCoroutine(waitAndStart());
			return;			
		}
#endif

		if (freeCoins > 0)
		{
			// do the og facebook feed...... need endpoint
			// award credits
			StartCoroutine(manageCollectSequence());			
			return;
		}
		else if (dealPackage != null)
		{
			if (Data.debugMode && DevGUIMenuLuckyDeal.useFakeServerPurchase)
			{
				StartCoroutine(manageCollectSequence());			
				return;
			}			
			dealPackage.makeWheelDealPurchase(bonusPercent);
		}

	}

	private void addCredits()
	{
		int amount = 0;

		if (freeCoins > 0)
		{
			amount = freeCoins;
		}
		else if (collectData != null)
		{
			amount = collectData.getInt("credits", 0);
		}

		SlotsPlayer.addNonpendingFeatureCredits(amount, "wheelDeal");
	}

	private void startCollectBanner()		
	{
		StartCoroutine(AnimationListController.playListOfAnimationInformation(creditsRecievedAnims));
	}

	public void onClickCollect(Dict args = null)
	{
		Dialog.close();
	}	

	public void onClickClose(Dict args = null)
	{
		Dialog.close();
	}

	public override void close()
	{
		if (state == "COLLECT")
		{
			addCredits();		
			PlayerPrefsCache.SetInt(Prefs.HAS_SPUN_LUCKY_DEAL, 0);

			// they collected so lets get a new event!
			if (!eventTimer.isExpired)
			{
				getEventAction();
			}
		}

		// this audio can be quite long, so kill it if need be if they close early
		Audio.stopSound(Audio.findPlayingAudio("IntroWheelDeal"));
		Audio.stopSound(Audio.findPlayingAudio("OfferSummaryWheelDeal"));
	}

	public override void purchaseFailed(bool timedOut)
	{
		errMessage += " *The purchase has failed, hopefuly economey manager will return control to us.* ";
	}		

	public override PurchaseSuccessActionType purchaseSucceeded(JSON data, PurchaseFeatureData.Type purchaseType)
	{
		collectData = data;

		if (Data.debugMode && data != null)
		{
			DevGUIMenuLuckyDeal.fakePurchaseJSON = data.ToString();
		}

		// send the ever important purchase accepted event
		string eventId = "";
		if (data != null)
		{
			eventId = data.getString("event", "");
		}
		if (eventId != "")
		{
			CreditAction.acceptPurchasedItem(eventId);
		}

		state = "COLLECT";

		StartCoroutine(manageCollectSequence());

		return PurchaseSuccessActionType.skipThankYouDialog;
	}	

	void Update()
	{
		AndroidUtil.checkBackButton(onClickClose);

		updateTimer();		
	}

	public static bool showDialog(string motdKey = "", Dict args = null)
	{
		if (args == null)
		{
			args = Dict.create(
				D.IS_LOBBY_ONLY_DIALOG, true,
				D.MOTD_KEY, motdKey
			);
		}
		Scheduler.addDialog("lucky_deal", args);
		return true;
	}	
}
