using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Handles function related to handling the in-game currency economy - not real money.
*/

public static class CreditsEconomy
{
	public const long ECONOMY_MULTIPLIER_UNSET = -1;
	public const long DEFAULT_ECONOMY_MULTIPLIER = 200;

	// Explicitly sets the economy multiplier.
	// Broken out into a function call (instead of a setter) to prevent accidental setting after data is loaded.
	public static void setMultiplier(long multiplier)
	{
		_economyMultiplier = multiplier;
	}	

	// Gets the economy multiplier, if any, and caches it for future use. Returns 1 otherwise.
	public static long economyMultiplier
	{
		get
		{
			return _economyMultiplier;
		}
	}
	private static long _economyMultiplier = ECONOMY_MULTIPLIER_UNSET;
		
	// Just doing the math, but not formatting.
	public static long multipliedCredits(long credit)
	{
		// This handles if the economy multiplier hasn't been set yet, and we haven't logged into the game. 
		// This would mean their credits, xp, etc is all null. Mainly for the facebook page upon initial entry
		if (Data.isPlayerDataSet == false && economyMultiplier == ECONOMY_MULTIPLIER_UNSET)
		{
			if (SkuResources.currentSku == SkuId.HIR)
			{
				return credit * DEFAULT_ECONOMY_MULTIPLIER;
			}
			else
			{
				return credit * economyMultiplier;
			}
		}
		else
		{
			return credit * economyMultiplier;
		}
	}

	public static string convertCredits(long credit, bool shouldFormat = true)
	{
		long newCredit = multipliedCredits(credit);
		
		if (shouldFormat == true)
		{
			return CommonText.formatNumber(newCredit);
		}
		else
		{
			return newCredit.ToString();
		}
	}

	// Convert the multiplied value to an abbreviated string
	public static string multiplyAndFormatNumberAbbreviated(long credit, int decimalPoints = 1, bool shouldRoundUp = true)
	{
		long newCredit = multipliedCredits(credit);
		return CommonText.formatNumberAbbreviated(newCredit, decimalPoints,shouldRoundUp);
	}

	public static string multiplyAndFormatNumberWithCharacterLimit(long credit, int maxDecimalPoints, int maxDigits, bool shouldRoundUp = true)
	{
		long newCredit = multipliedCredits(credit);
		return CommonText.formatNumberAbbreviated(newCredit, maxDecimalPoints, shouldRoundUp, maxDigits);
	}

	// Convert the multiplied value to a truncated value with an order of magnitude suffix
	public static string multiplyAndFormatNumberTextSuffix(long credit, int decimalPoints = 1, bool shouldRoundUp = true, bool doAllCaps = true)
	{
		long newCredit = multipliedCredits(credit);
		return CommonText.formatNumberTextSuffix(newCredit, decimalPoints, shouldRoundUp, doAllCaps);
	}
}
