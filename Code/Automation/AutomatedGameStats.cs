using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

#if ZYNGA_TRAMP
[System.Serializable]
public class AutomatedGameStats 
{
	// Constants
	public const double RPT_MAX_LIMIT = 1.0D; // A game's return to player (rtp) shouldn't exceed this %
	public const int RPT_MAX_SPIN_COUNT_THRESHOLD = 400; // A game's return to player (rtp) isn't really useful until at least this many spin
	public const double MEMORY_MAX_LIMIT = 192000000.0D; // A games memory foot print should not exceed this, in bytes
	public const double MIN_TIME_FOR_SUCCESS = 5.0D;

	public string headerString = "GAME STATS";

	// Whether or not these stats should use active values or assume logging has ended.
	public bool statsActive = true;

	// Tracks specific number of logs.
	public int numberOfExceptions;
	public int numberOfErrors;
	public int numberOfWarnings;

	// Adds all logs to get total number of logs.
	public int totalNumberOfLogs
	{
		get 
		{
			return (numberOfWarnings + numberOfErrors + numberOfExceptions);	
		}
	}

	// Stats that we want to track for a specific game.
	public System.DateTime timeStarted = System.DateTime.MinValue;

	public System.DateTime timeEnded;

	public int spinsDone;
	public int forcedSpinsDone;
	public int normalSpinsDone;
	public int fastSpinsDone;

	public int autoSpinsRequested;
	public int autoSpinsReceived;
	public int autoSpinsFinished;

	public double maxMemory;
	public double runningSumForMeanMemory;
	public int memorySampleCount;

	public System.DateTime spinTimerStart;
	public double maxSpinTime;
	public double runningSumForMeanSpinTime;
	public int spinTimeSampleCount;

	public long startingPlayerCredits;
	public long endingPlayerCredits;

	// Store credits without applying the economy multiplier
	// Apply the multiplier when we display the value.
	public long totalAmountBet;

	public Dictionary<string, int> bonusGamesEntered;

	// Coins returned to the player.
	public long netCredits
	{
		get
		{
			if (!statsActive)
			{
				return endingPlayerCredits - startingPlayerCredits;
			}
			return SlotsPlayer.creditAmount - startingPlayerCredits;
		}
	}

	// Amount rewarded to the player.
	public long coinsReturned;		// This is only tracked when server debug is on.

	// Return to player - calculated whenever needed.
	public double rtp
	{
		get
		{
			return ((double) coinsReturned) / totalAmountBet;
		}
	}

	// Constants that determine how many of each error type we consider "severe".
	// I.e. 50 warnings is severe, 15 errors is severe, 5 exceptions is severe.
	public const int WARNING_MAX = 50;
	public const int ERROR_MAX = 15;
	public const int EXCEPTION_MAX = 5;

	// Colors used to lerp between 0 and the severe level for each log type.
	// Dividing by 255.0 because I forgot it's supposed to be 0 and 1 and this fix was easier.
	public static readonly Color MIN_YELLOW = new Color(244.0f/255.0f, 232.0f/255.0f, 157.0f/255.0f);
	public static readonly Color MAX_YELLOW = new Color(128.0f/255.0f, 122.0f/255.0f, 31.0f/255.0f);

	public static readonly Color MIN_RED = new Color(249.0f/255.0f, 177.0f/255.0f, 177.0f/255.0f);
	public static readonly Color MAX_RED = new Color(154.0f/255.0f, 15.0f/255.0f, 15.0f/255.0f);

	public static readonly Color MIN_PINK = new Color(245.0f/255.0f, 135.0f/255.0f, 234.0f/255.0f);
	public static readonly Color MAX_PINK = new Color(137.0f/255.0f, 18.0f/255.0f, 125.0f/255.0f);

	public static readonly Color HAPPY_COLOR = new Color(68.0f/255.0f, 223.0f/255.0f, 104.0f/255.0f);

	// Creates the stats and starts logging them.
	public AutomatedGameStats()
	{
		timeStarted = System.DateTime.Now;
		if (SlotsPlayer.instance != null && SlotsPlayer.instance.credits != null)
		{
			startingPlayerCredits = SlotsPlayer.creditAmount;
		}

		bonusGamesEntered = new Dictionary<string, int>();
	}

