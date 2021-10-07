//#define DEBUG_STUD

using System.Collections.Generic;
using UnityEngine;

/*
Data structures for holding information about STUD offers provided with player login data.
*/

public class STUDAction : IResetGame
{
	public enum Type
	{
		UNDEFINED = 0,
		VALIDATION_ONLY = 1,
		ONE_CLICK_SALE = 3,       // A sale that appears in the header.
		MOBILE_TO_MOBILE_XPROMO = 16, // Only used on mobile.
		WATCH_TO_EARN = 17,
		XPROMO_SURFACE_WEB = 18,

		// No client reference to these?
		OUT_OF_CREDITS_DIALOG_WEB = 4,  // Not used on mobile.
		OUT_OF_CREDITS_DIALOG_IOS = 5,
		OUT_OF_CREDITS_DIALOG_ANDROID = 6,
		OUT_OF_CREDITS_DIALOG_KINDLE = 15,
		OUT_OF_CREDITS_DIALOG_UNITYWEB = 99, // 'Out of Coins Dialog - UnityWeb'
		
		POPCORN_SALE_WEB = 19,      // Not used on mobile.
		POPCORN_SALE_IOS = 20,
		POPCORN_SALE_ANDROID = 21,
		POPCORN_SALE_KINDLE = 22,
		POPCORN_SALE_UNITYWEB = 101, //'Popcorn Sale - UnityWeb'

		HAPPY_HOUR_SALE_WEB = 23,   // Not Used on mobile.
		HAPPY_HOUR_SALE_IOS = 24,
		HAPPY_HOUR_SALE_ANDROID = 25,
		HAPPY_HOUR_SALE_KINDLE = 26,
		HAPPY_HOUR_SALE_UNITYWEB = 102, // Happy Hour Sale Packages - UnityWeb'

		PAYER_REACTIVATION_SALE_WEB = 27, // Not Used on mobile.
		PAYER_REACTIVATION_SALE_IOS = 28,
		PAYER_REACTIVATION_SALE_ANDROID = 29,
		PAYER_REACTIVATION_SALE_KINDLE = 30,
		PAYER_REACTIVATION_SALE_UNITYWEB = 103,

		VIP_SALE_WEB = 31,        // Not Used on mobile.
		VIP_SALE_IOS = 32,
		VIP_SALE_ANDROID = 33,
		VIP_SALE_KINDLE = 34,
		VIP_SALE_WINDOWS = 89,
		VIP_SALE_UNITYWEB = 104,

		ON_CLICK_SALE_WEB = 39,     // Not Used on mobile.

		NEW_OOC_WEB = 40,       // Not Used on mobile.
		NEW_OOC_IOS = 41,
		NEW_OOC_ANDROID = 42,
		NEW_OOC_KINDLE = 43,
		NEW_OOC_UNITYWEB = 106, // 'Out of Coins - UnityWeb'

		OOC_THREE_IOS = 51,
		OOC_THREE_ANDROID = 52,
		OOC_THREE_KINDLE = 53,
		OOC_THREE_UNITYWEB = 108, //'Out of Coins - UnityWeb (3 pkg)'

		RAINY_DAY_OBSOLETE = 54,

		MULTI_SKU_POPCORN_SALE_IOS = 60,
		MULTI_SKU_POPCORN_SALE_ANDROID = 61,
		MULTI_SKU_POPCORN_SALE_KINDLE = 62,
		MULTI_SKU_POPCORN_SALE_UNITYWEB = 110, //Multi-Sku Popcorn Sale - UnityWeb' (SIR only?)

		BUY_PAGE_V3_IOS = 76,
		BUY_PAGE_V3_ANDROID = 77,
		BUY_PAGE_V3_KINDLE = 78,
		BUY_PAGE_V3_WINDOWS = 96,
		BUY_PAGE_V3_UNITYWEB = 111, //'Buy Page v3 - UnityWeb', 'popcorn'

