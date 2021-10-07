using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Runtime.Serialization;

namespace Zap.Automation
{
#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	[Serializable]
	public class RobustChallengesTest : FeatureTest
	{	
		private const string IN_GAME_OPEN_CHALLENGES = "RobustInGameLobbyButton";
		private const float OPEN_DIALOG_WAIT_TIME = 5.0f; // Wait for animations to complete.
		private const string DIALOG_BUTTON_CLOSE = "RobustDialogCloseButton";
		private const string DIALOG_BUTTON_OBJECTIVE = "RobustDialogObjectiveButton";
		private const string ROBUST_CAROUSEL_KEY = "robust_challenges_motd";

		// Variables used during testing process.
		private bool isTestRunning = false;
		private bool isWaitingForEvent = false;

		// Test Data Results Tracking.
		private int totalSpinCount = 0;
		private int numObjectivesCompleted = 0;
		private float averageSpinCountPerObjective = 0.0f;

		private System.Random random;

		// Empty constructor needed for serialization.
		public RobustChallengesTest(){}
		
		// We need this because it is a requirement for ISerializable to deserialize the data
		public RobustChallengesTest(SerializationInfo info, StreamingContext context)
		{
			base.DeserializeBaseData(info, context);
		}

		public override void init()
		{
			base.init();
		}

		public override IEnumerator doTest()
		{
			Debug.LogFormat("ZAPLOG -- Starting RobustChallengesTest");
			// Make sure this is off when we are testing, we will handle closing dialogs manually.
			ZyngaAutomatedPlayer.instance.shouldClearDialogs = false;

			random = new System.Random();
			// The meat of the test should be done here.
			// Register for these campaign events since we will need to hook into them to know when server events come down.
			ChallengeCampaign.onShowCampaignComplete += campaignComplete;
			ChallengeCampaign.onShowMissionComplete += missionComplete;
			ChallengeCampaign.onShowChallengeComplete += challengeComplete;
			ChallengeCampaign.onShowChallengeReset += challengeReset;

			// Check if the event is active:
			if (!RobustCampaign.hasActiveRobustCampaignInstance)
			{
				// Bail out if its not.
				if (CampaignDirector.robust != null)
				{
					Debug.LogErrorFormat("Could not perform test, robust challenges was not active because: {0}", CampaignDirector.robust.notActiveReason);
					Debug.LogException(new System.Exception("Could not perform test, robust challenges was not active because: " + CampaignDirector.robust.notActiveReason));
				}
				else
				{
					Debug.LogError("ZAPLOG -- Could not perform test, robust challenges was not active because the campaign was null");
					Debug.LogException(new System.Exception("Could not perform test, robust challenges was not active because the campaign was null"));
				}
				testIsFinished();
				yield break;
			}
			isTestRunning = true;

			yield return RoutineRunner.instance.StartCoroutine(waitForNeutralLobby());
			// If we are ready to play the event, then start the loop.
			yield return RoutineRunner.instance.StartCoroutine(testingLoop());
			testIsFinished();
		}
		
		private IEnumerator testingLoop()
		{
			while (isTestRunning)
			{
				while (Loading.isLoading)
				{
					yield return new WaitForSeconds(0.5f);
				}
				// Open up the robust challenges MOTD dialog to attempt an objective
				yield return RoutineRunner.instance.StartCoroutine(tryOpenRobustChallengesMotd());

				yield return RoutineRunner.instance.StartCoroutine(waitForDialogToOpen());

				if (Dialog.instance != null && Dialog.instance.currentDialog != null)
				{
					string dialogKey = Dialog.instance.currentDialog.type.keyName;
					if (dialogKey == "robust_challenges_motd")
					{
						// If there are any unfinished challenges then open that game and spin until the challenge is complete.
						yield return RoutineRunner.instance.StartCoroutine(automateChallengesMotd());
					}
					else
					{
						Debug.LogErrorFormat("ZAPLOG -- incorrect dialog was open: {0}", dialogKey);
					}
				}
				else
				{
					Debug.LogErrorFormat("RobustChallengesTest.cs -- testingLoop() -- no dialog was open.");
				}
			}

			// Once we have stopped the test, automate once more to clear out the collect dialog.
			yield return RoutineRunner.instance.StartCoroutine(CommonAutomation.automateClearDialogsAndShrouds());
		}

