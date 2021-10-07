using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Zap.Automation;

namespace Zap.Automation.Editor
{
	#if UNITY_EDITOR && !ZYNGA_PRODUCTION	
	public class ZAPObserverWindow : EditorWindow, IHasCustomMenu
	{
		private Automatable currentAutomatable = null;

		private AutomatableResult automatableResult;
		private TestResult testResult;
		private ZapLog selectedLog;

		#region OnGUI control variables
		private int testGridInt = 0;
		private Vector2 leftScrollPosition = Vector2.zero;
		private Vector2 rightScrollPosition = Vector2.zero;
		private Vector2 logScrollPosition = Vector2.zero;
		private Dictionary<string, TopBarButtonMethod> topBarButtonsMap = new Dictionary<string, TopBarButtonMethod>();
		private int selectedTestType = 0;
		private List<string> currentAutomatableTests = new List<string>();
		private Dictionary<string, Type> testTypeMap = new Dictionary<string, Type>();
		private Dictionary<ZapLogType, bool> logFilters = new Dictionary<ZapLogType, bool>()
		{
			{ ZapLogType.Warning, true },
			{ ZapLogType.Error, true },
			{ ZapLogType.Exception, true },
			{ ZapLogType.Outcome, true },
			{ ZapLogType.Desync, true }
		};
		private GUIStyle logStyle;
		
		private string startingCredits = null;
		private bool didLoadingHappen = false;
		private static bool isAllowingEditorPause = false;
		private static bool isUsingRandomWagersForSpins = true;
		#endregion OnGUI control variables

		#region Unity EditorWindow
		[MenuItem("Zynga/ZAP/Test Observer", false, 406)]
		public static void showWindow()
		{
			// try to dock next to Scene window
			EditorWindow[] windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
			EditorWindow setUpWindow = windows.FirstOrDefault(e => e.titleContent.text.Contains("ZAP Setup"));

			if (setUpWindow != null)
			{
				GetWindow<ZAPObserverWindow>("ZAP Observer", false, setUpWindow.GetType());
			}
			else
			{
				GetWindow<ZAPObserverWindow>("ZAP Observer", false);
			}
			
			isAllowingEditorPause = SlotsPlayer.getPreferences().GetBool(ZAPPrefs.IS_ALLOWING_EDITOR_PAUSE, false);
			isUsingRandomWagersForSpins = SlotsPlayer.getPreferences().GetBool(ZAPPrefs.IS_USING_RANDOM_WAGERS_FOR_SPINS, true);
		}

		public void AddItemsToMenu(GenericMenu menu)
		{
		}
		#endregion Unity EditorWindow 

		private void OnEnable()
		{
			//Get all Test subclasses
			testTypeMap = new Dictionary<string, Type>();
			List<Type> testSubclasses = Assembly.GetAssembly(typeof(Test)).GetTypes().Where(t => t.IsSubclassOf(typeof(Test))).ToList();
			foreach (Type t in testSubclasses)
			{
				testTypeMap.Add(t.Name, t);
			}

			currentAutomatable = CreateInstance<AutomatableSlotBaseGame>();
		}

		private void Update()
		{
			if (ZyngaAutomatedPlayer.instance != null)
			{
				if (currentAutomatable != ZyngaAutomatedPlayer.instance.currentAutomatable)
				{
					currentAutomatable = ZyngaAutomatedPlayer.instance.currentAutomatable;
				}
			}
			else
			{
				didLoadingHappen = false;
			}

			if (SlotsPlayer.instance.socialMember != null && string.IsNullOrEmpty(startingCredits))
			{
				startingCredits = SlotsPlayer.instance.socialMember.credits.ToString();
			}
		}
		
		private void OnInspectorUpdate()
		{
			Repaint();
		}

		private TestPlan loadTestPlan()
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

