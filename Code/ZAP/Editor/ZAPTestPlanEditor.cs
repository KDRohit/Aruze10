using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System;
using System.IO;
using Newtonsoft.Json;
using Zap.Automation;
using System.Reflection;
using UnityEditor.SceneManagement;

namespace Zap.Automation.Editor
{
	#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	public class ZAPTestPlanEditor : EditorWindow, IHasCustomMenu
	{
		#region Context Menu Options
		private GUIContent openResultsWindow = new GUIContent("Open Tab/Test Results");
		private GUIContent observerWindowText = new GUIContent("Open Tab/ZAP Observer");
		#endregion Context Menu Options

		#region OnGUI Specific Variables
		private List<HelpMessage> helpMessages = new List<HelpMessage>();

		private Vector2 testPlansScrollPosition = Vector2.zero;
		private Vector2 middlePaneScrollPosition = Vector2.zero;
		private Vector2 detailsScrollPosition = Vector2.zero;

		private int testPlanGridInt = -1;

		private List<bool> automatableCollapsed = new List<bool>();
		private List<int> automatableSelectedTestType = new List<int>();

		private int selectedAutomatableType = 0;
		private Dictionary<string, Type> automatableTypeMap = new Dictionary<string, Type>();

		private Dictionary<string, Type> testTypeMap = new Dictionary<string, Type>();
		private Dictionary<string, List<string>> automatableTestCompatabilityMap;
		
		private UnityEngine.Object detailsObject;

		private string testPlanFileName = "";
		private string testPlanFilePath = "";

		private string quickAddGameKey = "gameKey";
		#endregion OnGUI Specific Variables

		#region Private Variables
		private TestPlan selectedTestPlan;

		//private string defaultLocation;
		private List<string> testPlanNames = new List<string>();
		private Automatable selectedAutomatable;
		private Test selectedTest;
		private Dictionary<int, TestPlan> cachedPlans = new Dictionary<int, TestPlan>();

		private bool includePorts = true;
		private bool includeNonProductionReady = true;
		private bool includeLapsedLicenses = true;
		#endregion Private Variables

		#region Unity EditorWindow 
		[MenuItem("Zynga/ZAP/Test Setup", false, 405)]
		public static void showWindow()
		{
			// try to dock next to Scene window
			EditorWindow[] windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
			EditorWindow sceneWindow = windows.FirstOrDefault(e => e.titleContent.text.Contains("Scene"));

			if (sceneWindow != null)
			{
				GetWindow<ZAPTestPlanEditor>("ZAP Test Setup", false, sceneWindow.GetType());
			}
			else
			{
				GetWindow<ZAPTestPlanEditor>("ZAP Test Setup", false);
			}			
		}

		//Implement IHasCustomMenu.AddItemsToMenu
		public void AddItemsToMenu(GenericMenu menu)
		{
			//menu.AddItem(observerWindowText, false, openObserverWindow);
			//menu.AddItem(openResultsWindow, false, null);
		}		
		#endregion Unity EditorWindow 

		private void OnEnable()
		{
			//Set the help messages
			helpMessages = new List<HelpMessage>
			{
				//Example
				{ new HelpMessage("NULL", MessageType.None) },
				{ new HelpMessage("Make sure there is a test plan!", MessageType.Warning) },
				{ new HelpMessage("Could not save test plan to " + testPlanFilePath, MessageType.Warning) },
				{ new HelpMessage("Be sure to add Automatables to your test plan", MessageType.Warning) },
				{ new HelpMessage("Deserializing large test plans can take a few seconds", MessageType.Info) },				
			};

			//Lets get our testplans
			testPlanNames = ZAPEditorHelpers.getTestPlansAtLocation();

			//Get all Automatable subclasses
			automatableTypeMap = new Dictionary<string, Type>();
			automatableTestCompatabilityMap = new Dictionary<string, List<string>>();
			List<Type> automatableSubclasses = Assembly.GetAssembly(typeof(Automatable)).GetTypes().Where(t => t.IsSubclassOf(typeof(Automatable))).ToList();
			foreach (Type t in automatableSubclasses)
			{
				automatableTypeMap.Add(t.Name, t);
				automatableTestCompatabilityMap.Add(t.Name, new List<string>());
			}

			//Get all Test subclasses
			testTypeMap = new Dictionary<string, Type>();
			List<Type> testSubclasses = Assembly.GetAssembly(typeof(Test)).GetTypes().Where(t => t.IsSubclassOf(typeof(Test))).ToList();
			foreach(Type t in testSubclasses)
			{
				if (t.Name == "Pretest" || t.Name == "FeatureTest")
				{
					// Dont add base classes since they do nothing on their own.
					continue;
				}
				testTypeMap.Add(t.Name, t);
				// Now populate the compatability map with this test.
				Test test = (Test)Activator.CreateInstance(t);
				foreach(string automatable in test.compatibleAutomatables(automatableTypeMap.Keys.ToList()))
				{
					automatableTestCompatabilityMap[automatable].Add(t.Name);
				}
			}

			if (SlotResourceMap.map == null)
			{
				SlotResourceMap.populateAll();
			}
		}

		private void Update()
		{		
			helpMessages[1].active = (selectedTestPlan == null);
			if(selectedTestPlan != null)
			{
				helpMessages[3].active = (selectedTestPlan.automatables.Count == 0);
			}
		}

		private void OnInspectorUpdate()
		{
			if (detailsObject != null)
			{
				Repaint();
			}
		}

#region OnGUI
		private void OnGUI()
		{
			//This is the toolbar at the top of the window.
			drawTopBar();
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			EditorGUILayout.LabelField("ZAP Info", EditorStyles.boldLabel);
			EditorGUILayout.LabelField("1) The Test Plan loaded into the setup window will not currently persist in the UI when play is pressed.  Hit Play and load your test plan.");
			EditorGUILayout.LabelField("2) Like TRAMP, ZAP, wants to run the test plan from the lobby.");
			EditorGUILayout.LabelField("If you click and automatable or test, itll open the object in the details pane.");
			EditorGUILayout.EndHorizontal();
			NGUIEditorTools.DrawSeparator();
			EditorGUILayout.BeginVertical();			
			EditorGUILayout.BeginHorizontal();

			//Left Pane
			EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinHeight(150), GUILayout.MaxHeight(730));
			EditorGUILayout.LabelField("Test Plans", EditorStyles.toolbar);
			helpMessages[4].active = true;
			testPlansScrollPosition = EditorGUILayout.BeginScrollView(testPlansScrollPosition);
			testPlanGridInt = GUILayout.SelectionGrid(testPlanGridInt, testPlanNames.ToArray(), 1, EditorStyles.miniButton);
			
			// Make sure that the index isn't out of bounds.  Which can happen (I assume if
			// test plans are deleted via the OS).
			if (testPlanGridInt >= testPlanNames.Count)
			{
				testPlanGridInt = -1;
				selectedTestPlan = null;
			}

			if (testPlanGridInt != -1)
			{
				if (!cachedPlans.ContainsKey(testPlanGridInt)) //Haven't cached this plan yet.
				{	
					cachedPlans.Add(testPlanGridInt,
						JsonConvert.DeserializeObject<TestPlan>(
							File.ReadAllText(
								Path.Combine(ZAPFileHandler.getZapSaveFileLocation(), testPlanNames[testPlanGridInt])),
							new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All }));
				}
				else if (cachedPlans[testPlanGridInt] == null) //We have cached this plan but it was nulled out.
				{
					cachedPlans[testPlanGridInt] = JsonConvert.DeserializeObject<TestPlan>(
						File.ReadAllText(
							Path.Combine(ZAPFileHandler.getZapSaveFileLocation(), testPlanNames[testPlanGridInt])),
						new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
				}
				selectedTestPlan = cachedPlans[testPlanGridInt];
				testPlanFileName = testPlanNames[testPlanGridInt].Replace(".json", "");
			}
			
			EditorGUILayout.EndScrollView();
			EditorGUILayout.Space();
			if (testPlanGridInt == -1 && selectedTestPlan != null)
			{
				EditorGUILayout.LabelField("Temp Plan", EditorStyles.toolbar);
				GUI.enabled = false;
				EditorGUILayout.LabelField(((string.IsNullOrEmpty(testPlanFileName)) ? "new TestPlan" : testPlanFileName), EditorStyles.miniButton);
				GUI.enabled = true;
			}
			EditorGUILayout.Space();

			EditorGUILayout.LabelField("Quick Plans", EditorStyles.toolbar);
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			EditorGUILayout.LabelField("Basic Game Test Plan");
			quickAddGameKey = EditorGUILayout.TextField(quickAddGameKey, EditorStyles.toolbarTextField);
			if(GUILayout.Button("Create", EditorStyles.toolbarButton, GUILayout.Width(75)))
			{				
				selectedTestPlan = CreateInstance(typeof(TestPlan)) as TestPlan;
				selectedTestPlan.automatables.Add(ZAPEditorHelpers.defaultBaseGameAutomatable(quickAddGameKey));
				testPlanFileName = "TestPlan_" + quickAddGameKey;
				testPlanGridInt = -1;
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space();

			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			EditorGUILayout.LabelField("All Games");			
			if (GUILayout.Button("Create", EditorStyles.toolbarButton, GUILayout.Width(75)))
			{
				selectedTestPlan = CreateInstance(typeof(TestPlan)) as TestPlan;
				foreach (string gameKey in SlotResourceMap.map.Keys)
				{
					SlotResourceData srd = SlotResourceMap.map[gameKey];
					switch(srd.gameStatus)
					{
						case SlotResourceData.GameStatus.PORT:
						case SlotResourceData.GameStatus.PORT_NEEDS_ART:
							if (includePorts)
							{
								selectedTestPlan.automatables.Add(ZAPEditorHelpers.defaultBaseGameAutomatable(gameKey));
							}
							break;
						case SlotResourceData.GameStatus.NON_PRODUCTION_READY:
							if (includeNonProductionReady)
							{
								selectedTestPlan.automatables.Add(ZAPEditorHelpers.defaultBaseGameAutomatable(gameKey));
							}
							break;
						case SlotResourceData.GameStatus.PRODUCTION_READY:
						case SlotResourceData.GameStatus.PRODUCTION_READY_REFACTORED:
						case SlotResourceData.GameStatus.PRODUCTION_READY_POSSIBLY_REFACTORED:
							selectedTestPlan.automatables.Add(ZAPEditorHelpers.defaultBaseGameAutomatable(gameKey));
							break;
						case SlotResourceData.GameStatus.LICENSE_LAPSED:
							if (includeLapsedLicenses)
							{
								selectedTestPlan.automatables.Add(ZAPEditorHelpers.defaultBaseGameAutomatable(gameKey));
							}
							break;
						default:
							break;
					}					
				}
				testPlanFileName = "TestPlan_AllGames";
				testPlanGridInt = -1;
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			includePorts = GUILayout.Toggle(includePorts, "Ports");
			includeNonProductionReady = GUILayout.Toggle(includeNonProductionReady, "Non production");
			includeLapsedLicenses = GUILayout.Toggle(includeLapsedLicenses, "License lapsed");
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.EndVertical();

			//Middle Pane 
			EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinWidth(300), GUILayout.MinHeight(150), GUILayout.MaxHeight(730) );
			if (selectedTestPlan == null)
			{
				EditorGUILayout.LabelField("Automatables", EditorStyles.toolbar);
			}
			else
			{				
				EditorGUILayout.LabelField("Automatables in " + testPlanFileName + " : " + selectedTestPlan.automatables.Count, EditorStyles.toolbar);
				GUILayout.BeginHorizontal();
				selectedAutomatableType = EditorGUILayout.Popup(selectedAutomatableType, automatableTypeMap.Keys.ToArray(), EditorStyles.toolbarDropDown, GUILayout.Width(200));
				if (GUILayout.Button("Add Automatable", EditorStyles.toolbarButton, GUILayout.Width(100)))
				{
					selectedTestPlan.automatables.Add((Automatable)Activator.CreateInstance(automatableTypeMap[automatableTypeMap.Keys.ToList()[selectedAutomatableType]]));
				}
				GUILayout.EndHorizontal();
			}
			
			middlePaneScrollPosition = EditorGUILayout.BeginScrollView(middlePaneScrollPosition);
			
			if (selectedTestPlan != null)
			{
				if (automatableCollapsed.Count != selectedTestPlan.automatables.Count)
				{
					automatableCollapsed = Enumerable.Repeat(true, selectedTestPlan.automatables.Count).ToList();
				}
				if (automatableSelectedTestType.Count != selectedTestPlan.automatables.Count)
				{
					automatableSelectedTestType = Enumerable.Repeat(0, selectedTestPlan.automatables.Count).ToList();
				}
				for(int i = 0; i < selectedTestPlan.automatables.ToList().Count; i++)
				{
					EditorGUILayout.BeginHorizontal();					
					if (GUILayout.Button("-", EditorStyles.miniButtonLeft, GUILayout.Width(20)))
					{
						selectedTestPlan.automatables.Remove(selectedTestPlan.automatables[i]);
						//we dont want to try to draw anything for i since we are removing it anyway
						continue;
					}
					string automatableLabelString = (string.IsNullOrEmpty(selectedTestPlan.automatables[i].key)) ? selectedTestPlan.automatables[i].GetType().Name.ToString() : selectedTestPlan.automatables[i].key;
					if (GUILayout.Button(automatableLabelString, EditorStyles.miniButtonMid, GUILayout.Width(170)))
					{
						detailsObject = selectedTestPlan.automatables[i];
					}
					string collapsedString = (automatableCollapsed[i]) ? "Collapse" : "Expand";
					if (GUILayout.Button(collapsedString, EditorStyles.miniButtonRight, GUILayout.Width(70)))
					{
						automatableCollapsed[i] = !automatableCollapsed[i];
					}
					EditorGUILayout.EndHorizontal();

					if (automatableCollapsed[i])
					{
						// If we want this expanded then show all the tests.
						foreach (Test test in selectedTestPlan.automatables[i].tests.ToList())
						{
							EditorGUILayout.BeginHorizontal();
							GUILayout.Space(30);
							// TODO --  add the ability to move tests up/down.
							if (GUILayout.Button("-", EditorStyles.miniButtonLeft, GUILayout.Width(20)))
							{
								selectedTestPlan.automatables[i].tests.Remove(test);
							}
							if (GUILayout.Button(test.GetType().Name.ToString(), EditorStyles.miniButtonRight, GUILayout.Width(214)))
							{
								detailsObject = test;
							}
							EditorGUILayout.EndHorizontal();
						}
						GUILayout.BeginHorizontal();
						GUILayout.Space(30);

						// Get the filtered dropdown list and use that to set the selected test key for this automatable.
						string automatableKey = selectedTestPlan.automatables[i].GetType().Name;
						automatableSelectedTestType[i] = EditorGUILayout.Popup(
							automatableSelectedTestType[i],
							automatableTestCompatabilityMap[automatableKey].ToArray(),
							EditorStyles.miniButtonLeft,
							GUILayout.Width(120));

						if (GUILayout.Button("Add Test", EditorStyles.miniButtonRight, GUILayout.Width(75)))
						{
							// Grab they string key of the test we just selected and add the test to the automatable.
							string testKey = automatableTestCompatabilityMap[automatableKey][automatableSelectedTestType[i]];
							selectedTestPlan.automatables[i].tests.Add(
								(Test)Activator.CreateInstance(
									testTypeMap[testKey]
								));
						}
						GUILayout.EndHorizontal();
						EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
					}
				}				
			}
			EditorGUILayout.EndScrollView();
			EditorGUILayout.EndVertical();

			//Right Pane 
			EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinWidth(500), GUILayout.MinHeight(150), GUILayout.MaxHeight(730));
			EditorGUILayout.LabelField("Selection Details", EditorStyles.toolbar);
			detailsScrollPosition = EditorGUILayout.BeginScrollView(detailsScrollPosition);

			if (detailsObject != null)
			{
				SerializedObject serializedDetailsObject = new SerializedObject(detailsObject);
				SerializedProperty prop = serializedDetailsObject.GetIterator();
				bool isFirstProp = true;
				while (prop.NextVisible(isFirstProp))
				{
					EditorGUILayout.PropertyField(prop, true);
					if (isFirstProp)
					{
						isFirstProp = false;
					}
				}

				serializedDetailsObject.ApplyModifiedProperties();
				serializedDetailsObject.Update();
				EditorUtility.SetDirty(detailsObject);
			}

			EditorGUILayout.EndScrollView();
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();

			Rect backgroundRect = new Rect(0,
				GUILayoutUtility.GetLastRect().y + GUILayoutUtility.GetLastRect().height + 4,
				EditorGUIUtility.currentViewWidth,
				EditorGUIUtility.singleLineHeight);

			if (Event.current.type == EventType.Repaint)
			{
				EditorStyles.toolbar.Draw(backgroundRect, false, true, true, false);
			}

			GUI.enabled = (selectedTestPlan != null);			
			EditorGUILayout.BeginHorizontal();			
			testPlanFileName = EditorGUILayout.TextField(testPlanFileName, EditorStyles.toolbarTextField);
			if(GUILayout.Button("Save Test Plan", EditorStyles.toolbarButton))
			{
				selectedTestPlan.testPlanName = testPlanFileName;
				string jsonTest = JsonConvert.SerializeObject(selectedTestPlan, Formatting.Indented, new JsonSerializerSettings
				{
					TypeNameHandling = TypeNameHandling.All
				});

				string folder = ZAPFileHandler.getZapSaveFileLocation();
				
				testPlanFilePath = EditorUtility.SaveFilePanel("Save Test Plan", folder, testPlanFileName, "json");

				if (!string.IsNullOrEmpty(testPlanFilePath))
				{
					using (StreamWriter sw = new StreamWriter(testPlanFilePath))
					{
						sw.Write(jsonTest);
					}
					helpMessages[2].active = false;					
					testPlanNames = ZAPEditorHelpers.getTestPlansAtLocation();
					for(int i = 0; i < testPlanNames.Count; i++)
					{						
						if(testPlanNames[i].Equals(testPlanFileName+".json"))
						{
							testPlanGridInt = i;
							if (cachedPlans.ContainsKey(testPlanGridInt))
							{
								cachedPlans[i] = selectedTestPlan;
							}
						}
					}
				}
				else
				{
					helpMessages[2].active = true;
				}
			}
			if (GUILayout.Button("Run Test Plan", EditorStyles.toolbarButton))
			{
				if (Application.isPlaying)
				{
					// Set the test plan and go!
					ZyngaAutomatedPlayer.instance.currentTestPlan = selectedTestPlan;
					ZyngaAutomatedPlayer.instance.startAutomation();
				}
				else
				{
					selectedTestPlan.testPlanName = testPlanFileName;
					string testPlanJSON = JsonConvert.SerializeObject(selectedTestPlan, Formatting.Indented, new JsonSerializerSettings
					{
						TypeNameHandling = TypeNameHandling.All
					});
					// Save the test plan to prefs.
					SlotsPlayer.getPreferences().SetString(ZAPPrefs.TEST_PLAN_JSON, testPlanJSON);
					EditorApplication.isPlaying = false;
					EditorSceneManager.OpenScene("Assets/Data/HIR/Scenes/Startup.unity");
					// Mark this so that we start the automation once the game starts.
					SlotsPlayer.getPreferences().SetInt(ZAPPrefs.SHOULD_AUTOMATE_ON_PLAY, 1);
					// Start the game.
					EditorApplication.isPlaying = true;
				}
			}
			GUI.enabled = true;
			if (GUILayout.Button("Resume Test Plan", EditorStyles.toolbarButton))
			{
				if (Application.isPlaying)
				{
					ZyngaAutomatedPlayer.instance.resumeAutomation();
				}
			}
			EditorGUILayout.EndHorizontal();
			
			//Draw the help box
			drawHelpBoxes();
			EditorGUILayout.EndVertical();
		}
		#endregion OnGUI

