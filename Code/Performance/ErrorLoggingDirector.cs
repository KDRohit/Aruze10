using UnityEngine;
using System;

// Common entry point provides some additional Zynga-specific
// log handling too (for Stats, Splunk, and the internal DevGUI).
public class ErrorLoggingDirector : MonoBehaviour
{
	private const int LOG_FLOODING_THRESHOLD = 100;	// Stop transmitting data after too many items in an app session.
	private static int numberOfTransmittedLogs = 0;
	
	void Awake ()
	{
		// Zynga log handling that is in addition to Crittercism
#if !(UNITY_WSA_10_0 || UNITY_WEBGL)
        AppDomain.CurrentDomain.UnhandledException += exceptionHandler;
#endif
		
		SplunkEventManager.init();
		Application.logMessageReceived += logHandler;
		
		Destroy(this);
	}

	// Process logging from Unity to record things out to the DevGUI, Splunk, and/or Stats.
#if (UNITY_WSA_10_0 || UNITY_WEBGL)
// Make public so that it can be called from App.xaml.cs.
public static void logHandler(string name, string stack, LogType type)
#else
private static void logHandler(string name, string stack, LogType type)
#endif
	{
		
		switch (type)
		{
			case LogType.Log:
				if (DevGUIMenuTools.customLogLogs && SmartLog.containsKeyword(name))
				{
					CustomLog.Log.log(name + "\n" + stack, Color.white);
				}
				break;
				
			case LogType.Warning:
				Userflows.logWarning(name);
				if (numberOfTransmittedLogs < LOG_FLOODING_THRESHOLD)
				{
					numberOfTransmittedLogs++;	// Important that this is first, to prevent recursion.
					if (Glb.serverLogWarnings)
					{
						Server.sendLogWarning(name, stack);
					}
				}
				if (DevGUIMenuTools.customLogWarnings && SmartLog.containsKeyword(name))
				{
					CustomLog.Log.log(name + "\n" + stack, Color.yellow);
				}
				break;
			
			case LogType.Error:
				Userflows.logError(name);
				if (numberOfTransmittedLogs < LOG_FLOODING_THRESHOLD)
				{
					numberOfTransmittedLogs++;	// Important that this is first, to prevent recursion.
					if (Glb.serverLogErrors)
					{
						Server.sendLogError(name, stack);
					}
					if (StatsManager.Instance != null)
					{
						StatsManager.Instance.LogCount("debug","error", name, stack, Glb.clientVersion);
					}
				}
				if (DevGUIMenuTools.customLogErrors && SmartLog.containsKeyword(name))
				{
					CustomLog.Log.log(name + "\n" + stack, Color.red);
				}
				break;
		
			case LogType.Exception:
				Userflows.logError(name);
				if (numberOfTransmittedLogs < LOG_FLOODING_THRESHOLD)
				{
					numberOfTransmittedLogs++;	// Important that this is first, to prevent recursion.
					if (Glb.serverLogErrors)
					{
						Server.sendLogError(name, stack);
					}
					if (StatsManager.Instance != null)
					{
						StatsManager.Instance.LogCount("debug", "exception", name, stack, Glb.clientVersion);
					}
				}
				if (DevGUIMenuTools.customLogErrors && SmartLog.containsKeyword(name))
				{
					CustomLog.Log.log(name + "\n" + stack, Color.red);
				}
				break;	
		}
	}

#if !(UNITY_WSA_10_0 || UNITY_WEBGL)
    // Catch unhandled exceptions and filter them into the above logHandler to be recorded.
    private static void exceptionHandler (object sender, UnhandledExceptionEventArgs args)
	{
		string name = "unknown unhandled exception";
		string stack = "";
		
		if (args != null && args.ExceptionObject != null && args.ExceptionObject is Exception)
		{
			Exception e = args.ExceptionObject as Exception;
			name = e.GetType().FullName;
			stack = e.StackTrace.ToString();
		}
		
		logHandler(name, stack, LogType.Exception);
	}
#endif
}