	// Loads stats from JSON. There's a lot to load. Data is cool. I like data.
	public AutomatedGameStats(JSON json)
	{
		this.statsActive = false;

		this.numberOfExceptions = json.getInt(AutomationJSONKeys.EXCEPTION_KEY, 0);
		this.numberOfErrors = json.getInt(AutomationJSONKeys.ERROR_KEY, 0);
		this.numberOfWarnings = json.getInt(AutomationJSONKeys.WARNINGS_KEY, 0);
		this.timeStarted = System.DateTime.Parse(json.getString(AutomationJSONKeys.TIME_STARTED_KEY, System.DateTime.MinValue.ToString()));
		this.timeEnded = System.DateTime.Parse(json.getString(AutomationJSONKeys.TIME_ENDED_KEY, System.DateTime.MinValue.ToString()));
		this.spinsDone = json.getInt(AutomationJSONKeys.SPINS_DONE_KEY, 0);
		this.forcedSpinsDone = json.getInt(AutomationJSONKeys.FORCED_SPINS_DONE_KEY, 0);
		this.normalSpinsDone = json.getInt(AutomationJSONKeys.NORMAL_SPINS_DONE_KEY, 0);
		this.fastSpinsDone = json.getInt(AutomationJSONKeys.FAST_SPINS_DONE_KEY, 0);
		this.autoSpinsRequested = json.getInt(AutomationJSONKeys.AUTOSPINS_REQUESTED_KEY, 0);
		this.autoSpinsReceived = json.getInt(AutomationJSONKeys.AUTOSPINS_RECEIVED_KEY, 0);
		this.autoSpinsFinished = json.getInt(AutomationJSONKeys.AUTOSPINS_FINISHED_KEY, 0);
		this.maxMemory = json.getDouble(AutomationJSONKeys.MAX_MEMORY_KEY, 0);
		this.runningSumForMeanMemory = json.getDouble(AutomationJSONKeys.MEAN_MEMORY_KEY, 0);
		this.memorySampleCount = json.getInt(AutomationJSONKeys.MEMORY_SAMPLE_KEY, 0);
		this.spinTimerStart = System.DateTime.Parse(json.getString(AutomationJSONKeys.SPIN_TIMER_START_KEY, System.DateTime.MinValue.ToString()));
		this.maxSpinTime = json.getDouble(AutomationJSONKeys.MAX_SPIN_TIME_KEY, 0);
		this.runningSumForMeanSpinTime = json.getDouble(AutomationJSONKeys.MEAN_SPIN_TIME_KEY, 0);
		this.spinTimeSampleCount = json.getInt(AutomationJSONKeys.SPIN_TIME_SAMPLE_COUNT_KEY, 0);
		this.startingPlayerCredits = json.getLong(AutomationJSONKeys.STARTING_PLAYER_CREDS_KEY, 0);
		this.endingPlayerCredits = json.getLong(AutomationJSONKeys.ENDING_PLAYER_CREDS_KEY, 0);
		this.totalAmountBet = json.getLong(AutomationJSONKeys.TOTAL_BET_KEY, 0);
		this.coinsReturned = json.getLong(AutomationJSONKeys.TOTAL_REWARD_KEY, 0);
		this.bonusGamesEntered = json.getStringIntDict(AutomationJSONKeys.BONUS_GAMES_ENTERED_KEY);
	}