		#region OnGUI Components
		private void drawTopBar()
		{
			Rect backgroundRect = new Rect(0,
				-1,
				EditorGUIUtility.currentViewWidth,
				EditorGUIUtility.singleLineHeight);

			if (Event.current.type == EventType.Repaint)
			{
				EditorStyles.toolbar.Draw(backgroundRect, false, true, true, false);
			}

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Setup Test Plan");
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Create", EditorStyles.toolbarButton))
			{
				selectedTestPlan = (TestPlan)ScriptableObject.CreateInstance(typeof(TestPlan));
				testPlanGridInt = -1;
				testPlanFileName = "new TestPlan";
			}
			//TODO I need to remove once Nick's idea of see the available test plans in editor window is added
			if (GUILayout.Button("Load", EditorStyles.toolbarButton))
			{				
				string path = EditorUtility.OpenFilePanel("Load Test Plan JSON", @"Assets/ZAP/Test Plan JSONs/", "json");
				
				if (path.Length != 0)
				{	
					selectedTestPlan = JsonConvert.DeserializeObject<TestPlan>(File.ReadAllText(path), new JsonSerializerSettings
					{
						TypeNameHandling = TypeNameHandling.All
					});					
				}
			}
			EditorGUILayout.EndHorizontal();
		}

		private void drawHelpBoxes()
		{
			foreach(HelpMessage hm in helpMessages)
			{
				if (hm.active)
				{
					EditorGUILayout.HelpBox(hm.message, hm.messageType);					
				}
			}
		}
		#endregion OnGUI Components

		#region Helper Classes
		private class HelpMessage
		{
			public bool active = false;
			public string message;
			public MessageType messageType;

			public HelpMessage(string p_message, MessageType p_messageType)
			{
				message = p_message;
				messageType = p_messageType;
			}
		}
		#endregion Helper Classes
	}
	#endif
}
