using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using System.Net;

/**
This is a Unity Editor helper class to set user login settings from the menu.
To add more users, search the below code for all instances of "NEW USER GOES HERE".
*/
public class EditorLoginSettings : ScriptableWizard
{
	public bool showOptionalLogs;   // Whether or not to show optional logs in the editor [e.g. the server communication logs]
	public bool serverDebugging;    // Whether or not the server is given the debug flag
	public bool automation;         // Whether or not we want automation stuff to be turned on
	public LogType minLogLevel;		// Controls the minimum level of logging that will be displayed to console
	public bool useAssetBundles;	// Whether or not to use asset bundles in the Editor.
	public bool useLocalBundles;	// Whether or not to use local asset bundles in the Editor.
	public bool streamNullTextures;	// If true, all streamed textures are null
	public float streamTexturesDelay;	// Amount of seconds to delay streaming, to simulate slow downloading.
	public string playerLocale;
	public SystemLanguage playerCurrentLanguage;
	public string[] configFiles = null;
	public int selectedConfigFileIndex;
	public string selectedConfigFile;
	public string statusString = "";
	public bool isCrappyDevice;	// Whether or not to make MobileUIUtils simulate a crappy device
	public string dotsPerInch;
	public string targetFrameRate;
	public string localURL;
	public string simulatedInstalledAppIDs;
	public string lobbyInboxIconGroup;
	public string lobbyInboxIconGame;
	public bool showStartupMOTDs;	// (editor only) If false, don't show MOTD's upon startup.
	public bool markMOTDsSeen = true;
	public string simulatedZID; // user player data from another ZID
	public bool useSDBundles; // when set to true, has AssetBundleVariants.getIdealVariantForThisDevice() return the SD bundle variant
	public bool launchDirectlyIntoGame; // will skip initial lobby and launch into game
	public string launchDirectlyGameKey; // gamekey for game to directly load into
	public bool launchDirectlyIntoGameAlways; // never can exit game and load lobby, instead will keep reloading same game
	public bool dialogPopupDefault; // enable the dialog blocker during gameplay
	public float normalizedPortraitModeSafeAreaHeight; // Normalized safe area height when the game is displayed in portrait mode (allows simulation in editor of Screen.safeArea from Unity which tells where the notch on some phones is)
	public bool isForcingWebglLandscapeInPortraitModeGames; // Toggle that will disable the auto switching to Portrait mode while in the Editor.  Useful if you want to see how WebGL will function, or in Audio's case had issues recording the game when it changed aspect ratios.
	public bool isForcingWebglDotCom; // Toggle that will force WebGL to be in DotCom mode.

	private const string DEFAULT_DEV_CONFIG = "hir_mobile_dev_config.txt";

	private GUIContent guiContent = new GUIContent(); // for easier tooltip functions
	

#if ZYNGA_PRODUCTION
	public bool isProduction = true;
#else
	public bool isProduction = false;
#endif

	private string suggestedGameRes = "";
	private string[] dotsPerInchOptions = new string[] { "", "320", "160" };
	private string[] targetFrameRateOptions = new string[] { "", "60", "30", "20", "15", "1" };

	private enum DevicePresets
	{
		SelectDevice,
		iPhone4,
		iPhone5,
		iPadMini,
		iPad4,
		FourByThreeSmallDevice,
		EightByFiveTablet,
		EightByFiveTabletHD
	}

	[MenuItem ("Zynga/Game Login Settings %#l")]static void CreateWizard()
	{
		ScriptableWizard.DisplayWizard<EditorLoginSettings>("Game Login Settings", "Close");
	}

