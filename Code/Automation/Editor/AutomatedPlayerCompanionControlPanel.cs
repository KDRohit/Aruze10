using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;	// For List.Where()

public class AutomatedPlayerCompanionControlPanel : EditorWindow {
	
	#if ZYNGA_TRAMP

	// Control panel variables
	private static AutomatedPlayerCompanionControlPanel instance = null;
	private AutomatedPlayerCompanion companion;

	private const int LABEL_MAX_CHARS = 9999;
	private const int TEST_ACTION_MAX_CHARS = 4;

	// Styles for the GUI
	private bool stylesInitialized = false;
	private GUIStyle windowStyle;
	private GUIStyle overviewPanelStyle;
	private GUIStyle gameSelectionPanelStyle;
	private GUIStyle gameDetailPanelStyle;
	private GUIStyle blackTextField;

	private GUIStyle pinkTextStyle;
	private GUIStyle redTextStyle;
	private GUIStyle yellowTextStyle;
	private GUIStyle blueTextStyle;
	private GUIStyle panelTitleStyle;
	private GUIStyle panelHeaderStyle;
	private GUIStyle removeButtonStyle;

	private GUIStyle defaultLabelStyle;
	private GUIStyle defaultToggleStyle;
	private GUIStyle whiteToggleStyle;

	private static readonly Color BLUE_TEXT_COLOR = new Color(0.63f, 1.0f, 1.0f);
	private static readonly Color RED_TEXT_COLOR = new Color(0.95f, 0.21f, 0.21f);
	private static readonly Color PINK_TEXT_COLOR = new Color(0.95f, 0.51f, 0.95f);
	private static readonly Color WHITE_TEXT_COLOR = new Color(0.85f, 0.85f, 0.85f);

	// For displaying the control panel
	private bool showOverviewPanel = true;
	private bool showGameSelectionPanel = true;
	private bool showGameDetailsPanel = true;

	// Store panel and window sizes
	private const float windowHeight = 900.0f;

	// Calculated the minimum size of the window based on which panels are active.
	private Vector2 windowSize ()
	{
		float overview = showOverviewPanel ? overviewPanelSize.x : 0.0f;
		float selection = showGameSelectionPanel ? gameSelectionPanelSize.x : 0.0f;
		float details = showGameDetailsPanel ? gameDetailsPanelSize.x : 0.0f;
		return new Vector2(overview + selection + details + 30.0f, windowHeight + 30.0f);
	}
	private Vector2 overviewPanelSize = new Vector2(300.0f, windowHeight);
	private Vector2 gameSelectionPanelSize = new Vector2(400.0f, windowHeight);
	private Vector2 gameDetailsPanelSize = new Vector2(650.0f, windowHeight / 3.0f);

	// Overview variables
	private string gameToAddtoQueue = "";
	private AutomatedCompanionLog selectedVisualLog;
	private bool savedVisualLog = true;
	private string checkoutString = AutomatedPlayerProcesses.getBranchName();

	private string pullOnCompletionString = AutomatedPlayerProcesses.getBranchName();

	// For displaying the game and test queue selection buttons
	private string gameFilterKeyword = "";
	private string gameFilterLogMessage = "";
	private string filterSortStatusString = "";
	private List<string> visibleTestedGameKeys;
	private Vector2 gameSelectionScrollPos;
	private Vector2 archiveSelectionScrollPos;
	private Vector2 testPlanSelectionScrollPos = new Vector2(0.0f, -500.0f);

	// For displaying the selected game's overview
	private string selectedGameKey;
	private AutomatedGameIteration selectedGame;
	private AutomatedGameStats selectedGameStats;
	private bool showTestPlanToggle = false; 				// true to show game test plan, false to show game summary
	private string keyPress = "";
	private Vector2 gameOverviewScrollPos;
	private Vector2 testedActionsScrollPos;
	private Vector2 gameIterationScrollPos;

	// For displaying the regex list panel
	private bool showRegexListPanelToggle = false; 			// true to show stack, false to show message
	private bool converseRegexListFilterToggle = false; 	// true to filter the logs by the regex list conversely.
	private string regexListFileName = TRAMPLogFiles.LADI_REGEX_FILE_DEFAULT;
	private Vector2 regexListScrollPos;

	// For displaying the selected game log
	//private bool showStackTraceToggle = false; 				// true to show log stack trace, false to show message
	private string logMessageToDisplay = "";
	private LogInfo selectedLogInfoToDisplay;
	private Vector2 logDisplayScrollPos;

	// For searching through *ALL* logs
	private bool showAllLogsToggle = false;
	private string searchKeywords = string.Empty;
	private string ignoreKeywords = string.Empty;
	private Vector2 searchResultsScrollPos;
	private Dictionary<string, List<AutomatedCompanionLog>> allGameLogs;

	// For displaying all of the game's logs for selection
	private bool showLobbyLogsToggle = false;
	private bool filterLogsBySingleKeyword = false;
	private List<AutomatedCompanionLog> gameLogs;
	private List<AutomatedCompanionLog> lobbyLogs;
	private AutomatedCompanionLog selectedGameLog;
	private Vector2 gamelogSelectionScrollPos;
	private Vector2 lobbyLogSelectionScrollPos;
	private string logFilterKeyword = "";
	private string editedFilterKeyword = "";
	private Regex logFilterRegex;

	// Allows user to filter logs based on a list of regexs
	private bool useRegexList = false; 
	private Regex selectedRegexUnion;
	private List<string> selectedRegexList;

	// The type of sorting the user currently selected.
	private GameKeySortType selectedGameSortType = GameKeySortType.ALPHABET;
	private LogsFilterType selectedLogFilterType = LogsFilterType.NONE;
	private GameKeyFilterType selectedGameFilterType = GameKeyFilterType.NONE;

	// Stores all the archive paths so that we don't have to do it every OnGUI call.
	private Dictionary<string, string> archivePaths;

	private int maxArchivesToLoad = 20;

	// Flag for whether or not all the past archives should be found and shown.
	private bool showTestArchives = false;

	public enum GameKeySortType
	{
		ALPHABET, 		// Sort by alphabetical
		TEST_ORDER,		// Sort by order of testing
		SEVERITY		// Sort by severity: e.g. error, warning, logs
	}

	public enum GameKeyFilterType
	{
		NONE,		// No filters, show all
		KEYWORD,	// Filter by keyword (searching)
		LOG_MESSAGE, // Filter by games that have a specific log.
		EXCEPTIONS,	// Filter games with only exceptions
		ERRORS,		// Filter games with only errors
		WARNINGS,	// Filter games with only warnings
		NORMAL		// Filter games with no errors or warnings at all
	}

	public enum LogsFilterType
	{
		NONE,		// No filters, show all
		ERRORS,		// Filter logs with only errors
		WARNINGS,	// Filter logs with only warnings
		EXCEPTIONS	// Filter logs with no errors or warnings at all
	}

	public enum LogInfo
	{
		MESSAGE,		// Show log message
		STACK_TRACE,	// Show log stack trace
		OUTCOME,		// Show the slot outcome that happened for this log
		PREV_OUTCOME	// Show the slot outcome that happened before this log
	}

	[MenuItem ("TRAMP/Companion Control Panel")]
	static void Init()
	{
		if (instance == null)
		{
			// Get existing open window or if none, make a new one:
			instance = (AutomatedPlayerCompanionControlPanel)EditorWindow.GetWindow<AutomatedPlayerCompanionControlPanel>("LADI Control Panel", true, typeof(AutomatedPlayerControlPanel));
			instance.Show();
			instance.Focus();
			instance.minSize = instance.windowSize();
			instance.selectedRegexList = new List<string>();
			// Create the lists for the game keys and logs
			instance.visibleTestedGameKeys = new List<string>();
			instance.gameLogs = new List<AutomatedCompanionLog>();
			instance.lobbyLogs = new List<AutomatedCompanionLog>();
			instance.archivePaths = new Dictionary<string, string>();

			// Finds all the past archives. We don't want to do this often since it accesses disk.
			instance.findAllArchives();
		}
		if (instance != null)
		{
			instance.stylesInitialized = false; // TODO remove this when we finalize styles.
			instance.loadSelectedRegex();
			instance.Show();
			instance.Focus();
		}
	}

	public void OnInspectorUpdate()
	{
		if (EditorApplication.isPlaying)
		{
			Repaint();
		}
	}

	void OnGUI()
	{
		if (instance != null)
		{
			if (!stylesInitialized)
			{
				initStyles();
			}

			// Sometimes, LADI might crash but the control panel is still running, so we need to re-reference the instance.
			if (companion != AutomatedPlayerCompanion.instance)
			{
				companion = AutomatedPlayerCompanion.instance;
				filterTestedGameButtons();
			}

			minSize = windowSize();
			drawControlPanel();

			if (AutomatedPlayerCompanion.addLogToControlPanel== null || AutomatedPlayerCompanion.addTestedGameToControlPanel == null)
			{
				AutomatedPlayerCompanion.addLogToControlPanel = AutomatedPlayerCompanionControlPanel.addLog;
				AutomatedPlayerCompanion.addTestedGameToControlPanel = AutomatedPlayerCompanionControlPanel.addTestedGameButton;
			}
		}
		else
		{
			Init();
		}
	}

