#pragma warning disable 0618, 0168, 0414
// /*
// ** Class: UAWrapper
// ** Author: Michael Christensen-Calvin <mchristensencalvin@zynga.com>
// ** Date: September 15, 2015
// ** Description: Wrapper class to handle the initialization and calling of the Zynga UAWrapper SDK.
// */

using System.Collections.Generic;
using UnityEngine;
using Zynga.Metrics.UserAcquisition;
using System;
using com.adjust.sdk;

public class UAWrapper : IDependencyInitializer , IResetGame
{
	private InitializationManager initMgr;

	private static UAWrapper _instance;

	public static UAWrapper Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new UAWrapper();
			}
			return _instance;
		}
	}

	public UserAcquisitionPackage userAcquisitionPackage = null;

	private const string NEW_DEVICE_ID = "NewDeviceID";
	public const string FIRST_TIME_DEPOSIT = "FirstTimeDeposit";
	public const string SPIN_ANY = "SpinAny";
	public const string SPIN_10 = "Spin10";
	public const string SPIN_100 = "Spin100";
	public const string SPIN_1000 = "Spin1000";
	public const string COMPLETE_ANY_LOGIN = "CompleteAnyLogin";
	public const string COLLECT_FIRST_DAILY_BONUS = "CollectFirstDailyBonus";
	public const string REACH_LEVEL_1 = "ReachLevel1";
	public const string REACH_LEVEL_5 = "ReachLevel5";
	public const string REACH_LEVEL_10 = "ReachLevel10";
	public const string REACH_LEVEL_25 = "ReachLevel25";
	public const string REACH_LEVEL_35 = "ReachLevel35";
	public const string REACH_LEVEL_50 = "ReachLevel50";
	public const string REACH_LEVEL_60 = "ReachLevel60";
	public const string REACH_LEVEL_80 = "ReachLevel80";
	public const string REACH_LEVEL_100 = "ReachLevel100";
	public const string REACH_LEVEL_120 = "ReachLevel120";
	public const string REACH_LEVEL_140 = "ReachLevel140";
	public const string REACH_LEVEL_160 = "ReachLevel160";

	// The different app tokens per platform.
