using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Runtime.Serialization;

namespace Zap.Automation
{
#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	[Serializable]
	public class DailyBonusTest : FeatureTest
	{
		private const string RETURN_TO_LOBBY_ACTION = "Back";
		private const string LOBBY_OPEN_DAILY_BONUS_ACTION = "LobbyDailyBonusOpen";
		private const string OPEN_DAILY_BONUS_BUTTON_ACTION = "DailyBonusDialogCollectButton";
		private const float OPEN_DIALOG_WAIT_TIME = 7.0f; // Need to wait for animation to complete.

		public DailyBonusTest() {}
		public DailyBonusTest(SerializationInfo info, StreamingContext context)
		{
			base.DeserializeBaseData(info, context);
		}

		public override void init()
		{
			base.init();
		}

		public override IEnumerator doTest()
		{
			// Make sure this is off when we are testing, we will handle closing dialogs manually.
			ZyngaAutomatedPlayer.instance.shouldClearDialogs = false;
			while (Loading.isLoading)
			{
				yield return null;
			}

			// Wait for a neutral lobby.
			yield return RoutineRunner.instance.StartCoroutine(waitForNeutralLobby());

			// Only perform this test if the daily bonus is active.
			if (SlotsPlayer.instance != null &&
				SlotsPlayer.instance.dailyBonusTimer != null &&
				SlotsPlayer.instance.dailyBonusTimer.isExpired)
			{
				if (!GameState.isMainLobby)
				{
					// If we aren't in the main lobby, then hit the return to lobby button.
					yield return RoutineRunner.instance.StartCoroutine(ActionController.doAction(RETURN_TO_LOBBY_ACTION));
				}

				// Now that we are back in the lobby, dialogs could have opened again, so clear.
				yield return RoutineRunner.instance.StartCoroutine(waitForNeutralLobby());

				// Hit the lobby button to show the daily bonus dialog.
				yield return RoutineRunner.instance.StartCoroutine(ActionController.doAction(LOBBY_OPEN_DAILY_BONUS_ACTION));
				// Wait for it to open.
				yield return new WaitForSeconds(OPEN_DIALOG_WAIT_TIME);

				// Now click on the collect button.
				yield return RoutineRunner.instance.StartCoroutine(ActionController.doAction(OPEN_DAILY_BONUS_BUTTON_ACTION));
			}
			else
			{
				Debug.LogException(new System.Exception("Daily Bonus cannot be tested because it is not active."));
			}
			testIsFinished();
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
		}
	}
#endif
}
