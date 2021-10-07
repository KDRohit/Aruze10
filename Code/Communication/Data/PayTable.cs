using UnityEngine;
using System.Collections.Generic;

// PayTable class instance - the global data from "pay_tables".  Contains list of spin results that will give the player a win.
public class PayTable : IResetGame
{
	public string keyName;
	public bool isAnyEvaluation; // true if any 7 or bar match defined on a symbol in scat paytable
	
	public struct ScatterWin
	{
		public int id;
		public int credits;
		public string bonusGame;
		public int freeSpins;
		public string[] symbols;
		public BonusGameChoices bonusGameChoices;
	};
	public Dictionary<int,ScatterWin> scatterWins = new Dictionary<int,ScatterWin>();
	
	public struct LineWin
	{
		public int id;
		public int credits;
		public int symbolMatchCount;
		public string symbol;
	};
	public Dictionary<int,LineWin> lineWins = new Dictionary<int,LineWin>();

	public struct BonusGameChoices
	{
		public List<BonusGameChoice> gameChoices;
		public string keyName;
	};

	public struct BonusGameChoice
	{
		public string keyName;
		public string gameName;
		public string gameDescription;
		public string extraInfo;
	}
	
	private static Dictionary<string, PayTable> _all = new Dictionary<string,PayTable>();
	
	public PayTable (string key, JSON data)
	{
		keyName = key;
		
		JSON[] jsonScatterWins = data.getJsonArray("scatter_wins");
		foreach (JSON jsonScatter in jsonScatterWins)
		{
			ScatterWin scatter = new ScatterWin();
			scatter.id = jsonScatter.getInt("id", 0);
			scatter.credits = jsonScatter.getInt("credits", 0);
			scatter.bonusGame = jsonScatter.getString("bonus_game", "");
			scatter.freeSpins = jsonScatter.getInt("free_spins", 0);
			scatter.symbols = jsonScatter.getStringArray("symbols");

			// Bonus game choices are now available as part of a scatter win.
			JSON[] bonusGameChoicesJSON = jsonScatter.getJsonArray("bonus_game_choice_games");
			if (bonusGameChoicesJSON != null && bonusGameChoicesJSON.Length > 0)
			{
				scatter.bonusGameChoices = new BonusGameChoices();
				scatter.bonusGameChoices.gameChoices = new List<BonusGameChoice>();
				scatter.bonusGameChoices.keyName = bonusGameChoicesJSON[0].getString("key_name", "");
				List<List<string>> bonusGameList = bonusGameChoicesJSON[0].getStringListList("game_choices");
				if (bonusGameList != null && bonusGameList.Count > 0)
				{
					foreach (List<string> bonusGameData in bonusGameList)
					{
						BonusGameChoice bonusGameChoice = new BonusGameChoice();
						bonusGameChoice.keyName = bonusGameData[0];
						bonusGameChoice.gameName = bonusGameData[1];
						bonusGameChoice.gameDescription = bonusGameData[2];
						bonusGameChoice.extraInfo = bonusGameData[3];
						scatter.bonusGameChoices.gameChoices.Add(bonusGameChoice);
					}
				}
				else
				{
					Debug.LogWarning("Bonus choices were found without appropriate choices.");
				}
			}
			
			scatterWins[scatter.id] = scatter;
		}
		
		JSON[] jsonLineWins = data.getJsonArray("line_wins");
		foreach (JSON jsonLine in jsonLineWins)
		{
			LineWin line = new LineWin();
			line.id = jsonLine.getInt("id", 0);
			line.credits = jsonLine.getInt("credits", 0);
			line.symbolMatchCount = jsonLine.getInt("symbol_match_count", 0);
			line.symbol = jsonLine.getString("symbol", "");

			bool isAnyEvaluationFlag = jsonLine.getBool("is_any_evaluation", false);
			if (isAnyEvaluationFlag)
			{
				isAnyEvaluation = true;
			}

			lineWins[line.id] = line;
		}
	}
	
	public static void populateAll(JSON[] payTables)
	{
		// Debug.Log("payTables = " + payTables);
		// Debug.Log("payTables.Length = " + payTables.Length);
		// make sure the pay table array has somthing in it
		if (payTables.Length > 0)
		{
			foreach (JSON data in payTables)
			{
				if (data != null)
				{
					string key = data.getString("key_name", "");
					
					if (key == "")
					{
						Debug.LogError("Cannot process empty pay table key");
						continue;
					}
					else if (_all.ContainsKey(key))
					{
						// This could happen if multiple games use the same paytable,
						// and it was already populated by a previously loaded game.
						continue;
					}
					
					_all[key] = new PayTable(key, data);
				}
			}
		}
	}
	
	public static PayTable find(string keyName)
	{
		PayTable result = null;
		
		if (!_all.TryGetValue(keyName, out result))
		{
			Debug.LogError("Failed to find PayTable for key " + keyName);
		}
		
		return result;
	}
	
	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		_all = new Dictionary<string,PayTable>();
	}
}