		// 'buy bonus round' actions (79/80/81/82) not defined in Mobile Client
		// BUY_BONUS_ROUND_ACTION_UNITYWEB = 112, //'Buy Bonus Round - UnityWeb'
	}

	public Type type;
	public GameTimerRange timerRange = null;
	public string imageFolderPath = "";
	public int bonusSaleMultiplier = 1;
	public int maxBonusSalePercent = 0;
	public List<STUDCreditPackage> newCreditPackages = new List<STUDCreditPackage>(); // creditPackages from the new system.

	// avoids boxing and allocations when dictionary is used with enum key
	// http://stackoverflow.com/questions/26280788/dictionary-enum-key-performance
	public struct TypeEnumComparer : IEqualityComparer<Type>
	{
		public bool Equals(Type x, Type y)
		{
			return x == y;
		}

		public int GetHashCode(Type enumVal)
		{
			return (int)enumVal;
		}
	}

	private static Dictionary<Type, STUDAction> all = new Dictionary<Type, STUDAction>(new TypeEnumComparer());

	public static void populateAll(JSON studs)
	{
		if (studs == null)
		{
			Debug.LogWarning("STUDAction::populateAll - No actions to add.");
			return;
		}

		// The data isprovided in a hierarchy with the first level being the personas
		// and the second level being the actions within the personas. However, since we
		// only care about the actions, we only have a class to store the actions.
		// The data should always contain a maximum of one action of each action type,
		// across all personas.

		// Create a new collection, just in case this is a re-populate in the middle of a session.
		all = new Dictionary<Type, STUDAction>(new TypeEnumComparer());

		// Need to do it the retarded way since backend keeps making protocols with data for keys.
		List<string> ids = studs.getKeyList();
		foreach (string personaId in ids)
		{
			JSON personaJson = studs.getJSON(personaId);
			if (personaJson != null)
			{
				// Need to do it the retarded way since backend keeps making protocols with data for keys.
				List<string> actionTypeIds = personaJson.getKeyList();
				foreach (string actionTypeId in actionTypeIds)
				{
					JSON actionJson = personaJson.getJSON(actionTypeId);
					if (actionJson != null && actionJson.getBool("enabled", false))
					{
						new STUDAction(actionTypeId, actionJson);
					}
				}
			}
		}
	}

