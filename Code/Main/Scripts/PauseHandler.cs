using System.Collections;
using UnityEngine;
using Zynga.Zdk;

/*
This is a single entry point for handling all code that needs to fire when the application pauses or resumes.
Put all code and function calls here instead of creating more OnApplicationPause function in other scripts.
*/

public class PauseHandler : TICoroutineMonoBehaviour
{
	public static PauseHandler instance = null;

	private void Awake()
	{
		instance = this;
	}

	//OnApplicationPause is not called on webGL. Instead if the user tabs away from the Unity player, it will receive
	//a Focus event instead.
	private void OnApplicationFocus(bool hasFocus)
	{
		//If we don't have focus, then we are effectively "paused"
		OnApplicationPause(!hasFocus);
	}
	
	private void OnApplicationPause(bool isPaused)
	{
#if UNITY_EDITOR
		if (!isPaused)
		{
			// This has not worked at all on Android or iOS since Unity 5.1, and now crashes iOS when called.
			Application.runInBackground = true;
		}
#endif

		LobbyLoader.NotePauseOccurred();
		Userflows.notePauseOccurred();
		
		if (NotificationManager.Instance != null)
		{
			NotificationManager.Instance.pauseHandler(isPaused);
		}

		if (MainLobby.instance != null)
		{
			MainLobby.instance.pauseHandler(isPaused);
		}

		// If we're expecting a change in PN...
		if (!isPaused)
		{
			if (IncentivizedSoftPromptDialog.awaitingIncentivePNState)
			{
				IncentivizedSoftPromptDialog.onEnableFromPrompt();	
			}
			else if (SoftPromptDialog.awaitingPNState)
			{
				SoftPromptDialog.onEnableFromPrompt();
			}
		}
		
		bool wasLoggingIn = false;

		if (Loading.instance != null)
		{
			wasLoggingIn = Loading.instance.isLoggingIn;  // Must be stored before calling the Loading script's pauseHandler.
			Loading.instance.pauseHandler(isPaused);
		}

		if (StatsManager.Instance != null)
		{
			// Flush calls first
			StatsManager.Instance.pauseHandler(isPaused);

			if (!isPaused && !wasLoggingIn)
			{
				// Only if we are resuming the app d we log a visit here,
				// when in the logging in process, we let GameLoader call this.
				StatsManager.Instance.LogVisit();
			}
		}

		if (UAWrapper.Instance != null)
		{
			UAWrapper.Instance.PauseHandler(isPaused);
		}

		// Update analytics package
		AnalyticsManager.onAppPausedOrResumed(isPaused);

		if (isPaused)
		{
			// force flush of any pending/batched splunk events
			Server.handlePendingSplunkEvents(true);
		}

		if (isPaused)
		{
			Userflows.flowStart("game_paused");
		}
		else
		{
			Userflows.flowEnd("game_paused");
		}

		if (!isPaused)
		{
			if (SlotsPlayer.isLoggedIn && !wasLoggingIn && System.DateTime.Now.Subtract(SlotsPlayer.loginTime).TotalHours >= Glb.MOBILE_RESET_HOURS)
			{
				// If unpausing the game after the time limit for a session length,
				// force a reset to refresh login data, including STUD and experiments.
				// Only reset the game if a player is already logged in and not in the process of logging in.
				Glb.resetGame("Unpausing after timeout time has expired.");
				return;
			}
			else
			{
				// If we're on a special list of devices that can't be trusted to restore properly, reset the game:
				var deviceModel = SystemInfo.deviceModel;
				//Debug.LogWarning("## OnApplicationPause - DeviceName: " + deviceModel + " ; Reload List: " + Glb.devicesForceReloadOnResume);

				foreach (string token in Glb.devicesForceReloadOnResume)
				{
					//Debug.LogWarning("## Checking for device: " + token);
					if (!string.IsNullOrEmpty(token) && token == deviceModel)
					{
						// We're on the reset list:
						if (wasLoggingIn)
						{
							// The normal resetGame() code interferes with the login flow when first going through Facebook.
							// We can't have this, going to another way to help solve the problem now.
							RoutineRunner.instance.StartCoroutine(cameraRefresher());
						}
						else
						{
							// Wasn't in the process of logging in, so just reset.
							Glb.resetGame("Unpausing a device on the force reload list.");
						}
						return;
					}
				}
			}
		}

		if (!isPaused)
		{
			LobbyGame.checkSkuGameUnlock();
		}

		if (URLStartupManager.Instance != null)
		{
			// We want this after the reset game check, so that we wont swallow the link by resetting before we call the server and hear a response.
			URLStartupManager.Instance.pauseHandler(isPaused);
		}

		if (RoyalRushTooltipController.instance != null && !isPaused)
		{
			RoyalRushTooltipController.instance.onUnpause();
		}
	}

	public static IEnumerator cameraRefresher()
	{
		ShroudScript shroud = Dialog.instance.shroud;
		yield return null;
		if (PlayerPrefsCache.GetInt(Prefs.UPGRADE_FROM_SOCIAL_SCREEN, 0) == 1)
		{
			yield return new WaitForSeconds(0.5f);
		}
		/*
		 * Flicker the loading screen textures for blacklisted devices!
		 * The problem: On Unity 4.3.x, for some specific devices, after coming out of pause, texture
		 * data alternates between being nulled out and being correct until a scene load or a sprite is
		 * drawn over those textures.  We are flickering a shroud to do this.
		 */
		if (Loading.isLoading)
		{
			shroud = Loading.instance.shroud;
			shroud.gameObject.SetActive(true);
		}
		iTween.ValueTo(shroud.gameObject, iTween.Hash("from", shroud.sprite.alpha, "to", Dialog.SHROUD_FADE_ALPHA, "time", .125f, "onupdate", "updateFade"));
		yield return new WaitForSeconds(0.125f);
		iTween.ValueTo(shroud.gameObject, iTween.Hash("from", shroud.sprite.alpha, "to", 0, "time", .125f, "onupdate", "updateFade"));

		//always make sure the loading shroud is inactive at the end, even if not loading, just in case it was somehow enabled!
		Loading.instance.shroud.gameObject.SetActive(false);
		yield return null;
	}

#if UNITY_EDITOR
	public static void simPause(bool isPaused)
	{
		if (isPaused)
		{
			// In order to simulate the proper pause behaviour we would have on device, we are telling Unity to pause
			// in the background, and then opening up a browser window (to google.com since it is pretty simple and stable)
			Application.runInBackground = false;
			Application.OpenURL("https://www.google.com");
		}
	}
#endif

	// Might as well handle quitting here too.
	private void OnApplicationQuit()
	{
		Userflows.flowEnd("run_time");
		AnalyticsManager.Instance.OnAppShutdown();
		Glb.isQuitting = true;
	}
}
