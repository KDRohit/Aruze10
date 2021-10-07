using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Data structure to hold the mystery gift data, which will get processed one at a time after normal spin outcomes are done.
*/

public class MysteryGift : IResetGame
{
	public static List<JSON> outcomes = new List<JSON>();	// A list of all gifts from the latest spin.
	public static GameTimerRange increasedMysteryGiftChanceRange = null;
	public static GameTimerRange increasedBigSliceChanceRange = null;
	
	public static void init()
	{
		increasedMysteryGiftChanceRange = new GameTimerRange(
			Data.liveData.getInt("INCREASE_MYSTERY_GIFT_CHANCE_START_TIMESTAMP", 0),
			Data.liveData.getInt("INCREASE_MYSTERY_GIFT_CHANCE_END_TIMESTAMP", 0),
			Data.liveData.getBool("INCREASE_MYSTERY_GIFT_ENABLED", false)
		);
		increasedMysteryGiftChanceRange.registerFunction(onMysteryGiftExpired);

		increasedBigSliceChanceRange = new GameTimerRange(
			Data.liveData.getInt("INCREASE_BIG_SLICE_CHANCE_START_TIMESTAMP", 0),
			Data.liveData.getInt("INCREASE_BIG_SLICE_CHANCE_END_TIMESTAMP", 0),
			Data.liveData.getBool("INCREASE_BIG_SLICE_ENABLED", false)
		);
		increasedBigSliceChanceRange.registerFunction(onMysteryGiftExpired);
	}

	public static void onMysteryGiftExpired(Dict args = null, GameTimerRange originalTimer = null)
	{
		LobbyInfo.refreshAllLobbyOptionButtons();
		if (Overlay.instance != null && Overlay.instance.jackpotMystery != null)
		{
			Overlay.instance.jackpotMystery.mysteryGiftExpired();
		}
	}

	// Returns whether the player should be able to have this feature.
	public static bool canHaveIncreasedMysteryGiftChance
	{
		get
		{
			return
				LobbyGame.pinnedMysteryGiftGames != null &&
				LobbyGame.pinnedMysteryGiftGames.Count > 0;
		}
	}

	// Returns whether the player currently DOES have this feature.
	public static bool isIncreasedMysteryGiftChance
	{
		get
		{
			return
				canHaveIncreasedMysteryGiftChance &&
				increasedMysteryGiftChanceRange.isActive;
		}
	}

	public static bool isIncreasedBigSliceChance
	{
		get
		{
			return
				ExperimentWrapper.IncreaseBigSliceChance.isInExperiment &&
				LobbyGame.pinnedMysteryGiftGames.Count > 0 &&
				increasedBigSliceChanceRange.isActive;
		}
	}

	// Show the next mystery gift.
	public static void showGift(JSON outcome)
	{
		outcomes.Remove(outcome);
		
		switch (GameState.game.mysteryGiftType)
		{
			case MysteryGiftType.MYSTERY_GIFT:
				MysteryGiftBaseDialog.showDialog(outcome);
				break;
			case MysteryGiftType.BIG_SLICE:
				BigSliceDialog.showDialog(outcome);
				break;
		}
	}
	
	// Implements IResetGame
	public static void resetStaticClassData()
	{
		outcomes = new List<JSON>();
		increasedMysteryGiftChanceRange = null;
		increasedBigSliceChanceRange = null;
	}
}
