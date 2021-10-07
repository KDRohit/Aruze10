/* Run TRAMP using the command line tool ./tools/run-TRAMP.sh
 *
 * Sped Up: 			./tools/run-TRAMP.sh --HIR --TimeScale 10
 * Memory Test:			./tools/run-TRAMP.sh --HIR --TestMemory true
 * Number of Spins:		./tools/run-TRAMP.sh --HIR -n 100
 * Run HIR Test from JSON oneGame.json (not support by command line tool):
 	/Applications/Unity/Unity.app/Contents/MacOS/Unity -projectPath "/Users/pludington/hir-client-mobile/Unity" -executeMethod CommandLineHelpers.trampHIR "-CustomArgs:branchName=$(git symbolic-ref HEAD);testFile=/Users/pludington/hir-client-mobile/Unity/Assets/Code/Automation/oneGame.json;timeScale=1;testMemory=true;"
 * ** Results **
 * Files will output to the path Application.persistentDataPath + /TRAMP
 * It will be something like /Users/pludington/Library/Application Support/Zynga/Hit It Rich/TRAMP
 */

using System.Collections;
using System.Collections.Generic;
using System.Text;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

#if ZYNGA_TRAMP
// TRAMP - testing repeatedly all meaningful products
[ExecuteInEditMode]
[InitializeOnLoad]
public class AutomatedPlayer : TICoroutineMonoBehaviour
{
	// Base game information:
	[HideInInspector] public bool forceFreespins = false;
	[HideInInspector] public bool forcePickem = false;
	[HideInInspector] public bool forceOther = false;

	public int numberOfSpins = 10; // Used by the AUTOMATIC_TEST_FLAG.
	private bool loadingGame = false;
	private AutomatedTestAction lastActionPerformed = null;

	public AutomatedPlayerCompanion companion;

	public enum GameMode
	{
		NONE = 0,
		LOBBY = 1,
		LOADING = 2,
		BASE_GAME = 3,
		BONUS_GAME = 4,
	}

	private GameMode gameMode = GameMode.NONE;
	private GameMode lastGameMode = GameMode.NONE;
	private static bool didInit = false;
    private List<string> gameKeyCheck = new List<string>();

	private static bool startedFromCommandLine = false;
	private static bool isAborting = false;
	public static AutomatedPlayer instance = null;
	public static bool isOver9000Vertices = false;

	const string startupScene = "Assets/Data/HIR/Scenes/Startup.unity";
	public const string AUTOMATIC_TEST_FLAG = "[AUTO]";
	public const string TRAMP_DEBUG_COLOR = "aqua";
	private const float SLAM_STOP_MIN_DELAY = 0.0f;
	private const float SLAM_STOP_MAX_DELAY = 1.0f;

	public float slamStopDelayTime 
	{
		get 
		{
			return Random.Range(SLAM_STOP_MIN_DELAY, SLAM_STOP_MAX_DELAY);
		}
	}
	public static bool playRandomGamesInQueue = false;
	public const string PLAY_RANDOM_GAMES_IN_QUEUE_EDITOR_PREF = "TRAMP_AutomatedPlayer_playRandomGamesInQueue";

	public static bool forceGameExitAfterMaxTime = true;
	public const string FORCE_GAME_EXIT_AFTER_MAX_TIME_EDITOR_PREF = "TRAMP_AutomatedPlayer_forceGameExitAfterMaxTime";
	public static bool repeatTestsOnCompletion = false;
	public const string REPEAT_TESTS_ON_COMPLETION_EDITOR_PREF = "TRAMP_AutomatedPlayer_repeatTestsOnCompletion";
	public static bool pullLatestFromGitOnCompletion = false;
	public const string PULL_LATEST_FROM_GIT_ON_COMPLETION_EDITOR_PREF = "TRAMP_AutomatedPlayer_pullLatestFromGitOnCompletion";
	public static string branchToPullOnCompletion;
	public const string BRANCH_TO_PULL_ON_COMPLETION_EDITOR_PREF = "TRAMP_AutomatedPlayer_branchToPullOnCompletion";
	public static bool shouldPlayInReverseOrder = false;
	public const string SHOULD_PLAY_IN_REVERSE_ORDER_EDITOR_PREF = "TRAMP_AutomatedPlayer_shouldPlayInReverseOrder";
	public static bool shouldSkipPorts = true;
	public const string SHOULD_SKIP_PORTS_EDITOR_PREF = "TRAMP_AutomatedPlayer_shouldSkipPorts";

	public static bool shouldCheckPaytableImages = true;
	private static bool checkingBonusGameForPaytableImages = false;

	public static bool shouldTestAutospins = true;
	public const string SHOULD_TEST_AUTOSPINS_EDITOR_PREF = "TRAMP_AutomatedPlayer_shouldTestAutospins";

	public static bool shouldSlamStopOnSpins = false;
	public const string SHOULD_SLAM_STOP_ON_SPINS_EDITOR_PREF = "TRAMP_AutomatedPlayer_shouldSlamStopOnSpins";
	public static int numberOfAutospins = 50;
	public static int currentNumberOfAutospinsToComplete = 0;

	public static SpinDirectionTypeEnum spinDirectionType = SpinDirectionTypeEnum.SPIN_BUTTON;
	public const string SPIN_DIRECTION_TYPE_EDITOR_PREF = "TRAMP_AutomatedPlayer_spinDirectionType";

	public enum SpinDirectionTypeEnum
	{
		SPIN_BUTTON = 0,
		RANDOM_DIRECTION,
		ALTERNATING_DIRECTION
	}

	private SlotReel.ESpinDirection prevDirectionSpun = SlotReel.ESpinDirection.Down; // tracks what the previous spin direciton was so it can be alternated

	public static bool shouldTestGiftedBonusSpins = true;
	public const string SHOULD_TEST_GIFTED_BONUS_SPINS_EDITOR_PREF = "TRAMP_AutomatedPlayer_shouldTestGiftedBonusSpins";

	private bool lobbyTriggeredGiftedBonus = false;

	// The index of the current instance that's running.
	public static int instanceIndex = 0;

	// The total number of TRAMP instances that are running.
	public static int totalNumInstances = 1;

	// Whether or not tramp is running in multiple instances.
	public static bool areMultipleInstances
	{
		get
		{
			return (totalNumInstances > 1);
		}
	}

	public static bool isLastInstance
	{
		get
		{
			return (instanceIndex == totalNumInstances-1);
		}
	}

	public static System.Guid SessionId
	{
		get;
		private set;
	}

	private TICoroutine sampleMemoryCoroutine;

	static AutomatedPlayer()
	{
		UnityEditor.EditorApplication.update += Update;
	}

	public static bool isAutomating
	{
		get;
		private set;
	}

	public static bool isRunningAutospins
	{
		get;
		private set;
	}

	public static void init()
	{
		if (!didInit)
		{

			// All command line arguments.
			Dictionary<string, string> commandLineArguments = CommandLineReader.GetCustomArguments(true);

			// Get the instance information from command line
			if (commandLineArguments.ContainsKey("instanceIndex"))
			{
				instanceIndex = int.Parse(commandLineArguments["instanceIndex"]);
				commandLineArguments.Remove("instanceIndex");
			}
			if (commandLineArguments.ContainsKey("numInstances"))
			{
				totalNumInstances = int.Parse(commandLineArguments["numInstances"]);
				commandLineArguments.Remove("numInstances");
			}
			if (commandLineArguments.ContainsKey("trampDirectory"))
			{
				string trampDir = commandLineArguments["trampDirectory"];
				commandLineArguments.Remove("trampDirectory");
				if (!string.IsNullOrEmpty(trampDir))
				{
					TRAMPLogFiles.TRAMP_DIRECTORY = trampDir;
				}
			}

			TRAMPLogFiles.logToOther("TRAMP> *** Starting at {0}", System.DateTime.Now); 

			// If needed, create an object for TRAMP to run on.
			if (instance == null)
			{
				GameObject go = new GameObject();
				go.name = "TRAMP - Automated Player";
				go.AddComponent<AutomatedPlayer>();
				DontDestroyOnLoad(go);
			}

			// Create LADI
			new AutomatedPlayerCompanion();
			instance.companion = AutomatedPlayerCompanion.instance;

			startedFromCommandLine = (PlayerPrefsCache.GetInt(DebugPrefs.STARTED_FROM_COMMAND_LINE, 0) != 0);

			if (startedFromCommandLine && !isAutomating)
			{				
				// Load the Test Plan
				if (!TRAMPLogFiles.loadCurrentTestPlan())
				{
					// Clear old files
					TRAMPLogFiles.resetResults(false);

					// New Session
					SessionId = System.Guid.NewGuid();

					// We need to make the test plan
					instance.companion.timeStarted = System.DateTime.Now;

					TRAMPSplunk.sessionStartedEvent(instance.companion);

					TRAMPLogFiles.logToOther("TRAMP> *** Starting from scratch at {0}", instance.companion.timeStarted);

					Debug.LogFormat("<color={0}>TRAMP> Started the tests at {1}, SessionId = {2}</color>",
						TRAMP_DEBUG_COLOR, instance.companion.timeStarted, SessionId.ToString());

					// Get the command line arguments
					foreach (KeyValuePair<string, string> kvp in commandLineArguments)
					{

						if (kvp.Key == "testFile")
						{
							instance.companion.testFile = kvp.Value;
						}
						else if (kvp.Key == "timeScale")
						{
							instance.companion.timeScale = float.Parse(kvp.Value);
						}
						else if (kvp.Key == "testMemory")
						{
							instance.companion.isTestMemory = bool.Parse(kvp.Value);
						}
						else if (kvp.Key == "branchName")
						{
							string kvpValue = kvp.Value;

							if (!string.IsNullOrEmpty(kvpValue))
							{
								instance.companion.branchName = kvpValue;
							}
						}
						else if (kvp.Key == "numberOfSpins")
						{
							instance.numberOfSpins = int.Parse(kvp.Value);
						}
						else if (kvp.Key == "testMiniGames")
						{
							instance.companion.shouldTestMiniGames = bool.Parse(kvp.Value);
						}
						else if (kvp.Key == "resetEditorPrefs")
						{
							bool isResetingEditorPrefs = bool.Parse(kvp.Value);
							if (isResetingEditorPrefs)
							{
								resetSavedPlayerSettingsFromEditorPrefsToDefault();
							}
						}
						else if (kvp.Key == "fbLogin")
						{
							TRAMPLogFiles.logToOther("TRAMP> using Facebook login user: {0} token: {1}", PlayerPrefs.GetString("EDITOR_FB_TOKEN_USERID"), PlayerPrefs.GetString("EDITOR_FB_TOKEN_TOKENSTRING"));
						}
						else
						{
							Debug.LogErrorFormat("<color={0}>TRAMP> Command line argument \"{1}\" is not valid.</color>", TRAMP_DEBUG_COLOR, kvp.Key);
						}
					}

					instance.initGamesToTest();
				}

				Time.timeScale = instance.companion.timeScale;
				SessionId = new System.Guid(instance.companion.sessionId);
				instance.playGamesFromTestPlan();
			}
			else
			{
				TRAMPLogFiles.logToOther("TRAMP> *** Starting from existing file at {0}", System.DateTime.Now);

				// Load the Test Plan
				if (!TRAMPLogFiles.loadCurrentTestPlan())
				{
					instance.companion.timeStarted = System.DateTime.Now;

					Debug.LogFormat("<color={0}>TRAMP> Started the tests at {1}</color>", TRAMP_DEBUG_COLOR, instance.companion.timeStarted);

					instance.companion.testFile = AUTOMATIC_TEST_FLAG;
					instance.discoverGamesToTest();			
				}

				Time.timeScale = instance.companion.timeScale;
				SessionId = new System.Guid(instance.companion.sessionId);
				instance.playGamesFromTestPlan();
			}

			didInit = true;
		}
	}

