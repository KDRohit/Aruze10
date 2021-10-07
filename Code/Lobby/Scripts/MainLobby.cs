using UnityEngine;
using System.Collections;
using Com.Scheduler;
using CustomLog;
using FeatureOrchestrator;
using TMPro;

/**
Controls the look and behavior of the main lobby.
*/

public abstract class MainLobby : MonoBehaviour, IResetGame
{
	public TextMeshPro inboxCountLabel;
	public GameObject earlyAccessTag;
	public ParticleSystem touchSparkle;

	public static MainLobby instance = null;
	// Pre-casted variables for other code to use for referencing things in SKU-specific lobbies.
	public static MainLobbyV3 hirV3 = null;

	private static string currentSpeicalLobbyUserflowKey = null; // used to track what special lobby the main lobby launched so we can track it correctly
	
	public static bool isFirstTime = true;					// First time showing the main lobby this play session.
	public static bool didLaunchGameSinceLastLobby = false;	// Reset to false upon every return to the lobby.
	public static bool wasUnlockAllGames = false;

	public static int pageBeforeGame = DEFAULT_LAST_GAME_PAGE;		// Remember what lobby page the player is on so we can return to it from a game.
	private static bool wasSneakPreviewActive = false;

	protected const float IPAD_ASPECT = 1.33f;

	// Sizes and spacing for the lobby UI.
	protected const int MAIN_BUTTON_SPOT_WIDTH = 468;
	protected const int MAIN_BUTTON_SPOT_HEIGHT = 338;
	
	protected const int MAIN_BUTTON_HORIZONTAL_SPACING = 132;
	protected const int MAIN_BUTTON_VERTICAL_SPACING = 57;


	public const int MAIN_BUTTON_SPOTS_PER_ROW = 4;				// Number of game buttons on the main lobby per row.
	public static int MAIN_BUTTON_SPOTS_ROWS_PER_PAGE = 2;	// Number of rows of game buttons on the main lobby per page.
	public static int MAIN_BUTTON_SPOTS_PER_PAGE = MAIN_BUTTON_SPOTS_PER_ROW * MAIN_BUTTON_SPOTS_ROWS_PER_PAGE;
	public static int VIP_BUTTON_SPOTS_PER_ROW = 5;			// Number of game buttons on the VIP lobby per row.
	public static int VIP_BUTTON_SPOTS_PER_PAGE = VIP_BUTTON_SPOTS_PER_ROW * MAIN_BUTTON_SPOTS_ROWS_PER_PAGE;

	public const float TOUCH_SPARKLE_EMISSION_RATE = 8.0f; // The emission rate for the touch sparkles.
	
	protected LobbyInfo lobbyInfo = null;
	public const int DEFAULT_LAST_GAME_PAGE = -1;

	public static bool isTransitioning { get; protected set; }		// Whether transitioning between different parts of the lobby.

	void Awake()
	{
		if (Glb.isResetting)
		{
			// pointless, we are about to reset the game, and this is a consistent entry point for errors
			return;
		}
		
		instance = this;
		// There is no need nor desire to use preprocessor conditions to set these SKU-specific instances.
		// Using "as" to cast the variables will automatically make them null if the instance isn't of the casted-to type.
		hirV3 = instance as MainLobbyV3;		// all the places that use hir need to account for this?
		
    	// Whenever we come back into the lobby check if we were previously tracking a special lobby and end that userflow
		if (!string.IsNullOrEmpty(currentSpeicalLobbyUserflowKey))
		{
			endCurrentSpecialLobbyUserflow();
		}	

		// Clear the game state, we aren't in a game, we don't report the game stack specifically. Only the current game
		// you're in has any relevance.
		GameState.clearGameStack();
		
		LobbyLoader.lastLobby = LobbyInfo.Type.MAIN;
		
		if (SelectGameUnlockDialog.gamesToDisplay == null)
		{
			SelectGameUnlockDialog.setupGameList();
		}

		lobbyInfo = LobbyInfo.find(LobbyInfo.Type.MAIN);
		if (lobbyInfo == null)
		{
			// Show generic reset dialog?
			Server.forceGameRefresh("game_reset event", "actions_error_message", false);
			Debug.LogError("No main lobby data found. Lets try to reset here");
			return;
		}

		preAwake();
		postAwake();
	}

	
	// Do stuff that has to happen first during Awake().
	protected virtual void preAwake()
	{
		NGUIExt.attachToAnchor(gameObject, NGUIExt.SceneAnchor.CENTER, transform.localPosition);
				
		DisposableObject.register(gameObject);
		
		organizeOptions();
				
		if (!Audio.isPlaying("lobbyambienceloop0"))
		{
			// Playing ambience aborts the lobby music loop,
			// so only play this if it's not already playing,
			// since the lobby music loop can start playing
			// from Data.cs right after global data is set,
			// way before the lobby is instantiated.
			Audio.play("lobbyambienceloop0");
		}
		playLobbyMusic();
		
		Overlay.instance.top.hideLobbyButton();
		Overlay.instance.topV2.onLobbyAwake();
		
		// Make sure the overlay is visible, just in case we're returning from a game and it was hidden.
		Overlay.instance.top.show(true);
		
		// This sets the initial value of the inbox count label in the lobby.
		Overlay.instance.top.updateInboxCount();

		refreshEarlyAccessTag();
	}
	
