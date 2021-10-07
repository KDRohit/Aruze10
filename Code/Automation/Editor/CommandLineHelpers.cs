using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Zap.Automation;

public class CommandLineHelpers
{
	const string startupScene = "Assets/Data/HIR/Scenes/Startup.unity";

#region ZAP
#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	public static void runZap()
	{
		Debug.LogFormat("ZAPLOG -- Starting ZAP!");
		// First make sure that we have the results/plans folders setup properly so that we save stuff out.
		setZapDefaultLocations();
		Dictionary<string, string> commandLineArguments = CommandLineReader.GetCustomArguments(true);
		JSON testJson = null;
		foreach (KeyValuePair<string, string> arg in commandLineArguments)
		{
			switch(arg.Key)
			{
				case "testPlan":
					// Set the test plan that we are going to run.
					string testPlanName = arg.Value;
					if (testPlanName == "default")
					{
						// TODO -- If this is passed then we want to generate a test plan here.
					}
					else if (!testPlanName.EndsWith(".json"))
					{
						// Someone might forget .json so lets append that if they didnt put it in here.
						testPlanName += ".json";
					}
					string basePath = SlotsPlayer.getPreferences().GetString(Zap.Automation.ZAPPrefs.ZAP_SAVE_LOCATION, "");
					string testPlanPath = System.IO.Path.Combine(basePath, testPlanName);
					//Load the file into a string.
					System.IO.StreamReader stream = new System.IO.StreamReader(testPlanPath);
					string testPlanText = stream.ReadToEnd();
					// Try to parse this as JSON to make sure its valid.
					try
					{
						testJson = new JSON(testPlanText);
						if (testJson == null)
						{
							Debug.LogFormat("ZAPLOG -- Failed to parse the text as JSON, ZAP wont run.: {0}", testPlanText);
						}
						else
						{
							Debug.LogFormat("ZAPLOG -- Loaded test plan {0} to run in ZAP", testPlanPath);
						}
					}
					catch (System.Exception e)
					{
						// If we can't run open this as a JSON object, then we wont be able to run the test plan.
						// Throw an error and then bail on this process.
						Debug.LogErrorFormat("ZAPLOG -- CommandLineHelpers.cs -- runZap() -- failed to convert the test plan to JSON, not attempting to run it.");
						exitUnity(1);
						return;
					}
					break;
				case "resetEditorPrefs":
					// If we want to reset the player prefs (will grab a new ZID in editor) then do that here.
					if (arg.Value == "true" || arg.Value == "True")
					{
						Debug.LogFormat("ZAPLOG -- Resetting Player Prefs from CLI args.");
						SlotsPlayer.getPreferences().DeleteAll();
					}
					break;
				case "summaryOutput":
					Zap.Automation.ZyngaAutomatedPlayer.instance.zapSummarySaveLocation = arg.Value;
					break;
			}
		}

		if (testJson == null)
		{
			exitUnity(1);
			return; // We already logged above why we wont be running.
		}
		// If it is a valid test, set up  the PlayerPrefs for playing when we start.
		SlotsPlayer.getPreferences().SetString(Zap.Automation.ZAPPrefs.TEST_PLAN_JSON, testJson.ToString());
		SlotsPlayer.getPreferences().SetInt(Zap.Automation.ZAPPrefs.SHOULD_AUTOMATE_ON_PLAY, 1);
		SlotsPlayer.getPreferences().SetInt(Zap.Automation.ZAPPrefs.SHOULD_RESUME, 0);
		SlotsPlayer.getPreferences().SetInt(DebugPrefs.STARTED_FROM_COMMAND_LINE, 1);
		setZapDefaultLocations(); // Set again in case we reset.
		
		// Now that we have done the preparsing, lets get this party started and play!
		UnityEditor.EditorApplication.isPlaying = false;
		SlotsPlayer.getPreferences().Save();
		UnityEditor.SceneManagement.EditorSceneManager.OpenScene(startupScene);
		UnityEditor.EditorApplication.isPlaying = true;

		// Mute the sound of the game:
		EditorUtility.audioMasterMute = true;
		Debug.LogFormat("ZAPLOG -- end of runZAP function, zap should start itself shortly.");
	}

