using Com.HitItRich.Feature.TimedBonus;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

public class PremiumSlice : IResetGame
{
	private class SliceData
	{
		public bool isPremium;
		public long creditValue;

		public SliceData(long credits, bool premium)
		{
			isPremium = premium;
			creditValue = credits;
		}
	}

	private const string PREMIUM_SLICE_GROUP = "premium_slice";
	private const string PACKAGE_KEY_FIELD = "premium_slice_key";
	private const string OUTCOME_TYPE_FIELD = "outcome_type";
	private const string BONUS_GAME_NAME_FIELD = "bonus_game";
	private const string PAY_TABLE_FIELD = "bonus_game_pay_table";
	private const string ROUNDS_FIELD = "rounds";
	private const string SLICE_WINS_FIELD = "wins";
	
	private static bool logPackageError = true;

	public long sliceValue { get; private set; }
	public string packageName { get; private set; }
	public string outcomeType { get; private set; }
	public string bonusGameName { get; private set; }
	public string bonusGamePayTable { get; private set; }
	public int nextPremiumSliceIndex { get; private set; }

	
	private bool premiumSliceOffer;
	private string roundId;
	private Dictionary<string, SliceData> wheelData;
	private ReadOnlyCollection<string> readOnlyWinList;
	private string[] orderedWinList;

	public static void instantiateFeature(JSON data)
	{
		if (instance != null)
		{
			instance.unregisterServerEvents();
		}
		instance = new PremiumSlice();
		instance.init(data);
	}

	public static PremiumSlice instance { get; private set; }
	
	public void init(JSON data)
	{
		premiumSliceOffer = parseLoginData(data) && readPayTable(); //parse paytable for wheel slice order
		setNextPremiumSliceIndex();
		registerServerEvents();
	}

	public static string sliceCost
	{
		get
		{
			PremiumSlicePackage package = getCurrentPackage();
			return package != null && package.purchasePackage != null ? package.purchasePackage.getLocalizedPrice() : "";
		}
	}

	public ReadOnlyCollection<string> getSliceOrder()
	{
		return readOnlyWinList;
	}

	public static string sliceCreditValueAbbreviated
	{
		get
		{
			long creditAmount = instance != null ? instance.sliceValue : 0L;
			return CreditsEconomy.multiplyAndFormatNumberAbbreviated(creditAmount, 0, false);
		}
	}

	public static string sliceCreditValue
	{
		get
		{
			long creditAmount = instance != null ? instance.sliceValue : 0L;
			return CreditsEconomy.convertCredits(creditAmount);
		}
	}


	private void registerServerEvents()
	{
		Server.registerEventDelegate("premium_slice_outcome", onSlicePurchased, true);
	}

	private void unregisterServerEvents()
	{
		Server.unregisterEventDelegate("premium_slice_outcome", onSlicePurchased, true);
	}

	private void onSlicePurchased(JSON outcome)
	{
		PremiumSliceData data = new PremiumSliceData(outcome);
		NewDailyBonusDialog.showPremiumSpin(data);
	}

	public bool hasOffer()
	{
		return premiumSliceOffer;
	}

	public void markOfferComplete()
	{
		premiumSliceOffer = false;
	}

	public long getCreditsForSlice(int sliceIndex)
	{
		if (wheelData != null && orderedWinList != null && orderedWinList.Length > sliceIndex)
		{
			string winId = orderedWinList[sliceIndex];
			SliceData data = null;
			if (wheelData.TryGetValue(winId, out data))
			{
				return data.creditValue;
			}
		}
		return 0;
	}

	private void setNextPremiumSliceIndex()
	{
		nextPremiumSliceIndex = 0;
		if (orderedWinList != null)
		{
			for (int i = 0; i < orderedWinList.Length; i++)
			{
				if (!isSlicePremiun(i))
				{
					nextPremiumSliceIndex = i;
					break;
				}
			}	
		}
	}

	public HashSet<int> getAllPremiumSliceIndecies()
	{
		HashSet<int> premiumSlices = new HashSet<int>();
		if (wheelData != null)
		{
			HashSet<string> allWinIds = new HashSet<string>();
			foreach (KeyValuePair<string,SliceData> kvp in wheelData)
			{
				
				if (kvp.Value != null && kvp.Value.isPremium)
				{
					allWinIds.Add(kvp.Key);
				}
			}

			if (orderedWinList != null)
			{
				for (int i = 0; i < orderedWinList.Length; i++)
				{
					if(!string.IsNullOrEmpty(orderedWinList[i]) && allWinIds.Contains(orderedWinList[i]))
					{
						premiumSlices.Add(i);
					}
				}	
			}
		}

		return premiumSlices;
	}

