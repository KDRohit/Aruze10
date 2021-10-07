using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

#if ZYNGA_TRAMP
// This class holds information for one specific test iteration of a game.
public class AutomatedGameIteration {

	// Max time for each specific game action.
	public const float MAX_TIME_ALLOWED_PER_ACTION = 180.0f;
	private const int MAX_AMOUNT_PER_ACTION = 9999;

	// Variables to check the state of the game test, in the event that it crashes.
	public bool isGameLoadedSuccessful = false;
	public bool isTesting = false;

	public bool forceEnded = false;

	// The maximum allowed run time for this game, can help move past stalled games at a certain point.
	public double maximumAllowedRunTime
	{
		get
		{
			// Actions already tested, added with remaining test actions, plus one for any active action.
			return (actionsTested.Count + 1 + remainingTestActions.Count) * MAX_TIME_ALLOWED_PER_ACTION;
		}
	}

	// Identifier and number for this game iteration.
	public int gameIterationNumber;

	// Reference to the common game that's not iteration independent
	public AutomatedGame commonGame;

	// Stats for this specific game test run.
	public AutomatedGameStats stats;

	// Condenses any repeated actions into a single key value pair of <actionName, actionCount>.
	// i.e. 5 spins in a row would get condensed to <"spin", 5>. Helpful for displaying the actions list.
	public List<KeyValuePair<string, int>> remainingTestActions;
	// Save all actions tested.
	public List<string> actionsTested;
	public string lastAction;
	
	private JSON prevSlotOutcome;
	private JSON mostRecentSlotOutcome;

	// Keeps track of any desync logs that need to be retroactively updated with the next outcome.
	private List<AutomatedCompanionLog> desyncLogsToBeUpdated;

	public List<AutomatedCompanionLog> gameLogs;			// List of logs that occurred during the game.
	public List<AutomatedCompanionVisualBug> visualBugs;	// List of visual bugs that are manually report during TRAMP run.

	// Time since the last action was popped. -1 if no action is active.
	public float timeSinceLastAction = -1.0f;

	// The total duration this test has been running. -1 if not started yet.
	public float totalGameRuntime = -1.0f;

	// Creates a new AutomatedGameIteration given a common game to identify with and a game iteration number.
	// Note: This will be null and 0 at first, because game iterations are only tied to common games when they start.

	// Keeps track of when the last action occurred. Used as a timer.
	private System.DateTime actionStart = System.DateTime.MinValue;

	public AutomatedGameIteration(AutomatedGame commonGame = null, int gameIteration = 0)
	{
		init(commonGame, gameIteration);
	}

	// Constructor to load an automated game iteration from JSON, given an automated game for it to connect with.
	public AutomatedGameIteration(AutomatedGame commonGame, JSON json)
	{
		init(commonGame, json.getInt(AutomationJSONKeys.ITERATION_NUM_KEY, 0));

		// Load the game stats from JSON. If the game hasn't been tested yet, there won't be any stats.
		JSON statsJSON = json.getJSON(AutomationJSONKeys.STATS_KEY);
		if (statsJSON != null)
		{
			this.stats = new AutomatedGameStats(json.getJSON(AutomationJSONKeys.STATS_KEY));
		}

		// Load the actions already tested for this game, will be empty if the game hasn't been tested yet.
		this.actionsTested = new List<string>(json.getStringArray(AutomationJSONKeys.ACTIONS_TESTED_KEY));

		// Load all remaining test actions that were not yet tested. This is most important for games not yet tested.
		this.remainingTestActions = new List<KeyValuePair<string, int>>();
		JSON actionsRemainingJson = json.getJSON(AutomationJSONKeys.ACTIONS_REMAINING_KEY);
		if (actionsRemainingJson != null)
		{
			// Iterate through each specific remaining action.
			foreach (string key in actionsRemainingJson.getKeyList())
			{
				// Load the specific action JSON.
				JSON action = actionsRemainingJson.getJSON(key);

				// Store the action name and how many times it should be tested.
				string actionName = action.getString(AutomationJSONKeys.ACTION_NAME_KEY, "spin");
				int actionCount = action.getInt(AutomationJSONKeys.ACTION_COUNT_KEY, 1);

				// Queue the loaded action.
				addTestAction(actionName, actionCount);
			}
		}

		// Loads the game logs from JSON.
		this.gameLogs = new List<AutomatedCompanionLog>();
		JSON gameLogsJson = json.getJSON(AutomationJSONKeys.GAME_LOGS_KEY);
		if (gameLogsJson != null)
		{
			foreach (string key in gameLogsJson.getKeyList())
			{
				addLog(new AutomatedCompanionLog(gameLogsJson.getJSON(key)));
			}
		}

		// Loads the visual bugs from JSON
		visualBugs = new List<AutomatedCompanionVisualBug>();
		JSON visualBugJson = json.getJSON(AutomationJSONKeys.VISUAL_BUGS_KEY);
		if (visualBugJson != null)
		{
			foreach (string key in visualBugJson.getKeyList())
			{
				visualBugs.Add(new AutomatedCompanionVisualBug(visualBugJson.getJSON(key)));
			}
		}
	}

