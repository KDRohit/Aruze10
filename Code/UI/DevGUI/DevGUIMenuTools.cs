using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using CustomLog;
using UnityEngine;
using Zynga.Core.Util;

/*
A dev panel.
*/

public class DevGUIMenuTools : DevGUIMenu
{
	private static string dumpCache = "";
	
	public static bool customLogLogs = false;
	public static bool customLogWarnings = false;
	public static bool customLogErrors = false;
	public static bool customLogEditor = false;

	public static bool disableFeatures = PlayerPrefs.GetInt(DebugPrefs.SUPPRESS_ALL_DIALOGS) == 1;

	public override void drawGuts()
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label("Device Info", GUILayout.Width(isHiRes ? 260 : 130));
		GUILayout.Label("clientID: " + Zynga.Core.Platform.DeviceInfo.ClientId.ToString());
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Share screenshot"))
		{

			string screenshotPath = System.IO.Path.Combine(Application.persistentDataPath, "screenshot.png");
			if (System.IO.File.Exists(screenshotPath))
			{
				System.IO.File.Delete(screenshotPath);
			}

			ScreenCapture.CaptureScreenshot("screenshot.png");
			RoutineRunner.instance.StartCoroutine(delayedScreenshotShare(screenshotPath, "This is a test!"));
		}
		
		GUILayout.EndHorizontal();
		
		// This check is just in case any normal mechanic sets the value outside of this range
		if (Time.timeScale >= 0.1f && Time.timeScale <= 10f)
		{
			// Time.timeScale = drawSlider("Animation Speed", Time.timeScale, 0.1f, 10f); with an extra button to reset it.
			GUILayout.BeginHorizontal();
			GUILayout.Label("Animation Speed",GUILayout.Width(isHiRes ? 260 : 130));
			if (GUILayout.Button("1", GUILayout.Width(isHiRes ? 80 : 40)))
			{
				Time.timeScale = 1;
			}
			if (GUILayout.Button("10", GUILayout.Width(isHiRes ? 80 : 40)))
			{
				Time.timeScale = 10;
			}
			GUILayout.Label(string.Format("{0:0.000}", Time.timeScale), GUILayout.Width(isHiRes ? 100 : 50));
			Time.timeScale = GUILayout.HorizontalSlider(Time.timeScale, 0.1f, 10f, GUILayout.Width(isHiRes ? 300 : 150));

			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			Common.dontWait = GUILayout.Toggle(Common.dontWait , "Don't Wait");
			
			GUILayout.FlexibleSpace();

			Input.drawColliderVisualizer = GUILayout.Toggle(Input.drawColliderVisualizer, "Show Colliders Clicked", GUILayout.Width(isHiRes ? 300 : 150));
			ColliderVisualizer.instance.enableContinuousVisualColliders = GUILayout.Toggle(ColliderVisualizer.instance.enableContinuousVisualColliders, "Visualize All Colliders", GUILayout.Width(isHiRes ? 300 : 150));
			ColliderVisualizer.instance.active = ColliderVisualizer.instance.enableContinuousVisualColliders || Input.drawColliderVisualizer;
			if (!ColliderVisualizer.instance.active)
			{
				ColliderVisualizer.instance.disableVisualizer();
			}
			GUILayout.EndHorizontal();
		}

		GUILayout.Space(10);

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Reset Player"))
		{
			PlayerAction.resetPlayer();
			// Also reset some player prefs so it's like a fresh install.
			SlotsPlayer.resetLocalSettings();

			DevGUI.isActive = false;
		}

		if (GUILayout.Button("Reset Requests"))
		{
			PlayerAction.resetRequests();
			DevGUI.isActive = false;
			GenericDialog.showDialog(
				Dict.create(
					D.TITLE, "SENT",
					D.MESSAGE, "The action has been sent. You should restart the game for the client to receive the new limits.",
					D.REASON, "dev-gui-reset-requests-sent"
				),
				SchedulerPriority.PriorityType.IMMEDIATE
			);
		}

