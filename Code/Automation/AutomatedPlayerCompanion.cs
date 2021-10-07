 using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

#if ZYNGA_TRAMP
public class AutomatedPlayerCompanion
{
	// The instance of this companion that will be used, there should only ever be one companion.
	public static AutomatedPlayerCompanion instance = null;

	// Specific ID of this automated session.
	public string sessionId;

	// Start and end times for the entire automated session.
	public System.DateTime timeStarted = System.DateTime.MinValue;
	public System.DateTime timeEnded = System.DateTime.MaxValue;

	// Time scale of the automation.
	public float timeScale = 0;

	public bool isTestMemory = false;
	public bool shouldTestMiniGames = false;

	// File that the test should be loaded from.
	public string testFile = "[AUTO]";
	public string branchName = "[LEFT BLANK]";

	// Total numbers for all tests.
	public int totalGreenStatusTests = 0;
	public int totalYellowStatusTests = 0;
	public int totalRedStatusTests = 0;
	public int totalPinkStatusTests = 0;

	// Exceptions across all games.
	public Dictionary<string,int> gamekeyExceptionCounts;

	// Whether or not the test plan was aborted.
	public bool isTestPlanAborted = false;

	// Debug messages
	public const string LADI_DEBUG_COLOR = "magenta";
	public const string FAILED_QUEUE_ADD = "<color={0}>[LADI] Failed to add game with key \"{1}\" to test queue: Key does not exist.</color>";
	public const string FAILED_TO_LOAD = "<color={0}>[LADI] Failed to load LADI file at {1} due to: {2} </color>";
	public const string FAILED_TO_SAVE = "<color={0}>[LADI] Failed to save LADI file at {1} due to: {2} </color>";
	public const string GAME_LOAD_NO_DATA = "<color={0}>[LADI] Failed to load game {1} from LADI file. The key exists, but there is no data!</color>";
	public const string LADI_LOAD_GAME = "<color={0}>[LADI] Successfully loaded game {1} with {2} iterations</color>";

	// This contains information to visually display about the current game.
	private AutomatedCompanionVisualizer visualizer;

	// If this is the current active companion or a past loaded archive.
	public bool isCurrentTest = true;

	// Number of visual logs logged to LADI.
	private int numberOfVisualBugs = 0;

	// Delegates to push game logs and game keys to the control panel as they arrive.
	public delegate void AddLogToControlPanel(AutomatedCompanionLog log, bool isLobbyLog);
	public static AddLogToControlPanel addLogToControlPanel;
	public delegate void AddTestedGameToControlPanel(string gameKey);
	public static AddTestedGameToControlPanel addTestedGameToControlPanel;

	// A dictionary of all games that have been tested, with game key's as dictionary keys.
	// If a game is tested multiple times, LADI will save all iterations in a list. 
	public Dictionary<string, AutomatedGame> gamesTested;

	public List<KeyValuePair<string, AutomatedGameIteration>> gamesToTest;

	// The current active game iteration.
	public AutomatedGameIteration activeGame;

	public List<AutomatedCompanionLog> lobbyLogs;

	private bool isAutoSpinning = false;

	// Constructor for LADI to be created.
	// Load a specific JSON file if provided, otherwise load default.
	// Also specify if this companion should be the singleton instance.
	public AutomatedPlayerCompanion(bool isSingleton = true, JSON testJSON = null)
	{

		// Make this the singleton instance if necessary.
		if (isSingleton)
		{
			AutomatedPlayerCompanion.instance = this;
		}

		// Initialize collections.
		gamekeyExceptionCounts = new Dictionary<string, int>();
		gamesToTest = new List<KeyValuePair<string, AutomatedGameIteration>>();
		gamesTested = new Dictionary<string, AutomatedGame>();
		lobbyLogs = new List<AutomatedCompanionLog>();

		Debug.LogFormat("<color={0}> [LADI] Initializing LADI </color>", LADI_DEBUG_COLOR);

		if (AutomatedPlayer.instance != null)
		{
			sessionId = AutomatedPlayer.SessionId.ToString();
		}

		// If no JSON provided, just load default. Otherwise load the JSON.
		if (testJSON == null)
		{
			TRAMPLogFiles.loadCompanionFromFile();
		}
		else
		{
			loadJSON(testJSON);
		}

		if (visualizer == null)
		{
			visualizer = AutomatedCompanionVisualizer.instance;
			Debug.Log("LADI Game Info Visualizer initialized");
		}
	}

	// Loads the most recent active test.
	public void loadCurrentTest()
	{

		TRAMPLogFiles.loadCompanionFromFile();
		TRAMPLogFiles.loadCurrentTestPlan();
		isCurrentTest = true;

	}
	
	public void updateBranchName()
	{
		string currentBranch = AutomatedPlayerProcesses.getBranchName();
		if (string.IsNullOrEmpty(branchName) || branchName.Equals("[LEFT BLANK]"))
		{
			branchName = currentBranch;
		}
		else if (!branchName.Equals(currentBranch))
		{
			Debug.LogWarningFormat("Test was started on git branch {0}, but the current branch is {1}. This may skew test data.", branchName, currentBranch);
			branchName = currentBranch;
		}
	}