	// Initialize the styles here so we don't have to do it every frame.
	public void initStyles()
	{
		Texture2D blackBackground = Texture2D.blackTexture;
		blackBackground.Resize(512, 512);
		// Set the style for the whole control panel window.
		windowStyle = new GUIStyle(GUI.skin.box);

		// Set the style for the window components.
		overviewPanelStyle = new GUIStyle(GUI.skin.box);
		overviewPanelStyle.fixedWidth = overviewPanelSize.x;
		overviewPanelStyle.fixedHeight = overviewPanelSize.y;
		overviewPanelStyle.normal.textColor = WHITE_TEXT_COLOR;
		overviewPanelStyle.normal.background = blackBackground; // Use a black background to make text clearer

		gameSelectionPanelStyle = new GUIStyle(overviewPanelStyle);
		gameSelectionPanelStyle.fixedWidth = gameSelectionPanelSize.x;
		gameSelectionPanelStyle.fixedHeight = gameSelectionPanelSize.y;

		gameDetailPanelStyle = new GUIStyle(overviewPanelStyle);
		gameDetailPanelStyle.fixedWidth = gameDetailsPanelSize.x;
		gameDetailPanelStyle.fixedHeight = gameDetailsPanelSize.y;

		blackTextField = new GUIStyle(GUI.skin.label);
		blackTextField.wordWrap = true;
		blackTextField.clipping = TextClipping.Overflow;
		blackTextField.normal.textColor = WHITE_TEXT_COLOR;
		blackTextField.normal.background = blackBackground;

		// Change the default styles to show white text (against the black background)
		defaultLabelStyle = GUI.skin.label;
		defaultToggleStyle = GUI.skin.toggle;
		whiteToggleStyle = new GUIStyle(GUI.skin.toggle);
		whiteToggleStyle.normal.textColor = WHITE_TEXT_COLOR;
		whiteToggleStyle.active.textColor = WHITE_TEXT_COLOR;
		whiteToggleStyle.hover.textColor = WHITE_TEXT_COLOR;

		pinkTextStyle = new GUIStyle(blackTextField);
		pinkTextStyle.normal.textColor = PINK_TEXT_COLOR;

		redTextStyle = new GUIStyle(blackTextField);
		redTextStyle.normal.textColor = RED_TEXT_COLOR;

		yellowTextStyle = new GUIStyle(blackTextField);
		yellowTextStyle.normal.textColor = Color.yellow;

		blueTextStyle = new GUIStyle(blackTextField);
		blueTextStyle.normal.textColor = BLUE_TEXT_COLOR;

		panelTitleStyle = new GUIStyle(blackTextField);
		panelTitleStyle.alignment = TextAnchor.MiddleCenter;
		panelTitleStyle.fontSize = 19;

		panelHeaderStyle = new GUIStyle(blackTextField);
		panelHeaderStyle.alignment = TextAnchor.MiddleCenter;
		panelHeaderStyle.fontSize = 14;

		removeButtonStyle = new GUIStyle(GUI.skin.button);
		removeButtonStyle.fixedWidth = 20.0f;
		stylesInitialized = true;
	}

	// This is the main method for drawing the control panel.
	public void drawControlPanel()
	{
		// Change the default color of these gui styles so they show up in Unity personal.
		// We will reset them at the end of onGUI so we don't affect other UI
	#if !UNITY_PRO_LICENSE
		GUI.skin.label = blackTextField;
		GUI.skin.toggle = whiteToggleStyle;
	#endif

		EditorGUILayout.BeginHorizontal(windowStyle);
		// Draw the overview
		if (showOverviewPanel)
		{
			EditorGUILayout.BeginVertical(overviewPanelStyle);
			drawOverview();
			EditorGUILayout.EndVertical();
		}
		// Draw the game selection buttons
		if (showGameSelectionPanel)
		{
			EditorGUILayout.BeginVertical(gameSelectionPanelStyle);
			drawGameSelectionPanel();
			EditorGUILayout.EndVertical();
		}
		if (showGameDetailsPanel)
		{
			EditorGUILayout.BeginVertical();
			// Draw the game overview and test plan
			EditorGUILayout.BeginVertical(gameDetailPanelStyle);
			drawGameOverviewPanel();
			EditorGUILayout.EndVertical();

			// Draw the game log selection panel
			EditorGUILayout.BeginVertical(gameDetailPanelStyle);
			drawLogSelectionPanel();
			EditorGUILayout.EndVertical();

			// Draw the game logs
			EditorGUILayout.BeginVertical(gameDetailPanelStyle);
			drawLogDisplayPanel();
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndVertical();
		}
		EditorGUILayout.EndHorizontal();

		// Draw the toggles to allow the user to hide certain panels they don't want to see.
		EditorGUILayout.BeginHorizontal();
		showOverviewPanel = GUILayout.Toggle(showOverviewPanel, "Show Control Panel Overview");
		showGameSelectionPanel = GUILayout.Toggle(showGameSelectionPanel, "Show Game Selection Panel");
		showGameDetailsPanel = GUILayout.Toggle(showGameDetailsPanel, "Show Game Details Panel");
		EditorGUILayout.EndHorizontal();

		// Reset the GUIStyles we changed.
	#if !UNITY_PRO_LICENSE
		GUI.skin.label = defaultLabelStyle;
		GUI.skin.toggle = defaultToggleStyle;
	#endif
	}