#if ZYNGA_GOOGLE
	private const string APP_TOKEN = "###UAWRAPPER_APP_TOKEN_ANDROID###";
	private const string FTUE_CALLBACK_TOKEN = "###UAWRAPPER_FTUE_CALLBACK_TOKEN_ANDROID###";
	private const string PURCHASE_TOKEN = "###UAWRAPPER_PURCHASE_TOKEN_ANDROID###";
	private const string PURCHASE_FAILED_TOKEN = "###UAWRAPPER_PURCHASE_FAILED_TOKEN_ANDROID###";
	private const string PURCHASE_NOT_VERIFIED_TOKEN = "###UAWRAPPER_PURCHASE_NOT_VERIFIED_TOKEN_ANDROID###";
	private const string PURCHASE_UNKNOWN_TOKEN = "###UAWRAPPER_PURCHASE_UNKNOWN_TOKEN_ANDROID###";
	private const string FIRST_TIME_DEPOSIT_TOKEN = "###UAWRAPPER_FTD_TOKEN_ANDROID###";
	private const string OPEN_TOKEN = "###UAWRAPPER_OPEN_TOKEN_ANDROID###";
	private const string REGISTRATION_TOKEN = "###UAWRAPPER_REGISTRATION_TOKEN_ANDROID###";
	private const string GAME_UPDATED_TOKEN = "###UAWRAPPER_GAME_UPDATED_TOKEN_ANDROID###";
	private const string NEW_DEVICE_ID_TOKEN = "###UAWRAPPER_NEW_DEVICE_ID_TOKEN_ANDROID###";
	private const string ADJUST_DEVICE_TRACKING_TOKEN = "###UAWRAPPER_ADJUST_DEVICE_TRACKING_TOKEN_ANDROID###";
	private const string SPIN_ANY_TOKEN = "###UAWRAPPER_SPIN_ANY_TOKEN_ANDROID###";
	private const string SPIN_10_TOKEN = "###UAWRAPPER_SPIN_10_TOKEN_ANDROID###";
	private const string SPIN_100_TOKEN = "###UAWRAPPER_SPIN_100_TOKEN_ANDROID###";
	private const string SPIN_1000_TOKEN = "###UAWRAPPER_SPIN_1000_TOKEN_ANDROID###";
	private const string COMPLETE_ANY_LOGIN_TOKEN = "###UAWRAPPER_COMPLETE_LOGIN_TOKEN_ANDROID###";
	private const string COLLECT_FIRST_DAILY_BONUS_TOKEN = "###UAWRAPPER_COLLECT_FIRST_DAILY_BONUS_TOKEN_ANDROID###";
	private const string REACH_LEVEL_1_TOKEN = "###UAWRAPPER_REACH_LEVEL_1_TOKEN_ANDROID###";
	private const string REACH_LEVEL_5_TOKEN = "###UAWRAPPER_REACH_LEVEL_5_TOKEN_ANDROID###";
	private const string REACH_LEVEL_10_TOKEN = "###UAWRAPPER_REACH_LEVEL_10_TOKEN_ANDROID###";
	private const string REACH_LEVEL_25_TOKEN = "###UAWRAPPER_REACH_LEVEL_25_TOKEN_ANDROID###";
	private const string REACH_LEVEL_35_TOKEN = "###UAWRAPPER_REACH_LEVEL_35_TOKEN_ANDROID###";
	private const string REACH_LEVEL_50_TOKEN = "###UAWRAPPER_REACH_LEVEL_50_TOKEN_ANDROID###";
	private const string REACH_LEVEL_60_TOKEN = "###UAWRAPPER_REACH_LEVEL_60_TOKEN_ANDROID###";
	private const string REACH_LEVEL_80_TOKEN = "###UAWRAPPER_REACH_LEVEL_80_TOKEN_ANDROID###";
	private const string REACH_LEVEL_100_TOKEN = "###UAWRAPPER_REACH_LEVEL_100_TOKEN_ANDROID###";
	private const string REACH_LEVEL_120_TOKEN = "###UAWRAPPER_REACH_LEVEL_120_TOKEN_ANDROID###";
	private const string REACH_LEVEL_140_TOKEN = "###UAWRAPPER_REACH_LEVEL_140_TOKEN_ANDROID###";
	private const string REACH_LEVEL_160_TOKEN = "###UAWRAPPER_REACH_LEVEL_160_TOKEN_ANDROID###";
	private long SECRETID = 2;
	private long INFO1 = 856078143;
	private long INFO2 = 1268496949;
	private long INFO3 = 2023109341;
	private long INFO4 = 1430150671;
