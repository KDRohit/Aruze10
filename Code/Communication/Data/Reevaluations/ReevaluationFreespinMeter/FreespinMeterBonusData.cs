using System.Collections.Generic;

public class FreespinMeterBonusData
{
	public string tier;
	public int freeSpins;
	public int freeSpinsReset;
	public int wagerMultiplier;
	public int wagerMultiplierReset;
	public int payoutMultiplier;
	public List<FreeSpinMeterBonusSymbol> symbols;

	public FreespinMeterBonusData(JSON bonusJSON)
	{
		if (bonusJSON == null)
		{
			return;
		}

		tier = bonusJSON.getString("tier", "");
		freeSpins = bonusJSON.getInt("free_spins", 0);
		freeSpinsReset = bonusJSON.getInt("free_spins_reset", 0);
		wagerMultiplier = bonusJSON.getInt("wager_multiplier", 0);
		wagerMultiplierReset = bonusJSON.getInt("wager_multiplier_reset", 0);
		payoutMultiplier = bonusJSON.getInt("payout_multiplier", 0);

		JSON[] symbolsJSONArray = bonusJSON.getJsonArray("symbols", true);
		if (symbolsJSONArray != null && symbolsJSONArray.Length > 0)
		{
			symbols = new List<FreeSpinMeterBonusSymbol>();
			foreach (JSON symbolJSON in symbolsJSONArray)
			{
				symbols.Add(new FreeSpinMeterBonusSymbol(symbolJSON));
			}
		}
	}

	public class FreeSpinMeterBonusSymbol
	{
		public string symbol;
		public int reel;
		public int pos;

		public FreeSpinMeterBonusSymbol(JSON symbolJSON)
		{
			symbol = symbolJSON.getString("symbol", "");
			reel = symbolJSON.getInt("reel", 0);
			pos = symbolJSON.getInt("pos", 0);
		}
	}
}