using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Base class for all VIP lobbies for all SKU's.
*/

public abstract class VIPLobby : MonoBehaviour, IResetGame
{
	public TextMeshPro vipJackpotTickerLabel;
	public VIPNewIcon[] vipNewIcons;
	public UIMeterNGUI vipProgressMeter;
	public TextMeshPro vipProgressPercentLabel;
	public GameObject optionButtonPrefab;
	public ParticleSystem touchSparkle;

	public bool isTransitioning { get; protected set; }	// Whether transitioning between different parts of the lobby.
	protected List<LobbyOption> options = new List<LobbyOption>();
	protected LobbyOption earlyAccessOption = null;

	public static VIPLobby instance = null;

	public static int highlightGameLevel = -1;	// If set when going to the VIP room, make sure this VIP level's game is not scrolled offscreen by default.

	protected static bool isFirstTime = true; 	// First time showing the VIP lobby to the player this play session.
	protected static int pageBeforeGame = -1;		// Remember what lobby page the player is on so we can return to it from a game.

	// Due to linked VIP there's another set of icons sitting on the prefab now. We store them all in an array and modify the ones we need
	// depending on this offset.
	protected static int vipIconIndexOffset = 0;

	protected virtual void Awake()
	{
		instance = this;
		
		NGUIExt.attachToAnchor(gameObject, NGUIExt.SceneAnchor.CENTER, transform.localPosition);

		LobbyLoader.lastLobby = LobbyInfo.Type.VIP;

		DisposableObject.register(gameObject);

		refreshUI();

		organizeOptions();

		Overlay.instance.top.hideLobbyButton();
		
		if (isFirstTime)
		{
			// If this IS the first VIP room load, then show the vip motds.
			MOTDFramework.showGlobalMOTD(MOTDFramework.SURFACE_POINT.VIP);
		}
		
		isFirstTime = false;
		// If the VIP welcome dialog hasn't been shown yet, do it now.
		VIPLevelUpDialog.showWelcomeIfNecessary();
						
		StatsManager.Instance.LogCount("bottom_nav", "vip_room", "", "", "", "click");
		Bugsnag.LeaveBreadcrumb("Entering the high limit room");

		if (LobbyGame.vipEarlyAccessGame != null)
		{
			StatsManager.Instance.LogCount("lobby", "vip_room", "view", "early_access");
		}
		
		if (LobbyGame.vipEarlyAccessGame != null)
		{
			PlayerPrefsCache.SetString(Prefs.LAST_SEEN_NEW_VIP_GAME, LobbyGame.vipEarlyAccessGame.keyName);
		}
		
		if (ProgressiveJackpot.vipJackpot != null && !ExperimentWrapper.VIPLobbyRevamp.isInExperiment)
		{
			ProgressiveJackpot.vipJackpot.registerLabel(vipJackpotTickerLabel);
		}
		else
		{
			Debug.LogWarning("No VIP progressive found.");
		}

		NetworkAchievements.processRewards();
				
		MainLobby.playLobbyMusic();	// Handles VIP lobby music too.

		NGUIExt.enableAllMouseInput();
	}

	void Update()
	{
		// Every Touch on the users screen should be making sparkles
		if (TouchInput.isTouchDown)
		{
			CommonEffects.setEmissionRate(touchSparkle, MainLobby.TOUCH_SPARKLE_EMISSION_RATE);
			Vector3 p = Camera.main.ScreenToWorldPoint(new Vector3(TouchInput.position.x, TouchInput.position.y, touchSparkle.transform.position.z));
			touchSparkle.transform.position = p;
		}
		else
		{
			CommonEffects.setEmissionRate(touchSparkle, 0);
		}
				
		if (!isTransitioning &&
			(Dialog.instance != null && !Dialog.instance.isShowing) &&
		    !DevGUI.isActive &&
			!CustomLog.Log.isActive) // only perform back button functionality on Lobby if no dialog is open
		{
			AndroidUtil.checkBackButton(backClicked);
		}
	}