		#region OnGUI
		private void OnGUI()
		{
			string info = (currentAutomatable != null) ? ("Current Automatable: " + currentAutomatable.key) : ("ZAP not running");
			ZAPEditorHelpers.drawTopBar(info, topBarButtonsMap);
			EditorGUILayout.Space();
			//The layout for the entire window
			EditorGUILayout.BeginVertical(GUILayout.MinHeight(300));

			//Session Data Window
			EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
			EditorGUILayout.BeginVertical();
			EditorGUILayout.LabelField("Session Information", EditorStyles.toolbar);			
			if (ZyngaAutomatedPlayer.instance != null && ZyngaAutomatedPlayer.instance.results != null)
			{
				TestPlanResults results = ZyngaAutomatedPlayer.instance.results;				
				EditorGUILayout.LabelField("Player ZID: " + results.playerZID);
				EditorGUILayout.LabelField("GIT branch: " + results.gitBranch);
				EditorGUILayout.LabelField("Start Time: " + results.testPlanStartTime);
				EditorGUILayout.LabelField("End Time: " + results.testPlanEndTime);
				EditorGUILayout.LabelField("Run Time: " + results.testPlanRunTime);
				EditorGUILayout.LabelField("Starting Credits: " + results.playerStartingCredits);
				EditorGUILayout.LabelField("Ending Credits: " + results.playerEndingCredits);
				EditorGUILayout.LabelField("Warning Count: " + results.warningCount);
				EditorGUILayout.LabelField("Error Count: " + results.errorCount);
				EditorGUILayout.LabelField("Exception Count: " + results.exceptionCount);
			}
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();			
			EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinWidth(300), GUILayout.MinHeight(300), GUILayout.MaxHeight(520));
			//This should be the spot where we can add tests into an automatables list as well as see the tests that the current automatable has queued up			
			EditorGUILayout.LabelField("Test List", EditorStyles.toolbar);
			leftScrollPosition = EditorGUILayout.BeginScrollView(leftScrollPosition);
			if (currentAutomatable != null)
			{
				currentAutomatableTests = new List<string>();
				foreach (Test test in currentAutomatable.tests)
				{
					currentAutomatableTests.Add(test.GetType().Name);
				}				
				testGridInt = GUILayout.SelectionGrid(testGridInt, currentAutomatableTests.ToArray(), 1, EditorStyles.miniButton);
			}
			EditorGUILayout.EndScrollView();
			GUILayout.BeginHorizontal();
			selectedTestType = EditorGUILayout.Popup(selectedTestType, testTypeMap.Keys.ToArray(), EditorStyles.toolbarDropDown, GUILayout.Width(200));
			GUI.enabled = false;
			if (GUILayout.Button("Add Test", EditorStyles.toolbarButton, GUILayout.Width(100)))
			{
				currentAutomatable.tests.Add((Test)Activator.CreateInstance(testTypeMap[testTypeMap.Keys.ToList()[selectedTestType]]));
			}
			GUI.enabled = true;
			GUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();			
			//This box will show test state info, relies on the TestResult class so for now we wait.
			EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinHeight(300), GUILayout.MaxHeight(520));
			EditorGUILayout.LabelField("Current Test Info", EditorStyles.toolbar);
			rightScrollPosition = EditorGUILayout.BeginScrollView(rightScrollPosition);
			if (currentAutomatable != null)
			{
				if(currentAutomatable.tests!=null)
				{
					if (currentAutomatable.tests.Count > testGridInt && currentAutomatable.tests[testGridInt].result != null)
					{
						GUIStyle resultsArea = EditorStyles.textArea;
						resultsArea.wordWrap = true;
						GUI.enabled = false;
						EditorGUILayout.BeginVertical(EditorStyles.helpBox);
						EditorGUILayout.LabelField("Date Ran: " + currentAutomatable.tests[testGridInt].result.startTime.ToShortDateString());
						EditorGUILayout.LabelField("Started at " + currentAutomatable.tests[testGridInt].result.startTime.ToShortTimeString() + " and ended at " + currentAutomatable.tests[testGridInt].result.endTime.ToShortTimeString());
						EditorGUILayout.LabelField("Ran for: " + currentAutomatable.tests[testGridInt].result.runTime);
						EditorGUILayout.LabelField("Starting Balance: " + currentAutomatable.tests[testGridInt].result.startingCredits);
						EditorGUILayout.LabelField("Ending Balance: " + currentAutomatable.tests[testGridInt].result.endingCredits);
						EditorGUILayout.EndVertical();
						EditorGUILayout.Space();

						//No need to draw an empty box
						if (currentAutomatable.tests[testGridInt].result.additionalInfo.Count > 0)
						{
							EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
							EditorGUILayout.LabelField("Additional Info");
							EditorGUILayout.EndHorizontal();
							EditorGUILayout.BeginVertical(EditorStyles.helpBox);
							foreach (KeyValuePair<string, string> kvp in currentAutomatable.tests[testGridInt].result.additionalInfo)
							{
								if (GUILayout.Button("<color=#10a500><b>" + kvp.Key + "</b></color> " + kvp.Value, logStyle))
								{

								}
							}							
							EditorGUILayout.EndVertical();
							EditorGUILayout.Space();
						}
						GUI.enabled = true;
					}
				}
			}
			EditorGUILayout.EndScrollView();
			EditorGUILayout.EndVertical();
			
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();

