using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;

// This class creates the editor window that allows us to create JIRA tickets in the Unity Editor.
// Author: Leo Schnee
// Date: 5/3/2017


public class AutomatedPlayerJIRACreator : EditorWindow 
{
#if ZYNGA_TRAMP

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

	public static string[] versionsArray = null;


	private Vector2 verticalScrollbarValue = new Vector2(0.0f, -500.0f);

	private static readonly Color BLACK_TEXT_COLOR = new Color(0.0f, 0.0f, 0.0f);
	private static readonly Color BLUE_TEXT_COLOR = new Color(0.63f, 1.0f, 1.0f);
	private static readonly Color RED_TEXT_COLOR = new Color(0.95f, 0.21f, 0.21f);
	private static readonly Color PINK_TEXT_COLOR = new Color(0.95f, 0.51f, 0.95f);
	private static readonly Color WHITE_TEXT_COLOR = new Color(0.85f, 0.85f, 0.85f);
	private static readonly Color GREY_TEXT_COLOR = Color.grey;

	private static readonly string[] priorities = new string[] {"P0", "P1", "P2", "P3", "P4", "TBD"};

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


	// Store panel and window sizes
	private const float windowHeight = 900.0f;
	private const float windowWidth = 900.0f;
	private const string JIRA_WEBSITE_URL_FORMAT = "https://jira.corp.zynga.com/browse/{0}";

	// Calculated the minimum size of the window based on which panels are active.
	private Vector2 windowSize ()
	{
		float selection = gameSelectionPanelSize.x;
		return new Vector2(selection, windowHeight);
	}
	private Vector2 gameSelectionPanelSize = new Vector2(windowWidth, windowHeight);


	//END GUI STYLES

	private AutomatedGameJIRAData gameJIRAdata = null;
	private float maxWidthOfFieldName = 25;
	public JSON resultJSON = null;
	private JiraSender jiraSender = null;
	private int LDAPUserIndex = userList.list.Length - 1;
	public int fixVersionsArrayIndex = 0;
	public int affectsVersionsArrayIndex = 0;


	[MenuItem("JIRA/AutomatedPlayerJIRACreator")]
	public static void showWindow ()
	{
		AutomatedGameJIRAData gameJIRAdata = new AutomatedGameJIRAData(null, null, "[Not Given]");
		AutomatedPlayerJIRACreator.init(gameJIRAdata);
	}

	public static AutomatedPlayerJIRACreator init(AutomatedGameJIRAData data)
	{
		AutomatedPlayerJIRACreator instance = null;
		
		if (data != null)
		{
			if (instance == null)
			{
				// Get existing open window or if none, make a new one:
				instance = (AutomatedPlayerJIRACreator)EditorWindow.CreateInstance(typeof(AutomatedPlayerJIRACreator));
				instance.gameJIRAdata = data;
				instance.Show();
				instance.Focus();
				instance.minSize = instance.windowSize();
				instance.populateVersionArray();

				if (versionsArray != null)
				{
					instance.fixVersionsArrayIndex = versionsArray.Length - 1;
					instance.affectsVersionsArrayIndex = versionsArray.Length - 1;
				}

			}
		}
		else
		{
			Debug.LogError("AutomatedGameJIRAData is null, can't make this JIRA ticket.");
		}

		return instance;
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
		Dictionary<string,string> headers = form.headers;
		string auth = "Basic " + System.Convert.ToBase64String(
			System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(AutomatedGameJIRAData.USER_NAME + ":" + AutomatedGameJIRAData.PASSWORD));
		headers["Authorization"] = auth;
		headers["Content-Type"] = "application/json";

		UnityWebRequest www = UnityWebRequest.Get(URL);

		foreach (KeyValuePair<string,string> kvp in headers)
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
		//EditorGUILayout.BeginVertical(gameSelectionPanelStyle);
		verticalScrollbarValue = GUILayout.BeginScrollView(
			verticalScrollbarValue,
			false,
			true
		);
		if (versionsArray != null)
		{
			if (resultJSON == null)
			{
				drawJIRACreationSection();
				float heightOfTitle = GUI.skin.button.CalcSize(new GUIContent("Submit JIRA")).y;
				if (GUILayout.Button("Submit JIRA", GUILayout.Height(heightOfTitle)))
				{
					if (jiraSender == null)
					{
						jiraSender = JiraSender.create(gameJIRAdata, jiraSenderCallback);
						jiraSender.StartCoroutine(jiraSender.sendJiraRequest());
					}
				}
			}
			else
			{
				// Make a button here that lets you click into the JIRA ticket.
				if (GUILayout.Button("Open " + string.Format(JIRA_WEBSITE_URL_FORMAT, resultJSON.getString("key", ""))))
				{
					Application.OpenURL(string.Format(JIRA_WEBSITE_URL_FORMAT, resultJSON.getString("key", "")));
				}
			}
		}
		GUILayout.EndScrollView();

		EditorGUILayout.EndHorizontal();
	}

