using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Runtime.Serialization;
using System.Linq;

namespace Zap.Automation
{
#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	[Serializable]
	public class CheatsTest : Test
	{		
		private List<TimeSpan> timePerSpinList = new List<TimeSpan>();

		public CheatsTest()
		{
		}

		public CheatsTest(SerializationInfo info, StreamingContext context)
		{
			iterations = (int)info.GetValue("iterations", typeof(int));
		}

		public override void init()
		{
			base.init();
			result.testType = "CheatsTest";

			ForcedOutcomeRegistrationModule[] forcedList = SlotBaseGame.instance.GetComponents<ForcedOutcomeRegistrationModule>();

			for (int index = 0; index < forcedList.Length; index++)
			{
				ForcedOutcomeRegistrationModule forced = forcedList[index];

				// Note: Legacy games only had one ForcedOutcomeRegistrationModule and left the targetGameKey blank
				if (string.IsNullOrEmpty(forced.targetGameKey) || forced.targetGameKey == result.parentAutomatableKey)
				{
					foreach (SlotBaseGame.SerializedForcedOutcomeData data in forced.forcedOutcomeList)
					{
						Debug.Log(data.getKeyCodeForForcedOutcomeType(true));
					}
				}
			}		
		}

		public override IEnumerator doTest()
		{
			Debug.LogFormat("ZAPLOG -- doing Cheats Test");
			SlotBaseGame baseGame = ReelGame.activeGame as SlotBaseGame;
			ReelGameBonusPoolsModule bonusPoolModule = null;
			foreach(SlotModule module in baseGame.cachedAttachedSlotModules)
			{
				if(module.GetType() == typeof(ReelGameBonusPoolsModule))
				{
					bonusPoolModule = module as ReelGameBonusPoolsModule;
					break;
				}
			}

			if (baseGame != null)
			{
				for (int i = 0; i < iterations; i++)
				{
					Debug.LogFormat("ZAPLOG -- ChestTest iteration: {0}", i);
					ForcedOutcomeRegistrationModule[] forcedList = SlotBaseGame.instance.GetComponents<ForcedOutcomeRegistrationModule>();

					for (int index = 0; index < forcedList.Length; index++)
					{
						Debug.LogFormat("ZAPLOG -- CheatTest forcedList Index: {0}", index);
						ForcedOutcomeRegistrationModule forced = forcedList[index];

						// Note: Legacy games only had one ForcedOutcomeRegistrationModule and left the targetGameKey blank
						if (string.IsNullOrEmpty(forced.targetGameKey) || forced.targetGameKey == result.parentAutomatableKey)
						{
							string forcedKeyCode;
							foreach (SlotBaseGame.SerializedForcedOutcomeData data in forced.forcedOutcomeList)
							{
								if (data.spinTestRunCount < 1)
								{
									continue;
								}
								
								if (!data.isIgnoredByTramp && data.forcedOutcome.fakeServerMessage == null)
								{
									forcedKeyCode = data.getKeyCodeForForcedOutcomeType(isForTramp: true);

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

									if (forcedKeyCode == "spin")
									{
										// if we get a "spin" for a TRAMP forced outcome it means that the forced outcome couldn't be mapped, so log info about it
										Debug.LogFormat("<color={0}>ZAP> {1} Encountered unknown forced outcome \"{2}\" during test setup, adding \"spin\" instead.</color>",
											Color.green,
											result.parentAutomatableKey,
											data.forcedOutcomeType);

										yield return RoutineRunner.instance.StartCoroutine(ActionController.doAction("Spin"));
									}
									else
									{
										yield return RoutineRunner.instance.StartCoroutine(ActionController.doAction(new KeyPressAction(forcedKeyCode)));
									}

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
												while (baseGame.gameObject.activeInHierarchy && baseGame.isGameBusy)
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

									while (ZyngaAutomatedPlayer.instance.wait())
									{
										// Check and see if there are any clickable UIButtons
										if (BonusGameManager.instance != null)
										{
											if (BonusGamePresenter.instance != null)
											{
												yield return RoutineRunner.instance.StartCoroutine(CommonAutomation.clickRandomColliderIn(BonusGamePresenter.instance.gameObject, isLoggingNothingToClickErrorMsg: false));
											}
										}

										if (bonusPoolModule != null && bonusPoolModule.bonusPoolComponent.gameObject.activeInHierarchy)
										{
											yield return RoutineRunner.instance.StartCoroutine(CommonAutomation.clickRandomColliderIn(bonusPoolModule.bonusPoolComponent.gameObject));
										}
										
										yield return null;
									}


									DateTime spinEndTime = DateTime.Now;
									TimeSpan spinLength = spinEndTime - spinStartTime;
									timePerSpinList.Add(spinLength);
									result.additionalInfo.Add(new KeyValuePair<string, string>("CHEAT[" + forcedKeyCode + "]", "Spin length: " + spinLength));
									result.additionalInfo.Add(new KeyValuePair<string, string>("CHEAT[" + forcedKeyCode + "]", "Pre spin credits: " + preSpinCredits));
									result.additionalInfo.Add(new KeyValuePair<string, string>("CHEAT[" + forcedKeyCode + "]", "Post spin credits: " + SlotsPlayer.instance.socialMember.credits));
									result.additionalInfo.Add(new KeyValuePair<string, string>("CHEAT[" + forcedKeyCode + "]", "Change in credits: " + (SlotsPlayer.instance.socialMember.credits - preSpinCredits)));
									if (ReelGame.activeGame.getCurrentOutcome() != null)
									{										
										result.additionalInfo.Add(new KeyValuePair<string, string>("CHEAT[" + forcedKeyCode + "]", "Outcome: " + baseGame.outcome.getJsonObject()));
									}

									while (ZyngaAutomatedPlayer.instance.wait())
									{
										yield return null;
									}
									yield return null;
								}
								else
								{
									Debug.LogError("WE SKIPPED A CHEAT KEY :" + data.getKeyCodeForForcedOutcomeType(isForTramp: true));
								}								
							}
						}
					}					
				}
				double doubleAverageTicks = timePerSpinList.Average(timeSpan => timeSpan.Ticks);
				long longAverageTicks = Convert.ToInt64(doubleAverageTicks);
				TimeSpan averageTime = new TimeSpan(longAverageTicks);
				result.additionalInfo.Add(new KeyValuePair<string, string>("AVERAGE_TIME_PER_SPIN", ": " + averageTime));
			}
			testIsFinished();
		}

		public override List<string> compatibleAutomatables(List<string> potentialAutomatables)
		{
			return new List<string>(){"AutomatableSlotBaseGame"};
		}
	}
#endif
}
