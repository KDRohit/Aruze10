using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System;
using UnityEngine;

namespace Zap.Automation
{
#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	using UnityEditor;

	[Serializable]
	public class AutomatableSlotBaseGame : Automatable
	{		
		private enum State
		{
			BaseGame,
			PickingGame,
			FreeSpinGame,
			CheatsTest
		};

		private State zapGameState = State.BaseGame;
		private const float MAX_TIMEOUT_FOR_BACK_BUTTON_ENTERING_LOADING = 5.0f;

		public AutomatableSlotBaseGame()
		{
		}

		public AutomatableSlotBaseGame(SerializationInfo info, StreamingContext context)
		{
			tests = (List<Test>)info.GetValue("tests", typeof(List<Test>));
			key = (string)info.GetValue("gameKey", typeof(string));
		}

		//Removes pickinggame/freespin tests in games that do not have the corresponding feature.
		public void checkTests()
		{			
			if (string.IsNullOrEmpty(SlotResourceMap.map[key].bonusPrefabPath))
			{
				this.tests.RemoveAll(i => i.GetType() == typeof(PickingGameTest));
				Debug.LogError("No bonusPrefabPath found in the SlotResourceMap for " + key + ", removing the PickingGameTest.");
			}

			if (string.IsNullOrEmpty(SlotResourceMap.map[key].freeSpinPrefabPath))
			{
				this.tests.RemoveAll(i => i.GetType() == typeof(FreeSpinTest));
				Debug.LogError("No freeSpinPrefabPath found in the SlotResourceMap for " + key + ", removing the FreeSpinTest.");
			}
		}
				
		public override IEnumerator startTests()
		{
			Debug.Log("ZAPLOG - AutomatableSlotBaseGame.startTests() - Called. Load Game: " + key);
			checkTests();

			// Wait for loading screen to end.
			while (Loading.isLoading)
			{
				yield return new WaitForSeconds(0.5f);
			}

			// Way for a couple seconds (variable) to make sure everything has queued that will.
			yield return new WaitForSeconds(1.0f);
			
			// Clear any open dialogs that are queued.
			yield return RoutineRunner.instance.StartCoroutine(CommonAutomation.automateClearDialogsAndShrouds());

			// Wait for a second to make sure everything has closed.
			yield return new WaitForSeconds(1f);
			
			LobbyGame lobbyGame = LobbyGame.find(key);
			if (lobbyGame == null)
			{
				Debug.LogError("ZAPLOG -- Unable to find LobbyGame for key = " + key);
			}
			
			if (lobbyGame != null && !lobbyGame.isUnlocked)
			{
				Debug.LogFormat("ZAPLOG -- Game is not unlocked, unlocking it before continuing the test.");
				yield return RoutineRunner.instance.StartCoroutine(CommonAutomation.unlockGameAndWait(lobbyGame.keyName));
				yield return RoutineRunner.instance.StartCoroutine(CommonAutomation.automateClearDialogsAndShrouds());
			}

			// Now load the game and enter it.
			yield return RoutineRunner.instance.StartCoroutine(loadGame());

			// Clear any open dialogs that are queued.
			Debug.Log("ZAPLOG - " + key + " Loaded");
			float timeout = 0f;
			while (ReelGame.activeGame == null)
			{
				// Waiting until this is not null.
				yield return null;
				timeout += Time.unscaledDeltaTime;
				if (timeout > 10f)
				{
					Debug.LogFormat("ZAPLOG -- Timed out waiting for the ReelGame.activeGame to not be null.");
					yield break;
				}
			}
			SlotBaseGame baseGame = ReelGame.activeGame as SlotBaseGame;

			//Check to see if the base game has a jackpot built in
			BuiltInProgressiveJackpotBaseGameModule jackpotModule = null;
			if (baseGame == null)
			{
				Debug.LogErrorFormat("ZAPLOG -- base game failed to load, skipping tests.");
				yield break;
			}

			foreach(SlotModule module in baseGame.cachedAttachedSlotModules)
			{
				if(module.GetType() == typeof(BuiltInProgressiveJackpotBaseGameModule))
				{
					jackpotModule = module as BuiltInProgressiveJackpotBaseGameModule;
				}
			}
				
			//TODO - Need a way to check to see if the module is active 
			for (int i = 0; i < tests.Count; i++)
			{
				Test test = tests[i];
				switch (test.GetType().Name)
				{
					case "PickingGameTest":
						zapGameState = State.PickingGame;
						break;
					case "FreeSpinTest":
						zapGameState = State.FreeSpinGame;
						break;
					case "CheatsTest":
						zapGameState = State.CheatsTest;
						break;
					default:
						zapGameState = State.BaseGame;
						break;
				}

				Debug.Log("ZAPLOG -- " + key + " " + test.GetType());
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

				activeTest = test;

				//Wait while we are busy
				while (ZyngaAutomatedPlayer.instance.wait())
				{
					yield return null;
				}
				
				// In addition to checking for a BonusGamePresenter we also need to check for
				// a bonus pools object, since games like elvira01 that use that will need
				// buttons pressed when these are presented
				BaseBonusPoolsComponent bonusPoolsComponent = ReelGame.activeGame.GetComponentInChildren<BaseBonusPoolsComponent>(true);

				//Start test
				RoutineRunner.instance.StartCoroutine(test.doTest());
				while (!test.isTestFinished)
				{
					if (zapGameState == State.BaseGame)
					{
						while (BonusGamePresenter.instance != null)
						{
							// Make sure we skip random pressing if a dialog becomes active so that we don't
							// simulate multiple clicks in the same frame which can prevent clicks from registering
							if (CommonAutomation.IsDialogActive())
							{
								yield return null;
							}
							else
							{
								yield return RoutineRunner.instance.StartCoroutine(ActionController.doAction("RandomClick", BonusGameManager.instance.gameObject));
							}
						}

						// Check if we have a bonus pool showing that needs stuff pressed on it
						while (bonusPoolsComponent != null && bonusPoolsComponent.gameObject.activeInHierarchy)
						{
							// Make sure we skip random pressing if a dialog becomes active so that we don't
							// simulate multiple clicks in the same frame which can prevent clicks from registering
							if (CommonAutomation.IsDialogActive())
							{
								yield return null;
							}
							else
							{
								yield return RoutineRunner.instance.StartCoroutine(ActionController.doAction("RandomClick", bonusPoolsComponent.gameObject));
							}
						}
					}
					yield return null;
				}
				yield return null;
			}
		}

