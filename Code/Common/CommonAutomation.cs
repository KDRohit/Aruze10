using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;

#if ZYNGA_TRAMP || UNITY_EDITOR

/**
This is a purely static class of generic useful functions that relate to automation.
*/
public static class CommonAutomation
{
	private const int MAX_BUTTON_COLLIDER_FIND_ATTEMPTS = 3;
	private const float BUTTON_COLLIDER_REFIND_ATTEMPT_TIME = 5.0f;

	// Selects the routine that should run automation coroutines.
	// If TRAMP is active, uses TRAMP to run any automation coroutines.
	// Otherwise, default to RoutineRunner.
	public static TICoroutineMonoBehaviour routineRunnerBehaviour
	{
		get
		{
#if ZYNGA_TRAMP
			if (AutomatedPlayer.instance != null)
			{
				return AutomatedPlayer.instance;
			}
#endif
			return RoutineRunner.instance;
		}
	}

	// Eventually this should call something that will automate a dialog, but for not just randomly click until we close it.
	public static IEnumerator automateOpenDialog()
	{
		DialogBase lastAttemptedDialog = null;
		while (IsDialogActive())
		{
			// Wait for the dialog to not be transitioning, otherwise we could accidentally close
			// a dialog before it is done opening, or attempt to close a dialog that is already
			// closing.
			if (Dialog.isTransitioning)
			{
				yield return null;
			}
			else
			{
				if (lastAttemptedDialog == Dialog.instance.currentDialog)
				{
					Debug.LogWarning("ZAP could not automatically clear dialog " + lastAttemptedDialog.gameObject.name + ". Forcing close.");
					Dialog.close();

					yield return null;
				}
				else
				{
					lastAttemptedDialog = Dialog.instance.currentDialog;
					yield return routineRunnerBehaviour.StartCoroutine(Dialog.instance.currentDialog.automate());
				}
			}
		}
	}

	public static IEnumerator automateClearDialogs()
	{
		do
		{
			if (Glb.isResetting)
			{
				// If we are resetting then kill the routine.
				yield break;
			}

			// If there is an Open Dialog, clear it
			yield return routineRunnerBehaviour.StartCoroutine(automateOpenDialog());

			// Give next dialog a chance to start
			yield return null;
		}
		while (IsDialogActive());
	}

	public static bool IsDialogActive()
	{
		return Dialog.instance != null && Dialog.instance.isShowing && Dialog.instance.currentDialog;
	}

	public static IEnumerator automateCloseShrouds()
	{
		while (IsShroudActive())
		{
			if (Glb.isResetting)
			{
				// If we are resetting then kill the routine.
				yield break;
			}
			// If there is an Open Shroud/Tooltip, clear it
			Overlay.instance.hideShroud();

			// Give next Shroud/Tooltip a chance to start
			yield return null;
		}
	}

	public static bool IsShroudActive()
	{
		return Overlay.instance != null && Overlay.instance.shroud != null && Overlay.instance.shroud.activeSelf;
	}

	// TODO Add visualizer to this coroutine.
	public static IEnumerator automateClearDialogsAndShrouds()
	{
		while (IsShroudActive() && !Glb.isResetting)
		{
			// TODO Add visualizer to this coroutine.
			yield return routineRunnerBehaviour.StartCoroutine(automateCloseShrouds());
		}

		while (IsDialogActive() && !Glb.isResetting)
		{
			// TODO Add visualizer to this coroutine.
			yield return routineRunnerBehaviour.StartCoroutine(automateClearDialogs());
		}
	}

	public static IEnumerator waitForDialog(string dialogKey)
	{
		bool dialogFound = false;
		Scheduler.run();
		while (IsDialogActive() || Scheduler.hasTask)
		{
			if (IsDialogActive())
			{
				if (Dialog.instance.currentDialog.type.keyName == dialogKey)
				{
					dialogFound = true;
					yield return routineRunnerBehaviour.StartCoroutine(Dialog.instance.currentDialog.automate());
					yield break;
				}
				else
				{
					yield return routineRunnerBehaviour.StartCoroutine(automateOpenDialog());
				}
			}
			yield return null;
		}
		if (!dialogFound)
		{
			Debug.LogError(string.Format("Waiting for dialog {0} but it never showed up", dialogKey));
		}
	}

	public static void loadLastGame()
	{
		LobbyGame game = LobbyGame.find(PlayerPrefsCache.GetString(Prefs.LAST_SLOT_GAME, ""));

		if (game != null)
		{
			GameState.pushGame(game);
			Loading.show(Loading.LoadingTransactionTarget.GAME);
			Glb.loadGame();
		}
	}

