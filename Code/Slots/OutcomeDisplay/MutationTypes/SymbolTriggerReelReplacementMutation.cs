using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Class for parsing the mutaiton called "symbol_trigger_reel_replacement"
which was first used in the ainsworth15 freespin game.

Creation Date: 9/28/2018
Original Author: Scott Lepthien
*/
public class SymbolTriggerReelReplacementMutation : MutationBase
{
	public class TriggerSymbolInfo
	{
		public int layer;
		public int reel;
		public int pos;
		public string symbolName;
	}
	
	public class ReplacementSymbolInfo
	{
		public int layer;
		public int reel;
		public int pos;
		public string fromSymbolName;
		public string toSymbolName;
	}
	
	public class ResultInfo
	{
		public ResultInfo()
		{
			triggerSymbol = new TriggerSymbolInfo();
		}
	
		public TriggerSymbolInfo triggerSymbol;
		public List<ReplacementSymbolInfo> replacedSymbolList = new List<ReplacementSymbolInfo>();
	}

	public List<ResultInfo> results = new List<ResultInfo>();
	public HashSet<int> reelsReplaced = new HashSet<int>();

	public SymbolTriggerReelReplacementMutation(JSON mutation) : base(mutation)
	{
		JSON[] resultsArrayJson = mutation.getJsonArray("results");
		JSON triggerJson;
		JSON dataJson;

		for (int i = 0; i < resultsArrayJson.Length; i++)
		{
			dataJson = resultsArrayJson[i];
			ResultInfo currentResult = new ResultInfo();
			
			triggerJson = dataJson.getJSON("trigger");
			currentResult.triggerSymbol.layer = triggerJson.getInt("layer", 0);
			currentResult.triggerSymbol.reel = triggerJson.getInt("reel", 0);
			// adjust reel for zero based
			if (currentResult.triggerSymbol.reel > 0)
			{
				currentResult.triggerSymbol.reel -= 1;
			}
			
			currentResult.triggerSymbol.pos = triggerJson.getInt("pos", 0);
			// adjust pos for zero based
			if (currentResult.triggerSymbol.pos > 0)
			{
				currentResult.triggerSymbol.pos -= 1;
			}
			
			currentResult.triggerSymbol.symbolName = triggerJson.getString("symbol", "");

			JSON[] replacementArrayJson = dataJson.getJsonArray("replacements");
			if (replacementArrayJson != null && replacementArrayJson.Length > 0)
			{
				foreach (JSON replacementDataJson in replacementArrayJson)
				{
					ReplacementSymbolInfo replacementInfo = new ReplacementSymbolInfo();
	
					replacementInfo.layer = replacementDataJson.getInt("layer", 0);
					replacementInfo.reel = replacementDataJson.getInt("reel", 0);
					// adjust reel for zero based
					if (replacementInfo.reel > 0)
					{
						replacementInfo.reel -= 1;
					}
					
					replacementInfo.pos = replacementDataJson.getInt("pos", 0);
					// adjust pos for zero based
					if (replacementInfo.pos > 0)
					{
						replacementInfo.pos -= 1;
					}
					
					replacementInfo.fromSymbolName = replacementDataJson.getString("from_symbol", "");
					replacementInfo.toSymbolName = replacementDataJson.getString("to_symbol", "");
					currentResult.replacedSymbolList.Add(replacementInfo);
				}
			}

			for (int k = 0; k < currentResult.replacedSymbolList.Count; k++)
			{
				if (!reelsReplaced.Contains(currentResult.replacedSymbolList[k].reel))
				{
					reelsReplaced.Add(currentResult.replacedSymbolList[k].reel);
				}
			}

			results.Add(currentResult);
		}
	}
}