	public static void Update()
	{

		if (isAborting || isAutomating == false)
		{
			return;
		}

		if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
		{
			bool restartPlaying = true;	

			if ((instance != null && instance.companion != null && instance.companion.gamesToTest.Count <= 0) ||
				(instance != null && instance.companion != null && instance.companion.activeGame != null && !instance.companion.activeGame.isTesting))
			{
				restartPlaying = false;
			}


			if (restartPlaying)
			{
				UnityEditor.EditorApplication.isPlaying = true;

				Debug.LogFormat("<color={0}>TRAMP> Automatically forced Play.</color>", TRAMP_DEBUG_COLOR);
				TRAMPLogFiles.logToOther("TRAMP> Automatically forced Play.");
			}
		}

		if (UnityEditor.EditorApplication.isPaused && (instance == null || !instance.allowPause))
		{
			UnityEditor.EditorApplication.isPaused = false;
			Debug.LogFormat("<color={0}>TRAMP> Automatically forced an Unpause.</color>", TRAMP_DEBUG_COLOR);
		}

		checkForTimeout();

	}

	// Checks if a game was stalled or TRAMP testing is timing out. Should be called from Update().
	private static void checkForTimeout()
	{
		// Ensure TRAMP has actually been initialized.
		if (instance != null && instance.companion != null)
		{
			// If a game is currently testing, we need to check if the game is timing out.
			if (instance.companion.activeGame != null && instance.companion.activeGame.isTesting)
			{

				// Increment the amount of game test time, only if the editor isn't paused.
				if (!UnityEditor.EditorApplication.isPaused)
				{
					// Increase test time, ignoring time scale.
					instance.companion.activeGame.updateTestTime(Time.deltaTime / Time.timeScale);
				}

				// Check if a specific action is taking too long or if the whole game in general is taking too long, and stop game test if so.
				if (forceGameExitAfterMaxTime && (instance.companion.activeGame.isActionTakingTooLong() || instance.companion.activeGame.isGameTestTakingTooLong()))
				{

					string exceptionString = string.Format("TRAMP> Forcing game {0} to end due to timeout. Time Elapsed: {1}. Time Since Last Action: {2}",
						instance.companion.activeGame.commonGame.gameKey, instance.companion.activeGame.totalGameRuntime,
						instance.companion.activeGame.timeSinceLastAction);

					// Throw an exception to restart TRAMP
					throw new System.Exception(exceptionString);

				}
			}
			else
			{
				// TODO: As a new feature in the future, we should check if anything is timing out in the lobby too. This only works for games.
			}
		}	
	}

	private static void logHandler(string name, string stack, LogType type)
	{
		if (instance != null && isAutomating)
		{
			if (AutomatedPlayerCompanion.instance.activeGame != null)
			{
				string fullLogMessage = "";

				if (type != LogType.Exception)
				{
					switch (type)
					{
						case LogType.Log:
							fullLogMessage = "<LOG> " + name;
							break;
				
						case LogType.Warning:
							fullLogMessage = "<WARNING> " + name;
							break;
			
						case LogType.Error:
						
							fullLogMessage = "<ERROR> " + name;
							break;
					}

					instance.companion.addLog(type, fullLogMessage, stack);

				}
				else
				{
					fullLogMessage = "<EXCEPTION> " + name;
					instance.companion.addLog(type, fullLogMessage, stack);
					instance.handleException(fullLogMessage, stack, LogType.Exception);

					TRAMPSplunk.ForceEventsToServer();

				}
			}
			else
			{
				string stackString = "";
				if (type == LogType.Exception)
				{
					stackString = stack;
				}

				TRAMPLogFiles.logToOther("TRAMP> <{0}> {1}\n{2}", type.ToString().ToUpper(), name, stackString);
			}
		}
		else
		{
			TRAMPLogFiles.logToOther("TRAMP> <{0}> {1}{2}{3}", type, name, stack == "" ? "" : "\n", stack);
		}
	}

	private static void exceptionHandler(object sender, System.UnhandledExceptionEventArgs args)
	{
		string name = "unknown unhandled exception";
		string stack = "";
		
		if (args != null && args.ExceptionObject != null && args.ExceptionObject is System.Exception)
		{
			System.Exception e = args.ExceptionObject as System.Exception;
			name = e.GetType().FullName;
			stack = e.StackTrace.ToString();
		}
		
		logHandler(name, stack, LogType.Exception);
	}

	private void handleException(string condition, string stackTrace, LogType type)
	{
		AutomatedGameIteration activeGame = companion.activeGame;

		if (type == LogType.Exception)
		{
			// Count the exception for the Summary report
			incrementFatalErrorCount(activeGame.commonGame.gameKey);

			if (activeGame != null)
			{
				spinFinished();
				gameTestDone();
			}
			else
			{
				TRAMPLogFiles.logToOther("TRAMP> <EXCEPTION> {0}\n{1}", condition, stackTrace);
			}

			restartAutomation(false);
		}
	}

	public bool allowPause
	{
		get;
		set;
	}

	// Allows all of the editor pref saved values to be reset to default,
	// this will be helpful since all the values will also persist if the editor
	// is closed and reopened, so now there will be a way to load into the default
	// state, or to reset to it, instead of using the saved values
	private static void resetSavedPlayerSettingsFromEditorPrefsToDefault()
	{
		playRandomGamesInQueue = false;
		EditorPrefs.SetBool(PLAY_RANDOM_GAMES_IN_QUEUE_EDITOR_PREF, playRandomGamesInQueue);

		forceGameExitAfterMaxTime = true;
		EditorPrefs.SetBool(FORCE_GAME_EXIT_AFTER_MAX_TIME_EDITOR_PREF, forceGameExitAfterMaxTime);

		repeatTestsOnCompletion = false;
		EditorPrefs.SetBool(REPEAT_TESTS_ON_COMPLETION_EDITOR_PREF, repeatTestsOnCompletion);

		pullLatestFromGitOnCompletion = false;
		EditorPrefs.SetBool(PULL_LATEST_FROM_GIT_ON_COMPLETION_EDITOR_PREF, pullLatestFromGitOnCompletion);

		branchToPullOnCompletion = "";
		EditorPrefs.SetString(BRANCH_TO_PULL_ON_COMPLETION_EDITOR_PREF, branchToPullOnCompletion);

		shouldPlayInReverseOrder = false;
		EditorPrefs.SetBool(SHOULD_PLAY_IN_REVERSE_ORDER_EDITOR_PREF, shouldPlayInReverseOrder);

		shouldSkipPorts = true;
		EditorPrefs.SetBool(SHOULD_SKIP_PORTS_EDITOR_PREF, shouldSkipPorts);

		shouldTestAutospins = true;
		EditorPrefs.SetBool(SHOULD_TEST_AUTOSPINS_EDITOR_PREF, shouldTestAutospins);

		shouldSlamStopOnSpins = false;
		EditorPrefs.SetBool(SHOULD_SLAM_STOP_ON_SPINS_EDITOR_PREF, shouldSlamStopOnSpins);

		spinDirectionType = SpinDirectionTypeEnum.SPIN_BUTTON;
		EditorPrefs.SetInt(SPIN_DIRECTION_TYPE_EDITOR_PREF, (int)spinDirectionType);

		shouldTestGiftedBonusSpins = true;
		EditorPrefs.SetBool(SHOULD_TEST_GIFTED_BONUS_SPINS_EDITOR_PREF, shouldTestGiftedBonusSpins);
	}

	// Load settings stored out in editor prefs so they will persist between the
	// game being started and stopped.
	private static void loadSavedPlayerSettingsFromEditorPrefs()
	{
		playRandomGamesInQueue = EditorPrefs.GetBool(PLAY_RANDOM_GAMES_IN_QUEUE_EDITOR_PREF);
		forceGameExitAfterMaxTime = EditorPrefs.GetBool(FORCE_GAME_EXIT_AFTER_MAX_TIME_EDITOR_PREF);
		repeatTestsOnCompletion = EditorPrefs.GetBool(REPEAT_TESTS_ON_COMPLETION_EDITOR_PREF);
		pullLatestFromGitOnCompletion = EditorPrefs.GetBool(PULL_LATEST_FROM_GIT_ON_COMPLETION_EDITOR_PREF);
		branchToPullOnCompletion = EditorPrefs.GetString(BRANCH_TO_PULL_ON_COMPLETION_EDITOR_PREF);
		shouldPlayInReverseOrder = EditorPrefs.GetBool(SHOULD_PLAY_IN_REVERSE_ORDER_EDITOR_PREF);
		shouldSkipPorts = EditorPrefs.GetBool(SHOULD_SKIP_PORTS_EDITOR_PREF);
		shouldTestAutospins = EditorPrefs.GetBool(SHOULD_TEST_AUTOSPINS_EDITOR_PREF);
		shouldSlamStopOnSpins = EditorPrefs.GetBool(SHOULD_SLAM_STOP_ON_SPINS_EDITOR_PREF);
		spinDirectionType = (SpinDirectionTypeEnum)EditorPrefs.GetInt(SPIN_DIRECTION_TYPE_EDITOR_PREF);
		shouldTestGiftedBonusSpins = EditorPrefs.GetBool(SHOULD_TEST_GIFTED_BONUS_SPINS_EDITOR_PREF);
	}

