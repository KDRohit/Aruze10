using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Zap.Automation
{
#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	using Newtonsoft.Json;
	using UnityEditor;

	//This class is useful for testing but after said testing will most likely be removed for a more "test-centric" handling of the logs.
	public static class ZAPLogHandler
	{
		//Used for a running tally while the test plan is running.
		public static int errorCount = 0;
		public static int exceptionCount = 0;
		public static int warningCount = 0;
		public static int desyncCount = 0;

		//Purely for testing, will make more sense for the results for specific to store thier own logs list so they can be filtered/sorted nicer while reviewing the test plan results
		public static List<ZapLog> logs = new List<ZapLog>();		

		public static void handleLog(string logMessage, string stackTrace, LogType logType)
		{
			ZapLog log = null;
			switch (logType)
			{
				case LogType.Error:
					log = new ZapLog(logMessage, stackTrace, ZapLogType.Error, "#800000");
					if (ReelGame.activeGame != null && ReelGame.activeGame.getCurrentOutcome() != null)
					{
						log.additionalInfo.Add(new KeyValuePair<string, string>("CURRENT_OUTCOME", ReelGame.activeGame.getCurrentOutcome().getJsonObject().ToString()));
					}
					if (logMessage.Contains("Coins desync detected on mobile client:"))
					{
						string desyncLog = logMessage.Replace("Coins desync detected on mobile client:", "");
						log = new ZapLog(desyncLog, stackTrace, ZapLogType.Desync, "#f442d7");
						desyncCount++;
					}
					errorCount++;
					break;
				case LogType.Exception:
					log = new ZapLog(logMessage, stackTrace, ZapLogType.Exception, "#ff0000");
					if (ReelGame.activeGame != null && ReelGame.activeGame.getCurrentOutcome() != null)
					{
						log.additionalInfo.Add(new KeyValuePair<string, string>("CURRENT_OUTCOME", ReelGame.activeGame.getCurrentOutcome().getJsonObject().ToString()));
					}
					exceptionCount++;
					break;
				case LogType.Warning:
					log = new ZapLog(logMessage, stackTrace, ZapLogType.Warning, "#ffcc00");
					warningCount++;
					break;
				case LogType.Log:
					if(logMessage.Contains("SlotOutcome.outcomeJson:"))
					{
						string outcomeJSON = logMessage.Replace("SlotOutcome.outcomeJson:", "");
						log = new ZapLog(outcomeJSON, stackTrace, ZapLogType.Outcome, "#4169E1");
					}					
					break;
				default:
					break;
			}

			if (log != null)
			{
				logs.Add(log);
				if (log.logType == ZapLogType.Exception)
				{
					ZyngaAutomatedPlayer.instance.skipAutomatable();
				}
			}
		}
	}

	

	public enum ZapLogType
	{
		Error = 0,
		Assert = 1,
		Warning = 2,
		Log = 3,
		Exception = 4,
		Outcome = 5,
		Desync = 6
	}

	//Simple class to save log data
	[Serializable]
	public class ZapLog
	{
		public DateTime timestamp;
		public string message;
		public string stackTrace;
		public ZapLogType logType;
		public string color;
		public List<KeyValuePair<string, string>> additionalInfo = new List<KeyValuePair<string, string>>();
		
		public ZapLog(string m, string st, ZapLogType lt, string c)
		{
			message = m;
			stackTrace = st;
			logType = lt;
			color = c;
			timestamp = DateTime.UtcNow;
		}
		
		public override string ToString()
		{
			return "[" + logType.ToString() + "] " + message + " - " + stackTrace;
		}
	}
#endif
}