		// Attempt to open the robust challenges MOTD. If it is on, then the carousel should be active and we can click on that.
		// If we are in a game, then the icon should be active and we can click on that. If either of these tests fail (we should
		// hit both of these on a normal challenges playthrough then the test should spit out that it failed.)
		private IEnumerator tryOpenRobustChallengesMotd()
		{
			Debug.LogFormat("ZAPLOG -- Trying to open the robust challenges MOTD");

			// Clear any open dialogs that might have opened after a spin, etc..
			yield return RoutineRunner.instance.StartCoroutine(CommonAutomation.automateClearDialogsAndShrouds());
			
			// Wait for a second to let dialogs finish closing.
			yield return new WaitForSeconds(1f);
			
			if (GameState.isMainLobby)
			{
				CarouselData data = null;
				// Try to find the carousel for robust challenges.
				for (int i = 0; i < CarouselData.looped.Count; i++)
				{
					if (CarouselData.looped[i].actionName == ROBUST_CAROUSEL_KEY)
					{
						// Found the robust carousel.
						data = CarouselData.looped[i];
					}
				}
				if (data != null)
				{
					Debug.LogFormat("ZAPLOG -- Click on the carousel card to open the Robust Challenges MOTD");
					// Activate it!
					DoSomething.now(data.actionName);
				}
				else
				{
					Debug.LogError("ZAPLOG -- Could not find the robust challenges carousel.");
					Debug.LogException(new System.Exception("Could not find the robust challenges carousel."));
					isTestRunning = false;
				}
			}
			else
			{
				Debug.LogFormat("ZAPLOG -- Clicking on the in game challenges icon to open the MOTD");
				// Try to click the challenges button in game.
				yield return RoutineRunner.instance.StartCoroutine(ActionController.doAction(IN_GAME_OPEN_CHALLENGES));
			}
			// Wait for it to open.
			yield return new WaitForSeconds(OPEN_DIALOG_WAIT_TIME);
		}

		// Once the dialog is open, we want to now automate the dialog to either close it, or go into the correct game
		// so that we can start spinning.
		private IEnumerator automateChallengesMotd()
		{
			Debug.LogFormat("ZAPLOG -- Automating the open challenges dialog.");
			// Click on an action button.
			Action buttonAction = ActionController.getAction(DIALOG_BUTTON_OBJECTIVE);
			if (buttonAction != null && buttonAction.isValid())
			{
				yield return RoutineRunner.instance.StartCoroutine(ActionController.doAction(DIALOG_BUTTON_OBJECTIVE));
				// This should load that game. Now spin until we get the event from the server.
				yield return RoutineRunner.instance.StartCoroutine(spinUntilChallengeEvent());
			}
			else if (GameState.game != null &&
				CampaignDirector.robust != null &&
				CampaignDirector.robust.currentMission != null &&
				CampaignDirector.robust.currentMission.containsGame(GameState.game.keyName))
			{
				// If we are in a game with a current challenge then there wont be a play button so just close the dialog and spin.
				yield return RoutineRunner.instance.StartCoroutine(ActionController.doAction(DIALOG_BUTTON_CLOSE));
				yield return RoutineRunner.instance.StartCoroutine(spinUntilChallengeEvent());
			}
			else if (CampaignDirector.robust != null &&
				CampaignDirector.robust.currentMission != null)
			{
				// Some challenges can be completed in any game, and dont have play button, search for that now.
				bool hasGamelessObjective = false;
				for (int i = 0; i < CampaignDirector.robust.currentMission.objectives.Count; i++)
				{
					if (string.IsNullOrEmpty(CampaignDirector.robust.currentMission.objectives[i].game))
					{
						// If we found an objective without a game, then load up a random game and spin.
						hasGamelessObjective = true;
						break;
					}
				}
				if (hasGamelessObjective)
				{
					yield return RoutineRunner.instance.StartCoroutine(ActionController.doAction(DIALOG_BUTTON_CLOSE));
					// Open up a random game that we have unlocked.
					string gameKey = CommonZap.getRandomUnlockedGame();
					yield return RoutineRunner.instance.StartCoroutine(CommonZap.loadGameFromLobby(gameKey));
					yield return RoutineRunner.instance.StartCoroutine(spinUntilChallengeEvent());
				}
				else
				{
					yield return RoutineRunner.instance.StartCoroutine(ActionController.doAction(DIALOG_BUTTON_CLOSE));
					isTestRunning = false;
					throw new System.Exception("ZAPLOG -- RobustChallengesTest.cs -- tryOpenRobustChallengesMotd() -- Cannot find a play button and no valid objective seems to exist, bailing out of test.");
				}
			}
			else
			{
				Debug.LogErrorFormat("ZAPLOG -- failed to find a button object. Calling close instead.");
				yield return RoutineRunner.instance.StartCoroutine(ActionController.doAction(DIALOG_BUTTON_CLOSE));
			}
		}