	private void Awake()
	{
		instance = this;

		// Load saved out EditorPrefs values
		loadSavedPlayerSettingsFromEditorPrefs();

		System.AppDomain.CurrentDomain.UnhandledException += exceptionHandler;
		Application.logMessageReceived += logHandler;
		
		// This is created in the Startup scene, which needs to be persistent even during a game reset.
		try
		{
			DontDestroyOnLoad(gameObject);
		}
		catch(System.InvalidOperationException ex)
		{
			if(ex.Message == "The following game object is invoking the DontDestroyOnLoad method: TRAMP - Automated Player. Notice that DontDestroyOnLoad can only be used in play mode and, as such, cannot be part of an editor script.")
			{
				// Do nothing, it happens
			}
			else
			{
				throw ex;
			}
		}
	}

	public void startAutomation()
	{
		if (!isAutomating)
		{
			isAutomating = true;
			AutomatedPlayerCompanion.instance.updateBranchName();
			StartCoroutine(automateUpdate());
		}
		else
		{
			Debug.LogErrorFormat("<color={0}>TRAMP> AutomatedPlayer::startAutomation() - Already started automation!  Can't start again!</color>",
					TRAMP_DEBUG_COLOR);
		}
	}

	public static void endTest(bool startNewTest = false)
	{
		internalFlushAllLogs();
		TRAMPLogFiles.completeTestPlan();

		isAutomating = false;
		UnityEditor.EditorApplication.isPlaying = false;
		
		// If there are multiple instances, kill Unity at the end of test.
		if (areMultipleInstances || UnityEditorInternal.InternalEditorUtility.inBatchMode)
		{
			UnityEditor.EditorApplication.Exit(0);
		}
	}

	public void restartAutomation(bool forced)
	{
		isAutomating = true;

		gameTestDone(forced);

		if (startedFromCommandLine)
		{
			spinFinished();

			if (companion.activeGame != null &&
				companion.activeGame.remainingTestActions.Count > 0)
			{
				gameTestDone();
			}
		}

		internalFlushAllLogs();

		UnityEditor.EditorApplication.isPlaying = false;

		lastGameMode = GameMode.NONE;
		gameMode = GameMode.NONE;
	}	

	public void stopAutomation()
	{
		TRAMPLogFiles.logToOther("TRAMP> *** Stopping at {0}", System.DateTime.Now);

		if (startedFromCommandLine)
		{
			spinFinished();

			// If a game is currently being tested, save it's logs and stop the game test.
			if (companion != null && companion.activeGame != null &&
				companion.activeGame.remainingTestActions.Count <= 0)
			{
				gameTestDone();
			}
				
			TRAMPLogFiles.saveAllFiles();
			
			// Stop playmode.
			UnityEditor.EditorApplication.isPlaying = false;
		
			// If there are no more games to test, end the test and quit Unity.
			if (companion != null && companion.isCurrentTest && companion.gamesToTest.Count <= 0)
			{
				PlayerPrefsCache.SetInt(DebugPrefs.STARTED_FROM_COMMAND_LINE, 0);

				if (companion != null)
				{
					companion.timeEnded = System.DateTime.Now;
				}
				Debug.LogFormat("<color={0}>TRAMP> Completed the tests at {1}</color>", 
					TRAMP_DEBUG_COLOR, instance.companion.timeEnded);

				UnityEditor.EditorApplication.Exit(0);
			}
		}
		lastGameMode = GameMode.NONE;
		gameMode = GameMode.NONE;
		isAutomating = false;
	}

	// Abort the test, delete all files, and exit.
	public static void abort()
	{
		isAborting = true;
		isAutomating = false;
		UnityEditor.EditorApplication.isPaused = true;

		// Delete all files.
		TRAMPLogFiles.resetResults(false);

		if (instance)
		{
			instance.lastGameMode = GameMode.NONE;
			instance.gameMode = GameMode.NONE;
		}

		Debug.LogErrorFormat("<color={0}>TRAMP> TRAMP was <color=red>aborted</color> at {1:u}</color>", 
			TRAMP_DEBUG_COLOR, System.DateTime.Now);

		// Quit Unity.
		UnityEditor.EditorApplication.Exit(0);
	}

	public static void exit()
	{
		System.DateTime timeEnded = System.DateTime.Now;

		isAborting = true;
		isAutomating = false;

		if (instance != null)
		{
			if (instance.companion != null)
			{
				instance.companion.timeEnded = timeEnded;
			}

			instance.companion.endActiveGameLog(instance.gameMode);

			instance.lastGameMode = GameMode.NONE;
			instance.gameMode = GameMode.NONE;
		}

		Debug.LogErrorFormat("<color={0}>TRAMP> TRAMP was <color=red>exited</color> at {1:u}</color>", 
			TRAMP_DEBUG_COLOR, timeEnded);

		internalFlushAllLogs();

		UnityEditor.EditorApplication.Exit(0);
	}

	public static void flushAllLogs()
	{
		// Flushing is consider an error so that all the log info about the current spin is put into the Results.txt
		// It is assumed you are forcing the log because you are interested in somethnig that has happen, but
		// might not technically be an error.  Also because the summary will be forcing into TextSummary.txt but the 
		// tests will not be finished.
		Debug.LogErrorFormat("<color={0}>TRAMP> Forcing the logs to disk. Testing incomplete!</color>",
			TRAMP_DEBUG_COLOR);

		internalFlushAllLogs();
	}

	private static void internalFlushAllLogs()
	{
		if (instance != null && instance.companion != null && instance.companion.activeGame != null)
		{
			instance.companion.activeGame.print();
		}
			
		TRAMPLogFiles.saveAllFiles();
		TRAMPSplunk.ForceEventsToServer();
	}

	public void playGamesFromTextAsset(string path = "")
	{
		JSON jsonString = null;
		if (path != "")
		{
			try
			{			
				if (System.IO.File.Exists(path))
				{
					jsonString = new JSON(System.IO.File.ReadAllText(path));
				}
			}
			catch (System.Exception e)
			{
				Debug.LogErrorFormat("<color={0}>TRAMP> Can't playGamesFromTextAsset in file {1} because {2}</color>",
					TRAMP_DEBUG_COLOR, path, e);
			}
		}

		if (jsonString != null)
		{
			playGamesFromJSON(jsonString);
		}
	}

	public void playGamesFromJSON(JSON testJSON)
	{
		foreach (string gameKey in testJSON.getKeyList())
		{
			JSON[] testPlanActionList = testJSON.getJsonArray(gameKey);

			AutomatedGameIteration gameTest = companion.addNewGameToTest(gameKey);
			if (gameTest != null)
			{
				foreach (JSON action in testPlanActionList)
				{
					string actionName = action.getKeyList()[0];
					gameTest.addTestAction(actionName, action.getInt(actionName, 1));
				}
			}
		}

		instance.startAutomation();
	}

	public void playGamesFromTestPlan()
	{
		instance.startAutomation();
	}

	public void playNextGameInQueue()
	{
		if (companion.gamesToTest.Count > 0)
		{
			int gameToTestIndex = 0;

			if (playRandomGamesInQueue)
			{
				gameToTestIndex = Random.Range(0, companion.gamesToTest.Count);
			}
			else if (shouldPlayInReverseOrder)
			{
				gameToTestIndex = companion.gamesToTest.Count - gameToTestIndex - 1; // Last element in the list.
			}

			// Play the next game.
			AutomatedPlayerCompanion.instance.startNextGameLog(gameToTestIndex);

			// Save in case of crash
			TRAMPLogFiles.saveAllFiles();
		}
		else
		{
			if (repeatTestsOnCompletion)
			{
				initGamesToTest();
			}
			else
			{
				endTest();
				if (pullLatestFromGitOnCompletion)
				{
					AutomatedPlayerProcesses.checkoutAndRestartUnity(branchToPullOnCompletion);
				}
			}
		}
	}

	public void initGamesToTest()
	{
		if (companion.testFile == AUTOMATIC_TEST_FLAG)
		{
			instance.discoverGamesToTest();
		}
		else
		{
			if (string.IsNullOrEmpty(companion.testFile))
			{
				Debug.LogErrorFormat("<color={0}>TRAMP> Can't run any tests because the testFile path is null or empty.</color>",
					TRAMP_DEBUG_COLOR);
				abort();
			}
			else
			{
				Time.timeScale = instance.companion.timeScale;
				instance.playGamesFromTextAsset(instance.companion.testFile);
				didInit = true;
				return;
			}
		}
	}

	public void automateStop()
	{
		StartCoroutine(SpinPanel.instance.automateStop());
	}

	public void gameTestDone(bool forced = false)
	{
		if (isRunningAutospins)
		{
			isRunningAutospins = false;
			// The game will continue to run autospins unless it runs out of spins so we have slam that stop button      
			automateStop();
		}

		TRAMPSplunk.gameTestEndEvent();

		companion.endActiveGameLog(gameMode, forced);

		// Remove any remaining tests, we are forcing it to be done.
		if (sampleMemoryCoroutine != null)
		{
			StopCoroutine(sampleMemoryCoroutine);
			sampleMemoryCoroutine = null;
		}

		TRAMPLogFiles.saveAllFiles();
	}

	private IEnumerator automateUpdate()
	{
		while (isAutomating) // Only do the automation while the DevGUI isn't active.
		{
			// We don't want to automate when the devGUI can block input.
			if (!DevGUI.isActive)
			{
				
				lastGameMode = gameMode;
				setGameMode();
				switch (gameMode)
				{
					case GameMode.BASE_GAME:
						yield return StartCoroutine(handleBaseGameAutomation());
						break;
					case GameMode.BONUS_GAME:
						yield return StartCoroutine(handleBonusGameAutomation());
						break;
					case GameMode.LOADING:
						yield return StartCoroutine(handleLoadingAutomation());
						break;
					case GameMode.LOBBY:
						yield return StartCoroutine(handleLobbyAutomation());
						break;
					case GameMode.NONE:
						yield return StartCoroutine(handleFreakOutAutomation());
						break;
				}
			}
			else
			{
				// Make sure we wait a frame or we will infinte loop.
				yield return null;
			}
		}

		Debug.LogFormat("<color={0}>TRAMP> Stopping Automation.</color>", TRAMP_DEBUG_COLOR);
	}

