using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System;
using UnityEngine;

namespace Zap.Automation
{
#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	[Serializable]
	public class TestPlan : ScriptableObject, ISerializable
	{
		public string testPlanName;
		public List<Automatable> automatables = new List<Automatable>();

		public TestPlan() : base()
		{
		}

		public TestPlan(SerializationInfo info, StreamingContext context)
		{
			automatables = (List<Automatable>)info.GetValue("thingsToTest", typeof(List<Automatable>));
			testPlanName = (string)info.GetValue("testPlanName", typeof(string));
		}

		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("thingsToTest", automatables);
			info.AddValue("testPlanName", testPlanName);
		}

		public static TestPlan generateTestPlan(int i, int numInstances)
		{
			/* TODO -- used for instanced testing runs, this should generate a test plan of:
			n = allGames/numInstances
			should generate a test plan of n games.
			starting at index: i * n of the all games list.

			We will then serialize this to JSON and store it in the PlayerPref before running the test.
			*/
			return null;
		}
	}
#endif
}