	// Loads a specific archived test run, given the JSON data for it.
	public void loadPastTest(JSON ladiData, JSON testPlan)
	{

		// Save the current files to make sure we don't lose anything.
		TRAMPLogFiles.saveAllFiles();
		
		// Stop automation, since we can't automate on past runs.
		if (AutomatedPlayer.isAutomating)
		{
			AutomatedPlayer.instance.stopAutomation();
		}

		try
		{
			loadJSON(ladiData);
			loadTestPlanFromJSON(testPlan);
			isCurrentTest = false;
		}
		catch (System.Exception e)
		{
			// If something went wrong with the loading, log an error and reload the current test.
			Debug.LogErrorFormat("Failed to load past TRAMP test: {0}", e);
			loadCurrentTest();
		}
	}

	// Loads a past test given an existing companion to load from.
	public void loadPastTest(AutomatedPlayerCompanion testToLoad)
	{

		// Create JSON files out of the provided test.
		JSON testJSON = new JSON(testToLoad.ToJSON());
		JSON testPlanJSON = new JSON(testToLoad.testPlanToJSON());

		// Load test as normal.
		loadPastTest(testJSON, testPlanJSON);
	}

	// Combine several tests given a JSON file.
	public static AutomatedPlayerCompanion combineTests(JSON combinedJSON, bool loadImmediately = true)
	{

		// Load the JSON array of game tests.
		JSON[] instancesJSON = combinedJSON.getJsonArray(AutomationJSONKeys.INSTANCES_KEY, true);

		// The base test that everything will be merged into.
		AutomatedPlayerCompanion baseTest = null;

		// Iterate through each test in the JSON file.
		for (int i = 0; i < instancesJSON.Length; i++)
		{

			// Grabs the JSON from the JSON array.
			JSON instanceJSON = instancesJSON[i];

			// Create an AutomatedPlayerCompanion instance from the JSON data.
			AutomatedPlayerCompanion instanceTest = new AutomatedPlayerCompanion(false, instanceJSON);

			// If there's not already a base test, make the loaded companion the base test.
			// Otherwise, merge the loaded instance into the base test.
			if (baseTest == null)
			{
				baseTest = instanceTest;
			}
			else
			{
				baseTest.combineTest(instanceTest);
			}
		}
		
		// If the combining was successful, attempt to load the combined test.
		if (baseTest != null)
		{
			if (loadImmediately && instance != null)
			{
				instance.loadPastTest(baseTest);
			}
			else
			{
				Debug.LogError("LADI hasn't been initiliazed, cannot load combined tests");
			}
		}
		else
		{
			Debug.LogError("Failed to load JSON from file. No instances found");
		}
		
		return baseTest;
	}

	// Merges another AutomatedPlayerCompanion into the existing one.
	public void combineTest(AutomatedPlayerCompanion otherTest)
	{

		// Add in the log counts.
		totalGreenStatusTests += otherTest.totalGreenStatusTests;
		totalYellowStatusTests += otherTest.totalYellowStatusTests;
		totalRedStatusTests += otherTest.totalRedStatusTests;
		numberOfVisualBugs += otherTest.numberOfVisualBugs;

		// Iterate through each tested game and add their results.
		foreach (KeyValuePair<string, AutomatedGame> gameTest in otherTest.gamesTested)
		{
			gamesTested.Add(gameTest.Key, gameTest.Value);
		}

		// Add in the exception counts dictionary.
		foreach (KeyValuePair<string, int> exception in otherTest.gamekeyExceptionCounts)
		{

			if (gamekeyExceptionCounts.ContainsKey(exception.Key))
			{
				gamekeyExceptionCounts[exception.Key] += exception.Value;
			}
			else
			{
				gamekeyExceptionCounts.Add(exception.Key, exception.Value);
			}
		}
	}

	// Updates any values related to the active spin.
	public void updateCurrentSpinValues(long currentReward, long netReturns)
	{
		if (activeGame != null)
		{
			activeGame.stats.coinsReturned += currentReward;
		}
	}

	// Forces the status numbers for games to update, based on tested games.
	public void updateGameStatusCount()
	{

		// First, reset them all back to zero.
		totalGreenStatusTests = 0;
		totalPinkStatusTests = 0;
		totalRedStatusTests = 0;
		totalYellowStatusTests = 0;

		// Iterate through each game, and determine its status.
		foreach (AutomatedGame game in gamesTested.Values)
		{

			// Use the average stats across all iterations. It always takes the worst test.
			// Reporting per-iteration status results is verbose and can be misleading.
			AutomatedGameStats averageStats = game.averageStats;

			if (averageStats.numberOfExceptions > 0)
			{
				totalPinkStatusTests++;
			}
			else if (averageStats.numberOfErrors > 0)
			{
				totalRedStatusTests++;
			}
			else if (averageStats.numberOfWarnings > 0)
			{
				totalYellowStatusTests++;
			}
			else
			{
				// If there are warnings, errors, or exceptions, it's green.
				totalGreenStatusTests++;
			}
		}
	}