#elif UNITY_IPHONE
	private const string APP_TOKEN = "###UAWRAPPER_APP_TOKEN_IOS###";
	private const string FTUE_CALLBACK_TOKEN = "###UAWRAPPER_FTUE_CALLBACK_TOKEN_IOS###";
	private const string PURCHASE_TOKEN = "###UAWRAPPER_PURCHASE_TOKEN_IOS###";
	private const string PURCHASE_FAILED_TOKEN = "###UAWRAPPER_PURCHASE_FAILED_TOKEN_IOS###";
	private const string PURCHASE_NOT_VERIFIED_TOKEN = "###UAWRAPPER_PURCHASE_NOT_VERIFIED_TOKEN_IOS###";
	private const string PURCHASE_UNKNOWN_TOKEN = "###UAWRAPPER_PURCHASE_UNKNOWN_TOKEN_IOS###";
	private const string FIRST_TIME_DEPOSIT_TOKEN = "###UAWRAPPER_FTD_TOKEN_IOS###";
	private const string OPEN_TOKEN = "###UAWRAPPER_OPEN_TOKEN_IOS###";
	private const string REGISTRATION_TOKEN = "###UAWRAPPER_REGISTRATION_TOKEN_IOS###";
	private const string GAME_UPDATED_TOKEN = "###UAWRAPPER_GAME_UPDATED_TOKEN_IOS###";
	private const string NEW_DEVICE_ID_TOKEN = "###UAWRAPPER_NEW_DEVICE_ID_TOKEN_IOS###";
	private const string ADJUST_DEVICE_TRACKING_TOKEN = "###UAWRAPPER_ADJUST_DEVICE_TRACKING_TOKEN_IOS###";
	private const string SPIN_ANY_TOKEN = "###UAWRAPPER_SPIN_ANY_TOKEN_IOS###";
	private const string SPIN_10_TOKEN = "###UAWRAPPER_SPIN_10_TOKEN_IOS###";
	private const string SPIN_100_TOKEN = "###UAWRAPPER_SPIN_100_TOKEN_IOS###";
	private const string SPIN_1000_TOKEN = "###UAWRAPPER_SPIN_1000_TOKEN_IOS###";
	private const string COMPLETE_ANY_LOGIN_TOKEN = "###UAWRAPPER_COMPLETE_LOGIN_TOKEN_IOS###";
	private const string COLLECT_FIRST_DAILY_BONUS_TOKEN = "###UAWRAPPER_COLLECT_FIRST_DAILY_BONUS_TOKEN_IOS###";
	private const string REACH_LEVEL_1_TOKEN = "###UAWRAPPER_REACH_LEVEL_1_TOKEN_IOS###";
	private const string REACH_LEVEL_5_TOKEN = "###UAWRAPPER_REACH_LEVEL_5_TOKEN_IOS###";
	private const string REACH_LEVEL_10_TOKEN = "###UAWRAPPER_REACH_LEVEL_10_TOKEN_IOS###";
	private const string REACH_LEVEL_25_TOKEN = "###UAWRAPPER_REACH_LEVEL_25_TOKEN_IOS###";
	private const string REACH_LEVEL_35_TOKEN = "###UAWRAPPER_REACH_LEVEL_35_TOKEN_IOS###";
	private const string REACH_LEVEL_50_TOKEN = "###UAWRAPPER_REACH_LEVEL_50_TOKEN_IOS###";
	private const string REACH_LEVEL_60_TOKEN = "###UAWRAPPER_REACH_LEVEL_60_TOKEN_IOS###";
	private const string REACH_LEVEL_80_TOKEN = "###UAWRAPPER_REACH_LEVEL_80_TOKEN_IOS###";
	private const string REACH_LEVEL_100_TOKEN = "###UAWRAPPER_REACH_LEVEL_100_TOKEN_IOS###";
	private const string REACH_LEVEL_120_TOKEN = "###UAWRAPPER_REACH_LEVEL_120_TOKEN_IOS###";
	private const string REACH_LEVEL_140_TOKEN = "###UAWRAPPER_REACH_LEVEL_140_TOKEN_IOS###";
	private const string REACH_LEVEL_160_TOKEN = "###UAWRAPPER_REACH_LEVEL_160_TOKEN_IOS###";
	private long SECRETID = 2;
	private long INFO1 = 1998962022;
	private long INFO2 = 1059676974;
	private long INFO3 = 1658190237;
	private long INFO4 = 1798659457;
