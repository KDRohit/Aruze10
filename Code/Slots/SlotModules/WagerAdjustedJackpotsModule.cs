using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Module for updating Jackpot UI labels that are affected by wager amount. 
 * So labels will change values as user changes bet level.
 *
 * Original Author: Carl Gloria
 * Creation Date: 8/20/2019
 */

public class WagerAdjustedJackpotsModule : SlotModule 
{
	private const string JSON_KEY_FREESPIN = "_freespin";
	private const string JSON_KEY_SC_SYMBOLS_VALUE = "sc_symbols_value";
	private const string JSON_KEY_SYMBOL = "symbol";
	private const string JSON_KEY_CREDITS = "credits";

	[Tooltip("Info for displaying the jackpot values that change as the bet amount is updated")]
	[SerializeField] private JackpotData[] jackpotDataList;

#region SlotModule functions
	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		return true;
	}

	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		// PARSE DATA, GET MODIFIERS
		JSON[] modifierData = SlotBaseGame.instance.modifierExports;

		string reSpinDataKey = GameState.game.keyName + JSON_KEY_FREESPIN;

		JSON featureInitData = null;
		for (int i = 0; i < modifierData.Length; i++)
		{
			if (modifierData[i].hasKey(reSpinDataKey))
			{
				featureInitData = modifierData[i].getJSON(reSpinDataKey);
				break;
			}
		}

		if (featureInitData != null)
		{
			if (featureInitData.hasKey(JSON_KEY_SC_SYMBOLS_VALUE))
			{
				JSON[] values = featureInitData.getJsonArray(JSON_KEY_SC_SYMBOLS_VALUE);

				for (int i = 0; i < jackpotDataList.Length; i++)
				{
					initJackpot(jackpotDataList[i], values);
				}
			}
		}
		else
		{
			Debug.LogWarning("Starting Information not found. Check the reel set data JSON.");
		}

		yield break;
	}

	public override bool needsToExecuteOnWagerChange(long currentWager)
	{
		return reelGame != null;
	}

	public override void executeOnWagerChange(long currentWager)
	{
		for (int i = 0; i < jackpotDataList.Length; i++)
		{
			jackpotDataList[i].updateValueLabels();
		}
	}
#endregion

	private void initJackpot(JackpotData jackpotData, JSON[] values)
	{
		for (int i = 0; i < values.Length; i++)
		{
			if (values[i].hasKey(JSON_KEY_SYMBOL))
			{
				string symbolName = values[i].getString(JSON_KEY_SYMBOL, "");

				if (symbolName == jackpotData.jackpotKey)
				{
					long miniJackpotValue = values[i].getLong(JSON_KEY_CREDITS, 0);
					jackpotData.init(reelGame, miniJackpotValue);

					return;
				}
			}
		}
			
#if UNITY_EDITOR
		Debug.LogError("WagerAdjustedJackpotsModule.initJackpot() Couldn't find entry in JSON for jackpotKey = " + jackpotData.jackpotKey);
#endif
	}

	[System.Serializable]
	public class JackpotData
	{
		[Tooltip("Key for non progressive jackpot types that change as the bet amount is changed.  Often named mini/major/minor/maxi ex. zynga06")]
		public string jackpotKey;
		[Tooltip("Labels for this jackpot that need to update as the bet amount changes")]
		[SerializeField] private LabelWrapperComponent[] valueLabels;
		private ReelGame reelGame;
		private long basePayout;
		private bool isInit = false;

		public void init(ReelGame reelGame, long basePayout)
		{
			this.reelGame = reelGame;
			this.basePayout = basePayout;
			isInit = true;
			updateValueLabels();
		}

		// Used for when the player is changing their bet to update the value labels with the
		// new value for the jackpot
		public void updateValueLabels()
		{
			if (isInit)
			{
				long totalPayout = basePayout * reelGame.multiplier;

				for (int i = 0; i < valueLabels.Length; i++)
				{
					valueLabels[i].text = CreditsEconomy.multiplyAndFormatNumberAbbreviated(credit: totalPayout, decimalPoints: 1, shouldRoundUp: false);
				}
			}
		}
	}
}