	public void OnEnable()
	{
		showOptionalLogs = (SlotsPlayer.getPreferences().GetInt(DebugPrefs.OPTIONAL_LOGS, 0) != 0);		// Whether or not to show optional logs in the editor [e.g. the server communication logs]
		serverDebugging = (SlotsPlayer.getPreferences().GetInt(DebugPrefs.SERVER_DEBUGGING, 0) != 0);	// Whether or not the server is given the debug flag
		automation = (SlotsPlayer.getPreferences().GetInt(DebugPrefs.ZYNGA_TRAMP, 0) != 0); // Whether or not we want automation stuff to be turned on
		minLogLevel = (LogType)(SlotsPlayer.getPreferences().GetInt(DebugPrefs.MIN_LOG_LEVEL, (int)LogType.Log)); // Controls the minimum level of logging that will be displayed to console
		useAssetBundles = (SlotsPlayer.getPreferences().GetInt(DebugPrefs.USE_ASSET_BUNDLES, 0) != 0);	// Whether or not to use asset bundles in the Editor.
		useLocalBundles = (SlotsPlayer.getPreferences().GetInt(DebugPrefs.USE_LOCAL_BUNDLES, 0) != 0);	// Whether or not to use local asset bundles in the Editor.
		streamNullTextures = (SlotsPlayer.getPreferences().GetInt(DebugPrefs.STREAM_NULL_TEXTURES, 0) != 0);	// If true, all streamed textures are null
		streamTexturesDelay = SlotsPlayer.getPreferences().GetFloat(DebugPrefs.STREAM_TEXTURES_DELAY, 0.0f);	// Amount of seconds to delay streaming, to simulate slow downloading.
		playerLocale = SlotsPlayer.getPreferences().GetString(DebugPrefs.LOCALE, "en_US");
		string lang = SlotsPlayer.getPreferences().GetString(DebugPrefs.CURRENT_LANGUAGE, "English");
		playerCurrentLanguage = (SystemLanguage)System.Enum.Parse(typeof(SystemLanguage), string.IsNullOrEmpty(lang) ? "English" : lang);
		isCrappyDevice = (SlotsPlayer.getPreferences().GetInt(DebugPrefs.IS_CRAPPY_DEVICE, 0) != 0);	// Whether or not to make MobileUIUtils simulate a crappy device
		dotsPerInch = SlotsPlayer.getPreferences().GetString(DebugPrefs.DOTS_PER_INCH, "");
		targetFrameRate = SlotsPlayer.getPreferences().GetString(DebugPrefs.TARGET_FRAME_RATE, "");
		localURL = SlotsPlayer.getPreferences().GetString(DebugPrefs.LOCAL_URL, "");
		simulatedInstalledAppIDs = SlotsPlayer.getPreferences().GetString(DebugPrefs.SIMULATED_INSTALLED_APP_IDS, "");
		lobbyInboxIconGroup = SlotsPlayer.getPreferences().GetString(DebugPrefs.LOBBY_INBOX_ICON_GROUP, "");
		lobbyInboxIconGame = SlotsPlayer.getPreferences().GetString(DebugPrefs.LOBBY_INBOX_ICON_GAME, "");
		showStartupMOTDs = (SlotsPlayer.getPreferences().GetInt(DebugPrefs.SHOW_STARTUP_MOTDS, 1) == 1);	// (editor only) If false, don't show MOTD's upon startup.
		markMOTDsSeen = (SlotsPlayer.getPreferences().GetInt(DebugPrefs.MARK_MOTDS_SEEN, 1) == 1);	// (editor only) If false, don't mark  MOTD's as seen
		launchDirectlyIntoGame = (SlotsPlayer.getPreferences().GetInt(DebugPrefs.LAUNCH_DIRECTLY_INTO_GAME, 0) != 0); // true:launches directly into game instead of lobby
		launchDirectlyGameKey = SlotsPlayer.getPreferences().GetString(DebugPrefs.LAUNCH_DIRECTLY_GAME_KEY, "gen01"); // game key to directly launch into
		launchDirectlyIntoGameAlways = (SlotsPlayer.getPreferences().GetInt(DebugPrefs.LAUNCH_DIRECTLY_INTO_GAME_ALWAYS, 0) != 0); // true:when tring to load lobby from directly loaded game, will reload game instead
		dialogPopupDefault = (SlotsPlayer.getPreferences().GetInt(DebugPrefs.SUPPRESS_ALL_DIALOGS, 0) != 0);
		normalizedPortraitModeSafeAreaHeight = SlotsPlayer.getPreferences().GetFloat(DebugPrefs.NORMALIZED_PORTRAIT_MODE_SAFE_AREA_OFFSET, 0.0f); // (editor only) Controls simulated portrait mode safe area to account for notches on phones, on device a value returned by Unity will be used.
		isForcingWebglLandscapeInPortraitModeGames = SlotsPlayer.getPreferences().GetBool(DebugPrefs.FORCE_WEBGL_LANDSCAPE_IN_PORTRAIT_MODE_GAMES, false); // (editor only) Allows the auto swapping to Portrait mode while in editor to be disabled (which would work similar to how the game will function in WebGL).
		isForcingWebglDotCom = SlotsPlayer.getPreferences().GetBool(DebugPrefs.FORCE_WEBGL_DOTCOM_MODE, false); // (editor only) Use to toggle webgl into "DotCom" client.
	}

