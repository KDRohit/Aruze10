using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PrizePop;

/*
Class: Purchase Feature Data
Author: mchristensencalvin@zynga.com
Description: This is a wrapper class around the different ways we have to define the 
	data points for purchase features. It originally came down in STUDAction and we want
	to swap to defining things in EOS.
*/
public class PurchaseFeatureData : IResetGame
{
	public enum Type
	{
		BUY_PAGE,
		POPCORN_SALE,
		VIP_SALE,
		HAPPY_HOUR_SALE,
		PAYER_REACTIVATION_SALE,
		OUT_OF_CREDITS_THREE,
		OUT_OF_CREDITS,
		RICH_PASS,
		PREMIUM_SLICE,
		PRIZE_POP,
		NONE,
		ONE_CLICK_BUY,
		BONUS_GAME,
		STREAK_SALE
	}

	public Type type;
	public GameTimerRange timerRange = null;
	public string imageFolderPath = "";
	public string collectablesImageFolderPath = "";
	public int bonusSaleMultiplier = 1;
	public int maxBonusSalePercent = 0;
	public List<CreditPackage> creditPackages;
	public List<RichPassPackage> richPassPackages;
	public List<PremiumSlicePackage> premiumSlicePackages;
	public List<PrizePopPackage> prizePopPackages;
	public List<CreditPackage> bonusGamePackages;

	// Card Events
	public List<KeyValuePair<CreditPackage.CreditEvent, string>> cardEvents;

	private const string PACKAGE_FORMAT = "package_{0}";

	#region STATIC_VARIRABLES

	public static bool shouldReadEos = false;
	/* -- STATIC VARIABLES -- */
	private static Dictionary<Type, PurchaseFeatureData> _allStud;
	private static Dictionary<Type, PurchaseFeatureData> _allEos;

	public static Dictionary<Type, PurchaseFeatureData> allStud
	{
		get
		{
			if (_allStud == null)
			{
				_allStud = new Dictionary<Type, PurchaseFeatureData>();
			}
			return _allStud;
		}
	}

	public static Dictionary<Type, PurchaseFeatureData> allEos
	{
		get
		{
			if (_allEos == null)
			{
				_allEos = new Dictionary<Type, PurchaseFeatureData>();
			}
			return _allEos;
		}
	}

	#endregion

	private PurchaseFeatureData(RichPassExperiment experiment, Type type)
	{
		this.type = type;
		if (richPassPackages == null)
		{
			richPassPackages = new List<RichPassPackage>();
		}

		string packageName = experiment.packageKey;
		if (!string.IsNullOrEmpty(packageName))
		{
			RichPassPackage package = new RichPassPackage(packageName);
			richPassPackages.Add(package);
		}
	}
	private PurchaseFeatureData(PremiumSliceExperiment experiment, Type type)
	{
		this.type = type;
		if (premiumSlicePackages == null)
		{
			premiumSlicePackages = new List<PremiumSlicePackage>();
		}
		
		string packageName = global::PremiumSlice.instance != null ? global::PremiumSlice.instance.packageName : "";
		if (!string.IsNullOrEmpty(packageName))
		{
			PremiumSlicePackage package = new PremiumSlicePackage(packageName);
			premiumSlicePackages.Add(package);
		}
	}

	private PurchaseFeatureData(PrizePopExperiment experiment)
	{
		this.type = Type.PRIZE_POP;
		if (prizePopPackages == null)
		{
			prizePopPackages = new List<PrizePopPackage>();
		}
		else
		{
			prizePopPackages.Clear();
		}

		string packageNamesCsv = experiment.packageKeys;
		string[] packageNames = packageNamesCsv.Split(',');
		for (int i = 0; i < packageNames.Length; i++)
		{
			string packageName = packageNames[i];
			PrizePopPackage package = new PrizePopPackage(packageName);
			prizePopPackages.Add(package);
		}
	}

	private PurchaseFeatureData(LottoBlastExperiment experiment)
	{
		this.type = Type.BONUS_GAME;

		if (bonusGamePackages == null)
		{
			bonusGamePackages = new List<CreditPackage>();
		}

		string packageNamesCsv = experiment.package;
		string[] packageNames = packageNamesCsv.Split(',');
		for (int i = 0; i < packageNames.Length; i++)
		{
			CreditPackage package = new CreditPackage(PurchasablePackage.find(packageNames[i]), 0, false);
			bonusGamePackages.Add(package);
		}
	}