	// Called when a request for a spin is sent to the server.
	public void spinRequested()
	{
		
		if (visualizer != null)
		{
			visualizer.spinRequested();
		}
	}

	// Called when we receive an outcome for a spin from the server.
	public void spinReceived()
	{
		if (activeGame != null && AutomatedPlayer.isRunningAutospins && isAutoSpinning)
		{

			activeGame.stats.autoSpinsReceived++;
			if (!(activeGame.stats.autoSpinsRequested == activeGame.stats.autoSpinsReceived))
			{
				Debug.LogErrorFormat("<color={0}>TRAMP> Requested autospins spin count ({1}) does not match outcomes received ({2})!",
					AutomatedPlayer.TRAMP_DEBUG_COLOR,
					activeGame.stats.autoSpinsRequested,
					activeGame.stats.autoSpinsReceived);
			}
		}
	}

	// Called when specifically an auto spin is received.
	// Note that spinRequested() is called in ADDITION to this method, not instead of. 
	public void autospinRequested()
	{
		if (activeGame != null && AutomatedPlayer.isRunningAutospins)
		{
			if (activeGame.stats.autoSpinsRequested != activeGame.stats.autoSpinsFinished)
			{
				Debug.LogErrorFormat("{0} autospin spin requested but previous spin did not finish!",
					activeGame.commonGame.gameKey);
			}
			activeGame.stats.autoSpinsRequested++;

			// We're in the middle of an auto spin.
			isAutoSpinning = true;
		}
	}

	// Called when a spin is completed.
	public void spinFinished()
	{
		if (visualizer != null)
		{
			visualizer.spinFinished();
		}

		// If we're autospinning, finish the autospin.
		if (isAutoSpinning)
		{
			autospinFinished();
		}
	}

	// Called when an autospin finishes spinning.
	public void autospinFinished()
	{
		if (activeGame != null && AutomatedPlayer.isRunningAutospins)
		{
			activeGame.stats.autoSpinsFinished++;	

			// if the autospins numbers don't all match up
			if (activeGame.stats.autoSpinsRequested != activeGame.stats.autoSpinsReceived ||
				activeGame.stats.autoSpinsReceived != activeGame.stats.autoSpinsFinished)
			{
				Debug.LogErrorFormat("<color={0}>TRAMP> {1} autospin spins out of sync! Requested {2}, Received {3}, Finished {4}</color>",
					AutomatedPlayer.TRAMP_DEBUG_COLOR,
					activeGame.commonGame.gameKey,
					activeGame.stats.autoSpinsRequested,
					activeGame.stats.autoSpinsReceived,
					activeGame.stats.autoSpinsFinished);	
			}

			// No longer in the middle of an auto spin.
			isAutoSpinning = false;
		}

		if (AutomatedPlayer.currentNumberOfAutospinsToComplete < activeGame.stats.autoSpinsFinished)
		{
			Debug.LogErrorFormat("<color={0}>TRAMP> {1} Expecting to be done with autospins at {2}, but we're at {3} autospins.</color>",
					AutomatedPlayer.TRAMP_DEBUG_COLOR,
					activeGame.commonGame.gameKey,
					AutomatedPlayer.currentNumberOfAutospinsToComplete,
					activeGame.stats.autoSpinsFinished);
		}

		if (AutomatedPlayer.currentNumberOfAutospinsToComplete == activeGame.stats.autoSpinsFinished)
		{
			AutomatedPlayer.instance.automateStop();// Click the stop button here so we stop doing auto spins.
			activeGame.stats.autoSpinsRequested = 0;
			activeGame.stats.autoSpinsReceived = 0;
			activeGame.stats.autoSpinsFinished = 0;
		}
	}

	// Updates the time scale.
	public void setTimeScale(float newTimeScale)
	{
		Time.timeScale = newTimeScale;
		timeScale = Time.timeScale;
	}

	// Adds a new game to test with a specified key.
	public AutomatedGameIteration addNewGameToTest(string gameKey, bool addToFront = false)
	{

		if (LobbyGame.find(gameKey) != null)
		{
			Debug.LogFormat("<color={0}>[LADI] Unlocking game with key \"{1}\" to test.</color>", LADI_DEBUG_COLOR, gameKey);
			PlayerAction.devGameUnlock(gameKey);

			AutomatedGameIteration newTest = new AutomatedGameIteration();
			newTest.addTestAction(AutomatedPlayer.AUTOMATIC_TEST_FLAG);
			KeyValuePair<string, AutomatedGameIteration> newEntry = new KeyValuePair<string, AutomatedGameIteration>(gameKey, newTest);

			if (addToFront)
			{
				gamesToTest.Insert(0, newEntry);
			}
			else
			{
				gamesToTest.Add(newEntry);
			}
			
			return newTest;
		}
		else
		{
			Debug.LogFormat(FAILED_QUEUE_ADD, LADI_DEBUG_COLOR, gameKey);
		}

		return null;

	}

