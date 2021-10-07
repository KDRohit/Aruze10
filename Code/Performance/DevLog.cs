using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
This is an internal performance logging class.
*/
public class DevLog : IResetGame
{
	private const int RUNNING_LOG_LENGTH = 1000;
	
	private static Dictionary<string, float> markScopes = new Dictionary<string, float>();
	private static List<string> runningLog = new List<string>();
	private static string recentLog = "";
	
	/// Used for marking things in code for performance testing.
	/// Displays the time elapsed between calls that use the same scope value.
	/// Takes a message and optional context to add to the Debug.Log() output.
	public static void mark(string scope, string message, UnityEngine.Object context = null)
	{
		if (!Data.debugMode)
		{
			return;
		}
	
		float elapsed = 0f;
		if (markScopes.ContainsKey(scope))
		{
			elapsed = Time.realtimeSinceStartup - markScopes[scope];
			markScopes[scope] = Time.realtimeSinceStartup;
		}
		else
		{
			markScopes.Add(scope, Time.realtimeSinceStartup);
		}
		
		Debug.Log(string.Format("{0:0000.00} --{1}--> {2}", elapsed, scope, message), context);	
	}
	
	/// Records stuff in a persistent client log
	public static void record(string message)
	{
		if (!Data.debugMode)
		{
			return;
		}
		
		runningLog.Add(string.Format("[{0:0000.00}] {1}", Time.realtimeSinceStartup, message));
		
		// Trim the log
		while (runningLog.Count > RUNNING_LOG_LENGTH)
		{
			runningLog.RemoveAt(0);
		}
		
		recentLog = "";
	}
	
	/// Returns the client log as a giant string
	public static string getRecords()
	{
		if (recentLog == "")
		{
			foreach (string record in runningLog)
			{
				recentLog += record + "\n";
			}
		}
		return recentLog;
	}
	
	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		markScopes = new Dictionary<string, float>();
		runningLog = new List<string>();
		recentLog = "";
	}
}
