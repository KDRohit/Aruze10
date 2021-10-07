using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModifierExportUnlockAreaAndResetFreespinSlotModule : SlotModule
{
	public ModifierExportUnlockAreaAndResetFreespin modifierExportUnlockAreaAndResetFreespin;

	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		return reelSetDataJson.hasKey("modifier_exports");
	}

	// Get the startup data so that we can initialize the locked row values
	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		JSON[] modifierExports = reelSetDataJson.getJsonArray("modifier_exports");
		
		// this is where we update our initial locked row values from startup data
		foreach (JSON exportJSON in modifierExports)
		{
			if (exportJSON.getString("type", "") == "unlock_area_and_reset_freespin")
			{
				modifierExportUnlockAreaAndResetFreespin = new ModifierExportUnlockAreaAndResetFreespin(exportJSON);
			}
		}

		yield break;
	}

	public class ModifierExportUnlockAreaAndResetFreespin
	{
		public string type;
		public int initialLockedRow;
		public List<ReevaluationUnlockAreaAndResetFreespin.UnlockAreaAndResetLockedRowsInfo> lockedRows;
		public List<ModifierExportUnlockAreaAndResetFreespinNonProgressiveJackpotPayouts> jackpotPayouts;
		public List<ScatterSymbolPayout> scatterSymbolPayouts;

		public ModifierExportUnlockAreaAndResetFreespin(JSON exportJSON)
		{
			type = exportJSON.getString("type", "");
			initialLockedRow = exportJSON.getInt("initial_locked_row", 0);

			JSON[] lockedRowsInfoArray = exportJSON.getJsonArray("locked_rows_info", true);

			if (lockedRowsInfoArray != null && lockedRowsInfoArray.Length > 0)
			{
				lockedRows = new List<ReevaluationUnlockAreaAndResetFreespin.UnlockAreaAndResetLockedRowsInfo>();
				foreach (JSON lockedRowJSON in lockedRowsInfoArray)
				{
					lockedRows.Add(new ReevaluationUnlockAreaAndResetFreespin.UnlockAreaAndResetLockedRowsInfo(lockedRowJSON));
				}
			}

			JSON[] scatterDataArray = exportJSON.getJsonArray("scatter_symbols_payouts", true);

			if (scatterDataArray != null && scatterDataArray.Length > 0)
			{
				scatterSymbolPayouts = new List<ScatterSymbolPayout>();
				foreach (JSON scatterJSON in scatterDataArray)
				{
					scatterSymbolPayouts.Add(
						new ScatterSymbolPayout(scatterJSON));
				}
			}

			JSON[] jackpotDataArray = exportJSON.getJsonArray("scatter", true);

			if (jackpotDataArray != null && jackpotDataArray.Length > 0)
			{
				jackpotPayouts = new List<ModifierExportUnlockAreaAndResetFreespinNonProgressiveJackpotPayouts>();
				foreach (JSON jackpotJSON in jackpotDataArray)
				{
					jackpotPayouts.Add(
						new ModifierExportUnlockAreaAndResetFreespinNonProgressiveJackpotPayouts(jackpotJSON));
				}
			}
		}
	}

	public class ModifierExportUnlockAreaAndResetFreespinNonProgressiveJackpotPayouts
	{
		public string symbolName;
		public int credits;
		public float wagerMultiplier;

		public ModifierExportUnlockAreaAndResetFreespinNonProgressiveJackpotPayouts(JSON jackpotJSON)
		{
			symbolName = jackpotJSON.getString("symbol_name", "");
			credits = jackpotJSON.getInt("credits", 0);
			wagerMultiplier = jackpotJSON.getFloat("wager_multiplier", 0);
		}
	}

	public class ScatterSymbolPayout
	{
		public string symbolName;
		public long credits;
		public long wagerMultiplier;
		public bool isProgressiveJackpot = false;

		public ScatterSymbolPayout()
		{
			//default constructor
		}

		public ScatterSymbolPayout(JSON json)
		{
			symbolName = json.getString("symbol_name", "");
			credits = json.getLong("credits", 0);
			wagerMultiplier = json.getLong("wager_multiplier", 0);
		}
	}
}