	// Draw the overview of TRAMP control panel and LADI control panel
	private void drawOverview()
	{

		drawTrampControlPanel();

		GUILayout.BeginHorizontal();
		AutomatedPlayer.pullLatestFromGitOnCompletion = GUILayout.Toggle(AutomatedPlayer.pullLatestFromGitOnCompletion, "Pull & Restart When Done:");
		pullOnCompletionString = GUILayout.TextField(pullOnCompletionString);
		GUILayout.EndHorizontal();

		AutomatedPlayer.branchToPullOnCompletion = pullOnCompletionString;
		
		AutomatedPlayer.repeatTestsOnCompletion = GUILayout.Toggle(AutomatedPlayer.repeatTestsOnCompletion, "Repeat Testing When Done");

		if (AutomatedPlayer.repeatTestsOnCompletion && AutomatedPlayer.pullLatestFromGitOnCompletion)
		{
			AutomatedPlayer.pullLatestFromGitOnCompletion = false;
		}

		// Draw the toggles for editing TRAMP behavior.
		AutomatedPlayer.playRandomGamesInQueue = GUILayout.Toggle(AutomatedPlayer.playRandomGamesInQueue, "Test Randomly");
		EditorPrefs.SetBool(AutomatedPlayer.PLAY_RANDOM_GAMES_IN_QUEUE_EDITOR_PREF, AutomatedPlayer.playRandomGamesInQueue);

		AutomatedPlayer.forceGameExitAfterMaxTime = GUILayout.Toggle(AutomatedPlayer.forceGameExitAfterMaxTime, "Force Game End After Max Time");
		EditorPrefs.SetBool(AutomatedPlayer.FORCE_GAME_EXIT_AFTER_MAX_TIME_EDITOR_PREF, AutomatedPlayer.forceGameExitAfterMaxTime);

		AutomatedPlayer.shouldPlayInReverseOrder = GUILayout.Toggle(AutomatedPlayer.shouldPlayInReverseOrder, "Play in Reverse Order");
		EditorPrefs.SetBool(AutomatedPlayer.SHOULD_PLAY_IN_REVERSE_ORDER_EDITOR_PREF, AutomatedPlayer.shouldPlayInReverseOrder);

		AutomatedPlayer.shouldSkipPorts = GUILayout.Toggle(AutomatedPlayer.shouldSkipPorts, "Skip Ports");
		EditorPrefs.SetBool(AutomatedPlayer.SHOULD_SKIP_PORTS_EDITOR_PREF, AutomatedPlayer.shouldSkipPorts);

		AutomatedPlayer.shouldSlamStopOnSpins = GUILayout.Toggle(AutomatedPlayer.shouldSlamStopOnSpins, "Slam Stop On Spins");
		EditorPrefs.SetBool(AutomatedPlayer.SHOULD_SLAM_STOP_ON_SPINS_EDITOR_PREF, AutomatedPlayer.shouldSlamStopOnSpins);

		AutomatedPlayer.shouldTestAutospins = GUILayout.Toggle(AutomatedPlayer.shouldTestAutospins, "Test Autospins");
		EditorPrefs.SetBool(AutomatedPlayer.SHOULD_TEST_AUTOSPINS_EDITOR_PREF, AutomatedPlayer.shouldTestAutospins);

		AutomatedPlayer.spinDirectionType = (AutomatedPlayer.SpinDirectionTypeEnum)EditorGUILayout.EnumPopup("Spin Direction Type", AutomatedPlayer.spinDirectionType);
		EditorPrefs.SetInt(AutomatedPlayer.SPIN_DIRECTION_TYPE_EDITOR_PREF, (int)AutomatedPlayer.spinDirectionType);

		AutomatedPlayer.shouldTestGiftedBonusSpins = GUILayout.Toggle(AutomatedPlayer.shouldTestGiftedBonusSpins, "Test Gifted Bonus Games");
		EditorPrefs.SetBool(AutomatedPlayer.SHOULD_TEST_GIFTED_BONUS_SPINS_EDITOR_PREF, AutomatedPlayer.shouldTestGiftedBonusSpins);

		// Allow the user to see the colliders in game mode.
		Input.drawColliderVisualizer = GUILayout.Toggle(Input.drawColliderVisualizer, "Show Colliders TRAMP Clicks On");
		ColliderVisualizer.instance.enableContinuousVisualColliders = GUILayout.Toggle(ColliderVisualizer.instance.enableContinuousVisualColliders, "Continuously Visualize All Colliders");
		ColliderVisualizer.instance.active = ColliderVisualizer.instance.enableContinuousVisualColliders || Input.drawColliderVisualizer;
		if (!ColliderVisualizer.instance.active)
		{
			ColliderVisualizer.instance.disableVisualizer();
		}

		if (AutomatedPlayer.instance != null)
		{
			// Allow the user to log an error and edit its message. This is saved to the LADI logs.
			Color previousColor = GUI.color;
			GUI.color = AutomatedGameStats.MIN_RED;
			if (GUILayout.Button("Click To Log Visual Error"))
			{
				selectedVisualLog = companion.reportVisualBug();
				savedVisualLog = false;
			}
			GUI.color = previousColor;

			if (selectedVisualLog != null && !savedVisualLog)
			{
				GUILayout.Label("Edit Visual Error Log Message:");
				selectedVisualLog.updateMessage(GUILayout.TextField(selectedVisualLog.logMessage, 99));
				savedVisualLog = GUILayout.Toggle(savedVisualLog, "Save Visual Log Message", GUI.skin.button);
			}

			// Allow the user to change the animation speed using a slider.
			GUILayout.Label(string.Format("Change Animation Speed ({0:N2})", Time.timeScale));
			GUILayout.BeginHorizontal();
			if (companion != null)
			{
				if (GUILayout.Button("1", GUILayout.MaxWidth(40.0f)))
				{
					companion.setTimeScale(1.0f);
				}
				companion.setTimeScale(GUILayout.HorizontalSlider(Time.timeScale, 0.0f, 10.0f));
				if (GUILayout.Button("10", GUILayout.MaxWidth(40.0f)))
				{
					companion.setTimeScale(10.0f);
				}
			}
			GUILayout.EndHorizontal();
		}
		else
		{
			GUILayout.Label("TRAMP Not Initialized");

			drawTrampControlPanel();

		}

		if (AutomatedPlayerCompanion.instance != null)
		{

			// Allow the user to edit the game test queue.
			GUILayout.Label("Game To Add: ");
			gameToAddtoQueue = GUILayout.TextField(gameToAddtoQueue);

			if (GUILayout.Button("Add Game To Queue"))
			{
				companion.addNewGameToTest(gameToAddtoQueue);
			}
			if (GUILayout.Button("Add Game To Front Of Queue"))
			{
				companion.addNewGameToTest(gameToAddtoQueue, true);
			}
			if (GUILayout.Button("Add Game And Play Immediately"))
			{
				companion.addAndPlayGameImmediate(gameToAddtoQueue);
			}
			if (GUILayout.Button("Remove All Games From Queue"))
			{
				companion.gamesToTest = new List<KeyValuePair<string, AutomatedGameIteration>>();
			}
		
			showAllLogsToggle = GUILayout.Toggle(showAllLogsToggle, "Search All Logs From TRAMP Run", GUI.skin.button);

		}
		
		// Additional functionality
		GUILayout.Label("_ Additional Features _", panelTitleStyle);

		// Takes the user to the TRAMP directory.
		if (GUILayout.Button("Open TRAMP directory"))
		{

			// Reveals in Finder/Explorer.
			EditorUtility.RevealInFinder(TRAMPLogFiles.getTRAMPDirectory());

		}

		if (GUILayout.Button("Pull Latest From Git (Needs VPN)"))
		{

			if (AutomatedPlayer.isAutomating)
			{
				AutomatedPlayer.instance.stopAutomation();
			}

			AutomatedPlayerProcesses.pullLatestFromGit();
		}
		
		EditorGUILayout.BeginHorizontal();

		if (GUILayout.Button("Checkout & Restart: "))
		{
			AutomatedPlayerProcesses.checkoutAndRestartUnity(checkoutString);
		}
		checkoutString = GUILayout.TextField(checkoutString);
		EditorGUILayout.EndHorizontal();



		// Show the past test run archives.
		GUILayout.Label("_ TRAMP Archives _", panelTitleStyle);

		// Make it scrollable since there may be lots of archives.
		archiveSelectionScrollPos = GUILayout.BeginScrollView(archiveSelectionScrollPos, false, true);

		// Resets to the latest test plan, picks up where it left off.
		if (GUILayout.Button("Reset To Current Test"))
		{

			// Load and filter the buttons again so that they show up.
			companion.loadCurrentTest();
			filterTestedGameButtons();

		}

		// Lets the user choose a specific archive folder to load.
		if (GUILayout.Button("Manually Select Test Folder"))
		{

			string path = EditorUtility.OpenFolderPanel("Select a TRAMP archive", TRAMPLogFiles.getTRAMPDirectory(), "");
			TRAMPLogFiles.loadTRAMPRun(path);
			filterTestedGameButtons();

		}

		// Load several different TRAMP instances into one TRAMP instance.
		if (GUILayout.Button("Combine & Load TRAMP Instances"))
		{

			// Let the user select the file.
			string path = EditorUtility.OpenFilePanel("Select Instances File", TRAMPLogFiles.getTRAMPDirectory(), "");
			
			// Try to read the file, and if successful, combine the tests.
			try
			{
				string text = System.IO.File.ReadAllText(path);
				JSON fileJSON = new JSON(text);
				AutomatedPlayerCompanion.combineTests(fileJSON);
				filterTestedGameButtons();
			}
			catch (System.Exception e)
			{
				Debug.LogError("Could not load TRAMP instances file. Please ensure it's a single JSON file, or use combine-TRAMP-instances.sh to combine runs.");
				Debug.LogError(e);
			}
		}

		// Toggles showing all of the test archives in the LADI control panel.
		if (showTestArchives)
		{

			if (GUILayout.Button("Hide Test Archives"))
			{
				showTestArchives = false;
			}

			// Finds all the archive folders again and refreshes.
			if (GUILayout.Button("Refresh Archives") || archivePaths == null)
			{
				findAllArchives();
			}

			GUILayout.Label("All Archives", panelHeaderStyle);

			// Displays all archive names, and if selected, opens that archive for viewing.
			foreach (KeyValuePair<string, string> kvp in archivePaths)
			{

				if (GUILayout.Button(kvp.Key))
				{
					TRAMPLogFiles.loadTRAMPRun(kvp.Value);
					filterTestedGameButtons();
				}
			}

		}
		else
		{
			if (GUILayout.Button("Show All Test Archives"))
			{
				showTestArchives = true;
			}
		}

		GUILayout.EndScrollView();

	}

	// Searches the TRAMP folder for all archive folders.
	private void findAllArchives()
	{

		// Log a message so we know when this is happening. It shouldn't happen too often.
		Debug.Log("Finding all TRAMP archives in the TRAMP directory");

		// Clear existing archive paths.
		archivePaths.Clear();

		// Grabs all of the archived directories from tramp logs.
		string[] directories = TRAMPLogFiles.getArchivedTrampRunDirectories();

		// List to store directory names.
		List<string> directoryNames = new List<string>();

		// Get the directory name from each directory path.
		foreach (string directory in directories)
		{

			// Gets a directory name by checking an inner file and getting it's parent directory. Probably a better way to do this.
			string[] innerFiles = System.IO.Directory.GetFiles(directory);

			if (innerFiles.Length > 0)
			{
				string innerFile = System.IO.Directory.GetFiles(directory)[0];
				directoryNames.Add(System.IO.Path.GetDirectoryName(innerFile));
			}
		}

		// By default the alphabetical order is backwards, so reverse it. We want newest first.
		directoryNames.Reverse();

		// Store the archive path in a dictionary of paths and directory names, for displaying/loading later.
		for(int i = 0; i < directories.Length; i++)
		{

			if (i >= maxArchivesToLoad)
			{
				break;
			}
			
			string directory = directoryNames[i];

			string dirName = System.IO.Path.GetFileName(directory);
			dirName = TRAMPLogFiles.getArchiveNameFromDirectory(dirName);
			
			string buttonText = dirName;

			archivePaths.Add(dirName, directory);
		}
	}

	// Draw TRAMP's control panel.
	// This is useful if LADI isn't running or if the user simply wants to access TRAMP.
	private void drawTrampControlPanel()
	{
		GUILayout.Label("_ TRAMP Control Panel _", panelTitleStyle);
		AutomatedPlayerControlPanel.DrawControlPanel(AutomatedPlayer.instance);
	}