	private void jiraSenderCallback()
	{
		resultJSON = gameJIRAdata.resultJSON;

		if (resultJSON != null && resultJSON.getString("key", "") == "")
		{
			resultJSON = null; // Something went wrong and this didn't send.
		}
		jiraSender = null;
	}

	// Draw the table of contents for the user to select which game or test plan to view.
	private void drawJIRACreationSection()
	{
		GUILayout.Label("JIRA Maker", panelTitleStyle);

		createStaticField("Project: ", gameJIRAdata.project);
		createStaticField("Game: ", string.Format("{0} ({1})", gameJIRAdata.gameKey, gameJIRAdata.gameName));

		gameJIRAdata.summary = createDynamicTextBox("Summary: ", gameJIRAdata.summary); // Make Dynamic Text field
		gameJIRAdata.priority = (JIRAPriority)createToolbar("Priority: ", (int)gameJIRAdata.priority, 6, priorities); // Made dynamic dropdown
		createStaticField("Spin Number: ", gameJIRAdata.spinNumber + "");

		//TODO: Set up this so that it auto figures out which affects version it should use.
		//affectsVersionsArrayIndex = createToolbar("Fix Version: ", affectsVersionsArrayIndex, 4, versionsArray); // Made dynamic dropdown
		//gameJIRAdata.affectsVersion = versionsArray[affectsVersionsArrayIndex];

		fixVersionsArrayIndex = createToolbar("Fix Version: ", fixVersionsArrayIndex, 4, versionsArray); // Made dynamic dropdown
		gameJIRAdata.fixVersion = versionsArray[fixVersionsArrayIndex];
		if (fixVersionsArrayIndex == versionsArray.Length - 1)
		{
			gameJIRAdata.fixVersion = "";
		}

		LDAPUserIndex = createToolbar("Assignee: ", LDAPUserIndex, 4, userList.userNames); // Made dynamic dropdown
		if (LDAPUserIndex == userList.list.Length - 1)
		{
			gameJIRAdata.assignee = createDynamicTextBox("Custom LDAP Assignee: ", gameJIRAdata.assignee);
		}
		else
		{
			gameJIRAdata.assignee = userList.userKeys[LDAPUserIndex];
		}

		createStaticField("LDAP Reporter: ", AutomatedGameJIRAData.USER_NAME);
		createStaticField("Stack Trace: ", gameJIRAdata.stackTrace);
		gameJIRAdata.notes = createDynamicTextBox("Notes: ", gameJIRAdata.notes); // Make dynamic text field
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
		return titleToToolBar[title].draw(this);
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
		gameSelectionPanelStyle.fixedWidth = gameSelectionPanelSize.x;
		gameSelectionPanelStyle.fixedHeight = gameSelectionPanelSize.y;
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

		public int draw(AutomatedPlayerJIRACreator jiraCreator)
		{
			if (!string.IsNullOrEmpty(title) && maxOptionsPerLine > 0 && options != null && AutomatedPlayerJIRACreator.stylesInitialized)
			{
				jiraCreator.maxWidthOfFieldName = jiraCreator.getMaxWidthOfFieldName(AutomatedPlayerJIRACreator.defaultFieldNameStyle, title);
				float heightOfTitle = AutomatedPlayerJIRACreator.defaultFieldNameStyle.CalcSize(new GUIContent(title)).y;
				GUILayout.BeginHorizontal();
					GUILayout.Label(title, AutomatedPlayerJIRACreator.defaultFieldNameStyle, GUILayout.Width(jiraCreator.maxWidthOfFieldName));
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
							GUILayout.Width(windowWidth));
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

	private class JiraSender : MonoBehaviour
	{
		public delegate void postJiraCallback();
		private AutomatedGameJIRAData gameJIRAdata;
		private postJiraCallback callback;
		private static GameObject go = null;

		public static JiraSender create(AutomatedGameJIRAData jiraData, postJiraCallback cb)
		{
			if (go == null)
			{
				go = new GameObject("AutomatedJiraSubmitter");
			}
			JiraSender instance = go.AddComponent<JiraSender>();

			instance.gameJIRAdata = jiraData;
			instance.callback = cb;
			return instance;
		}

		public IEnumerator sendJiraRequest()
		{
			yield return gameJIRAdata.sendRequest();
			callback();
			DestroyImmediate(JiraSender.go);
		}
	}

#endif
}
