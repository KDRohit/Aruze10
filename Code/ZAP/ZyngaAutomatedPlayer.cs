using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Zap.Automation
{
#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	using Newtonsoft.Json;
	
	public class ZyngaAutomatedPlayer : TICoroutineMonoBehaviour, IResetGame
	{
		#region properties
		private const float MAXIMUM_ACTION_TIME = 5.0f;

		public int automatableIndex = 0;

		public bool _shouldClearDialogs = true;
		public bool shouldClearDialogs
		{
			get
			{
				return _shouldClearDialogs;
			}
			set
			{
				if (value != _shouldClearDialogs)
				{
					Debug.LogFormat("ZAPLOG -- changing shouldClearDialogs to {0}", value);
				}
				_shouldClearDialogs = value;
			}
		}

		private bool _isAllowingEditorPause = false;
		public bool isAllowingEditorPause
		{
			get { return _isAllowingEditorPause; }
			set
			{
				_isAllowingEditorPause = value;
				SlotsPlayer.getPreferences().SetBool(ZAPPrefs.IS_ALLOWING_EDITOR_PAUSE, _isAllowingEditorPause);
			}
		}

		private bool _isUsingRandomWagersForSpins = true;
		public bool isUsingRandomWagersForSpins
		{
			get { return _isUsingRandomWagersForSpins; }
			set
			{
				_isUsingRandomWagersForSpins = value;
				SlotsPlayer.getPreferences().SetBool(ZAPPrefs.IS_USING_RANDOM_WAGERS_FOR_SPINS, _isUsingRandomWagersForSpins);
			}
		}

		private bool _testing = false;
		public bool testing
		{
			get { return _testing; }
			set { }
		}

		private bool _pauseTesting = false;
		public bool pauseTesting
		{
			get { return _pauseTesting; }
			set
			{
				//If we pause ZAP we want to stop it from closing dialogs as well
				if (hasCoroutine("checkForDialogs"))
				{
					if (value)
					{
						pauseCoroutine("checkForDialogs");
					}
					else
					{
						resumeCoroutine("checkForDialogs");
					}
				}
				_pauseTesting = value;
			}
		}

		public string sessionStartTime = "";
		public string sessionEndTime = "";

		public TestPlan currentTestPlan;
		public string zapSummarySaveLocation = "";

		private Automatable _currentAutomatable;
		public Automatable currentAutomatable
		{
			get {return _currentAutomatable;}
			set {}
		}

		public enum ZAPState
		{
			IDLE,
			STARTING,
			TESTING_SLOT_GAME,
			TESTING_FEATURE,
			TESTING_COMPLETE
		};

		public ZAPState zapState = ZAPState.IDLE;

		private static ZyngaAutomatedPlayer _instance;
		public static ZyngaAutomatedPlayer instance
		{
			get
			{
				if (Application.isPlaying)
				{
					if (_instance != null)
					{
						return _instance;
					}
					else
					{
						_instance = new GameObject("ZAP").AddComponent<ZyngaAutomatedPlayer>();
						hasBeenSetup = true;
						DontDestroyOnLoad(_instance.gameObject);
						return _instance;
					}
				}
				else
				{
					return _instance;
				}
			}
			private set
			{
				_instance = value;
			}
		}

		public static bool hasBeenSetup = false;

		public TestPlanResults results;
#endregion

		void Awake()
		{
			ActionController.deserialize();
			_instance = this;
			_isAllowingEditorPause = SlotsPlayer.getPreferences().GetBool(ZAPPrefs.IS_ALLOWING_EDITOR_PAUSE, false);
			_isUsingRandomWagersForSpins = SlotsPlayer.getPreferences().GetBool(ZAPPrefs.IS_USING_RANDOM_WAGERS_FOR_SPINS, true);
#if UNITY_EDITOR
			UnityEditor.EditorApplication.playModeStateChanged += onUnityPlayModeChanged;
			UnityEditor.EditorApplication.pauseStateChanged += onUnityEditorPauseChanged;
#endif
		}

		private void OnDestroy()
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.playModeStateChanged -= onUnityPlayModeChanged;
			UnityEditor.EditorApplication.pauseStateChanged -= onUnityEditorPauseChanged;
#endif
		}

		public static bool restartAutomation = false;

#if UNITY_EDITOR
		public static void onUnityPlayModeChanged(PlayModeStateChange state)
		{
			if (state == PlayModeStateChange.ExitingPlayMode)
			{
				if (instance.results != null)
				{
					instance.results.saveToFile();
				}

				if (instance.currentAutomatable != null)
				{
					Debug.LogError("User is stopping Unity! ZAP State recorded - [Current Automatable : " + instance.currentAutomatable.key + "] [Current Test Index : " + instance.currentAutomatable.testIndex + "]");

					if (instance.currentAutomatable.tests.Count - 1 == instance.currentAutomatable.testIndex)
					{
						//We broke on the last test in the automatables list, lets increase the automatable index by one 
						Debug.LogError("ZAP State: Broke on final test of automatable [" + instance.currentAutomatable.key + "] moving to the next automatable on resume, and reseting the test index to 0");
						instance.automatableIndex = SlotsPlayer.getPreferences().GetInt(ZAPPrefs.CURRENT_AUTOMATABLE_INDEX_KEY, 0);
						instance.automatableIndex++;
						SlotsPlayer.getPreferences().SetInt(ZAPPrefs.CURRENT_AUTOMATABLE_INDEX_KEY, instance.automatableIndex);
						SlotsPlayer.getPreferences().SetInt(ZAPPrefs.CURRENT_TEST_INDEX_KEY, 0);
					}
				}
			}
		}

		public static void onUnityEditorPauseChanged(PauseState state)
		{
			if (state == PauseState.Paused)
			{
				if (instance.currentAutomatable != null || instance.zapState == ZAPState.STARTING)
				{
					if (!instance._isAllowingEditorPause)
					{
						UnityEditor.EditorApplication.isPaused = false;
						Debug.LogFormat("ZAPLOG -- Automatically unpaused the game.");
					}
				}
			}
		}
#endif

		public bool wait()
		{
			if (_pauseTesting)
			{
				return true;
			}

			// Used for multiple zap types.
			SlotBaseGame baseGame = ReelGame.activeGame as SlotBaseGame;
			switch (zapState)
			{
				case ZAPState.TESTING_SLOT_GAME:
					if ((baseGame == null || baseGame.isGameBusy) || CommonAutomation.IsDialogActive())
					{
						return true;
					}
					break;
				case ZAPState.TESTING_FEATURE:
					// Features usually need to interact with dialogs, so let them handle whether those are open.
					// Only wait on the slot game spinning.
					if ((baseGame == null || baseGame.isGameBusy))
					{
						return true;
					}
					break;
			}
			return false;
		}

		public void setupForResume()
		{
			Debug.LogFormat("ZAPLOG -- Detected a game reset, turning on resume.");
			// If we were automated and a reset game in, we should try to resume afterwards.
			Zynga.Core.Util.PreferencesBase prefs = SlotsPlayer.getPreferences();
			prefs.SetInt(ZAPPrefs.SHOULD_RESUME, 1);
			prefs.Save();
		}

		//TODO: Cant have this on while in State.Feature
		private IEnumerator checkForDialogs()
		{
			Debug.LogFormat("ZAPLOG -- Kicking off checkForDialog(), dialogs will now autoclose while shouldClearDialogs is true");
			yield return null;
			while (!Glb.isResetting) // Break out if we are resetting the game.
			{
				if (shouldClearDialogs)
				{
					yield return StartCoroutine(CommonAutomation.automateClearDialogsAndShrouds());
				}
				else
				{
					yield return null;
				}
			}
		}

		public void skipAutomatable()
		{
			Debug.LogFormat("ZAPLOG -- Skipping automatable.");
			if (results != null)
			{
				results.saveToFile();
			}
			else
			{
				// We dont want to crash but we should error here.
				Debug.LogErrorFormat("ZyngaAutomatedPlayer.cs -- skipAutomatable() -- could not find results to save to file.");
			}

			if (currentAutomatable != null)
			{
				Debug.LogError("ZAP Exception stopping Unity! ZAP State recorded - [Current Automatable : " + currentAutomatable.key + "] [Current Test Index : " + currentAutomatable.testIndex + "]");

				automatableIndex = SlotsPlayer.getPreferences().GetInt(ZAPPrefs.CURRENT_AUTOMATABLE_INDEX_KEY, 0);
				automatableIndex++;
				SlotsPlayer.getPreferences().SetInt(ZAPPrefs.CURRENT_AUTOMATABLE_INDEX_KEY, automatableIndex);
				
				// Setup ZAP to resume after the Editor stops and restarts
				// The restart occurs from ZAPAutoRestart
				setupForResume();
				UnityEditor.EditorApplication.isPlaying = false;
			}
		}

		public TestPlan loadTestPlanFromPrefs()
		{
			TestPlan testPlan = null;
			string testPlanJSON = SlotsPlayer.getPreferences().GetString(ZAPPrefs.TEST_PLAN_JSON, "");
			if (testPlanJSON.Length != 0)
			{
				testPlan = JsonConvert.DeserializeObject<TestPlan>(testPlanJSON, new JsonSerializerSettings
				{
					TypeNameHandling = TypeNameHandling.All
				});
			}
			return testPlan;
		}

		public void resumeAutomation()
		{
			zapState = ZAPState.STARTING;

			//Deserialize results
			TestPlanResults.directoryPath = SlotsPlayer.getPreferences().GetString(ZAPPrefs.TEST_RESULTS_FOLDER_KEY);
			string testPlanResultsFile = TestPlanResults.directoryPath + "Results.json";
			if (!string.IsNullOrEmpty(testPlanResultsFile))
			{
				results = JsonConvert.DeserializeObject<TestPlanResults>(File.ReadAllText(testPlanResultsFile), new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
			}
			if (results != null)
			{
				currentTestPlan = results.testPlan;
				Debug.LogFormat("ZAPLOG -- resuming test plan: {0}", currentTestPlan.name);
				automatableIndex = SlotsPlayer.getPreferences().GetInt(ZAPPrefs.CURRENT_AUTOMATABLE_INDEX_KEY, 0);
				StartCoroutine(runCurrentTestPlan());
			}
			else
			{
				Debug.LogErrorFormat("ZAPLOG -- resumeAutomation() -- failed to find result file, starting automation stored in player prefs");
				startAutomation();
			}
		}

		// Does some initial setup for running through a test plan.
		// We create the results, create the directories and reset the testing indicies.
		public void startAutomation()
		{
			if (currentTestPlan == null)
			{
				Debug.LogFormat("ZAPLOG -- No test plan setup when starting automation, trying to load from player prefs.");
				currentTestPlan = loadTestPlanFromPrefs();
				if (currentTestPlan == null)
				{
					// If it is still null, then lets yell and bail.
					Debug.LogFormat("ZAPLOG -- Failed to load a test plan from player prefs! Something is not right here... bailing");
					closeEditorIfDesired(1);
					return;
				}
			}
			zapState = ZAPState.STARTING;
			automatableIndex = 0;
			SlotsPlayer.getPreferences().SetInt(ZAPPrefs.CURRENT_TEST_INDEX_KEY, 0);
			SlotsPlayer.getPreferences().SetInt(ZAPPrefs.CURRENT_AUTOMATABLE_INDEX_KEY, 0);
			//Set any pertinent results data on start
			results = new TestPlanResults();
			results.testPlan = currentTestPlan;
			results.gitBranch = AutomatedPlayerProcesses.getBranchName();
			if (SlotsPlayer.instance.socialMember != null)
			{
				results.playerZID = SlotsPlayer.instance.socialMember.zId;
				results.playerStartingCredits = SlotsPlayer.instance.socialMember.credits;
			}
			results.testPlanStartTime = DateTime.UtcNow;

			string folderName = "ZAPRun-" + results.testPlanStartTime.ToString("yyyy-dd-M--HH-mm-ss");
			TestPlanResults.directoryPath = ZAPFileHandler.getZapResultsFileLocation() + "/" + folderName + "/";
			Directory.CreateDirectory(TestPlanResults.directoryPath);

			sessionStartTime = DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString();
			StartCoroutine(runCurrentTestPlan());
		}

		// Run through the test plan.
		private IEnumerator runCurrentTestPlan()
		{
			Debug.LogFormat("ZAPLOG -- running the test plan!");
			_testing = true;
			Application.logMessageReceived += ZAPLogHandler.handleLog;
			
			// Do an initial results save to make sure that the location for results
			// exists so that things will not break if we don't finish the first automatable
			results.saveToFile();
			
			for (int index = automatableIndex; index < currentTestPlan.automatables.Count; index++)
			{
				yield return StartCoroutine(runAutomatable(index));
			}

			Application.logMessageReceived -= ZAPLogHandler.handleLog;

			sessionEndTime = DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString();
			_testing = false;
			zapState = ZAPState.TESTING_COMPLETE;

			//Set the finaly results data now that we finished our testplan
			results.playerEndingCredits = SlotsPlayer.instance.socialMember.credits;
			results.testPlanEndTime = DateTime.UtcNow;

			TimeSpan timeSpan = results.testPlanEndTime - results.testPlanStartTime;
			results.testPlanRunTime = string.Format("{0} hours, {1} minutes, {2} seconds, {3} milliseconds", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);

			results.warningCount = ZAPLogHandler.warningCount;
			results.errorCount = ZAPLogHandler.errorCount;
			results.exceptionCount = ZAPLogHandler.exceptionCount;

			results.saveToFile();

			//We have finished the entire ZAP run reset our state prefs
			SlotsPlayer.getPreferences().SetString(ZAPPrefs.TEST_RESULTS_FOLDER_KEY, TestPlanResults.directoryPath);
			SlotsPlayer.getPreferences().SetInt(ZAPPrefs.CURRENT_AUTOMATABLE_INDEX_KEY, 0);
			SlotsPlayer.getPreferences().SetInt(ZAPPrefs.CURRENT_TEST_INDEX_KEY, 0);

			// Lets wait a frame to make sure everything finishes.
			yield return null;

			postRunCleanup(); // Do any cleanup we want to after finishing a test.
			closeEditorIfDesired(0);

			Debug.LogFormat("ZAPLOG -- End of test plan!");
		}

		private IEnumerator runAutomatable(int index)
		{
			Debug.LogFormat("ZAPLOG -- running automatable at index: {0}", index);
			Automatable automatable = currentTestPlan.automatables[index];
			SlotsPlayer.getPreferences().SetInt(ZAPPrefs.CURRENT_AUTOMATABLE_INDEX_KEY, index);
			automatable.automatableResult = new AutomatableResult(automatable);
			automatable.automatableResult.startTime = DateTime.UtcNow;
			while (SlotsPlayer.instance == null || SlotsPlayer.instance.socialMember == null)
			{
				// Wait for the player to be instantiated.
				yield return null;
			}

			automatable.automatableResult.startingCredits = SlotsPlayer.instance.socialMember.credits;

			bool isAutoClosingDialogs = false;
			switch (automatable.GetType().Name)
			{
				case "AutomatableSlotBaseGame":
					zapState = ZAPState.TESTING_SLOT_GAME;
					isAutoClosingDialogs = true;
					StartCoroutine("checkForDialogs", checkForDialogs());
					break;
				case "AutomatableDialog":
				case "AutomatableTestSetup":
					zapState = ZAPState.TESTING_FEATURE;
					break;
				default:
					zapState = ZAPState.IDLE;
					break;
			}

			_currentAutomatable = automatable;

			yield return StartCoroutine(automatable.startTests());

			// If we started the routine to auto close dialogs, kill it now.
			if (isAutoClosingDialogs)
			{
				StopCoroutine("checkForDialogs");
			}

			automatable.automatableResult.endTime = DateTime.UtcNow;
			automatable.automatableResult.endingCredits = SlotsPlayer.instance.socialMember.credits;

			TimeSpan automatableTimeSpan = automatable.automatableResult.endTime - automatable.automatableResult.startTime;
			results.testPlanRunTime = string.Format("{0} hours, {1} minutes, {2} seconds, {3} milliseconds", automatableTimeSpan.Hours, automatableTimeSpan.Minutes, automatableTimeSpan.Seconds, automatableTimeSpan.Milliseconds);

			results.automatableResults.Add(automatable.automatableResult);
			automatable.saveResult();
			results.saveToFile();
			if (automatable.GetType().Name == "AutomatableTestSetup")
			{
				// Since we are about to reset, set our next index
				SlotsPlayer.getPreferences().SetInt(ZAPPrefs.CURRENT_AUTOMATABLE_INDEX_KEY, index + 1);
				automatable.onTestsFinished();
				while (Glb.isResetting)
				{
					// Then wait for the reset to kill this coroutine.
					yield return null;
				}
			}
		}

		private void postRunCleanup()
		{
			// If this was a jenkins run, then we want to parse the results and create a summary.
			if (results != null)
			{
				string summary = results.getSummary();
				string summarySavePath = "zap_test_summary.txt";
				#if UNITY_EDITOR
				if (SlotsPlayer.getPreferences().GetInt(DebugPrefs.STARTED_FROM_COMMAND_LINE, 0) == 1)
				{
					// Override the save path to the location specified in the command line arguments.
					summarySavePath = string.IsNullOrEmpty(zapSummarySaveLocation) ? summarySavePath : zapSummarySaveLocation;
				}
				#endif
				System.IO.StreamWriter ws = new System.IO.StreamWriter(summarySavePath);
				ws.Write(summary);
			}
		}

		private void closeEditorIfDesired(int exitCode)
		{
			#if UNITY_EDITOR
			// Only close the editor if we are in the editor.
			if (SlotsPlayer.getPreferences().GetInt(DebugPrefs.STARTED_FROM_COMMAND_LINE, 0) == 1)
			{
				// And only do this if running from command line.
				Debug.LogErrorFormat("ZyngaAutomatedPlayer.cs -- runAutomatable() -- Exiting UNITY now that ZAP is finished!");
				UnityEditor.EditorApplication.Exit(exitCode);
			}
			#endif
		}

		public static void resetStaticClassData()
		{
			// Remove the reference so that we generate a new one.
			_instance = null;
			hasBeenSetup = false;
		}
	}
#endif
}