		private IEnumerator loadGame()
		{
			Debug.LogFormat("ZAPLOG -- Starting loading into game: {0}", key);
			if (GameState.game != null && (key == "" || key == GameState.game.keyName))
			{
				// We are in the game that we want to do the automation in.
				key = GameState.game.keyName;
				automatableResult.automatableName = GameState.game.name;
				Debug.LogFormat("ZAPLOG -- already in that game.");
			}
			else
			{
				bool isBackButtonPressed = false;
				if (!GameState.isMainLobby)
				{
					isBackButtonPressed = true;
					yield return RoutineRunner.instance.StartCoroutine(ActionController.doAction("Back"));
				}

				if (isBackButtonPressed)
				{
					// We need to wait until the back button launches the the Loading screen before proceeding.
					// So we can correctly wait for the Loading screen to be hidden.  Previously this was just a time
					// wait which is a bit unsafe since we don't know for sure that the Loading has started and can
					// create a situation where we don't correctly wait for the Loading to happen.
					
					// NOTE : Going to add a safety timeout, since this could cause issues if for instance
					// the back button could not be pressed.  I'll log something if the timeout is hit which
					// should hopefully help diagnose what went wrong.
					float timeWaitedForLoadingToStart = 0.0f;
					while (!Loading.isLoading && timeWaitedForLoadingToStart < MAX_TIMEOUT_FOR_BACK_BUTTON_ENTERING_LOADING)
					{
						yield return null;
						timeWaitedForLoadingToStart += Time.unscaledDeltaTime;
					}

					if (timeWaitedForLoadingToStart > MAX_TIMEOUT_FOR_BACK_BUTTON_ENTERING_LOADING)
					{
						Debug.LogError("ZAPLOG - AutomatableSlotBaseGame.loadGame() - Timed out waiting for the Overlay Back button to trigger a Loading state back to main lobby!");
					}
				}
				
				while (!GameState.isMainLobby || Loading.isLoading)
				{
					yield return null;
				}
				Debug.LogFormat("ZAPLOG -- finished loading back to lobby, now loading into game.");

				LobbyGame gameInfo = LobbyGame.find(key);
				if (gameInfo != null)
				{
					automatableResult.automatableName = gameInfo.name;
				}

				SlotResourceData resourceData = SlotResourceMap.getData(key);

				// Load the game.
				Overlay.instance.top.showLobbyButton();
				GameState.pushGame(gameInfo);
				Loading.show(Loading.LoadingTransactionTarget.GAME);
				Glb.loadGame();
				
				SlotBaseGame baseGame = ReelGame.activeGame as SlotBaseGame;
				while (GameState.isMainLobby || Loading.isLoading || baseGame == null || baseGame.isExecutingGameStartModules)
				{
					// Wait until we get into the game and intro animations are done.
					yield return null;

					if (baseGame == null)
					{
						baseGame = ReelGame.activeGame as SlotBaseGame;
					}
				}
				
				Debug.LogFormat("ZAPLOG -- Finished clearing game intro: {0}", key);
				
				// Automate the bet selector to just pick the default chosen bet amount so we can clear that out of the way before
				// starting to test the game
				BuiltInProgressiveJackpotBaseGameModule betSelectorModule = baseGame.getBuiltInProgressiveJackpotBaseGameModule();
				if (betSelectorModule != null)
				{
					Debug.LogFormat("ZAPLOG -- Automating bet selector for: {0}", key);
					yield return RoutineRunner.instance.StartCoroutine(betSelectorModule.automateBetSelection());
				}
				
				Debug.LogFormat("ZAPLOG -- Finished loading into game: {0}", key);
			}
			Debug.LogFormat("ZAPLOG -- Finished loadGame : {0}", key);
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
			info.AddValue("gameKey", key);
		}
	}
#endif
}