	//Streak sale
	private PurchaseFeatureData(StreakSaleExperiment experiment, JSON configJson)
	{
		this.type = Type.STREAK_SALE;

		if (creditPackages == null)
		{
			creditPackages = new List<CreditPackage>();
		}

		foreach (JSON packageEntry in configJson.getJsonArray("packages"))
		{
			string packageName = packageEntry.getString("coin_package", "");
			if (packageName.Length > 0)
			{
				CreditPackage package = new CreditPackage(PurchasablePackage.find(packageName), 0, false);
				package.collectableDropKeyName = packageEntry.getString("card_pack", "");
				package.bonus = packageEntry.getInt("base_bonus_pct", 0);
				creditPackages.Add(package);
			}

		}

	}

	// Create a PurchaseFeatureData from a STUDAction
	public PurchaseFeatureData(STUDAction action, Type type)
	{
		this.type = type;
		if (action != null)
		{
			timerRange = action.timerRange;
			imageFolderPath = action.imageFolderPath;
			bonusSaleMultiplier = action.bonusSaleMultiplier;
			maxBonusSalePercent = action.maxBonusSalePercent;
			creditPackages = new List<CreditPackage>();
			for (int i = 0; i < action.newCreditPackages.Count; i++)
			{
				creditPackages.Add(new CreditPackage(action.newCreditPackages[i]));
			}
		}
	}

	// Create a purchaseFeatureData from an eos experiment
	public PurchaseFeatureData(PurchaseExperiment experiment, int numPackages, Type type)
	{
		this.type = type;
		int startSeconds = experiment.startSeconds;
		int endSeconds = experiment.endSeconds;
		imageFolderPath = experiment.imageFolderPath;
		collectablesImageFolderPath = experiment.imagePathCollections;

		string eventData = experiment.collectiblesEvents;
		string eventLiftData = experiment.collectiblesEventLifts;
		// MCC -- Moving this logic over to BuyPageCardEvent since its not really relevant to all PurchaseFeatureData instances

		timerRange = new GameTimerRange(startSeconds, endSeconds);
		timerRange.registerFunction(setSaleNotification);
		creditPackages = new List<CreditPackage>();
		bool hasAtLeastOneCardDrop = false;
		for (int i = 0; i < numPackages; ++i)
		{
			string keyName = string.Format(PACKAGE_FORMAT, i + 1);
			string dropKeyName = experiment.getPackageValue(keyName, "_collectible_pack", "");
			if (!string.IsNullOrEmpty(dropKeyName) && dropKeyName != "nothing")
			{
				hasAtLeastOneCardDrop = true;
				break;
			}
		}

		if (type == Type.BUY_PAGE && hasAtLeastOneCardDrop)
		{
			if (!string.IsNullOrEmpty(eventData))
			{
				BuyPageCardEvent.instance.setPackageEvents(eventData, eventLiftData);
			}
			cardEvents = BuyPageCardEvent.instance.getEventsFromStrings(eventData, eventLiftData);
		}

		for (int i = 0; i < numPackages; i++)
		{
			string keyName = string.Format(PACKAGE_FORMAT, i + 1);
			string dropKeyName = experiment.getPackageValue(keyName, "_collectible_pack", "");
			bool hasCardPack = !string.IsNullOrEmpty(dropKeyName) && dropKeyName != "nothing";

			CreditPackage.CreditEvent activeEvent = CreditPackage.CreditEvent.NOTHING;
			string eventText = "";
			if (cardEvents != null && cardEvents.Count > i && hasCardPack)
			{
				activeEvent = cardEvents[i].Key;
				eventText = cardEvents[i].Value;
			}

			CreditPackage newPackage = new CreditPackage(keyName, experiment, activeEvent, eventText);
			creditPackages.Add(newPackage);
			maxBonusSalePercent = Mathf.Max(maxBonusSalePercent, newPackage.saleBonusPercent);
		}
	}

	#region STATIC_METHODS

