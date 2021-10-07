using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Runtime.Serialization;

/*
Class Name: RobustChallengesResetPretest.cs
Author: Michael Christensen-Calvin <mchristensencalvin@zynga.com>
Description: This class is used for resetting the RobustChallenges feature progress before starting testing.
Feature-flow: Add this to an AutomatableTestSetup to make sure that you reset challenge progress.
*/
namespace Zap.Automation
{
#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	[Serializable]
	public class RobustChallengesResetPretest : Pretest
	{
		private bool isWaitingOnChallengeReset = false;

		// need a default constructor to create objects
		public RobustChallengesResetPretest() {}

		// we need this because it is a requirement for ISerializable to deserialize the data
		public RobustChallengesResetPretest(SerializationInfo info, StreamingContext context)
		{
			DeserializeBaseData(info, context);
		}

		public override void DeserializeBaseData(SerializationInfo info, StreamingContext context)
		{
			base.DeserializeBaseData(info, context);
		}
		
		public override void init()
		{
			base.init();
		}

		public override IEnumerator doTest()
		{
			// Wait for us to finish if we are loading something.
			while (Loading.isLoading)
			{
				yield return new WaitForSeconds(0.5f);
			}

			Server.registerEventDelegate("reset_challenges", challengeResetCallback);
			isWaitingOnChallengeReset = true;
			RobustChallengesAction.resetChallenges();
			yield return RoutineRunner.instance.StartCoroutine(waitForCallbacks());
			testIsFinished();
		}

		private IEnumerator waitForCallbacks()
		{
			while (isWaitingOnChallengeReset)
			{
				yield return new WaitForSeconds(0.5f);
			}
			yield break;
		}

		private void challengeResetCallback(JSON data)
		{
			if (!data.getBool("success", false))
			{
				throw new System.Exception("Failed to reset challenges.");
			}
			isWaitingOnChallengeReset = false;
		}
	}
#endif
}