	private IEnumerator handleBaseGameAutomation()
	{
		if (loadingGame)
		{
			// If the script is loading into the game then we should just wait for that coroutine to finish.
			yield break;
		}

		// Check and see if there are any dialogs etc. open and clear them
		// TODO Add visualizer to this coroutine.
		yield return StartCoroutine(CommonAutomation.automateClearDialogsAndShrouds());

		if (BonusGameManager.instance != null && BonusGamePresenter.instance != null)
		{
			// Handle portals in the base game.
			// TODO Add visualizer to this coroutine.
			yield return StartCoroutine(CommonAutomation.clickRandomColliderIn(BonusGamePresenter.instance.gameObject));
		}

		if (ReelGame.activeGame is SlotBaseGame)
		{
			SlotBaseGame baseGame = ReelGame.activeGame as SlotBaseGame;
			if (baseGame != null && !baseGame.isGameBusy && SpinPanel.instance.isButtonsEnabled)
			{
				// If the game seems to be ready to spin, but WebGL input is disabled, their may
				// be a game related bet selector active like in elvis03, so try and force the game
				// to click something on the game
				if (baseGame.isModuleBlockingWebGLKeyboardInputForSlotGame())
				{
					yield return StartCoroutine(CommonAutomation.clickRandomColliderIn(baseGame.gameObject));
				}
			
				// The game is ready to be spun, are there tests left?
				if (companion.activeGame != null &&
					companion.activeGame.remainingTestActions.Count > 0)
				{
					SpinPanel spinPanel = SpinPanel.instance;
					if (spinPanel != null)
					{
						// pick a random wager amount
						// TODO Add visualizer to this coroutine.
						yield return StartCoroutine(spinPanel.automateChangeWager());

						// If our next action is going to be an autospins action, we should make sure
						// TRAMP has enough money to run the whole action.
						// NOTE: 	This section will mess with the RTP.
						string nextAction = companion.activeGame.peekNextAction();
						int multiplier = AutomatedTestAction.getSpinCountFromAction(nextAction);

						// Check and see if we need to add money so the player can spin
						if ((spinPanel.betAmount * multiplier) > SlotsPlayer.creditAmount)
						{
							long firstDifference = spinPanel.betAmount - SlotsPlayer.creditAmount;
							
							// firstDifference [will account for first spin]
							// +
							// spinPanel.betAmount * (multiplier - 1) [will account for all remaining spins]
							// 	This section will cancel out to 0 if multiplier is 1.
							long finalDifference = (spinPanel.betAmount * (multiplier - 1)) + firstDifference;

							PlayerAction.addCredits(finalDifference);
							SlotsPlayer.addCredits(finalDifference, "TRAMP");

							Debug.LogFormat("<color={0}>TRAMP> Adding difference of {1} credits to player for action: {2}.</color>",
								TRAMP_DEBUG_COLOR, finalDifference, nextAction);

							// Wait a bit for the added credits to sync with the server before continuing.
							yield return StartCoroutine(ServerAction.waitForActionBatch());
						}

						// Now that we've added the money lets spin
						// TODO Add visualizer to this coroutine.
						yield return StartCoroutine(spinBaseGame());
					}
				}
				else
				{
					// The automatedBaseGameInfo class/game is done.
					gameTestDone();

					if (companion.gamesToTest.Count <= 0)
					{
						if (repeatTestsOnCompletion)
						{
							initGamesToTest();
						}
						else
						{
							endTest();

							if (pullLatestFromGitOnCompletion)
							{
								AutomatedPlayerProcesses.checkoutAndRestartUnity(branchToPullOnCompletion);
							}
						}
					}
					else if (GameState.game.keyName == companion.gamesToTest[0].Key && companion.gamesToTest[0].Value.actionsTested.Count > 0)
					{
						//Don't return to the lobby if we're already loaded into the next game we need to test and the game was already in the middle of a test when we left it.
						//This would happen after finished a bonus game triggered by an outside source, such as some feature.
						playNextGameInQueue();
					}
					else if (Overlay.instance != null && Overlay.instance.top != null)
					{
						// TODO Add visualizer to this coroutine.
						Overlay.instance.top.clickLobbyButton();		
					}
					else
					{
						Glb.loadLobby();
					}

					// Give the stop/lobby load a chance to kick in
					yield return null;

					yield break;
				}
			}
			else
			{
				// portals need custom handling since they occur in the base game but have clickable objects located under the Bonus Game Panel
				if (baseGame != null)
				{
					PortalScript portalScript = baseGame.GetComponent<PortalScript>();
					if (portalScript != null && BonusGameManager.instance != null)
					{
						// detected that this game has a portal script, so we need to check if we should be clicking the portal script banners
						yield return StartCoroutine(CommonAutomation.clickRandomColliderIn(BonusGameManager.instance.gameObject));
					}
				}

				// Some modules tend to block TRAMP and need to be checked in special cases.
				List<SlotModule> modules = ReelGame.activeGame.cachedAttachedSlotModules;
				foreach (SlotModule module in modules)
				{
					// Bonus pools confuse TRAMP because they aren't bonus games, yet they require random clicks.
					if (module is ReelGameBonusPoolsModule)
					{
						// Grab the bonus pools component to check if TRAMP needs to do anything special. 
						ReelGameBonusPoolsModule bonusPoolsModule = module as ReelGameBonusPoolsModule;
						GameObject bonusPoolsParent = bonusPoolsModule.bonusPoolComponent.gameObject;

						// If the bonus pools are active, start clicking randomly.
						if (bonusPoolsParent.activeSelf)
						{						
							// We have to pass in a special camera here since Bonus Pools don't have a camera in any parent. 
							yield return StartCoroutine(CommonAutomation.clickRandomColliderIn(bonusPoolsParent, bonusPoolsModule.bonusPoolsCamera));		 

						}
					}
				}
			}
		}
	}