	public STUDAction(string idString, JSON data)
	{
		//Debug.LogWarning("## Adding STUD Package:" + idString);
		type = (Type)int.Parse(idString);
		int startSeconds = 0;
		int endSeconds = 0;
		JSON param = data.getJSON("parameters");
		if (param != null)
		{
			startSeconds = param.getInt("start_date", 0);
			if (startSeconds == 0)
			{
				// If there was no start_date field, then check for a popcorn start_date.
				// These should never both occur on the same action, so there won't be overlap.
				startSeconds = param.getInt("popcorn_sale_start_date", 0);
			}

			endSeconds = param.getInt("end_date", 0);
			if (endSeconds == 0)
			{
				// If there was no end_date field, then check for a popcorn end_date.
				// These should never both occur on the same action, so there won't be overlap.
				endSeconds = param.getInt("popcorn_sale_end_date", 0);
			}
			imageFolderPath = param.getString("folder_name", "");
		}
		else
		{
			// It turns out that this case is intentional and okay (we can have a stud action with no parameters):
			//Debug.LogWarning("STUDAction::STUDAction - Empty parameters on action: " + idString);
		}
		
		#if DEBUG_STUD
		  Debug.LogWarningFormat("idString: {0}, Got STUD Package Type {1}", idString, type.ToString() );
		#endif

		// Since the credit package data doesn't come in a consistently named way,
		// we must look for it based on the type of action it is.
		switch (type)
		{
			case Type.POPCORN_SALE_IOS:
			case Type.POPCORN_SALE_ANDROID:
			case Type.POPCORN_SALE_KINDLE:
			case Type.POPCORN_SALE_UNITYWEB:
				newCreditPackages = checkNewPackages(param,
					"popcorn_sale_package_1",
					"popcorn_sale_package_2",
					"popcorn_sale_package_3"
				);
				startSeconds = param.getInt("popcorn_sale_start_date", 1);
				endSeconds = param.getInt("popcorn_sale_end_date", 0);
				break;

			case Type.HAPPY_HOUR_SALE_IOS:
			case Type.HAPPY_HOUR_SALE_ANDROID:
			case Type.HAPPY_HOUR_SALE_KINDLE:
			case Type.HAPPY_HOUR_SALE_UNITYWEB:
				newCreditPackages = checkNewPackages(param,
					"happy_hour_sale_package_1",
					"happy_hour_sale_package_2"
				);
				startSeconds = param.getInt("happy_hour_sale_start_date", 1);
				endSeconds = param.getInt("happy_hour_sale_end_date", 0);
				break;

			case Type.PAYER_REACTIVATION_SALE_IOS:
			case Type.PAYER_REACTIVATION_SALE_ANDROID:
			case Type.PAYER_REACTIVATION_SALE_KINDLE:
			case Type.PAYER_REACTIVATION_SALE_UNITYWEB:
				newCreditPackages = checkNewPackages(param,
					"payer_reactivation_sale_package_1"
				);
				startSeconds = param.getInt("payer_reactivation_sale_start_date", 1);
				endSeconds = param.getInt("payer_reactivation_sale_end_date", 0);
				break;

			case Type.VIP_SALE_IOS:
			case Type.VIP_SALE_ANDROID:
			case Type.VIP_SALE_KINDLE:
			case Type.VIP_SALE_WINDOWS:
			case Type.VIP_SALE_UNITYWEB:
				newCreditPackages = checkNewPackages(param,
					"vip_sale_package_1",
					"vip_sale_package_2",
					"vip_sale_package_3"
				);
				startSeconds = param.getInt("vip_sale_start_date", 1);
				endSeconds = param.getInt("vip_sale_end_date", 0);
				break;

			case Type.NEW_OOC_ANDROID:
			case Type.NEW_OOC_IOS:
			case Type.NEW_OOC_KINDLE:
			case Type.NEW_OOC_UNITYWEB:
				newCreditPackages = checkNewPackages(param,
					"popcorn_sale_package_1");
				startSeconds = param.getInt("popcorn_sale_start_date", 1);
				endSeconds = param.getInt("popcorn_sale_end_date", 0);
				break;

			case Type.MULTI_SKU_POPCORN_SALE_IOS:
			case Type.MULTI_SKU_POPCORN_SALE_ANDROID:
			case Type.MULTI_SKU_POPCORN_SALE_KINDLE:
			case Type.MULTI_SKU_POPCORN_SALE_UNITYWEB:
				newCreditPackages = checkNewPackages(param,
					"popcorn_sale_package_1",
					"popcorn_sale_package_2",
					"popcorn_sale_package_3"
				);
				startSeconds = param.getInt("popcorn_sale_start_date", 1);
				endSeconds = param.getInt("popcorn_sale_end_date", 0);
				break;
			case Type.OOC_THREE_IOS:
			case Type.OOC_THREE_ANDROID:
			case Type.OOC_THREE_KINDLE:
			case Type.OOC_THREE_UNITYWEB:
				newCreditPackages = checkNewPackages(param,
					"popcorn_sale_package_1",
					"popcorn_sale_package_2",
					"popcorn_sale_package_3"
				);	 
				break;
			case Type.BUY_PAGE_V3_IOS:
			case Type.BUY_PAGE_V3_ANDROID:
			case Type.BUY_PAGE_V3_KINDLE:
			case Type.BUY_PAGE_V3_WINDOWS:
			case Type.BUY_PAGE_V3_UNITYWEB:
				startSeconds = param.getInt("popcorn_sale_start_date", 1);
				endSeconds = param.getInt("popcorn_sale_end_date", 0);
				newCreditPackages = checkNewPackages(param,
					"popcorn_sale_package_1",
					"popcorn_sale_package_2",
					"popcorn_sale_package_3",
					"popcorn_sale_package_4",
					"popcorn_sale_package_5",
					"popcorn_sale_package_6"
				);
				maxBonusSalePercent = 0;
				// Go through each and find the max multiplier.
				param.getInt("popcorn_sale_sale_bonus_pct", 1);
				foreach( STUDCreditPackage package in newCreditPackages)
				{
					maxBonusSalePercent = Mathf.Max(package.saleBonusPercent, maxBonusSalePercent);
				}
				break;
		}
			
		timerRange = new GameTimerRange(startSeconds, endSeconds);
		timerRange.registerFunction(setSaleNotification);

		if (all.ContainsKey(type))
		{
			Debug.LogWarning("Duplicate STUDAction type: " + type);
		}
		else
		{
			all.Add(type, this);
		}
	}

