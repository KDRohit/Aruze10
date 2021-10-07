using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Zap.Automation;
using System.Linq;
using System.IO;
using Newtonsoft.Json;

namespace Zap.Automation.Editor
{
	#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	public class ZAPResultsWindow : EditorWindow
	{
		#region private variables
		DirectoryInfo resultDirectory;		
		private int directoryLevel = 0;
		private List<GUIDirectoryInfo> directoryDetails;
				
		private Vector2 directoryScrollPosition = Vector2.zero;
		private Vector2 additionalInfoScrollPosition = Vector2.zero;
		private Vector2 logsScrollPosition = Vector2.zero;

		private Texture folderTexture;
		private Texture2D fileTexture;
		private GUIStyle fileStyle;

		private bool showTestLogs = false;
		private Dictionary<ZapLogType, bool> logFilters = new Dictionary<ZapLogType, bool>()
		{
			{ ZapLogType.Warning, true },
			{ ZapLogType.Error, true },
			{ ZapLogType.Exception, true },
			{ ZapLogType.Outcome, true },
			{ ZapLogType.Desync, true }
		};

		private string fileSearchString = "";
		
		//When we open a result object cache it so we don't have to deserialize it again this session.
		private Dictionary<string, object> cachedResultObjects = new Dictionary<string, object>();

		private int selectedJiraType = 0;
		private readonly Dictionary<int, string> jiraTicketType = new Dictionary<int, string>()
		{
			{ 0, "Auto" },
			{ 1, "TestPlan Results" },
			{ 2, "Automatable Result" },
			{ 3, "Test Result" },
			{ 4, "Zap Log" }
		};	

		private GUIDirectoryInfo selected;
		private object displayObject;
		private ZapLog selectedLog;
		#endregion private variables

		#region Unity EditorWindow 
		[MenuItem("Zynga/ZAP/Test Results", false, 407)]
		public static void showWindow()
		{
			// try to dock next to Scene window
			EditorWindow[] windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
			EditorWindow observerWindow = windows.FirstOrDefault(e => e.titleContent.text.Contains("ZAP Observer"));

			if (observerWindow != null)
			{
				GetWindow<ZAPResultsWindow>("ZAP Results", false, observerWindow.GetType());
			}
			else
			{
				GetWindow<ZAPResultsWindow>("ZAP Results", false);
			}
		}

		public void AddItemsToMenu(GenericMenu menu)
		{

		}

		private void OnEnable()
		{
			directoryDetails = new List<GUIDirectoryInfo>();
			string resultFileLocation = ZAPFileHandler.getZapResultsFileLocation();
			
			// Make sure that the file location exists, in case it hasn't been used yet
			// or was cleaned up using the Operating System
			CommonFileSystem.createDirectoryIfNotExisting(resultFileLocation);
			
			resultDirectory = new DirectoryInfo(resultFileLocation);
			getResultsDirectory(resultDirectory, null);

			folderTexture = AssetDatabase.GetCachedIcon(Path.GetFileName(Application.dataPath));
			fileTexture = EditorGUIUtility.FindTexture("TextAsset Icon");
		}

		//when we focus back to this window just check the results directory again
		private void OnFocus()
		{
			directoryDetails = new List<GUIDirectoryInfo>();
			resultDirectory = new DirectoryInfo(ZAPFileHandler.getZapResultsFileLocation());
			getResultsDirectory(resultDirectory, null);
		}
		#endregion Unity EditorWindow 	



		#region OnGui
		private void OnGUI()
		{			
			ZAPEditorHelpers.drawTopBar("Results Browser", null);
			EditorGUILayout.Space();

			EditorGUILayout.BeginHorizontal();			
			//Right Window
			EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinWidth(300), GUILayout.MinHeight(300), GUILayout.MaxHeight(1080));
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);			
			fileSearchString = GUILayout.TextField(fileSearchString, EditorStyles.toolbarTextField, GUILayout.Width(250));
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Search", EditorStyles.toolbarButton))
			{
				//TODO: Stopping this since there is more pressing things, but this will search/filter the results by name
			}			
			EditorGUILayout.EndHorizontal();
			directoryScrollPosition = EditorGUILayout.BeginScrollView(directoryScrollPosition);
			foreach(GUIDirectoryInfo guiDirectory in directoryDetails)
			{				
				if (guiDirectory.parent == null || (guiDirectory.parent.opened && guiDirectory.visible))
				{
					if (guiDirectory.isFolder)
					{
						drawFolder(guiDirectory, guiDirectory.indentLevel == 0);
					}
					else
					{
						if (guiDirectory.name.Contains(fileSearchString))
						{
							drawFile(guiDirectory);
						}
					}
				}								
			}
			EditorGUILayout.EndScrollView();
			EditorGUILayout.EndVertical();

			EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinHeight(300), GUILayout.MaxHeight(1080));
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar );
			GUILayout.FlexibleSpace();
			//selectedJiraType = EditorGUILayout.Popup(selectedJiraType, jiraTicketType.Values.ToArray(), EditorStyles.toolbarDropDown, GUILayout.Width(100));			
			if (GUILayout.Button("Create JIRA", EditorStyles.toolbarButton))
			{
				createJira();
			}
			EditorGUILayout.EndHorizontal();
			if (displayObject == null)
			{
				EditorGUILayout.HelpBox("To Create a Jira select a result in the result browser.  We will try to create a JiRA ticket with information based on what data you have selected if you want a specific type you can uses the drop down on the bar above.", MessageType.Info);
			}
			else
			{
				switch (displayObject.GetType().Name)
				{
					case "TestPlan":
						drawTestPlan((TestPlan)displayObject);
						break;
					case "TestPlanResults":
						drawTestPlanResults((TestPlanResults)displayObject);
						break;
					case "AutomatableResult":
						drawAutomatableResult((AutomatableResult)displayObject);
						break;
					case "TestResult":
						drawTestResult((TestResult)displayObject);
						break;
					default:
						Debug.Log("Selected something undrawable somehow. [" + displayObject.GetType().Name + "]");
						break;
				}
			}
		
			EditorGUILayout.EndVertical();

			EditorGUILayout.EndHorizontal();
		}
		#endregion OnGui
		
		private void createJira()
		{
			ZAPJiraData zapJIRAdata = null;

			if (displayObject == null)
			{
				EditorGUILayout.HelpBox("To Create a Jira select a result in the result browser.  We will try to create a JiRA ticket with information based on what data you have selected if you want a specific type you can uses the drop down on the bar above.", MessageType.Info);
			}
			else
			{
				switch (displayObject.GetType().Name)
				{
					case "TestPlanResults":
						zapJIRAdata = new ZAPJiraData((TestPlanResults)displayObject, selectedLog);
						break;
					case "AutomatableResult":
						zapJIRAdata = new ZAPJiraData((AutomatableResult)displayObject, selectedLog);
						break;
					case "TestResult":
						zapJIRAdata = new ZAPJiraData((TestResult)displayObject, selectedLog);
						break;
					default:
						Debug.Log("Selected something undrawable somehow. [" + displayObject.GetType().Name + "]");
						break;
				}
			}

			ZAPJiraCreatorWindow.init(zapJIRAdata);
		}

		private void drawFolder(GUIDirectoryInfo info, bool isTop)
		{
			if (isTop)
			{
				EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			}
			else
			{
				EditorGUILayout.BeginHorizontal();
			}
			GUILayout.Space(info.indentLevel * 16);							
			info.opened = EditorGUILayout.Foldout(info.opened, new GUIContent(folderTexture), EditorStyles.foldout);
			GUILayout.Space(-28);
			Color bgColor = GUI.backgroundColor;
			GUI.backgroundColor = Color.clear;
			if (GUILayout.Button(info.name))
			{
				info.opened = !info.opened;
			}
			GUI.backgroundColor = bgColor;
			GUILayout.FlexibleSpace();			
			setChildrenVisibilty(info, info.opened);			
			EditorGUILayout.EndHorizontal();
		}

		private void drawFile(GUIDirectoryInfo info)
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(info.indentLevel * 16);
			GUILayout.Label(new GUIContent(fileTexture), GUILayout.Width(16), GUILayout.Height(EditorGUIUtility.singleLineHeight));
			GUILayout.Space(-4);			
			Color bgColor = GUI.backgroundColor;
			if (selected == info)
			{
				//62, 95, 150
				GUI.backgroundColor = new Color(62f / 255f, 95f / 255f, 150f / 255f);
			}
			else
			{
				GUI.backgroundColor = Color.clear;
			}
			fileStyle = GUI.skin.button;
			fileStyle.margin = new RectOffset(0, 0, 0, 0);			
			if (GUILayout.Button(info.name, fileStyle))
			{
				if (selected == info)
				{
					selected = null;
					displayObject = null;
				}
				else
				{
					selected = info;
					if (cachedResultObjects.ContainsKey(info.path))
					{
						displayObject = cachedResultObjects[info.path];
					}
					else
					{
						if (info.path.Length != 0)
						{
							displayObject = JsonConvert.DeserializeObject(File.ReadAllText(info.path), new JsonSerializerSettings
							{
								TypeNameHandling = TypeNameHandling.All
							});
							cachedResultObjects.Add(info.path, displayObject);
						}
					}
				}
			}
			GUI.backgroundColor = bgColor;
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
		}

		private void drawTestPlan(TestPlan testPlan)
		{

		}

		private void drawTestPlanResults(TestPlanResults testPlanResults)
		{
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			EditorGUILayout.LabelField("Date Ran: " + testPlanResults.testPlanStartTime.ToShortDateString());
			EditorGUILayout.LabelField("Started at " + testPlanResults.testPlanStartTime.ToShortTimeString() + " and ended at " + testPlanResults.testPlanEndTime.ToShortTimeString());
			EditorGUILayout.LabelField("Ran for: " + testPlanResults.testPlanRunTime);
			EditorGUILayout.LabelField("Starting Balance: " + testPlanResults.playerStartingCredits);
			EditorGUILayout.LabelField("Ending Balance: " + testPlanResults.playerEndingCredits);
			EditorGUILayout.EndVertical();
			EditorGUILayout.Space();

			//Grab the default color so we can set it back.
			Color bgColor = GUI.backgroundColor;		

			//Lets figure out how to color the warnings box
			GUI.backgroundColor = severityColor(testPlanResults.warningCount, testPlanResults.errorCount, testPlanResults.exceptionCount, 0);
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			//Reset the bg color to normal
			GUI.backgroundColor = bgColor;
			EditorGUILayout.LabelField("Warnings: " + testPlanResults.warningCount);
			EditorGUILayout.LabelField("Errors: " + testPlanResults.errorCount);
			EditorGUILayout.LabelField("Exceptions: " + testPlanResults.exceptionCount);
			EditorGUILayout.EndVertical();
			EditorGUILayout.Space();

			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			EditorGUILayout.LabelField("Git Branch: " + testPlanResults.gitBranch);
			EditorGUILayout.EndVertical();
			EditorGUILayout.Space();

			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			EditorGUILayout.LabelField("Automatables tested");
			foreach (string automatable in testPlanResults.automatableFilePaths)
			{
				string[] splitPath = automatable.Split('/');
				EditorGUILayout.BeginHorizontal();				
				if (GUILayout.Button(splitPath[splitPath.Length-1].Replace(".json", ""), EditorStyles.miniButton, GUILayout.Width(100)))
				{

				}				
				EditorGUILayout.EndHorizontal();
			}
			EditorGUILayout.EndVertical();
		}

		private void drawAutomatableResult(AutomatableResult automatableResult)
		{
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			EditorGUILayout.LabelField("Date Ran: " + automatableResult.startTime.ToShortDateString());
			EditorGUILayout.LabelField("Started at " + automatableResult.startTime.ToShortTimeString() + " and ended at " + automatableResult.endTime.ToShortTimeString());
			EditorGUILayout.LabelField("Ran for: " + automatableResult.runTime);
			EditorGUILayout.LabelField("Starting Balance: " + automatableResult.startingCredits);
			EditorGUILayout.LabelField("Ending Balance: " + automatableResult.endingCredits);
			EditorGUILayout.EndVertical();
			EditorGUILayout.Space();


		}

		private void drawTestResult(TestResult testResult)
		{
			GUIStyle logStyle;
			//Set the log style to allow rich text 
			logStyle = EditorStyles.toolbarTextField;
			logStyle.richText = true;
			logStyle.wordWrap = true;

			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			EditorGUILayout.LabelField("Date Ran: " + testResult.startTime.ToShortDateString());
			EditorGUILayout.LabelField("Started at " + testResult.startTime.ToShortTimeString() + " and ended at " + testResult.endTime.ToShortTimeString());
			EditorGUILayout.LabelField("Ran for: " + testResult.runTime);
			EditorGUILayout.LabelField("Starting Balance: " + testResult.startingCredits);
			EditorGUILayout.LabelField("Ending Balance: " + testResult.endingCredits);
			EditorGUILayout.EndVertical();
			EditorGUILayout.Space();
			
			//Grab the default color so we can set it back.
			Color bgColor = GUI.backgroundColor;

			//Lets figure out how to color the severity box
			GUI.backgroundColor = severityColor(testResult.warnings, testResult.errors, testResult.exceptions, 0);
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			//Reset the bg color to normal
			GUI.backgroundColor = bgColor;
			EditorGUILayout.LabelField("Warnings: " + testResult.warnings);
			EditorGUILayout.LabelField("Errors: " + testResult.errors);
			EditorGUILayout.LabelField("Exceptions: " + testResult.exceptions);
			EditorGUILayout.EndVertical();
			EditorGUILayout.Space();

			//No need to draw an empty box
			if (testResult.additionalInfo.Count > 0)
			{
				EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
				EditorGUILayout.LabelField("Additional Info");
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.BeginVertical(EditorStyles.helpBox);				
				additionalInfoScrollPosition = EditorGUILayout.BeginScrollView(additionalInfoScrollPosition);
				foreach (KeyValuePair<string, string> kvp in testResult.additionalInfo)
				{					
					if (GUILayout.Button("<color=#10a500><b>" + kvp.Key + "</b></color> " + kvp.Value, logStyle))
					{
						
					}
				}
				EditorGUILayout.EndScrollView();
				EditorGUILayout.EndVertical();
				EditorGUILayout.Space();
			}
						
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			showTestLogs = EditorGUILayout.Foldout(showTestLogs, "Test Logs", EditorStyles.foldout);
			EditorGUILayout.EndHorizontal();
			if (showTestLogs)
			{
				EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
				List<ZapLogType> keysList = logFilters.Keys.ToList();
				foreach (ZapLogType key in keysList)
				{
					logFilters[key] = GUILayout.Toggle(logFilters[key], new GUIContent(key.ToString() + "s"), EditorStyles.toolbarButton);
				}
				EditorGUILayout.EndHorizontal();

				logsScrollPosition = EditorGUILayout.BeginScrollView(logsScrollPosition, GUILayout.MaxHeight(400));

				foreach (ZapLog log in testResult.testLogs)
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
						if (GUILayout.Button("<color=" + log.color + "><b>[" + log.logType + "]</b></color> " + log.message.Replace("\n", " "), logStyle))
						{
							selectedLog = log;
						}
					}
				}
				GUI.backgroundColor = bgColor;
				EditorGUILayout.EndScrollView();
			}
		}

		//This should gives us a color back that is between green and red based on the number of warnings, errors, exceptions, and desyncs
		private Color severityColor(int warnings, int errors, int exceptions, int desyncs)
		{			
			//Heavily weight colors towards red for errors, exceptions, desyncs
			float r = Mathf.Clamp(((255 * (errors + exceptions + desyncs) + warnings) / 255.0f), 0.0f, 1.0f);
			float g = Mathf.Clamp(((255.0f - (5 * (errors + exceptions + desyncs))) / 255.0f), 0.0f, 1.0f);
			float b = 0.0f;
			return new Color(r, g, b);
		}

		private void setChildrenVisibilty(GUIDirectoryInfo info, bool isVisible)
		{
			foreach(GUIDirectoryInfo child in info.children)
			{
				child.visible = isVisible;
				setChildrenVisibilty(child, isVisible);
			}
		}

		private void getResultsDirectory(DirectoryInfo directoryInfo, GUIDirectoryInfo parent)
		{
			foreach (DirectoryInfo di in directoryInfo.GetDirectories())
			{
				GUIDirectoryInfo info = new GUIDirectoryInfo(di.Name, di.FullName, directoryLevel, true, parent);
				directoryDetails.Add(info);
				directoryLevel++;
				foreach (FileInfo fi in di.GetFiles())
				{
					if (!fi.Extension.Equals(".meta"))
					{
						directoryDetails.Add(new GUIDirectoryInfo(fi.Name, fi.FullName, directoryLevel, false, info));
					}
				}
				getResultsDirectory(di, info);	
				
				directoryLevel--;
			}			
		}

		/*TODO: Reevaluate this class, currently handling all the things in the directory, 
		 * however it may make more sense to split this into two or even three smaller 
		 * classes ResultRoot, Folder, File giving us more control over the filtering/searching 
		 * and how they are displayed.*/
		private class GUIDirectoryInfo
		{
			public int indentLevel = 0;
			public bool isFolder = false;
			public string name = "";
			public string path = "";
			public bool opened = true;
			public bool visible = true;
			public GUIDirectoryInfo parent;
			public List<GUIDirectoryInfo> children = new List<GUIDirectoryInfo>();

			public GUIDirectoryInfo(string fileName, string folderPath, int indent, bool folder, GUIDirectoryInfo parentDirectory)
			{
				name = fileName;
				path = folderPath;
				indentLevel = indent;
				isFolder = folder;
				parent = parentDirectory;

				if (parent != null)
				{
					parent.children.Add(this);
				}
			}

			public override string ToString()
			{
				return "[" + name + "] " + indentLevel + " - " + ((parent == null) ? "null" : parent.name);
			}
		}
	}
	#endif
}