	public void init(AutomatedGame commonGame, int gameIteration)
	{
		this.commonGame = commonGame;
		this.gameLogs = new List<AutomatedCompanionLog>();
		this.gameIterationNumber = gameIteration;
		this.visualBugs = new List<AutomatedCompanionVisualBug>();
		this.remainingTestActions = new List<KeyValuePair<string, int>>();
		this.actionsTested = new List<string>();

		isGameLoadedSuccessful = false;
		isTesting = false;
	}

	// Checks if the last popped action is taking too long.
 	public bool isActionTakingTooLong()
	{
		if (timeSinceLastAction > MAX_TIME_ALLOWED_PER_ACTION)
		{
			return true;
		}

		return false;

	}
	
	// Checks if the total game test is taking too long.
	public bool isGameTestTakingTooLong()
	{

		if (totalGameRuntime > maximumAllowedRunTime)
		{
			return true;
		}

		return false;
	}

	// Returns a JSON string of all important data for this game iteration.
	public string ToJSON()
	{
		StringBuilder build = new StringBuilder();

		// Append the game iteration number to JSON data.
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.ITERATION_NUM_KEY, gameIterationNumber));

		// Append the stats to JSON data.
		build.AppendFormat("\"{0}\":{{", AutomationJSONKeys.STATS_KEY);
		if (stats != null)
		{
			build.Append(stats.ToJSON());
		}
		build.Append("},");

		// Append the actions tested to JSON data.
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.ACTIONS_TESTED_KEY, actionsTested));
		build.AppendFormat("\"{0}\":{{", AutomationJSONKeys.ACTIONS_REMAINING_KEY);

		for (int i = 0; i < remainingTestActions.Count; i++)
		{
			// First, use the action index as a key for this action.
			build.AppendFormat("\"{0}\":{{", i);

			// Save the name of this current action.
			build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.ACTION_NAME_KEY, remainingTestActions[i].Key));

			// Append the action counter to this current JSON block.
			build.AppendFormat("{0}", JSON.createJsonString(AutomationJSONKeys.ACTION_COUNT_KEY, remainingTestActions[i].Value));

			build.Append("}");

			if (i < remainingTestActions.Count -1)
			{
				build.Append(",");
			}
		}
		build.Append("},");

		// Append the game logs to JSON data.
		build.AppendFormat("\"{0}\":{{", AutomationJSONKeys.GAME_LOGS_KEY);
		for (int i = 0; i < gameLogs.Count; i++)
		{
			build.AppendFormat("\"{0}\":{{", i);
			build.Append(gameLogs[i].ToJSON());
			build.Append("}");
			if (i < gameLogs.Count - 1)
			{
				build.Append(",");
			}
		}
		build.Append("},");


		// Append the visual bugs to JSON data.
		build.AppendFormat("\"{0}\":{{", AutomationJSONKeys.VISUAL_BUGS_KEY);
		for (int i = 0; i < visualBugs.Count; i++)
		{
			build.Append(visualBugs[i].toJSON());
			if (i < visualBugs.Count-1)
			{
				build.Append(",");
			}
		}
		build.Append("}");


		return build.ToString();

	}

	// Log a game without setting up test actions.
	public void continueLogging()
	{
		if (this.stats == null)
		{
			this.stats = new AutomatedGameStats();
		}

		// Refresh any log builders that are used for printing to files.
		clearLogBuilders();
		TRAMPSplunk.startNewGameTest();

		// If the active game doesn't match with this game, something is definitely wrong.
		if (GameState.game == null || GameState.game.keyName != commonGame.gameKey)
		{
			Debug.LogErrorFormat("<color={0}>TRAMP> Spinning in the wrong game, expecting {1} but got {2}</color>", 
				AutomatedPlayer.TRAMP_DEBUG_COLOR, commonGame.gameKey, (GameState.game == null ? "null" : GameState.game.keyName));
		}

		TRAMPSplunk.gameTestStartEventData.GameKey = commonGame.gameKey;
		TRAMPSplunk.gameTestStartEventData.GameName = commonGame.gameName;
		TRAMPSplunk.gameTestStartEventData.StartTime = stats.timeStarted;
		TRAMPSplunk.gameTestStartEventData.StartingCredits = stats.startingPlayerCredits;
		TRAMPLogFiles.logToOther("TRAMP> --- Starting {0} - {1} at {2} with {3} credits",
			commonGame.gameKey, commonGame.gameName, stats.timeStarted, stats.startingPlayerCredits);

		printed = false;
		AutomatedPlayer.isOver9000Vertices = false;

		// The game is now loaded and testing.
		isGameLoadedSuccessful = true;
		isTesting = true;

		// Save the test plan, to be safe.
		TRAMPLogFiles.saveCurrentTestPlan();

		if (AutomatedCompanionVisualizer.instance != null)
		{
			AutomatedCompanionVisualizer.instance.gameLoad();
		}

		Debug.LogFormat("<color={0}>TRAMP> Continuing Automation of {1} at {2}</color>",
			AutomatedPlayer.TRAMP_DEBUG_COLOR,
			commonGame.gameKey,
			stats.timeStarted);
	}

	// Method to signify that a game should start logging and prep test actions.
	// This is different from the constructor, since iterations can be created but not started.
	public void start()
	{
		// Create a new stats, to start logging stats.
		this.stats = new AutomatedGameStats();

		// Refresh any log builders that are used for printing to files.
		clearLogBuilders();
		TRAMPSplunk.startNewGameTest();

		// If the active game doesn't match with this game, something is definitely wrong.
		if (GameState.game == null || GameState.game.keyName != commonGame.gameKey)
		{
			Debug.LogErrorFormat("<color={0}>TRAMP> Spinning in the wrong game, expecting {1} but got {2}</color>", 
				AutomatedPlayer.TRAMP_DEBUG_COLOR, commonGame.gameKey, (GameState.game == null ? "null" : GameState.game.keyName));
		}

		TRAMPSplunk.gameTestStartEventData.GameKey = commonGame.gameKey;
		TRAMPSplunk.gameTestStartEventData.GameName = commonGame.gameName;
		TRAMPSplunk.gameTestStartEventData.StartTime = stats.timeStarted;
		TRAMPSplunk.gameTestStartEventData.StartingCredits = stats.startingPlayerCredits;
		TRAMPLogFiles.logToOther("TRAMP> --- Starting {0} - {1} at {2} with {3} credits",
			commonGame.gameKey, commonGame.gameName, stats.timeStarted, stats.startingPlayerCredits);

		printed = false;
		AutomatedPlayer.isOver9000Vertices = false;

		// If there's an auto flag in the remaining actions, we need to convert it to actual actions.
		if (containsAction(AutomatedPlayer.AUTOMATIC_TEST_FLAG))
		{	
			if (AutomatedPlayerCompanion.instance.shouldTestMiniGames && needsToCheckForActiveMiniGame())
			{
				addTestAction(AutomatedTestAction.FEATURE_MINI_GAME_ACTION, 9999);
			}
			// Iterate through each action, and check for the auto flag.
			for (int i = 0; i < remainingTestActions.Count; i++)
			{
				while (getActionAt(i).Key == AutomatedPlayer.AUTOMATIC_TEST_FLAG)
				{
					// First, remove the auto flag since it itself is not an action.
					decrementActionAt(i);

					// Add default auto spins at the same location the flag was found.
					addTestActionAt(i, AutomatedTestAction.SPIN_ACTION, AutomatedPlayer.instance.numberOfSpins);

					if (AutomatedPlayer.shouldTestGiftedBonusSpins && SlotsPlayer.isFacebookUser)
					{
						addTestAction(AutomatedTestAction.FORCE_GIFTED_GAME_ACTION, 2);
					}

					// Caluated ratio of normal spins to forced spin
					int numberOfForcedSpins = Mathf.RoundToInt(Mathf.Min(AutomatedPlayer.instance.numberOfSpins / 10.0f, 1));

					// - Add forced spin actions
					if (SlotBaseGame.instance != null)
					{
						ForcedOutcomeRegistrationModule[] forcedList = SlotBaseGame.instance.GetComponents<ForcedOutcomeRegistrationModule>();
						
						for (int index = 0; index < forcedList.Length; index++)
						{
							ForcedOutcomeRegistrationModule forced = forcedList[index];

							// Note: Legacy games only had one ForcedOutcomeRegistrationModule and left the targetGameKey blank
							if (string.IsNullOrEmpty(forced.targetGameKey)
								|| forced.targetGameKey == commonGame.gameKey)
							{
								string forcedKeyCode;
								foreach (SlotBaseGame.SerializedForcedOutcomeData data in forced.forcedOutcomeList)
								{
									if (!data.isIgnoredByTramp && data.forcedOutcome.fakeServerMessage == null)
									{
										forcedKeyCode = data.getKeyCodeForForcedOutcomeType(isForTramp: true);

										if (forcedKeyCode == AutomatedTestAction.SPIN_ACTION)
										{
											// if we get a "spin" for a TRAMP forced outcome it means that the forced outcome couldn't be mapped, so log info about it
											Debug.LogFormat("<color={0}>TRAMP> {1} Encountered unknown forced outcome \"{2}\" during test setup, adding \"spin\" instead.</color>", 
												AutomatedPlayer.TRAMP_DEBUG_COLOR,
												commonGame.gameKey,
												data.forcedOutcomeType);
										}
										// We need to multiply the number of forced spins, by spinTestRunCount in order to determine
										// the correct number to force here in order to ensure we test each forced feature/bonus the required number of times
										int numberOfSpinsForThisOutcome = numberOfForcedSpins * data.spinTestRunCount;

										// Add the auto number of forced spins for each key code found. 
										addTestAction(forcedKeyCode, numberOfSpinsForThisOutcome);
									}
									// TODO: Make this check work.
									//else if (SlotBaseGame.instance.isUsingFakeServerMessage() && SlotBaseGame.instance.isUsingForcedOutcomes())
									//{

										// TRAMP will skip any game with a fake server message, but if there are also forced outcomes, something may be wrong/need to be checked.
									//	Debug.LogErrorFormat("<color={0}>TRAMP> {1} There is a fake server message AND forced outcomes in this game. You may want to look into this.",
									//		AutomatedPlayer.TRAMP_DEBUG_COLOR,
									//		commonGame.gameKey);

									//}
								}
							}
						}
					}
					else
					{
						Debug.LogErrorFormat("<color={0}>TRAMP> {1} No BaseGame Defined for {1}!",
											AutomatedPlayer.TRAMP_DEBUG_COLOR,
											commonGame.gameKey);
					}
						
					if (AutomatedPlayer.shouldTestAutospins)
					{
						int countRemaining = AutomatedPlayer.numberOfAutospins;
						while (countRemaining >= 10)
						{
							if (countRemaining >= 100)
							{
								addTestAction(AutomatedTestAction.AUTOSPIN_100, 1);
								countRemaining -= 100;
							}
							else if (countRemaining >= 50)
							{
								addTestAction(AutomatedTestAction.AUTOSPIN_50, 1);
								countRemaining -= 50;
							}
							else if (countRemaining >= 25)
							{
								addTestAction(AutomatedTestAction.AUTOSPIN_25, 1);
								countRemaining -= 25;
							}
							else
							{
								addTestAction(AutomatedTestAction.AUTOSPIN_10, 1);
								countRemaining -= 10;
							}
						}
					}

					if (AutomatedPlayer.shouldTestGiftedBonusSpins && SlotsPlayer.isFacebookUser)
					{
						addTestAction(AutomatedTestAction.PLAY_GIFTED_GAME_ACTION, 1);
					}

					// Add a desync at the end of the game.
					addTestAction(AutomatedTestAction.DESYNC_CHECK_ACTION);
				}
			}
		}
			
		// TODO: Fix this to run from existing test actions
		if (remainingTestActions.Count > 0)
		{
			// Nothing?
			TRAMPLogFiles.logToOther("TRAMP> {0} Does this happen anymore? gameTest.remainingTestActions.Count > 0 [{1} > 0]", 
				commonGame.gameKey, remainingTestActions.Count);
		}

		// The game is now loaded and testing.
		isGameLoadedSuccessful = true;
		isTesting = true;
		totalGameRuntime = 0.0f;

		// Save the test plan, to be safe.
		TRAMPLogFiles.saveCurrentTestPlan();

		if (AutomatedCompanionVisualizer.instance != null)
		{
			AutomatedCompanionVisualizer.instance.gameLoad();
		}

		Debug.LogFormat("<color={0}>TRAMP> Starting Automation of {1} at {2}</color>", 
			AutomatedPlayer.TRAMP_DEBUG_COLOR,
			commonGame.gameKey,
			stats.timeStarted);
	}

	// TRAMP is done testing this game and moving to a new one
	public void done(AutomatedPlayer.GameMode gameMode)
	{
		if (stats != null)
		{
			if (!((stats.autoSpinsRequested == stats.autoSpinsReceived) && 
				(stats.autoSpinsReceived == stats.autoSpinsFinished)))
			{
				string logMsg = string.Format("<color={0}>Autospin action(s) did not complete as expected! Requested: {1}, Received: {2}, Finished: {3}</color>",
					AutomatedPlayer.TRAMP_DEBUG_COLOR,
					stats.autoSpinsRequested,
					stats.autoSpinsReceived,
					stats.autoSpinsFinished);
				
				Debug.LogError(logMsg);

				createNewLog(LogType.Error, logMsg, string.Empty);
			}

			// Save the ending stats information, and mark the stats as done so that they use only logged info.
			stats.timeEnded = System.DateTime.Now;
			stats.endingPlayerCredits = SlotsPlayer.creditAmount;
			stats.statsActive = false;
		}

		// This game is no longer testing.
		isTesting = false;

		if (stats != null && stats.getOverallSeconds() < AutomatedGameStats.MIN_TIME_FOR_SUCCESS && !forceEnded)
		{
			string exceptionMessage = string.Format("Game terminated prematurely at {0} seconds, likely due to a crash.", stats.getOverallSeconds());
			createNewLog(LogType.Exception, exceptionMessage, "");
		}

		// If the game successfully loaded, end the spin.
		if (isGameLoadedSuccessful)
		{
			spinFinished(gameMode);
		}
		else
		{
			TRAMPLogFiles.saveCurrentTestPlan();
		}

		if (stats != null)
		{
			stats.timeEnded = System.DateTime.Now;
		}
		if (TRAMPSplunk.gameTestEndEventData != null)
		{
			TRAMPSplunk.gameTestEndEventData.EndTime = System.DateTime.Now;
		}
		
		TRAMPSplunk.gameTestEndEvent();
		TRAMPSplunk.gameTestSummaryEvent(stats);


		TRAMPLogFiles.logToOther("TRAMP> --- Finished {0} - {1} at {2} with {3} credits",
			commonGame.gameKey, commonGame.gameName, System.DateTime.Now, SlotsPlayer.creditAmount);

		// Print out any game information to file.
		print();

		TRAMPSplunk.ForceEventsToServer();
	}

	// Load specific test actions from JSON.
	public void loadActionsFromJSON(JSON actionsJSON)
	{
		// Load the remaining test actions from JSON
		if (actionsJSON != null)
		{
			foreach (string key in actionsJSON.getKeyList())
			{
				JSON action = actionsJSON.getJSON(key);
				string actionName = action.getString(AutomationJSONKeys.ACTION_NAME_KEY, "spin");
				int actionCount = action.getInt(AutomationJSONKeys.ACTION_COUNT_KEY, 0);
				remainingTestActions.Add(new KeyValuePair<string, int>(actionName, actionCount));
			}
		}
	}

	// Saves an action tested, so that it can be looked back on.
	public void saveAction(AutomatedTestAction action)
	{
		// Action is done, no currently active action.
		timeSinceLastAction = -1.0f;

		actionsTested.Add(action.actionName);
	}

	public void updateTestTime(float deltaTime)
	{	

		// Increases time since the last action. Shouldn't be affected by any timeScale changes.
		if (timeSinceLastAction >= 0.0f)
		{
			timeSinceLastAction += deltaTime;
		}

		// Increases total game runtime if the game has started.
		if (totalGameRuntime >= 0.0f)
		{
			totalGameRuntime += deltaTime;
		}
	}

	// Creates a brand new log to be added to this game iteration.
	public AutomatedCompanionLog createNewLog(LogType logType, string message, string stack)
	{
		// Cleans out any unecessary colors from the log message.
		string cleanedMessage = TRAMPLogFiles.cleanMessage(message);

		// Creates the new log
		AutomatedCompanionLog newLog = new AutomatedCompanionLog(logType, cleanedMessage, stack, stats.totalNumberOfLogs, System.DateTime.Now, lastAction, stats.spinsDone, mostRecentSlotOutcome, prevSlotOutcome);

		// Desyncs need to happen in a particular way. They're reported BEFORE the next outcome is sent down...
		// But we need the next outcome to determine where the desync happened.
		// In this case, we're going to wait for the next outcome, then update the desync log.
		if (cleanedMessage.Contains(PlayerResource.DESYNC_MESSAGE_PREFIX))
		{

			if (desyncLogsToBeUpdated == null)
			{
				desyncLogsToBeUpdated = new List<AutomatedCompanionLog>();
			}

			desyncLogsToBeUpdated.Add(newLog);
		}

		// Checks each log type, and logs it accordingly.
		switch (logType)
		{
			case LogType.Warning:
				if (AutomatedCompanionLog.isValidWarning(cleanedMessage))
				{
					addLog(newLog);
					stats.numberOfWarnings++;
					logWarningBuilder.AppendFormat("{0} {1}\n", newLog.timestamp, cleanedMessage);
				}
				break;

			case LogType.Error:
				if (AutomatedCompanionLog.isValidError(cleanedMessage))
				{
					addLog(newLog);
					stats.numberOfErrors++;
					logErrorBuilder.AppendFormat("{0} {1}\n", newLog.timestamp, cleanedMessage);
				}
				break;

			case LogType.Exception:
				addLog(newLog);
				stats.numberOfExceptions++;
				logExceptionBuilder.AppendFormat("{0} {1}\n", newLog.timestamp, cleanedMessage);
				print();
				break;
		}
				
		return newLog;
	}

	// Adds and returns a new log to the list of game logs. Also used when loading from JSON.
	public AutomatedCompanionLog addLog(AutomatedCompanionLog log)
	{
		gameLogs.Add(log);
		return log;
	}

	// Adds an action to test, as specified by the action name. Will be added "count" number of times.
	public void addTestAction(string action, int count = 1)
	{
		int actionAmount = remainingTestActions.Count;
		int lastIndex = actionAmount - 1;

		// If the action at the end of the queue is the same, combine the amounts.
		if (actionAmount > 0 && remainingTestActions[lastIndex].Key == action)
		{
			updateValueAt(lastIndex, count + remainingTestActions[lastIndex].Value);
		}
		else
		{
			remainingTestActions.Add(new KeyValuePair<string, int>(action, count));
		}
	}

	// Updates the value of a test action at a specified index.
	// If the index is out of bounds, it will do nothing and return.
	public void updateValueAt(int index, int count)
	{
		if (index < 0 || index >= remainingTestActions.Count)
		{
			return;
		}
		if (count <= 0)
		{
			removeActionAt(index);
		}
		else if (count > MAX_AMOUNT_PER_ACTION)
		{
			// If we exceed the maxiumum amount, just add a new action element to the list.
			remainingTestActions[index] = new KeyValuePair<string, int>(remainingTestActions[index].Key, MAX_AMOUNT_PER_ACTION);
			addTestActionAt(index + 1, remainingTestActions[index].Key, count - MAX_AMOUNT_PER_ACTION);
		}
		else
		{
			remainingTestActions[index] = new KeyValuePair<string, int>(remainingTestActions[index].Key, count);
		}
	}

	// Decrements an action in remainingTestActions.
	public void decrementActionAt(int index)
	{
		updateValueAt(index, remainingTestActions[index].Value - 1);
	}

	// Adds an action to remainingTestActions at a specific index, and adds "count" many times.
	public void addTestActionAt(int index, string action, int count = 1)
	{
		if (index > 0 && index < remainingTestActions.Count)
		{
			KeyValuePair<string, int> actionAtIndex = getActionAt(index);
			if (actionAtIndex.Key == action)
			{
				updateValueAt(index, count + actionAtIndex.Value);
			}
			else
			{
				remainingTestActions.Insert(index, new KeyValuePair<string, int>(action, count));
			}
		}
		else
		{
			remainingTestActions.Add(new KeyValuePair<string, int>(action, count));
		}
	}

	// Returns a specific action queued up for this iteration to remainingTestActions.
	public KeyValuePair<string, int> getActionAt(int index)
	{
		if (remainingTestActions.Count > index)
		{
			return remainingTestActions[index];
		}
		else
		{
			string exceptionString = string.Format("Tried to get test action at index {0} when there are only {1} actions left!", index, remainingTestActions.Count);
			throw new System.IndexOutOfRangeException(exceptionString);
		}
	}

	// Checks and returns the next action in the action queue.
	public string peekNextAction()
	{
		return getActionAt(0).Key;
	}

	// Returns and removes the next action in the action queue.
	public string popNextAction()
	{
		lastAction = getActionAt(0).Key;
		decrementActionAt(0);

		// Record when this action was popped.
		timeSinceLastAction = 0.0f;

		return lastAction;
	}

	// Removes an action at a specific index.
	public string removeActionAt(int index)
	{
		string action = getActionAt(index).Key;
		remainingTestActions.RemoveAt(index);
		return action;
	}
		
	// Checks if the action queue contains the specified action.
	public bool containsAction(string actionToFind)
	{
		foreach(KeyValuePair<string, int> kv in remainingTestActions)
		{
			if (kv.Key == actionToFind)
			{
				return true;
			}
		}

		return false;
	}

	// Counts a single spin from the automated game. 
	public void countSpin(long betAmount, bool forced)
	{
		appendFinalBuilder();
		stats.totalAmountBet += betAmount;
		stats.spinsDone++;

		if (forced)
		{
			stats.forcedSpinsDone++;
		}
		else if (Time.timeScale == 1.0f)
		{
			stats.normalSpinsDone++;
		}
		else
		{
			stats.fastSpinsDone++;
		}
	}

	public string getActionString()
	{
		StringBuilder build = new StringBuilder();

		bool isFirstLine = false;
		foreach(KeyValuePair<string, int> action in remainingTestActions)
		{
			if (!isFirstLine)
			{
				build.Append("\n");
				isFirstLine = true;
			}

			build.AppendFormat("{0} : {1}", action.Key, action.Value);
		}

		return build.ToString();
	}
		
		
	public List<AutomatedCompanionLog> getLogsByType(LogType logType)
	{
		List<AutomatedCompanionLog> logsToReturn = new List<AutomatedCompanionLog>();
		foreach (AutomatedCompanionLog log in gameLogs)
		{
			if (log.logType == logType)
			{
				logsToReturn.Add(log);
			}
		}

		return logsToReturn;
	}

	public bool hasLogsOfType(LogType type)
	{
		return stats.hasLogsOfType(type);
	}

	public int getReelSetCount()
	{
		int reelSetsCount = 1;
		if (ReelGame.activeGame != null && ReelGame.activeGame is MultiSlotBaseGame)
		{
			reelSetsCount = 4;
		}
			

		return reelSetsCount;
	}


	public string getTestSummary()
	{
		StringBuilder stringBuilder = new StringBuilder();

		stringBuilder.AppendLine();
		stringBuilder.AppendLine();
		stringBuilder.AppendFormat("-- {0} ({1}) --", commonGame.gameName, commonGame.gameKey);

		stringBuilder.AppendLine();
		stringBuilder.Append(stats.ToString());
		return stringBuilder.ToString();
	
	}

	public string jsonText()
	{
		if (remainingTestActions.Count > 0)
		{
			string jsonText = "\"" + commonGame.gameKey + "\":[";

			foreach (KeyValuePair<string, int> action in remainingTestActions)
			{
				jsonText += "{\"" + action.Key + "\":" + action.Value + "},";
			}
			jsonText += "],\n";
			return jsonText;
		}
		else
		{
			return null;
		}
	}
		
	public void spinFinished(AutomatedPlayer.GameMode gameMode)
	{
		// Collect spin data to TRAMPLog/Splunk
		TRAMPSplunk.spinEvent(this);

		// Determine if this is a boring spin,
		if (gameMode == AutomatedPlayer.GameMode.BASE_GAME &&
			Time.timeScale == 1 && // Normal Time Scale
			ReelGame.activeGame.outcome != null &&
			ReelGame.activeGame.outcome.getCredits() == 0 && // non-win
			ReelGame.activeGame.outcome.getAnticipationTriggers() == null && // non-anticipation
			!ReelGame.activeGame.engine.isSlamStopPressed) // That where not slammed
		{
			stats.updateSpinTimeStats(TRAMPSplunk.spinDataEventData.getTotalSpinTime());

			if (TRAMPSplunk.spinDataEventData.getTotalSpinTimeMinusNetworkLatency() > stats.getSpinTimeMaxLimit())
			{
				// Boring spin takes too long.
				Debug.LogErrorFormat("{0} non-win, non-anticipation, spin (minus network latency) took too long: {1:N3} seconds, the max is {2:N3}",
					GameState.game.keyName, TRAMPSplunk.spinDataEventData.getTotalSpinTimeMinusNetworkLatency(), stats.getSpinTimeMaxLimit());
			}
		}
	}

	// So many string builders.
	private StringBuilder postBuilder = new StringBuilder();
	private StringBuilder recievedBuilder = new StringBuilder();
	private StringBuilder lastPostAndReviedBuilder = new StringBuilder();
	private StringBuilder logBuilder = new StringBuilder();
	private StringBuilder logWarningBuilder = new StringBuilder();
	private StringBuilder logErrorBuilder = new StringBuilder();
	private StringBuilder logExceptionBuilder = new StringBuilder();
	private StringBuilder finallogWarningBuilder = new StringBuilder();
	private StringBuilder finallogErrorBuilder = new StringBuilder();
	private StringBuilder finallogExceptionBuilder = new StringBuilder();
	private StringBuilder finalResults = null;

	private bool printed = false;

	// Called when we send something to the server.
	public void post(string message)
	{
		postBuilder.AppendFormat("{0:HH:mm:ss.fff} {1}\n", System.DateTime.Now, message);
	}

	// Attempt to determine if a JSON message recieved in the recieved() function contains a slots_outcome event
	private bool isJsonSlotsOutcome(JSON passedJson)
	{
		// the server response can have more than one event, and the slots_outcome is an event
		JSON[] eventsJsonArray = passedJson.getJsonArray("events");

		for (int i = 0; i < eventsJsonArray.Length; i++)
		{
			JSON currentEventJson = eventsJsonArray[i];
			string typeString = currentEventJson.getString("type", "");
			
			if (typeString == "slots_outcome")
			{
				// found a slots_outcome
				return true;
			}
		}

		return false;
	}

	// Called when we get a response from the server.
	public void recieved(string message, bool isMiniGameOutcome = false)
	{
		// Update server message on received builder.
		recievedBuilder.AppendFormat("{0:HH:mm:ss.fff} {1}\n", System.DateTime.Now, message);
		if (AutomatedPlayerCompanion.instance != null && !string.IsNullOrEmpty(message))
		{
			JSON serverResponseJson = new JSON(message);
			
			if (isJsonSlotsOutcome(serverResponseJson) || isMiniGameOutcome)
			{
				
				// we only want to track the slot outcomes, not all the server traffic
				// all of the server traffic will be logged via the recievedBuilder into the TRAMP
				// log if you need to view it though
				prevSlotOutcome = mostRecentSlotOutcome;
				mostRecentSlotOutcome = serverResponseJson;

				// If there are any desync logs to update, update them.
				if (desyncLogsToBeUpdated != null && desyncLogsToBeUpdated.Count > 0)
				{

					// Iterate through each desync log and update its outcomes.
					foreach (AutomatedCompanionLog desyncLog in desyncLogsToBeUpdated)
					{
						desyncLog.prevOutcome = prevSlotOutcome;
						desyncLog.outcome = mostRecentSlotOutcome;
					}

					// Clear out the desync log list.
					desyncLogsToBeUpdated.Clear();
				}
			}

			long startingCredits = serverResponseJson.getLong("starting_credits", 0);
			long endingCredits = serverResponseJson.getLong("ending_credits", 0);
			AutomatedPlayerCompanion.instance.updateCurrentSpinValues((endingCredits - startingCredits +  ReelGame.activeGame.betAmount), (endingCredits - startingCredits));
		}
	}

	private void clearLogBuilders()
	{
		postBuilder.Length = 0;
		recievedBuilder.Length = 0;
		logBuilder.Length = 0;
		logWarningBuilder.Length = 0;
		logErrorBuilder.Length = 0;
		logExceptionBuilder.Length = 0;
		lastPostAndReviedBuilder.Length = 0;
	}

	public void print()
	{
		if (!printed)
		{
			printed = true;

			// Do final tests etc.
			// Memory get too high?

			/* This is temporarily commented out because it's incorrectly reporting games are using too much memory...
			 * Mainly because LADI is taking up a ton of memory as it continuously logs games.
			if (stats.isMemoryToHigh())
			{
				string logMessage = string.Format("<ERROR> Error: Memory reached {0:N1} MB which exceeds the maximum limit of {1:N1} MB", 
					stats.getMaxMemoryInMB(),
					stats.getMemoryMaxLimitInMB());
				TRAMPLogFiles.logToOther(logMessage);

				createNewLog(LogType.Error, logMessage, "");
			}
			*/

			// Spin times too long?
			if (stats.isSpinTimeTooHigh())
			{
				string logMessage = string.Format("<ERROR> Error: Spin times reached {0:N1}s which exceeds the maximum limit of {1:N1}s", 
					stats.maxSpinTime,
					stats.getSpinTimeMaxLimit());
			
				createNewLog(LogType.Error, logMessage, "");
			}

			// rtp to high?
			if (stats.isRTPTooHigh())
			{
				string logMessage = string.Format("<ERROR> Error: rtp {0:P} exceeds the maximum limit of {1:P}", 
					stats.rtp,
					AutomatedGameStats.RPT_MAX_LIMIT);

				TRAMPLogFiles.logToOther(logMessage);

				createNewLog(LogType.Error, logMessage, "");
			
			}

			appendFinalBuilder();
			finalResults = new StringBuilder();
			finalResults.AppendFormat("*******************  {0}  *******************", commonGame.gameKey);
			finalResults.AppendLine();

			finalResults.AppendFormat("After {0} spins in {1} our rtp was {2:P}", stats.spinsDone, commonGame.gameKey, stats.rtp);

			finalResults.AppendLine();
			finalResults.AppendFormat("\t Average time per spin was {0:N2} seconds.", stats.getOverallMeanSpinTimes());

			if (stats.maxSpinTime > 0)
			{
				finalResults.AppendLine();
				finalResults.AppendFormat("\t Average time per non-win, non-anticipation spin was {0:N3}s and the max was {1:N3}s using {2} samples.",
					stats.getMeanSpinTime(),
					stats.maxSpinTime,
					stats.spinTimeSampleCount);
			}

			if (stats.maxMemory > 0
			    && stats.getMeanMemory() > 0)
			{
				finalResults.AppendLine();
				finalResults.AppendFormat("\t Memory max was {0:N2} MB and the mean was {1:N2} MB using {2} samples.",
					stats.getMaxMemoryInMB(), 
					stats.getMeanMemoryInMB(), 
					stats.memorySampleCount);
			}

			if (stats.bonusGamesEntered != null
			    && stats.bonusGamesEntered.Count > 0)
			{
				finalResults.AppendLine();
				finalResults.AppendFormat("\t Bonus Games entered:");
				finalResults.AppendLine();
				foreach (KeyValuePair<string, int> kvp in stats.bonusGamesEntered)
				{
					finalResults.AppendFormat("\t\t{0} entered {1} times. ({2:p})", kvp.Key, kvp.Value, (float)kvp.Value / stats.spinsDone);
					finalResults.AppendLine();
				}
			}

			finalResults.AppendLine();
			finalResults.AppendFormat("{0} Exceptions:", stats.numberOfExceptions);
			finalResults.AppendLine();
			finalResults.Append(finallogExceptionBuilder);
			finalResults.AppendLine();
			finalResults.AppendFormat("{0} Errors:", stats.numberOfErrors);
			finalResults.AppendLine();
			finalResults.Append(finallogErrorBuilder);
			finalResults.AppendLine();
			finalResults.AppendFormat("{0} Warnings:", stats.numberOfWarnings);
			finalResults.AppendLine();
			finalResults.Append(finallogWarningBuilder);

			// Save results to file.
			TRAMPLogFiles.appendTextToFile(finalResults.ToString(), TRAMPLogFiles.TEST_RESULTS_FILE);
			TRAMPLogFiles.appendTextToFile(getTestSummary(), TRAMPLogFiles.SUMMARY_FILE);
			UnityEngine.Debug.Log(finalResults.ToString());

		}
	}

	public void appendFinalBuilder()
	{
		if (logExceptionBuilder.Length > 0)
		{
			// We want to print out the Errors and then the rest of the logs.
			finallogExceptionBuilder.AppendFormat("{0} Spin #{1}:\n", commonGame.gameKey, stats.spinsDone)
				.Append(postBuilder.ToString())
				.Append("\n")
				.Append(recievedBuilder.ToString())
				.Append("\n")
				.Append(logExceptionBuilder.ToString())
				.Append("Last Spin:\n")
				.Append(lastPostAndReviedBuilder.ToString());
		}
		if (logErrorBuilder.Length > 0)
		{
			// We want to print out the Errors and then the rest of the logs.
			finallogErrorBuilder.AppendFormat("{0} Spin #{1}:\n", commonGame.gameKey, stats.spinsDone)
				.Append(postBuilder.ToString())
				.Append("\n")
				.Append(recievedBuilder.ToString())
				.Append("\n")
				.Append(logErrorBuilder.ToString())
				.Append("Last Spin:\n")
				.Append(lastPostAndReviedBuilder.ToString());
		}
		if (logWarningBuilder.Length > 0)
		{
			// We want to print out the Errors and then the rest of the logs.
			finallogWarningBuilder.AppendFormat("{0} Spin #{1}:\n", commonGame.gameKey, stats.spinsDone)
				.Append(postBuilder.ToString())
				.Append("\n")
				.Append(recievedBuilder.ToString())
				.Append("\n")
				.Append(logWarningBuilder.ToString());
		}
		string lastSpin = string.Format("{0}\n{1}", postBuilder, recievedBuilder);

		clearLogBuilders();

		lastPostAndReviedBuilder.AppendLine(lastSpin);
	}

	private bool needsToCheckForActiveMiniGame()
	{
		if (SlotBaseGame.instance != null)
		{
			return SlotBaseGame.instance.hasActiveFeatureMiniGame();
		}

		return false;
	}
}
#endif