	//Take the game we're currently testing and re-add it to the stack with the test plan it was already in the middle of
	public void addCurrentGameToTopOfTestStack()
	{
		activeGame.isTesting = false;
		KeyValuePair<string, AutomatedGameIteration> currentGameTest = new KeyValuePair<string, AutomatedGameIteration>(activeGame.commonGame.gameKey, activeGame);
		gamesToTest.Insert(0, currentGameTest);
	}

	// Starts the next or specified 
	public KeyValuePair<string, AutomatedGameIteration> startNextGameLog(int index = 0)
	{
		KeyValuePair<string, AutomatedGameIteration> gameToTest;

		if (index == 0)
		{
			gameToTest = popNextGameInQueue();
		}
		else
		{
			gameToTest = gamesToTest[index];
			gamesToTest.RemoveAt(index);
		}

		startGameLog(gameToTest);

		return gameToTest;
	}

	// Removes and returns the next game in the games to test queue.
	public KeyValuePair<string, AutomatedGameIteration> popNextGameInQueue()
	{
		KeyValuePair<string, AutomatedGameIteration> nextGame = gamesToTest[0];
		gamesToTest.RemoveAt(0);
		return nextGame;
	}

	// Begins logging the specified game.
	public void startGameLog(KeyValuePair<string, AutomatedGameIteration> gameToTest)
	{
		// If there's already an active game, stop it's log.
		if (activeGame != null)
		{
			endActiveGameLog(AutomatedPlayer.instance.getGameMode());
		}

		string newGameKey = gameToTest.Key;

		SlotResourceData resourceData = SlotResourceMap.getData(newGameKey);

		if (AutomatedPlayer.shouldSkipPorts && 
			resourceData != null && resourceData.isPort)
		{
			activeGame = null;
			return;
		}

		// If the new game to test already exists.
		if (gamesTested.ContainsKey(newGameKey))
		{
			// Retrieve the existing game.
			AutomatedGame automatedGame;
			gamesTested.TryGetValue(newGameKey, out automatedGame);

			gameToTest.Value.commonGame = automatedGame;
		}
		// If the new game to test doesn't exist, make a new one.
		else
		{

			AutomatedGame newGame = new AutomatedGame(gameToTest.Key);
			gameToTest.Value.commonGame = newGame;
		}

		// Set the active game.
		activeGame = gameToTest.Value;
	}

	// Stop logging the active game.
	public void endActiveGameLog(AutomatedPlayer.GameMode gameMode, bool forced = false)
	{
		if (activeGame != null)
		{
			// Check if there's already an existing automated game for this iteration.
			AutomatedGame existingAutomatedGame = getGameTested(activeGame.commonGame.gameKey);

			// If the game was force ended, set the correct flag.
			activeGame.forceEnded = forced;

			// Let the iteration know it's done.
			activeGame.done(gameMode);

			// If there wasn't an existing common game for this iteration.
			if (existingAutomatedGame == null)
			{
				// Add a brand new entry to the automated game dictionary.
				gamesTested.Add(activeGame.commonGame.gameKey, activeGame.commonGame);
				existingAutomatedGame = activeGame.commonGame;

				// We need to add the iteration before we push the tested game to control panel.
				existingAutomatedGame.addNewIteration(activeGame);
				if (AutomatedPlayerCompanion.addTestedGameToControlPanel != null)
				{
					AutomatedPlayerCompanion.addTestedGameToControlPanel(activeGame.commonGame.gameKey); // Notify the control panel that a new game was added.
				}
			}
			// If there was an automated game, just add this iteration to it.
			else
			{
				existingAutomatedGame.addNewIteration(activeGame);
			}

			if (visualizer != null)
			{
				visualizer.resetValues();
			}
		}

		activeGame = null;

		// Save everything in case of crash.
		TRAMPLogFiles.saveAllFiles();

	}

	public void continueLoggingExistingGame(string gameKey)
	{
		AutomatedGameIteration iterationToTest = null;
		// If the new game to test already exists.
		if (gamesTested.ContainsKey(gameKey))
		{
			// Retrieve the existing game.
			AutomatedGame testedGame = getGameTested(gameKey);

			iterationToTest = testedGame.getLatestIteration();
		}
		// If the new game to test doesn't exist, make a new one.
		else
		{
			iterationToTest = new AutomatedGameIteration();
			AutomatedGame newGame = new AutomatedGame(gameKey);
			iterationToTest.commonGame = newGame;
		}

		// Set the active game.
		if (iterationToTest != null)
		{
			activeGame = iterationToTest;
			activeGame.saveAction(new AutomatedTestAction(AutomatedTestAction.PLAY_GIFTED_GAME_ACTION));
			activeGame.continueLogging();
		}
	}