	public static void setZapDefaultLocations()
	{
		// @todo: Currently storing these in a tools folder for our project. If we want this to work on device we will need
		// to pick a location that we have access to at build time.
		// Force these to the default values so that we always know where to look no matter what machine we are running on.
		SlotsPlayer.getPreferences().SetString(Zap.Automation.ZAPPrefs.ZAP_RESULTS_LOCATION, ZAPFileHandler.DEFAULT_ZAP_RESULTS_LOCATION);
		SlotsPlayer.getPreferences().SetString(Zap.Automation.ZAPPrefs.ZAP_SAVE_LOCATION, ZAPFileHandler.DEFAULT_ZAP_SAVE_LOCATION);			
	}

	public static void exitUnity(int exitCode = 0)
	{
		EditorApplication.Exit(exitCode);
	}
#endif
#endregion
	private static readonly string[] skuDefines = new string[] 
	{
		"ZYNGA_SKU_HIR"
	};

	public static void playHIR()
	{
		// Get the compile defines straight
		ChangeSKU(SkuId.HIR);

		// These should only be 1 for TRAMP
		SlotsPlayer.getPreferences().SetInt(DebugPrefs.ZYNGA_TRAMP, 0);
		SlotsPlayer.getPreferences().SetInt(DebugPrefs.STARTED_FROM_COMMAND_LINE, 0);

		UnityEditor.EditorApplication.isPlaying = false;
		SlotsPlayer.getPreferences().Save();

		UnityEditor.SceneManagement.EditorSceneManager.OpenScene( startupScene );
	}

	public static void trampHIR()
	{
		// Get the compile defines straight
		ChangeSKU(SkuId.HIR, true);

		Dictionary<string, string> commandLineArguments = CommandLineReader.GetCustomArguments(true);
		if (commandLineArguments.ContainsKey("fbLogin"))
		{
			string [] argParts = commandLineArguments["fbLogin"].Split(',');
			string fbUserID = argParts[0];
			string fbAccessToken = argParts[1];
			SlotsPlayer.getPreferences().SetInt(SocialManager.kLoginPreference, (int)SocialManager.SocialLoginPreference.Facebook);
			SlotsPlayer.getPreferences().SetInt(SocialManager.kUpgradeZid, 0);
			SlotsPlayer.getPreferences().SetInt(SocialManager.kFacebookLoginSaved, 1);
			SlotsPlayer.getPreferences().SetInt("FbEnabled", 1);
			SlotsPlayer.getPreferences().SetString("EDITOR_FB_TOKEN_USERID", fbUserID);
			SlotsPlayer.getPreferences().SetString("EDITOR_FB_TOKEN_TOKENSTRING", fbAccessToken);
			SlotsPlayer.getPreferences().SetInt("EDITOR_FB_TOKEN_PERMISSION_COUNT", 3);
			SlotsPlayer.getPreferences().SetString("EDITOR_FB_TOKEN_PERMISSION_0", "user_friends");
			SlotsPlayer.getPreferences().SetString("EDITOR_FB_TOKEN_PERMISSION_1", "email");
			SlotsPlayer.getPreferences().SetString("EDITOR_FB_TOKEN_PERMISSION_2", "public_profile");
		}

		SlotsPlayer.getPreferences().SetInt(DebugPrefs.ZYNGA_TRAMP, 1);
		SlotsPlayer.getPreferences().SetInt(DebugPrefs.STARTED_FROM_COMMAND_LINE, 1);
		UnityEditor.EditorApplication.isPlaying = false;
		SlotsPlayer.getPreferences().Save();

		UnityEditor.SceneManagement.EditorSceneManager.OpenScene( startupScene );
		UnityEditor.EditorApplication.isPlaying = true;

		// Mute the sound of the game:
		EditorUtility.audioMasterMute = true;
	}