			NGUIEditorTools.DrawSeparator();
						
			drawAnimationSpeedControl();
			drawBottomButtonsControl();			

			NGUIEditorTools.DrawSeparator();

			//Logs section - Old TRAMP had a section for this per test iteration. 			
			drawLogFilterButtons();
			logScrollPosition = EditorGUILayout.BeginScrollView(logScrollPosition, new GUILayoutOption[] { GUILayout.MinHeight(150), GUILayout.MaxHeight(750) });

			//Set the log style to allow rich text 
			logStyle = EditorStyles.toolbarTextField;
			logStyle.richText = true;
			logStyle.wordWrap = true;

			Color bgColor = GUI.backgroundColor;
			
			//Just testing some ideas out for displaying this information
			foreach (ZapLog log in ZAPLogHandler.logs.ToArray()) //ToArray() so the GUI doesnt complain about the list getting changed while its being drawn if a new log comes in
			{
				if (selectedLog == log)
				{
					//62, 95, 150
					GUI.backgroundColor = new Color(62f / 255f, 95f / 255f, 150f / 255f);
				}
				else
				{
					GUI.backgroundColor = Color.clear;
				}
				if (logFilters.ContainsKey(log.logType) && logFilters[log.logType])
				{
					//Unity gui elements have a problem displaying the large outcomes, if this happens just don't draw that outcome.
					try
					{
						if (GUILayout.Button("<color=" + log.color + "><b>[" + log.logType + "]</b></color> " + log.message.Replace("\n", " "), logStyle))
						{
							selectedLog = log;
						}
					}
					catch (Exception e)
					{
						if (GUILayout.Button("<color=" + log.color + "><b>[" + log.logType + "]</b></color> " + "There was an error displaying this log, it has been saved. Error: " + e.Message, logStyle))
						{
							selectedLog = log;
						}
					}
					//EditorGUILayout.LabelField("<color=" + log.color + "><b>[" + log.logType + "]</b></color> " + log.message.Replace("\n", " "), logStyle);
					if (log.logType == ZapLogType.Error || log.logType == ZapLogType.Exception)
					{
						//may want to include a button to open the file, display the outcome that the game was running during this log
						//For outcomes see Server.cs line 564 -> trace from there. (AutomatedPlayer -> AutomatedPlayerCompanion)
					}
				}
			}
			GUI.backgroundColor = bgColor;
			
			EditorGUILayout.EndScrollView();
						
