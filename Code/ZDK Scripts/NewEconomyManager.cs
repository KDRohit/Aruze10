// This allows testing the full flow of IAP in the editor
// And on device for now
#if UNITY_EDITOR
#define LOCAL_IAP_TESTING
#define PURCHASE_MULTIPLIER_TESTING // Enable this for local purchase multiplier testing in editor.
#endif
// This allows testing the full flow of IAP in the editor
// And on device for now
using System;
using Zynga.Payments.IAP.Impl;
using Zynga.Payments.IAP;
using Zynga.Core.JsonUtil;

using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using CustomLog;
using UnityEngine;
using Zynga.Zdk;
using Zynga.Zdk.Services.Common;
using Zynga.Core.Util;
using Facebook.Unity;

public class NewEconomyManager : IDependencyInitializer
{
	private const string PURCHASE_DIALOG_PREF = "new_econ_mgr_purchase_dialog";
	private const float PURCHASE_TIMEOUT_SECONDS = 60.0f;
	private const float FAKE_PURCHASE_WAIT_TIME = 2.0f;
	private const string RAINY_DAY_EVENT = "rainy_day_purchased";
	private const string RICH_PASS_PURCHASED_EVENT = "rp_pass_type_purchased";
	private const string PREMIUM_SLICE_PURCHASED_EVENT = "premium_slice_purchased";
	private const string BONUS_GAME_PURCHASED = "rewardable_purchased";
	private const string ADJUST_TRACK_PURCHASE = "adjusttrackpurchase";
	private const string ADJUST_TRACK_TYPE = "adjust_track_type";

	public static bool Initialized = false;
	private static Dictionary<string, PurchasableItem> iapPackages = new Dictionary<string, PurchasableItem>();

	private InitializationManager initMgr;

	private static bool _firstLoad = true;				// This flag is cleared when the first complete load of economy data has completed.

	public static bool FirstLoad
	{
		get { return _firstLoad;}
	}

	public static bool PurchasesEnabled
	{
		get { return _purchasesEnabled;}
	}

	private static bool _purchasesEnabled = false;		// Everything has loaded successfully and it's okay to buy.

	public InAppPurchaserBase InAppPurchase {
		get
		{
			if (PackageProvider.Instance.Payments.InAppPurchaser != null)
			{
				return PackageProvider.Instance.Payments.InAppPurchaser;
			}
			else
			{
				return null;
			}
		}
	}

	public static bool isPurchaseInProgress
	{
		get { return _purchaseInProgress;}
	}

	private static bool _purchaseInProgress = false;
	private static bool _isLifecycleSale = false;
	private static PurchaseFeatureData.Type purchaseInProgressType = PurchaseFeatureData.Type.NONE;
	private static RewardPurchaseOffer inProgressPurchaseOffer = null;