	public static void trampImportAndQuit()
	{
		trampHIR();
		UnityEditor.EditorApplication.Exit(1);
	}

	public static void ChangeSKU(SkuId sku, bool isTRAMP = false)
	{
		// Remove all the defines first
		foreach(string skuDefineString in skuDefines)
		{
			CommonEditor.RemoveScriptingDefineSymbolForGroup(skuDefineString, BuildTargetGroup.Android);
			CommonEditor.RemoveScriptingDefineSymbolForGroup(skuDefineString, BuildTargetGroup.iOS);
			CommonEditor.RemoveScriptingDefineSymbolForGroup(skuDefineString, BuildTargetGroup.WebGL);
		}

		// Determine if TRAMP is on (Default is off)
		if (isTRAMP)
		{
			CommonEditor.AddScriptingDefineSymbolForGroup("ZYNGA_TRAMP", BuildTargetGroup.Android);
			CommonEditor.AddScriptingDefineSymbolForGroup("ZYNGA_TRAMP", BuildTargetGroup.iOS);
			CommonEditor.AddScriptingDefineSymbolForGroup("ZYNGA_TRAMP", BuildTargetGroup.WebGL);
		}
		else
		{
			CommonEditor.RemoveScriptingDefineSymbolForGroup("ZYNGA_TRAMP", BuildTargetGroup.Android);
			CommonEditor.RemoveScriptingDefineSymbolForGroup("ZYNGA_TRAMP", BuildTargetGroup.iOS);
			CommonEditor.RemoveScriptingDefineSymbolForGroup("ZYNGA_TRAMP", BuildTargetGroup.WebGL);
		}

		// Add back the one we are switching too
		switch (sku)
		{
			case SkuId.HIR:
				CommonEditor.AddScriptingDefineSymbolForGroup("ZYNGA_SKU_HIR", BuildTargetGroup.Android);
				CommonEditor.AddScriptingDefineSymbolForGroup("ZYNGA_SKU_HIR", BuildTargetGroup.iOS);
				CommonEditor.AddScriptingDefineSymbolForGroup("ZYNGA_SKU_HIR", BuildTargetGroup.WebGL);
				break;
			case SkuId.UNKNOWN:
			default:
				Debug.LogErrorFormat("CommandLineHelpers does not support SKU {0}", sku);
				break;
		}
	}

#if ZYNGA_TRAMP
	[MenuItem ("TRAMP/Disable")]
	static void trampDisable()
	{
		CommonEditor.RemoveScriptingDefineSymbolForGroup("ZYNGA_TRAMP", BuildTargetGroup.Android);
		CommonEditor.RemoveScriptingDefineSymbolForGroup("ZYNGA_TRAMP", BuildTargetGroup.iOS);
		CommonEditor.RemoveScriptingDefineSymbolForGroup("ZYNGA_TRAMP", BuildTargetGroup.WebGL);
		SlotsPlayer.getPreferences().SetInt(DebugPrefs.ZYNGA_TRAMP, 0);
		SlotsPlayer.getPreferences().SetInt(DebugPrefs.STARTED_FROM_COMMAND_LINE, 0);
	}
#else
	[MenuItem ("Zynga/TRAMP/Enable")]
	static void trampEnable()
	{
		CommonEditor.AddScriptingDefineSymbolForGroup("ZYNGA_TRAMP", BuildTargetGroup.Android);
		CommonEditor.AddScriptingDefineSymbolForGroup("ZYNGA_TRAMP", BuildTargetGroup.iOS);
		CommonEditor.AddScriptingDefineSymbolForGroup("ZYNGA_TRAMP", BuildTargetGroup.WebGL);
		SlotsPlayer.getPreferences().SetInt(DebugPrefs.ZYNGA_TRAMP, 1);
		SlotsPlayer.getPreferences().SetInt(DebugPrefs.STARTED_FROM_COMMAND_LINE, 0);
	}
#endif
}