	private IEnumerator checkForMissingBonusPaytableImages()
	{
		// See if paytable image exists.
		if (BonusGameManager.instance != null && !string.IsNullOrEmpty(BonusGameManager.instance.currentGameKey))
		{
			// Figure out current bonus game key name.
			string bonusGameKey = "";
			GameState.BonusGameNameData bonusGameNameData = GameState.bonusGameNameData;
			string bonusGameType = BonusGameManager.instance.currentGameType;

			Debug.LogFormat("<color={0}>Checking for missing paytable images, type {1}</color>", TRAMP_DEBUG_COLOR, bonusGameType);
			// Can skip "portal" type since we've already gotten past that.
			if (bonusGameType == "gifting")
			{
				for (int i = 0; i < bonusGameNameData.giftingBonusGameNames.Count; i++)
				{
					string name = bonusGameNameData.giftingBonusGameNames[i];
					SlotOutcome outcome = SlotOutcome.getBonusGameOutcome(BonusGameManager.currentBonusGameOutcome, name);
					if (outcome != null)
					{
						bonusGameKey = name;
						break;
					}
				}
			}
			else if (bonusGameType == "challenge")
			{
				for (int i = 0; i < bonusGameNameData.challengeBonusGameNames.Count; i++)
				{
					string name = bonusGameNameData.challengeBonusGameNames[i];
					SlotOutcome outcome = SlotOutcome.getBonusGameOutcome(BonusGameManager.currentBonusGameOutcome, name);
					if (outcome != null)
					{
						bonusGameKey = name;
						break;
					}
				}
			}
			else if (bonusGameType == "credit")
			{
				for (int i = 0; i < bonusGameNameData.creditBonusGameNames.Count; i++)
				{
					string name = bonusGameNameData.creditBonusGameNames[i];
					SlotOutcome outcome = SlotOutcome.getBonusGameOutcome(BonusGameManager.currentBonusGameOutcome, name);
					if (outcome != null)
					{
						bonusGameKey = name;
						break;
					}
				}
			}
			else if (bonusGameType == "scatter")
			{
				for (int i = 0; i < bonusGameNameData.scatterPickGameBonusGameNames.Count; i++)
				{
					string name = bonusGameNameData.scatterPickGameBonusGameNames[i];
					SlotOutcome outcome = SlotOutcome.getBonusGameOutcome(BonusGameManager.currentBonusGameOutcome, name);
					if (outcome != null)
					{
						bonusGameKey = name;
						break;
					}
				}
			}
			else
			{
				Debug.LogFormat("<color={0}>Unknown bonus game type {1}</color>", TRAMP_DEBUG_COLOR, bonusGameType);
			}

			if (string.IsNullOrEmpty(bonusGameKey))
			{
				Debug.LogFormat("<color={0}>Could not find bonus game key</color>", TRAMP_DEBUG_COLOR);
				yield break;
			}

			// Wait for a bit so that intro animations get a chance to finish, we want the screenshot to be of actual
			// gameplay.
			yield return new WaitForSeconds(15.0f);

			Debug.LogFormat("<color={0}>Checking for missing paytable images for {1}</color>", TRAMP_DEBUG_COLOR, bonusGameKey);
			BonusGame bonusGameData = BonusGame.find(bonusGameKey);
			if (bonusGameData != null)
			{
				if (string.IsNullOrEmpty(bonusGameData.paytableImage))
				{
					Debug.LogFormat("<color={0}>No paytable image defined, skipping file check.</color>", TRAMP_DEBUG_COLOR);
					yield break;
				}

				// NB: "bonusGameData.paytableImage" data returns a URL designed for the web version of the game.  The code
				// below is adapted from PaytableBonus.init and SlotResourceMap.createPaytableImage to replicate the
				// search process used by the paytable dialog to find the paytable image asset paths.

				// Adapted from PaytableBonus.init to convert .jpg/.png URL to <name>_paytable
				string imageBaseName = PaytableBonus.getPaytableBonusImageBasename(bonusGameData);
				Debug.LogFormat("<color={0}>paytable image: {1}</color>", TRAMP_DEBUG_COLOR, imageBaseName);

				SlotResourceData entry = SlotResourceMap.getData(companion.activeGame.commonGame.gameKey);
				if (entry != null)
				{
					// Adapted from SlotResourceMap.createPaytableImage to generate standard game-specific and
					// game-group-specific paths within Assets/Data, and then convert those to absolute paths for
					// checking file existence and screenshot save target filename.
					string dataPath = System.IO.Path.Combine(Application.dataPath, "Data"); // get full path to Assets/Data
					// Get full game-specific path to file in Assets/Data/Games/<game group>/<gamekey>/Images
					string basicImagePath = System.IO.Path.Combine(dataPath, entry.getGameSpecificImagePath(imageBaseName) + ".png");
					// Get full game-group-specific path to file in Assets/Data/Games/<game group>/<game group>_common/Images
					string commonImagePath = System.IO.Path.Combine(dataPath, entry.getGroupSpecificImagePath(imageBaseName) + ".png");
					Debug.LogFormat("<color={0}>Check for {1} and {2}</color>", TRAMP_DEBUG_COLOR, basicImagePath, commonImagePath);
					if (!(System.IO.File.Exists(basicImagePath) || System.IO.File.Exists(commonImagePath)))
					{
						string basicImagePathJPG = System.IO.Path.Combine(dataPath, entry.getGameSpecificImagePath(imageBaseName) + ".jpg");
						string commonImagePathJPG = System.IO.Path.Combine(dataPath, entry.getGroupSpecificImagePath(imageBaseName) + ".jpg");
						if (!(System.IO.File.Exists(basicImagePathJPG) || System.IO.File.Exists(commonImagePathJPG)))
						{
							Debug.LogWarning("Paytable image missing at: " + basicImagePath + " and " + commonImagePath);
#if UNITY_EDITOR
							yield return StartCoroutine(capturePaytableScreenshot(basicImagePath));
#endif
						}
						else
						{
							Debug.LogWarning("Paytable image is using JPG at: " + basicImagePathJPG + " and " + commonImagePathJPG);
						}
					}
				}
			}
			else
			{
				Debug.LogFormat("<color={0}>No bonus game data found for {1}</color>", TRAMP_DEBUG_COLOR, bonusGameKey);
			}
		}
	}

#if UNITY_EDITOR
	public static Vector2 GetMainGameViewSize()
	{
		System.Type T = System.Type.GetType("UnityEditor.GameView,UnityEditor");
		System.Reflection.MethodInfo GetSizeOfMainGameView = T.GetMethod("GetSizeOfMainGameView",System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
		System.Object Res = GetSizeOfMainGameView.Invoke(null,null);
		return (Vector2)Res;
	}

	public static UnityEditor.EditorWindow GetMainGameView()
	{
		System.Type T = System.Type.GetType("UnityEditor.GameView,UnityEditor");
		System.Reflection.MethodInfo GetMainGameView = T.GetMethod("GetMainGameView",System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
		System.Object Res = GetMainGameView.Invoke(null,null);
		return (UnityEditor.EditorWindow)Res;
	}

	public static void changeGameViewSize(int width, int height, bool useExtraY)
	{
		int extraY = 17; // Extra gizmos on the top of game view;
		Rect R = GetMainGameView().position;
		R.width = width;
		R.height = height;
		if (useExtraY)
		{
			R.height += extraY;
		}
		GetMainGameView().position = R;
	}

	private IEnumerator capturePaytableScreenshot(string saveScreenshotImagePath)
	{
		if (!string.IsNullOrEmpty(saveScreenshotImagePath))
		{
			// Hide the visualizer panel so it doesn't obscure the screenshot.
			if (AutomatedCompanionVisualizer.instance != null)
			{
				AutomatedCompanionVisualizer.instance.hide();
			}
			yield return null;	// Wait a frame
			Debug.LogFormat("<color={0}>CAPTURE screenshot to {1}</color>", TRAMP_DEBUG_COLOR, saveScreenshotImagePath);

			Debug.LogFormat("<color={0}>Changing screen size for screen shot. {1}</color>", TRAMP_DEBUG_COLOR, saveScreenshotImagePath);
			Vector2 oldSize = GetMainGameViewSize();
			changeGameViewSize(1024, 768, true);
			yield return null; // 2 frames for NGUI to catch up.
			yield return null;
			ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath, saveScreenshotImagePath));
			// Wait for screenshot save to finish, it is asynchronous with no completion hooks.
			while (!System.IO.File.Exists(saveScreenshotImagePath))
			{
				Debug.Log("Waiting for the screenshot to be saved to " + saveScreenshotImagePath);
				yield return null;
			}
			Debug.LogFormat("<color={0}>revert screen size from screen shot. {1}</color>", TRAMP_DEBUG_COLOR, saveScreenshotImagePath);
			changeGameViewSize((int)oldSize.x, (int)oldSize.y, false);
			TextureImporter textureImporter = null;
			string textureImporterPath = "Assets" + saveScreenshotImagePath.Replace(Application.dataPath, "");
			AssetDatabase.ImportAsset(textureImporterPath, ImportAssetOptions.ForceUpdate);
			while (textureImporter == null)
			{
				Debug.Log("Waiting for textureImporter to not be null from path " + textureImporterPath);
				textureImporter = TextureImporter.GetAtPath(textureImporterPath) as TextureImporter;
				yield return null;
			}
			setTextureImporterOverrides(
				textureImporter,
				"iPhone",
				512,
				TextureImporterFormat.PVRTC_RGB4,
				100,
				false);
			setTextureImporterOverrides(
				textureImporter,
				"Android",
				512,
				TextureImporterFormat.ETC_RGB4,
				50,
				false);
			setTextureImporterOverrides(
				textureImporter,
				"default",
				512,
				TextureImporterFormat.Automatic,
				50,
				false);

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

			// Show the visualizer panel again.
			if (AutomatedCompanionVisualizer.instance != null)
			{
				AutomatedCompanionVisualizer.instance.show();
			}
		}
	}
#endif

	public static void setTextureImporterOverrides(TextureImporter importer, string platform, int maxTextureSize, TextureImporterFormat textureFormat, int quality, bool allowsAlphaSplit)
	{
		// gets the existing platform settinga
		TextureImporterPlatformSettings settings = null;
		if (platform != "default")
		{
			settings = importer.GetPlatformTextureSettings(platform);
		}
		else
		{
			settings = importer.GetDefaultPlatformTextureSettings();
		}

		// set our desired overrides
		settings.overridden = true;
		settings.maxTextureSize = maxTextureSize;
		settings.format = textureFormat;
		settings.compressionQuality = quality;
		settings.allowsAlphaSplitting = allowsAlphaSplit;

		// Set them back to the importer
		importer.SetPlatformTextureSettings(settings);
		importer.SaveAndReimport();
	}

	// TODO Add visualizer to this coroutine.
	private IEnumerator handleBonusGameAutomation()
	{
		if (lastGameMode == GameMode.BASE_GAME)
		{
			// Count the bonus game we just entered.
			if (BonusGameManager.instance != null)
			{
				if (companion.activeGame != null)
				{
					companion.activeGame.stats.countBonusGame(BonusGameManager.instance.summaryScreenGameName);
				}
			}
			else
			{
				Debug.LogErrorFormat("<color={0}>TRAMP> Trying to handle BonusGameAutomation, but there is no BonusGameManager!</color>",
					TRAMP_DEBUG_COLOR);
			}

			if (shouldCheckPaytableImages)
			{
				// Turn on check for bonus paytable images on this run.
				checkingBonusGameForPaytableImages = true;
			}
		}
		else if (lastGameMode == GameMode.LOADING && GameState.giftedBonus != null)
		{
			// Entering a gifted bonus game from the lobby.
			if (companion.activeGame == null)
			{
				string gameKey = BonusGameManager.instance.currentGameKey;
				companion.continueLoggingExistingGame(gameKey);

				if (companion.activeGame == null)
				{
					Debug.LogErrorFormat("<color={0}>TRAMP> Failed to set companion.activeGame!</color>", AutomatedPlayer.TRAMP_DEBUG_COLOR);
				}
				else
				{
					// Count the bonus game we just entered.
					if (BonusGameManager.instance != null)
					{
						companion.activeGame.stats.countBonusGame(BonusGameManager.instance.summaryScreenGameName);
					}
				}
			}
		}

		// TODO Add visualizer to this coroutine.
		yield return StartCoroutine(CommonAutomation.automateOpenDialog());

		// Check and see if there are any clickable UIButtons
		if (BonusGameManager.instance != null)
		{
			// if BonusGamePresenter is null that probably means the bonus game was terminated 
			// and we'll leave this state the next time setGameMode() is called
			if (BonusGamePresenter.instance != null)
			{
				// Handle portals in the base game.
				// TODO Add visualizer to this coroutine.
				yield return StartCoroutine(CommonAutomation.clickRandomColliderIn(BonusGamePresenter.instance.gameObject));

				// Check for missing bonus paytable images
				if (BonusGameManager.isBonusGameActive && BonusGameManager.instance.currentGameType != "portal"
					&& checkingBonusGameForPaytableImages == true)
				{
					checkingBonusGameForPaytableImages = false;
					StartCoroutine(checkForMissingBonusPaytableImages());
				}
			}
		}
		else
		{
			Debug.LogErrorFormat("<color={0}>TRAMP> There isn't a BonusGameManager!</color>",
					TRAMP_DEBUG_COLOR);
		}
	}

