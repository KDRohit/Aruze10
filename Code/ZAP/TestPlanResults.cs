using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using UnityEngine;

/// <summary>
/// WIP:  Figuring out how to split up the result classes, IE Do we want a TestPlanResult, AutomatableResult, TestResult, etc... 
/// this would be useful in the sense that we could serialize the results seperately and load up portions of test results.
/// If we tested all the games and the TestPlanResults only show two with exceptions, we can simply load the result for those two 
/// rather than the results for all the games in the plan.
/// 
/// We will want to store Information pertaining to each test an Automatable ran, and each iteration of that test. 
/// </summary>

namespace Zap.Automation
{
#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	using UnityEditor;
	using Newtonsoft.Json;

	//An overview of the all of the test plans results, for a quick glance and break down of problem areas
	[Serializable]
	public class TestPlanResults : ISerializable
	{
		public static string directoryPath;
		//Save the TestPlan that gave us these results so it can be run again if need be.
		private TestPlan _testPlan;  //Maybe just save a filepath/name? rather than serializing the TestPlan for now serializing it incase they didnt save out thier test plan
		private string testPlanPath;
		public string testPlanLogFile; //store out all logs for the run not just the ones the tests catch for themselves?

		//Result classes, automatables will have links to the individual test results
		public List<AutomatableResult> automatableResults = new List<AutomatableResult>();
		public List<string> automatableFilePaths = new List<string>();

		//Session Data
		public string gitBranch;
		public DateTime testPlanStartTime;
		public DateTime testPlanEndTime;
		public string testPlanRunTime;
		public int warningCount;
		public int errorCount;
		public int exceptionCount;

		public int crashCount; //Not sure if ZAP can track this currently
		public int locksCount; //If we have to reset ZAP for some reason, increase this number

		//Player data
		public long playerStartingCredits;
		public long playerEndingCredits;
		public string playerZID;

		public TestPlanResults() { }

		public TestPlanResults(SerializationInfo info, StreamingContext context)
		{
			playerZID = (string)info.GetValue("playerZID", typeof(string));
			gitBranch = (string)info.GetValue("gitBranch", typeof(string));
			playerStartingCredits = (long)info.GetValue("playerStartingCredits", typeof(long));
			playerEndingCredits = (long)info.GetValue("playerEndingCredits", typeof(long));
			testPlanStartTime = (DateTime)info.GetValue("testPlanStartTime", typeof(DateTime));
			testPlanEndTime = (DateTime)info.GetValue("testPlanEndTime", typeof(DateTime));
			testPlanRunTime = (string)info.GetValue("testPlanRunTime", typeof(string));

			warningCount = (int)info.GetValue("warningCount", typeof(int));
			errorCount = (int)info.GetValue("errorCount", typeof(int));
			exceptionCount = (int)info.GetValue("exceptionCount", typeof(int));

			testPlanPath = (string)info.GetValue("testPlan", typeof(string));
			automatableFilePaths = (List<string>)info.GetValue("automatableResults", typeof(List<string>));
		}

		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("playerZID", playerZID);
			info.AddValue("gitBranch", gitBranch);
			info.AddValue("playerStartingCredits", playerStartingCredits);
			info.AddValue("playerEndingCredits", playerEndingCredits);
			info.AddValue("testPlanStartTime", testPlanStartTime);
			info.AddValue("testPlanEndTime", testPlanEndTime);
			info.AddValue("testPlanRunTime", testPlanRunTime);
			info.AddValue("warningCount", warningCount);
			info.AddValue("errorCount", errorCount);
			info.AddValue("exceptionCount", exceptionCount);

			if (string.IsNullOrEmpty(directoryPath))
			{
				string folderName = "ZAPRun-" + ZyngaAutomatedPlayer.instance.results.testPlanStartTime.ToString("yyyy-dd-M--HH-mm-ss");
				directoryPath = ZAPFileHandler.getZapResultsFileLocation() + "/" + folderName + "/";
			}
			Directory.CreateDirectory(directoryPath + "Automatables/");

			//Serialize our test plan to a file so it can be used to test these results
			string testPlanJSON = JsonConvert.SerializeObject(testPlan, Formatting.Indented, new JsonSerializerSettings
			{
				TypeNameHandling = TypeNameHandling.All
			});
			string planPath = directoryPath + testPlan.testPlanName + ".json";
			using (StreamWriter sw = new StreamWriter(planPath))
			{
				sw.Write(testPlanJSON);
				Debug.LogFormat("ZAPLOG -- Writing out test plan JSON: {0}", testPlanJSON);
			}

			//Now that we serialized and saved the TestPlan, lets store the path for it in the results for an easy look up later.
			info.AddValue("testPlan", planPath);

			List<string> automatableResultFiles = new List<string>();
			foreach(AutomatableResult automatableResult in automatableResults)
			{
				//Serialize Automatble result to file
				string automatableJSON = JsonConvert.SerializeObject(automatableResult, Formatting.Indented, new JsonSerializerSettings
				{
					TypeNameHandling = TypeNameHandling.All
				});

				string automatablePath = directoryPath + "Automatables/" + automatableResult.automatableKey + "/" + automatableResult.automatableKey + ".json";
				//using (StreamWriter sw = new StreamWriter(automatablePath))
				//{
				//	sw.Write(automatableJSON);
				//}

				automatableResultFiles.Add(automatablePath);
			}

			info.AddValue("automatableResults", automatableResultFiles);
		}

		public string getSummary()
		{
			System.Text.StringBuilder builder = new System.Text.StringBuilder();
			builder.AppendLine("Error Count: " + errorCount);
			// Go through each sub element and ask for summaries.
			foreach (AutomatableResult result in automatableResults)
			{
				string resultSummary = result.getSummary();
				if (resultSummary != null)
				{
					// Only add a line to the summary if it returned something.
					builder.AppendLine(resultSummary);
				}
			}
			return builder.ToString();
		}

		public TestPlan testPlan
		{
			set { _testPlan = value; }
			get
			{
				if (_testPlan != null)
				{
					return _testPlan;
				}
				else
				{
					if (!string.IsNullOrEmpty(testPlanPath))
					{
						if (File.Exists(testPlanPath))
						{
							_testPlan = JsonConvert.DeserializeObject<TestPlan>(File.ReadAllText(testPlanPath), new JsonSerializerSettings
							{
								TypeNameHandling = TypeNameHandling.All
							});
						}
						else
						{
							_testPlan = null;
							Debug.Log("ZAPResults: file [" + testPlanPath + "] does not exist!");
						}
					}
					else
					{
						_testPlan = null;
						Debug.Log("ZAPResults: test plan path is null or empty!");
					}
				}

				return _testPlan;
			}
		}

		public void saveToFile()
		{
			string resultsJSON = JsonConvert.SerializeObject(
				this,
				Formatting.Indented,
				new JsonSerializerSettings
				{
					TypeNameHandling = TypeNameHandling.All
				});
			string filePath = directoryPath + "Results.json";

			Debug.LogFormat("ZAPLOG -- Saving to file at path: {0}", filePath);
			using (StreamWriter sw = new StreamWriter(filePath))
			{
				sw.Write(resultsJSON);
			}

			SlotsPlayer.getPreferences().SetString(ZAPPrefs.TEST_RESULTS_FOLDER_KEY, directoryPath);
		}
	}
#endif
}