#elif ZYNGA_KINDLE
	public const string APP_TOKEN = "###UAWRAPPER_APP_TOKEN_KINDLE###";
	private const string FTUE_CALLBACK_TOKEN = "###UAWRAPPER_FTUE_CALLBACK_TOKEN_KINDLE###";
	private const string PURCHASE_TOKEN = "###UAWRAPPER_PURCHASE_TOKEN_KINDLE###";
	private const string PURCHASE_FAILED_TOKEN = "###UAWRAPPER_PURCHASE_FAILED_TOKEN_KINDLE###";
	private const string PURCHASE_NOT_VERIFIED_TOKEN = "###UAWRAPPER_PURCHASE_NOT_VERIFIED_TOKEN_KINDLE###";
	private const string PURCHASE_UNKNOWN_TOKEN = "###UAWRAPPER_PURCHASE_UNKNOWN_TOKEN_KINDLE###";
	private const string FIRST_TIME_DEPOSIT_TOKEN = "###UAWRAPPER_FTD_TOKEN_KINDLE###";
	private const string OPEN_TOKEN = "###UAWRAPPER_OPEN_TOKEN_KINDLE###";
	private const string REGISTRATION_TOKEN = "###UAWRAPPER_REGISTRATION_TOKEN_KINDLE###";
	private const string GAME_UPDATED_TOKEN = "###UAWRAPPER_GAME_UPDATED_TOKEN_KINDLE###";
	private const string NEW_DEVICE_ID_TOKEN = "###UAWRAPPER_NEW_DEVICE_ID_TOKEN_KINDLE###";
	private const string ADJUST_DEVICE_TRACKING_TOKEN = "###UAWRAPPER_ADJUST_DEVICE_TRACKING_TOKEN_KINDLE###";
	private const string SPIN_ANY_TOKEN = "###UAWRAPPER_SPIN_ANY_TOKEN_KINDLE###";
	private const string SPIN_10_TOKEN = "###UAWRAPPER_SPIN_10_TOKEN_KINDLE###";
	private const string SPIN_100_TOKEN = "###UAWRAPPER_SPIN_100_TOKEN_KINDLE###";
	private const string SPIN_1000_TOKEN = "###UAWRAPPER_SPIN_1000_TOKEN_KINDLE###";
	private const string COMPLETE_ANY_LOGIN_TOKEN = "###UAWRAPPER_COMPLETE_LOGIN_TOKEN_KINDLE###";
	private const string COLLECT_FIRST_DAILY_BONUS_TOKEN = "###UAWRAPPER_COLLECT_FIRST_DAILY_BONUS_TOKEN_KINDLE###";
	private const string REACH_LEVEL_1_TOKEN = "###UAWRAPPER_REACH_LEVEL_1_TOKEN_KINDLE###";
	private const string REACH_LEVEL_5_TOKEN = "###UAWRAPPER_REACH_LEVEL_5_TOKEN_KINDLE###";
	private const string REACH_LEVEL_10_TOKEN = "###UAWRAPPER_REACH_LEVEL_10_TOKEN_KINDLE###";
	private const string REACH_LEVEL_25_TOKEN = "###UAWRAPPER_REACH_LEVEL_25_TOKEN_KINDLE###";
	private const string REACH_LEVEL_35_TOKEN = "###UAWRAPPER_REACH_LEVEL_35_TOKEN_KINDLE###";
	private const string REACH_LEVEL_50_TOKEN = "###UAWRAPPER_REACH_LEVEL_50_TOKEN_KINDLE###";
	private const string REACH_LEVEL_60_TOKEN = "###UAWRAPPER_REACH_LEVEL_60_TOKEN_KINDLE###";
	private const string REACH_LEVEL_80_TOKEN = "###UAWRAPPER_REACH_LEVEL_80_TOKEN_KINDLE###";
	private const string REACH_LEVEL_100_TOKEN = "###UAWRAPPER_REACH_LEVEL_100_TOKEN_KINDLE###";
	private const string REACH_LEVEL_120_TOKEN = "###UAWRAPPER_REACH_LEVEL_120_TOKEN_KINDLE###";
	private const string REACH_LEVEL_140_TOKEN = "###UAWRAPPER_REACH_LEVEL_140_TOKEN_KINDLE###";
	private const string REACH_LEVEL_160_TOKEN = "###UAWRAPPER_REACH_LEVEL_160_TOKEN_KINDLE###";
	private long SECRETID = 2;
	private long INFO1 = 474802768;
	private long INFO2 = 399597178;
	private long INFO3 = 1450610342;
	private long INFO4 = 463237149;
