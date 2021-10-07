using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using UnityEngine;

// This class stores the results (ie. logs/exceptions/outcomes) of an Automatable test.
// These can be pulled up to view the test results, as well as parsed to create reports.
namespace Zap.Automation
{
#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	using UnityEditor;
	using Newtonsoft.Json;	//The results of a specific automatable

	[Serializable]
	public class AutomatableResult : ISerializable
	{
		public string automatableType;
		public string automatableKey;
		public string automatableName;
		public DateTime startTime;
		public DateTime endTime;
		public string runTime;
		public long startingCredits;
		public long endingCredits;
		public List<TestResult> testResults = new List<TestResult>();
		private List<string> testResultPaths = new List<string>();

		public AutomatableResult() { }

		public AutomatableResult(Automatable automatable)
		{
			automatableType = automatable.GetType().Name;
			automatableKey = automatable.key;
		}

		public AutomatableResult(SerializationInfo info, StreamingContext context)
		{
			automatableType = (string)info.GetValue("automatableType", typeof(string));
			automatableKey = (string)info.GetValue("automatableKey", typeof(string));
			automatableName = (string)info.GetValue("automatableName", typeof(string));
			startTime = (DateTime)info.GetValue("startTime", typeof(DateTime));
			endTime = (DateTime)info.GetValue("endTime", typeof(DateTime));
			runTime = (string)info.GetValue("runTime", typeof(string));
			startingCredits = (long)info.GetValue("startingCredits", typeof(long));
			endingCredits = (long)info.GetValue("endingCredits", typeof(long));
			testResultPaths = (List<string>)info.GetValue("testResults", typeof(List<string>));
		}

		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("automatableType", automatableType);
			info.AddValue("automatableKey", automatableKey);
			info.AddValue("automatableName", automatableName);
			info.AddValue("startTime", startTime);
			info.AddValue("endTime", endTime);
			info.AddValue("runTime", runTime);
			info.AddValue("startingCredits", startingCredits);
			info.AddValue("endingCredits", endingCredits);

			if (string.IsNullOrEmpty(TestPlanResults.directoryPath))
			{
				string folderName = "ZAPRun-" + ZyngaAutomatedPlayer.instance.results.testPlanStartTime.ToString("yyyy-dd-M--HH-mm-ss");
				TestPlanResults.directoryPath = ZAPFileHandler.getZapResultsFileLocation() + "/" + folderName + "/";
			}
			Directory.CreateDirectory(TestPlanResults.directoryPath + "Automatables/" + automatableKey + "/Tests/");

			List<string> testResultFiles = new List<string>();
			foreach (TestResult testResult in testResults)
			{
				//Serialize Automatble result to file
				string jsonTest = JsonConvert.SerializeObject(testResult, Formatting.Indented, new JsonSerializerSettings
				{
					TypeNameHandling = TypeNameHandling.All
				});

				string filePath = TestPlanResults.directoryPath + "Automatables/" + automatableKey + "/Tests/" + testResult.testType + "-" + testResult.testID + ".json";

				testResultFiles.Add(filePath);
			}

			info.AddValue("testResults", testResultFiles);
		}

		public string getSummary(int logLevelDesired = 3, bool breakDownByTest = true)
		{
			System.Text.StringBuilder builder = new System.Text.StringBuilder();
			// Count all the error/exception/warnings from the child tests.
			int errorCount = 0;
			int exceptionCount = 0;
			int warningCount = 0;
			foreach (TestResult testResult in testResults)
			{
				exceptionCount += testResult.exceptions;
				errorCount += testResult.errors;
				warningCount += testResult.warnings;
				if (breakDownByTest)
				{
					// If we want a test breakdown of counts, then grab those as we go through.
					builder.Append(testResult.getSummary(logLevelDesired));
				}
			}
			switch (logLevelDesired)
			{
				// 0 logLevel will not report any.
				case 1: // Exceptions
					if (exceptionCount > 0)
					{
						builder.Insert(0 , string.Format("Exception Count: {0}", exceptionCount));
					}
					break;
				case 2: // + Errors
					if (exceptionCount > 0 || errorCount > 0)
					{
						builder.Insert(0 , string.Format("Exception Count: {0}", exceptionCount));
					}
					break;
				case 3: // + Warnings
					if (exceptionCount > 0 || errorCount > 0 || warningCount > 0)
					{
						builder.Insert(0 , string.Format("Exception Count: {0}", exceptionCount));
					}
					break;
				case 4: // Log everything regardless of values.
					builder.Insert(0 , string.Format("Exception Count: {0}", exceptionCount));
					break;
			}
			return builder.ToString();
		}
	}
#endif
}
