using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using UnityEngine;

namespace Zap.Automation
{
#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	using Newtonsoft.Json;

	[Serializable]
	public abstract class Test : ScriptableObject, ISerializable
	{		
		public bool isTestFinished = false;
		public int iterations = 1;
		
		public abstract IEnumerator doTest();

		[IgnoreDataMember]
		public TestResult result = null;
		
		public virtual void init()
		{
			result = new TestResult();
			result.startingCredits = SlotsPlayer.instance.socialMember.credits;
			result.startTime = DateTime.UtcNow;
			Application.logMessageReceived += handleLog;
		}
		
		// base serialization method. Anything that inherits should call make a base call to this
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("iterations", iterations);
		}

		// base serialization method. Anything that inherits should call make a base call to this
		public virtual void DeserializeBaseData(SerializationInfo info, StreamingContext context)
		{
			info.TryGetValue<int>("iterations", out iterations);
		}

		public virtual void testIsFinished()
		{
			isTestFinished = true;
			Application.logMessageReceived -= handleLog;
			result.endTime = DateTime.UtcNow;
			TimeSpan runtime = result.endTime - result.startTime;
			result.runTime = string.Format("{0} hours, {1} minutes, {2} seconds, {3} milliseconds", runtime.Hours, runtime.Minutes, runtime.Seconds, runtime.Milliseconds);
			result.endingCredits = SlotsPlayer.instance.socialMember.credits;
			saveResults();
		}

		public virtual void saveResults()
		{
			//Serialize Automatble result to file
			string jsonTest = JsonConvert.SerializeObject(result, Formatting.Indented, new JsonSerializerSettings
			{
				TypeNameHandling = TypeNameHandling.All
			});

			string filePath = TestPlanResults.directoryPath + "Automatables/" + result.parentAutomatableKey + "/Tests/" + result.testType + "-" + result.testID + ".json";
			Directory.CreateDirectory(TestPlanResults.directoryPath + "Automatables/" + result.parentAutomatableKey + "/Tests/");
			using (StreamWriter sw = new StreamWriter(filePath))
			{
				sw.Write(jsonTest);
			}
		}

		public virtual void handleLog(string logMessage, string stackTrace, LogType logType)
		{
			ZapLog log = null;
			switch (logType)
			{
				case LogType.Error:
					result.errors++;					
					if (logMessage.Contains("Coins desync detected on mobile client:"))
					{
						string outcomeJSON = logMessage.Replace("Coins desync detected on mobile client:", "");
						log = new ZapLog(outcomeJSON, stackTrace, ZapLogType.Desync, "#f442d7");
					}
					else
					{
						log = new ZapLog(logMessage, stackTrace, ZapLogType.Error, "#800000");
					}
					break;
				case LogType.Exception:
					result.exceptions++;
					log = new ZapLog(logMessage, stackTrace, ZapLogType.Exception, "#ff0000");					
					break;
				case LogType.Warning:
					result.warnings++;
					log = new ZapLog(logMessage, stackTrace, ZapLogType.Warning, "#ffcc00");					
					break;
				case LogType.Log:
					if (logMessage.Contains("SlotOutcome.outcomeJson:"))
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
				result.testLogs.Add(log);

				if (log.logType == ZapLogType.Exception)
				{
					// The game is going to be restarted because of this, so we need to save these results.
					// This is in addition to the save that is triggered by ZAPLogHandler because this logging
					// callback may happen after that one (it does right now).
					saveResults();
				}
			}
		}

		public virtual List<string> compatibleAutomatables(List<string> potentialAutomatables)
		{
			// By default a test is valid for every type passed in.
			return potentialAutomatables;
		}

		protected void addTestStepLog(string step, string message)
		{
			result.additionalInfo.Add(new KeyValuePair<string, string>(step, message));
			Debug.LogFormat("ZAPLOG -- {0} -- {1}", step, message);
		}
	}
#endif
}