	// TODO Add visualizer to this coroutine.
	private IEnumerator spinBaseGame()
	{
		// TODO Add visualizer to this coroutine.
		Collider spinButton = CommonAutomation.getSpinButton();
		if (spinButton != null)
		{
			if (spinButton.gameObject == null)
			{
				throw new System.Exception("When spinning the BaseGame, somehow the spinButton's gameobject is null!");
			}

			if (ReelGame.activeGame != null && ReelGame.activeGame is SlotBaseGame && companion.activeGame != null)
			{
				long betAmount = 0;
				if (SpinPanel.instance != null)
				{
					betAmount = SpinPanel.instance.betAmount;
				}
				else
				{
					Debug.LogWarningFormat("<color={0}>TRAMP> No spin pannel was set, can't get bet amount.</color>",
						TRAMP_DEBUG_COLOR);
				}

				string actionName = companion.activeGame.popNextAction();

				AutomatedTestAction nextAction = new AutomatedTestAction(actionName);

				isRunningAutospins = false;

				if (nextAction != null)
				{
					switch (nextAction.action)
					{
						case AutomatedTestAction.Action.SPIN:
							AutomatedPlayerCompanion.instance.activeGame.countSpin(betAmount, false);
							// ~~~~~~~~~~~ NOTE THE RANDOMNESS ~~~~~~~~~~~
							// Check the spin mode and control how the spin is started based on that
							List<SwipeableReel> swipeableReelList = ReelGame.activeGame.engine.getAllSwipeableReels();
							bool isSwipeSpinning = false;
							SlotReel.ESpinDirection spinDirection = SlotReel.ESpinDirection.Down;

							// if there are no swipeable reels then we can't swipe to spin so just use the button
							// (i.e. plop/tumble games)
							if (swipeableReelList.Count > 0)
							{
								switch (spinDirectionType)
								{
									case SpinDirectionTypeEnum.ALTERNATING_DIRECTION:
									{
										isSwipeSpinning = true;

										if (prevDirectionSpun == SlotReel.ESpinDirection.Down)
										{
											spinDirection = SlotReel.ESpinDirection.Up;
										}
										else
										{
											spinDirection = SlotReel.ESpinDirection.Down;
										}
										break;
									}

									case SpinDirectionTypeEnum.RANDOM_DIRECTION:
									{
										isSwipeSpinning = true;
										spinDirection = (SlotReel.ESpinDirection)Random.Range(0, 2);
										break;
									}
								}
							}

							if (isSwipeSpinning)
							{
								Debug.Log("AutomatedPlayer.spinBaseGame() - swipe Spin triggered! spinDirection = " + spinDirection);
								if (spinDirection == SlotReel.ESpinDirection.Down)
								{
									// spinning down
									swipeableReelList[0].simulateReelSwipe(0.5f);
								}
								else
								{
									// spinning up
									swipeableReelList[0].simulateReelSwipe(-0.5f);
								}
							}
							else
							{
								Debug.Log("AutomatedPlayer.spinBaseGame() - spinButton Spin triggered!");
								yield return StartCoroutine(CommonAutomation.clickRandomColliderIn(spinButton.gameObject));
							}

							// track what the last spin direction used was
							prevDirectionSpun = spinDirection;
							break;

						case AutomatedTestAction.Action.AUTOSPIN_10:
							isRunningAutospins = true;
							currentNumberOfAutospinsToComplete = 10;
							yield return StartCoroutine(SpinPanel.instance.automateAutoSpin(10));
							break;

						case AutomatedTestAction.Action.AUTOSPIN_25:
							isRunningAutospins = true;
							currentNumberOfAutospinsToComplete = 25;
							yield return StartCoroutine(SpinPanel.instance.automateAutoSpin(25));
							break;

						case AutomatedTestAction.Action.AUTOSPIN_50:
							isRunningAutospins = true;
							currentNumberOfAutospinsToComplete = 50;
							yield return StartCoroutine(SpinPanel.instance.automateAutoSpin(50));
							break;

						case AutomatedTestAction.Action.AUTOSPIN_100:
							isRunningAutospins = true;
							currentNumberOfAutospinsToComplete = 100;
							yield return StartCoroutine(SpinPanel.instance.automateAutoSpin(100));
							break;

						case AutomatedTestAction.Action.KEY_PRESS:
							AutomatedPlayerCompanion.instance.activeGame.countSpin(betAmount, true);
							// ~~~~~~~~~~~ NOTE THE RANDOMNESS ~~~~~~~~~~~
							yield return instance.StartCoroutine(Input.simulateKeyPress(nextAction.actionName));
							break;

						case AutomatedTestAction.Action.DESYNC_CHECK:
							AutomatedPlayerCompanion.instance.activeGame.countSpin(betAmount, false);
							// check if we were desync checking and got an outcome that contained a regular pay out
							if (ReelGame.activeGame.outcomeDisplayController.getNumLoopedOutcomes() > 0 && lastActionPerformed != null && lastActionPerformed.action == AutomatedTestAction.Action.DESYNC_CHECK)
							{
								nextAction.isLastSpin = true;
							}
							// ~~~~~~~~~~~ NOTE THE RANDOMNESS ~~~~~~~~~~~
							yield return StartCoroutine(CommonAutomation.clickRandomColliderIn(spinButton.gameObject));
							break;

						case AutomatedTestAction.Action.FEATURE_MINI_GAME_CHECK:
							AutomatedPlayerCompanion.instance.activeGame.countSpin(betAmount, false);
							// ~~~~~~~~~~~ NOTE THE RANDOMNESS ~~~~~~~~~~~
							yield return StartCoroutine(CommonAutomation.clickRandomColliderIn(spinButton.gameObject));
							break;

						case AutomatedTestAction.Action.FORCE_GIFTED_GAME:
							sendFakeGiftGameToSelf();
							break;

						case AutomatedTestAction.Action.PLAY_GIFTED_GAME:
							//Debug.LogFormat("<color={0}>TRAMP> Opening Inbox Dialog</color>", TRAMP_DEBUG_COLOR);
							//InboxDialog.showDialog(InboxDialog.Tab.SPINS);
							//yield return StartCoroutine(CommonAutomation.waitForDialog("inbox"));
							break;

						default:
							Debug.LogErrorFormat("<color={0}>TRAMP> Not sure how to handle this action</color>",
								TRAMP_DEBUG_COLOR);
							break;
					}

					lastActionPerformed = nextAction;
					companion.activeGame.saveAction(lastActionPerformed);
				}
				else
				{
					Debug.LogErrorFormat("<color={0}>TRAMP> Trying to spin the base game but there is no next action!</color>",
						TRAMP_DEBUG_COLOR); 
					gameTestDone();
				}
			}
			else
			{
				Debug.LogErrorFormat("<color={0}>TRAMP> Trying to spin the base game but ReelGame.active is {1}</color>",
					TRAMP_DEBUG_COLOR, ReelGame.activeGame);
				gameTestDone();
			}
		}
	}

	private IEnumerator handleLoadingAutomation()
	{
		// Give the load a chance to start
		yield return null;

		// Wait while it is loading
		while (Loading.isLoading)
		{
			yield return null;
		}
	}

	// TODO Add visualizer to this coroutine.
	private IEnumerator handleLobbyAutomation()
	{
		// Give any dialogs a chance to popup
		yield return new WaitForSeconds(1.0f);
		// Check and see if there are any dialogs etc. open and clear them
		// TODO Add visualizer to this coroutine.

		yield return StartCoroutine(CommonAutomation.automateClearDialogsAndShrouds());

		if (AutomatedPlayer.shouldTestGiftedBonusSpins)
		{
			//Debug.LogFormat("<color={0}>TRAMP> handleLobbyAutomation: Opening Inbox Dialog</color>", TRAMP_DEBUG_COLOR);
			//InboxDialog.showDialog(InboxDialog.Tab.SPINS);
			//yield return StartCoroutine(CommonAutomation.waitForDialog("inbox"));
			//if (InboxDialog.didLaunchBonusGame)
			//{
				// Make note that lobby triggered a gifted bonus game.
			//	lobbyTriggeredGiftedBonus = true;
			//	yield break;
			//}
		}

		if (lobbyTriggeredGiftedBonus)
		{
			// Back from gifted bonus, clear companion's active game (which was set to the gifted game temporarily).
			lobbyTriggeredGiftedBonus = false;
			companion.endActiveGameLog(GameMode.BONUS_GAME);
		}

		// Queue up the next game if necessary
		if (companion.activeGame == null)
		{
			playNextGameInQueue();
		}

		if (companion.activeGame != null)
		{
			// TODO Add visualizer to this coroutine.
			yield return StartCoroutine(playGame(companion.activeGame));
		}
	}

	// TODO Add visualizer to this coroutine.
	private IEnumerator handleFreakOutAutomation()
	{
		if (lastGameMode == GameMode.NONE)
		{
			Debug.LogErrorFormat("<color={0}>TRAMP> handleFreakOutAutomation() called twice in a row, will try resetting.</color>",
				TRAMP_DEBUG_COLOR);

			// Try resetting
			Glb.resetGame("TRAMP> handleFreakOutAutomation() called twice in a row");
		}
		else
		{
			Debug.LogErrorFormat("<color={0}>TRAMP> No idea what to automate in this state, will try to clear dialogs.</color>",
				TRAMP_DEBUG_COLOR);

			// Give any dialogs a chance to popup
			yield return new WaitForSeconds(1.0f);

			// Check and see if there are any dialogs etc. open and clear them
			// TODO Add visualizer to this coroutine.
			yield return StartCoroutine(CommonAutomation.automateClearDialogsAndShrouds());
		}
	}

	// This adds games to the gameTestPlan.testActions automatically with games
	// it finds, sets there tests to automatic, and then sorts the list alphabetically
	private void discoverGamesToTest()
	{		
		// Ludington: Note that you can't use LobbyGame.getAll() to discover games because 
		// it returns all games regaurdless of SKU, and TRAMP can't make sense of which 
		// games are valid.

		// Important to get valid keys first for multiple TRAMP instances.
		List<string> validKeys = new List<string>();
 
		// Find Games in the slot_resource_map.txt file, and add them to valid keys list.
		foreach (string gameKey in SlotResourceMap.map.Keys)
		{
			// Don't add the game to the test plan if theres no basegame to load into
			if (!string.IsNullOrEmpty(SlotResourceMap.map[gameKey].slotPrefabPath))
			{
				validKeys.Add(gameKey);
			}
		}

		// The total number of games this instance should test. Can't be less than one.
		int numGamesToTest = Mathf.Max(1, validKeys.Count / totalNumInstances);

		// Start at the game assigned to this TRAMP instance.
		int startIndex = instanceIndex * numGamesToTest;

		// If the start index is beyond the range of valid games, just end immediately.
		if (startIndex > validKeys.Count)
		{
			endTest();
			return;
		}

		// End before the last index, depending on how many there instances there are. Can't be more than validkeys count.
		int endIndex = Mathf.Min(startIndex + numGamesToTest, validKeys.Count);

		// If this is the last TRAMP instance, test all remaining games. Needed for odd numbers of games.
		if (instanceIndex == (totalNumInstances-1))
		{
			endIndex = validKeys.Count;
		}

		// Iterate through all valid games, and add them to the test plan.
		for (int i = startIndex; i < endIndex; i++)
		{
            addGameKey();
            bool checkgame = checkGameAlreadyAdded(validKeys[i].ToString());
            if (!checkgame) {
                companion.addNewGameToTest(validKeys[i]);
            }
		}

		// Sort the keys
		companion.gamesToTest.Sort((a, b) => a.Key.CompareTo(b.Key));
	}