	// Draw the table of contents for the user to select which game or test plan to view.
	private void drawGameSelectionPanel()
	{
		GUILayout.Label("Game Table of Contents", panelTitleStyle);

		// Display the game selection buttons
		EditorGUILayout.BeginVertical();
		drawTestedGameButtonsPanel();
		drawGameSelectionTestPlanQueuePanel();
		EditorGUILayout.EndVertical();
	}

	// Draws the panel that lets the user select the active game, or any game that has completed testing.
	private void drawTestedGameButtonsPanel()
	{

		if (companion != null && companion.isCurrentTest)
		{
			GUILayout.Label("Showing Current Test");
		}
		else
		{
			GUILayout.Label("Showing past test run");
		}

		GUILayout.Label("_________ Test Plan _________", panelHeaderStyle);

		if (companion != null)
		{

			EditorGUILayout.BeginHorizontal();
			selectedGameFilterType = (AutomatedPlayerCompanionControlPanel.GameKeyFilterType) EditorGUILayout.EnumPopup("Filter Options: ", selectedGameFilterType);
			if (GUILayout.Button("Filter"))
			{
				filterTestedGameButtons(false);
			}
			EditorGUILayout.EndHorizontal();

			if (selectedGameFilterType == GameKeyFilterType.KEYWORD)
			{
				// Display the filter options for game buttons
				gameFilterKeyword = EditorGUILayout.TextField("Filter Keyword", gameFilterKeyword);
			}
			else if (selectedGameFilterType == GameKeyFilterType.LOG_MESSAGE)
			{
				gameFilterLogMessage = EditorGUILayout.TextField("Log Message", gameFilterLogMessage);
			}

			// Display the sorting options for game buttons
			EditorGUILayout.BeginHorizontal();
			selectedGameSortType = (AutomatedPlayerCompanionControlPanel.GameKeySortType) EditorGUILayout.EnumPopup("Sort Options: ", selectedGameSortType);
			if (GUILayout.Button("Sort"))
			{
				sortTestedGameButtons();
			}
			EditorGUILayout.EndHorizontal();

			// Allow player to select game currently being tested
			if (companion.activeGame != null)
			{
				if (GUILayout.Button(string.Format("Active Game: ({0}) {1}", companion.activeGame.commonGame.gameKey, companion.activeGame.commonGame.gameName)))
				{
					setSelectedGame("", companion.activeGame);
				}
			}
			else
			{
				GUILayout.Label("No currently active game");
			}

			// Draw a scroll menu of games that have been tested
			GUILayout.Label("_________ Games Tested _________", panelHeaderStyle);

			// Copies a list of the games tested with filter options to your clipboard.
			if (GUILayout.Button("Copy games tested to clipboard"))
			{

				string summaryString = filterSortStatusString;

				if (selectedGameFilterType == GameKeyFilterType.KEYWORD)
				{
					summaryString += string.Format("\nFilter Keyword: \"{0}\"", gameFilterKeyword);
				}
				else if (selectedGameFilterType == GameKeyFilterType.LOG_MESSAGE)
				{
					summaryString += string.Format("\nFilter Log Message: \"{0}\"", gameFilterLogMessage);
				}

				summaryString += "\n----------";

				foreach (string gameKey in visibleTestedGameKeys)
				{
					summaryString += "\n" + gameKey;
				}

				// Copy to clipboard.
				EditorGUIUtility.systemCopyBuffer = summaryString;
			}
			
			if (selectedGameFilterType != GameKeyFilterType.NONE)
			{
				GUILayout.Label(filterSortStatusString);
			}

			gameSelectionScrollPos = GUILayout.BeginScrollView(gameSelectionScrollPos, false, true, GUILayout.MaxHeight(400.0f));
			if (visibleTestedGameKeys != null)
			{
				foreach (string gameKey in visibleTestedGameKeys)
				{
					Color preColor = GUI.color;
					AutomatedGame thisGame = companion.getAutomatedGameByKey(gameKey);

					if (thisGame != null && thisGame.gameIterations.Count > 0)
					{
						GUI.color = thisGame.getGameSeverityColor();
					}

					string gameName = "";
					LobbyGame lobbyGame = LobbyGame.find(gameKey);
					if (lobbyGame != null)
					{
						gameName = lobbyGame.name;
					}
					else
					{
						gameName = "Game name unavailable.";
					}

					if (GUILayout.Button(string.Format("({0}) {1}", gameKey, gameName)))
					{
						GUI.color = preColor;
						setSelectedGame(gameKey);
						if (selectedGame.commonGame.hasAverage())
						{
							selectedGameStats = selectedGame.commonGame.averageStats;
						}
						else
						{
							selectedGameStats = selectedGame.stats;
						}
					}

					GUI.color = preColor;
				}
			}
			else
			{
				Debug.LogErrorFormat("<color={0}>LADI> Game keys not initialized properly!</color>", AutomatedPlayerCompanion.LADI_DEBUG_COLOR);
			}
			GUILayout.EndScrollView();
		}
		else
		{

			GUILayout.Label("LADI Not Initialized");
			
		}
	}

	// Draws the panel that lets the user select a test plan from the queue.
	private void drawGameSelectionTestPlanQueuePanel()
	{

		if (companion != null)
		{
			GUILayout.Label("_________ Games In Queue _________", panelHeaderStyle);
			List<KeyValuePair<string, AutomatedGameIteration>> gamesToTest = companion.getGamesToTestQueue();
			testPlanSelectionScrollPos = GUILayout.BeginScrollView(testPlanSelectionScrollPos, false, true, GUILayout.MaxHeight(400.0f));
			
			foreach (KeyValuePair<string, AutomatedGameIteration> testPlan in gamesToTest)
			{
				Color previousColor = GUI.color;
				GUILayout.BeginHorizontal();
				if (GUILayout.Button("-", removeButtonStyle))
				{
					companion.removeGameFromQueue(testPlan.Value);
					break; // We must break out of this loop to prevent index out of range.
				}

				LobbyGame lobbyGame =  LobbyGame.find(testPlan.Key);
				string gameName = "Cannot find game name";
				if (lobbyGame != null)
				{
					gameName = lobbyGame.name;
				}

				GUI.color = CommonColor.getColorForGame(testPlan.Key);
				string gameStatus = CommonColor.getStatusForGame(testPlan.Key);

				if (GUILayout.Button(string.Format("{0} {1} ({2})", gameStatus, testPlan.Key, gameName)))
				{
					setSelectedGame(testPlan.Key, testPlan.Value);
					showTestPlanToggle = true;
				}
				GUILayout.EndHorizontal();
				GUI.color = previousColor;
			}

			GUILayout.EndScrollView();
		}
	}

	// Draw information about the currently selected game such as the test summary and queued test actions.
	private void drawGameOverviewPanel()
	{
		GUILayout.Label("Game Overview Panel", panelTitleStyle);


		if (companion != null)
		{

			if (selectedGame != null)
			{
				// Allow the user to selected the iteration of the tested game
				AutomatedGame automatedGame = selectedGame.commonGame;
				Color previousColor = GUI.color;
				if (automatedGame != null && automatedGame.gameIterations != null && automatedGame.gameIterations.Count > 0)
				{
					GUILayout.Label("Select an iteration of " + selectedGameKey + " below to show its stats:");
					EditorGUILayout.BeginHorizontal(GUILayout.Height(50.0f));
					gameIterationScrollPos = GUILayout.BeginScrollView(gameIterationScrollPos,true, false, GUILayout.Height(40.0f));
					EditorGUILayout.BeginHorizontal();
					if (automatedGame.hasAverage() && GUILayout.Button("Average", GUILayout.MinWidth(80.0f)))
					{
						selectedGameStats = selectedGame.commonGame.averageStats;
					}

					foreach(AutomatedGameIteration gameIteration in automatedGame.gameIterations)
					{
						GUI.color = gameIteration.stats.getColorBySeverity();
						if (GUILayout.Button(gameIteration.gameIterationNumber.ToString(), GUILayout.MinWidth(40.0f)))
						{
							setSelectedGame("", gameIteration);
						}
					}
					EditorGUILayout.EndHorizontal();
					GUILayout.EndScrollView();
					EditorGUILayout.EndHorizontal();
				}
				else
				{
					GUILayout.Label("Game has no tested iterations yet");
				}
				GUI.color = previousColor;
				string showTestPlanToggleButtonText = showTestPlanToggle ? "Show Game's Details" : "Show Game's Test Plan";
				showTestPlanToggle = GUILayout.Toggle(showTestPlanToggle, showTestPlanToggleButtonText, GUI.skin.button);

				if (showTestPlanToggle)
				{
					GUILayout.Label(string.Format("GameKey: {0} Iteration: {1}", selectedGameKey, selectedGame.gameIterationNumber));
					// Not tested yet.
					if (!companion.isIterationTested(selectedGame))
					{
						// If the selected game iteration is the active game iteration, allow the user
						// to edit future test actions and view already tested actions.
						if (isSelectedGameIterationActiveGame())
						{
							drawActiveGameTestPlanPanel(companion.activeGame);
						}
						else
						{
							// Draw the remaining test actions for a game not done testing.
							// The user will be able to edit the actions.
							drawRemainingTestActionsPanel();
						}
					}
					else
					{
						// Draw the test actions performed for a game iteration that's already tested. 
						drawTestedActionsPanel();
					}
				}
				else
				{
					// Draw the details/test summary for the currently selected game.
					drawGameTestSummaryPanel();
				}
			}
			else
			{
				GUILayout.Label("Select a game to show it's information");
			}
		}
		else
		{
			GUILayout.Label("LADI Not Initialized");
		}
	}

