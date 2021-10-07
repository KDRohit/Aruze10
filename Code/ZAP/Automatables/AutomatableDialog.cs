using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System;
using UnityEngine;

namespace Zap.Automation
{
#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	[Serializable]
	public class AutomatableDialog : Automatable
	{
		public enum DialogType
		{
			CoinDialog,
			DailyBonus,
			DailyRace,
			RobustChallenges,
			XP
		}

		public DialogType dialogKey;

		public AutomatableDialog()
		{
		}

		public AutomatableDialog(SerializationInfo info, StreamingContext context)
		{
			tests = (List<Test>)info.GetValue("tests", typeof(List<Test>));
			key = (string)info.GetValue("key", typeof(string));
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
				if (Glb.isResetting)
				{
					// If we are resetting during the automatable process, we want to pause
					// here and wait while the coroutines get killed.
					while (Glb.isResetting)
					{
						yield return null;
					}
					// If this coroutine hasnt gotten killed already then do so now.
					yield break;
				}
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
					Debug.LogError("ZAPLOG -- No result set for the test.");
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
			throw new System.NotImplementedException();
		}

		public override Test getNextTest()
		{
			throw new System.NotImplementedException();
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("tests", tests);
			info.AddValue("key", key);
		}
	}
#endif
}