	// Do stuff that has to happen after certain other things in Awake().
	protected virtual void postAwake()
	{
		if (!isFirstTime)
		{
			// Return to the scroll position the player was on when launching a game.
			restorePreviousScrollPosition();
		}

		if (LobbyLoader.autoLaunchGameResult == LobbyGame.LaunchResult.NO_LAUNCH)
		{
			// Only show startup dialogs if no game was autolaunched.
			// This is mainly because if a game autolaunched but we
			// still ended up in the lobby here, then that means the
			// game is showing the initial bet selection dialog.
			// So we don't want to interrupt that with MOTD's and shit.
			if (isFirstTime && !ConfigManager.finishedSync)
			{
				ConfigManager.registerForSyncCompleteDelegate(onProtonSyncFinished);
			}
			else
			{
				MOTDFramework.showGlobalMOTD(isFirstTime ? MOTDFramework.SURFACE_POINT.APP_ENTRY : MOTDFramework.SURFACE_POINT.RTL);
			}
		}

		LobbyGame.checkSkuGameUnlock();
		
		if (isFirstTime)
		{
			// Moving this call to here so that this is set after the experiments have loaded.
			if (SlotsPlayer.instance != null)
			{
				wasUnlockAllGames = UnlockAllGamesFeature.instance != null && UnlockAllGamesFeature.instance.isEnabled;
			}

			if (LoLa.sneakPreviewTimeRange != null)
			{
				wasSneakPreviewActive = LoLa.sneakPreviewTimeRange.isActive;
			}
		}

		// unfortunately we show this outside of the MOTD framework...
		// TODO: update with current todolist refactor
		if (GameExperience.totalSpinCount == 0)
		{
			RobustChallengesObjectivesDialog.showDialog();
		}

		/* MCC -- After we try to launch MOTDs, do the lobby check.
		This is so that we dont mark this user as new and then trigger
		the MOTD before the second load. */
		DailyBonusForcedCollection.instance.doLobbyCheck();

		// If we are returning to the lobby, then check whether the Daily Challenge is over.
		if (Quest.activeQuest is DailyChallenge && DailyChallenge.checkExpired())
		{
			// Only do this if we don't show the dialog, becuase if we show the dialog then 
			// we dont want to mark it as seen unless we have actually shown it.
			PlayerAction.saveCustomTimestamp(DailyChallenge.LAST_SEEN_OVER_TIMESTAMP_KEY);
			DailyChallenge.lastSeenOverDialog = GameTimer.currentTime;
		}

		// MCC -- Whenever we load back into the lobby, check if the queue is stuck.
		MOTDFramework.checkIfQueueIsStuck();

		//show network achievement rewards
		NetworkAchievements.processRewards();

		didLaunchGameSinceLastLobby = false;
		isFirstTime = false; //reset

		ChallengeLobby.sideBarUI = null; //we're no longer in a challenge lobby

		NGUIExt.enableAllMouseInput();
	}

	private void onProtonSyncFinished()
	{
		//Rare edge-case where proton hasn't finished its sync with the server before lobby loads.
		//The sync has a timeout of 3 seconds so this only occurs if we're able to load into the main lobby before that completes
		//Need to wait for proton to finish or else related MOTDS might not appear.
		MOTDFramework.showGlobalMOTD(MOTDFramework.SURFACE_POINT.APP_ENTRY);
	}
	
	// Overrides should also use this base logic.
	public virtual bool shouldShowMFS
	{
		get
		{
			return
				SlotsPlayer.isSocialFriendsEnabled && // Only show the MFS automatically in the lobby if the player is connected to facebook or is in network friends.
				didLaunchGameSinceLastLobby;
		}
	}
	
	public virtual IEnumerator resolutionChangeHandler()
	{
		yield break;
	}