    // Adding game key to the list
    private void addGameKey() {
        
        for (int i = 0; i < companion.gamesToTest.Count; i++)
        {
            KeyValuePair<string, AutomatedGameIteration> game = companion.gamesToTest[i];

            gameKeyCheck.Add(game.Key);
      
        }
    }

    // Checking to see if the game is already added to the queue.
    private bool checkGameAlreadyAdded(string gameKey) {
        if (gameKeyCheck.Contains(gameKey)) {
            return true;
        }
        return false;
    }
	
	// Loads the game specified by the gameKey.
	private IEnumerator loadGame(string gameKey, bool force = false)
	{
		loadingGame = true;
		// It takes a frame to make sure the current state of the game is correct.
		yield return null; 

		// First we want to see if we're currently in a game.
		if (!force && GameState.game != null && (gameKey == "" || gameKey == GameState.game.keyName))
		{
			// We are in the game that we want to do the automation in.
			gameKey = GameState.game.keyName;
		}
		else
		{
			// We want to click on the more games button to go back to the lobby.
			while (!GameState.isMainLobby || Loading.isLoading)
			{
				if (Dialog.instance != null && Dialog.instance.isShowing && Dialog.instance.currentDialog)
				{
					// TODO Add visualizer to this coroutine.
					yield return StartCoroutine(Dialog.instance.currentDialog.automate());
				}
				else if (Overlay.instance != null && Overlay.instance.top != null && Overlay.instance.top.lobbyButton != null)
				{
					// TODO Add visualizer to this coroutine.
					yield return StartCoroutine(CommonAutomation.clickRandomColliderIn(Overlay.instance.top.lobbyButton.gameObject));
				}

				yield return null;
			}
				
			LobbyGame gameInfo = LobbyGame.find(gameKey);
			SlotResourceData resourceData = SlotResourceMap.getData(gameKey);
			if (gameInfo == null || resourceData == null)
			{			
				// TRAMP can't play the game without this data and must skip it.

				if (gameInfo == null)
				{
					Debug.LogErrorFormat("<color={0}>TRAMP> LobbyGame is null for {1}</color>", TRAMP_DEBUG_COLOR, gameKey);
				}
					
				if (resourceData == null)
				{
					Debug.LogErrorFormat("<color={0}>TRAMP> SlotResourceData is null for {1}</color>", TRAMP_DEBUG_COLOR, gameKey);
				}
				
				gameTestDone();
				yield break;
			}

			if (SlotsPlayer.instance != null && SlotsPlayer.instance.socialMember != null)
			{
				// if (SlotsPlayer.instance.socialMember.experienceLevel < gameInfo.unlockLevel)
				// {
				// 	Debug.LogErrorFormat("<color={0}>TRAMP> Can not test {1} because it is locked (level {2} but player is only {3}.</color>",
				// 		TRAMP_DEBUG_COLOR, gameKey, gameInfo.unlockLevel, SlotsPlayer.instance.socialMember.experienceLevel);

				// 	gameTestDone();
				// 	yield break;
				// }

				// TODO: TRAMP is skipping games because of this test but it should be able to play them, 
				// so, for now, I'm commenting it out.
				if (gameInfo.isVIPGame && SlotsPlayer.instance.socialMember.vipLevel < gameInfo.vipLevel.levelNumber)
				{
					TRAMPLogFiles.logToOther("Can not test {0} because it is a VIP game with a VIP level number of {1} but player's VIP Level is only {2}.",
						gameKey, gameInfo.vipLevel.levelNumber, SlotsPlayer.instance.socialMember.vipLevel);

					//gameTestDone();
					//yield break;
				}
			}

			// TODO: TRAMP is skipping games because of this test but it should be able to play them, 
			// so, for now, I'm commenting it out.
			if (!gameInfo.isActive)
			{
				TRAMPLogFiles.logToOther("Can not test {0} because it is not active.", gameKey);

				//gameTestDone();
				//yield break;
			}

			if (!SlotsWagerSets.doesGameHaveWagerSet(gameKey))
			{
				Debug.LogErrorFormat("<color={0}>TRAMP> Missing Wager Set for: {1}. Disable \"Use new wager system\" from DevGUI Main tab to load this game.</color>", TRAMP_DEBUG_COLOR, gameKey);

				gameTestDone();
				yield break;
			}
			else
			{
				// Load the game.
				Overlay.instance.top.showLobbyButton();
				GameState.pushGame(gameInfo);
				Loading.show(Loading.LoadingTransactionTarget.GAME);
				Glb.loadGame();
			}

			while (GameState.isMainLobby || Loading.isLoading)
			{
				// Wait until we get into the game.
				yield return null;
			}

			// Check for paytable image/license data.
			if (gameInfo.license == "" && gameInfo.groupInfo.license == "" && !gameKey.Contains("gen") && gameKey != "lbb01")
			{
				Debug.LogWarning(string.Format("No paytable license info found for game {0}", gameKey));
			}
			SlotGameData slotGameData = SlotGameData.find(gameKey);
			if (slotGameData == null)
			{
				Debug.LogError(string.Format("Could not find slot game data for game: {0}", gameKey));
			}
			else
			{
				for (int i=0; i < slotGameData.bonusGames.Length; i++)
				{
					string bonusGameKey = slotGameData.bonusGames[i];
					BonusGame bonusGameData = BonusGame.find(bonusGameKey);
					if (bonusGameData == null)
					{
						Debug.LogError(string.Format("Could not find BonusGame data for bonus game {0}", bonusGameKey));
					}
					else
					{
						string imageBaseName = PaytableBonus.getPaytableBonusImageBasename(bonusGameData);
						if (string.IsNullOrEmpty(imageBaseName))
						{
							Debug.LogWarning(string.Format("No paytable image defined for bonus game {0}", bonusGameKey));
						}
						else
						{
							Debug.LogFormat("<color={0}>Game {1} bonus game {2} uses paytable image: {3}</color>", TRAMP_DEBUG_COLOR, gameKey, bonusGameKey, imageBaseName);
						}
					}
				}
			}

			companion.activeGame.start();

		}

		loadingGame = false;
	}

	public IEnumerator playGame(AutomatedGameIteration game)
	{

		if (game == null)
		{
			Debug.LogErrorFormat("<color={0}>TRAMP> Can't play with a null AutomatedBaseGameInfo!</color>",
					TRAMP_DEBUG_COLOR);
			yield break;
		}

		// If the game to be tested was already testing and was loaded in the past.
		if (game.isGameLoadedSuccessful && game.isTesting)
		{
			TRAMPLogFiles.saveCurrentTestPlan();

			Debug.LogErrorFormat("<color={0}>TRAMP> Something went wrong during the last test of {1}, moving on to next game!</color>",
				TRAMP_DEBUG_COLOR, AutomatedPlayerCompanion.instance.activeGame.commonGame.gameKey);

			playNextGameInQueue();

			yield break; 
		}
			
		yield return StartCoroutine(loadGame(game.commonGame.gameKey));

		if (companion.isTestMemory)
		{
			sampleMemoryCoroutine = StartCoroutine(sampleMemory());
		}
	}

	private void setGameMode()
	{
		if (Loading.isLoading)
		{
			gameMode = GameMode.LOADING;
		}
		else if (BonusGameManager.isBonusGameActive || GameState.giftedBonus != null)
		{
			// Are we in a bonus game?
			gameMode = GameMode.BONUS_GAME;
		}
		else if (ReelGame.activeGame != null && ReelGame.activeGame is SlotBaseGame)
		{
			// Are we in the Base game?
			gameMode = GameMode.BASE_GAME;
		}
		else if (GameState.isMainLobby)
		{
			gameMode = GameMode.LOBBY;
		}
		else
		{
			gameMode = GameMode.NONE;
		}
	}

	/// Logging stuff.
	public void post(string message)
	{
		if (AutomatedPlayerCompanion.instance.activeGame != null)
		{
			switch (gameMode)
			{
				case GameMode.BASE_GAME:
				case GameMode.BONUS_GAME:
					AutomatedPlayerCompanion.instance.activeGame.post(message);
					break;
			}
		}
		else
		{
			TRAMPLogFiles.logToOther(message);
		}
	}

	public void recieved(string message)
	{
		if (AutomatedPlayerCompanion.instance.activeGame != null)
		{
			switch (gameMode)
			{
				case GameMode.BASE_GAME:
				case GameMode.BONUS_GAME:
					AutomatedPlayerCompanion.instance.activeGame.recieved(message);
					break;
			}
		}
		else
		{
			TRAMPLogFiles.logToOther(message);
		}
	}

	private void incrementFatalErrorCount(string gameKey)
	{
		int count;
		if (companion.gamekeyExceptionCounts.TryGetValue(gameKey, out count))
		{
			companion.gamekeyExceptionCounts.Remove(gameKey);
			count++;
		}
		else
		{
			count = 1;
		}
		companion.gamekeyExceptionCounts.Add(gameKey, count);
	}