	// Given a list of game stats, returns a single AutomatedGameStats with the average of all those stats.
	public static AutomatedGameStats getAverageStats(List<AutomatedGameStats> stats)
	{
		if (stats.Count > 0)
		{
			// Creates the new class to store average stats in.
			AutomatedGameStats avgStats = new AutomatedGameStats();

			// Sets the stats as inactive so that they don't use active values.
			avgStats.statsActive = false;

			// These values will have to be averaged slightly differently.
			double avgTime = 0;
			long avgCoinsReturned = 0;

			// Iterates through each stats class and adds the values to the average stats.
			foreach (AutomatedGameStats stat in stats)
			{
				avgStats.numberOfExceptions += stat.numberOfExceptions;
				avgStats.numberOfErrors += stat.numberOfErrors;
				avgStats.numberOfWarnings += stat.numberOfWarnings;
		
				avgStats.spinsDone += stat.spinsDone;
				avgStats.forcedSpinsDone += stat.forcedSpinsDone;
				avgStats.normalSpinsDone += stat.normalSpinsDone;
				avgStats.fastSpinsDone += stat.fastSpinsDone;

				avgStats.autoSpinsRequested += stat.autoSpinsRequested;
				avgStats.autoSpinsReceived += stat.autoSpinsReceived;
				avgStats.autoSpinsFinished += stat.autoSpinsFinished;
		
				avgStats.maxMemory += stat.maxMemory;
				avgStats.runningSumForMeanMemory += stat.runningSumForMeanMemory;
				avgStats.memorySampleCount += stat.memorySampleCount;

				avgStats.maxSpinTime += stat.maxSpinTime;
				avgStats.runningSumForMeanSpinTime += stat.runningSumForMeanSpinTime;

				avgStats.totalAmountBet += stat.totalAmountBet;
				avgStats.coinsReturned += stat.coinsReturned;

				avgTime += stat.getOverallSeconds();
				avgCoinsReturned += stat.coinsReturned;

			}


			// Average all the sums by dividing by total stats.
			avgStats.numberOfExceptions = Mathf.CeilToInt((float)avgStats.numberOfExceptions / (float)stats.Count);
			avgStats.numberOfErrors = Mathf.CeilToInt((float)avgStats.numberOfErrors / (float)stats.Count);
			avgStats.numberOfWarnings = Mathf.CeilToInt((float)avgStats.numberOfWarnings / (float)stats.Count);

			avgStats.spinsDone /= stats.Count;
			avgStats.forcedSpinsDone /= stats.Count;
			avgStats.normalSpinsDone /= stats.Count;
			avgStats.fastSpinsDone /= stats.Count;

			avgStats.autoSpinsRequested /= stats.Count;
			avgStats.autoSpinsReceived /= stats.Count;
			avgStats.autoSpinsFinished /= stats.Count;

			avgStats.maxMemory /= stats.Count;
			avgStats.runningSumForMeanMemory /= stats.Count;
			avgStats.memorySampleCount /= stats.Count;

			avgStats.maxSpinTime /= stats.Count;
			avgStats.runningSumForMeanSpinTime /= stats.Count;

			avgStats.totalAmountBet /= stats.Count;
			avgStats.coinsReturned /= stats.Count;

			avgTime /= stats.Count;
			avgCoinsReturned /= stats.Count;

			// Sets start and end values so avgStats can correctly calculate average overall time.
			// Why do we do it this way, you ask? I'd be happy to tell you:
			// "Overall Time" and "Coins Returned" are just values calculated based on start and end values.
			// For example, overall time is just startTime - endTime, and coins Returned is just ending credits - starting credits.
			// However, it's the OVERALL values we want to average...
			// So first, we get the overall values and average them like normal from all the stats classes.
			// But then, to ensure that the AVERAGE stats class can correctly calculate those values, we need to set the starts and ends.
			// We already have the average, so if we just set the "Start" values to 0, and the "end" values to the average...
			// Then the average stats will always return the correct overall values! Afterall, (Average Overall - 0) = Average Overall!
			avgStats.timeStarted = System.DateTime.MinValue;   // 0
			avgStats.timeEnded = System.DateTime.MinValue.AddSeconds((double)Mathf.Max(0.0f, (float)avgTime));

			// Sets this value so that avgStats can correctly calculate average RTP.
			avgStats.startingPlayerCredits = 0;
			avgStats.endingPlayerCredits = avgCoinsReturned;

			return avgStats;
		}
		else
		{
			Debug.LogErrorFormat("Attempting to get average stats for an empty stats list!");
		}

		return null;
	}
		