		if (GUILayout.Button("Reset Game"))
		{
			Glb.resetGame("Dev Panel button");
			DevGUI.isActive = false;
		}

		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Reset Charm Tooltip"))
		{
			PlayerPrefsCache.SetInt(Prefs.SHOWN_CHARMS_TOOLTIP, 0);
			Glb.resetGame("Dev panel, Reset Charm Tooltip");
			DevGUI.isActive = false;
		}

		if (GUILayout.Button("Reset Charm with Buy MOTD"))
		{
			CustomPlayerData.setValue(CustomPlayerData.CHARM_WITH_BUY_VIEWED_MOTD_VERSION, 0);
			Glb.resetGame("Dev panel, Reset Charm with Buy MOTD");
			DevGUI.isActive = false;
		}

		if (GUILayout.Button("Reset Quests MOTD"))
		{
			PlayerPrefsCache.SetInt(Prefs.LAST_SEEN_MOTD_QUEST_COUNTER, 0);
			DevGUI.isActive = false;
		}

		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Reset Migration Popup"))
		{
			PlayerAction.resetMigrationStatus();
			PlayerAction.changeTimeStamp();
			DevGUI.isActive = false;
			GenericDialog.showDialog(
				Dict.create(
					D.TITLE, "SENT",
					D.MESSAGE, "The action has been sent. You should restart the game for the client to get your 30K bling.",
					D.REASON, "dev-gui-reset-migration-sent"
				),
				SchedulerPriority.PriorityType.IMMEDIATE
			);
		}

		if (GUILayout.Button("Reset WOZ Slots XPromo"))
		{
			PlayerPrefsCache.SetInt(Prefs.HAS_WOZ_SLOTS_INSTALLED_BEFORE_CHECK, -1);
			DevGUI.isActive = false;
			GenericDialog.showDialog(
				Dict.create(
					D.TITLE, "RESET",
					D.MESSAGE, "The WOZ Slots XPromo has been reset for the next time you launch the game.",
					D.REASON, "dev-gui-reset-woz-slots-xpromo"
				),
				SchedulerPriority.PriorityType.IMMEDIATE
			);
		}
		
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Unity Types Dump"))
		{
			dumpCache = dumpActiveTypes();
		}

		if (GUILayout.Button("Resolution Refresh"))
		{
			RoutineRunner.instance.StartCoroutine(ResolutionChangeHandler.callResolutionChangeHandlers());
		}
		
#if UNITY_EDITOR
		if (GUILayout.Button("Sim Pause"))
		{
			PauseHandler.simPause(true);
		}

		if (GUILayout.Button("Sim Unpause"))
		{
			PauseHandler.simPause(false);
		}
#endif

		GUILayout.EndHorizontal();
		
		if (dumpCache != "")
		{
			GUILayout.TextArea(dumpCache);
		}
		
		GUILayout.Space(10);
		
		GUILayout.BeginHorizontal();

		if (GUILayout.Button("FPS"))
		{
			DebuggingFPSMemoryComponent.Singleton.gameObject.SetActive(!DebuggingFPSMemoryComponent.Singleton.gameObject.activeInHierarchy);
			LoadingHIRV3.hirV3.toggleCamera(DebuggingFPSMemoryComponent.Singleton.gameObject.activeSelf, true);
		}

		if (GUILayout.Button("Log"))
		{
			Log.isActive = true;
			DevGUI.isActive = false;
		}