	// Do some stuff to get the menu options organized for display.
	protected virtual void organizeOptions()
	{
		LobbyInfo lobbyInfo = LobbyInfo.find(LobbyInfo.Type.VIP);
		if (ExperimentWrapper.VIPLobbyRevamp.isInExperiment)
		{
			lobbyInfo = LobbyInfo.find(LobbyInfo.Type.VIP_REVAMP);
		}
		
		if (lobbyInfo == null)
		{
			Debug.LogError("No VIP lobby data found.");
			return;
		}
		
		options = new List<LobbyOption>(lobbyInfo.allLobbyOptions);
		earlyAccessOption = null;
		
		// Find the early access option.
		foreach (LobbyOption option in options)
		{
			if (option.game != null && option.game == LobbyGame.vipEarlyAccessGame)
			{
				earlyAccessOption = option;
				break;
			}
			else if (option.type == LobbyOption.Type.VIP_EARLY_ACCESS_COMING_SOON)
			{
				// No game, but this is the "Coming Soon" option.
				earlyAccessOption = option;
				break;
			}
		}

		if (earlyAccessOption == null && lobbyInfo.type != LobbyInfo.Type.VIP_REVAMP)
		{
			// This should never happen as long as we plan on showing a COMING SOON option for no game.
			Debug.LogError("Could not find a VIP early access lobby option.");
		}
		else
		{
			// Remove the early access game from the page scroller list,
			// since it gets its own special position outside the scroller.
			options.Remove(earlyAccessOption);
		}
		
		// Sort them by VIP level.
		options.Sort(LobbyOption.sortVIPOptions);
	}
	
	// Play lobby music. Allows for SKU overrides.
	public virtual void playLobbyInstanceMusic()
	{
		Debug.LogWarning("You should play some VIP lobby music in your override");
	}
	
	// NGUI button callback.
	public void backClicked()
	{
		if (isTransitioning)
		{
			return;
		}
		StartCoroutine(transitionToMainLobby());
	}
	
	public virtual IEnumerator transitionToMainLobby()
	{
		yield return null;

		if (Overlay.instance.jackpotMystery != null && Overlay.instance.jackpotMystery.tokenBar != null)
		{
			//Clean up the VIP Bar before heading back to the main lobby
			Destroy(Overlay.instance.jackpotMystery.tokenBar.gameObject);
		}

		Audio.play("minimenuclose0");
		Bugsnag.LeaveBreadcrumb("Exiting the VIP room");
		isTransitioning = true;
		NGUIExt.disableAllMouseInput();
	}

	// Any time the player's VIP points changes while in the VIP room, we need to refresh some of the UI immediately.
	public virtual VIPLevel refreshUI()
	{
		VIPLevel currentLevel = VIPLevel.find(SlotsPlayer.instance.vipNewLevel);
					
		vipNewIcons[0 + vipIconIndexOffset].setLevel(currentLevel);
		if (currentLevel.levelNumber >= VIPLevel.maxLevel.levelNumber)
		{
			// If already at the max level, then only show the current level card, centered.
			// Hide the next elements.
			vipNewIcons[1 + vipIconIndexOffset].transform.parent.parent.gameObject.SetActive(false);
			
			// Center the current card.
			CommonTransform.setX(vipNewIcons[0 + vipIconIndexOffset].transform.parent, 0);
		}
		else
		{
			setProgressMeter(currentLevel);
		}
		
		return currentLevel;
	}	

	protected virtual void setProgressMeter(VIPLevel currentLevel)
	{
		// Always get the values as though they were the next level
		VIPLevel nextLevel = VIPLevel.find(currentLevel.levelNumber + 1); 

		vipProgressMeter.maximumValue = nextLevel.vipPointsRequired - currentLevel.vipPointsRequired;
		vipProgressMeter.currentValue = SlotsPlayer.instance.vipPoints - currentLevel.vipPointsRequired;
						
		int percent = Mathf.FloorToInt(100.0f * vipProgressMeter.currentValue / vipProgressMeter.maximumValue);

		vipProgressPercentLabel.text = Localize.text("{0}_percent", CommonText.formatNumber(percent));

		// If the VIP ststus boost is enabled, we show them the boost card.
		int nextLevelModifier = VIPStatusBoostEvent.isEnabled() ? VIPStatusBoostEvent.fakeLevel : 1;

		vipNewIcons[1 + vipIconIndexOffset].setLevel(currentLevel.levelNumber + nextLevelModifier);
	}

	// NGUI button callback.
	protected virtual void benefitsClicked()
	{
		// Launch the VIP dialog and go straight to the benefits page of it.
		// Each sku most override this to launch the appropriate dialog.
	}

	public virtual int getTrackedScrollPosition()
	{
		return 0;
	}

	public static void resetStaticClassData()
	{
		highlightGameLevel = -1;
		isFirstTime = true;
		pageBeforeGame = -1;
	}
}