	public void OnWizardUpdate()
	{
		if (configFiles == null)
		{
			configFiles = getConfigFiles();
		}

		if (configFiles != null)
		{
			resetToSavedSelectedConfig();
		}
	}

	/// Needs to exist as part of a ScriptableWizard
	public void OnWizardCreate()
	{
		configFiles = getConfigFiles();
	}

	private string[] getConfigFiles()
	{
		string configFilePath = Application.dataPath + "/Resources/Config";
		string[] configFiles = System.IO.Directory.GetFiles(configFilePath, "*.txt");
		if (configFiles != null)
		{
			for (int i = 0; i < configFiles.Length; i++)
			{
				configFiles[i] = System.IO.Path.GetFileName(configFiles[i]);
			}
		}
		return configFiles;
	}

	private void resetToSavedSelectedConfig()
	{
		selectedConfigFile = !string.IsNullOrEmpty(SharedConfig.currentConfigName) ? SharedConfig.currentConfigName : getSavedSelectedConfig();
		selectedConfigFileIndex = System.Array.IndexOf(configFiles, selectedConfigFile);
		
		if (selectedConfigFileIndex == -1)
		{
			if (!isProduction)
			{
				selectConfigFileByIndex(System.Array.IndexOf(configFiles, DEFAULT_DEV_CONFIG));
			}
			else
			{
				selectConfigFileByIndex(0);
			}
			
			Debug.LogWarning(string.Format("No saved selected config found in prefs, using default config {0}", selectedConfigFile));
		}
	}

	private string getSavedSelectedConfig()
	{
		string prefKey = DebugPrefs.EDITOR_CONFIG_FILE + SkuResources.currentSku.ToString();
		return SlotsPlayer.getPreferences().GetString(prefKey, "");
	}

	private void saveSelectedConfigFile()
	{
		string prefKey = DebugPrefs.EDITOR_CONFIG_FILE + SkuResources.currentSku.ToString();
		string current = getSavedSelectedConfig();
		if (current != selectedConfigFile)
		{
			Debug.LogFormat("Resetting the live data versionining after switching environments.");
			// Reset this live data if we are swapping environments.
			SlotsPlayer.getPreferences().SetString(Prefs.LIVE_DATA_VERSION, "0");
		}
		SlotsPlayer.getPreferences().SetString(prefKey, selectedConfigFile);
	}

