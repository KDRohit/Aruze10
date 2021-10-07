using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Zynga.Core.Util;

/**
 * Buy Page Perks Feature
*/

public class BuyPagePerk : IResetGame
{
	public static GameTimerRange timerRange = null;

	public static void init()
	{
		// ensure that the experiment is inited
		timerRange = new GameTimerRange(
			ExperimentWrapper.BuyPageBooster.startTimeInSecs,
			ExperimentWrapper.BuyPageBooster.endTimeInSecs,
			ExperimentWrapper.BuyPageBooster.isInExperiment);
		if (isActive)
		{
			registerPerkDeactiveDelegate();
		}
	}

	// NOTE this means the buy page perk event is active, not that the perk is in use
	public static bool isActive
	{
		get
			{
				return (timerRange!=null) && timerRange.isActive;
			}
	}

	// NOTE this means the buy page perk event is active and its perk is the best one available, not that the actual perk is in use
	public static bool isActiveAndBest
	{
		get
			{
				if (isActive)
				{
					List<string> bufDefKeys = findPerkBuffDefKeyNames();
					if (bufDefKeys.Count > 0)
					{
						// if a  null def is returned that means the buy page def is not the better one,.
						return (BuffDef.find(bufDefKeys[0], onlyIfBetterThanActive:true) != null);
					}
				}
				return false;
			}
	}

	// returns true if the buy page perk is currently in use
	public static bool isInUse
	{
		get
			{
				if (isActive)
				{
					List<string> bufDefKeys = findPerkBuffDefKeyNames();
					if (bufDefKeys.Count > 0)
					{
						// considered to be in use if exact mactch is found with active perk
						return (BuffDef.isActivated(bufDefKeys[0]));
					}
				}
				return false;
			}
	}	

	public static long timeRemaining
	{
		get
			{
				return timerRange.timeRemaining;
			}
	}

	public static void onDeactivateDelegate(BuffDef buffDef)
	{
		if (buffDef != null && findPerkBuffDefKeyNames().Contains(buffDef.keyName))
		{
			Buff.log("Perk deactivated {0}", buffDef.keyName);
			Buff.registerDeactivateDelegate(onDeactivateDelegate);
			BuyPagePerkMOTD.showDialog(isEventExpiryDialog:true);
		}
	}

	private static void registerPerkDeactiveDelegate()
	{
		Buff.registerDeactivateDelegate(onDeactivateDelegate);
	}

	private static List<string> findPerkBuffDefKeyNames()
	{
		List<string> perkBuffDefKeys = new List<string>();
		PurchaseFeatureData featureData = PurchaseFeatureData.BuyPage;
		if (featureData != null)
		{
			for (int i = 0; i < featureData.creditPackages.Count; i++)
			{
				string buffDefKey = featureData.creditPackages[i].perkPackage;
				if (!string.IsNullOrEmpty(buffDefKey))
				{
					BuffDef buffDef = BuffDef.find(buffDefKey);
					if (buffDef != null)
					{
						perkBuffDefKeys.Add(buffDef.keyName);
					}
				}
			}
		}
		return perkBuffDefKeys;
	}
	
	// Implements IResetGame
	public static void resetStaticClassData()
	{
		timerRange = null;
	}
}
