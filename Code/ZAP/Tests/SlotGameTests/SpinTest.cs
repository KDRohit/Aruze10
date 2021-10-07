using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.Serialization;
using System.Linq;
using UnityEngine;

namespace Zap.Automation
{
#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	//TODO: Slam stop on reels
	[Serializable]
	public class SpinTest : Test
	{
		private List<TimeSpan> timePerSpinList = new List<TimeSpan>();

		public SpinTest()
		{
		}

		public SpinTest(int iterations)
		{
			this.iterations = iterations;
		}

		public SpinTest(SerializationInfo info, StreamingContext context)
		{
			iterations = (int)info.GetValue("iterations", typeof(int));
		}

		public override void init()
		{
			Debug.LogFormat("ZAPLOG -- initializing SpinTest!");
			base.init();
			result.testType = "SpinTest";
		}

		public override IEnumerator doTest()
		{
			Debug.LogFormat("ZAPLOG -- doing test: SpinTest!");

			
			long startingCredits = CreditsEconomy.multipliedCredits(SlotsPlayer.instance.socialMember.credits);
			// Get the maximum bet amount for the game, since we may be randomizing the wager amount, the saftest thing
			// to do to ensure we have enough credits going into the test is to determine the max that the player
			// could lose during the test. For fixed tests that aren't maxed, or ones that are using random wagers
			// the actual amount needed will end up being a fair bit less then this, but this should be a safe number.
			long betAmount = CreditsEconomy.multipliedCredits(SlotsWagerSets.getMaxGameWagerSetValue(GameState.game.keyName));
			long totalPotentialLoss = betAmount * iterations;

			Debug.LogFormat("ZAPLOG -- doTest -- starting credits: {0}, betAmount: {1}, total loss: {2}", startingCredits, betAmount, totalPotentialLoss);
			if (totalPotentialLoss > startingCredits)
			{
				long amountNeeded = totalPotentialLoss - startingCredits;
				Debug.LogFormat("ZAPLOG -- Not enough coins to handle all losing spins, adding {0}", amountNeeded);
				yield return RoutineRunner.instance.StartCoroutine(CommonAutomation.addCoinsAndWait(amountNeeded));
			}

			for (int i = 0; i < iterations; i++)
			{
				Debug.LogFormat("ZAPLOG -- spintest iteration: {0}", i);
				DateTime spinStartTime = DateTime.Now;
				long preSpinCredits = SlotsPlayer.instance.socialMember.credits;

				// Randomly adjust the bet
				if (ZyngaAutomatedPlayer.instance.isUsingRandomWagersForSpins)
				{
					SpinPanel spinPanel = SpinPanel.instance;
					if (spinPanel != null)
					{
						yield return RoutineRunner.instance.StartCoroutine(spinPanel.automateChangeWagerForZap());
					}
				}

				yield return RoutineRunner.instance.StartCoroutine(ActionController.doAction("Spin"));

				while (ZyngaAutomatedPlayer.instance.wait())
				{
					yield return null;
				}
				yield return null;
				DateTime spinEndTime = DateTime.Now;
				TimeSpan spinLength = spinEndTime - spinStartTime;
				timePerSpinList.Add(spinLength);
				result.additionalInfo.Add(new KeyValuePair<string, string>("ITERATION_" + i, "Spin length: " + spinLength));
				result.additionalInfo.Add(new KeyValuePair<string, string>("ITERATION_" + i, "Pre spin credits: " + preSpinCredits));
				result.additionalInfo.Add(new KeyValuePair<string, string>("ITERATION_" + i, "Post spin credits: " + SlotsPlayer.instance.socialMember.credits));
				result.additionalInfo.Add(new KeyValuePair<string, string>("ITERATION_" + i, "Change in credits: " + (SlotsPlayer.instance.socialMember.credits - preSpinCredits)));
				if (ReelGame.activeGame.getCurrentOutcome() != null)
				{
					result.additionalInfo.Add(new KeyValuePair<string, string>("ITERATION_" + i, "Win amount: " + ReelGame.activeGame.getCurrentOutcome().getCredits()));
					result.additionalInfo.Add(new KeyValuePair<string, string>("ITERATION_" + i, "Outcome: " + ReelGame.activeGame.getCurrentOutcome().getJsonObject().ToString()));
				}
			}
			double doubleAverageTicks = timePerSpinList.Average(timeSpan => timeSpan.Ticks);
			long longAverageTicks = Convert.ToInt64(doubleAverageTicks);
			TimeSpan averageTime = new TimeSpan(longAverageTicks);
			result.additionalInfo.Add(new KeyValuePair<string, string>("AVERAGE_TIME_PER_SPIN", ": " + averageTime));
			testIsFinished();
			Debug.LogFormat("ZAPLOG -- finished the spin test!");
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