	public bool isSlicePremiun(int sliceIndex)
	{
		if (orderedWinList != null && orderedWinList.Length > sliceIndex)
		{
			string winId = orderedWinList[sliceIndex];
			SliceData data = null;
			if (wheelData.TryGetValue(winId, out data))
			{
				return data.isPremium;
			}
		}
		return false;
	}

	private bool readPayTable()
	{
		JSON paytable = BonusGamePaytable.findPaytable("base_bonus", bonusGamePayTable);
		readOnlyWinList = null;
		if (paytable != null)
		{
			JSON[] rounds = paytable.getJsonArray("rounds");
			if (rounds != null)
			{
				for (int i = 0; i < rounds.Length; i++)
				{
					JSON roundData = rounds[i];
					if (roundData == null)
					{
						continue;
					}

					string id = roundData.getString("id", "");
					if (id == roundId)
					{
						JSON[] wins = roundData.getJsonArray("wins");
						{
							orderedWinList = new string[wins.Length];
							for (int winIndex = 0; winIndex < wins.Length; winIndex++)
							{
								int position = wins[winIndex].getInt("sort_index", -1);
								if (position >= 1)
								{
									--position;  //sort index is 1 based
									orderedWinList[position] = wins[winIndex].getString("id", "");
								}
							}
						}
						break;
					}
				}
				readOnlyWinList = new ReadOnlyCollection<string>(orderedWinList);
			}
		}
		else
		{
			Debug.LogError("Could not find premium slice paytable -- disabling feature");
			return false;
		}

		return true;
	}

	private bool parseLoginData(JSON data)
	{
		if (data == null)
		{
			return false;
		}

		nextPremiumSliceIndex = -1;
		wheelData = new Dictionary<string, SliceData>();
		packageName = data.getString(PACKAGE_KEY_FIELD, "");
		outcomeType = data.getString(OUTCOME_TYPE_FIELD, "");
		bonusGameName = data.getString(BONUS_GAME_NAME_FIELD, "");
		bonusGamePayTable = data.getString(PAY_TABLE_FIELD, "");
		sliceValue = 0;
		JSON roundData = data.getJSON(ROUNDS_FIELD);
		if (roundData != null)
		{
			roundId = roundData.getString("round_id", "");
			JSON wins = roundData.getJSON(SLICE_WINS_FIELD);
			if (wins != null)
			{
				int index = 0;
				foreach (string key in wins.getKeyList())
				{
					if (string.IsNullOrEmpty(key))
					{
						continue;
					}
					JSON sliceItem = wins.getJSON(key);
					if (sliceItem != null)
					{
						bool isPremium = sliceItem.getString("group", "") == PREMIUM_SLICE_GROUP;
						SliceData sliceData = new SliceData(sliceItem.getLong("credits", 0L), isPremium);
						if (sliceData.creditValue > sliceValue)
						{
							sliceValue = sliceData.creditValue;
						}
						wheelData.Add(key, sliceData);
					}
					index++;
				}
				return wheelData.Count > 0;
			}
		}
		return false;
	}

	public static void purchasePremiumSlice()
	{
		PremiumSlicePackage package = getCurrentPackage();
		if (package != null)
		{
			logPurchase();
			if (package.purchasePackage != null)
			{
				package.purchasePackage.makePurchase(0,false, -1, "PremiumSlicePackage");	
			}
			else if (logPackageError)
			{
				logPackageError = false;
				Debug.LogError("Purchase package is null for premium slice");
			}

		}
		else if (logPackageError)
		{
			logPackageError = false;
			Debug.LogError("No premium slice package available");
		}
	}
	
	public static PremiumSlicePackage getCurrentPackage()
	{
		if (instance == null)
		{
			return null;
		}
		
		PurchaseFeatureData data = PurchaseFeatureData.PremiumSlice;
		if (data != null && data.premiumSlicePackages != null && data.premiumSlicePackages.Count > 0)
		{
			for (int i = 0; i < data.premiumSlicePackages.Count; i++)
			{
				if (data.premiumSlicePackages[i].purchasePackage.keyName == instance.packageName)
				{
					return data.premiumSlicePackages[i];
				}
			}
		}
		return null;
	}
	
	private static void logPurchase()
	{
		//Currently no stats are being logged.
	}
	~PremiumSlice()
	{
		unregisterServerEvents();
	}
	
	public static void resetStaticClassData()
	{
		logPackageError = true;
	}
	
}
