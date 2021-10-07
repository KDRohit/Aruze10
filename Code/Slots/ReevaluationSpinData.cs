using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Class to store data about spin reevaluations
*/
public class ReevaluationSpinData
{
	public HashSet<int> staticReels = new HashSet<int>();
	public int[] reelStops;
	public Dictionary<int, Dictionary<int, string>> stickySymbols = new Dictionary<int, Dictionary<int, string>>();
	public Dictionary<int, Dictionary<int, string>> stickySCSymbols = new Dictionary<int, Dictionary<int, string>>();
	public List<List<string>> reevaluatedMatrix = new List<List<string>>();
	public List<SlotOutcome> subOutcomes = new List<SlotOutcome>();				// Payout info

	public const string FIELD_STATIC_REELS = "static_reels";
	public const string FIELD_REEL_STOPS = "reevaluated_stops";
	public const string FIELD_STICKY_SYMBOLS = "new_stickies";
	public const string FIELD_STICKY_SC_SYMBOLS = "scatter_stickies";
	public const string FIELD_REEVALUATED_MATRIX = "reevaluated_matrix";
	public const string FIELD_OUTCOMES = "outcomes";

	private const string PROPERTY_STICKY_SYMBOLS_COLUMN = "reel";
	private const string PROPERTY_STICKY_SYMBOLS_ROW = "position";
	private const string PROPERTY_STICK_SYMBOLS_NAME = "to_symbol";

	public ReevaluationSpinData(JSON reevaluationJson)
	{
		int[] staticReelsArray = reevaluationJson.getIntArray(FIELD_STATIC_REELS);

		foreach (int staticReel in staticReelsArray)
		{
			staticReels.Add(staticReel);
		}

		reelStops = reevaluationJson.getIntArray(FIELD_REEL_STOPS);

		JSON[] stickyLocations = reevaluationJson.getJsonArray(FIELD_STICKY_SYMBOLS);
		
		if (stickyLocations != null && stickyLocations.Length > 0)
		{
			foreach (JSON stickySymbolLoc in stickyLocations)
			{
				int column = System.Convert.ToInt32(stickySymbolLoc.getInt(PROPERTY_STICKY_SYMBOLS_COLUMN, 0));
				int row = System.Convert.ToInt32(stickySymbolLoc.getInt(PROPERTY_STICKY_SYMBOLS_ROW, 0));

				if (!stickySymbols.ContainsKey(column))
				{
					stickySymbols.Add(column, new Dictionary<int, string>());
				}

				stickySymbols[column].Add(row, stickySymbolLoc.getString(PROPERTY_STICK_SYMBOLS_NAME, ""));
			}
		}

		JSON[] scStickyLocations = reevaluationJson.getJsonArray(FIELD_STICKY_SC_SYMBOLS);
		
		if (scStickyLocations != null && scStickyLocations.Length > 0)
		{
			foreach (JSON stickySymbolLoc in scStickyLocations)
			{
				int column = System.Convert.ToInt32(stickySymbolLoc.getInt(PROPERTY_STICKY_SYMBOLS_COLUMN, 0));
				int row = System.Convert.ToInt32(stickySymbolLoc.getInt(PROPERTY_STICKY_SYMBOLS_ROW, 0));

				if (!stickySCSymbols.ContainsKey(column))
				{
					stickySCSymbols.Add(column, new Dictionary<int, string>());
				}
				
				stickySCSymbols[column].Add(row, stickySymbolLoc.getString(PROPERTY_STICK_SYMBOLS_NAME, ""));
			}
		}

		reevaluatedMatrix = reevaluationJson.getStringListList(FIELD_REEVALUATED_MATRIX);

		JSON[] outcomes = reevaluationJson.getJsonArray(FIELD_OUTCOMES);
		foreach (JSON outcome in outcomes)
		{
			subOutcomes.Add(new SlotOutcome(outcome));
		}
	}

	/// Tells if there are sub outcomes
	public bool hasSubOutcomes()
	{
		return subOutcomes.Count > 0;
	}

	/// Print out what is stored, used for debugging
	public void printData()
	{
		string arrayDataStr = "";

		foreach (int staticReel in staticReels)
		{
			arrayDataStr += " " + staticReel.ToString();
		}

		Debug.Log("static_reels = {" + arrayDataStr + " }");

		arrayDataStr = "";
		for (int i = 0; i < reelStops.Length; i++)
		{
			arrayDataStr += " " + reelStops[i].ToString();
		}

		Debug.Log("reelStops = {" + arrayDataStr + " }");

		string stickSymbolStr = "stickySymbols = \n{\n";
		foreach(KeyValuePair<int, Dictionary<int, string>> columnData in stickySymbols)
		{
			int column = columnData.Key;

			foreach(KeyValuePair<int, string> rowData in columnData.Value)
			{
				int row = rowData.Key;
				stickSymbolStr += " [" + column + "][" + row + "] = " + rowData.Value + "\n";
			}
		}
		stickSymbolStr += "}";
		Debug.Log(stickSymbolStr);
	}
}