	public void refreshEarlyAccessTag()
	{
		if (earlyAccessTag != null)
		{
			earlyAccessTag.SetActive(
				Glb.SHOW_EARLY_ACCESS_TAG &&
				LobbyGame.vipEarlyAccessGame != null &&
				LobbyGame.vipEarlyAccessGame.keyName != PlayerPrefsCache.GetString(Prefs.LAST_SEEN_NEW_VIP_GAME, "")
			);
		}
	}

	// Return to the page the player was on when launching a game.
	protected virtual void restorePreviousScrollPosition()
	{
	}
	
	public virtual void refreshFriendsList()
	{	
	}
	
	// Play the current lobby music, if a lobby is loaded.
	public static void playLobbyMusic()
	{
		if (instance != null && !isTransitioning)
		{
			instance.playLobbyInstanceMusic();
		}
		else if (VIPLobby.instance != null && !VIPLobby.instance.isTransitioning)
		{
			VIPLobby.instance.playLobbyInstanceMusic();
		}
		else if (MaxVoltageLobbyHIR.instance != null && !MaxVoltageLobbyHIR.instance.isTransitioning)
		{
			MaxVoltageLobbyHIR.instance.playLobbyInstanceMusic();
		}
		else if (ChallengeLobby.instance != null && !ChallengeLobby.instance.isTransitioning)
		{
			ChallengeLobby.instance.playLobbyInstanceMusic();
		}		
	}

	// Play lobby music. Allows for SKU overrides.
	protected void playLobbyInstanceMusic()
	{
		if (EliteManager.isActive && EliteManager.hasActivePass && !EliteManager.showLobbyTransition)
		{
			AssetBundleManager.downloadAndCacheBundle("elite", skipMapping:true); //Manually caching the bundle here so we can skip the bundle mapping for the music
			Audio.switchMusicKeyImmediate(EliteManager.ELITE_LOBBY_MUSIC);
		}
		else if (!Audio.isPlaying("spookylandingloop"))
		{
			Audio.switchMusicKeyImmediate("spookylandingloop");
		}
	}
	
	protected virtual void Update()
	{
		// Sparkles moved to MainLobbyHIR.

		if (!isTransitioning &&
			(Dialog.instance != null && !Dialog.instance.isShowing) &&
			!DevGUI.isActive &&
			!Log.isActive
			)
		{
			 // only perform back button functionality on Lobby if no dialog is open
			AndroidUtil.checkBackButton(AndroidUtil.androidQuit);
		}

		if (wasSneakPreviewActive)
		{
			wasSneakPreviewActive = LoLa.sneakPreviewTimeRange.isActive;
			
			if (!wasSneakPreviewActive)
			{
				// If you're out of time, but for some stupid reason the experiment is on, you're now coming soon.
				// The refresh function should catch showing the actual game object.
				LobbyGame.expireSneakPreview();
				LobbyInfo.refreshAllLobbyOptionButtons();
			}
		}
	}
	
	// Callback for when the game is paused (called from PauseHandler.cs).
	public virtual void pauseHandler(bool isPaused)
	{
	}
	
	// Do some stuff to get the menu options organized for display.
	protected virtual void organizeOptions()
	{
	}
	
	public virtual int getTrackedScrollPosition()
	{
		return 0;
	}
		
	// Start a userflow for a special lobby
	public static void startSpecialLobbyUserflow(string userflowKey)
	{
		// check if we already have some other special lobby flow, in which case end that one
		// this really shouldn't happen, but this is a failsafe to ensure we start tracking
		// the new special lobby when it is launched
		if (!string.IsNullOrEmpty(currentSpeicalLobbyUserflowKey))
		{
			endCurrentSpecialLobbyUserflow();
		}

		currentSpeicalLobbyUserflowKey = userflowKey;
		Userflows.flowStart(userflowKey);
	}

	// End the userflow for a special lobby
	protected static void endSpecialLobbyUserflow(string userflowKey)
	{
		Userflows.flowEnd(userflowKey);
	}

	// Ends the current special lobby userflow and null it out
	protected static void endCurrentSpecialLobbyUserflow()
	{
		if (!string.IsNullOrEmpty(currentSpeicalLobbyUserflowKey))
		{
			endSpecialLobbyUserflow(currentSpeicalLobbyUserflowKey);
			currentSpeicalLobbyUserflowKey = null;
		}
		else
		{
			Debug.LogWarning("MainLobby.endCurrentSpecialLobbyUserflow() - Called when currentSpeicalLobbyUserflowKey was null or empty!");
		}
	}

	// The main call to go into the VIP Lobby from here.
	public virtual IEnumerator transitionToVIPLobby()
	{
		yield return null;

		startSpecialLobbyUserflow(LobbyInfo.Type.VIP.ToString() + "-lobby");

		isTransitioning = true;
		Audio.play("SelectPremiumAction");
		
		NGUIExt.disableAllMouseInput();
	}

