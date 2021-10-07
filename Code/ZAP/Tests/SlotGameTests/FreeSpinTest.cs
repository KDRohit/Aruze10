using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Runtime.Serialization;

namespace Zap.Automation
{
#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	using UnityEditor;

	[Serializable]
	public class FreeSpinTest : Test
	{
		private bool doPaytableCheck = true;

		public FreeSpinTest()
		{
		}

		public FreeSpinTest(SerializationInfo info, StreamingContext context)
		{
			iterations = (int)info.GetValue("iterations", typeof(int));
		}

		public override void init()
		{
			base.init();
			result.testType = "FreeSpinTest";
		}

		public override IEnumerator doTest()
		{
			SlotBaseGame baseGame = ReelGame.activeGame as SlotBaseGame;

			//Make sure the forced outcome is set up for the picking game
			if (!baseGame.checkForcedOutcomes("G"))
			{
				result.additionalInfo.Add(new KeyValuePair<string, string>("Zap Test Error", "Free Spin test, The base game had no forced outcome for the cheat key 'G' aborting test"));
				testIsFinished();
				yield break;
			}

			for (int i = 0; i < iterations; i++)
			{
				yield return RoutineRunner.instance.StartCoroutine(ActionController.doAction("FreeSpinCheat"));
				
				if (baseGame.banners.Length > 0)
				{
					//Some games have this list set with no gameobjects, ZAP won't be able to click on anything.
					bool bannerRootsAreNull = false;

					foreach (GameObject bannerRoot in baseGame.bannerRoots)
					{
						bannerRootsAreNull = (bannerRoot == null);
					}
										
					if (!bannerRootsAreNull)
					{
						while (baseGame.GetComponent<PortalScript>() == null)
						{
							yield return null;
						}
						PortalScript portalScript = baseGame.GetComponent<PortalScript>();
						if (portalScript != null && BonusGameManager.instance != null)
						{
							while (BonusGameManager.instance.bonusGameName == "")
							{
								yield return RoutineRunner.instance.StartCoroutine(ActionController.doAction("RandomClick", BonusGameManager.instance.gameObject));
							}
						}
					}
					else
					{
						Debug.Log("FreeSpinTest: the banner root list is set to " + baseGame.bannerRoots.Length + " objects, but all the objects are null. Most likely this game is not using portal banners");
					}
				}

				while (BonusGamePresenter.instance == null)
				{
					yield return null;
				}
					
				//If we are using a portal instead of banners
				if (!baseGame.playFreespinsInBasegame)
				{
					while (baseGame == ReelGame.activeGame)
					{
						yield return RoutineRunner.instance.StartCoroutine(ActionController.doAction("RandomClick", BonusGamePresenter.instance.gameObject));
					}
				}
				
				//If we pause ZAP wait here until unpaused
				while (ZyngaAutomatedPlayer.instance.wait())
				{
					yield return null;
					// Check and see if there are any clickable UIButtons
					if (BonusGameManager.instance != null)
					{
						if (BonusGamePresenter.instance != null)
						{
							if (doPaytableCheck)
							{
								doPaytableCheck = false;
								RoutineRunner.instance.StartCoroutine(ZyngaAutomatedPaytableImageGrabber.checkForMissingBonusPaytableImages());
							}
							yield return RoutineRunner.instance.StartCoroutine(CommonAutomation.clickRandomColliderIn(BonusGamePresenter.instance.gameObject, isLoggingNothingToClickErrorMsg: false));
						}
					}
				}

				SlotOutcome currentOutcome = baseGame.outcome;

				Debug.Log("Outcome details: isBonus - " + currentOutcome.isBonus + " | isGifting - " + currentOutcome.isGifting + " | ");

				if (currentOutcome != null)
				{
					result.additionalInfo.Add(new KeyValuePair<string, string>("ITERATION_" + i, baseGame.getCurrentOutcome().getJsonObject().ToString()));
				}

				yield return null;
			}
			testIsFinished();
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