	//We have this method to control when streak sale purchase feature data is initialized to eliminate a race condition where streak sale inits before all the purchase feature data is initted.
	public static void populateStreakSale(JSON configJson)
	{
		if (ExperimentWrapper.StreakSale.isInExperiment)
		{
			shouldReadEos = Data.liveData.getBool("STUD_EOS_READ", false);
			allEos.Add(Type.STREAK_SALE, new PurchaseFeatureData(ExperimentWrapper.StreakSale.experimentData, configJson));
		}
	}

	public static void populatePremiumSlice()
	{
		if (ExperimentWrapper.PremiumSlice.isInExperiment)
		{
			allEos.Add(Type.PREMIUM_SLICE, new PurchaseFeatureData(ExperimentWrapper.PremiumSlice.experimentData, Type.PREMIUM_SLICE));
		}
	}

	public static void populateAll()
	{
		// Populate with feature data from EOS
		// MCC -- Whenever we add a new experiment that has been converted, just add a line here passing in the experiment data and the number of packages.
		shouldReadEos = Data.liveData.getBool("STUD_EOS_READ", false);
		if (ExperimentWrapper.OutOfCredits.isInExperiment)
		{
			allEos.Add(Type.OUT_OF_CREDITS, new PurchaseFeatureData(ExperimentWrapper.OutOfCredits.experimentData, 1, Type.OUT_OF_CREDITS));
		}

		if (ExperimentWrapper.BuyPage.isInExperiment)
		{
			allEos.Add(Type.BUY_PAGE, new PurchaseFeatureData(ExperimentWrapper.BuyPage.experimentData, 6, Type.BUY_PAGE));
		}

		if (ExperimentWrapper.OneClickBuy.isInExperiment)
		{
			allEos.Add(Type.ONE_CLICK_BUY, new PurchaseFeatureData(ExperimentWrapper.OneClickBuy.experimentData, 1, Type.ONE_CLICK_BUY));
		}

		if (ExperimentWrapper.PopcornSale.isInExperiment)
		{
			allEos.Add(Type.POPCORN_SALE, new PurchaseFeatureData(ExperimentWrapper.PopcornSale.experimentData, 3, Type.POPCORN_SALE));
		}

		if (ExperimentWrapper.RichPass.isInExperiment)
		{
			allEos.Add(Type.RICH_PASS, new PurchaseFeatureData(ExperimentWrapper.RichPass.experimentData, Type.RICH_PASS));
		}

		if (ExperimentWrapper.PrizePop.isInExperiment)
		{
			allEos.Add(Type.PRIZE_POP, new PurchaseFeatureData(ExperimentWrapper.PrizePop.experimentData));
		}

		if (ExperimentWrapper.LevelLotto.isInExperiment)
		{
			allEos.Add(Type.BONUS_GAME, new PurchaseFeatureData(ExperimentWrapper.LevelLotto.experimentData));
		}

		STUDAction action;

		action = STUDAction.findBuyCreditsDialog();
		if (action != null)
		{
			allStud[Type.BUY_PAGE] = new PurchaseFeatureData(action, Type.BUY_PAGE);
		}
		else
		{
			Debug.LogErrorFormat("PurchaseFeatureData.cs -- populateAll -- weirdly..there is no buy page action for some reason.");
		}

		action = STUDAction.findOutOfCreditsThreeDialog();
		if (action != null)
		{
			allStud[Type.OUT_OF_CREDITS_THREE] = new PurchaseFeatureData(action, Type.OUT_OF_CREDITS_THREE);
		}

		action = STUDAction.findOutOfCreditsDialog();
		if (action != null)
		{
			allStud[Type.OUT_OF_CREDITS] = new PurchaseFeatureData(action, Type.OUT_OF_CREDITS);
		}

		action = STUDAction.findPopcornSale();
		if (action != null)
		{
			allStud[Type.POPCORN_SALE] = new PurchaseFeatureData(action, Type.POPCORN_SALE);
		}

		action = STUDAction.findVipSale();
		if (action != null)
		{
			allStud[Type.VIP_SALE] = new PurchaseFeatureData(action, Type.VIP_SALE);
		}

		action = STUDAction.findPayerReactivationSale();
		if (action != null)
		{
			allStud[Type.PAYER_REACTIVATION_SALE] = new PurchaseFeatureData(action, Type.PAYER_REACTIVATION_SALE);
		}

		action = STUDAction.findHappyHourSale();
		if (action != null)
		{
			allStud[Type.HAPPY_HOUR_SALE] = new PurchaseFeatureData(action, Type.HAPPY_HOUR_SALE);
		}
	}