	public IEnumerator sampleMemory()
	{
		yield return new TIWaitForSeconds(5.0f);

		#if UNITY_EDITOR
		while (companion.activeGame != null && 
			companion.activeGame.remainingTestActions.Count > 0)
		{
			int memMono = (int)UnityEngine.Profiling.Profiler.GetMonoUsedSize();
				
			int countTextures = 0;
			int memTextures = 0;
			foreach (Texture tex in Resources.FindObjectsOfTypeAll(typeof(Texture)))
			{
				countTextures++;
				memTextures +=(int) ArtCheckGUI.getTextureMemory(tex);
			}

			int countMeshes = 0;
			int memMeshes = 0;
			foreach (Mesh mesh in Resources.FindObjectsOfTypeAll(typeof(Mesh)))
			{
				countMeshes++;
				memMeshes += UnityEngine.Profiling.Profiler.GetRuntimeMemorySize(mesh);
			}

			int countMaterials = 0;
			int memMaterials = 0;
			foreach (Material mat in Resources.FindObjectsOfTypeAll(typeof(Material)))
			{
				countMaterials++;
				memMaterials += UnityEngine.Profiling.Profiler.GetRuntimeMemorySize(mat);
			}

			int countAnimationClips = 0;
			int memAnimationClips = 0;
			foreach (AnimationClip ani in Resources.FindObjectsOfTypeAll(typeof(AnimationClip)))
			{
				countAnimationClips++;
				memAnimationClips += UnityEngine.Profiling.Profiler.GetRuntimeMemorySize(ani);
			}

			int countAudioClips = 0;
			int memAudioClips = 0;
			foreach (AudioClip clp in Resources.FindObjectsOfTypeAll(typeof(AudioClip)))
			{
				countAudioClips++;
				memAudioClips += UnityEngine.Profiling.Profiler.GetRuntimeMemorySize(clp);
			}

			int memTotal = memMono + memTextures + memMeshes + memMaterials + memAnimationClips +
				memAudioClips;

			if (companion.activeGame.stats != null)
			{
				companion.activeGame.stats.updateMemoryStats(memTotal);
			}

			TRAMPSplunk.sampleMemoryEvent(AutomatedPlayerCompanion.instance.activeGame, memMono, 
				countTextures, memTextures, 
				countMeshes, memMeshes, 
				countMaterials, memMaterials, 
				countAnimationClips, memAnimationClips, 
				countAudioClips, memAudioClips,
				memTotal);
			
			yield return new TIWaitForSeconds(5.0f);
		}
		#else
			maxMemory = -1;
		#endif
	}

	public GameMode getGameMode()
	{
		return gameMode;
	}

	// These are the methods called from non-tramp code to mark important events and milestones during execution
	// Note:  Putting them in their own class means they couldn't access AutomatedPlayer's internals.
	#region Events

	public static void spinClicked()
	{
		TRAMPSplunk.spinDataEventData.slotSpinClicked = System.DateTime.Now;

		Debug.LogFormat("<color={0}>TRAMP> spin Clicked at {1:yyyy-MM-dd_hh-mm-ss.fff-tt}</color>", 
			TRAMP_DEBUG_COLOR,
			TRAMPSplunk.spinDataEventData.slotSpinClicked);

		if (instance == null || AutomatedPlayerCompanion.instance.activeGame == null)
		{
			Debug.LogErrorFormat("<color={0}>TRAMP> AutomatedPlayer.spinClicked() called but their isn't a current game being tested?</color>",
				TRAMP_DEBUG_COLOR);
		}
	}

	public static void spinRequested()
	{
		TRAMPSplunk.spinDataEventData.slotSpinRequest = System.DateTime.Now;

		Debug.LogFormat("<color={0}>TRAMP> spin Requested at {1:yyyy-MM-dd_hh-mm-ss.fff-tt}, SpinClickToRequestTime = {2:N3}</color>", 
			TRAMP_DEBUG_COLOR,
			TRAMPSplunk.spinDataEventData.slotSpinRequest,
			TRAMPSplunk.spinDataEventData.getSpinClickToRequestTime());

		if (instance == null || AutomatedPlayerCompanion.instance.activeGame == null)
		{
			Debug.LogErrorFormat("<color={0}>TRAMP> AutomatedPlayer.spinRequested() called but their isn't a current game being tested?</color>",
				TRAMP_DEBUG_COLOR);
		}
		if (instance.companion != null)
		{
			// If we're running autospins, we have to count them differently
			if (instance.companion.activeGame != null && isRunningAutospins)
			{
				instance.companion.activeGame.countSpin(SpinPanel.instance.betAmount, false);
			}
			instance.companion.spinRequested();
		}

		// TODO: putting this here for now. Try to find a better home for this block of code
		if (shouldSlamStopOnSpins)
		{
			instance.StartCoroutine(SpinPanel.instance.automateSlamStop(instance.slamStopDelayTime));
		}
	}

	public static void autospinRequested()
	{

		if (AutomatedPlayerCompanion.instance != null)
		{
			AutomatedPlayerCompanion.instance.autospinRequested();
		}
	}

	public static void spinReceived()
	{
		TRAMPSplunk.spinDataEventData.slotSpinReceive = System.DateTime.Now;

		Debug.LogFormat("<color={0}>TRAMP> spin Received at {1:yyyy-MM-dd_hh-mm-ss.fff-tt}, SpinRequestToReceiveTime = {2:N3}</color>", 
			TRAMP_DEBUG_COLOR,
			TRAMPSplunk.spinDataEventData.slotSpinReceive,
			TRAMPSplunk.spinDataEventData.getSpinRequestToReceiveTime());
		
		if (instance == null || instance.companion.activeGame == null)
		{
			Debug.LogErrorFormat("<color={0}>TRAMP> AutomatedPlayer.spinReceived() called but their isn't a current game being tested?</color>",
				TRAMP_DEBUG_COLOR);
		}
		if (instance.companion != null)
		{
			instance.companion.spinReceived();
		}
	}

	public static void reelsStopped()
	{			
		// The reels stopped
		TRAMPSplunk.spinDataEventData.slotSpinReelsStop = System.DateTime.Now;

		Debug.LogFormat("<color={0}>TRAMP> Reels stopped at {1:yyyy-MM-dd_hh-mm-ss.fff-tt}, SpinReceiveToReelsStopTime = {2:N3}</color>", 
			TRAMP_DEBUG_COLOR,
			TRAMPSplunk.spinDataEventData.slotSpinReelsStop,
			TRAMPSplunk.spinDataEventData.getSpinReceiveToReelsStopTime());

		if (instance == null || instance.companion.activeGame == null)
		{
			Debug.LogErrorFormat("<color={0}>TRAMP> AutomatedPlayer.reelsStopped() called but their isn't a current game being tested?</color>",
				TRAMP_DEBUG_COLOR);
		}
			
	}

	public static void spinFinished()
	{		
		TRAMPSplunk.spinDataEventData.slotSpinComplete = System.DateTime.Now;

		Debug.LogFormat("<color={0}>TRAMP> spin Finished at {1:yyyy-MM-dd_hh-mm-ss.fff-tt}, ReelsStopToSpinCompleteTime = {2:N3} TotalSpinTime = {3:N3}</color>",
			TRAMP_DEBUG_COLOR, 
			TRAMPSplunk.spinDataEventData.slotSpinComplete,
			TRAMPSplunk.spinDataEventData.getReelsStopToSpinCompleteTime(),
			TRAMPSplunk.spinDataEventData.getTotalSpinTime());

		if (AutomatedPlayerCompanion.instance != null && AutomatedPlayerCompanion.instance.activeGame != null)
		{
			// The spin just ended.
			AutomatedPlayerCompanion.instance.activeGame.spinFinished(instance.gameMode);
		}
		else
		{
			Debug.LogErrorFormat("<color={0}>TRAMP> AutomatedPlayer.spinFinished() called but there isn't a current game being tested?</color>",
				TRAMP_DEBUG_COLOR);
		}

		if (instance.companion != null)
		{
			instance.companion.spinFinished();
		}
	}

	public static void spinSlamStopped()
	{
		TRAMPSplunk.spinDataEventData.slotSpinSlamStopped = System.DateTime.Now;

		Debug.LogFormat("<color={0}>TRAMP> spin Slam Stopped at {1:yyyy-MM-dd_hh-mm-ss.fff-tt}</color>", 
			AutomatedPlayer.TRAMP_DEBUG_COLOR,
			TRAMPSplunk.spinDataEventData.slotSpinSlamStopped);
	}

	// Send fake gifted game to myself.
	private void sendFakeGiftGameToSelf()
	{
		if (SlotsPlayer.isFacebookUser)
		{
			long fakeGiftZid;
			long.TryParse(SlotsPlayer.instance.socialMember.zId, out fakeGiftZid);
			if (GameState.game != null && !string.IsNullOrEmpty(GameState.game.keyName))
			{
				string fakeGiftDesignator = GameState.game.keyName;
				string fakeGiftBonusGame = null;
				SlotGameData gameData = SlotGameData.find(GameState.game.keyName);
				foreach(string bonusGameName in gameData.bonusGames)
				{
					BonusGame bonusGameData = BonusGame.find(bonusGameName);
					if (bonusGameData.gift)
					{
						fakeGiftBonusGame = bonusGameName;
						break;
					}
				}
				if (fakeGiftZid > 0 && !string.IsNullOrEmpty(fakeGiftDesignator) && !string.IsNullOrEmpty(fakeGiftBonusGame))
				{
					Debug.LogFormat("<color={0}>TRAMP> Sending fake gifted game {1} for {2}</color>", TRAMP_DEBUG_COLOR, fakeGiftBonusGame, fakeGiftDesignator);
					SendFakeGiftAction.sendGift(fakeGiftZid, fakeGiftDesignator, fakeGiftBonusGame);
				}
			}
		}
	}

	#endregion

	#region ** Methods for use by the editor script AutomationPlayerEditor.cs ONLY **

	// Do not use, this is for the editor script only
	public static void turnAutomationOff(AutomatedPlayer targetFromEditor)
	{
		isAutomating = false;

		PlayerPrefsCache.SetInt(DebugPrefs.ZYNGA_TRAMP, 0);
		PlayerPrefsCache.SetInt(DebugPrefs.STARTED_FROM_COMMAND_LINE, 0);

		spinFinished();

		instance.gameTestDone(true);

		UnityEditor.EditorApplication.isPlaying = false;
		PlayerPrefsCache.Save();
	}

	// Do not use, this is for the editor script only
	public static void turnAutomationOn(AutomatedPlayer targetFromEditor)
	{
		PlayerPrefsCache.SetInt(DebugPrefs.STARTED_FROM_COMMAND_LINE, 1);
		UnityEditor.EditorApplication.isPlaying = false;
		UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Data/HIR/Scenes/Startup.unity");
		UnityEditor.EditorApplication.isPlaying = true;
		PlayerPrefsCache.Save();
	}

	#endregion
}
#else

// Dummy placeholder which is necessary as long as AutomatedPlayer is saved in a scene or prefab
#if UNITY_EDITOR

public class AutomatedPlayer : TICoroutineMonoBehaviour
{
	public TextAsset testInformation;
}
#endif

#endif // ZYNGA_TRAMP
