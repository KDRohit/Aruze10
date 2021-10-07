using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System;
using UnityEngine;

namespace Zap.Automation
{
#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	[Serializable]
	public class AutomatableTestSetup : Automatable
	{
		public bool shouldReset = true;

		public AutomatableTestSetup()
		{
		}

		public AutomatableTestSetup(SerializationInfo info, StreamingContext context)
		{
			tests = (List<Test>)info.GetValue("tests", typeof(List<Test>));
			key = (string)info.GetValue("key", typeof(string));
			shouldReset = (bool)info.GetValue("shouldReset", typeof(bool));
		}

		public void init(String gameKey, List<Test> tests)
		{
			this.key = gameKey;
			this.tests = tests;
		}

		public override IEnumerator startTests()
		{
			Test test;
			for (int i = 0; i < tests.Count; i++)
			{
				Debug.LogFormat("ZAPLOG -- running test setup task index: {0}", i);
				// Set the index.
				test = tests[i];
				test.init();
				if (test.result != null)
				{
					test.result.testID = i;
					test.result.parentAutomatableKey = key;
					automatableResult.testResults.Add(test.result);
				}
				else
				{
					Debug.LogError("No result set for the test.");
				}
				yield return RoutineRunner.instance.StartCoroutine(test.doTest());

				while (!test.isTestFinished)
				{
					yield return null;
				}
				yield return null;
			}
		}

		public override void onTestsFinished()
		{
			Debug.LogFormat("ZAPLOG -- Finishing AutomatableTestSetup with key: {0}", key);
			// This is called after we have totally finished the tests and saved out the results.
			// Increment the automatable index.

			// Mark the resume flag
			SlotsPlayer.getPreferences().SetInt(ZAPPrefs.SHOULD_RESUME, 1);
			SlotsPlayer.getPreferences().Save();

			Glb.resetGame("Resetting after we have finish test setup to get new data.");
		}

		public override Test getNextTest()
		{
			throw new System.NotImplementedException();
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("tests", tests);
			info.AddValue("key", key);
			info.AddValue("shouldReset", shouldReset);
		}
	}
#endif
}