	// Gets all the games of a specified log type.
	public List<AutomatedGame> getGamesWithLogs(LogType logType) 
	{
		List<AutomatedGame> gamesWithLogs = new List<AutomatedGame>();

		// Iterate through each entry in the automated games list.
		foreach (KeyValuePair<string, AutomatedGame> game in gamesTested)
		{
			// Iterate through each iteration of each game.
			foreach (AutomatedGameIteration gameIteration in game.Value.gameIterations)
			{
				if (gameIteration.stats.hasLogsOfType(logType))
				{
					gamesWithLogs.Add(game.Value);
				}
			}
		}

		// Also check the active game.
		if (activeGame.stats.hasLogsOfType(logType))
		{
			gamesWithLogs.Add(activeGame.commonGame);
		}

		return gamesWithLogs;
	}

	// Returns a list of all game keys that have logs of the specified type. Used for filtering.
	public HashSet<string> getAllGameKeysWithLogs(LogType logType) 
	{
		HashSet<string> gameKeysWithLogs = getTestedGameKeysWithLogs(logType);
		// Also check the active game.
		if (activeGame.stats.hasLogsOfType(logType))
		{
			gameKeysWithLogs.Add(activeGame.commonGame.gameKey);
		}
		return gameKeysWithLogs;
	}

	// Returns a list of tested game keys that have logs of the specified type. Used for filtering.
	public HashSet<string> getTestedGameKeysWithLogs(LogType logType) 
	{
		HashSet<string> gameKeysWithLogs = new HashSet<string>();

		// Iterate through each entry in the automated games list.
		foreach (KeyValuePair<string, AutomatedGame> game in gamesTested)
		{
			// Iterate through each iteration of each game.
			foreach (AutomatedGameIteration gameIteration in game.Value.gameIterations)
			{
				if (gameIteration.stats.hasLogsOfType(logType))
				{
					gameKeysWithLogs.Add(game.Value.gameKey);
					break;
				}
			}
		}
		return gameKeysWithLogs;
	}

	// Returns a list of tested game keys and the active game without issues. (no warnings, errors, or exceptions)
	public HashSet<string> getAllGameKeysWithoutIssues() 
	{
		HashSet<string> gameKeysWithoutIssues = getTestedGameKeysWithoutIssues();
		// Also check the active game.
		if (activeGame.stats.hasNoIssues())
		{
			gameKeysWithoutIssues.Add(activeGame.commonGame.gameKey);
		}

		return gameKeysWithoutIssues;
	}

	// Returns a list of tested game keys without issues
	public HashSet<string> getTestedGameKeysWithoutIssues() 
	{
		HashSet<string> gameKeysWithoutIssues = new HashSet<string>();

		// Iterate through each entry in the automated games list.
		foreach (KeyValuePair<string, AutomatedGame> game in gamesTested)
		{
			// Iterate through each iteration of each game.
			foreach (AutomatedGameIteration gameIteration in game.Value.gameIterations)
			{
				if (gameIteration.stats.hasNoIssues())
				{
					gameKeysWithoutIssues.Add(game.Value.gameKey);
					break;
				}
			}
		}
		return gameKeysWithoutIssues;
	}

	// Gets all automated games that had visual bugs reported.
	public List<AutomatedGame> getGamesWithVisualBugs()
	{
		List<AutomatedGame> gamesWithVisualBugs = new List<AutomatedGame>();

		// Iterate through each entry in the automated games list.
		foreach (KeyValuePair<string, AutomatedGame> game in gamesTested)
		{
			// Iterate through each iteration of each game.
			foreach (AutomatedGameIteration gameIteration in game.Value.gameIterations)
			{
				if (gameIteration.visualBugs.Count > 0)
				{
					gamesWithVisualBugs.Add(game.Value);
				}
			}
		}

		return gamesWithVisualBugs;
	}
		
	// Adds a log to the currently active game.
	public AutomatedCompanionLog addLog(LogType logType, string message, string stack)
	{
		// We don't really care about regular logs. Spare us redundancy. Only show the good stuff.
		if (logType != LogType.Log)
		{
			if (activeGame != null && AutomatedPlayer.instance.getGameMode() != AutomatedPlayer.GameMode.LOBBY)
			{
				AutomatedCompanionLog log = activeGame.createNewLog(logType, message, stack);
				if (AutomatedPlayerCompanion.addLogToControlPanel != null && log != null)
				{
					AutomatedPlayerCompanion.addLogToControlPanel(log, false);
				}
				return log;
			}
			// If there isn't an active game, add the log as a lobby log.
			else
			{
				if (lobbyLogs == null)
				{
					lobbyLogs = new List<AutomatedCompanionLog>();
				}

				AutomatedCompanionLog lobbyLog = new AutomatedCompanionLog(logType, message, stack, lobbyLogs.Count - 1, System.DateTime.Now, "Unknown", -1);
				lobbyLogs.Add(lobbyLog);
				if (AutomatedPlayerCompanion.addLogToControlPanel != null && lobbyLog != null)
				{
					AutomatedPlayerCompanion.addLogToControlPanel(lobbyLog, true);
				}
				return lobbyLog;
			}
		}

		return null;
	}