	// Allow the user to edit the remaining test actions for the test plan.
	private void drawRemainingTestActionsPanel()
	{
		GUILayout.BeginVertical();
		GUILayout.Label(string.Format("Remaining Test Plan Actions for ({0}):", selectedGameKey), panelHeaderStyle);
		gameOverviewScrollPos = GUILayout.BeginScrollView(gameOverviewScrollPos, false, true);
		int editValue = 0;
		int count = selectedGame.remainingTestActions.Count;
		for (int i = 0; i < count; i++)
		{
			GUILayout.BeginHorizontal();
			KeyValuePair<string, int> kv = selectedGame.remainingTestActions[i];

			// Display the button for removing an action.
			if (GUILayout.Button("-", removeButtonStyle) && count > 1)
			{
				selectedGame.removeActionAt(i);
				break; // We must break out of this loop to prevent index out of range.
			}
			GUILayout.Label(string.Format("Action: {0}", kv.Key));
			editValue = kv.Value;
			// Get the new value from the text field and try to parse it as an int.
			string textFieldValue = GUILayout.TextField(editValue.ToString(), TEST_ACTION_MAX_CHARS);
			if (!int.TryParse(textFieldValue, out editValue))
			{
				// If it can't be parsed as an int, just set it to the original value
				editValue = kv.Value;
			}

			// Update the amount of the test action.
			if (editValue != kv.Value)
			{
				selectedGame.updateValueAt(i, editValue);
				break; // Break out of this loop in case the action is removed.
			}
			GUILayout.EndHorizontal();
		}
		GUILayout.EndScrollView();

		// Draw the buttons to allow the user to add test actions to be played by TRAMP.
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Add Spin"))
		{
			selectedGame.addTestAction(AutomatedTestAction.SPIN_ACTION);
		}
		if (GUILayout.Button("Add Desync Check"))
		{
			selectedGame.addTestAction(AutomatedTestAction.DESYNC_CHECK_ACTION);
		}
		if (GUILayout.Button("Add Key Press") && !string.IsNullOrEmpty(keyPress))
		{
			selectedGame.addTestAction(keyPress);
		}
		keyPress = GUILayout.TextField(keyPress, 1);
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label("Add Auto Spins: ");
		if (GUILayout.Button("10", GUILayout.ExpandWidth(true)))
		{
			selectedGame.addTestAction(AutomatedTestAction.AUTOSPIN_10);
		}
		if (GUILayout.Button("25", GUILayout.ExpandWidth(true)))
		{
			selectedGame.addTestAction(AutomatedTestAction.AUTOSPIN_25);
		}
		if (GUILayout.Button("50", GUILayout.ExpandWidth(true)))
		{
			selectedGame.addTestAction(AutomatedTestAction.AUTOSPIN_50);
		}
		if (GUILayout.Button("100", GUILayout.ExpandWidth(true)))
		{
			selectedGame.addTestAction(AutomatedTestAction.AUTOSPIN_100);
		}
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
	}

	// Only for the ACTIVE game! Games done testing or still in queue should not call this.
	// Show the test plan actions and allow the player to edit them.
	private void drawActiveGameTestPlanPanel(AutomatedGameIteration gameResults)
	{
		GUILayout.BeginHorizontal();
		drawRemainingTestActionsPanel();
		drawTestedActionsPanel();
		GUILayout.EndHorizontal();
	}

	// Draw the test actions for the selected game that isn't an active game.
	private void drawTestedActionsPanel()
	{
		GUILayout.BeginVertical();
		List<string> actions = selectedGame.actionsTested;
		GUILayout.Label(string.Format("Actions Tested for ({0}):", selectedGameKey), panelHeaderStyle);
		testedActionsScrollPos = GUILayout.BeginScrollView(testedActionsScrollPos, false, true);
		foreach (string action in actions)
		{
			GUILayout.Label(string.Format("Action: {0}", action));
		}
		GUILayout.EndScrollView();
		GUILayout.EndVertical();
	}

	// Draw the game's test summary.
	private void drawGameTestSummaryPanel()
	{
		string iterationLabel = "";
		if (selectedGameStats != null)
		{
			if (selectedGameStats.timeStarted == System.DateTime.MinValue)
			{
				iterationLabel = string.Format("Average statistics across all iterations of {0}", selectedGameKey);
			}
			else if (isSelectedGameIterationActiveGame())
			{
				iterationLabel = string.Format("Active game statistics for {0}", selectedGameKey);
			}
			else
			{
				iterationLabel = string.Format("Statistics for iteration {0} of {1}", selectedGame.gameIterationNumber, selectedGameKey);
			}
			GUILayout.Label(iterationLabel, panelHeaderStyle);

			GUILayout.Label(selectedGameStats.ToString());
		}
		else
		{
			GUILayout.Label("No stats found");
		}
	}

	// Draws the panel/group for the selected game log
	private void drawLogDisplayPanel()
	{
		GUILayout.Label("Log Overview Panel", panelTitleStyle);
		if (selectedGameLog != null)
		{
			selectedLogInfoToDisplay = (LogInfo)EditorGUILayout.EnumPopup("Select Log Info: ", selectedLogInfoToDisplay);

			// Display the selected log information (message, stack trace, or outcome)
			logDisplayScrollPos = GUILayout.BeginScrollView(logDisplayScrollPos, false, true);
			string logNote = ""; // Store an issue with displaying the log

			// Limit the characters we can draw so we don't kill the editor GUI
			// Note: this may be an issue when displaying outcomes.
			if (logMessageToDisplay.Length > LABEL_MAX_CHARS)
			{
				logNote += "| LOG TOO LONG! MAY BE TRUNCATED! (Check files instead)";
				logMessageToDisplay = logMessageToDisplay.Substring(0, LABEL_MAX_CHARS);
			}
			if (selectedGameLog.logNum < 0)
			{
				logNote += "| NEGATIVE LOG NUM, MAY BE JSON ERROR! ";
			}
			GUILayout.Label(logNote, redTextStyle); // Notify the user there was a problem.
			GUILayout.Label(string.Format("Log Type: {0}", selectedGameLog.logType.ToString()), getLogLabelStyleWithType(selectedGameLog.logType));
			GUILayout.Label(string.Format("Log Num: {0}", selectedGameLog.logNum));
			GUILayout.Label(string.Format("Timestamp: {0}", selectedGameLog.timestamp.ToLongTimeString()));
			GUILayout.TextArea(logMessageToDisplay, LABEL_MAX_CHARS, blueTextStyle);
			if (GUILayout.Button("Show TRAMP Log Files"))
			{
				string trampLogPath = "file://" + TRAMPLogFiles.getTRAMPDirectory() + System.IO.Path.DirectorySeparatorChar;
				trampLogPath = trampLogPath.Replace(" ", "%20");
				Application.OpenURL(trampLogPath);
			}

			if (GUILayout.Button("Draft JIRA"))
			{
				Debug.Log("Looks like you want to make a JIRA ticket for this issue. I guess I should populate something.");

				AutomatedGameJIRAData gameJIRAdata = new AutomatedGameJIRAData(selectedGame, selectedGameLog, companion.branchName);
				AutomatedPlayerJIRACreator JIRAWindow = AutomatedPlayerJIRACreator.init(gameJIRAdata);
			}


			GUILayout.EndScrollView();

			// Change the log's state AFTER we drew the last state to prevent repaint errors.
			switch(selectedLogInfoToDisplay)
			{
				case LogInfo.STACK_TRACE:
					logMessageToDisplay = selectedGameLog.stackTrace;
					break;
				case LogInfo.OUTCOME:
					if (selectedGameLog.outcome != null)
					{
						logMessageToDisplay = selectedGameLog.outcome.ToString();
					}
					else
					{
						logMessageToDisplay = "NO OUTCOME (warnings do not have outcomes)";
					}
					break;
				case LogInfo.PREV_OUTCOME:
					if (selectedGameLog.prevOutcome != null)
					{
						logMessageToDisplay = selectedGameLog.prevOutcome.ToString();
					}
					else
					{
						logMessageToDisplay = "NO PREV-OUTCOME (warnings do not have outcomes)";
					}
					break;
				default:
					logMessageToDisplay = selectedGameLog.logMessage;
					break;
			}
		}
		else
		{
			GUILayout.Label("Select a game log below to view its details");
		}
	}

