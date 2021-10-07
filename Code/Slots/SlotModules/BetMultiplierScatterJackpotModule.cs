using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Used for the SCM and SCF scatter jackpot payouts that happen in games like Elvis03.

Creation Date: 5/4/2018
Original Author: Scott Lepthien
*/
public class BetMultiplierScatterJackpotModule : SlotModule
{
	[System.Serializable]
	public class BetMultiplierScatterJackpotData
	{
		public string symbolName;
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
				valueLabels[i].text = CreditsEconomy.convertCredits(totalPayout);
			}
		}
	}

	[SerializeField] private BetMultiplierScatterJackpotData[] scatterJackpotDataList;
	[SerializeField] private string jackpotContainerJsonKey = "scatter_payout_jackpot";

	public override void Awake()
	{
		base.Awake();
		
		// Attempt to append the game keyname to the jackpot container key (if it is setup for string.Format)
		jackpotContainerJsonKey = string.Format(jackpotContainerJsonKey, GameState.game.keyName);
	}

	//executeOnSlotGameStartedNoCoroutine() section
	//executes right when a base game starts or when a freespin game finishes initing.
	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		return true;
	}

	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		// if this is a freespins game we need to extract the basePayout info from the same module on the base game
		// since that will have gotten that data and stored it form the started data
		if (reelGame is FreeSpinGame)
		{
			BetMultiplierScatterJackpotModule baseGameModule = getModuleOnBaseGame();
			if (baseGameModule == null)
			{
				Debug.LogError("BetMultiplierScatterJackpotModule.executeOnSlotGameStartedNoCoroutine() - Unable to find matching module on base game, destroying this module!");
				
				// Force the labels to be zeroed out so they don't get stuck with whatever
				// they might have set on them in the prefab
				for (int i = 0; i < scatterJackpotDataList.Length; i++)
				{
					scatterJackpotDataList[i].init(0);
				}
				
				// remove this module since it isn't going to function correctly 
				// without being able to grab the base game data
				Destroy(this);

				return;
			}
			else
			{
				for (int i = 0; i < scatterJackpotDataList.Length; i++)
				{
					long basePayout = baseGameModule.getBasePayoutForScatterJackpotSymbol(scatterJackpotDataList[i].symbolName);
					scatterJackpotDataList[i].init(basePayout);
				}
			}
		}
		else
		{
			for (int i = 0; i < scatterJackpotDataList.Length; i++)
			{
				long basePayout = getBasePayoutForScatterJackpotSymbol(reelSetDataJson, scatterJackpotDataList[i].symbolName);
				scatterJackpotDataList[i].init(basePayout);
			}
		}
	}

	// Helper function to get the same module attached to the base game
	private static BetMultiplierScatterJackpotModule getModuleOnBaseGame()
	{
		if (SlotBaseGame.instance != null)
		{
			for (int i = 0; i < SlotBaseGame.instance.cachedAttachedSlotModules.Count; i++)
			{
				BetMultiplierScatterJackpotModule module = SlotBaseGame.instance.cachedAttachedSlotModules[i] as BetMultiplierScatterJackpotModule;
				if (module != null)
				{
					return module;
				}
			}
		}

		return null;
	}

	// Find the base payout for use by FreeSpinGame to extract this data which should have already been obtained from the started
	// info on the base game
	private long getBasePayoutForScatterJackpotSymbol(string symbolName)
	{
		for (int i = 0; i < scatterJackpotDataList.Length; i++)
		{
			if (scatterJackpotDataList[i].symbolName == symbolName)
			{
				return scatterJackpotDataList[i].basePayout;
			}
		}

		return 0;
	}

	// Find the base payout info for the passed scatter jackpot inside of the slot game started info and return it
	private long getBasePayoutForScatterJackpotSymbol(JSON reelSetDataJson, string symbolName)
	{
		JSON[] modifiers = reelSetDataJson.getJsonArray("modifier_exports");

		for (int i = 0; i < modifiers.Length; i++)
		{
			JSON scatterPayoutJackpotJSON = modifiers[i].getJSON(jackpotContainerJsonKey);
			if (scatterPayoutJackpotJSON != null)
			{
				JSON symbolEntry = scatterPayoutJackpotJSON.getJSON(symbolName);
				if (symbolEntry != null)
				{
					return symbolEntry.getLong("credits", 0);
				}
			}
		}

		long symbolCredits = getBasePayoutForScatterJackpotFromUnlockAreaModifierExport(symbolName);

		if (symbolCredits == 0)
		{
			Debug.LogWarning("BetMultiplierScatterJackpotModule.getBasePayoutForScatterJackpotSymbol() - Unable to find entry for symbolName = " + symbolName);
		}

		return symbolCredits;
	}

	// for gen86 we get the same data the unlock_area_and_reset_freespin modifier export because it actually
	// is created from a different mutator on the server. So look for that and get the credit value here.
	private long getBasePayoutForScatterJackpotFromUnlockAreaModifierExport(string symbolName)
	{
		if (ReelGame.activeGame.modifierExports == null)
		{
			return 0;
		}

		foreach (JSON exportJSON in ReelGame.activeGame.modifierExports)
		{
			if (exportJSON.getString("type", "") == "unlock_area_and_reset_freespin")
			{
				JSON[] nonProgressiveJackpotPayouts = exportJSON.getJsonArray("non_progressive_jackpot_payouts");
				if (nonProgressiveJackpotPayouts != null && nonProgressiveJackpotPayouts.Length > 0)
				{
					foreach (JSON nonProgressiveJackputPayout in nonProgressiveJackpotPayouts)
					{
						if (nonProgressiveJackputPayout.getString("symbol_name", "") == symbolName)
						{
							return nonProgressiveJackputPayout.getLong("credits", 0);
						}
					}
				}
			}
		}

		return 0;
	}

	//Update the active jackpots on wager change
	public override bool needsToExecuteOnWagerChange(long currentWager)
	{		
		return true;
	}

	public override void executeOnWagerChange(long currentWager)
	{
		for (int i = 0; i < scatterJackpotDataList.Length; i++)
		{
			scatterJackpotDataList[i].updateValueLabels();
		}
	}
}