	public void OnGUI()
	{
		GUILayout.BeginHorizontal();
		showOptionalLogs = GUILayout.Toggle(showOptionalLogs, "Show Optional Logs");
		serverDebugging = GUILayout.Toggle(serverDebugging, "Server Debugging");
		automation = GUILayout.Toggle(automation, "Automation");
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		minLogLevel = (LogType)EditorGUILayout.EnumPopup("Min Log Level", minLogLevel);
		GUILayout.EndHorizontal();
		
		if (configFiles != null)
		{
			selectedConfigFileIndex = Mathf.Clamp(selectedConfigFileIndex, 0, configFiles.Length);
			selectConfigFileByIndex(EditorGUILayout.Popup(selectedConfigFileIndex, configFiles));
		}

		GUILayout.BeginHorizontal();
		this.isProduction = GUILayout.Toggle(isProduction, "Compile for Production", GUILayout.Width(150));
		GUILayout.EndHorizontal();

		this.playerLocale = EditorGUILayout.TextField("Player locale", this.playerLocale, GUILayout.Width(400));
		this.playerCurrentLanguage = (SystemLanguage)EditorGUILayout.EnumPopup("Player current language", this.playerCurrentLanguage);
		
		GUILayout.BeginHorizontal();
		useAssetBundles = GUILayout.Toggle(useAssetBundles, "Use Asset Bundles", GUILayout.Width(150));
		useLocalBundles = GUILayout.Toggle(useLocalBundles, "Use Local Asset Bundles", GUILayout.Width(150));
		useSDBundles = GUILayout.Toggle(useSDBundles, "Use SD Bundles", GUILayout.Width(150));
		GUILayout.EndHorizontal();

		GUILayout.Space(20);
		GUILayout.BeginHorizontal();
		DevicePresets selectedPreset = (DevicePresets)EditorGUILayout.EnumPopup("Simulated Device Presets:", DevicePresets.SelectDevice, GUILayout.Width(250));
		GUILayout.Label("...or set each property individually below:");
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
				
		switch (selectedPreset)
		{
			case DevicePresets.SelectDevice:
				// No selection was made.
				break;
				
			case DevicePresets.iPhone4:
				dotsPerInch = "320";
				targetFrameRate = "20";
				isCrappyDevice = true;
				suggestedGameRes = "960 x 640";
				break;
				
			case DevicePresets.iPhone5:
				dotsPerInch = "320";
				targetFrameRate = "30";
				isCrappyDevice = false;
				suggestedGameRes = "1136 x 640";
				break;

			case DevicePresets.iPadMini:
				dotsPerInch = "160";
				targetFrameRate = "20";
				isCrappyDevice = true;
				suggestedGameRes = "1024 x 768";
				break;

			case DevicePresets.iPad4:
				dotsPerInch = "320";
				targetFrameRate = "30";
				isCrappyDevice = false;
				suggestedGameRes = "2048 x 1536";
				break;

			case DevicePresets.FourByThreeSmallDevice:
				dotsPerInch = "320";
				targetFrameRate = "30";
				isCrappyDevice = false;
				suggestedGameRes = "1024 x 768";
				break;

			case DevicePresets.EightByFiveTablet:
				dotsPerInch = "160";
				targetFrameRate = "30";
				isCrappyDevice = false;
				suggestedGameRes = "1280 x 800";
				break;

			case DevicePresets.EightByFiveTabletHD:
				dotsPerInch = "320";
				targetFrameRate = "30";
				isCrappyDevice = false;
				suggestedGameRes = "2560 x 1600";
				break;
		}	

		GUILayout.BeginHorizontal();
		isCrappyDevice = GUILayout.Toggle(isCrappyDevice, "Crappy Device");
		
		GUILayout.FlexibleSpace();
		int selectedIndex = GUILayout.SelectionGrid(System.Array.IndexOf(dotsPerInchOptions, dotsPerInch), dotsPerInchOptions, dotsPerInchOptions.Length);
		dotsPerInch = dotsPerInchOptions[selectedIndex];
		GUILayout.Label("Dots Per Inch");
		
		GUILayout.FlexibleSpace();
		
		selectedIndex = GUILayout.SelectionGrid(System.Array.IndexOf(targetFrameRateOptions, targetFrameRate), targetFrameRateOptions, targetFrameRateOptions.Length);
		targetFrameRate = targetFrameRateOptions[selectedIndex];
		GUILayout.Label("Target FPS");

		GUILayout.EndHorizontal();
		
		if (suggestedGameRes != "")
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label("Suggested Game Resolution: " + suggestedGameRes);
			GUILayout.EndHorizontal();
		}
		
		streamNullTextures = GUILayout.Toggle(streamNullTextures, "Make all streamed in textures null (for testing null image logic/treatments).");
		streamTexturesDelay = floatInputField("Streamed textures delay, to simulate slow downloading", streamTexturesDelay, 0.5f);
		if (GUILayout.Button("Clear Cache", GUILayout.Width(200)))
		{
			Common.clearTemporaryCache();
		}
		
#if UNITY_ANDROID
	#if ZYNGA_KINDLE
		bool isKindle = true;
		string androidTarget = "ZYNGA_KINDLE is set, ZYNGA_GOOGLE is clear (reopen dialog to verify after toggling)";
	#else
		bool isKindle = false;
		string androidTarget = "ZYNGA_KINDLE is clear, ZYNGA_GOOGLE is set (reopen dialog to verify after toggling)";
	#endif
		
		GUILayout.BeginHorizontal();
		
		isKindle = GUILayout.Toggle(isKindle, androidTarget);
				
	#if ZYNGA_KINDLE
		if (!isKindle)
		{
			Build.menuBuildKindleToggle();
		}
	#else
		if (isKindle)
		{
			Build.menuBuildKindleToggle();
		}
	#endif
		
		GUILayout.EndHorizontal();
#endif

