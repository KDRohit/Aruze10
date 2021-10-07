using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Runtime.Serialization;

// @todo : This test doesn't actually seem to be filled in
namespace Zap.Automation
{
#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	[Serializable]
	public class AutoSpinTest : Test
	{
		public int numberOfAutoSpins = 10;

		public AutoSpinTest()
		{
		}

		public AutoSpinTest(SerializationInfo info, StreamingContext context)
		{
			iterations = (int)info.GetValue("iterations", typeof(int));
		}

		public override void init()
		{
			base.init();
			result.testType = "AutoSpinTest";
		}
		public override IEnumerator doTest()
		{
			Debug.LogFormat("ZAPLOG -- doing test: AutoSpinTest!");
			
			long startingCredits = CreditsEconomy.multipliedCredits(SlotsPlayer.instance.socialMember.credits);
			long betAmount = CreditsEconomy.multipliedCredits(ReelGame.activeGame.betAmount);
			long totalPotentialLoss = betAmount * iterations * numberOfAutoSpins;
			
			Debug.LogFormat("ZAPLOG -- doTest -- starting credits: {0}, betAmount: {1}, total loss: {2}", startingCredits, betAmount, totalPotentialLoss);
			if (totalPotentialLoss > startingCredits)
			{
				long amountNeeded = totalPotentialLoss - startingCredits;
				Debug.LogFormat("ZAPLOG -- Not enough coins to handle all losing spins, adding {0}", amountNeeded);
				yield return RoutineRunner.instance.StartCoroutine(CommonAutomation.addCoinsAndWait(amountNeeded));
			}

			for (int i = 0; i < iterations; i++)
			{
				Debug.LogFormat("ZAPLOG -- AutoSpinTest iteration: {0}, numberOfAutoSpins: {1}", i, numberOfAutoSpins);
				
				DateTime autospinStartTime = DateTime.Now;
				long preAutospinCredits = SlotsPlayer.instance.socialMember.credits;
				
				SpinPanel.instance.automateAutoSpinForZap(numberOfAutoSpins);
				
				while (ZyngaAutomatedPlayer.instance.wait())
				{
					yield return null;
				}
				yield return null;
				DateTime autospinEndTime = DateTime.Now;
				TimeSpan autospinDuration = autospinEndTime - autospinStartTime;
				result.additionalInfo.Add(new KeyValuePair<string, string>("ITERATION_" + i, "Autospin duration: " + autospinDuration));
				result.additionalInfo.Add(new KeyValuePair<string, string>("ITERATION_" + i, "Pre autospin credits: " + preAutospinCredits));
				result.additionalInfo.Add(new KeyValuePair<string, string>("ITERATION_" + i, "Post autospin credits: " + SlotsPlayer.instance.socialMember.credits));
				result.additionalInfo.Add(new KeyValuePair<string, string>("ITERATION_" + i, "Change in credits: " + (SlotsPlayer.instance.socialMember.credits - preAutospinCredits)));
			}
			
			testIsFinished();
			Debug.LogFormat("ZAPLOG -- finished: AutoSpinTest!");

			yield return null;
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("iterations", iterations);
		}

		public override List<string> compatibleAutomatables(List<string> potentialAutomatables)
		{
			return new List<string>(){"AutomatableSlotBaseGame"};
		}		
	}
#endif
}
