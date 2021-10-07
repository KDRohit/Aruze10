using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Runtime.Serialization;

namespace Zap.Automation
{
#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	[Serializable]
	public class PickingGameTest : Test
	{
		private bool doPaytableCheck = true;

		public PickingGameTest()
		{
		}

		public PickingGameTest(SerializationInfo info, StreamingContext context)
		{
			iterations = (int)info.GetValue("iterations", typeof(int));
		}

		public override void init()
		{
			base.init();
			result.testType = "PickingGameTest";
		}

		public override IEnumerator doTest()
		{
			SlotBaseGame baseGame = ReelGame.activeGame as SlotBaseGame;
			//Make sure the forced outcome is set up for the picking game
			if (!baseGame.checkForcedOutcomes("C"))
			{
				result.additionalInfo.Add(new KeyValuePair<string, string>("Zap Test Error", "Picking game test, The base game had no forced outcome for the cheat key 'C' aborting test"));
				testIsFinished();
				yield break;
			}

			for (int i = 0; i < iterations; i++)
			{
				yield return RoutineRunner.instance.StartCoroutine(ActionController.doAction("PickingGameCheat"));
				
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
							while (baseGame.gameObject.activeInHierarchy)
							{
								yield return RoutineRunner.instance.StartCoroutine(ActionController.doAction("RandomClick", BonusGameManager.instance.gameObject));
							}
						}
					}
					else
					{
						Debug.Log("PickingGameTest: the banner root list is set to " + baseGame.bannerRoots.Length + " objects, but all the objects are null. Most likely this game is not using portal banners");
					}
				}

				while (baseGame.getCurrentOutcome() == null)
				{
					yield return null;
				}

				SlotOutcome currentOutcome = baseGame.getCurrentOutcome();

				if (currentOutcome != null)
				{
					result.additionalInfo.Add(new KeyValuePair<string, string>("ITERATION_" + i, baseGame.getCurrentOutcome().getJsonObject().ToString()));
				}

				while (BonusGamePresenter.instance == null || baseGame.gameObject.activeInHierarchy)
				{
					yield return null;
				}

				while (BonusGameManager.instance.currentGameType == BonusGameType.PORTAL)
				{
					yield return RoutineRunner.instance.StartCoroutine(ActionController.doAction("RandomClick", BonusGameManager.instance.gameObject));
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
							yield return RoutineRunner.instance.StartCoroutine(CommonAutomation.clickRandomColliderIn(BonusGamePresenter.instance.gameObject));
						}
					}
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