	// Panel to load, edit and save regex lists.
	// The default filename is defined in TRAMPLogFiles
	// Allow the user to put in their own filename
	private void drawRegexListPanel()
	{
		GUILayout.Label("Edit Keyword List Panel");
		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Load List"))
		{
			loadSelectedRegex();
		}
		if (GUILayout.Button("Apply List"))
		{
			applyRegexList();
		}
		if(GUILayout.Button("Save List"))
		{
			saveSelectedRegexList();
		}
		if(GUILayout.Button("Add Keyword"))
		{
			selectedRegexList.Add("");
		}
		GUILayout.Label("Filename: ");
		regexListFileName = GUILayout.TextField(regexListFileName);
		EditorGUILayout.EndHorizontal();
		if (selectedRegexList != null)
		{
			regexListScrollPos = GUILayout.BeginScrollView(regexListScrollPos, false, true);
			for(int i = 0; i < selectedRegexList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button("-", removeButtonStyle))
				{
					selectedRegexList.Remove(selectedRegexList[i]);
					break;
				}
				selectedRegexList[i] = GUILayout.TextField(selectedRegexList[i]);
				EditorGUILayout.EndHorizontal();
			}
			GUILayout.EndScrollView();
		}
		else
		{
			GUILayout.Label("No keyword list loaded");
		}
	}

	// Displays *ALL* game logs in the Log selection panel and allows searching
	private void drawLogSearchPanel()
	{
		if (allGameLogs == null)
		{
			allGameLogs = new Dictionary<string, List<AutomatedCompanionLog>>();
		}
		if (allGameLogs.Count != (companion.gamesTested.Count + 1))		// Total games tested + 1 for lobby
		{
			// Go through all the entries
			foreach (KeyValuePair<string, AutomatedGame> gameEntry in companion.gamesTested)
			{
				string gameKey = gameEntry.Value.gameKey;
				// Add logs if not present
				if (!allGameLogs.ContainsKey(gameKey))
				{
					foreach (AutomatedGameIteration iteration in gameEntry.Value.gameIterations)
					{
						allGameLogs.Add(gameKey, iteration.gameLogs);
					}
				}
			}

			// Overwrite Lobby logs in case there were new ones
			allGameLogs["Lobby"] = companion.lobbyLogs;
		}

		EditorGUILayout.BeginHorizontal();
		GUILayout.Label("All Logs", panelTitleStyle);
		GUILayout.FlexibleSpace();
		showAllLogsToggle = !(GUILayout.Toggle(!showAllLogsToggle, "Close Search", GUI.skin.button));
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		GUILayout.Label("Search Keywords: ");
		searchKeywords = GUILayout.TextField(searchKeywords, GUILayout.Width(200));
		GUILayout.FlexibleSpace();
		GUILayout.Label("Ignore Keywords: ");
		ignoreKeywords = GUILayout.TextField(ignoreKeywords, GUILayout.Width(200));
		EditorGUILayout.EndHorizontal();

		List<AutomatedCompanionLog> filteredList = new List<AutomatedCompanionLog>();
		string[] searchKeywordsList = searchKeywords.Split(' ');
		string[] ignoreKeywordsList = ignoreKeywords.Split(' ');

		searchResultsScrollPos = GUILayout.BeginScrollView(searchResultsScrollPos, false, true);
		foreach (KeyValuePair<string, List<AutomatedCompanionLog>> gameLogsEntry in allGameLogs)
		{
			filteredList = gameLogsEntry.Value;

			if (!string.IsNullOrEmpty(searchKeywords))
			{
				foreach (string searchKey in searchKeywordsList)
				{
					filteredList = filteredList.Where(logEntry => logEntry.logMessage.IndexOf(searchKey, System.StringComparison.OrdinalIgnoreCase) != -1).ToList();
				}
			}

			if (!string.IsNullOrEmpty(ignoreKeywords))
			{
				foreach (string ignoreKey in ignoreKeywordsList)
				{
					filteredList = filteredList.Where(logEntry => logEntry.logMessage.IndexOf(ignoreKey, System.StringComparison.OrdinalIgnoreCase) <= 0).ToList();
				}
			}

			foreach (AutomatedCompanionLog log in filteredList)
			{
				if (log != null)
				{
					if (GUILayout.Button(gameLogsEntry.Key + ": " + log.logTitle, getLogLabelStyleWithType(log.logType)))
					{
						selectedGameLog = log;
					}
				}
			}
		}
		GUILayout.EndScrollView();
	}

	// Draws the selection panel/group for the game and lobby logs or the panel for editing the keyword list.
	private void drawLogSelectionPanel()
	{

		// Display the header for editing the Keyword list.
		if (showRegexListPanelToggle)
		{
			GUILayout.Label("Edit Keyword List Panel (not sanitized)", panelTitleStyle);
			GUILayout.Label(string.Format("default file: {0}", TRAMPLogFiles.LADI_REGEX_FILE_DEFAULT), panelHeaderStyle);
			GUILayout.Label(string.Format("keyword string: {0}", selectedRegexUnion));
		}
		else if (showAllLogsToggle)
		{
			drawLogSearchPanel();
			return;
		}
		else
		{
			GUILayout.Label("Log Selection Panel", panelTitleStyle);
		}

		// Display the buttons for selecting and filtering.
		EditorGUILayout.BeginHorizontal();
		string showLobbyLogsToggleButtonText = showLobbyLogsToggle ? "Show Game Logs" : "Show Lobby Logs";
		showLobbyLogsToggle = GUILayout.Toggle(showLobbyLogsToggle, showLobbyLogsToggleButtonText, GUI.skin.button);
		useRegexList = GUILayout.Toggle(useRegexList, "Filter with Keyword list");
		string showRegexListButtonText = showRegexListPanelToggle ? "Show Log Selection Panel" : "Edit Keyword List";
		showRegexListPanelToggle = GUILayout.Toggle(showRegexListPanelToggle, showRegexListButtonText, GUI.skin.button);
		converseRegexListFilterToggle = GUILayout.Toggle(converseRegexListFilterToggle, "Filter Keyword List by exclusion");
		EditorGUILayout.EndHorizontal();

		// Draw either the game logs, lobby logs, or keyword list editor panels.
		if (showRegexListPanelToggle)
		{
			drawRegexListPanel();
		}
		else
		{
			// Display the options for filtering the log buttons.
			EditorGUILayout.BeginHorizontal();
			editedFilterKeyword = EditorGUILayout.TextField("Filter Keyword:", logFilterKeyword);
			if (editedFilterKeyword != logFilterKeyword)
			{
				logFilterKeyword = editedFilterKeyword;
				logFilterRegex = new Regex(logFilterKeyword);
			}
			filterLogsBySingleKeyword = GUILayout.Toggle(filterLogsBySingleKeyword, "Filter by single keyword");
			selectedLogFilterType = (AutomatedPlayerCompanionControlPanel.LogsFilterType) EditorGUILayout.EnumPopup("Filter by log type: ", selectedLogFilterType);
			if (GUILayout.Button("Apply Filter"))
			{
				// Filter either the lobby logs or the game logs depending on what the user selected.
				filterLogs(!showLobbyLogsToggle);
			}
			EditorGUILayout.EndHorizontal();

			// Display either lobby logs or game logs.
			if (showLobbyLogsToggle)
			{
				// Display the lobby log selection buttons
				GUILayout.Label("Lobby Logs", panelHeaderStyle);
				if (companion != null && lobbyLogs != null)
				{
					lobbyLogSelectionScrollPos = GUILayout.BeginScrollView(lobbyLogSelectionScrollPos, false, true);
					drawLogSelectionButtons(lobbyLogs);
					GUILayout.EndScrollView();
				}
				else
				{
					GUILayout.Label("No Lobby Logs to show");
				}
			}
			else if (selectedGame != null && selectedGame.commonGame != null)
			{
				// Display the logs for the currently selected game iteration.
				GUILayout.Label(string.Format("Game logs for ({0})", selectedGame.commonGame.gameKey), panelHeaderStyle);
				if (gameLogs != null)
				{
					gamelogSelectionScrollPos = GUILayout.BeginScrollView(gamelogSelectionScrollPos, false, true);
					drawLogSelectionButtons(gameLogs);
					GUILayout.EndScrollView();
				}
				else
				{
					Debug.LogErrorFormat("<color={0}>LADI> Game logs not initialized properly!</color>", AutomatedPlayerCompanion.LADI_DEBUG_COLOR);
				}
			}
		}
	}

	// Draw the buttons for the user to select a specific log (lobby or game).
	private void drawLogSelectionButtons(List<AutomatedCompanionLog> logList)
	{
		foreach (AutomatedCompanionLog log in logList)
		{
			if (log != null)
			{
				if (GUILayout.Button(log.logTitle, getLogLabelStyleWithType(log.logType)))
				{
					selectedGameLog = log;
				}
			}
		}
	}

	////////////////////////////////////////////////////////////////////
	//////////////////////     Helper Methods     //////////////////////
	////////////////////////////////////////////////////////////////////

	// Add a log to the lobby or games list. (called by LADI through a delegate).
	public static void addLog(AutomatedCompanionLog log, bool isLobbyLog)
	{
		if (log != null && instance.gameLogs != null && instance != null && filterSingleLog(log, instance.selectedLogFilterType, instance.filterLogsBySingleKeyword, instance.logFilterRegex))
		{
			if (isLobbyLog)
			{
				instance.lobbyLogs.Add(log);
			}
			else if (instance.isSelectedGameIterationActiveGame())
			{
				instance.gameLogs.Add(log);
			}
		}
	}

	// Add a game log to the gameLogs list. (called by LADI through a delegate).
	public static void addTestedGameButton(string gameKey)
	{
		if (gameKey != null && instance != null && instance.filterSingleTestedGameButton(gameKey))
		{
			if (instance.visibleTestedGameKeys != null && !instance.visibleTestedGameKeys.Contains(gameKey))
			{
				instance.visibleTestedGameKeys.Add(gameKey);
			}
			else if (instance.visibleTestedGameKeys == null)
			{
				instance.visibleTestedGameKeys = new List<string>();
				instance.visibleTestedGameKeys.Add(gameKey);
			}
			instance.filterTestedGameButtons();
		}
	}

	// Reloads LADI control panel from logs.
	// Useful if we open the control panel after TRAMP has already been running for a long time.
	private void reloadFromLog()
	{
		TRAMPLogFiles.loadCompanionFromFile();
	}

	// Set's the information for the current game with the given game key.
	private void setSelectedGame(string key = "", AutomatedGameIteration game = null)
	{
		selectedGame = null;

		if (game != null)
		{
			if (string.IsNullOrEmpty(key))
			{
				selectedGameKey = game.commonGame.gameKey;
			}
			else
			{
				selectedGameKey = key;
			}
			selectedGame = game;
			selectedGameStats = game.stats;
		}
		else
		{
			if (!string.IsNullOrEmpty(key))
			{
				AutomatedGame automatedGame = companion.getAutomatedGameByKey(key);
				selectedGame = automatedGame.getLatestIteration();
				selectedGameKey = key;
			}
		}
		filterLogs(true);

		if (selectedGame == null)
		{
			Debug.LogErrorFormat("<color={0}>LADI> Current game not set</color>", AutomatedPlayerCompanion.LADI_DEBUG_COLOR);
		}
	}
		
	// Sorts the buttons to select test games.
	private void sortTestedGameButtons(bool isConverse = false)
	{
		switch (selectedGameSortType)
		{
			case GameKeySortType.ALPHABET:
				visibleTestedGameKeys.Sort((a, b) => a.CompareTo(b));
				break;
			case GameKeySortType.SEVERITY:
				List<AutomatedGame> gamesBySeverity = new List<AutomatedGame>(companion.gamesTested.Values);
				gamesBySeverity.Sort(AutomatedGame.sortBySeverity);
				sortVisibleGamesWithGamesList(gamesBySeverity);
				break;
			case GameKeySortType.TEST_ORDER:
				List<AutomatedGame> gamesInTestOrder = new List<AutomatedGame>(companion.gamesTested.Values);
				gamesInTestOrder.Sort((a, b) => { if (a.getLatestIteration().stats.timeStarted > b.getLatestIteration().stats.timeStarted) return 1; else if (a.getLatestIteration().stats.timeStarted < b.getLatestIteration().stats.timeStarted) return -1; else return 0;});
				sortVisibleGamesWithGamesList(gamesInTestOrder);
				break;
			default:
				Debug.LogErrorFormat("<color={0}>LADI> Failed to sort game keys</color>", AutomatedPlayerCompanion.LADI_DEBUG_COLOR);
				break;
		}

		filterSortStatusString = string.Format("Showing {0} games, with filter {1}, sorted by {2}:", visibleTestedGameKeys.Count, selectedGameFilterType.ToString(), selectedGameSortType.ToString());
	}

	// Filter the buttons for tested game iterations.
	private void filterTestedGameButtons(bool isConverse = false)
	{
		if (companion == null || companion.gamesTested == null)
		{
			Debug.LogErrorFormat("<color={0}>LADI> Failed to filter game keys, LADI is null</color>", AutomatedPlayerCompanion.LADI_DEBUG_COLOR);
			return;
		}

		switch (selectedGameFilterType)
		{
			case GameKeyFilterType.KEYWORD:
				visibleTestedGameKeys = new List<string>();
				Regex expression = new Regex(gameFilterKeyword, RegexOptions.IgnoreCase);
				foreach (string gameKey in companion.gamesTested.Keys)
				{
					LobbyGame lobbyGame = LobbyGame.find(gameKey);
					// Match the keyword with both the game key and the game name. 
					if (expression.IsMatch(gameKey))
					{
						visibleTestedGameKeys.Add(gameKey);
					}
					else if(lobbyGame != null && expression.IsMatch(lobbyGame.name))
					{
						visibleTestedGameKeys.Add(gameKey);
					}
				}
				break;
			case GameKeyFilterType.LOG_MESSAGE:
				visibleTestedGameKeys = new List<string>();

				// Iterate through each game tested by TRAMP.
				foreach (AutomatedGame game in companion.gamesTested.Values)
				{
					// Check each iteration of the game.
					foreach (AutomatedGameIteration itr in game.gameIterations)
					{
						// Check all of that iterations logs.
						foreach (AutomatedCompanionLog log in itr.gameLogs)
						{
							// If the game hasn't already been added and the log contains message.
							if (!visibleTestedGameKeys.Contains(game.gameKey) && log.logMessage.ToLower().Contains(gameFilterLogMessage.ToLower()))
							{
								visibleTestedGameKeys.Add(game.gameKey);
							}
						}
					}
				}
				break;
			case GameKeyFilterType.EXCEPTIONS:
				visibleTestedGameKeys = new List<string>(companion.getTestedGameKeysWithLogs(LogType.Exception));
				break;
			case GameKeyFilterType.ERRORS:
				visibleTestedGameKeys = new List<string>(companion.getTestedGameKeysWithLogs(LogType.Error));
				break;
			case GameKeyFilterType.WARNINGS:
				visibleTestedGameKeys = new List<string>(companion.getTestedGameKeysWithLogs(LogType.Warning));
				break;
			case GameKeyFilterType.NORMAL:
				visibleTestedGameKeys = new List<string>(companion.getTestedGameKeysWithoutIssues());
				break;
			case GameKeyFilterType.NONE:
			default:
				visibleTestedGameKeys = new List<string>(companion.gamesTested.Keys);
				break;
		}
		// Re-Sort the list
		sortTestedGameButtons();
	}

	// Filter a single button for a tested game iteration according to the selected GameKeyFilterType.
	private bool filterSingleTestedGameButton(string gameKey)
	{
		if (companion == null || companion.gamesTested == null)
		{
			return false;
		}

		// We only want to filter games that have already been tested.
		AutomatedGame filterAutomatedGame = companion.getGameTested(gameKey);
		if (filterAutomatedGame != null)
		{
			switch (selectedGameFilterType)
			{
				case GameKeyFilterType.KEYWORD:
					Regex expression = new Regex(gameFilterKeyword);
					return expression.IsMatch(gameKey);
				case GameKeyFilterType.EXCEPTIONS:
					return filterAutomatedGame.averageStats.hasLogsOfType(LogType.Exception);
				case GameKeyFilterType.ERRORS:
					return filterAutomatedGame.averageStats.hasLogsOfType(LogType.Error);
				case GameKeyFilterType.WARNINGS:
					return filterAutomatedGame.averageStats.hasLogsOfType(LogType.Warning);
				default:
					return true;
			}
		}
		return false;
	}

	// Calls the load regex list from file and unions the regular expressions into one.
	private void loadSelectedRegex()
	{
		selectedRegexList = loadRegexListFromFile(regexListFileName);
		applyRegexList();
	}

	// Convert the list of regular expressions into a single regular expression.
	private void applyRegexList()
	{
		selectedRegexUnion = new Regex(unionRegexList(selectedRegexList), RegexOptions.IgnoreCase);
	}

	// Calls the load regex list from file and unions the regular expressions into one.
	private void saveSelectedRegexList()
	{
		string RegexListPath = System.IO.Path.Combine(TRAMPLogFiles.LADI_REGEX_DIRECTORY, regexListFileName);
		try
		{
			if (!System.IO.Directory.Exists(TRAMPLogFiles.LADI_REGEX_DIRECTORY))
			{
				System.IO.Directory.CreateDirectory(TRAMPLogFiles.LADI_REGEX_DIRECTORY);
			}
			saveRegexListToJson(RegexListPath, selectedRegexList);
		}
		catch (System.Exception exception)
		{
			Debug.LogErrorFormat("<color={0}>TRAMP> Can't save REGEX to file: {1}. Exception: {2}</color>",
				AutomatedPlayer.TRAMP_DEBUG_COLOR, RegexListPath, exception.Message);
		}
	}

	// TODO perhaps put this in a common class
	// Loads a list regular expressions from a JSON file to be used in filtering logs.
	// Returns an empty regex list on failure.
	private List<string> loadRegexListFromFile (string fileName)
	{
		JSON jsonString = null;
		if (!string.IsNullOrEmpty(fileName))
		{
			string filePath = System.IO.Path.Combine(TRAMPLogFiles.LADI_REGEX_DIRECTORY, fileName);
			try
			{
				if (System.IO.Directory.Exists(TRAMPLogFiles.LADI_REGEX_DIRECTORY))
				{
					if (System.IO.File.Exists(filePath))
					{
						jsonString = new JSON(System.IO.File.ReadAllText(filePath));
					}
				}
			}
			catch (System.Exception exception)
			{
				Debug.LogErrorFormat("<color={0}>LADI> Can't load REGEX from file: {1}. Exception: {2}</color>",
					AutomatedPlayerCompanion.LADI_DEBUG_COLOR, filePath, exception.Message);
				return null;
			}
		}

		if (jsonString != null)
		{
			return new List<string>(jsonString.getStringArray("regexList"));
		}
		else
		{
			return new List<string>();
		}
	}

	// TODO perhaps put this in a common class?
	// Returns a union of regular expressions (unsanitized).
	// Returns null if the regex list is null or the list is empty.
	private string unionRegexList(List<string> regexList)
	{
		if (regexList == null || regexList.Count == 0)
		{
			return "";
		}
		StringBuilder regexStringBuilder = new StringBuilder("");
		regexStringBuilder.Append(regexList[0]);
		for (int i = 1; i < regexList.Count; i++)
		{
			if (!string.IsNullOrEmpty(regexList[i]))
			{
				regexStringBuilder.AppendFormat("|{0}",regexList[i]);
			}
		}
		return regexStringBuilder.ToString();
	}

	// TODO perhaps put this in a common class?
	// Saves a list of regular expressions into a JSON file as an array to be used in filtering logs.
	private void saveRegexListToJson (string fileName, List<string> regexList)
	{
		if (regexList == null || regexList.Count == 0)
		{
			return;
		}

		StringBuilder build = new StringBuilder("{\"regexList\":[");
		build.AppendFormat("\"{0}\"", JSON.sanitizeString(regexList[0]));
		for (int i = 1; i < regexList.Count; i++)
		{
			build.AppendFormat(",\"{0}\"", regexList[i]);
		}
		build.Append("]}");

		try
		{
			System.IO.File.WriteAllText(fileName, build.ToString());
		}
		catch (System.Exception exception)
		{
			Debug.LogErrorFormat("Can't save REGEX to file: {1}. Exception: {2}</color>",
				AutomatedPlayer.TRAMP_DEBUG_COLOR, fileName, exception.Message);
		}
	}

	// Filter either the control panel's game logs or lobby logs.
	private void filterLogs(bool filterGameLogs)
	{
		List<AutomatedCompanionLog> tempGameLogs;
		if (filterGameLogs)
		{		
			tempGameLogs = new List<AutomatedCompanionLog>(selectedGame.gameLogs);
		}
		else
		{
			tempGameLogs = new List<AutomatedCompanionLog>(companion.lobbyLogs);
		}

		// Filter the logs by the single keyword
		if (filterLogsBySingleKeyword)
		{
			tempGameLogs = filterLogsByRegex(tempGameLogs, logFilterRegex, false);
		}

		// Filter the logs by the keyword listt
		if (useRegexList)
		{
			tempGameLogs = filterLogsByRegex(tempGameLogs, selectedRegexUnion, converseRegexListFilterToggle);
		}

		// Filter the logs by type.
		if (filterGameLogs)
		{
			gameLogs = filterLogsByType(tempGameLogs, selectedLogFilterType);
		}
		else
		{
			lobbyLogs = filterLogsByType(tempGameLogs, selectedLogFilterType);
		}
	}

	// Filter a list of logs by a regular expression.
	private static List<AutomatedCompanionLog> filterLogsByRegex(List<AutomatedCompanionLog> passedLogs,
		Regex expression, 
		bool isConverse)
	{
		if (passedLogs != null)
		{
			if (expression != null)
			{
				List<AutomatedCompanionLog> filteredLogs = new List<AutomatedCompanionLog>();
				foreach(AutomatedCompanionLog log in passedLogs)
				{
					if (expression.IsMatch(log.logMessage))
					{
						if (!isConverse)
						{
							filteredLogs.Add(log);
						}
					}
					else if (isConverse)
					{
						filteredLogs.Add(log);
					}
				}
				return filteredLogs;
			}
			else
			{
				return new List<AutomatedCompanionLog>(passedLogs);
			}
		}
		return null;
	}

	// Fitler a list of logs by type.
	private static List<AutomatedCompanionLog> filterLogsByType(List<AutomatedCompanionLog> tempGameLogs,
		LogsFilterType filterType,
		bool isConverse = false)
	{
		if (tempGameLogs == null)
		{
			Debug.LogErrorFormat("<color={0}>LADI> Cannot filter logs</color>",AutomatedPlayerCompanion.LADI_DEBUG_COLOR);
			return null;
		}
		List<AutomatedCompanionLog> filteredLogs = new List<AutomatedCompanionLog>();
		switch(filterType)
		{
			case LogsFilterType.ERRORS:
				foreach (AutomatedCompanionLog log in tempGameLogs)
				{
					if (log.logType == LogType.Error)
					{
						filteredLogs.Add(log);
					}
				}
				break;
			case LogsFilterType.WARNINGS:
				foreach (AutomatedCompanionLog log in tempGameLogs)
				{
					if (log.logType == LogType.Warning)
					{
						filteredLogs.Add(log);
					}
				}
				break;
			case LogsFilterType.EXCEPTIONS:
				foreach (AutomatedCompanionLog log in tempGameLogs)
				{
					if (log.logType == LogType.Exception)
					{
						filteredLogs.Add(log);
					}
				}
				break;
			//case LogsFilterType.NONE:
			//case LogsFilterType.KEYWORD:
			default:
				filteredLogs = tempGameLogs;
				break;
		}
		return filteredLogs;
	}

	// Filter an individual log. Does not go through the regex list for performance reasons.
	private static bool filterSingleLog(AutomatedCompanionLog log, 
		LogsFilterType filterType,
		bool useRegex,
		Regex logFilterRegexKeyword)
	{
		if (useRegex && logFilterRegexKeyword != null)
		{
			if (!logFilterRegexKeyword.IsMatch(log.logMessage))
			{
				return false;
			}
		}

		switch(filterType)
		{
			case LogsFilterType.ERRORS:
				return log.logType == LogType.Error;
			case LogsFilterType.WARNINGS:
				return log.logType == LogType.Warning;
			case LogsFilterType.EXCEPTIONS:
				return log.logType == LogType.Exception;
			//case LogsFilterType.NONE:
			default:
				return true;
		}
	}

	// Draws all colliders in the scene.
	// ColliderVisualizer must be enabled for this to work.
	private void drawAllColliders()
	{
		Collider [] collidersInScene = (Collider []) GameObject.FindObjectsOfType(typeof(Collider));
		foreach (Collider colliderObject in collidersInScene)
		{
			if (colliderObject != null)
			{
				ColliderVisualizer.drawVisualCollider(colliderObject);
			}
		}
	}

	// Sorts visibleTestedGameKeys according to the order of an AutomatedGame list.
	// Assumes all games in visibleTestedGameKeys is in the passed AutomatedGame list.
	private void sortVisibleGamesWithGamesList(List<AutomatedGame> gamesList)
	{
		int curVisibleIndex = 0;
		foreach (AutomatedGame game in gamesList)
		{
			int index = visibleTestedGameKeys.FindIndex( (string s) => { return s == game.gameKey; } );
			if (index >= 0)
			{
				// Swap games that are not in their test order position
				if (index < visibleTestedGameKeys.Count)
				{
					visibleTestedGameKeys[index] = visibleTestedGameKeys[curVisibleIndex];
					visibleTestedGameKeys[curVisibleIndex] = game.gameKey;
					curVisibleIndex++;
				}
				else
				{
					Debug.LogErrorFormat("<color={0}>LADI> Failed to sort game keys: index out of bounds</color>", AutomatedPlayerCompanion.LADI_DEBUG_COLOR);
				}
			}
		}
	}

	public bool isSelectedGameIterationActiveGame()
	{
		if (companion != null && companion.activeGame != null)
		{
			return selectedGame == companion.activeGame;
		}
		return false;
	}

	public GUIStyle getLogLabelStyleWithType(LogType logType)
	{
		if (logType == LogType.Error)
		{
			return redTextStyle;
		}
		else if (logType == LogType.Exception)
		{
			return pinkTextStyle;
		}
		else if (logType == LogType.Warning)
		{
			return yellowTextStyle;
		}
		else
		{
			return blueTextStyle;
		}
	}

	#else
	void OnGUI()
	{
		GUILayout.Label("TRAMP disabled.");
		if (GUILayout.Button("Close"))
		{
			this.Close();
		}
	}
	#endif
}