	public static NewEconomyManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new NewEconomyManager();
			}
			return _instance;
		}
	}

	#if LOCAL_IAP_TESTING
	// For testing purchase flow without any backend calls in Editor
	// For testing purchase flow without any backend calls in Editor
	private PurchaseCompleteEventArgs fakePurchaseCurrency;

	private int fakePurchaseBonusPercent = 0;
	private GameTimer fakePurchaseFinishTime = null;
	private int fakePurchaseSaleBonusPercent = 0;
	private bool fakePurchaseIsBuyPage = false;
	#endif

	private static float purchaseTime = 0;
	private static string purchaseTransactionPending = "";
	private static int purchasesFailedWaiting = 0;
	private static DialogBase clickedDialog = null;	// The stack top dialog that was clicked when starting a purchase, so we know what to close when it succeeds.
	private static NewEconomyManager _instance;
	private Dictionary<string,string> refDict = null;

	// Sends purchase to Provider.
	// Todo: Move arguments to this function into a dictionary?
	public static void startPurchase(
		PurchasableItem item, 
		string transactionName, 
		int bonusPercent = 0, 
		string gameToUnlock = "", 
		bool isMultiplierPurchase = false, 
		int buyCreditsPagePackageIndex = -1, 
		string packageClass = "", 
		int bonusSalePercent = 0, 
		string economyTrackingNameOverride = null, 
		string economyTrackingVariantOverride = null, 
		bool isLifecycleSale = false, 
		string collectablePack = "", 
		PurchaseFeatureData.Type purchaseType = PurchaseFeatureData.Type.NONE, 
		int economyVersion = -1,
		int streakSalePackageIndex = -1,
		string buffKey = "",
		long seedValue = -1,
		string themeName = "",
		string lottoBlastKey = "",
		string petTreatKeyName = "",
		string bundleSaleId = "",
		RewardPurchaseOffer purchaseRewardable = null
	)
	{
		_isLifecycleSale = isLifecycleSale;
		Packages.Coroutines.Begin(Instance.startPurchaseCoroutine(item, transactionName, bonusPercent, gameToUnlock, isMultiplierPurchase, buyCreditsPagePackageIndex, packageClass, bonusSalePercent, economyTrackingNameOverride, economyTrackingVariantOverride, collectablePack, purchaseType, economyVersion, streakSalePackageIndex, buffKey, seedValue, themeName, lottoBlastKey, petTreatKeyName, bundleSaleId, purchaseRewardable));
	}

	// Use a coroutine so we can wait for the "Purchase in Progress" dialog to finish showing
	// before starting the purchase with the system, to prevent losing focus and pausing the
	// app before the "Purchase in Progress" dialog is finished showing.
	private IEnumerator startPurchaseCoroutine(
		PurchasableItem item, 
		string transactionName, 
		int bonusPercent, 
		string gameToUnlock, 
		bool isMultiplierPurchase, 
		int buyCreditsPagePackageIndex, 
		string packageClass, 
		int bonusSalePercent, 
		string economyTrackingNameOverride, 
		string economyTrackingVariantOverride, 
		string collectablePack, 
		PurchaseFeatureData.Type purchaseType, 
		int economyVersion,
		int streakSalePackageIndex = -1,
		string buffKey = "",
		long seedValue = -1,
		string themeName = "",
		string lottoBlastKey = "",
		string petTreatKeyName = "",
		string bundleSaleId = "",
		RewardPurchaseOffer purchaseRewardable = null
		)
	{
		//Locks out button mashers
		if (_purchaseInProgress)
		{
			if (Data.debugEconomy)
			{
				Debug.LogWarning("NewEconomyManager::startPurchaseCoroutine - Purchase already in progress.");
			}
			yield break;
		}

		if (item == null || !_purchasesEnabled)
		{
			// Immediately fail this transaction:
			Debug.LogError("NewEconomyManager::startPurchase - Invalid purchase setup.");
			Userflows.flowEnd(purchaseTransactionPending, false, "invalid-setup");


			GenericDialog.showDialog(
				Dict.create(
					D.TITLE, Localize.text("error"),
					D.MESSAGE, Localize.text("products_failed"),
					D.REASON, "new-economy-manager-products-failed",
					D.STACK, true
				),
				SchedulerPriority.PriorityType.IMMEDIATE
			);
			StatsManager.Instance.LogCount("debug", "purchasing", "products_failed");
			//restartMeco();

			yield break;
		}

		Bugsnag.LeaveBreadcrumb("NewEcononmyManager::StartPurchaseCourtine -- item is valid and purchases enabled");

		//Once per session warning after a purchase times out that they shouldn't necessarily make a second purchase right away
		if (purchasesFailedWaiting == 1)
		{
			if (Data.debugEconomy)
			{
				Debug.LogWarning(string.Format("NewEconomyManager::startPurchaseCoroutine - Already have {0} purchases waiting.", purchasesFailedWaiting));
			}

			GenericDialog.showDialog(
				Dict.create(
					D.TITLE, Localize.text("pending"),
					D.MESSAGE, Localize.text("pending_purchase_warning"),
					D.OPTION1,  Localize.textUpper("yes"),
					D.OPTION2,  Localize.textUpper("no"),
					D.REASON, "new-economy-manager-pending-purchase",
					D.CALLBACK, new DialogBase.AnswerDelegate((args) =>
						{
							purchasesFailedWaiting++;
							if (args != null && args.ContainsKey(D.ANSWER) && (args[D.ANSWER] as string) == "1")
							{
								Bugsnag.LeaveBreadcrumb("NewEcononmyManager::StartPurchaseCouroutine -- calling start purchase after pending dialog callback");
								startPurchase(item, transactionName, bonusPercent, gameToUnlock, isMultiplierPurchase, buyCreditsPagePackageIndex, packageClass, bonusSalePercent, economyTrackingNameOverride, economyTrackingVariantOverride, false, collectablePack, streakSalePackageIndex: streakSalePackageIndex, seedValue:seedValue, themeName: themeName, lottoBlastKey: lottoBlastKey);
							}
						}
					)
				),
				SchedulerPriority.PriorityType.IMMEDIATE
			);

			yield break;
		}


		Bugsnag.LeaveBreadcrumb("NewEcononmyManager::StartPurchaseCourtine -- no purchases waiting");

		// Save the name of this transaction so we can possibly fail it later,
		// then store the dollar value of this transaction so we can track it's impact:
		purchaseTransactionPending = transactionName;
		string msg = string.Format("NewEconomyManager::startPurchaseCoroutine - Item: {0} Trans: {1} Bonus: {2} Unlock: {3}", item.GameSkuId, transactionName == null ? "" : transactionName, bonusPercent, gameToUnlock == null ? "" : gameToUnlock);
		Bugsnag.LeaveBreadcrumb(msg);
		Userflows.logStep("passed-precheck", purchaseTransactionPending);
		

		// Remember the player's VIP level when making the purchase,
		// so it can be used on the purchase confirmation screen,
		// just in case the purchase itself puts the player into the
		// next level, but we still need to show the level at the time of purchase.
		BuyCreditsConfirmationDialog.vipNewLevelForPurchase = SlotsPlayer.instance.vipNewLevel;

		// Store the current dialog (if any) before showing the "purchase in progress" dialog
		PreferencesBase unityPrefs = SlotsPlayer.getPreferences();
		if (Dialog.instance != null && Dialog.instance.currentDialog != null)
		{
			unityPrefs.SetString(PURCHASE_DIALOG_PREF, Dialog.instance.currentDialog.name);
			clickedDialog = Dialog.instance.currentDialog;
		}
		else
		{
			unityPrefs.SetString(PURCHASE_DIALOG_PREF,"");
			clickedDialog = null;
		}

		unityPrefs.Save();


		// Proceed...
		{

			Bugsnag.LeaveBreadcrumb("NewEcononmyManager::StartPurchaseCoroutine -- Dialog show");

			// Immediately show the "purchase is progress" message.
			// This must be done before setting _purchaseInProgress to true,
			// since doing that prevents dialogs from opening during the purchase process.
			GenericDialog.showDialog(
				Dict.create(
					D.TITLE, Localize.text("purchasing"),
					D.MESSAGE, Localize.text("purchase_in_progress"),
					D.REASON, "new-economy-manager-purchase-in-progress",
					D.IS_WAITING, true
				),
				SchedulerPriority.PriorityType.IMMEDIATE
			);

			Userflows.logStep("wait-dialog-start", purchaseTransactionPending);

			_purchaseInProgress = true;
			purchaseInProgressType = purchaseType;
			inProgressPurchaseOffer = purchaseRewardable;
			purchaseTime = 0;

			StatsManager.Instance.LogCount("debug", "purchasing", "init_purchase", item.Name);
			Dictionary<string, object> additionalProperties = new Dictionary<string, object>();
			refDict = new Dictionary<string, string>();

			if (economyTrackingNameOverride != null)
			{
				refDict.Add("p", economyTrackingNameOverride);
			}
			else if (clickedDialog != null)
			{
				refDict.Add("p", clickedDialog.economyTrackingName);
			}

			refDict.Add("g", (economyTrackingVariantOverride != null) ? economyTrackingVariantOverride : GameState.currentStateOrKeyName);

			ServiceSession session = null;
			if (ZdkManager.Instance != null && ZdkManager.Instance.Zsession != null)
			{
				session = ZdkManager.Instance.Zsession;
			}
			if (session != null && session.Zid != null)
			{
				refDict.Add("zid", session.Zid.ToString());
			}
			if (bonusPercent != 0)
			{
				refDict.Add("bonus_percent", bonusPercent.ToString());
			}
			if (buyCreditsPagePackageIndex > -1)
			{
#if LOCAL_IAP_TESTING
				fakePurchaseIsBuyPage = true;
#endif
				refDict.Add("buy_page_package_index", buyCreditsPagePackageIndex.ToString());
			}
			// Spin it rich sale percentages are also on the popcorn sale dialog.
			refDict.Add("buy_page_sale_bonus_pct", bonusSalePercent.ToString());

			if (!string.IsNullOrEmpty(bundleSaleId))
			{
				refDict.Add("bundle_sale_id", bundleSaleId);
			}

			if (PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_BUY_PAGE_KEY))
			{
				refDict.Add("powerup_bonus", BuyPageBonusPowerup.salePercent.ToString());
			}

			if (economyVersion >= 0)
			{
				string economyJsonString = string.Format("{{\"economy_version\" : {0}}}", economyVersion);
				JSON economyJson = new JSON(economyJsonString);
				refDict.Add("prize_pop", economyJson.ToString());
			}

			if (streakSalePackageIndex >= 0)
			{
				refDict.Add("streak_sale_package_index", streakSalePackageIndex.ToString());

				if (StreakSaleManager.nextItemIsFree)
				{
					string freeItemPackageIndex = (streakSalePackageIndex + 1).ToString();
					string freeItemCoinPackage = StreakSaleManager.freeCoinPackage;
					string freeItemBonus = StreakSaleManager.freeBonusPct;
					string freeItemBaseBonus = StreakSaleManager.freeBaseBonusPct;
					string freeItemCollectiblePack = StreakSaleManager.freeCardPack;

					string streakSaleFreeRewardValueString = "{\"package_index\":" + freeItemPackageIndex + ",\"coin_package\":\"" + freeItemCoinPackage + "\",\"bonus_pct\":" + freeItemBonus + "\",\"base_bonus_pct\":" + freeItemBaseBonus + ",\"collectible_pack\":\"" + freeItemCollectiblePack + "\"}";
					refDict.Add("streak_sale_free_reward", streakSaleFreeRewardValueString);

					StreakSaleManager.nextItemIsFree = false;
				}
			}

			Bugsnag.LeaveBreadcrumb("NewEcononmyManager::StartPurchaseCoroutine -- build url");

			// Append the raw server name to the purchase:
			System.Uri serverUri = new System.Uri(Data.serverUrl);
			string uriWithoutScheme = serverUri.Host;
			string[] levels = null;
			if (!string.IsNullOrEmpty(uriWithoutScheme))
			{
				levels = uriWithoutScheme.Split('.');
			}
			if (levels != null && levels.Length > 0)
			{
				refDict.Add("server_env", levels[0]);
			}

			if (!string.IsNullOrEmpty(gameToUnlock))
			{
				refDict.Add("unlock_slot", gameToUnlock);
			}

			if (!string.IsNullOrEmpty(collectablePack))
			{
				refDict.Add("collectible_pack", collectablePack);
			}

			if (!string.IsNullOrEmpty(buffKey))
			{
				refDict.Add("buff_key", buffKey);
			}

			if (isMultiplierPurchase)
			{
				refDict.Add("is_multiplier_purchase", "1");
			}

			if (!string.IsNullOrEmpty(packageClass))
			{
				refDict.Add("class", packageClass);
			}

			if (seedValue > -1)
			{ 
				refDict.Add("seed_value", seedValue.ToString());
			}

			if (!string.IsNullOrEmpty(themeName))
			{
				refDict.Add("theme_name", themeName);
			}
			
			if (!string.IsNullOrEmpty(lottoBlastKey))
			{
				refDict.Add("lotto_blast_key", lottoBlastKey);
			}

			if (!string.IsNullOrEmpty(petTreatKeyName))
			{
				refDict.Add("pet_treat", petTreatKeyName);
			}

			string refData = JSON.createJsonString("", refDict);
			additionalProperties.Add("ref", refData);

			// Wait to make sure the "purchase in progress" dialog is finished showing before starting the purchase with the system.
			Userflows.logStep("wait", purchaseTransactionPending);
			yield return new WaitForSeconds(1.0f);
			// Also wait anymore necessary to make sure the dialog isn't still in the opening process,
			// mainly for devices that may be really slow and had a realtime lag longer than 1 second to show the dialog.
			while (Dialog.instance != null && Dialog.instance.isOpening)
			{
				yield return null;
			}

			Bugsnag.LeaveBreadcrumb("NewEcononmyManager::StartPurchaseCoroutine -- Initiating payment");
#if LOCAL_IAP_TESTING
			fakePurchaseBonusPercent = bonusPercent;
			fakePurchaseSaleBonusPercent = bonusSalePercent;
#else
			// Send the payment information to the server:
			PrePaymentAction.prePayment(getItemShortCode(item, item.GameSkuId.ToStringInvariant()), refData);
			Userflows.logStep("prepay-action", purchaseTransactionPending);
#endif

			// Create payment Request
			PurchaseRequest purchaseRequest = new PurchaseRequest(item.ProductId, additionalProperties);

			// Initiate the payment:

			yield return InAppPurchase.Purchase(purchaseRequest);
			
			Userflows.logStep("iap-request", purchaseTransactionPending);
			
			// Update the error strings - and then log the attempted purchase with the server:
			Server.makeErrorBaseStrings();

			if (Glb.serverLogPayments)
			{
				// Internal logging for this payment:
				Server.sendLogInfo("PURCHASE",
					string.Format("[purchase_event=PURCHASE_START] [package={0}] [ref_data={1}]", item.GameSkuId, refData));
			}
#if LOCAL_IAP_TESTING
			Packages.Coroutines.Begin(fakePurchaseRoutine());
#else
			Packages.Coroutines.Begin(purchaseInProgressRoutine());
#endif
		}
	}

	private IEnumerator purchaseInProgressRoutine()
	{
		while(_purchaseInProgress)
		{
			// Not sure what deltaTime is after the app loses focus for Apple IAP UI
			purchaseTime += (Time.deltaTime < 1.0f) ? Time.deltaTime : 1.0f;

			if (purchaseTime > PURCHASE_TIMEOUT_SECONDS)
			{
				Debug.LogError("NewEconomyManager::Update - Purchase timed out.");
				_purchaseInProgress = false;
				purchaseInProgressType = PurchaseFeatureData.Type.NONE;
				inProgressPurchaseOffer = null;
				ServerAction.clearFastUpdateMode();
				if (clickedDialog != null)
				{
					clickedDialog.purchaseFailed(true);
				}
				Dialog.close();	// Waiting dialog

				// Show the canceled purchase dialog, which allows users to contact support.
				CanceledPurchaseDialog.showDialog();

				GenericDialog.showDialog(
					Dict.create(
						D.TITLE, Localize.text("payment_wait"),
						D.MESSAGE,  Localize.text("payment_long_time"),
						D.REASON, "new-economy-manager-payment-wait"
					),
					SchedulerPriority.PriorityType.IMMEDIATE
				);

				StatsManager.Instance.LogCount("debug", "purchasing", "purchase_wait");

				if (Glb.serverLogPayments)
				{
					Server.sendLogInfo("PURCHASE", "[purchase_event=PURCHASE_TIMEOUT] Critical failure.");
				}
				if (!string.IsNullOrEmpty(purchaseTransactionPending))
				{
					Userflows.flowEnd(purchaseTransactionPending, false, "timeout");
					purchaseTransactionPending = "";
				}
				// This variable is used to show a popup about pending purchases
				purchasesFailedWaiting++;
			}
			else
			{
				// If we are still waiting and the timeout hasn't passed yet, then wait a frame.
				yield return null;
			}
		}
		yield break;
	}

	#if LOCAL_IAP_TESTING

	private IEnumerator fakePurchaseRoutine()
	{
		while(_purchaseInProgress)
		{
			if (fakePurchaseFinishTime != null && fakePurchaseFinishTime.isExpired)
			{
				Debug.Log("NewEconomyManager::Update - Completing fake purchase.");

				fakePurchaseFinishTime = null;
				JSON fakePurchase = null;

				var item = getPurchasableItemFromEvent(fakePurchaseCurrency);
				PurchasablePackage itemPackage = item.IsSet ? PurchasablePackage.getNewBySku(item.Value.GameSkuId) : null;


				// Look up the PurchasablePackage that we just attempted to buy:
				if (itemPackage != null)
				{
					long totalCredits = itemPackage.totalCredits(fakePurchaseBonusPercent, fakePurchaseIsBuyPage, fakePurchaseSaleBonusPercent);
					
					if (fakePurchaseIsBuyPage)
					{
						// We need to include the general multiplier in the fake amount so it matches what the dialog said.
						totalCredits *= PurchaseFeatureData.findBuyCreditsMultiplier();
					}
					
					long vipCredits = itemPackage.bonusVIPCredits();
					string jsonString = "{" +
						"\"vip_credits\":" + vipCredits +
						",\"vip_points\":" + itemPackage.vipPoints() +
						",\"popcorn_package_key_name\":\"" + itemPackage.keyName + "\"";

					jsonString += string.Format(",\"credits\":{0}", totalCredits);

					jsonString += "}";

					fakePurchase = new JSON(jsonString);
				}

				if (fakePurchase != null)
				{
					creditPurchase(fakePurchase);
				}
				else
				{
					if (item.IsEmpty)
					{
						Debug.LogError(string.Format("NewEconomyManager::Update - Could not complete (fake) purchase of package: {0}", "NULL fakePurchaseCurrency or PurchasableItem"));
					}
					else
					{
						Debug.LogError(string.Format("NewEconomyManager::Update - Could not complete (fake) purchase of package: {0}",item.Value.Name));
					}

					// Go back to the dialog that had the buy button.
					_purchaseInProgress = false;
					purchaseInProgressType = PurchaseFeatureData.Type.NONE;
					inProgressPurchaseOffer = null;

					Dialog.close();	// The waiting dialog
					if (clickedDialog != null)
					{
						clickedDialog.purchaseFailed(false);
					}

					// Show the canceled purchase dialog, which allows users to contact support.
					CanceledPurchaseDialog.showDialog();
				}
			}
			else
			{
				yield return null;
			}
		}
		yield break;
	}

	#endif

	#region ISVDependencyInitializer implementation
	// This method should be implemented to return the set of class type definitions that the implementor
	// is dependent upon.
	// is dependent upon.
	public System.Type[] GetDependencies()
	{
		// Needs SocialManager in order to do FB payments initialization
		return new System.Type[] { typeof(AuthManager), typeof(SocialManager) };
	}

	// This method should contain the logic required to initialize an object/system.  Once initialization is
	// complete, the implementing class should call the "mgr.InitializationComplete(this)" method to signal
	// that downstream dependencies can be initialized.
	public void Initialize(InitializationManager mgr)
	{
		_purchasesEnabled = false;
		iapPackages = new Dictionary<string, PurchasableItem>();
		initMgr = mgr;

		// Handle the callback
		// Unsubscribing first to ensure double initialization doesn't take place

		// Perform tasks after initialization. The catalog of PurchasableItem objects is now available
		string msg = "NewEconomyManager::OnSuccess - Economy Startup Success";
		Bugsnag.LeaveBreadcrumb(msg);

		Server.registerEventDelegate("item_purchased", creditPurchase, true);
		Server.registerEventDelegate("popcorn_purchased", creditPurchase, true);
		Server.registerEventDelegate("multiplier_purchased", creditPurchase, true);
		Server.registerEventDelegate(RAINY_DAY_EVENT, creditPurchase, true);
		Server.registerEventDelegate(RICH_PASS_PURCHASED_EVENT, onPassPurchased, true);;
		Server.registerEventDelegate(PREMIUM_SLICE_PURCHASED_EVENT, onPremiumSlicePurchased, true);
		Server.registerEventDelegate(BONUS_GAME_PURCHASED, onBonusGamePurchased, true);

		Initialized = true;

		// When startup is complete, we are supposedly guaranteed that there was a successful catalog load.
		// However, we may need to wait for global data to be in place, hence the delay coroutine.

		Packages.Coroutines.Begin(waitForGlobalData());

		InAppPurchase.PurchaseCompleted -= HandleOnPurchaseCompleted;
		InAppPurchase.PurchaseCompleted += HandleOnPurchaseCompleted;

		InAppPurchase.PurchaseCancelled -= HandleOnPurchaseCancelled;
		InAppPurchase.PurchaseCancelled += HandleOnPurchaseCancelled;

		InAppPurchase.PurchaseFailed -= HandleOnPurchaseFailed;
		InAppPurchase.PurchaseFailed += HandleOnPurchaseFailed;

		InAppPurchase.PurchaseWarningOccurred -= HandleOnPurchaseWarning;
		InAppPurchase.PurchaseWarningOccurred += HandleOnPurchaseWarning;

		InAppPurchase.PurchaseRestored -= HandleOnPurchaseRestored;
		InAppPurchase.PurchaseRestored += HandleOnPurchaseRestored;

		InAppPurchase.CatalogUpdated -= HandleCatalogUpdated;
		InAppPurchase.CatalogUpdated += HandleCatalogUpdated;


		if (Glb.serverLogPayments)
		{
			if (Instance.randomLoggingValue() == 0) {
				Server.sendLogInfo("purchase_event", "[INITIALIZE INAPPPURCHASE]");
			}
		}
	
		if (initMgr != null)
		{
			initMgr.InitializationComplete(this);
		}

	}

	// short description of this dependency for debugging purposes
	public string description()
	{
		return "NewEconomyManager";
	}

	#endregion ISVDependencyInitializer implementation

	public static PurchasableItem getZyngaEconomyItemByPackageName(string creditPackage)
	{
		if (iapPackages.ContainsKey(creditPackage))
		{
			return iapPackages[creditPackage];
		}

		return null;
	}

	void HandleCatalogUpdated (object sender, CatalogEventArgs e)
	{
		if (Glb.popcornSalePackages != null)
		{
			populateAllIAP();
			PurchasablePackage.populateAll(Glb.allPackages);
		}
	}

	void HandleOnPurchaseRestored (object sender, PurchaseRestoredEventArgs e)
	{
		// TODO How do I get the item from PurchaseRestoredEventArgs?
	}

	void HandleOnPurchaseWarning (object sender, PurchaseWarningEventArgs e)
	{
		var item = getPurchasableItemFromEvent(e);
		string name = item.IsSet ? item.Value.Name : e.ProductId;
		string description = item.IsSet ? item.Value.Description : string.Empty;
		Debug.LogWarningFormat("Purchase Delay Name={0} Desc={1}", name, description);
		ServerAction.setFastUpdateMode("item_purchased");
		ServerAction.setFastUpdateMode("popcorn_purchased");
		StatsManager.Instance.LogCount("debug", "purchasing", "warning_purchase", name);

		// TODO/MAYBE show dialog informing user that there is a temporary problem in the purchase flow, it will be
		// retried and may succeed in the future.
	}

	void HandleOnPurchaseCancelled (object sender, PurchaseCanceledEventArgs e)
	{
		if (e != null)
		{
			var item = getPurchasableItemFromEvent(e);

			string name = item.IsSet ? item.Value.Name : e.ProductId;
			string description = item.IsSet ? item.Value.Description : string.Empty;
			string requestId = e.PurchaseRequestInfo.IsSet ? e.PurchaseRequestInfo.Value.RequestId : "Missing Request Id";

			string msg = string.Format("NewEconomyManager::OnPurchaseCancel - Package: Name={0} Desc={1} id={2}", name, description, requestId);
			Log.log(msg, Color.green);

			StatsManager.Instance.LogCount("debug", "purchasing", "cancel_purchase", name);

			if (Glb.serverLogPayments)
			{
				// Log this cancelled transaction:
				object refData = "";
				if (e.PurchaseRequestInfo.IsSet && e.PurchaseRequestInfo.Value.Metadata.ContainsKey("ref"))
				{
					refData = e.PurchaseRequestInfo.Value.Metadata["ref"];
				}
				Server.sendLogInfo("purchase_event", string.Format("[purchase_event=PURCHASE_CANCELED] [package={0}] [ref_data={1}]", name, refData.ToString()));
			}
		
		}
		_purchaseInProgress = false;
		_isLifecycleSale = false;
		purchaseInProgressType = PurchaseFeatureData.Type.NONE;
		inProgressPurchaseOffer = null;

		if (clickedDialog != null) {
			clickedDialog.purchaseCancelled ();
		}

		// Don't bother trying if there is no dialog.
		if (Dialog.instance.currentDialog != null)
		{
			Dialog.close();	// Waiting dialog
		}

		// Show the canceled purchase dialog, which allows users to contact support.
		CanceledPurchaseDialog.showDialog();

		if (!string.IsNullOrEmpty (purchaseTransactionPending)) {
			Userflows.flowEnd(purchaseTransactionPending, true, "cancelled");
			purchaseTransactionPending = "";
		}
		if (Glb.serverLogPayments) {
			Server.sendLogInfo("purchase_event", "[PURCHASE_CANCELED] Package is null");
		}
	}

	void HandleOnPurchaseFailed (object sender, PurchaseFailedEventArgs e)
	{
		if (sender == null || e == null)
		{
			Debug.LogError("HandleOnPurchaseFailed: returned null args");
			return;
		}

		var item = getPurchasableItemFromEvent(e);
		string requestId = e.PurchaseRequestInfo.IsSet ? e.PurchaseRequestInfo.Value.RequestId : "Missing Request Id";

		string msg = "NewEconomyManager::OnPurchaseError - Error, no valid transaction or item information";

		if (item.IsSet)
		{		
			msg = string.Format
				(
					"NewEconomyManager::OnPurchaseError - Error: Name={0} Desc={1} id={2} error={3}"
				    , item.Value.Name
				    , item.Value.Description
				    , requestId
					, e.ErrorMessage
				);

			Log.log(msg, Color.green);
			Debug.LogError(msg);
		}
		else
		{
			Bugsnag.LeaveBreadcrumb(msg);
		}	  
		
		_purchaseInProgress = false;
		_isLifecycleSale = false;
		purchaseInProgressType = PurchaseFeatureData.Type.NONE;
		inProgressPurchaseOffer = null;

		if (clickedDialog != null)
		{
			clickedDialog.purchaseFailed(false);
		}
		Dialog.close();	// Waiting dialog
		
		// Show the canceled purchase dialog, which allows users to contact support.
		CanceledPurchaseDialog.showDialog();
		if (item.IsSet) { 
			StatsManager.Instance.LogCount ("debug", "purchasing", "error_purchase", item.Value.Name, requestId, e.ErrorMessage);
		} else {
			StatsManager.Instance.LogCount ("debug", "purchasing", "error_purchase");
		}
	}

	void HandleOnPurchaseCompleted (object sender, PurchaseCompleteEventArgs e)
	{
		string msg = string.Format("NewEconomyManager::HandlePurchase - Purchase is: {0}", e.ReceiptData);
		Bugsnag.LeaveBreadcrumb(msg);

		#if LOCAL_IAP_TESTING
		fakePurchaseCurrency = e;
		fakePurchaseFinishTime = new GameTimer(FAKE_PURCHASE_WAIT_TIME);
		#endif

		ServerAction.setFastUpdateMode("item_purchased");
		ServerAction.setFastUpdateMode("popcorn_purchased");

		//Must be called on all purchases after they succeed or fail.
		InAppPurchase.FinishTransaction(e.TransactionId);
		StatsManager.Instance.LogCount("debug", "purchasing", "handle_purchase", e.ReceiptData);

	}

	// Wait for global data before syncing up the Meco catalog with our global data purchases.
	// Note: we intentionally only sync the catalog with global data at startup, for safety.
	private IEnumerator waitForGlobalData()
	{
		// The game is completely unloadable if global data doesn't arrive,
		// so it is safe to wait for it, checking each frame for the flag.
		while (!Data.isGlobalDataSet)
		{
			yield return null;
		}

		populateAllIAP();
		// CRC - this is our new purchasable package:
		PurchasablePackage.populateAll(Glb.allPackages);

		string msg = "NewEconomyManager::waitForGlobalData - Economy catalog successfully synced.";
		Bugsnag.LeaveBreadcrumb(msg);

		// Only update these flags when everything has finished loading:
		_purchasesEnabled = true;
		_firstLoad = false;
	}

	//Fucntion that returns a random logging value
	// mainly used to throttle logging
	private int randomLoggingValue()
	{
		int zid = UnityEngine.Random.Range(1, 100);
		int rand = zid % Glb.RANDOM_LOGGING_VALUE;
		return rand;

	}

	public void pauseHandler(bool pause)
	{
		if (InAppPurchase != null)
		{
			if (!pause)
			{
				InAppPurchase.OnResume();
			}
		}
	}

	// Takes the  Catalog and populates the dictionary with Credit Package as the key
	public static void populateAllIAP()
	{
		string msg = "NewEconomyManager::populateAllIAP - Begin.";
		Bugsnag.LeaveBreadcrumb(msg);

		foreach(PurchasableItem item in Instance.InAppPurchase.PurchasableItems)
		{
			if (Data.debugEconomy)
			{
				Debug.LogFormat("PurchasableItem Name={0} Desc={1} Adjustments={2}", item.Name, item.Description, Json.Serialize(item.Adjustments));
			}

			if (item.Metadata != null)
			{
				// All products must have a credits package as part of User Data to
				// map the Economy.Item to PurchasebleItems sent down from SCAT
				if (item.Metadata.ContainsKey("zpayments_grant_code"))
				{
					string package = (string)item.Metadata["zpayments_grant_code"];
					if (!iapPackages.ContainsKey(package))
					{
						iapPackages.Add(package, item);
					}
					else
					{
						iapPackages[package] = item;
					}
					if (Data.debugEconomy)
					{
						Debug.Log(string.Format("NewEconomyManager::populateAllIAP - IAP Package received: {0}", package));
					}

					// This is an important sanity check to make sure our purchase data is set up correctly:
					if (package != item.Name)
					{
						// The SKU of the item *should* be set up so that it matches the grant code.
						// This previously happened due to an item being incorrectly renamed in MECO.
						if (Data.debugEconomy)
						{
							Debug.LogError(string.Format("NewEconomyManager::populateAllIAP - Mismatch with grant code: {0} vs SKU: {1}", package, item.GameSkuId));
						}
					}
				}
				else
				{
					Debug.LogError(string.Format("NewEconomyManager::populateAllIAP - Incomplete Package Specified for {0}", item.Name));
				}
			}
			else
			{
				Debug.LogError(string.Format("NewEconomyManager::populateAllIAP - No Package Specified for {0}", item.Name));
			}
		}

		msg = string.Format("NewEconomyManager::populateAllIAP - Added: {0} items from {1} total.", iapPackages.Count, Instance.InAppPurchase.PurchasableItems.Count);
		Bugsnag.LeaveBreadcrumb(msg);
	}

	public Option<PurchasableItem> getPurchasableItemFromEvent(PurchaseEventArgs args)
	{
		PurchasableItem item = (args != null && args.PurchaseRequestInfo.IsSet) ? args.PurchaseRequestInfo.Value.PurchasableItem : null;
		if (item == null && args != null && args.ProductId != null)
		{
			InAppPurchase.PurchasableItemsByProductId.TryGetValue(args.ProductId, out item);
		}

		if (args != null && args.ProductId == null)
		{
			Bugsnag.LeaveBreadcrumb("getPurchasableItemFromEvent: ProductId is null");
		}
		return Option<PurchasableItem>.Create(item);
	}

	// Final Callback sent from server when item finishes purchasing
	// Dont show credit purchase dialog, new Sub callback
	public static void creditPurchase(JSON data)
	{
		ServerAction.clearFastUpdateMode();

		if (data == null)
		{
			if (!string.IsNullOrEmpty(purchaseTransactionPending))
			{
				Userflows.flowEnd(purchaseTransactionPending, false, "invalid-server-data");
				purchaseTransactionPending = "";
			}
			
			Debug.LogError("NewEconomyManager.creditPurchase() callback called with no data!");
			return;
		}
		
		Userflows.logStep("server-response", purchaseTransactionPending);
		
		string type = data.getString("type", "");
		string eventId = data.getString("event", "");

		long creditsAdded = data.getLong("credits", 0);

		Debug.LogFormat("Girish: credits added {0}", creditsAdded.ToString());
		int vipAdded = data.getInt("vip_points", 0);
		long vipCredits = data.getLong("vip_credits", 0);
		long newPlayerTotal = data.getLong("player_total_credits", 0); // Total credits the player should now have.
		long baseCredits = data.getLong("premium_credits", 0);
		string gameKey = data.getString("game_key", ""); // Select Game Unlock
		string packageKey = data.getString("popcorn_package_key_name", ""); // This ?might? contain the ID of the credits package we just purchased.
		string feature = data.getString("feature", "");
		string xpromoTarget = data.getString("xpromo_target", "");

		// Percentages for BuyPage v3
		int bonusPercent = data.getInt("bonus_pct", 0);
		int saleBonusPercent = data.getInt("sale_bonus_pct", 0);
		int vipBonusPercent = data.getInt("vip_bonus_pct", 0);
		bool isJackpotEligible = false;

		//If the event id is not null then send the event immediately back to the server
		if (eventId != "")
		{
			CreditAction.acceptPurchasedItem(eventId);
		}

		if (ExperimentWrapper.BuyPageProgressive.jackpotDaysIsActive) //Need to look through the packages to see if the one we just purchased was eligible for the jackpot
		{
			PurchaseFeatureData buyCreditsData = PurchaseFeatureData.BuyPage;
			if (buyCreditsData != null)
			{
				List<CreditPackage> packages = buyCreditsData.creditPackages;
				if (packages != null)
				{
					for (int i = 0; i < packages.Count; i++)
					{
						if (packages[i].purchasePackage != null)
						{
							PurchasablePackage currentPackage = packages[i].purchasePackage;
							if (currentPackage != null && currentPackage.keyName == packageKey)
							{
								if (packages[i].isJackpotEligible)
								{
									isJackpotEligible = true;
									break;
								}
							}
						}
					}
				}
			}
		}

		// SKU game unlock.
		if (feature == "xpromo" && xpromoTarget != "")
		{
			LobbyGame skuGameUnlock = LobbyGame.find(gameKey);

			if (skuGameUnlock != null)
			{
				if (LobbyGame.skuGameUnlock != null &&
					skuGameUnlock != LobbyGame.skuGameUnlock)
				{
					Debug.LogError(
						"SKU Game Unlock: " + gameKey + " from server is different than " +
						LobbyGame.skuGameUnlock.keyName + " in client.");
				}
			}

			LobbyGame.skuGameUnlock = skuGameUnlock;
		}

		if (!string.IsNullOrEmpty(gameKey))
		{
			// This is a select game unlock event.
			// And this is not a credit purchase, so we want to get out of this function now.
			SlotsPlayer.unlockGame(eventId, gameKey);
			
			if (!string.IsNullOrEmpty(purchaseTransactionPending))
			{
				// Adding an additional field so we can track if this code is even used anymore
				Dictionary<string, string> extraFields = new Dictionary<string, string>();
				extraFields.Add("game_unlock_event", "true");
				Userflows.addExtraFieldsToFlow(purchaseTransactionPending, extraFields);
				Userflows.flowEnd(purchaseTransactionPending);

				purchaseTransactionPending = "";
			}
			
			return;
		}

		_purchaseInProgress = false;
		
		PurchaseFeatureData.Type purchaseType = purchaseInProgressType;
		RewardPurchaseOffer purchaseOffer = inProgressPurchaseOffer;
		purchaseInProgressType = PurchaseFeatureData.Type.NONE;
		inProgressPurchaseOffer = null;

		//if we closed a purchase dialog
		bool didClosePurchaseDialog = true;
		PreferencesBase unityPrefs = SlotsPlayer.getPreferences();
		string dialogName = unityPrefs.GetString(PURCHASE_DIALOG_PREF, "");
		if (clickedDialog != null)
		{
			if (clickedDialog.name != dialogName)
			{
				Debug.LogError("Clicked dialog does not match the last purchase dialog name");
			}

			DialogBase.PurchaseSuccessActionType purchaseSuccessActionType = clickedDialog.purchaseSucceeded(data, purchaseType);

			if (purchaseSuccessActionType == DialogBase.PurchaseSuccessActionType.closeDialog)
			{
				// We don't close the top dialog until the purchase is successful.
				// Do this before closing the waiting dialog so we don't see it sliding off or anything.
				// Closing it while it's not on top of the stack just silently removes it from the stack.
				Dialog.close(clickedDialog);
			}
			else if (purchaseSuccessActionType == DialogBase.PurchaseSuccessActionType.skipThankYouDialog)
			{
				// If we're not closing the clicked dialog now, then that dialog is
				// responsible for adding the credits instead of doing it here,
				// because it probably needs to do some fancy presentation first.
				didClosePurchaseDialog = false;
			}
			else if (purchaseSuccessActionType == DialogBase.PurchaseSuccessActionType.leaveDialogOpenAndShowThankYouDialog)
			{
				//Don't close the dialog, but still show the thank you dialog.
			}
		}
		else
		{
			//impossible to have a purchase dialog when we didn't click on anything
			//this case will happen if the user hard reloads the app in the middle of a purchase
			didClosePurchaseDialog = false;
		}

		// Don't bother trying if there is no dialog.
		if (Dialog.instance.currentDialog != null)
		{
			Dialog.close();	// The waiting dialog
		}

		#if LOCAL_IAP_TESTING
		PlayerAction.addCredits(creditsAdded);
		PlayerAction.addVIPPoints(vipAdded);
		#endif

		if (vipAdded > 0)
		{
			Userflows.logStep("vip", purchaseTransactionPending);
			SlotsPlayer.instance.addVIPPoints(vipAdded);	// VIP bonus percent is pre-calculated on the backend.
		}

		if (creditsAdded > 0)
		{
			Userflows.logStep("credits", purchaseTransactionPending);
#if UNITY_WEBGL
			if (!didClosePurchaseDialog)
			{
				//If you are in the one click experiment then there is no purchase dialog showing up
				//If this was a prize pop purchase then there was a purchase dialog so we want to respect its didClosePurchaseDialog value
				if(ExperimentWrapper.OneClickBuy.isInExperiment && (purchaseType != PurchaseFeatureData.Type.PRIZE_POP || purchaseType != PurchaseFeatureData.Type.BONUS_GAME))
				{
					didClosePurchaseDialog = true;
				}
			}
#endif
			if (didClosePurchaseDialog)
			{
				// Only add the credits now if we did close the purchase dialog,
				// because if we didn't close it, then the purchase dialog is
				// responsible for adding the credits after whatever presentation it's doing.


				// Validate the total credits to make sure that we are going to give them the correct amount of credits.
				long playerTotal = SlotsPlayer.creditAmount + creditsAdded + Server.totalPendingCredits;
				if (Data.liveData.getBool("DESYNC_CHECK_ON_PURCHASE", false) && playerTotal != newPlayerTotal)
				{
					// If the totals did not match up, throw an error and then only add to get us
					// to what the server says our new total should be.
					// this was added 2 years ago to work around https://jira.corp.zynga.com/browse/HIR-41007 a coin sync on purchase
					// and the cause is not known. The problem with this check is we do not know for sure if the
					// player_total_credits returned in the creditPurchase event data by the server will match our current credits because events returned
					// in the same batch of events as the creditPurchase event may not have not yet made the required credits.add calls since the event order
					// may vary.
					// This check then adjusts the credits thinking something is wrong, then the other events get handled and add the credits again and on spin
					// we get a desync, there should only be one desync when the player spins.
					// The feeeling is that after 2 years with all the refactoring that the original issue in HIR-41007 has probalby been fixed
					// and this workaround is not longer needed.
					Debug.LogErrorFormat("NewEconomyManager.cs -- creditPurchase -- found a total mismatch after adding credits to the user.");

					long creditsDifference = newPlayerTotal - SlotsPlayer.creditAmount;
					Debug.LogFormat("Girish: credits added 3 {0}", creditsAdded.ToString());
					SlotsPlayer.addCredits(creditsDifference, "purchase", true, false);
				}
				else
				{
					Debug.LogFormat("Girish: credits added 4 {0}", creditsAdded.ToString());
					// Otherwise just add the amount we are supposed to add.
					SlotsPlayer.addCredits(creditsAdded, "purchase", true, false);
				}

				// Only show a confirmation dialog if we closed the purchase dialog,
				// because if the purchase dialog is handling special presentation,
				// it is essentially confirmation of this purchase.
				switch (type)
				{
				default:
					// Show the standard confirmation dialog.
					Dict args = Dict.create(
						D.BONUS_CREDITS, (long)vipCredits,
						D.TOTAL_CREDITS, (long)creditsAdded,
						D.PACKAGE_KEY, packageKey,
						D.VIP_POINTS, vipAdded,
						D.DATA, data,
						D.BASE_CREDITS, baseCredits,
						D.BONUS_PERCENT, bonusPercent,
						D.SALE_BONUS_PERCENT, saleBonusPercent,
						D.VIP_BONUS_PERCENT, vipBonusPercent,
						D.IS_JACKPOT_ELIGIBLE, isJackpotEligible,
						D.TYPE, purchaseType,
						D.PAYLOAD, purchaseOffer
					);
					BuyCreditsConfirmationDialog.showDialog(args);
					break;
				}
			}
			else if (clickedDialog == null)
			{
				// if there was no purchase dialog open to be able to close, show confirmation here.
				Dict args = Dict.create(
					D.BONUS_CREDITS, (long)vipCredits,
					D.TOTAL_CREDITS, (long)creditsAdded,
					D.PACKAGE_KEY, packageKey,
					D.VIP_POINTS, vipAdded,
					D.DATA, data,
					D.BASE_CREDITS, baseCredits,
					D.BONUS_PERCENT, bonusPercent,
					D.SALE_BONUS_PERCENT, saleBonusPercent,
					D.VIP_BONUS_PERCENT, vipBonusPercent,
					D.IS_JACKPOT_ELIGIBLE, isJackpotEligible,
					D.SHOULD_HIDE_LOADING, false,
					D.TYPE, purchaseType,
					D.PAYLOAD, purchaseOffer
				);
				BuyCreditsConfirmationDialog.showDialog(args);

				if (!string.IsNullOrEmpty(dialogName))
				{
					Bugsnag.LeaveBreadcrumb("NewEcononmyManager: " + dialogName + " did not get closed properly");
					unityPrefs.SetString(PURCHASE_DIALOG_PREF, "");
					unityPrefs.Save();
				}
			}
		}

		// Extract some tracking data
		string transactionId = data.getString("transaction_id", "");
		string receiptId = data.getString("receipt_id", "");
		string receiptSig = data.getString("receipt_sig", "");
		string paidCurrency = data.getString("currency", "USD");
		float paidAmount = data.getFloat("amount", 0f);
		string clientId = data.getString("clientId", "");

// If webgl then no adjust tracking
#if !UNITY_WEBGL
		// Only record these if the paid amount is greater than 0.00
		if (paidAmount > 0f)
		{
			Userflows.logStep("paid", purchaseTransactionPending);

			Dictionary<string, string> extraFields = new Dictionary<string, string>();

			extraFields.Add("transactionId", transactionId);
			//This if check is here temporarily until the server returns the clientId
			// Update: server fix is out so just doing logging and no adjust tracking
			if (clientId.IsNullOrWhiteSpace())
			{
				extraFields.Add("clientId", "empty");
				SplunkEventManager.createSplunkEvent(ADJUST_TRACK_TYPE, ADJUST_TRACK_PURCHASE, extraFields);
			}
			else
			{
				extraFields.Add("clientId", clientId);
				// This check is here for the following edge case
				// if the user purchases on webgl and doesn't claim the credit
				// and opens the game in another platform, then it will track the webgl purchase
				// as an android or an ios one. 
				if (clientId != ((int)Zynga.Core.Platform.ClientId.WebGLFacebook).ToString())
				{
					extraFields.Add("tracking", "true");
					SplunkEventManager.createSplunkEvent(ADJUST_TRACK_TYPE, ADJUST_TRACK_PURCHASE, extraFields);
					UAWrapper.Instance.TrackPurchase(paidAmount, paidCurrency, transactionId, receiptId, receiptSig, packageKey);
				}
				else
				{
					extraFields.Add("tracking", "false");
					SplunkEventManager.createSplunkEvent(ADJUST_TRACK_TYPE, ADJUST_TRACK_PURCHASE, extraFields);
				}
			}
		}
#endif
		// Record this stat even if the paid amount was 0.00
		StatsManager.Instance.LogCount("debug", "purchasing", "credit_purchase", string.Format("{0}", paidAmount));

		if (purchasesFailedWaiting == 1)
		{
			// If the slow transaction finished without the user trying to start a new transaction,
			// then we never showed the "purcahse pending" dialog that this variable controls, so reset it for next time.
			// If we don't do this, then the next purchase attempt will show the "purchase pending" dialog
			// even though there is no pending purchase.
			purchasesFailedWaiting = 0;
		}

		if (Glb.serverLogPayments || Data.debugEconomy)
		{
			// Verbose logging of all the information we have about this purchase:
			string msg = string.Format("[package={0}] [credits={1}] [vip_points={2}] [vip_credits={3}] [base_credits={4}]",
				data.getString("item_key", packageKey), creditsAdded, vipAdded, vipCredits, baseCredits);

			if (Glb.serverLogPayments)
			{
				Server.sendLogInfo("PURCHASE", string.Format("[purchase_event=PURCHASE_COMPLETED] {0}", msg));
			}
			if (Data.debugEconomy)
			{
				Debug.Log(string.Format("NewEconomyManager::creditPurchase - {0}", msg));
			}
		}

		if (!string.IsNullOrEmpty(purchaseTransactionPending))
		{
			Dictionary<string, string> extraFields = new Dictionary<string, string>();
			extraFields.Add("credits_added", creditsAdded.ToString());
			extraFields.Add("client_credits", SlotsPlayer.creditAmount.ToString());
			Userflows.addExtraFieldsToFlow(purchaseTransactionPending, extraFields);
			Userflows.flowEnd(purchaseTransactionPending);

			purchaseTransactionPending = "";
		}

		// if we start any purchase, then we want to turn off the starter pack until the next refresh when the server can
		StarterDialog.didPurchase = true;

		if (_isLifecycleSale)
		{
			LifecycleDialog.didPurchase = true;
			_isLifecycleSale = false;
		}
		
		//Need to also reset the overlay buttons to hide the starter pack deals button
		if (Overlay.instance != null)
		{
			if (Overlay.instance.topV2 != null && Overlay.instance.topV2.buyButtonManager != null)
			{
				Overlay.instance.topV2.buyButtonManager.setButtonType();
			}

			if (Overlay.instance.topHIR != null)
			{
				Overlay.instance.topHIR.setButtonsVisibility();
			}
		}
	}

	//callback for when slice is purchased
	private void onPremiumSlicePurchased(JSON data)
	{
		string eventId = data.getString("event", "");
		if (eventId != "")
		{
			CreditAction.acceptPurchasedItem(eventId);
		}

		if (clickedDialog != null && clickedDialog.type.keyName == "premium_slice_purchase")
		{
			Dialog.close(clickedDialog); 
		}
		closePurchaseInProgressDialog();
		
		_purchaseInProgress = false;
		purchaseInProgressType = PurchaseFeatureData.Type.NONE;
		inProgressPurchaseOffer = null;

	}

	private void onBonusGamePurchased(JSON data)
	{
		string eventId = data.getString("event", "");
		if (eventId != "")
		{
			CreditAction.acceptPurchasedItem(eventId);
		}
		closePurchaseInProgressDialog();
		_purchaseInProgress = false;
		purchaseInProgressType = PurchaseFeatureData.Type.NONE;
		inProgressPurchaseOffer = null;
	}
	

	private void closePurchaseInProgressDialog()
	{
		List<DialogBase> openGenericDialogs = Dialog.instance.findOpenDialogsOfType("generic");

		foreach (DialogBase genericDialog in openGenericDialogs)
		{
			string reason = (string)genericDialog.dialogArgs.getWithDefault(D.REASON, "");
			if (reason == "new-economy-manager-purchase-in-progress")
			{
				Dialog.close(genericDialog); // Close the purchase in-progress dialog
				break; //Should really only be one in purchase dialog thats open
			}
		}
	}
	
	//Callback for when the Rich Pass Upgrade is purchased
	private void onPassPurchased(JSON data)
	{
		string eventId = data.getString("event", "");
		string type = data.getString("pass_type", "");
		int seasonId = data.getInt("season_id", -1);
		//If the event id is not null then send the event immediately back to the server
		if (eventId != "")
		{
			CreditAction.acceptPurchasedItem(eventId);
		}

		if (clickedDialog != null && clickedDialog.type.keyName == "rich_pass_upgrade_to_gold_dialog")
		{
			Dialog.close(clickedDialog); //If purchased from the upgrade dialog, don't show it again after the successful purchase
		}

		closePurchaseInProgressDialog();

		if (CampaignDirector.richPass != null && CampaignDirector.richPass.isActive)
		{
			if (CampaignDirector.richPass.timerRange.startTimestamp == seasonId) //Only actually upgrade the pass if we're in the same season
			{
				CampaignDirector.richPass.upgradePass(type);
			}
			else
			{
				Debug.LogErrorFormat("Rich Pass Purchased for season {0} but current season is {1}. Not upgrading to gold.", seasonId, CampaignDirector.richPass.timerRange.startTimestamp);
			}
		}

		_purchaseInProgress = false;
		purchaseInProgressType = PurchaseFeatureData.Type.NONE;
		inProgressPurchaseOffer = null;
	}

	// Retrieve additional data from our item:
	public static string getItemShortCode(PurchasableItem item, string defaultValue)
	{
		if (item.Metadata != null && item.Metadata.ContainsKey("zpayments_grant_code"))
		{
			return (string)item.Metadata["zpayments_grant_code"];
		}
		return defaultValue;
	}
}