	// Reports a visual bug on the current game.
	public AutomatedCompanionLog reportVisualBug()
	{
		string logMessage = "<VISUAL BUG> ";
		AutomatedCompanionLog newVisualLog = addLog(LogType.Error, logMessage, "");

		// Note- since this should be recorded from the control panel, add functionality to set a specific message.
		return newVisualLog;


	}

	// Gets the automated game of the specified key.
	public AutomatedGame getAutomatedGameByKey(string key)
	{
		AutomatedGame gameToReturn;
		if (gamesTested.TryGetValue(key, out gameToReturn))
		{
			return gameToReturn;
		}

		if (activeGame != null && activeGame.commonGame.gameKey == key)
		{
			return activeGame.commonGame;
		}

		return null;
	}

	// Retrieves the queue of games to test. (It's actually a list but we'll call it a queue)
	public List<KeyValuePair<string, AutomatedGameIteration>> getGamesToTestQueue()
	{
		return gamesToTest;
	}

	// Return a game that has already been tested by key.
	public AutomatedGame getGameTested(string key)
	{
		AutomatedGame gameTested;
		gamesTested.TryGetValue(key, out gameTested);

		return gameTested;
	}

	// Check if a specific iteration has already been tested.
	public bool isIterationTested(AutomatedGameIteration iteration)
	{
		if (iteration.commonGame != null)
		{
			if (iteration.commonGame.hasBeenTested())
			{
				if (iteration.commonGame.gameIterations.Contains(iteration))
				{
					return true;
				}
			}
		}

		return false;

	}
		
	// Stops the current game that's testing and immediately plays the desired game.
	public void addAndPlayGameImmediate(string key)
	{
		// First, add the new game to the queue.
		AutomatedGameIteration addedGame = addNewGameToTest(key, true);

		if (activeGame != null && addedGame != null)
		{
			// Stop the current game that's running and move on to forced game.
			AutomatedPlayer.instance.gameTestDone(true);
		}
	}

	// Removes a game from the queue with the specified key.
	public void removeGameFromQueue(AutomatedGameIteration gameToRemove)
	{
		for (int i = 0; i < gamesToTest.Count; i++)
		{
			KeyValuePair<string, AutomatedGameIteration> game = gamesToTest[i];

			if (game.Value == gameToRemove)
			{
				gamesToTest.RemoveAt(i);
			}
		}
	}

	// Toggles random testing for the automated player, which tests games at random.
	public bool toggleRandomTesting()
	{
		AutomatedPlayer.playRandomGamesInQueue = !AutomatedPlayer.playRandomGamesInQueue;
		return AutomatedPlayer.playRandomGamesInQueue;
	}

	// Toggles whether or not TRAMP can run tests on games already tested.
	public bool toggleRepeatTesting()
	{
		AutomatedPlayer.repeatTestsOnCompletion = !AutomatedPlayer.repeatTestsOnCompletion;
		return AutomatedPlayer.repeatTestsOnCompletion;
	}

