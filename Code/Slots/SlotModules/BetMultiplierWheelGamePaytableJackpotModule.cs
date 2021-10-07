using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Used for the display of fixed wheel game paytable jackpot payouts which need to be displayed 
on UI outside of the wheel game.

Creation Date: 4/1/2021
Original Author: Scott Lepthien
*/
public class BetMultiplierWheelGamePaytableJackpotModule : SlotModule
{
	[System.Serializable]
	public class BetMultiplierWheelJackpotData
	{
		[Tooltip("This is the Extra Data field from SCAT, which should contain something identifying this jackpot.")]
		public string extraData;
		[System.NonSerialized] public long basePayout;
		[SerializeField] private LabelWrapperComponent[] valueLabels;

		public void init(long basePayout)
		{
			this.basePayout = basePayout;
			updateValueLabels();
		}

		// Used for when the player is changing their bet to update the value labels with the
		// new value for the jackpot
		public void updateValueLabels()
		{
			long totalPayout = basePayout * SlotBaseGame.instance.multiplier;

			for (int i = 0; i < valueLabels.Length; i++)
			{
				valueLabels[i].text = CreditsEconomy.multiplyAndFormatNumberAbbreviated(totalPayout);
			}
		}
	}

	[SerializeField] private BetMultiplierWheelJackpotData[] wheelJackpotDataList;
	[Tooltip("This is the wheel paytable to check. Supports using \"{0}\" in the name to auto include the game key.")]
	[SerializeField] private string wheelPaytableKey = "{0}_wheel";
	[Tooltip("This will determine what round of the wheel paytable is checked.  Probably for the most part this will be 0, but some game may have multiple rounds with the jackpots only in a certain round.")]
	[SerializeField] private int roundToExtractFrom = 0;

	public override void Awake()
	{
		base.Awake();
		
		// Attempt to append the game keyname to the string if it contains {0}
		wheelPaytableKey = string.Format(wheelPaytableKey, GameState.game.keyName);
	}

	//executeOnSlotGameStartedNoCoroutine() section
	//executes right when a base game starts or when a freespin game finishes initing.
	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		return true;
	}

	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		JSON wheelPaytableJson = BonusGamePaytable.findPaytable(BonusGamePaytable.WHEEL_PAYTABLE, wheelPaytableKey);

		if (wheelPaytableJson != null)
		{
			for (int i = 0; i < wheelJackpotDataList.Length; i++)
			{
				long basePayout = getWheelSegmentValueForSegmentWithExtraData(wheelPaytableJson, wheelJackpotDataList[i].extraData);
				wheelJackpotDataList[i].init(basePayout);
			}
		}
		else
		{
			Debug.LogError($"BetMultiplierWheelGamePaytableJackpotModule.executeOnSlotGameStartedNoCoroutine() - Unable to find wheel paytable data for wheelPaytableKey = {wheelPaytableKey}");
		}
	}

	// Find the segment that has a matching extra data field and return its credit value (will return 0 if no match is found)
	private long getWheelSegmentValueForSegmentWithExtraData(JSON wheelPaytableJson, string segmentExtraData)
	{
		JSON[] rounds = wheelPaytableJson.getJsonArray("rounds");
		JSON[] roundWins = rounds[roundToExtractFrom].getJsonArray("wins");
		
		for (int i = 0; i < roundWins.Length; i++)
		{
			string currentWinExtraData = roundWins[i].getString("extra_data", "");
			if (segmentExtraData == currentWinExtraData)
			{
				return roundWins[i].getLong("credits", 0L);
			}
		}
		
		Debug.LogError($"BetMultiplierWheelGamePaytableJackpotModule.getWheelSegmentValueForSegmentWithExtraData() - Unable to find matching wheel segment with segmentExtraData = {segmentExtraData}; returning 0!");
		return 0;
	}

	//Update the active jackpots on wager change
	public override bool needsToExecuteOnWagerChange(long currentWager)
	{		
		return true;
	}

	public override void executeOnWagerChange(long currentWager)
	{
		for (int i = 0; i < wheelJackpotDataList.Length; i++)
		{
			wheelJackpotDataList[i].updateValueLabels();
		}
	}
}