			if(GUILayout.Button("SAVE RESULTS"))
			{
				if (ZyngaAutomatedPlayer.instance != null && ZyngaAutomatedPlayer.instance.results != null)
				{
					//Serialize Automatble result to file
					string resultsJSON = JsonConvert.SerializeObject(ZyngaAutomatedPlayer.instance.results, Formatting.Indented, new JsonSerializerSettings
					{
						TypeNameHandling = TypeNameHandling.All
					});

					string folderName = "ZAPRun-" + ZyngaAutomatedPlayer.instance.results.testPlanStartTime.ToString("yyyy-dd-M--HH-mm-ss");
					TestPlanResults.directoryPath = ZAPFileHandler.getZapResultsFileLocation() + "/" + folderName + "/";
					Directory.CreateDirectory(TestPlanResults.directoryPath);
					string filePath = TestPlanResults.directoryPath + "Results.json";
					using (StreamWriter sw = new StreamWriter(filePath))
					{
						sw.Write(resultsJSON);
					}
				}
				ShowNotification(new GUIContent("Saving results : " + TestPlanResults.directoryPath));			
			}
		}
		#endregion OnGUI

		#region OnGUI Components
		private void drawLogFilterButtons()
		{
			Rect backgroundRect = new Rect(0,
						(GUILayoutUtility.GetLastRect().y + GUILayoutUtility.GetLastRect().height),
						EditorGUIUtility.currentViewWidth,
						EditorGUIUtility.singleLineHeight);

			if (Event.current.type == EventType.Repaint)
			{
				EditorStyles.toolbar.Draw(backgroundRect, false, true, true, false);
			}

			EditorGUILayout.BeginHorizontal();
			List<ZapLogType> keysList = logFilters.Keys.ToList();
			foreach (ZapLogType key in keysList)
			{
				logFilters[key] = GUILayout.Toggle(logFilters[key], new GUIContent(key.ToString()+"s"), EditorStyles.toolbarButton);
			}
			EditorGUILayout.EndHorizontal();
		}

		private void drawAnimationSpeedControl()
		{
			Rect backgroundRect = new Rect(0,
						(GUILayoutUtility.GetLastRect().y + GUILayoutUtility.GetLastRect().height + 4),
						EditorGUIUtility.currentViewWidth,
						EditorGUIUtility.singleLineHeight);

			if (Event.current.type == EventType.Repaint)
			{
				EditorStyles.toolbar.Draw(backgroundRect, false, true, true, false);
			}

			EditorGUILayout.BeginHorizontal();
			Time.timeScale = EditorGUILayout.Slider("Animation Speed", Time.timeScale, 0.0f, 10.0f, new GUILayoutOption[] { GUILayout.MinWidth(400) });
			if (GUILayout.Button("0", EditorStyles.toolbarButton))
			{
				Time.timeScale = 0;
			}
			if (GUILayout.Button("1", EditorStyles.toolbarButton))
			{
				Time.timeScale = 1;
			}
			if (GUILayout.Button("10", EditorStyles.toolbarButton))
			{
				Time.timeScale = 10;
			}
			EditorGUILayout.EndHorizontal();
		}

		private void drawBottomButtonsControl()
		{
			Rect backgroundRect = new Rect(0,
						(GUILayoutUtility.GetLastRect().y + GUILayoutUtility.GetLastRect().height),
						EditorGUIUtility.currentViewWidth,
						EditorGUIUtility.singleLineHeight);

			if (Event.current.type == EventType.Repaint)
			{
				EditorStyles.toolbar.Draw(backgroundRect, false, true, true, false);
			}

			EditorGUILayout.BeginHorizontal();
			string pauseButtonString = "Pause ZAP";
			if (ZyngaAutomatedPlayer.instance != null)
			{
				pauseButtonString = (ZyngaAutomatedPlayer.instance.pauseTesting) ? "Un-Pause ZAP" : "Pause ZAP";
			}
			if (GUILayout.Button(pauseButtonString, EditorStyles.toolbarButton))
			{
				if(ZyngaAutomatedPlayer.instance != null)
				{
					ZyngaAutomatedPlayer.instance.pauseTesting = !ZyngaAutomatedPlayer.instance.pauseTesting;
				}
			}
			
			string allowEditorPauseButtonString = (isAllowingEditorPause) ? "Editor Pause: Allowed" : "Editor Pause: Dis-Allowed";

			if (GUILayout.Button(allowEditorPauseButtonString, EditorStyles.toolbarButton))
			{
				isAllowingEditorPause = !isAllowingEditorPause;
				if (ZyngaAutomatedPlayer.instance != null)
				{
					// Set the value in ZyngaAutomatedPlayer which also sets the player pref
					ZyngaAutomatedPlayer.instance.isAllowingEditorPause = isAllowingEditorPause;
				}
				else
				{
					// Need to set the player pref itself since ZyngaAutomatedPlayer doesn't exist right now
					SlotsPlayer.getPreferences().SetBool(ZAPPrefs.IS_ALLOWING_EDITOR_PAUSE, isAllowingEditorPause);
				}
			}

			//Move this to results window after testing
			if (GUILayout.Button("Create Jira Ticket", EditorStyles.toolbarButton))
			{
				if(currentAutomatable.automatableResult == null)
				{
					ZAPJiraCreatorWindow.init(null);
				}
				else
				{
					ZAPJiraData zapJIRAdata = new ZAPJiraData(currentAutomatable.automatableResult, selectedLog, ZyngaAutomatedPlayer.instance.results.gitBranch);
					ZAPJiraCreatorWindow.init(zapJIRAdata);
				}
			}

			EditorGUILayout.EndHorizontal();
			
			EditorGUILayout.BeginHorizontal();
			string randomWagersButtonString = (isUsingRandomWagersForSpins) ? "Random Wagers: On" : "Random Wagers: Off";

			if (GUILayout.Button(randomWagersButtonString, EditorStyles.toolbarButton))
			{
				isUsingRandomWagersForSpins = !isUsingRandomWagersForSpins;
				if (ZyngaAutomatedPlayer.instance != null)
				{
					// Set the value in ZyngaAutomatedPlayer which also sets the player pref
					ZyngaAutomatedPlayer.instance.isUsingRandomWagersForSpins = isUsingRandomWagersForSpins;
				}
				else
				{
					// Need to set the player pref itself since ZyngaAutomatedPlayer doesn't exist right now
					SlotsPlayer.getPreferences().SetBool(ZAPPrefs.IS_USING_RANDOM_WAGERS_FOR_SPINS, isUsingRandomWagersForSpins);
				}
			}
			EditorGUILayout.EndHorizontal();
		}
		#endregion OnGUI Components
	}
	#endif
}