#else
	public const string APP_TOKEN = "unknown";
	private const string FTUE_CALLBACK_TOKEN = "unknown";
	private const string PURCHASE_TOKEN = "unknown";
	private const string PURCHASE_FAILED_TOKEN = "unknown";
	private const string PURCHASE_NOT_VERIFIED_TOKEN = "unknown";
	private const string PURCHASE_UNKNOWN_TOKEN = "unknown";
	private const string FIRST_TIME_DEPOSIT_TOKEN = "unknown";
	private const string OPEN_TOKEN = "unknown";
	private const string REGISTRATION_TOKEN = "unknown";
	private const string GAME_UPDATED_TOKEN = "unknown";
	private const string NEW_DEVICE_ID_TOKEN = "unknown";
	private const string ADJUST_DEVICE_TRACKING_TOKEN = "unknown";
	private const string SPIN_ANY_TOKEN = "unknown";
	private const string SPIN_10_TOKEN = "unknown";
	private const string SPIN_100_TOKEN = "unknown";
	private const string SPIN_1000_TOKEN = "unknown";
	private const string COMPLETE_ANY_LOGIN_TOKEN = "unknown";
	private const string COLLECT_FIRST_DAILY_BONUS_TOKEN = "unknown";
	private const string REACH_LEVEL_1_TOKEN = "unknown";
	private const string REACH_LEVEL_5_TOKEN = "unknown";
	private const string REACH_LEVEL_10_TOKEN = "unknown";
	private const string REACH_LEVEL_25_TOKEN = "unknown";
	private const string REACH_LEVEL_35_TOKEN = "unknown";
	private const string REACH_LEVEL_50_TOKEN = "unknown";
	private const string REACH_LEVEL_60_TOKEN = "unknown";
	private const string REACH_LEVEL_80_TOKEN = "unknown";
	private const string REACH_LEVEL_100_TOKEN = "unknown";
	private const string REACH_LEVEL_120_TOKEN = "unknown";
	private const string REACH_LEVEL_140_TOKEN = "unknown";
	private const string REACH_LEVEL_160_TOKEN = "unknown";
	private long SECRETID = 0;
	private long INFO1 = 0;
	private long INFO2 = 0;
	private long INFO3 = 0;
	private long INFO4 = 0;