	// The main call to go into the VIP Lobby from here.
	public virtual IEnumerator transitionToLOZLobby()
	{
		yield return null;

		startSpecialLobbyUserflow(LobbyInfo.Type.LOZ.ToString() + "-lobby");

		isTransitioning = true;
		
		NGUIExt.disableAllMouseInput();
	}

	// The main call to go into the Max Voltage Lobby from here.
	public virtual IEnumerator transitionToMaxVoltageLobby()
	{
		yield return null;

		startSpecialLobbyUserflow(LobbyInfo.Type.MAX_VOLTAGE.ToString() + "-lobby");

		isTransitioning = true;
		
		NGUIExt.disableAllMouseInput();
	}

	// The main call to go into the Max Voltage Lobby from here.
	public virtual IEnumerator transitionToChallengeLobby(string campaignName)
	{
		yield return null;

		startSpecialLobbyUserflow(campaignName + "-lobby");

		isTransitioning = true;

		NGUIExt.disableAllMouseInput();
	}

	// The main call to go into the Slotventures Lobby from here.
	public virtual IEnumerator transitionToSlotventureLobby()
	{
		yield return null;

		isTransitioning = true;

		NGUIExt.disableAllMouseInput();
	}
	
	// Used with Scheduler to refresh the lobby safely.
	// This applies to whatever lobby is current loaded, not just main.
	public static void refresh(Dict args)
	{
		Scheduler.removeFunction(MainLobby.refresh);
		Loading.show(Loading.LoadingTransactionTarget.LOBBY);
		Glb.loadLobby();
	}

	// Returns the pinned menu option on the given page at the given local option spots.
	protected LobbyOption getPinnedOption(int page, int x, int y)
	{
		foreach (LobbyOption option in lobbyInfo.pinnedOptions[page])
		{
			foreach (Vector2int spot in option.pinned.spots)
			{
				if (spot.x == x && spot.y == y)
				{
					return option;
				}
			}
		}
		return null;
	}

	public static void resetStaticClassData()
	{
		isFirstTime = true;
		didLaunchGameSinceLastLobby = false;
		wasUnlockAllGames = false;
		wasSneakPreviewActive = false;
		pageBeforeGame = DEFAULT_LAST_GAME_PAGE;

		GameObject.Destroy(hirV3);
		hirV3 = null;

		// Try to force the special lobby userflow to end, since we are going to reset and not be in it anymore
		if (!string.IsNullOrEmpty(currentSpeicalLobbyUserflowKey))
		{
			endCurrentSpecialLobbyUserflow();
		}
	}
	
	// Manually call this method before we will explicitly destroy the lobby
	// before going into a game. This is necessary instead of using OnDestroy()
	// because OnDestroy() also gets called during shutdown, and the order in which
	// things are destroyed is unpredictable, causing some gameObjects to already
	// be null, resulting in NRE's that don't seem to make sense.
	public virtual void cleanupBeforeDestroy()
	{
		//Cleanup the different lobby option decorators static instances
		//so that the bundles don't remain in memory
		JackpotLobbyOptionDecorator.cleanup();
		JackpotLobbyOptionDecorator1x2.cleanup();
		ExtraFeatureLobbyOptionDecorator.cleanup();
		GiantJackpotLobbyOptionDecorator.cleanup();
		GiantJackpotLobbyOptionDecorator1x2.cleanup();
		BigSliceLobbyOptionDecorator.cleanup();
		BigSliceLobbyOptionDecorator1x2.cleanup();
		HighLimitLobbyOptionDecorator.cleanup();
		MultiJackpotLobbyOptionDecorator1x2.cleanup();
		MysteryGiftLobbyOptionDecorator.cleanup();
		MysteryGiftLobbyOptionDecorator1x2.cleanup();
		PersonalizedContentLobbyOptionDecorator1x2.cleanup();
		RoyalRushLobbyOptionDecorator.cleanup();
		RichPassLobbyOptionDecorator.cleanup();
		RichPassLobbyOptionDecorator1x2.cleanup();
		RichPassMultiJackpotLobbyOptionDecorator1x2.cleanup();
		RecommendedLobbyOptionDecorator1x2.cleanup();
		FavoriteLobbyOptionDecorator1x2.cleanup();
		MainLobbyBottomOverlayV4.cleanup();
		LobbyCarouselV3.cleanup();
		isTransitioning = false;
		instance = null;
		hirV3 = null;
		ConfigManager.unregisterForSyncCompleteDelegate(onProtonSyncFinished);
	}
}
 
