using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;
using Zap.Automation;
using System.Linq;

namespace Zap.Automation.Editor
{
	#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	public class ZAPJiraCreatorWindow : EditorWindow
	{
		// Styles for the GUI
		private static bool stylesInitialized = false;
		private static GUIStyle windowStyle;
		private static GUIStyle overviewPanelStyle;
		private static GUIStyle gameSelectionPanelStyle;
		private static GUIStyle gameDetailPanelStyle;
		private static GUIStyle blackTextField;

		private static GUIStyle blackTextStyle;
		private static GUIStyle pinkTextStyle;
		private static GUIStyle redTextStyle;
		private static GUIStyle yellowTextStyle;
		private static GUIStyle blueTextStyle;
		private static GUIStyle greyTextStyle;
		private static GUIStyle panelTitleStyle;
		private static GUIStyle panelHeaderStyle;
		private static GUIStyle removeButtonStyle;

		private static GUIStyle defaultLabelStyle;
		private static GUIStyle defaultToggleStyle;
		private static GUIStyle whiteToggleStyle;

		private static GUIStyle defaultFieldNameStyle;
		private static GUIStyle defaultStaticFieldStyle;
		private static GUIStyle defaultDynamicTextBoxStyle;
		private static GUIStyle defaultToolbarStyle;

		private static string[] versionsArray = null;

		private Vector2 verticalScrollbarValue = new Vector2(0.0f, -500.0f);
		private static readonly Color BLACK_TEXT_COLOR = new Color(0.0f, 0.0f, 0.0f);
		private static readonly Color BLUE_TEXT_COLOR = new Color(0.63f, 1.0f, 1.0f);
		private static readonly Color RED_TEXT_COLOR = new Color(0.95f, 0.21f, 0.21f);
		private static readonly Color PINK_TEXT_COLOR = new Color(0.95f, 0.51f, 0.95f);
		private static readonly Color WHITE_TEXT_COLOR = new Color(0.85f, 0.85f, 0.85f);
		private static readonly Color GREY_TEXT_COLOR = Color.grey;

		private static readonly string[] priorities = new string[] { "P0", "P1", "P2", "P3", "P4", "TBD" };

		private static readonly LDAPUser[] users = new LDAPUser[]
		{
		// Engineers:
		new LDAPUser("Alfred Anguiano", "aanguiano"),
		new LDAPUser("Bennett Yeates", "byeates"),
		new LDAPUser("Charles Sweet", "csweet"),
		new LDAPUser("Girish Nair", "gnair"),
		new LDAPUser("Hans Hameline", "hhameline"),
		new LDAPUser("Joel Gallant", "jgallant"),
		new LDAPUser("John Bess", "jbess"),
		new LDAPUser("Jon Sylvan", "jshelley"),
		new LDAPUser("Kevin Kralian", "kkralian"),
		new LDAPUser("Leo Schnee", "lschnee"),
		new LDAPUser("Scott Lepthien", "jlepthien"),
		new LDAPUser("MCC", "mchristensencalvin"),
		new LDAPUser("Nick Saito", "nsaito"),
		new LDAPUser("Shaun Peoples", "speoples"),
		new LDAPUser("Stephan Schwirzke", "sschwirzke"),
		new LDAPUser("Stephen Arredondo", "sarredondo"),
		new LDAPUser("Willy Lee", "wilee"),
		// Production:
		new LDAPUser("Justin Spalla", "jspalla"),
		// QA:
		new LDAPUser("Ceasar Jazmines", "cjazmines"),
		new LDAPUser("Cheri Tran", "chtran"),
		new LDAPUser("Jack Kealy", "jkealy"),
		new LDAPUser("Leon Lai", "leon"),
		// Custom:
		new LDAPUser("Custom", ""), // Option to put in your own user.
		};

		private static readonly LDAPUserList userList = new LDAPUserList(users);
		private Dictionary<GUIStyle, Dictionary<string, float>> maxWidthOfFieldNameCache = new Dictionary<GUIStyle, Dictionary<string, float>>();
		private const string JIRA_WEBSITE_URL_FORMAT = "https://jira.corp.zynga.com/browse/{0}";

		//END GUI STYLES

		private ZAPJiraData zapJIRAData = null;
		private float maxWidthOfFieldName = 25;
		public JSON resultJSON = null;
		private int LDAPUserIndex = userList.list.Length - 1;
		public int fixVersionsArrayIndex = 0;
		public int affectsVersionsArrayIndex = 0;
		public static ZAPJiraCreatorWindow jiraCreatorWindow;

		public static string windowTitle = "ZAP Jira";

		[MenuItem("Zynga/ZAP/Create Jira", false, 406)]
		public static void showWindow()
		{
			ZAPJiraData defaultZapJIRAData = new ZAPJiraData();
			init(defaultZapJIRAData);
		}

		public static void init(ZAPJiraData jiraData)
		{
			if (jiraCreatorWindow == null)
			{
				jiraCreatorWindow = GetWindow<ZAPJiraCreatorWindow>(windowTitle, true, new[] { typeof(ZAPResultsWindow), typeof(ZAPObserverWindow), typeof(ZAPTestPlanEditor), typeof(SceneView) });
			}

			if (versionsArray == null)
			{
				jiraCreatorWindow.populateVersionArray();

				if (versionsArray != null)
				{
					jiraCreatorWindow.fixVersionsArrayIndex = versionsArray.Length - 1;
					jiraCreatorWindow.affectsVersionsArrayIndex = versionsArray.Length - 1;
				}
			}

			if (jiraData != null)
			{
				jiraCreatorWindow.zapJIRAData = jiraData;
			}

			jiraCreatorWindow.Show();
			jiraCreatorWindow.Focus();
		}

		private void OnGUI()
		{
			if (!stylesInitialized)
			{
				initStyles();
			}
			if (stylesInitialized)
			{
				drawControlPanel();
			}
		}

		public void populateVersionArray()
		{
			string URL = "https://jira.corp.zynga.com/rest/api/2/project/HIR/versions";
			WWWForm form = new WWWForm();
			Dictionary<string, string> headers = form.headers;
			string auth = "Basic " + System.Convert.ToBase64String(
				System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(ZAPJiraData.USER_NAME + ":" + ZAPJiraData.PASSWORD));
			headers["Authorization"] = auth;
			headers["Content-Type"] = "application/json";

			UnityWebRequest www = UnityWebRequest.Get(URL);

			foreach (KeyValuePair<string, string> kvp in headers)
			{
				www.SetRequestHeader(kvp.Key, kvp.Value);
			}

			AsyncOperation operation = www.Send();
			while (!operation.isDone)
			{
				Debug.Log("Getting Versions Information: " + (operation.progress * 100.0f) + "% done.");
			}
			Debug.Log("Getting Versions Information: " + (operation.progress * 100.0f) + "% done.");

			string result = "";

			if (www.isNetworkError)
			{
				Debug.Log(www.error);
				result = www.error;
			}
			else
			{
				Debug.Log(www.downloadHandler.text);
				result = www.downloadHandler.text;
			}
			result = @"{ ""versions"": " + result + " }"; // We need to add a key here since the URL is truncating it off. 
			JSON versionJSONParent = new JSON(result);

			JSON[] versionJSONArray = versionJSONParent.getJsonArray("versions");
			List<string> versionStrings = new List<string>();
			if (versionJSONArray.Length > 0)
			{
				// We have something
				foreach (JSON versionJSON in versionJSONArray)
				{
					if (!versionJSON.getBool("released", false))
					{
						// This version has not been released.
						string name = versionJSON.getString("name", "");
						if (!string.IsNullOrEmpty(name) && name.Contains("Mobile Release"))
						{
							// We want to add this!
							versionStrings.Add(name);
						}
					}
				}

				if (versionStrings.Count > 0)
				{
					versionStrings.Add("No Version");
					fixVersionsArrayIndex = 0;//versionsArray.Length - 1;
					affectsVersionsArrayIndex = 0;//versionsArray.Length - 1;
					versionsArray = versionStrings.ToArray();
				}
			}
			else
			{
				Debug.LogError("Couldn't find the version information from " + URL);
			}
		}


		// This is the main method for drawing the control panel.
		public void drawControlPanel()
		{
			EditorGUILayout.BeginHorizontal(windowStyle);
			
			verticalScrollbarValue = GUILayout.BeginScrollView(
				verticalScrollbarValue,
				false,
				true
			);

			if (versionsArray != null)
			{
				if (resultJSON != null)
				{
					// Make a button here that lets you click into the JIRA ticket.
					if (GUILayout.Button("Open " + string.Format(JIRA_WEBSITE_URL_FORMAT, resultJSON.getString("key", ""))))
					{
						Application.OpenURL(string.Format(JIRA_WEBSITE_URL_FORMAT, resultJSON.getString("key", "")));
					}
				}
				else
				{
					drawJIRACreationSection();
					drawJiraSubmitButton();
				}
			}

			GUILayout.EndScrollView();
			EditorGUILayout.EndHorizontal();
		}

		private void drawJiraSubmitButton()
		{
			float heightOfTitle = GUI.skin.button.CalcSize(new GUIContent("Submit JIRA")).y;
			if (GUILayout.Button("Submit JIRA", GUILayout.Height(heightOfTitle)))
			{
				if (zapJIRAData != null)
				{
					zapJIRAData.resultJSON = null;
					zapJIRAData.sendRequestNoCoroutine();
				}
			}

			if(zapJIRAData != null && 
				zapJIRAData.jiraSubmitOperation != null &&
				zapJIRAData.jiraSubmitOperation.isDone && 
				zapJIRAData.resultJSON == null)
			{
				getJiraSumbitResults();
			}
		}

		private void getJiraSumbitResults()
		{
			string result = "";

			if (zapJIRAData.jiraSubmitTicketWebRequest.isNetworkError)
			{
				Debug.Log(zapJIRAData.jiraSubmitTicketWebRequest.error);
				result = zapJIRAData.jiraSubmitTicketWebRequest.error;
			}
			else
			{
				Debug.Log(zapJIRAData.jiraSubmitTicketWebRequest.downloadHandler.text);
				result = zapJIRAData.jiraSubmitTicketWebRequest.downloadHandler.text;
			}

			resultJSON = new JSON(result);
		}

		// Draw the table of contents for the user to select which game or test plan to view.
		private void drawJIRACreationSection()
		{
			GUILayout.Label("JIRA Maker", panelTitleStyle);

			createStaticField("Project: ", zapJIRAData.project);
			createStaticField("Game: ", string.Format("{0} ({1})", zapJIRAData.gameKey, zapJIRAData.gameName));

			zapJIRAData.summary = createDynamicTextBox("Summary: ", zapJIRAData.summary); // Make Dynamic Text field
			zapJIRAData.priority = (JIRAPriority)createToolbar("Priority: ", (int)zapJIRAData.priority, 6, priorities); // Made dynamic dropdown
			createStaticField("Spin Number: ", zapJIRAData.spinNumber + "");

			//TODO: Set up this so that it auto figures out which affects version it should use.
			//affectsVersionsArrayIndex = createToolbar("Fix Version: ", affectsVersionsArrayIndex, 4, versionsArray); // Made dynamic dropdown
			//gameJIRAdata.affectsVersion = versionsArray[affectsVersionsArrayIndex];

			fixVersionsArrayIndex = createToolbar("Fix Version: ", fixVersionsArrayIndex, 4, versionsArray); // Made dynamic dropdown
			zapJIRAData.fixVersion = versionsArray[fixVersionsArrayIndex];
			if (fixVersionsArrayIndex == versionsArray.Length - 1)
			{
				zapJIRAData.fixVersion = "";
			}

			LDAPUserIndex = createToolbar("Assignee: ", LDAPUserIndex, 4, userList.userNames); // Made dynamic dropdown
			if (LDAPUserIndex == userList.list.Length - 1)
			{
				zapJIRAData.assignee = createDynamicTextBox("Custom LDAP Assignee: ", zapJIRAData.assignee);
			}
			else
			{
				zapJIRAData.assignee = userList.userKeys[LDAPUserIndex];
			}

			createStaticField("LDAP Reporter: ", ZAPJiraData.USER_NAME);
			createStaticField("Stack Trace: ", zapJIRAData.stackTrace);
			zapJIRAData.notes = createDynamicTextBox("Notes: ", zapJIRAData.notes); // Make dynamic text field
		}

		public float getMaxWidthOfFieldName(GUIStyle style, string title)
		{
			if (!maxWidthOfFieldNameCache.ContainsKey(style))
			{
				maxWidthOfFieldNameCache[style] = new Dictionary<string, float>();
			}
			if (!maxWidthOfFieldNameCache[style].ContainsKey(title))
			{
				maxWidthOfFieldNameCache[style][title] = style.CalcSize(new GUIContent(title)).x;
				// update the cache size
				maxWidthOfFieldName = Mathf.Max(maxWidthOfFieldNameCache[style][title], maxWidthOfFieldName);
			}

			return maxWidthOfFieldName;
		}

		private void createStaticField(string title, string value)
		{
			// Get the width of the field name.
			maxWidthOfFieldName = getMaxWidthOfFieldName(defaultFieldNameStyle, title);
			GUILayout.BeginHorizontal();
			GUILayout.Label(title, defaultFieldNameStyle, GUILayout.Width(maxWidthOfFieldName));
			GUILayout.Label(value, defaultStaticFieldStyle);
			GUILayout.EndHorizontal();
		}

		private string createDynamicTextBox(string title, string value)
		{
			// Get the width of the field name.
			maxWidthOfFieldName = getMaxWidthOfFieldName(defaultFieldNameStyle, title);
			GUILayout.BeginHorizontal();
			GUILayout.Label(title, defaultFieldNameStyle, GUILayout.Width(maxWidthOfFieldName));
			value = GUILayout.TextField(value, defaultDynamicTextBoxStyle);
			GUILayout.EndHorizontal();
			return value;
		}

		Dictionary<string, GUIToolBarHelper> titleToToolBar = new Dictionary<string, GUIToolBarHelper>();

		private int createToolbar(string title, int selectedOptionIndex, int maxOptionsPerLine, string[] options)
		{
			if (!titleToToolBar.ContainsKey(title))
			{
				titleToToolBar[title] = new GUIToolBarHelper(title, selectedOptionIndex, maxOptionsPerLine, options);
			}
			return titleToToolBar[title].drawToolbar(this);
		}

		// Initialize the styles here so we don't have to do it every frame.
		public void initStyles()
		{
			Texture2D blackBackground = Texture2D.blackTexture;
			blackBackground.Resize(512, 512);

			Texture2D greyBackground = new Texture2D(1, 1);
			greyBackground.SetPixel(0, 0, Color.grey);
			greyBackground.Apply();

			// Set the style for the whole control panel window.
			windowStyle = new GUIStyle(GUI.skin.box);

			// Set the style for the window components.
			gameSelectionPanelStyle = new GUIStyle(GUI.skin.box);
			gameSelectionPanelStyle.normal.textColor = WHITE_TEXT_COLOR;
			gameSelectionPanelStyle.normal.background = blackBackground; // Use a black background to make text clearer

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

			blackTextStyle = new GUIStyle(blackTextField);
			blackTextStyle.normal.textColor = BLACK_TEXT_COLOR;

			pinkTextStyle = new GUIStyle(blackTextField);
			pinkTextStyle.normal.textColor = PINK_TEXT_COLOR;

			redTextStyle = new GUIStyle(blackTextField);
			redTextStyle.normal.textColor = RED_TEXT_COLOR;

			yellowTextStyle = new GUIStyle(blackTextField);
			yellowTextStyle.normal.textColor = Color.yellow;

			blueTextStyle = new GUIStyle(blackTextField);
			blueTextStyle.normal.textColor = BLUE_TEXT_COLOR;

			greyTextStyle = new GUIStyle(blackTextField);
			greyTextStyle.normal.textColor = GREY_TEXT_COLOR;

			defaultFieldNameStyle = new GUIStyle(blackTextField);
			defaultFieldNameStyle.alignment = TextAnchor.LowerRight;
			defaultFieldNameStyle.fontSize = 20;

			defaultStaticFieldStyle = new GUIStyle(greyTextStyle);
			defaultStaticFieldStyle.alignment = TextAnchor.LowerLeft;
			defaultStaticFieldStyle.fontSize = 20;

			defaultDynamicTextBoxStyle = new GUIStyle(blackTextStyle);
			defaultDynamicTextBoxStyle.alignment = TextAnchor.UpperLeft;
			defaultDynamicTextBoxStyle.normal.background = greyBackground;
			defaultDynamicTextBoxStyle.fontSize = 20;

			defaultToolbarStyle = new GUIStyle(GUI.skin.button);
			defaultToolbarStyle.wordWrap = true;

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

		private class GUIToolBarHelper
		{
			// TODO: Make this handle updates to its data.
			string title = null; // The title assosiated with this toolbar.
			int selectedOptionIndex; // The option that is currently selected for this.
			int maxOptionsPerLine = -1; // Number of options that should be in each line.

			string[][] options = null;

			public GUIToolBarHelper(string title, int selectedOptionIndex, int maxOptionsPerLine, string[] options)
			{
				this.title = title;
				this.selectedOptionIndex = selectedOptionIndex;
				this.maxOptionsPerLine = maxOptionsPerLine;

				populateOptionsFromArray(options);

			}

			private void populateOptionsFromArray(string[] array)
			{
				int numberOfRows = (array.Length - 1) / maxOptionsPerLine + 1;
				options = new string[numberOfRows][];

				for (int i = 0; i < numberOfRows; i++)
				{

					int startingIndex = i * maxOptionsPerLine;
					int numberOfOptionsInRow = array.Length - startingIndex;
					if (numberOfOptionsInRow > maxOptionsPerLine)
					{
						// The last row might have less options in it.
						numberOfOptionsInRow = maxOptionsPerLine;
					}

					options[i] = new string[numberOfOptionsInRow];
					System.Array.Copy(array, startingIndex, options[i], 0, numberOfOptionsInRow);

				}
			}

			public int drawToolbar(ZAPJiraCreatorWindow jiraCreator)
			{
				if (!string.IsNullOrEmpty(title) && maxOptionsPerLine > 0 && options != null && ZAPJiraCreatorWindow.stylesInitialized)
				{
					jiraCreator.maxWidthOfFieldName = jiraCreator.getMaxWidthOfFieldName(ZAPJiraCreatorWindow.defaultFieldNameStyle, title);
					float heightOfTitle = ZAPJiraCreatorWindow.defaultFieldNameStyle.CalcSize(new GUIContent(title)).y;
					GUILayout.BeginHorizontal();
					GUILayout.Label(title, ZAPJiraCreatorWindow.defaultFieldNameStyle, GUILayout.Width(jiraCreator.maxWidthOfFieldName));
					GUILayout.BeginVertical();
					for (int i = 0; i < options.Length; i++)
					{

						int startingIndex = i * maxOptionsPerLine;
						int numberOfOptionsInRow = options.Length - startingIndex;
						if (numberOfOptionsInRow > maxOptionsPerLine)
						{
							numberOfOptionsInRow = maxOptionsPerLine;
						}

						int selectedOptionForRow = -1;
						if (selectedOptionIndex >= i * maxOptionsPerLine &&
							selectedOptionIndex < ((i + 1) * maxOptionsPerLine))
						{
							selectedOptionForRow = selectedOptionIndex - (i * maxOptionsPerLine);
						}

						selectedOptionForRow = GUILayout.Toolbar(
							selectedOptionForRow,
							options[i],
							defaultToolbarStyle,
							GUILayout.Height(heightOfTitle),
							GUILayout.Width(jiraCreatorWindow.position.width));
						if (selectedOptionForRow != -1)
						{
							selectedOptionIndex = i * maxOptionsPerLine + selectedOptionForRow;
						}

					}
					GUILayout.EndVertical();
					GUILayout.EndHorizontal();


				}

				return selectedOptionIndex;
			}

		}

		private class LDAPUserList
		{
			public LDAPUser[] list;
			public string[] userNames;
			public string[] userKeys;

			public LDAPUserList(LDAPUser[] userArray)
			{
				list = userArray;
				userNames = new string[list.Length];
				userKeys = new string[list.Length];
				for (int i = 0; i < list.Length; i++)
				{
					userNames[i] = list[i].userName;
					userKeys[i] = list[i].userKey;
				}
			}
		}

		private class LDAPUser
		{
			public string userName;
			public string userKey;
			public LDAPUser(string userName, string userKey)
			{
				this.userName = userName;
				this.userKey = userKey;
			}
		}
	}
	#endif
}