	// Loads JSON data into this automated companion.
	public void loadJSON(JSON json)
	{

		// Make sure that the gamesTested list is cleared before we add to it.
		gamesTested.Clear();
		activeGame = null;

		this.numberOfVisualBugs = json.getInt(AutomationJSONKeys.NUM_OF_VISUAL_BUGS_KEY, 0);

		AutomatedPlayer.playRandomGamesInQueue = json.getBool(AutomationJSONKeys.RANDOM_TESTING, false);
		AutomatedPlayer.repeatTestsOnCompletion = json.getBool(AutomationJSONKeys.REPEAT_TESTING, false);
		AutomatedPlayer.pullLatestFromGitOnCompletion = json.getBool(AutomationJSONKeys.PULL_WHEN_DONE, false);
		ColliderVisualizer.instance.active = json.getBool(AutomationJSONKeys.COLLIDER_VISUALIZATION, false);

		sessionId = json.getString(AutomationJSONKeys.SESSION_ID_KEY, AutomatedPlayer.SessionId.ToString(), AutomatedPlayer.SessionId.ToString());
		gamekeyExceptionCounts = json.getStringIntDict(AutomationJSONKeys.GAMEKEY_EXCEPTION_COUNTS_KEY);
		timeStarted = System.DateTime.Parse(json.getString(AutomationJSONKeys.TIME_STARTED_KEY, System.DateTime.MinValue.ToString()));
		timeEnded = System.DateTime.Parse(json.getString(AutomationJSONKeys.TIME_ENDED_KEY, System.DateTime.MaxValue.ToString()));
		timeScale = json.getFloat(AutomationJSONKeys.TIME_SCALE_KEY, 1.0f);
		isTestMemory = json.getBool(AutomationJSONKeys.IS_TEST_MEMORY_KEY, false);
		shouldTestMiniGames = json.getBool(AutomationJSONKeys.SHOULD_TEST_MINI_GAMES_KEY, false);
		isTestPlanAborted = json.getBool(AutomationJSONKeys.IS_TEST_PLAN_ABORTED_KEY, false);
		testFile = json.getString(AutomationJSONKeys.TEST_FILE_KEY, AutomatedPlayer.AUTOMATIC_TEST_FLAG);
		branchName = json.getString(AutomationJSONKeys.BRANCH_NAME_KEY, "[LEFT BLANK]");
		totalGreenStatusTests = json.getInt(AutomationJSONKeys.TOTAL_GREEN_STATUS_TESTS_KEY, 0);
		totalYellowStatusTests = json.getInt(AutomationJSONKeys.TOTAL_YELLOW_STATUS_TESTS_KEY, 0);
		totalRedStatusTests = json.getInt(AutomationJSONKeys.TOTAL_RED_STATUS_TESTS_KEY, 0);
		gamekeyExceptionCounts = json.getStringIntDict(AutomationJSONKeys.GAMEKEY_EXCEPTION_COUNTS_KEY);
		isTestPlanAborted = json.getBool(AutomationJSONKeys.IS_TEST_PLAN_ABORTED_KEY, false);

		// Update the name of the branch to be the current branch name.
		updateBranchName();

		JSON automatedGamesJson = json.getJSON(AutomationJSONKeys.AUTOMATED_GAMES_KEY);
		if (automatedGamesJson != null)
		{
			foreach(string key in automatedGamesJson.getKeyList())
			{
				JSON loadedGameJson = automatedGamesJson.getJSON(key);
				if (loadedGameJson != null)
				{
					AutomatedGame loadedGame = new AutomatedGame(loadedGameJson);
					gamesTested.Add(key, loadedGame);

					Debug.LogFormat(LADI_LOAD_GAME, LADI_DEBUG_COLOR, key, loadedGame.gameIterations.Count);
				}

				else
				{
					Debug.LogErrorFormat(GAME_LOAD_NO_DATA, LADI_DEBUG_COLOR, key);
				}
			}
		}
	}