		private IEnumerator spinUntilChallengeEvent()
		{
			Debug.LogFormat("ZAPLOG -- Spinning until challenge event comes down.");
			// Mark us as waiting for an event.
			isWaitingForEvent = true;
			timeoutCounter = 0.0f;

			// Repeat until event comes down.
			while (isWaitingForEvent)
			{
				// Wait until we are in a game state.
				while (GameState.game == null)
				{
					timeoutCounter += 1.0f;
					if (timeoutCounter >= timeoutThreshold)
					{
						// Break out of test with error.
						isTestRunning = false;
						yield break;
					}
					// Keep waiting until we are loaded into game.
					yield return new WaitForSeconds(1.0f);
				}
				
				// Automate closing all dialogs that might have popped up.
				yield return RoutineRunner.instance.StartCoroutine(CommonAutomation.automateClearDialogsAndShrouds());

				// Randomly adjust the bet
				if (ZyngaAutomatedPlayer.instance.isUsingRandomWagersForSpins)
				{
					SpinPanel spinPanel = SpinPanel.instance;
					if (spinPanel != null)
					{
						yield return RoutineRunner.instance.StartCoroutine(spinPanel.automateChangeWagerForZap());
					}
				}

				// Spin the game.
				yield return RoutineRunner.instance.StartCoroutine(ActionController.doAction("Spin"));

				// Wait for the spin to finish.
				yield return RoutineRunner.instance.StartCoroutine(waitForGameToSpin());
			}
			// Once the event is over lets wait for a second for all events to get processed.
			yield return new WaitForSeconds(1.0f);
		}
#region EVENT_CALLBACKS		
		private void campaignComplete(string campaignId, List<JSON> eventData)
		{
			if (campaignId != CampaignDirector.ROBUST_CHALLENGES)
			{
				// Then this isnt relevant here. Bailing.
				return;
			}
			isWaitingForEvent = false; // Stop waiting the event.
			isTestRunning = false; // End the test.
		}

		private void missionComplete(string campaignId, List<JSON> eventData)
		{
			if (campaignId != CampaignDirector.ROBUST_CHALLENGES)
			{
				// Then this isnt relevant here. Bailing.
				return;
			}
			isWaitingForEvent = false; // Stop waiting the event.
		}

		private void challengeComplete(string campaignId, List<JSON> eventData)
		{
			if (campaignId != CampaignDirector.ROBUST_CHALLENGES)
			{
				// Then this isnt relevant here. Bailing.
				return;
			}
			isWaitingForEvent = false; // Stop waiting the event.
		}

		private void challengeReset(string campaignId, JSON eventData)
		{
			if (campaignId != CampaignDirector.ROBUST_CHALLENGES)
			{
				// Then this isnt relevant here. Bailing.
				return;
			}
			isWaitingForEvent = false;
		}
#endregion
	}
#endif
}