	// Returns a JSON string of all stats.
	public string ToJSON()
	{
		StringBuilder build = new StringBuilder();

		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.LOG_KEY, totalNumberOfLogs));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.EXCEPTION_KEY, numberOfExceptions));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.ERROR_KEY, numberOfErrors));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.WARNINGS_KEY, numberOfWarnings));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.TIME_STARTED_KEY, timeStarted.ToString()));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.TIME_ENDED_KEY, timeEnded.ToString()));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.SPINS_DONE_KEY, spinsDone));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.FORCED_SPINS_DONE_KEY, forcedSpinsDone));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.NORMAL_SPINS_DONE_KEY, normalSpinsDone));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.FAST_SPINS_DONE_KEY, fastSpinsDone));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.AUTOSPINS_REQUESTED_KEY, autoSpinsRequested));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.AUTOSPINS_RECEIVED_KEY, autoSpinsReceived));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.AUTOSPINS_FINISHED_KEY, autoSpinsFinished));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.MAX_MEMORY_KEY, maxMemory));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.MEAN_MEMORY_KEY, runningSumForMeanMemory));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.MEMORY_SAMPLE_KEY, memorySampleCount));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.SPIN_TIMER_START_KEY, spinTimerStart.ToString()));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.MAX_SPIN_TIME_KEY, maxSpinTime));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.MEAN_SPIN_TIME_KEY, runningSumForMeanSpinTime));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.SPIN_TIME_SAMPLE_COUNT_KEY, spinTimeSampleCount));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.STARTING_PLAYER_CREDS_KEY, startingPlayerCredits));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.ENDING_PLAYER_CREDS_KEY, endingPlayerCredits));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.TOTAL_BET_KEY, totalAmountBet));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.TOTAL_REWARD_KEY, coinsReturned));
		build.AppendFormat("{0}", JSON.createJsonString(AutomationJSONKeys.BONUS_GAMES_ENTERED_KEY, bonusGamesEntered));

		return build.ToString();
	}
		
	// Counts a bonus game in the stats.
	public void countBonusGame(string bonusGameName)
	{
		if (bonusGamesEntered.ContainsKey(bonusGameName))
		{
			bonusGamesEntered[bonusGameName]++;
		}
		else
		{
			bonusGamesEntered.Add(bonusGameName, 1);
		}
	}

	// Gets the total time that the stats (and game) were active.
	public System.TimeSpan getTotalTime()
	{
		// If the game ended, just return the time difference between start and end.
		if (!statsActive)
		{
			// Return however long the game ran.
			return timeEnded - timeStarted;

		}

		// If there was no end time set, just return the elapsed time from current time.
		return System.DateTime.Now - timeStarted;
	}
		
	// Gets the overall seconds that this game ran.
	public double getOverallSeconds()
	{
		return getTotalTime().TotalSeconds;
	}

	// Average overall spin times.
	public double getOverallMeanSpinTimes()
	{
		return getOverallSeconds() / spinsDone;
	}

	// Update spin time info.
	public void updateSpinTimeStats(double spinTimeSample)
	{		
		if (maxSpinTime < spinTimeSample)
		{
			maxSpinTime = spinTimeSample;
		}

		runningSumForMeanSpinTime += spinTimeSample;
		spinTimeSampleCount++;
	}

	// Gets the average for all spin times.
	public double getMeanSpinTime()
	{
		if (spinTimeSampleCount > 0)
		{
			return runningSumForMeanSpinTime / (double)spinTimeSampleCount;
		}
		else
		{
			return -1;
		}
	}

	// Updates the memory stats.
	public void updateMemoryStats(float memorySample)
	{
		if (maxMemory < memorySample)
		{
			maxMemory = memorySample;
		}

		runningSumForMeanMemory += memorySample;
		memorySampleCount++;
	}

	// Gets the average memory for this game.
	public double getMeanMemory()
	{
		if (memorySampleCount > 0)
		{
			return runningSumForMeanMemory / (double)memorySampleCount;
		}
		else
		{
			return -1;
		}
	}

	public double getMeanMemoryInMB()
	{
		return getMeanMemory() / (double)(1000000);
	}

	public double getMaxMemoryInMB()
	{
		return maxMemory / (double)(1000000);
	}

	public double getMemoryMaxLimitInMB()
	{
		return MEMORY_MAX_LIMIT / (double)(1000000);
	}

	// Checks if the memory used during this game is greater than a constant max.
	public bool isMemoryToHigh()
	{
		return maxMemory > MEMORY_MAX_LIMIT;
	}
		
	// Checks if the spin time is greater than a max constant.
	public bool isSpinTimeTooHigh()
	{
		if (maxSpinTime > 0)
		{
			return maxSpinTime > getSpinTimeMaxLimit();
		}
		else
		{
			return false;
		}
	}

	// Checks if the RTP is greater than an RTP threshhold.
	public bool isRTPTooHigh()
	{
		return spinsDone > RPT_MAX_SPIN_COUNT_THRESHOLD && rtp > RPT_MAX_LIMIT;
	}

	// Determines the max limit for spins.
	public double getSpinTimeMaxLimit()
	{
		float reelSetsCount = 1.0f;
		if (ReelGame.activeGame != null && ReelGame.activeGame is MultiSlotBaseGame)
		{
			reelSetsCount = 4.0f;
		}

		return (reelSetsCount * Mathf.Max(3.0f, 5.5f - 0.5f * (reelSetsCount)));
	}

	// Returns a color based on how many logs this game has, which should directly correlate with the severity of the game.
	// First, it checks exceptions, and returns a pink color if there are exceptions, shaded darker if there are more of them.
	// Then, it checks errors, and does the same.
	// Finally, it checks warnings.
	// If there are none of the above, it returns a happy green.
	public Color getColorBySeverity()
	{
		// If there are more than 0 exceptions OR the game exited earlier than expected.
		if (numberOfExceptions > 0)
		{
			Color exceptionColor = Color.Lerp(MIN_PINK, MAX_PINK, Mathf.Min((float)numberOfExceptions / (float)EXCEPTION_MAX, 1.0f));
			return exceptionColor;
		}
		else if (numberOfErrors > 0)
		{
			Color errorColor = Color.Lerp(MIN_RED, MAX_RED, Mathf.Min((float)numberOfErrors / (float)ERROR_MAX, 1.0f));
			return errorColor;
		}
		else if (numberOfWarnings > 0)
		{
			Color warningColor = Color.Lerp(MIN_YELLOW, MAX_YELLOW, Mathf.Min((float)numberOfWarnings/(float)WARNING_MAX, 1.0f));
			return warningColor;
		}

		return HAPPY_COLOR;
	}

	// Checks if these stats have any logs of the specified type.
	public bool hasLogsOfType(LogType type)
	{
		switch (type)
		{
		case LogType.Warning:
			if (numberOfWarnings > 0)
			{
				return true;
			}
			break;
		case LogType.Error:
			if (numberOfErrors > 0)
			{
				return true;
			}
			break;
		case LogType.Exception:
			if (numberOfExceptions > 0)
			{
				return true;
			}
			break;
		default:
			return false;
		}

		return false;
	}

	// Checks if there's any issues in this game.
	public bool hasNoIssues()
	{
		if (numberOfWarnings == 0 && numberOfErrors == 0 && numberOfExceptions == 0)
		{
			return true;
		}
		return false;
	}

	// Returns all stats as a formatted string, to easily view what happened during a game.
	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();

		string severityString = "GREEN";

		if (numberOfExceptions > 0)
		{
			severityString = "PINK";
		}

		else if (numberOfErrors > 0)
		{
			severityString = "RED";

		}
		else if (numberOfWarnings > 0)
		{
			severityString = "YELLOW";
		}

		stringBuilder.AppendFormat("Severity Level: {0}", severityString);
		stringBuilder.AppendLine();

		stringBuilder.AppendFormat("Spins: {0} ({1} forced, {2} normal, {3} fast)", spinsDone, forcedSpinsDone, normalSpinsDone, fastSpinsDone);
		stringBuilder.AppendLine();

		string rtpWarning = "";

		stringBuilder.AppendFormat("Overall RTP: {0:P2}{1}", rtp, rtpWarning);
		stringBuilder.AppendLine();
		stringBuilder.AppendFormat("({0} coins spent, {1} coins rewarded)", totalAmountBet, coinsReturned);
		stringBuilder.AppendLine();

		// Don't print these out for the average- they aren't relevant for average stats.
		if (timeStarted != System.DateTime.MinValue)
		{
			stringBuilder.AppendFormat("Date Started: {0}", timeStarted.ToLongDateString());
			stringBuilder.AppendLine();
			stringBuilder.AppendFormat("Start Time: {0}", timeStarted.ToLongTimeString());
			stringBuilder.AppendLine();
			stringBuilder.AppendFormat("End Time: {0}", timeEnded.ToLongTimeString());
			stringBuilder.AppendLine();
		}

		stringBuilder.AppendFormat("Overall time: {0:N2}s (average per spin {1:N2}s)", getOverallSeconds(), getOverallMeanSpinTimes());
		stringBuilder.AppendLine();

		if (maxSpinTime > 0)
		{
			string spinWarning = "";

			stringBuilder.AppendFormat("Non-win, non-anticipation spin time: average {0:N3}s (max {1:N3}s{2})",
				getMeanSpinTime(),
				maxSpinTime,
				spinWarning);
		}
		else
		{
			if (Time.timeScale != 1.0f)
			{
				stringBuilder.AppendFormat("Non-win, non-anticipation spin time: NOT TESTED (time scale {0:N0})", Time.timeScale);
			}
			else
			{
				stringBuilder.Append("Non-win, non-anticipation spin time: NOT TESTED");
			}
		}
		stringBuilder.AppendLine();

		if (maxMemory > 0
		    && getMeanMemory() > 0)
		{
			string memoryWarning = "";

			stringBuilder.AppendFormat("Memory usage: average {0:N1} MB (max {1:N1} MB{2})",
				getMeanMemoryInMB(),
				getMaxMemoryInMB(),
				memoryWarning);
		}
		else
		{
			stringBuilder.AppendFormat("Memory usage: NOT TESTED");
		}
		stringBuilder.AppendLine();

		stringBuilder.AppendFormat("Logged: {0} warnings, {1} errors, {2} exceptions", numberOfWarnings, numberOfErrors, numberOfExceptions);
		return stringBuilder.ToString();
	}
}
#endif