		GUILayout.BeginHorizontal();
		showStartupMOTDs = GUILayout.Toggle(showStartupMOTDs, "Show startup MOTD's as usual. Uncheck to suppress in editor only.");
		markMOTDsSeen = GUILayout.Toggle(markMOTDsSeen, "Mark MOTDs Seen. Uncheck to always see MOTDs");
		GUILayout.EndHorizontal();
		GUILayout.Space(20);
		
		GUILayout.Label("URL or '?' query string (eg: www.game.com?x=foo&y=bar)");
		GUILayout.BeginHorizontal();
		{
			localURL = EditorGUILayout.TextField("", localURL, GUILayout.Width(400));
			
			if (GUILayout.Button("X", GUILayout.Width(50)))
			{
				localURL = "";
				
				// It cleared the local URL parameters,
				// but you have to manually unfocus the text field to clear your input text, too.
				GUIUtility.keyboardControl = 0;
			}
		}
		GUILayout.EndHorizontal();
		
		GUILayout.Space(4);
		
		GUILayout.Label("Simulated installed App ID's - comma separated:");
		simulatedInstalledAppIDs = EditorGUILayout.TextField("", simulatedInstalledAppIDs, GUILayout.Width(400));

		GUILayout.Space(4);
				
		GUILayout.Label("Simulated User ZID");
		simulatedZID = EditorGUILayout.TextField("", simulatedZID, GUILayout.Width(200));

		GUILayout.Space(4);

		dialogPopupDefault = GUILayout.Toggle(dialogPopupDefault, "Suppress all dialog popups during gameplay");

		GUILayout.Space(4);
		
		// Fill the lobby with this icon (make sure it loads the 1x2 icon, and make sure they look right).
		// Fill the inbox with this icon (so you can make sure it looks right).
		GUILayout.Label("Lobby/Inbox Icon Preview (eg Group:'gen' and Game:'gen21')");
		lobbyInboxIconGroup = EditorGUILayout.TextField("Group", lobbyInboxIconGroup);
		lobbyInboxIconGame = EditorGUILayout.TextField("Game", lobbyInboxIconGame);
		
		if (GUILayout.Button("X", GUILayout.Width(50)))
		{
			lobbyInboxIconGroup = "";
			lobbyInboxIconGame = "";
			
			// It cleared the icon preview,
			// but you have to manually unfocus the text field to clear your input text, too.
			GUIUtility.keyboardControl = 0;
		}

		GUILayout.BeginHorizontal();
		launchDirectlyGameKey = drawTextfield("Launch Directly Gamekey:", launchDirectlyGameKey, "gamekey used to directly launch into");
		launchDirectlyIntoGame = drawToggle(launchDirectlyIntoGame, "Launch Directly into Game", "Skips lobby load, and will load into game at startup");
		launchDirectlyIntoGameAlways = drawToggle(launchDirectlyIntoGameAlways, "Launch Directly Always, Never can load lobby", "helpful when verifying a game's startup, will reload game even when tryin to go to lobby");
		GUILayout.EndHorizontal();
		
		GUILayout.BeginHorizontal();
		EditorGUIUtility.labelWidth = 275.0f;
		normalizedPortraitModeSafeAreaHeight = EditorGUILayout.Slider("Normalized Editor Portrait Mode Safe Area Offset", normalizedPortraitModeSafeAreaHeight, 0.0f, 1.0f);
		EditorGUIUtility.labelWidth = 0.0f;
		GUILayout.EndHorizontal();
		
		GUILayout.BeginHorizontal();
		isForcingWebglLandscapeInPortraitModeGames = GUILayout.Toggle(isForcingWebglLandscapeInPortraitModeGames, "Force Always In Landscape (Like WebGL)");
		isForcingWebglDotCom = GUILayout.Toggle(isForcingWebglDotCom, "Force WebGL to use DotCom client ID");
		GUILayout.EndHorizontal();

		GUILayout.Space(20);

		EditorGUILayout.LabelField(statusString);

		GUILayout.BeginHorizontal();

		GUILayout.FlexibleSpace();