	// Attempt to randomly click colliders in the passed in parent object
	// Can use isLoggingNoButtonMsg to control if errors should be logged when
	// nothing to press is found.  This can be a valid case for some testing
	// where for instance Freespins or Bonus Games are constantly polling to see
	// if there is something ZAP can click on.
	public static IEnumerator clickRandomColliderIn(GameObject parent, Camera buttonCamera = null, bool isLoggingNothingToClickErrorMsg = true)
	{
		for (int i = 0; i < MAX_BUTTON_COLLIDER_FIND_ATTEMPTS; ++i)
		{
			if (parent == null)
			{
				// it is possible for the parent to get destroyed while trying to click on it, so just make sure
				// it is still around before doing stuff with it.
				yield break;
			}

			Collider button = getRandomColliderIn(parent, buttonCamera);

			if (button != null)
			{
				// This needs to be on the Routine runner because if this script gets disabled we still want finish simulating the click.
				yield return routineRunnerBehaviour.StartCoroutine(Input.simulateMouseClickOn(button, 0, buttonCamera));
				yield break;
			}

			// sometimes the buttons will be off screen and we need to wait for them to animate in before clicking them.
			if (i < MAX_BUTTON_COLLIDER_FIND_ATTEMPTS - 1)
			{
				yield return new WaitForSeconds(BUTTON_COLLIDER_REFIND_ATTEMPT_TIME);
			}
		}

		if (parent == null)
		{
			// it is possible for the parent to get destroyed while trying to click on it, so just make sure
			// it is still around before doing stuff with it.
			yield break;
		}

		if (isLoggingNothingToClickErrorMsg)
		{
			Debug.LogErrorFormat("{0} : Could not find a button to click after trying {1} times in {2} seconds",
				parent.name,
				MAX_BUTTON_COLLIDER_FIND_ATTEMPTS,
				BUTTON_COLLIDER_REFIND_ATTEMPT_TIME * MAX_BUTTON_COLLIDER_FIND_ATTEMPTS);
		}
	}

	public static IEnumerator holdRandomColliderIn(GameObject parent, float time = 1.0f, Camera buttonCamera = null)
	{
		Collider button = getRandomColliderIn(parent, buttonCamera);
		if (button != null)
		{
			// This needs to be on the Routine runner because if this script gets disable we still want finish simulating the click.
			yield return routineRunnerBehaviour.StartCoroutine(Input.simulateMouseClickOn(button, time, buttonCamera));
		}
		else
		{
			// No buttons were found.
		}
	}

	public static Collider getRandomColliderIn(GameObject parent, Camera cameraForButton = null)
	{
		if (parent != null)
		{
			Collider[] colliders = parent.GetComponentsInChildren<Collider>();
			if (colliders != null && colliders.Length > 0)
			{
				Collider collider = colliders[Random.Range(0, colliders.Length)];

				if (collider != null && collider.enabled && collider.gameObject.layer != (int)Layers.LayerID.ID_HIDDEN)
				{
					RaycastHit hitInfo;
					if (cameraForButton == null)
					{
						cameraForButton = NGUIExt.getObjectCamera(collider.gameObject);
					}

					if (cameraForButton != null)
					{
						// take into account that the collider may not be centered on the object it is attached to
						Vector3 center;
						Vector3 direction;
						Vector3 adjustedCameraPosition = new Vector3(
							collider.gameObject.transform.position.x,
							collider.gameObject.transform.position.y,
							cameraForButton.transform.position.z);
						if (CommonGameObject.getColliderWorldCenter(collider, out center))
						{
							direction = center - adjustedCameraPosition;
						}
						else
						{
							direction = collider.transform.position - adjustedCameraPosition;
						}

						Debug.DrawRay(adjustedCameraPosition, direction);
						if (Physics.Raycast(adjustedCameraPosition, direction, out hitInfo))
						{
							if (hitInfo.collider == collider)
							{
								return collider;
							}
						}
					}
				}
			}
		}
		else
		{
			Debug.LogError("Trying to get a random button in a null parent.");
		}
		return null;
	}

	public static Collider getSpinButton()
	{
		if (ReelGame.activeGame != null && ReelGame.activeGame is SlotBaseGame)
		{
			SpinPanel spinPanel = SpinPanel.instance;
			if (spinPanel != null)
			{
				UIImageButton spinButtonImage = spinPanel.spinButton;
				if (spinButtonImage != null)
				{
					Collider spinButton = getRandomColliderIn(spinButtonImage.gameObject);
					if (spinButton != null)
					{
						return spinButton;
					}
				}
				else
				{
					Debug.LogError(ReelGame.activeGame.name + "> No spin button set in spin panel.");
				}
			}
			else
			{
				Debug.LogError(ReelGame.activeGame.name + "> There is no spin pannel for this game.");
			}
		}
		else
		{
			Debug.LogError("getSpinButton> There is not an active SlotBaseGame.");
		}
		return null;
	}

	public static IEnumerator addCoinsAndWait(long amount, float waitTime = 5.0f)
	{
#if ZYNGA_PRODUCTION
		Debug.LogErrorFormat("CommonAutomation.cs -- addCoinsAndWait() -- Cannot force add coins to a player on production.");
		yield break;
#else
		PlayerAction.addCredits(amount);
		SlotsPlayer.addCredits(amount, "ZAP");
		// Wait for the desired amount of time.
		yield return new WaitForSeconds(waitTime);
#endif
	}

	public static IEnumerator unlockGameAndWait(string gameKey, float waitTime = 5.0f)
	{
#if ZYNGA_PRODUCTION
		Debug.LogErrorFormat("CommonAutomation.cs -- unlockGameAndWait() -- Cannot force unlock a game for a player on production.");
		yield break;
#else
		// If the game isn't unlocked, unlock it!
		PlayerAction.devGameUnlock(gameKey);
		
		// Wait for the desired amount of time.
		// NOTE: Because we are waiting for the client to send and the server to receive
		// the unlock request we need to make sure that this isn't affected by Unity's timeScale
		// in case the game is sped up we still want to wait a set amount of time to make sure
		// the game unlocked
		if (waitTime > 0.0f)
		{
			float elapsedTime = 0.0f;
			while (elapsedTime < waitTime)
			{
				yield return null;
				elapsedTime += Time.unscaledDeltaTime;
			}
		}
#endif
	}	
}
#endif  // ZYNGA_TRAMP || UNITY_EDITOR
