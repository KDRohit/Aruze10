using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

#if ZYNGA_TRAMP
public class AutomatedGame
{
	// Common info for all games with this key.
	public string gameKey;
	public string gameName;

	// Debug message
	private const string KEY_NOT_FOUND_DEBUG = "<color={0}[LADI] Cannot find game with key {1}, data in test plan is incorrect. Check {2} to verify</color";

	// A list of all iterations for this game.
	public List<AutomatedGameIteration> gameIterations;

	// Average stats across all iterations.
	public AutomatedGameStats averageStats 
	{
		get
		{
			// Update and return the average stats... since we don't want to be updating all the time.
			return updateAverageStats();
		}
	}

	public static readonly sortBySeverityHelper sortBySeverity = new sortBySeverityHelper();

	// Creates a new Automated Game with the specified game key, used for all iterations of this game.
	public AutomatedGame(string gameKey)
	{
		init(gameKey);
	}

	// Loads an automated game from JSON, including all of it's tested iterations.
	public AutomatedGame(JSON json)
	{
		init(json.getString(AutomationJSONKeys.GAME_KEY_KEY, AutomationJSONKeys.DEFAULT_KEY));

		JSON gameIterationJson = json.getJSON(AutomationJSONKeys.GAME_ITERATIONS_KEY);
		if (gameIterationJson != null)
		{
			foreach (string key in gameIterationJson.getKeyList())
			{
				gameIterations.Add(new AutomatedGameIteration(this, gameIterationJson.getJSON(key)));
			}
		}
	}

	public void init(string gameKey)
	{
		this.gameKey = gameKey;

		LobbyGame gameWithKey = LobbyGame.find(gameKey);

		if (gameWithKey != null)
		{
			this.gameName = gameWithKey.name;
		}
		else
		{
			Debug.LogErrorFormat(KEY_NOT_FOUND_DEBUG, AutomatedPlayerCompanion.LADI_DEBUG_COLOR, gameKey, TRAMPLogFiles.CURRENT_TEST_PLAN_FILE);
		}

		this.gameIterations = new List<AutomatedGameIteration>();
	}

	public bool hasBeenTested()
	{
		if (gameIterations != null)
		{
			if (gameIterations.Count > 0)
			{
				return true;
			}
		}

		return false;

	}

	public bool hasAverage()
	{
		if (hasBeenTested())
		{
			if (gameIterations.Count > 1)
			{
				return true;
			}
		}

		return false;
	}

	// Adds a new iteration of this game, given the iteration object.
	// Essentially, the "floating" iteration that was previously in the test queue will get passed here...
	// Which actually saves the iteration under this common game.
	public AutomatedGameIteration addNewIteration(AutomatedGameIteration newIteration)
	{
		newIteration.commonGame = this;
		newIteration.gameIterationNumber = gameIterations.Count;
		gameIterations.Add(newIteration);
		return newIteration;
	}

	// Adds a log to the currently active iteration.
	public AutomatedCompanionLog addLogToCurrentIteration(LogType type, string message, string stack)
	{
		return getLatestIteration().createNewLog(type, message, stack);
	}

	// Returns the latest iteration of this game.
	public AutomatedGameIteration getLatestIteration()
	{
		return gameIterations[gameIterations.Count - 1];
	}

	// Updates the average stats with all the correct information.
	public AutomatedGameStats updateAverageStats()
	{
		// First, get a list of all stats.
		List<AutomatedGameStats> gameStats = new List<AutomatedGameStats>();
		foreach(AutomatedGameIteration iteration in gameIterations)
		{
			if (iteration.stats != null)
			{
				gameStats.Add(iteration.stats);
			}
		}
			
		// Then, pass that list to stats class to do the averaging.
		AutomatedGameStats avgStats = AutomatedGameStats.getAverageStats(gameStats);
		avgStats.headerString = "AVERAGE STATS ACROSS ALL ITERATIONS";

		return avgStats;
	
	}

	// Returns a summary of this games test as a string.
	public string getTestResults(bool shouldAverageIterations)
	{

		// Create the builder and append a header.
		StringBuilder summary = new StringBuilder();
		summary.AppendFormat("-- {0} ({1}) --", gameName, gameKey);
		summary.AppendLine();

		// Whether or not all iterations should be averaged.
		// If false, then each iteration will print it's own summary.
		if (shouldAverageIterations)
		{
			summary.AppendLine("- Average Stats -");
			summary.AppendLine(averageStats.ToString());
		}
		else
		{
			// Iterate through each iteration and print out it's summary.
			foreach (AutomatedGameIteration itr in gameIterations)
			{

				summary.AppendLine();
				summary.AppendFormat("- Iteration {0} -", itr.gameIterationNumber);
				summary.AppendLine();
				summary.Append(itr.stats.ToString());

			}
		}

		return summary.ToString();
	}

	// Gets the average stats and assigns a severity color to the game based on that.
	public Color getGameSeverityColor()
	{
		AutomatedGameStats avgStats = averageStats;
		return avgStats.getColorBySeverity();
	}
		
	// Returns a JSON string of the important info in this game.
	public string ToJSON()
	{
		StringBuilder build = new StringBuilder();

		// Saves the game key and name.
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.GAME_KEY_KEY, gameKey));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.GAME_NAME_KEY, gameName));

		// Append the game iterations to JSON data.
		build.AppendFormat("\"{0}\":{{", AutomationJSONKeys.GAME_ITERATIONS_KEY);
		for (int i = 0; i < gameIterations.Count; i++)
		{
			build.AppendFormat("\"{0}\":{{", i);

			build.Append(gameIterations[i].ToJSON());

			build.Append("}");

			if (i < gameIterations.Count-1)
			{
				build.Append(",");
			}
		}
		build.Append("}");

		return build.ToString();
	}

	// Helper used to sort games by severity, using average stats.
	public class sortBySeverityHelper : Comparer<AutomatedGame>
	{
		public override int Compare(AutomatedGame a, AutomatedGame b)
		{
			if (a.averageStats.numberOfExceptions > b.averageStats.numberOfExceptions)
			{
				return -1;
			}
			else if (a.averageStats.numberOfExceptions < b.averageStats.numberOfExceptions)
			{
				return 1;
			}
			else if (a.averageStats.numberOfErrors > b.averageStats.numberOfErrors)
			{
				return -1;
			}
			else if (a.averageStats.numberOfErrors < b.averageStats.numberOfErrors)
			{
				return 1;
			}
			else if (a.averageStats.numberOfWarnings > b.averageStats.numberOfWarnings)
			{
				return -1;
			}
			else if (a.averageStats.numberOfWarnings < b.averageStats.numberOfWarnings)
			{
				return 1;
			}
			else
			{
				return 0;
			}
		}
	}
}
#endif