		if (GUILayout.Button("Clear Prefs", GUILayout.Height(30), GUILayout.Width(100)))
		{
			SlotsPlayer.getPreferences().DeleteAll();
			PlayerPrefsCache.DeleteAll();
			PlayerPrefsCache.Save();
		}
		if (GUILayout.Button("Save Settings", GUILayout.Height(30), GUILayout.Width(100)))
		{
			this.saveSettings();
		}
		GUILayout.EndHorizontal();
		
	}

	private void selectConfigFileByIndex(int index)
	{
		index = Mathf.Clamp(index, 0, configFiles.Length);
		selectedConfigFileIndex = index;
		selectedConfigFile = configFiles[selectedConfigFileIndex];
	}

	private void saveSettings()
	{
		Debug.Log("Saving Settings...");

		SlotsPlayer.getPreferences().SetInt(DebugPrefs.OPTIONAL_LOGS, showOptionalLogs ? 1 : 0);
		SlotsPlayer.getPreferences().SetInt(DebugPrefs.SERVER_DEBUGGING, serverDebugging ? 1 : 0);
		SlotsPlayer.getPreferences().SetInt(DebugPrefs.MIN_LOG_LEVEL, (int)minLogLevel);
		saveSelectedConfigFile();
		SlotsPlayer.getPreferences().SetInt(DebugPrefs.USE_ASSET_BUNDLES, useAssetBundles ? 1 : 0);
		SlotsPlayer.getPreferences().SetInt(DebugPrefs.USE_LOCAL_BUNDLES, useLocalBundles ? 1 : 0);
		SlotsPlayer.getPreferences().SetInt(DebugPrefs.USE_SD_BUNDLES, useSDBundles ? 1 : 0);
		SlotsPlayer.getPreferences().SetString(DebugPrefs.LOCALE, playerLocale);
		SlotsPlayer.getPreferences().SetString(DebugPrefs.CURRENT_LANGUAGE, playerCurrentLanguage.ToString());
		SlotsPlayer.getPreferences().SetInt(DebugPrefs.IS_CRAPPY_DEVICE, isCrappyDevice ? 1 : 0);
		SlotsPlayer.getPreferences().SetString(DebugPrefs.DOTS_PER_INCH, dotsPerInch);
		SlotsPlayer.getPreferences().SetString(DebugPrefs.TARGET_FRAME_RATE, targetFrameRate);
		SlotsPlayer.getPreferences().SetString(DebugPrefs.LOCAL_URL, localURL);
		SlotsPlayer.getPreferences().SetString(DebugPrefs.LOBBY_INBOX_ICON_GROUP, lobbyInboxIconGroup);
		SlotsPlayer.getPreferences().SetString(DebugPrefs.LOBBY_INBOX_ICON_GAME, lobbyInboxIconGame);
		SlotsPlayer.getPreferences().SetInt(DebugPrefs.STREAM_NULL_TEXTURES, streamNullTextures ? 1 : 0);
		SlotsPlayer.getPreferences().SetFloat(DebugPrefs.STREAM_TEXTURES_DELAY, streamTexturesDelay);
		SlotsPlayer.getPreferences().SetString(DebugPrefs.SIMULATED_INSTALLED_APP_IDS, simulatedInstalledAppIDs);
		SlotsPlayer.getPreferences().SetString(Prefs.SIMULATED_USER_ID, simulatedZID);
		SlotsPlayer.getPreferences().SetInt(DebugPrefs.SHOW_STARTUP_MOTDS, showStartupMOTDs ? 1 : 0);
		SlotsPlayer.getPreferences().SetInt(DebugPrefs.MARK_MOTDS_SEEN, markMOTDsSeen ? 1 : 0);
		// Set preprocessor flags for TRAMP
		SlotsPlayer.getPreferences().SetInt(DebugPrefs.ZYNGA_TRAMP, automation ? 1 : 0);
		SlotsPlayer.getPreferences().SetInt(DebugPrefs.SUPPRESS_ALL_DIALOGS, dialogPopupDefault ? 1 : 0);
		SlotsPlayer.getPreferences().SetInt(DebugPrefs.LAUNCH_DIRECTLY_INTO_GAME, launchDirectlyIntoGame ? 1 : 0);
		SlotsPlayer.getPreferences().SetString(DebugPrefs.LAUNCH_DIRECTLY_GAME_KEY, launchDirectlyGameKey);
		SlotsPlayer.getPreferences().SetInt(DebugPrefs.LAUNCH_DIRECTLY_INTO_GAME_ALWAYS, launchDirectlyIntoGameAlways ? 1 : 0);
		SlotsPlayer.getPreferences().SetFloat(DebugPrefs.NORMALIZED_PORTRAIT_MODE_SAFE_AREA_OFFSET, normalizedPortraitModeSafeAreaHeight);
		SlotsPlayer.getPreferences().SetBool(DebugPrefs.FORCE_WEBGL_LANDSCAPE_IN_PORTRAIT_MODE_GAMES, isForcingWebglLandscapeInPortraitModeGames);
		SlotsPlayer.getPreferences().SetBool(DebugPrefs.FORCE_WEBGL_DOTCOM_MODE, isForcingWebglDotCom);
		SlotsPlayer.getPreferences().Save();
		
		if (automation)
		{
			CommonEditor.AddScriptingDefineSymbolForGroup("ZYNGA_TRAMP", BuildTargetGroup.iOS);
			CommonEditor.AddScriptingDefineSymbolForGroup("ZYNGA_TRAMP", BuildTargetGroup.Android);
			CommonEditor.AddScriptingDefineSymbolForGroup("ZYNGA_TRAMP", BuildTargetGroup.WebGL);
		}
		else
		{
			CommonEditor.RemoveScriptingDefineSymbolForGroup("ZYNGA_TRAMP", BuildTargetGroup.iOS);
			CommonEditor.RemoveScriptingDefineSymbolForGroup("ZYNGA_TRAMP", BuildTargetGroup.Android);
			CommonEditor.RemoveScriptingDefineSymbolForGroup("ZYNGA_TRAMP", BuildTargetGroup.WebGL);
		}

		// Set preprocessor flags for isProduction.
		string productionDefine = "ZYNGA_PRODUCTION";
		if (isProduction)
		{
			CommonEditor.AddScriptingDefineSymbolForGroup(productionDefine, BuildTargetGroup.iOS);
			CommonEditor.AddScriptingDefineSymbolForGroup(productionDefine, BuildTargetGroup.Android);
			CommonEditor.AddScriptingDefineSymbolForGroup(productionDefine, BuildTargetGroup.WebGL);
		}
		else
		{
			if (CommonEditor.IsScriptingDefineSymbolDefinedForGroup(productionDefine, BuildTargetGroup.iOS))
			{
				CommonEditor.RemoveScriptingDefineSymbolForGroup(productionDefine, BuildTargetGroup.iOS);
			}
			if (CommonEditor.IsScriptingDefineSymbolDefinedForGroup(productionDefine, BuildTargetGroup.Android))
			{
				CommonEditor.RemoveScriptingDefineSymbolForGroup(productionDefine, BuildTargetGroup.Android);
			}
			if (CommonEditor.IsScriptingDefineSymbolDefinedForGroup(productionDefine, BuildTargetGroup.WebGL))
			{
				CommonEditor.RemoveScriptingDefineSymbolForGroup(productionDefine, BuildTargetGroup.WebGL);
			}
		}

		// Force recompile, sometimes Unity doesn't otherwise.
		AssetDatabase.Refresh();


		Debug.Log("Editor Settings Saved");
		this.Close();
	}

	private void logStatus(string status)
	{
		this.statusString = status;
		Debug.Log(status);
	}

	private float floatInputField(string label, float val, float increment, float min = 0.0f, float max = float.MaxValue)
	{		
		int width = 30;
		
		GUILayout.BeginHorizontal();

		GUILayout.Label(label + ":");
		if (GUILayout.Button("-", GUILayout.Width(width)))
		{
			val = Mathf.Clamp(val - increment, min, max);
		}
		if (GUILayout.Button("+", GUILayout.Width(width)))
		{
			val = Mathf.Clamp(val + increment, min, max);
		}
		
		string stringValue = GUILayout.TextField(val.ToString(), GUILayout.Width(30)).Trim();
		
		GUILayout.EndHorizontal();

		try
		{
			val = float.Parse(stringValue);
		}
		catch {}
		
		return val;
	}

	private bool drawButton(string text, string toolTip = "")
	{
		guiContent.text = text;
		guiContent.tooltip = toolTip;

		return GUILayout.Button(guiContent);
	}

	private bool drawToggle(bool value, string text, string toolTip = "")
	{
		guiContent.text = text;
		guiContent.tooltip = toolTip;
	
		return GUILayout.Toggle(value, guiContent);
	}
	
	private string drawTextfield(string label, string text, string tooltip = "")
	{
		guiContent.text = label;
		guiContent.tooltip = tooltip;
		return EditorGUILayout.TextField(guiContent, text);
	}
}