	// Encodes this AutomatedPlayerCompanion into JSON, and returns the JSON string.
	public string ToJSON()
	{
		
		StringBuilder build = new StringBuilder();
		build.Append("{");
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.SESSION_ID_KEY, sessionId.ToString()));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.TIME_SCALE_KEY, timeScale));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.IS_TEST_MEMORY_KEY, isTestMemory));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.TEST_FILE_KEY, testFile));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.BRANCH_NAME_KEY, branchName));

		// Make sure the game status counts are fully updated.
		updateGameStatusCount();

		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.TOTAL_GREEN_STATUS_TESTS_KEY, totalGreenStatusTests));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.TOTAL_YELLOW_STATUS_TESTS_KEY, totalYellowStatusTests));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.TOTAL_RED_STATUS_TESTS_KEY, totalRedStatusTests));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.GAMEKEY_EXCEPTION_COUNTS_KEY, gamekeyExceptionCounts));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.IS_TEST_PLAN_ABORTED_KEY, isTestPlanAborted));
														 
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.NUM_OF_VISUAL_BUGS_KEY, numberOfVisualBugs));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.RANDOM_TESTING, AutomatedPlayer.playRandomGamesInQueue));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.REPEAT_TESTING, AutomatedPlayer.repeatTestsOnCompletion));
		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.PULL_WHEN_DONE, AutomatedPlayer.pullLatestFromGitOnCompletion));

		build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.COLLIDER_VISUALIZATION, ColliderVisualizer.instance.active));
				
		// Start automated games log
		build.AppendFormat("\"{0}\":{{", AutomationJSONKeys.AUTOMATED_GAMES_KEY);

		int automatedGamesWritten = 0;
		foreach (string key in gamesTested.Keys)
		{
			// Log all games with this key
			build.AppendFormat("\"{0}\":{{", key);
			AutomatedGame automatedGame;
			gamesTested.TryGetValue(key, out automatedGame);

			build.Append(automatedGame.ToJSON());
				
			// Close log of games with key
			build.Append("}");
			automatedGamesWritten++;
							
			if (automatedGamesWritten < gamesTested.Keys.Count)
			{
				build.Append(",");
			}
		}

		// Close automated games log
		build.Append("}");

		build.Append("}");

		return build.ToString();
	}

	// Encodes the test plan of gamesToTest to JSON and returns the JSON string.
	public string testPlanToJSON()
	{
		StringBuilder build = new StringBuilder();

		build.Append("{");

		int gamesToTestWritten = 0;
		build.AppendFormat("\"{0}\":{{", AutomationJSONKeys.GAMES_TO_TEST_KEY);
		foreach (KeyValuePair<string, AutomatedGameIteration> kv in gamesToTest)
		{
			build.AppendFormat("\"{0}\":{{", gamesToTestWritten);
			build.AppendFormat("{0},", JSON.createJsonString(AutomationJSONKeys.GAME_TO_TEST_KEY_KEY, kv.Key));

			build.AppendFormat("\"{0}\":{{", AutomationJSONKeys.GAME_TO_TEST_ITERATION_KEY);
			build.Append(kv.Value.ToJSON());
			build.Append("}");

			build.Append("}");

			gamesToTestWritten++;

			if (gamesToTestWritten < gamesToTest.Count)
			{
				build.Append(",");
			}
		}
		build.Append("}");

		build.Append("}");
			
		return build.ToString();
	}

	// Loads a JSON string as a test plan.
	public void loadTestPlanFromJSON(JSON json)
	{

		// Make sure the games to test list is clear before we add to it.
		gamesToTest.Clear();

		JSON gamesToTestJson = json.getJSON(AutomationJSONKeys.GAMES_TO_TEST_KEY);
		if (gamesToTestJson != null)
		{
			foreach (string key in gamesToTestJson.getKeyList())
			{
				JSON iterationJSON = gamesToTestJson.getJSON(key);
				if (iterationJSON != null)
				{
					string gameKey = iterationJSON.getString(AutomationJSONKeys.GAME_TO_TEST_KEY_KEY, "[BLANK]");

					JSON gameJSON = iterationJSON.getJSON(AutomationJSONKeys.GAME_TO_TEST_ITERATION_KEY);
					if (gameJSON != null)
					{
						AutomatedGameIteration loadedIteration = new AutomatedGameIteration(null, gameJSON);
						gamesToTest.Add(new KeyValuePair<string, AutomatedGameIteration>(gameKey, loadedIteration));
						Debug.LogFormat("<color={0}>[LADI] Unlocking game with key \"{1}\" from test plan.</color>", LADI_DEBUG_COLOR, gameKey);
						PlayerAction.devGameUnlock(gameKey);
					}
				}
			}
		}
	}
		
	// Returns a string of all relevant AutomatedPlayerCompanion data.
	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();

		stringBuilder.Append("-- TRAMP SUMMARY ---");
		stringBuilder.AppendLine();

		SkuId skuId = SkuId.HIR;
		
		stringBuilder.AppendFormat("SKU: {0}", skuId);
		stringBuilder.AppendLine();

		stringBuilder.AppendFormat("Config: {0}",PlayerPrefsCache.GetString(DebugPrefs.EDITOR_CONFIG_FILE + SkuResources.currentSku.ToString(), "NONE"));
		stringBuilder.AppendLine();

		stringBuilder.AppendFormat("Branch: {0}", branchName);
		stringBuilder.AppendLine();

		stringBuilder.AppendFormat("Games Tested: {0}", gamesTested.Count);
		stringBuilder.AppendLine();

		System.TimeSpan playTime = timeEnded - timeStarted;
		if (isTestPlanAborted)
		{
			stringBuilder.AppendFormat("Test started: {0:u}, ABORTED AT: {1:u} ({2:D2}:{3:D2}:{4:D2}.{5:D2})", timeStarted, timeEnded, 
				playTime.Hours, playTime.Minutes, playTime.Seconds, playTime.Milliseconds);
		}
		else
		{
			stringBuilder.AppendFormat("Test started: {0:u}, Ended: {1:u} ({2:D2}:{3:D2}:{4:D2}.{5:D2})", timeStarted, timeEnded, 
				playTime.Hours, playTime.Minutes, playTime.Seconds, playTime.Milliseconds);
		}
		stringBuilder.AppendLine();

		stringBuilder.AppendFormat("Time Scale: {0}", Time.timeScale);
		stringBuilder.AppendLine();

		// Make sure the game status counts are fully updated.
		updateGameStatusCount();

		stringBuilder.AppendFormat("RESULTS: {0} green, {1} yellow, {2} red, {3} pink", 
			totalGreenStatusTests, totalYellowStatusTests, totalRedStatusTests, totalPinkStatusTests);
		stringBuilder.AppendLine();

		stringBuilder.Append("Fatal Errors: ");
		if (gamekeyExceptionCounts != null && gamekeyExceptionCounts.Count > 0)
		{
			int index = 0;
			foreach (var item in gamekeyExceptionCounts)
			{
				stringBuilder.AppendFormat("{0}({1})", item.Key, item.Value);

				if (index < gamekeyExceptionCounts.Count - 1)
				{
					stringBuilder.Append(", ");
				}
				index++;
			}
		}
		else
		{
			stringBuilder.Append("NONE");
		}
		stringBuilder.AppendLine();

		stringBuilder.AppendLine();

		stringBuilder.AppendFormat("--- Game Summaries ---");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine();

		foreach (KeyValuePair<string, AutomatedGame> gameTested in gamesTested)
		{
			stringBuilder.Append(gameTested.Value.getTestResults(true));
			stringBuilder.AppendLine();
			stringBuilder.AppendLine();
		}

		return stringBuilder.ToString();
	}
}
#endif