		int verboseLoggingState = SlotsPlayer.getPreferences().GetInt(Prefs.VERBOSE_LOGGING, 0);
		bool isVerboseLoggingEnabled = verboseLoggingState > 0;
	    string oppositeVerboseLoggingState = isVerboseLoggingEnabled ? "Off" : "On";
	    if (GUILayout.Button($"Turn Verbose Logs {oppositeVerboseLoggingState}"))
	    {
		    SlotsPlayer.getPreferences().SetInt(Prefs.VERBOSE_LOGGING, isVerboseLoggingEnabled ? 0 : 1);
		    SlotsPlayer.getPreferences().Save();
		    
		    if (isVerboseLoggingEnabled)
		    {
			    Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
			    Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
		    }
		    else
		    {
			    Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.ScriptOnly);
			    Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.ScriptOnly);
		    }
	    }

	    if (GUILayout.Button("Show All Carousel Slides"))
		{
			CarouselData.activateAll();
			DevGUI.isActive = false;
		}

		GUILayout.EndHorizontal();
		GUILayout.Space(10);
		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Log Logs: " + onOff[customLogLogs ? 1 : 0]))
		{
			customLogLogs = !customLogLogs;
		}
		if (GUILayout.Button("Log Warnings: " + onOff[customLogWarnings ? 1 : 0]))
		{
			customLogWarnings = !customLogWarnings;
		}
		if (GUILayout.Button("Log Errors: " + onOff[customLogErrors ? 1 : 0]))
		{
			customLogErrors = !customLogErrors;
		}
		if (GUILayout.Button("Log EditorLogs: " + onOff[customLogEditor ? 1 : 0]))
		{
			customLogEditor = !customLogEditor;
		}

		if (GUILayout.Button("Economy Log " + (Data.debugEconomy ? "(On)" : "(Off)")))
		{
			Data.debugEconomy = !Data.debugEconomy;
		}		

		GUILayout.EndHorizontal();

		GUILayout.Space(10);

		GUILayout.BeginHorizontal();
		
		if (GUILayout.Button("Throw Exception"))
		{
			throw new System.Exception("Testing a generic exception throwing, nothing to see here.");
		}
		if (GUILayout.Button("Log Error"))
		{
			Debug.LogError("Testing a generic error logging, nothing to see here.");
		}
		if (GUILayout.Button("Trigger Exception"))
		{
			GameObject go = (Time.realtimeSinceStartup < 0f) ? new GameObject() : null;
			go.SetActive(false);
			GameObject.Destroy(go);
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Begin Userflow"))
		{
			Debug.LogWarning("Test Userflow: Begin.");
			Userflows.flowStart("testing");
		}
		if (GUILayout.Button("End Userflow"))
		{
			Debug.LogWarning("Test Userflow: End.");
			Userflows.flowEnd("testing");
		}
		if (GUILayout.Button("Fail Userflow"))
		{
			Debug.LogWarning("Test Userflow: Fail.");
			Userflows.flowEnd("testing", false, "failed");
		}
		GUILayout.EndHorizontal();

		GUILayout.Space(10);

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Post to FB Wall"))
		{
			//TODO: Girish
			/*Zynga.Zdk.BFCallback fbWallPostCallback = (data, errorMessage) =>
			{
				Debug.Log("*** fbWallPost callback: " + data);
			};

			SocialManager.Instance.PublishFeedOnSN(
				"Test FeedName " + Time.realtimeSinceStartup,
				"Test Caption" + Time.realtimeSinceStartup,
				"Test Description" + Time.realtimeSinceStartup,
				new Dictionary<string, string>(),
				new Dictionary<string, string>(),
				fbWallPostCallback
			);*/
		}
		
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Delete All Saved Data"))
		{
			Debug.LogError("DevGui -- Attempting to delete all PlayerPrefsCache.");
			PlayerPrefsCache.DeleteAll();
			PlayerPrefsCache.Save();

			// We must quit now - otherwise in-memory data will be written back to prefs when the app exits normally:
			Common.QuitApp();
		}

		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Crash"))
		{
			forceCrashOnPurpose();
		}

		if (GUILayout.Button("Divide by 0"))
		{
			int tmp = 0;
			tmp = 5/tmp;
		}

		if (GUILayout.Button("GC Login"))
		{
			PlayerPrefsCache.SetInt(DebugPrefs.FORCE_GC_LOGIN_PROMPT, 1);
			PlayerPrefsCache.Save();
			forceCrashOnPurpose();
		}

		GUILayout.EndHorizontal();

		GUILayout.Space(10);

		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Force welcome popup next load"))
		{
			PlayerPrefsCache.SetInt(DebugPrefs.FORCE_INTRO, 1);
			PlayerPrefsCache.Save();
		}

		GUILayout.EndHorizontal();

		GUILayout.Space(10);
		GUILayout.BeginHorizontal();
		
		GUILayout.Label("Splunk logging - Server forced for player: " + SlotsPlayer.instance.forceAllLogging);

		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();

		int serverDebuggingInt = PlayerPrefsCache.GetInt(DebugPrefs.SERVER_DEBUGGING, 0);
		bool isServerDebuggingOn = serverDebuggingInt != 0;
		if (GUILayout.Button("Toggle Server Debugging (" + (isServerDebuggingOn ? "On" : "Off") + ")"))
		{
			if (serverDebuggingInt == 1)
			{
				PlayerPrefsCache.SetInt(DebugPrefs.SERVER_DEBUGGING, 0);
			}
			else
			{
				PlayerPrefsCache.SetInt(DebugPrefs.SERVER_DEBUGGING, 1);
			}
			PlayerPrefsCache.Save();
		}

		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Toggle Error Log (" + (Glb.serverLogErrors ? "On" : "Off") + ")"))
		{
			Glb.serverLogErrors = !Glb.serverLogErrors;
		}

		if (GUILayout.Button("Toggle Warning Log (" + (Glb.serverLogWarnings ? "On" : "Off") + ")"))
		{
			Glb.serverLogWarnings = !Glb.serverLogWarnings;
		}

		if (GUILayout.Button("Toggle Payment Log (" + (Glb.serverLogPayments ? "On" : "Off") + ")"))
		{
			Glb.serverLogPayments = !Glb.serverLogPayments;
		}

		if (GUILayout.Button("Toggle Load Log (" + (Glb.serverLogLoadTime ? "On" : "Off") + ")"))
		{
			Glb.serverLogLoadTime = !Glb.serverLogLoadTime;
		}
			
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		if (GUILayout.Button((disableFeatures ? "Disable" : "Enable") + " All Feature Showing: " + (disableFeatures ? "Hidden" : "Shown")))
		{
			disableFeatures = !disableFeatures;
			if (SpinPanel.instance != null)
			{
				SpinPanel.instance.forceShowFeatureUI(!disableFeatures);
			}
		}
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Deauth FB token"))
		{
			Debug.Log("Girish: deauth fb token clicked");
			SlotsPlayer.getPreferences().SetBool(SocialManager.deAuthFBToken, true);
			SlotsPlayer.getPreferences().Save();
		}
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Reauth FB token"))
		{
			Debug.Log("Girish: reauth fb token clicked");
			SlotsPlayer.getPreferences().SetBool(SocialManager.deAuthFBToken, false);
			SlotsPlayer.getPreferences().Save();
		}
		GUILayout.EndHorizontal();
	}


	private IEnumerator delayedScreenshotShare(string screenshotPath, string text)
	{
		int waitingTime = 0;
		while (!System.IO.File.Exists(screenshotPath))
		{
			// Wait for a second so we dont check every frame.
			yield return new WaitForSeconds(1.0f);
			waitingTime += 1;
			if (waitingTime > 60)
			{
				// This is a dev function so this timeout value doesn't really matter
				// making it a minute but this should finish almost instantly on devices.
				break;
			}
		}
		NativeBindings.ShareContent("Screenshot", text, screenshotPath, "");
	}
	
	private string dumpActiveTypes()
	{
		List<string> typesList = new List<string>();
		
		System.Type componentType = typeof(Component);

#if !(UNITY_WSA_10_0 && NETFX_CORE)
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
		{
			if (assembly.FullName.FastStartsWith("UnityEngine"))
			{
				foreach (System.Type type in assembly.GetTypes())
				{
					if (componentType.IsAssignableFrom(type) ||
						type.AssemblyQualifiedName.Contains("Animat") ||
						type.AssemblyQualifiedName.Contains("Avatar"))
					{
						typesList.Add(type.AssemblyQualifiedName);
					}
				}
			}
		}
#endif //!UNITY_WSA_10_0

		typesList.Sort();
		
		string dump = "Active Unity types dump:";
		foreach (string typeName in typesList)
		{
			dump += "\n+++" + typeName.Split(',')[0] + "---";
		}
		
		return dump;
	}
	
	private void forceCrashOnPurpose()
	{
		List<int> crash = null;
		int i = crash.Count;
		Debug.Log("This message is only to suppress a compiler warning about not using the variable i: " + i);
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}