	// Set sale notification.
	private static void setSaleNotification(Dict args = null, GameTimerRange originalTimer = null)
	{
		if (Overlay.instance.top != null)
		{
			Overlay.instance.top.setupSaleNotification();
		}
	}

	// Checks if the specified popcorn credit packages exists in the data.
	// and returns a List of objects storing the package AND the bonus.
	private List<STUDCreditPackage> checkNewPackages(JSON data, params string[] keyNames)
	{
		List<STUDCreditPackage> list = new List<STUDCreditPackage>();
		foreach (string keyName in keyNames)
		{
			list.Add(new STUDCreditPackage(keyName, data));
		}
		return list;
	}

	// Returns the action of the given type, if any.
	// Only one action of any given type should exist.
	public static STUDAction find(STUDAction.Type type)
	{
		STUDAction action;
		if (all.TryGetValue(type, out action))
		{
			return action;
		}
		return null;
	}

	// Returns the STUDAction object for the popcorn sale for the current platform.
	public static STUDAction findPopcornSale()
	{
#if ZYNGA_SKU_HIR
	#if UNITY_IPHONE
		return STUDAction.find(STUDAction.Type.POPCORN_SALE_IOS);
	#elif ZYNGA_KINDLE
		return STUDAction.find(STUDAction.Type.POPCORN_SALE_KINDLE);
	#elif UNITY_WEBGL
		return STUDAction.find(STUDAction.Type.POPCORN_SALE_UNITYWEB);
	#elif UNITY_ANDROID || UNITY_EDITOR
		return STUDAction.find(STUDAction.Type.POPCORN_SALE_ANDROID);
	#else
		return null;	// This shouldn't ever really happen.
	#endif
#else
		return null;			// Unknown SKU
#endif
	}

	// Returns the STUDAction object for "out of credits" offers for the current platform.
	public static STUDAction findOutOfCreditsDialog()
	{
		if (!Packages.PaymentsManagerEnabled())
		{
			// We cannot have an active sale if the economy manager hasn't loaded.
			return null;
		}		
#if UNITY_IPHONE
		return STUDAction.find(STUDAction.Type.NEW_OOC_IOS);
#elif ZYNGA_KINDLE
		return STUDAction.find(STUDAction.Type.NEW_OOC_KINDLE);
#elif UNITY_WEBGL
		return STUDAction.find(STUDAction.Type.NEW_OOC_UNITYWEB);
#elif UNITY_ANDROID || UNITY_EDITOR
		return STUDAction.find(STUDAction.Type.NEW_OOC_ANDROID);
#else
		return null;	// This shouldn't ever really happen.
#endif
	}