	public static PurchaseFeatureData find(Type type)
	{
		if (shouldReadEos && allEos.ContainsKey(type))
		{
			return allEos[type];
		}
		if (allStud.ContainsKey(type))
		{
			return allStud[type];
		}

		return null;
	}

	public static PurchaseFeatureData BuyPage
	{ get { return find(Type.BUY_PAGE); } }

	public static PurchaseFeatureData OutOfCredits
	{ get { return find(Type.OUT_OF_CREDITS); } }

	public static PurchaseFeatureData OutOfCreditsThree
	{ get { return find(Type.OUT_OF_CREDITS_THREE); } }

	public static PurchaseFeatureData HappyHourSale
	{ get { return find(Type.HAPPY_HOUR_SALE); } }

	public static PurchaseFeatureData PayerReactivationSale
	{ get { return find(Type.PAYER_REACTIVATION_SALE); } }

	public static PurchaseFeatureData PopcornSale
	{ get { return find(Type.POPCORN_SALE); } }

	public static PurchaseFeatureData VipSale
	{ get { return find(Type.VIP_SALE); } }

	public static PurchaseFeatureData RichPass
	{ get { return find(Type.RICH_PASS); } }

	public static PurchaseFeatureData PremiumSlice
	{ get { return find(Type.PREMIUM_SLICE); } }

	public static PurchaseFeatureData PrizePop
	{ get { return find(Type.PRIZE_POP); } }

	public static PurchaseFeatureData LottoBlast
	{ get { return find(Type.BONUS_GAME); } }

	public static PurchaseFeatureData StreakSale
	{ get { return find(Type.STREAK_SALE); } }

	// Implements IResetGame
	public static void resetStaticClassData()
	{
		_allEos = new Dictionary<Type, PurchaseFeatureData>();
		_allStud = new Dictionary<Type, PurchaseFeatureData>();
	}

	// Is there a sale active for the current SKU?
	public static bool isSaleActive
	{
		get
		{
			if (!Packages.PaymentsManagerEnabled())
			{
				// We cannot have an active sale if the economy manager hasn't loaded.
				return false;
			}

			return
			(
				findBuyCreditsSalePercentage() > 0 ||
				ExperimentWrapper.FirstPurchaseOffer.isInExperiment ||
				PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_BUY_PAGE_KEY)
			);
		}
	}

	public static bool isActiveFromPowerup
	{
		get
		{
			if (!Packages.PaymentsManagerEnabled())
			{
				// We cannot have an active sale if the economy manager hasn't loaded.
				return false;
			}

			return
			(
				findBuyCreditsSalePercentage() <= 0 &&
				!ExperimentWrapper.FirstPurchaseOffer.isInExperiment &&
				PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_BUY_PAGE_KEY)
			);
		}
	}

	public static int findBuyCreditsMultiplier()
	{
		if (!Packages.PaymentsManagerEnabled())
		{
			// We cannot have an active sale if the economy manager hasn't loaded.
			return 1;
		}
		PurchaseFeatureData featureData = BuyPage;
		if (featureData != null && featureData.timerRange.isActive)
		{
			return featureData.bonusSaleMultiplier;
		}
		else
		{
			return 1;
		}
	}

	public static int findBuyCreditsSalePercentage()
	{
		if (!Packages.PaymentsManagerEnabled())
		{
			// We cannot have an active sale if the economy manager hasn't loaded.
			return 0;
		}
		PurchaseFeatureData data = BuyPage;
		if (data != null && data.timerRange.isActive)
		{
			return data.maxBonusSalePercent;
		}
		else
		{
			return 0;
		}
	}

	// Set sale notification.
	public static void setSaleNotification(Dict args = null, GameTimerRange originalTimer = null)
	{
		if (Overlay.instance.top != null)
		{
			Overlay.instance.top.setupSaleNotification();
		}
	}

	#endregion
}
