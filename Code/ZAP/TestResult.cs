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

	//The results for a specific test
	[Serializable]
	public class TestResult : ISerializable
	{
		public string parentAutomatableKey;
		public string testType;
		//We use this incase for some reason someone runs two tests of the same type on the same automatable in the same test plan
		public int testID;
		public DateTime startTime;
		public DateTime endTime;
		public string runTime;
		public long startingCredits;
		public long endingCredits;
		public int warnings;
		public int errors;
		public int exceptions;
		public List<ZapLog> testLogs = new List<ZapLog>();
		public List<KeyValuePair<string, string>> additionalInfo = new List<KeyValuePair<string, string>>();

		public TestResult() { }
		public TestResult(SerializationInfo info, StreamingContext context)
		{
			parentAutomatableKey = (string)info.GetValue("parentAutomatableKey", typeof(string));
			testType = (string)info.GetValue("testType", typeof(string));
			startTime = (DateTime)info.GetValue("startTime", typeof(DateTime));
			endTime = (DateTime)info.GetValue("endTime", typeof(DateTime));
			runTime = (string)info.GetValue("runTime", typeof(string));
			startingCredits = (long)info.GetValue("startingCredits", typeof(long));
			endingCredits = (long)info.GetValue("endingCredits", typeof(long));
			warnings = (int)info.GetValue("warnings", typeof(int));
			errors = (int)info.GetValue("errors", typeof(int));
			exceptions = (int)info.GetValue("exceptions", typeof(int));
			testLogs = (List<ZapLog>)info.GetValue("testLogs", typeof(List<ZapLog>));
			additionalInfo = (List<KeyValuePair<string, string>>)info.GetValue("additionalInfo", typeof(List<KeyValuePair<string, string>>));
		}

		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("parentAutomatableKey", parentAutomatableKey);
			info.AddValue("testType", testType);
			info.AddValue("startTime", startTime);
			info.AddValue("endTime", endTime);
			info.AddValue("runTime", runTime);
			info.AddValue("startingCredits", startingCredits);
			info.AddValue("endingCredits", endingCredits);
			info.AddValue("warnings", warnings);
			info.AddValue("errors", errors);
			info.AddValue("exceptions", exceptions);
			info.AddValue("testLogs", testLogs);			
			info.AddValue("additionalInfo", additionalInfo);
		}

		public string getSummary(int logLevelDesired = 0)
		{
			if (logLevelDesired > 0) // Log level 0 shouldnt report anything.
			{
				switch (logLevelDesired)
				{
					case 1: // Exceptions
						if (exceptions > 0)
						{
							return string.Format("{0} -- Exception Count: {1}", testType, exceptions);
						}
						break;
					case 2: // + Errors
						if (errors > 0 || exceptions > 0)
						{
							return string.Format("{0} -- Exception Count: {1} Error Count: {2}", testType, exceptions, errors);
						}
						break;
					case 3: // + Warnings
						if (exceptions > 0 || errors > 0 || warnings > 0)
						{
							return string.Format("{0} -- Exception Count: {1} Error Count: {2} Warning Count: {3}", testType, exceptions, errors, warnings);
						}
						break;
					case 4: // Log everything regardless of values.
						return string.Format("{0} -- Exception Count: {1} Error Count: {2} Warning Count: {3}", testType, exceptions, errors, warnings);
				}
			}
			return "";
		}

		//For Debugging
		public override string ToString()
		{
			string resultString = "[Result for " + testType + "]\n";
			resultString += "Started on: " + startTime.ToString() + "\n";
			resultString += "Ended on: " + endTime.ToString() + "\n";
			resultString += "Ran for: " + runTime + "\n";
			resultString += "Starting credits: " + startingCredits + "\n";
			resultString += "Ending credits: " + endingCredits + "\n";
			resultString += "The number of logs logged: " + testLogs.Count + "\n";
			resultString += "There are " + warnings + " warnings, " + errors + " errors, and + " + exceptions + " exceptions logged.\n";
			resultString += "Additional Info \n";
			foreach (KeyValuePair<string, string> pair in additionalInfo)
			{
				resultString += "[" + pair.Key + " " + pair.Value + "]\n";
			}
			return resultString;
		}
	}
#endif
}