	public static STUDAction findOutOfCreditsThreeDialog()
	{
		if (!Packages.PaymentsManagerEnabled())
		{
			// We cannot have an active sale if the economy manager hasn't loaded.
			return null;
		}		
#if UNITY_IPHONE
		return STUDAction.find(STUDAction.Type.OOC_THREE_IOS);
#elif UNITY_WEBGL
		return STUDAction.find(STUDAction.Type.OOC_THREE_UNITYWEB);
#elif ZYNGA_KINDLE
		return STUDAction.find(STUDAction.Type.OOC_THREE_KINDLE);
#elif UNITY_ANDROID || UNITY_EDITOR
		return STUDAction.find(STUDAction.Type.OOC_THREE_ANDROID);
#else
		return null;	// This shouldn't ever really happen.
#endif
	}

	// Returns the STUDAction object for "buy credits" offers for the current platform.
    public static STUDAction findBuyCreditsDialog()
	{
		if (!Packages.PaymentsManagerEnabled())
		{
			// We cannot have an active sale if the economy manager hasn't loaded.
			return null;
		}		
		STUDAction buyPageAction = null;		


#if UNITY_IPHONE
		buyPageAction = STUDAction.find(STUDAction.Type.BUY_PAGE_V3_IOS);
#elif ZYNGA_KINDLE
		buyPageAction = STUDAction.find(STUDAction.Type.BUY_PAGE_V3_KINDLE);
#elif UNITY_ANDROID
		buyPageAction = STUDAction.find(Type.BUY_PAGE_V3_ANDROID);
#elif UNITY_WEBGL
		buyPageAction = STUDAction.find(Type.BUY_PAGE_V3_UNITYWEB);
#elif UNITY_WSA_10_0 && NETFX_CORE
		buyPageAction = STUDAction.find(Type.BUY_PAGE_V3_WINDOWS);
#elif UNITY_EDITOR
		buyPageAction = STUDAction.find(Type.BUY_PAGE_V3_ANDROID);
#else
		return null;
#endif
		return buyPageAction;
	}
	
	public static STUDAction findHappyHourSale()
	{
		if (!Packages.PaymentsManagerEnabled())
		{
			// We cannot have an active sale if the economy manager hasn't loaded.
			return null;
		}

#if UNITY_IPHONE
		STUDAction action = STUDAction.find(STUDAction.Type.HAPPY_HOUR_SALE_IOS);
#elif ZYNGA_KINDLE
		STUDAction action =  STUDAction.find(STUDAction.Type.HAPPY_HOUR_SALE_KINDLE);
#elif UNITY_ANDROID
		STUDAction action = STUDAction.find(STUDAction.Type.HAPPY_HOUR_SALE_ANDROID);
#elif UNITY_WEBGL
		STUDAction action = STUDAction.find(STUDAction.Type.HAPPY_HOUR_SALE_UNITYWEB);
#elif UNITY_WSA_10_0 && NETFX_CORE
		STUDAction action = STUDAction.find(STUDAction.Type.HAPPY_HOUR_SALE_ANDROID);
#elif UNITY_EDITOR
		STUDAction action = STUDAction.find(STUDAction.Type.HAPPY_HOUR_SALE_ANDROID);
#else
		return null;	// This shouldn't ever really happen.
#endif

        if (action != null && action.timerRange.isActive)
		{
			return action;
		}
		// Return null if the action is already expired, because we can't use it anyway.
		return null;
	}

	public static STUDAction findPayerReactivationSale()
	{
		if (!Packages.PaymentsManagerEnabled())
		{
			// We cannot have an active sale if the economy manager hasn't loaded.
			return null;
		}		
#if UNITY_IPHONE
		return STUDAction.find(STUDAction.Type.PAYER_REACTIVATION_SALE_IOS);
#elif ZYNGA_KINDLE
		return STUDAction.find(STUDAction.Type.PAYER_REACTIVATION_SALE_KINDLE);
#elif UNITY_WEBGL
		return STUDAction.find(STUDAction.Type.PAYER_REACTIVATION_SALE_UNITYWEB);
#elif UNITY_ANDROID || UNITY_EDITOR
		return STUDAction.find(STUDAction.Type.PAYER_REACTIVATION_SALE_ANDROID);
#else
		return null;	// This shouldn't ever really happen.
#endif
	}

