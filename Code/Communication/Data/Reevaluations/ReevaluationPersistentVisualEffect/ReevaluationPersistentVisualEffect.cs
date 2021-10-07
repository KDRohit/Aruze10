using System.Collections.Generic;

//
// Reevaluation to deserialize and store "visual_data_count_on_symbol_land" or "visual_data_reset_on_bonus_name"
// reevaluation. This is used by PersistentVisualEffect to extract data to be used in
// populating the visual effect animation level, increasing, and resetting when triggered
//
// Author : Nick Saito <nsaito@zynga.com>
// Date : May 5th 2020
//
// games : gen98
//
public class ReevaluationPersistentVisualEffect : ReevaluationBase
{
	public int counter;
	public string counter_key; //example "gen98_pick"
	public List<TriggerSymbol> symbols;

	public ReevaluationPersistentVisualEffect(JSON reevalJSON) : base(reevalJSON)
	{
		counter = reevalJSON.getInt("counter", 0);
		counter_key = reevalJSON.getString("counter_key", "");

		JSON[] symbolsJSON = reevalJSON.getJsonArray("symbols");

		if (symbolsJSON != null)
		{
			symbols = new List<TriggerSymbol>();

			foreach (JSON symbolJSON in symbolsJSON)
			{
				symbols.Add(new TriggerSymbol(symbolJSON));
			}
		}
	}

	public class TriggerSymbol
	{
		public string symbol;
		public int reel;
		public int position;

		public TriggerSymbol(JSON triggerSymbolJSON)
		{
			symbol = triggerSymbolJSON.getString("symbol", "");
			reel = triggerSymbolJSON.getInt("reel", 0);
			position = triggerSymbolJSON.getInt("pos", 0);
		}
	}
}
