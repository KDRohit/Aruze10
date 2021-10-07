using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Runtime.Serialization;

namespace Zap.Automation
{
#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	[Serializable]
	public class BuyCreditsDialogTest : FeatureTest
	{
		public enum TestingLevel
		{
			OPEN_CLOSE,
			BUY_OPTION,
			BUY_ALL_OPTIONS
		}

		[SerializeField] private TestingLevel testingLevel;

		// need a default constructor to create objects
		public BuyCreditsDialogTest()
		{
		}

		// we need this because it is a requirement for ISerializable to deserialize the data
		public BuyCreditsDialogTest(SerializationInfo info, StreamingContext context)
		{
			DeserializeBaseData(info, context);
		}

		public override void DeserializeBaseData(SerializationInfo info, StreamingContext context)
		{
			base.DeserializeBaseData(info, context);
			info.TryGetValue<TestingLevel>("testingLevel", out testingLevel);
		}

		// serialize the data here
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("testingLevel", testingLevel);
		}

		public override void init()
		{
			base.init();
		}

		public override IEnumerator doTest()
		{
			// All of the testing levels should wait for a neutral lobby.
			yield return RoutineRunner.instance.StartCoroutine(waitForNeutralLobby());
			yield return RoutineRunner.instance.StartCoroutine(ActionController.doAction("CoinDialogOpen"));
			yield return RoutineRunner.instance.StartCoroutine(waitForDialogToOpen("buy_credits_v5"));
			if (!wasCorrectDialog)
			{
				testIsFinished();
				yield break; // Should have already logged the failure.
			}
			switch (testingLevel)
			{
				// Now that the dialog is open, we want to do different things.
				case TestingLevel.OPEN_CLOSE:
					// Just close the dialog.
					yield return RoutineRunner.instance.StartCoroutine(ActionController.doAction("CoinDialogClose"));					
					break;
			}
			testIsFinished();
		}

		public override List<string> compatibleAutomatables(List<string> potentialAutomatables)
		{
			// By default a test is valid for every type passed in.
			return new List<string>(){"AutomatableDialog"};
		}
	}
#endif
}