	public static STUDAction findVipSale()
	{
		if (!Packages.PaymentsManagerEnabled())
		{
			// We cannot have an active sale if the economy manager hasn't loaded.
			return null;
		}
#if UNITY_IPHONE
		STUDAction action = STUDAction.find(STUDAction.Type.VIP_SALE_IOS);
#elif ZYNGA_KINDLE
		STUDAction action = STUDAction.find(STUDAction.Type.VIP_SALE_KINDLE);
#elif UNITY_ANDROID
		STUDAction action = STUDAction.find(STUDAction.Type.VIP_SALE_ANDROID);
#elif UNITY_WEBGL
		STUDAction action = STUDAction.find(STUDAction.Type.VIP_SALE_UNITYWEB);
#elif UNITY_WSA_10_0 && NETFX_CORE
		STUDAction action = STUDAction.find(STUDAction.Type.VIP_SALE_WINDOWS);
#elif UNITY_EDITOR
		STUDAction action = STUDAction.find(STUDAction.Type.VIP_SALE_ANDROID);
#else
		return null;	// This shouldn't ever really happen.
#endif

        if (action != null && action.timerRange.isActive)
		{
			return action;
		}
		// Return null if the action is already expired, because we can't use it anyway.
		return null;
	}

	// Implements IResetGame
	public static void resetStaticClassData()
	{
		all = new Dictionary<Type, STUDAction>();
	}

	// Convenience class for keeping the package and the bonus information together when we grab it from STUD.
	public class STUDCreditPackage
	{
		public PurchasablePackage package;
		public long baseCoins = 0;
		public int bonus = 0;
		public int saleMultiplier = 1;
		public int saleBonusPercent = 0;
		public bool isJackpotEligible = false;
		public bool isBestValue = false;
		public bool isMostPopular = false;
		public string perkPackage = "";
		
		public STUDCreditPackage(PurchasablePackage package, int bonus, bool isJackpotEligible)
		{
			this.package = package;
			this.bonus = bonus;
			this.isJackpotEligible = isJackpotEligible;
		}

		public STUDCreditPackage(string keyName, JSON data)
		{
			string packageName = "group1_" + keyName;
			string packageKey = data.getString(packageName, "");

			bonus = data.getInt(packageName + "_bonus", 0);
			baseCoins = data.getLong(packageName + "_base_coins", 0);
			saleMultiplier = data.getInt(packageName + "_sale_multiplier", 1);
			saleBonusPercent = data.getInt(packageName + "_sale_bonus_pct", 0);
			isJackpotEligible = ExperimentWrapper.BuyPageProgressive.isInExperiment && (data.getString(packageName + "_qualifies_for_buypage_pjp", "") == "on");
			isBestValue = data.getString(packageName + "_best_value", "off") == "on";
			isMostPopular = data.getString(packageName + "_most_popular", "off") == "on";
			perkPackage = data.getString(packageName + "_buff_key", "");
			
		#if DEBUG_STUD
			Debug.LogWarningFormat("packageName: {0}, perkPackage: '{1}'",packageName, perkPackage);;
		#endif
						
			package = PurchasablePackage.find(packageKey);
			if (package == null)
			{
				Debug.LogWarning("Could not find credit package for STUD action: " + packageKey);
			}
		}

		public override string ToString()
		{
			return string.Format(
				"STUDCreditPackage:[package_key:{0},bonus_percentage:{1},sale_multiplier:{2},sale_bonus_pct:{3},base_coins:{4},is_jackpot:{5},is_best_value:{6},is_most_popular:{7},buff_key:{8}]",
				package.keyName.ToString(), bonus.ToString(), saleMultiplier.ToString(),
				saleBonusPercent.ToString(), baseCoins.ToString(),
				isJackpotEligible.ToString(), isBestValue.ToString(), isMostPopular.ToString(), perkPackage.ToString());
		}
	}
}