#endif

	public void Initialize()
	{
		TrackerEnvironment trackerEnvironment = Data.IsSandbox ? TrackerEnvironment.Sandbox : TrackerEnvironment.Production;
		TrackerLogLevel logLevel = Data.IsSandbox ? TrackerLogLevel.Verbose : TrackerLogLevel.Error;
		
		var uaTrackService = new Zynga.Metrics.UserAcquisition.ExternalFacades.Implementations.TrackService(ZdkManager.Instance.ZTrack);
#if UNITY_WEBGL || UNITY_EDITOR
		var adjustSettingsBuilder = new Zynga.Metrics.UserAcquisition.AdjustSettings.Builder(APP_TOKEN)
		{
			AppOpenToken = OPEN_TOKEN,
			FTUECompleteToken = FTUE_CALLBACK_TOKEN,
			GameUpdatedToken = GAME_UPDATED_TOKEN,
			RegistrationToken = REGISTRATION_TOKEN,
			PurchaseToken = PURCHASE_TOKEN,
			PurchaseFailedToken = PURCHASE_FAILED_TOKEN,
			PurchaseNotVerifiedToken = PURCHASE_NOT_VERIFIED_TOKEN,
			PurchaseUnknownToken = PURCHASE_UNKNOWN_TOKEN,
			AdjustDeviceTrackingToken = ADJUST_DEVICE_TRACKING_TOKEN
		};
#else
		var adjustSettingsBuilder = new Zynga.Metrics.UserAcquisition.AdjustSettings.Builder(APP_TOKEN, SECRETID, INFO1, INFO2, INFO3, INFO4)
		{
			AppOpenToken = OPEN_TOKEN,
			FTUECompleteToken = FTUE_CALLBACK_TOKEN,
			GameUpdatedToken = GAME_UPDATED_TOKEN,
			RegistrationToken = REGISTRATION_TOKEN,
			PurchaseToken = PURCHASE_TOKEN,
			PurchaseFailedToken = PURCHASE_FAILED_TOKEN,
			PurchaseNotVerifiedToken = PURCHASE_NOT_VERIFIED_TOKEN,
			PurchaseUnknownToken = PURCHASE_UNKNOWN_TOKEN,
			AdjustDeviceTrackingToken = ADJUST_DEVICE_TRACKING_TOKEN
		};
#endif
		adjustSettingsBuilder.AddCustomEvent(FIRST_TIME_DEPOSIT, FIRST_TIME_DEPOSIT_TOKEN);
		adjustSettingsBuilder.AddCustomEvent(SPIN_ANY, SPIN_ANY_TOKEN);
		adjustSettingsBuilder.AddCustomEvent(SPIN_10, SPIN_10_TOKEN);
		adjustSettingsBuilder.AddCustomEvent(SPIN_100, SPIN_100_TOKEN);
		adjustSettingsBuilder.AddCustomEvent(SPIN_1000, SPIN_1000_TOKEN);
		adjustSettingsBuilder.AddCustomEvent(COMPLETE_ANY_LOGIN, COMPLETE_ANY_LOGIN_TOKEN);
		adjustSettingsBuilder.AddCustomEvent(COLLECT_FIRST_DAILY_BONUS, COLLECT_FIRST_DAILY_BONUS_TOKEN);
		adjustSettingsBuilder.AddCustomEvent(REACH_LEVEL_1, REACH_LEVEL_1_TOKEN);
		adjustSettingsBuilder.AddCustomEvent(REACH_LEVEL_5, REACH_LEVEL_5_TOKEN);
		adjustSettingsBuilder.AddCustomEvent(REACH_LEVEL_10, REACH_LEVEL_10_TOKEN);
		adjustSettingsBuilder.AddCustomEvent(REACH_LEVEL_25, REACH_LEVEL_25_TOKEN);
		adjustSettingsBuilder.AddCustomEvent(REACH_LEVEL_35, REACH_LEVEL_35_TOKEN);
		adjustSettingsBuilder.AddCustomEvent(REACH_LEVEL_50, REACH_LEVEL_50_TOKEN);
		adjustSettingsBuilder.AddCustomEvent(REACH_LEVEL_60, REACH_LEVEL_60_TOKEN);
		adjustSettingsBuilder.AddCustomEvent(REACH_LEVEL_80, REACH_LEVEL_80_TOKEN);
		adjustSettingsBuilder.AddCustomEvent(REACH_LEVEL_100, REACH_LEVEL_100_TOKEN);
		adjustSettingsBuilder.AddCustomEvent(REACH_LEVEL_120, REACH_LEVEL_120_TOKEN);
		adjustSettingsBuilder.AddCustomEvent(REACH_LEVEL_140, REACH_LEVEL_140_TOKEN);
		adjustSettingsBuilder.AddCustomEvent(REACH_LEVEL_160, REACH_LEVEL_160_TOKEN);
		
		var trackerStrategy = new AdjustTrackerStrategy(uaTrackService, adjustSettingsBuilder.Build());
		trackerStrategy.OnAttributionChange += (attribution) => {
			AnalyticsManager.Instance.LogAdjust(attribution.adid);
		};
		userAcquisitionPackage = new UserAcquisitionPackage(Packages.UnityPrefs, new HIRSKAdConversionAdapter(uaTrackService.LogIosAppTrackingTransparencyConsent), trackerStrategy);
		SessionStartSettings sessionStartSettings = new SessionStartSettings.Builder(ZdkManager.Instance.Zsession.Zid.ToString(), Data.zAppId, trackerEnvironment, Glb.clientVersion)
		{
			LogLevel = logLevel
		}.Build();

		userAcquisitionPackage.Tracker.StartTrackingSession(sessionStartSettings);

		initMgr.InitializationComplete(this);
	}

	// Callback for when the game is paused/unpaused
	// Called from PauseHandler.cs
	public void PauseHandler(bool isPaused)
	{
		if (userAcquisitionPackage != null && userAcquisitionPackage.Tracker != null) {
			if (isPaused) {
				userAcquisitionPackage.Tracker.OnApplicationPaused ();
			} else {
				userAcquisitionPackage.Tracker.OnApplicationResumed ();
			}
		}
	}

	// Called when the 'FTUE' has completed. Since we don't have a FTUE on mobile we will call this after first spin on account.
	public void OnFTUECompleted()
	{
		if (userAcquisitionPackage != null && userAcquisitionPackage.Tracker != null) {
			userAcquisitionPackage.Tracker.OnFTUECompleted ();
		}
	}

	public void OnSpin(int totalSpinCount)
	{
		if (userAcquisitionPackage != null && userAcquisitionPackage.Tracker != null)
		{
			switch (totalSpinCount)
			{
				case 1:
					userAcquisitionPackage.Tracker.TrackCustomEvent(UAWrapper.SPIN_ANY);
					break;
				case 10:
					userAcquisitionPackage.Tracker.TrackCustomEvent(UAWrapper.SPIN_10);
					break;
				case 100:
					userAcquisitionPackage.Tracker.TrackCustomEvent(UAWrapper.SPIN_100);
					break;
				case 1000:
					userAcquisitionPackage.Tracker.TrackCustomEvent(UAWrapper.SPIN_1000);
					break;
				default:
					return;
			}
		}
	}

	public void onLevelUp(int newLevel)
	{
		if (userAcquisitionPackage != null && userAcquisitionPackage.Tracker != null) 
		{
			switch (newLevel)
			{
				case 1:
					userAcquisitionPackage.Tracker.TrackCustomEvent(UAWrapper.REACH_LEVEL_1);
					break;
				case 5:
					userAcquisitionPackage.Tracker.TrackCustomEvent(UAWrapper.REACH_LEVEL_5);
					break;
				case 10:
					userAcquisitionPackage.Tracker.TrackCustomEvent(UAWrapper.REACH_LEVEL_10);
					break;
				case 25:
					userAcquisitionPackage.Tracker.TrackCustomEvent(UAWrapper.REACH_LEVEL_25);
					break;
				case 35:
					userAcquisitionPackage.Tracker.TrackCustomEvent(UAWrapper.REACH_LEVEL_35);
					break;
				case 50:
					userAcquisitionPackage.Tracker.TrackCustomEvent(UAWrapper.REACH_LEVEL_50);
					break;
				case 60:
					userAcquisitionPackage.Tracker.TrackCustomEvent(UAWrapper.REACH_LEVEL_60);
					break;
				case 80:
					userAcquisitionPackage.Tracker.TrackCustomEvent(UAWrapper.REACH_LEVEL_80);
					break;
				case 100:
					userAcquisitionPackage.Tracker.TrackCustomEvent(UAWrapper.REACH_LEVEL_100);
					break;
				case 120:
					userAcquisitionPackage.Tracker.TrackCustomEvent(UAWrapper.REACH_LEVEL_120);
					break;
				case 140:
					userAcquisitionPackage.Tracker.TrackCustomEvent(UAWrapper.REACH_LEVEL_140);
					break;
				case 160:
					userAcquisitionPackage.Tracker.TrackCustomEvent(UAWrapper.REACH_LEVEL_160);
					break;
			}
		}
	}

	public void OnCollectDailyBonus()
	{
		if (userAcquisitionPackage != null && userAcquisitionPackage.Tracker != null) 
		{
			bool hasCollectedDailyBonus = CustomPlayerData.getBool(CustomPlayerData.DAILY_BONUS_COLLECTED, false);
			if (!hasCollectedDailyBonus)
			{
				userAcquisitionPackage.Tracker.TrackCustomEvent(UAWrapper.COLLECT_FIRST_DAILY_BONUS);
			}
		}
	}

	public void OnCompleteAnyLoginMethod()
	{
		if (userAcquisitionPackage != null && userAcquisitionPackage.Tracker != null)
		{
			userAcquisitionPackage.Tracker.TrackCustomEvent(UAWrapper.COMPLETE_ANY_LOGIN);
		}
	}

	public void TrackPurchase(float paidAmount, string paidCurrency, string transactionId, string receiptId, string receiptSig, string productId)
	{
		if (userAcquisitionPackage != null && userAcquisitionPackage.Tracker != null)
		{
			TransactionInfo transactionInfo;
			if (string.IsNullOrEmpty(transactionId))
			{
				transactionInfo = null;
			}
			else
			{
				transactionInfo = new TransactionInfo(transactionId, receiptId, receiptSig, productId);
			}

			if (userAcquisitionPackage.Tracker.TrackPurchase (paidCurrency, (double)paidAmount, transactionInfo))
			{
				// Piggy back on this func to also send the FTD (first time deposit) event
				if (!SlotsPlayer.instance.isPayerMobile)
				{
					userAcquisitionPackage.Tracker.TrackCustomEvent (UAWrapper.FIRST_TIME_DEPOSIT);
				}
				if (Data.debugMode)
				{
					Debug.LogFormat("UAWrapper.TrackPurchase() success, {0} {1} @ {2}", paidAmount, paidCurrency, transactionId);
				}
			}
			else
			{
				Debug.LogErrorFormat("UAWrapper.TrackPurchase() failure, {0} {1} @ {2}", paidAmount, paidCurrency, transactionId);
			}
		}
	}

	public void TrackPurchase(string currencyCode, double revenue, string transactionIdentifier, string receiptData, string receiptSignature)
	{
		if (userAcquisitionPackage != null && userAcquisitionPackage.Tracker != null)
		{
			userAcquisitionPackage.Tracker.TrackPurchase(currencyCode, revenue, new TransactionInfo(transactionIdentifier, receiptData, receiptSignature));
		}
	}

	// Called after a user has registered for our game. This is called oncer per user on login.
	public void OnRegistration()
	{
		Zynga.Core.Util.Snid snid = Zynga.Core.Util.Snid.Anonymous;
		if (SlotsPlayer.isFacebookUser)
		{
			snid = Zynga.Core.Util.Snid.Facebook;
		}
		if (userAcquisitionPackage != null && userAcquisitionPackage.Tracker != null)
		{
			userAcquisitionPackage.Tracker.OnRegistration(snid);
		}
	}

	public static void LogSplunkError(string message, string caller)
	{
		string logMessage = string.Format("{0} [device_id={1}]", message, Zynga.Slots.ZyngaConstantsGame.deviceAdvertisingID);
		Server.sendLogError(logMessage, caller);
	}

	public static void resetStaticClassData()
	{
		// czablocki - 2/2020: This is occasionally showing up in BugSnag SIGABRT breadcrumbs:
		// Message: "Glb.reinitializeGame() : Exception during reset of UAWrapper :" 
		// Changing this to check for nulls before calling Dispose
		if (Instance != null && Instance.userAcquisitionPackage != null)
		{
			Instance.userAcquisitionPackage.Dispose();
		}
	}

	#region ISVDependencyInitializer implementation

	// The UAWrapper is dependent on the ZDK and basic info (for a zruntime control)
	public System.Type[] GetDependencies()
	{
		return new System.Type[] { typeof(ZdkManager), typeof(BasicInfoLoader), typeof(AuthManager) };
	}

	// Initializes the UAWrapper
	public void Initialize(InitializationManager mgr)
	{
		initMgr = mgr;
		Initialize();
	}

	// Short description of this dependency for debugging purposes
	public string description()
	{
		return "UAWrapper";
	}

	#endregion ISVDependencyInitializer implementation
}
#pragma warning restore 0618, 0168, 0414
