using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Zynga.Core.Util;

public class SlotventuresLobby : ChallengeLobby
{
	public static GameObject toasterPrefabEnded;
	public static GameObject toasterPrefabEnding;
	public static GameObject characterInfoPrefab; // The character prefab that goes at the start
	public static GameObject coinExplosionAnimation;
	public static GameObject coinAttractObject;

	public const string CREDIT_SOURCE = "sventuresMissionComplete";
	public const string COMMON_BUNDLE_NAME = "slotventures_common";
	public const string COMMON_BUNDLE_NAME_SOUNDS = "slotventures_common_audio";
	
	public static string THEMED_BUNDLE_NAME 
	{
		get 
		{
			if (string.IsNullOrEmpty(assetData.themeName))
			{
				PreferencesBase prefs = SlotsPlayer.getPreferences();
				if (Data.debugMode && !string.IsNullOrEmpty(prefs.GetString(DebugPrefs.SLOTVENTURE_THEME_OVERRIDE, "")))
				{
					assetData.themeName = prefs.GetString(DebugPrefs.SLOTVENTURE_THEME_OVERRIDE, "");
				}
				else if (ExperimentWrapper.Slotventures.isEUE)
				{
					assetData.themeName = Data.liveData.getString("EUE_SLOTVENTURES_THEME", "").ToLower();
				}
				else
				{
					assetData.themeName = Data.liveData.getString("SLOTVENTURES_THEME", "").ToLower();
				}

			}
			return string.Format("slotventures_{0}", assetData.themeName);
		}
	}

	public static bool isBeingLazilyLoaded
	{
		get { return (AssetBundleManager.shouldLazyLoadBundle(SlotventuresLobby.COMMON_BUNDLE_NAME) 
				|| AssetBundleManager.shouldLazyLoadBundle(SlotventuresLobby.THEMED_BUNDLE_NAME)
				|| AssetBundleManager.shouldLazyLoadBundle(SlotventuresLobby.COMMON_BUNDLE_NAME_SOUNDS)); }
	}

	new public static LobbyAssetData assetData = null;

	public SlideController sliderControl;
	public GameObject sliderParent;
	public SlotventuresProgressPanel progressPanel;
	public TextMeshPro statusText;
	public GameObject toasterAnchor; // Not really a toaster a prefab comes in from the bottom
	public SlotventuresObjectiveList goalsPanel;

	public Renderer superForeground;
	public Renderer foreground;
	public Renderer midground;
	public Renderer background;

	[SerializeField] private GameObject euePopup;
	[SerializeField] private ButtonHandler euePopupMainButton;
	[SerializeField] private ButtonHandler euePopupContinueButton;
	[SerializeField] private UIAnchor[] objectAnchors;
	private bool isDisplayingEUE;

	// Non linked publics
	public GameObject attractor = null;
	public GameObject explosion = null;

	[System.NonSerialized] public bool waitingForCardPackToFinish = false;
	[System.NonSerialized] public bool isPlayingRewardsSequence = false; //Block the lobby option buttons while animating

	public int xSpacing = 675;
	public int ySpacingOdd = -40;
	public int ySpacingEven = -190;
	private int xSpacingOffset = 10;

	// Controlled and loaded through the standard challenge lobby flow. 
	public const string IN_GAME_UI_PATH = "Features/Slotventures/Common/Prefabs/SlotVentures Goals Panel";
	public const string LOBBY_PREFAB_PATH = "Features/Slotventures/Common/Prefabs/Slotventures Lobby Panel";
	public const string COIN_EXPLOSION_ANIMATION_PATH = "Features/Slotventures/Common/Prefabs/Small Coin Explosion";
	public const string COIN_ATTRACT_ANIMATION_PATH = "Features/Slotventures/Common/Prefabs/Coin Particle Animation";
	public const string TOASTER_PATH_OVER = "Features/Slotventures/Common/Prefabs/SlotVentures Conclusion Panel";
	public const string TOASTER_PATH_ENDING_SOON = "Features/Slotventures/Common/Prefabs/SlotVentures Toaster Event Ending";
	public const string OPTION_PREFAB_PORTAL_PATH = "Features/Slotventures/Slotventures Main Lobby/SlotVentures Sizer";
	public const string MAIN_LOBBY_OPTION_PATH = "Features/Slotventures/Slotventures Main Lobby/SlotVentures Main Option";

	public const string OPTION_PREFAB_PATH = "Features/Slotventures/{0}/Prefabs/SlotVentures Lobby Option";
	public const string JACKPOT_PREFAB = "Features/Slotventures/{0}/Prefabs/SlotVentures Lobby Option Jackpot";
	public static string FIRST_OPTION_PATH = "Features/Slotventures/{0}/Prefabs/Theme Character Intro";

	private string PARALLAX_SUPERFOREGROUND = "Features/Slotventures/{0}/Textures/Parallax Superforeground";
	private string PARALLAX_FOREGROUND = "Features/Slotventures/{0}/Textures/Parallax Foreground";
	private string PARALLAX_MIDGROUND = "Features/Slotventures/{0}/Textures/Parallax Midground";
	private string PARALLAX_BACKGROUND = "Features/Slotventures/{0}/Textures/Parallax Background";

	private const int ANIMATION_STEP_DELAY = 2;
	private const int ANIMATION_SPEED = 40;

	private static int lastKnownIndex = -1;
	private int imagesLoaded = 0;
	private int prefabsLoaded = 0;
	private int prefabsNeededToLoadCount = 0;
	private static GameTimerRange eventEndingTimer;
	private ChallengeLobbyCampaign slotventureCampaign = null;
	private List<LobbyOptionButtonSlotventure> slotventuresOptions = null;
	private SlotventuresJackpotButton jackpotOption = null;
	private GameObject introPrefabHandle = null;
	private bool needsToAddCredits = false;

	/*=========================================================================================
	STARTUP
	=========================================================================================*/
	void Awake()
	{
		instance = this;
		setLobbyData();
		slotventureCampaign = CampaignDirector.find(SlotventuresChallengeCampaign.CAMPAIGN_ID) as ChallengeLobbyCampaign;
		progressPanel.init(slotventureCampaign);

		// Clear the game state, we aren't in a game.
		GameState.clearGameStack();
		
		// load all the renderer stuff
		PARALLAX_SUPERFOREGROUND = string.Format(PARALLAX_SUPERFOREGROUND, assetData.themeName);
		PARALLAX_FOREGROUND = string.Format(PARALLAX_FOREGROUND, assetData.themeName);
		PARALLAX_MIDGROUND = string.Format(PARALLAX_MIDGROUND, assetData.themeName);
		PARALLAX_BACKGROUND = string.Format(PARALLAX_BACKGROUND, assetData.themeName);

		//These all get stored into static fields after a successful load so we only need to go through it once
		//TODO: Update places where these are instantiated from to not rely on static prefabs being available
		List<string> prefabsToLoad = new List<string>();
		if (toasterPrefabEnded == null)
		{
			prefabsToLoad.Add(TOASTER_PATH_OVER);
		}

		if (toasterPrefabEnding == null)
		{
			prefabsToLoad.Add(TOASTER_PATH_ENDING_SOON);
		}

		if (characterInfoPrefab == null)
		{
			prefabsToLoad.Add(FIRST_OPTION_PATH);
		}

		if (coinAttractObject == null)
		{
			prefabsToLoad.Add(COIN_ATTRACT_ANIMATION_PATH);
		}

		if (coinExplosionAnimation == null)
		{
			prefabsToLoad.Add(COIN_EXPLOSION_ANIMATION_PATH);
		}

		if (assetData.optionPrefab == null)
		{
			prefabsToLoad.Add(assetData.optionPrefabPath);
		}

		if (assetData.jackpotPrefab == null)
		{
			prefabsToLoad.Add(assetData.jackpotPrefabPath);
		}

		if (assetData.sideBarPrefab == null)
		{
			prefabsToLoad.Add(assetData.sideBarPrefabPath);
		}

		prefabsNeededToLoadCount = prefabsToLoad.Count;

		foreach (string prefabPath in prefabsToLoad)
		{
			AssetBundleManager.load(this,prefabPath, bundleLoadSuccess, bundleLoadFailure);
		}
		
		AssetBundleManager.load(this,string.Format(PARALLAX_SUPERFOREGROUND, assetData.themeName), imageLoadSuccess, bundleLoadFailure);
		AssetBundleManager.load(this,string.Format(PARALLAX_FOREGROUND, assetData.themeName), imageLoadSuccess, bundleLoadFailure);
		AssetBundleManager.load(this,string.Format(PARALLAX_MIDGROUND, assetData.themeName), imageLoadSuccess, bundleLoadFailure);
		AssetBundleManager.load(this,string.Format(PARALLAX_BACKGROUND, assetData.themeName), imageLoadSuccess, bundleLoadFailure);

	}

	private void Start()
	{
		if (slotventureCampaign.currentEventIndex >= slotventureCampaign.missions.Count - 1)
		{
			statusText.text = Localize.text("win_slotventures_jackpot", Localize.text(assetData.themeName.ToLower()));
		}
		else
		{
			slotventureCampaign.timerRange.registerLabel(statusText);
		}
	}

	private void finishLoading()
	{
		NGUIExt.attachToAnchor(gameObject, NGUIExt.SceneAnchor.CENTER, transform.localPosition);
		for (int i = 0; i < objectAnchors.Length; i++)
		{
			objectAnchors[i].uiCamera = NGUIExt.getObjectCamera(gameObject);
		}
		DisposableObject.register(gameObject);

		organizeOptions(slotventureCampaign);
		Overlay.instance.top.hideLobbyButton();
		MainLobby.playLobbyMusic();
		NGUIExt.enableAllMouseInput();
		StartCoroutine(finishTransition());

		if (lastKnownIndex == -1)
		{
			lastKnownIndex = slotventureCampaign.currentEventIndex;
		}
	}

	private IEnumerator reloadSlotventureLobby()
	{
		//hide with face to black
		yield return RoutineRunner.instance.StartCoroutine(BlackFaderScript.instance.fadeTo(1.0f));
		yield return new WaitForSeconds(0.5f);

		//remove current lobby
		cleanupBeforeDestroy();
		Destroy(gameObject);

		//reset static data and recreate
		resetStaticClassData();
		yield return null; //wait a frame for the OnDestroy function to fire before creating the new instance
		LobbyLoader.instance.createSlotventureLobby();
	}

	public void restart()
	{
		RoutineRunner.instance.StartCoroutine(reloadSlotventureLobby());
	}

	public IEnumerator finishTransition(bool playMusic = true)
	{
		if (playMusic)
		{
			Audio.switchMusicKeyImmediate(SlotventuresLobby.assetData.audioMap[LobbyAssetData.MUSIC]);
		}
		Loading.hide(Loading.LoadingTransactionResult.SUCCESS);

		if (BlackFaderScript.instance != null)
		{
			yield return StartCoroutine(BlackFaderScript.instance.fadeTo(0.0f));	
		}
		
		yield break;
	}

	public static LobbyAssetData setAssetData()
	{
		if (assetData == null)
		{
			assetData = new LobbyAssetData(SlotventuresChallengeCampaign.CAMPAIGN_ID, "slotventure");
		}

		return assetData;
	}

	public static void setupThemedAssetPaths()
	{
		assetData.lobbyPrefabPath = LOBBY_PREFAB_PATH;
		assetData.portalPrefabPath = OPTION_PREFAB_PORTAL_PATH;	
		assetData.sideBarPrefabPath = IN_GAME_UI_PATH;
		assetData.mainLobbyOptionPath = MAIN_LOBBY_OPTION_PATH;

		assetData.optionPrefabPath = string.Format(OPTION_PREFAB_PATH, assetData.themeName);
		assetData.jackpotPrefabPath = string.Format(JACKPOT_PREFAB, assetData.themeName); ;
		FIRST_OPTION_PATH = string.Format(FIRST_OPTION_PATH, assetData.themeName);
	}

	public static void setupAudioMap()
	{
		assetData.audioMap = new Dictionary<string, string>()
		{
			{ LobbyAssetData.OBJECTIVE_COMPLETE,        "ChallengeAllCompleteSlotVenturesCommon" },
			{ LobbyAssetData.ON_MISSION_COMPLETE,       "ReelsDimSlotVenturesCommon" },
			{ LobbyAssetData.ALL_OBJECTIVES_COMPLETE,   "ChallengeAllCompleteAnimateSlotVenturesCommon" },
			{ LobbyAssetData.TRANSITION,                string.Format("{0}{1}", "Transition", assetData.themeName) },
			{ LobbyAssetData.MUSIC,                     string.Format("{0}{1}", "LobbyBg", assetData.themeName) },
			{ LobbyAssetData.UNLOCK_NEW_GAME,           string.Format("{0}{1}", "UnlockNewGameCollect", assetData.themeName) },
			{ LobbyAssetData.JACKPOT_ROLLUP,            string.Format("{0}{1}", "RollupGameCompleted", assetData.themeName) },
			{ LobbyAssetData.COLLECT_JACKPOT,           string.Format("{0}{1}", "CollectJackpot", assetData.themeName) },
			{ LobbyAssetData.MOTD_OPEN,       			string.Format("{0}{1}", "LobbyMOTD", assetData.themeName) },
			{ LobbyAssetData.JACKPOT_TERM,              string.Format("{0}{1}", "RollupGameCompletedTerm", assetData.themeName) },
			{ LobbyAssetData.COLLECT_NEW_GAME, 			string.Format("{0}{1}", "GameCompleteFanfare", assetData.themeName)}
		};
	}
    protected override void setLobbyData()
	{
		LobbyLoader.lastLobby = LobbyInfo.Type.SLOTVENTURE;
		ChallengeLobby.sideBarUI = assetData.sideBarPrefab;
	}

	public override LobbyAssetData lobbyAssetData
	{
		get
		{
			return assetData;
		}
	}

	//// Do some stuff to get the menu options organized for display.
	public void organizeOptions(ChallengeLobbyCampaign slotventureCampaign)
	{
		slotventuresOptions = new List<LobbyOptionButtonSlotventure>();
		bool shouldAnimate = lastKnownIndex != -1 && lastKnownIndex != slotventureCampaign.currentEventIndex;
		isPlayingRewardsSequence = shouldAnimate;
		// + 1 for the starting item
		int count = slotventureCampaign.missions.Count + 1;

		// Normally this is count-1 but we have the starting item
		sliderControl.content.width = (count - 1) * xSpacing;
		
		//-1 to the count so we don't allow scrolling off-screen
		sliderControl.setBounds(-(xSpacing * (slotventureCampaign.missions.Count-1)), xSpacing * slotventureCampaign.missions.Count);

		// This is kind of a reusable/abusable index for all the weird offsetting we do.
		// Used to track what mission number to give to what prefab/where to start the scroller
		int index = 0;

		for (int i = 0; i < count; i++)
		{
			GameObject placedLobbyOption = null;

			bool isEven = (i + 1) % 2 == 0;
			int yOffset = isEven ? ySpacingEven : ySpacingOdd;

			// The last option is the jackpot option
			if (i == count - 1)
			{
				if (assetData.jackpotPrefab != null)
				{
					yOffset = ySpacingEven - ySpacingOdd; // this one is much taller than the others so it gets its own special place in the middle.
					placedLobbyOption = NGUITools.AddChild(sliderParent, assetData.jackpotPrefab, true);
					SlotventuresJackpotButton button = placedLobbyOption.GetComponent<SlotventuresJackpotButton>();
					button.lobbyReference = this;
					jackpotOption = button;

					bool isOptionCurrentEventIndex = slotventureCampaign.currentEventIndex == index;
					
					//Only have this button instantly on if its our current objective and we're not going to be doing the slide animation
					//The button will animate in if we're sliding to the jackpot
					button.collectButtonAnimator.gameObject.SetActive(isOptionCurrentEventIndex && !shouldAnimate && slotventureCampaign.state == ChallengeCampaign.IN_PROGRESS);
				}
				else
				{
					Debug.LogWarning("Invalid jackpot prefab");
				}
			}
			else if (i == 0)
			{
				if (characterInfoPrefab != null)
				{
					// This first option is the info option.
					placedLobbyOption = NGUITools.AddChild(sliderParent, characterInfoPrefab, true);
					introPrefabHandle = placedLobbyOption;
				}
				else
				{
					Debug.LogError("Invalid character info prefab");
				}

			}
			else
			{
				if (assetData.optionPrefab != null)
				{
					placedLobbyOption = NGUITools.AddChild(sliderParent, assetData.optionPrefab, true);

					LobbyOptionButtonSlotventure lobbyOption = placedLobbyOption.GetComponent<LobbyOptionButtonSlotventure>();

					slotventuresOptions.Add(lobbyOption);
					bool isOptionCurrentEventIndex = slotventureCampaign.currentEventIndex == index;
					bool isOptionLastKnownEventIndex = lastKnownIndex == index;
					if (slotventureCampaign.missions.Count > index)
					{
						lobbyOption.init(slotventureCampaign.missions[index], slotventureCampaign, shouldAnimate, isOptionCurrentEventIndex, isOptionLastKnownEventIndex);
						if (slotventureCampaign.missions[index].isComplete)
						{
							if (shouldAnimate && isOptionLastKnownEventIndex)
							{
								lobbyOption.lobbyCardAnimation.Play(LobbyOptionButtonSlotventure.ACTIVE);
							}
							else
							{
								lobbyOption.lobbyCardAnimation.Play(LobbyOptionButtonSlotventure.COMPLETED);
							}
						}
						else if (isOptionCurrentEventIndex)
						{
							if (shouldAnimate)
							{								
								lobbyOption.lobbyCardAnimation.Play(LobbyOptionButtonSlotventure.INCOMPLETE);
							}
							else
							{
								lobbyOption.lobbyCardAnimation.Play(LobbyOptionButtonSlotventure.ACTIVE);
							}
						}
						else
						{
							lobbyOption.lobbyCardAnimation.Play(LobbyOptionButtonSlotventure.INCOMPLETE);
						}
					}
					else
					{
						Debug.LogError("SlotventuresLobby::organizeOptions Mission count to lobby option mismatch. Not enough missions recieved");
					}
				}
				else
				{
					Debug.LogWarning("Invalid option prefab");
				}

				index++;
			}

			if (placedLobbyOption != null)
			{
				CommonTransform.setX(placedLobbyOption.transform, 0 + (i * xSpacing));
				CommonTransform.setY(placedLobbyOption.transform, yOffset);
			}

		}

		index = Mathf.Max(0, slotventureCampaign.missions.Count - 1);
		GameTimerRange waitToScrollTimer = null;

		if (lastKnownIndex == -1)
		{
			lastKnownIndex = slotventureCampaign.startingEventIndex;
		}

		int lobbyViewCount = PlayerPrefsCache.GetInt(Prefs.SEEN_SLOTVENTURES_LOBBY);

		// What we should do right after organizing options
		if (lobbyViewCount == 0 && ExperimentWrapper.Slotventures.isEUE ||
		    slotventureCampaign.currentEventIndex == 0 && !slotventureCampaign.currentMission.hasMadeProgress)
		{
			introPrefabHandle.SetActive(false);
			sliderControl.preventScrolling();
			// If it's the first mission and there's no progress, do the end to start pan
			sliderControl.safleySetXLocation(sliderParent.gameObject.transform.localPosition.x - slotventureCampaign.missions.Count * xSpacing);
			waitToScrollTimer = new GameTimerRange(GameTimer.currentTime, GameTimer.currentTime + ANIMATION_STEP_DELAY);
			waitToScrollTimer.registerFunction(setupLongPanForStart);
		}
		else if (lastKnownIndex == -1 || lastKnownIndex == slotventureCampaign.currentEventIndex)
		{
			// No changes, go to current mission
			index = Mathf.Max(0, slotventureCampaign.currentEventIndex);
			sliderControl.safleySetXLocation(sliderParent.gameObject.transform.localPosition.x - index * xSpacing);

			playEnterLobbyAudio();
			attemptEUEDisplay();
		}
		else
		{
			sliderControl.preventScrolling();
			needsToAddCredits = true;
			index = Mathf.Max(0, slotventureCampaign.currentEventIndex -1);
			// Empty out the current and next pips
			progressPanel.pips[(progressPanel.pips.Count - 1) - lastKnownIndex].playEmpty();
			if ((progressPanel.pips.Count - 1) - slotventureCampaign.currentEventIndex >= 0)
			{
				progressPanel.pips[(progressPanel.pips.Count - 1) - slotventureCampaign.currentEventIndex].playEmpty();
			}

			// We're on a new mission, slide from the end to the current to the next
			sliderControl.safleySetXLocation(sliderParent.gameObject.transform.localPosition.x - index * xSpacing);
			StartCoroutine(playGameTransition());
			goalsPanel.gameObject.SetActive(false);
		}

		if (index % 2 == 0)
		{
			sliderControl.uvPanning[1].hardSetOffsetX(0);
		}
		else
		{
			sliderControl.uvPanning[1].hardSetOffsetX(0.5f);
		}

		PlayerPrefsCache.SetInt(Prefs.SEEN_SLOTVENTURES_LOBBY, 1);
	}

	private void setupLongPanForStart(Dict args = null, GameTimerRange sender = null)
	{
		if (sliderControl != null)
		{
			sliderControl.onEndAnimation += onFinishIntroPan;
			sliderControl.scrollToHorizantalPosition(xSpacing * (slotventureCampaign.missions.Count - 1), ANIMATION_SPEED, true);
		}
	}

	private void onFinishIntroPan(Dict args = null)
	{
		if (sliderControl != null)
		{
			introPrefabHandle.SetActive(true);
			playEnterLobbyAudio();
			goalsPanel.onClickSlideOut();
			sliderControl.onEndAnimation -= onFinishIntroPan;
			sliderControl.enableScrolling();
			attemptEUEDisplay();
		}
	}

	private IEnumerator playGameTransition()
	{
		playEnterLobbyAudio();

		yield return new WaitForSeconds(4.5f);
		slotventuresOptions[slotventureCampaign.currentEventIndex - 1].lobbyCardAnimation.Play(LobbyOptionButtonSlotventure.COMPLETE_LOW_PEDESTAL);
		Audio.play(assetData.audioMap[LobbyAssetData.COLLECT_NEW_GAME]);

		long reward = slotventureCampaign.missions[slotventureCampaign.currentEventIndex - 1].getCreditsReward;
		if (slotventureCampaign.currentEventIndex != slotventureCampaign.missions.Count && (reward > 0))
		{
			if (Overlay.instance.top != null)
			{
				if (coinAttractObject == null)
				{
					Debug.LogError("attractor was null");
				}
				attractor = NGUITools.AddChild(slotventuresOptions[slotventureCampaign.currentEventIndex - 1].gameObject, coinAttractObject);

				if (attractor.GetComponentInChildren<particleAttractorSpherical>() == null)
				{
					Debug.LogError("script was null");
				}
				attractor.GetComponentInChildren<particleAttractorSpherical>().target = Overlay.instance.top.creditsTMPro.transform;

				yield return new WaitForSeconds(ANIMATION_STEP_DELAY);

				explosion = NGUITools.AddChild(Overlay.instance.top.creditsTMPro.gameObject, coinExplosionAnimation);
			}
			needsToAddCredits = false;

			SlotsPlayer.addFeatureCredits(reward, CREDIT_SOURCE);
		}
		
		progressPanel.pips[(progressPanel.pips.Count - 1) - lastKnownIndex].playComplete();
		yield return new WaitForSeconds(ANIMATION_STEP_DELAY);
		waitingForCardPackToFinish = (slotventureCampaign as SlotventuresChallengeCampaign).dropPackCheck();
		if (!waitingForCardPackToFinish)
		{
			yield return scrollToNextGame();
		}
	}

	public IEnumerator scrollToNextGame()
	{
		waitingForCardPackToFinish = false;
		// Scrolls by -xSpacing, not TO -xspacing
		sliderControl.scrollToHorizantalPosition(-xSpacing, isForced: true);
		yield return new WaitForSeconds(ANIMATION_STEP_DELAY);

		// The last option doesn't have this anim, that will play when we click collect
		if (slotventureCampaign.currentEventIndex < slotventureCampaign.missions.Count - 1)
		{			
			goalsPanel.gameObject.SetActive(true);
			goalsPanel.onClickSlideOut();
			Audio.play(SlotventuresLobby.assetData.audioMap[LobbyAssetData.UNLOCK_NEW_GAME]);
			LobbyOptionButtonSlotventure currentOption = slotventuresOptions[slotventureCampaign.currentEventIndex];
			currentOption.lobbyCardAnimation.Play(LobbyOptionButtonSlotventure.ACTIVATE);
			if (currentOption.cardPack != null && currentOption.cardPack.animator != null)
			{
				currentOption.cardPack.animator.Play(LobbyOptionButtonSlotventure.CARD_PACK_ACTIVATE);
			}
		}
		else if (jackpotOption != null)
		{
			jackpotOption.collectButtonAnimator.gameObject.SetActive(slotventureCampaign.state == ChallengeCampaign.IN_PROGRESS);
			if (jackpotOption.cardPack != null && jackpotOption.cardPack.animator != null)
			{
				jackpotOption.cardPack.animator.Play(LobbyOptionButtonSlotventure.ACTIVATE);
			}
		}
		
		if (slotventureCampaign.currentEventIndex > 0)
		{
			LobbyOptionButtonSlotventure previousOption = slotventuresOptions[slotventureCampaign.currentEventIndex - 1];
			if (previousOption.cardPack != null && previousOption.cardPack.animator != null)
			{
				previousOption.cardPack.animator.Play(LobbyOptionButtonSlotventure.CARD_PACK_INACTIVE);
			}
		}

		lastKnownIndex = slotventureCampaign.currentEventIndex;

		// Greater than 0 check since the last thing in the lobby is the jackpot which doesn't have a pip
		if ((progressPanel.pips.Count - 1) - lastKnownIndex >= 0)
		{
			progressPanel.pips[(progressPanel.pips.Count -1) - lastKnownIndex].playFillCurrent();
		}

		yield return new WaitForSeconds(ANIMATION_STEP_DELAY);

		sliderControl.enableScrolling();
		
		// make sure our buttons still work since spin panel may not have enabled them when we went back to lobby.
		Overlay.instance.setButtons(true);
		isPlayingRewardsSequence = false;

		attemptEUEDisplay();
	}

	/*=========================================================================================
	EUE DIALOG
	=========================================================================================*/
	/// <summary>
	/// Attempts to show the EUE popup if applicable
	/// </summary>
	private void attemptEUEDisplay()
	{
		if
		(
			!CustomPlayerData.getBool(CustomPlayerData.SLOTVENTURES_HAS_SEEN_EUE, true)
			&& ExperimentWrapper.Slotventures.isEUE
		)
		{
			euePopupMainButton.registerEventDelegate(onReturnToMainLobbyClick);
			euePopupContinueButton.registerEventDelegate(onKeepPlayingClick);
			toggleEUEPopup(true);
			sliderControl.enabled = false;
		}
	}

	private void toggleEUEPopup(bool isActive = false)
	{
		SafeSet.gameObjectActive(euePopup, isActive);
		isDisplayingEUE = isActive;
	}

	private void onKeepPlayingClick(Dict args = null)
	{
		StatsSlotventures.logEUEClick("click_jackpot");
		toggleEUEPopup();
		CustomPlayerData.setValue(CustomPlayerData.SLOTVENTURES_HAS_SEEN_EUE, true);
		sliderControl.enabled = true;
	}

	private void onReturnToMainLobbyClick(Dict args = null)
	{
		StatsSlotventures.logEUEClick();
		CustomPlayerData.setValue(CustomPlayerData.SLOTVENTURES_HAS_SEEN_EUE, true);
		backClicked();
	}

	public override IEnumerator transitionToMainLobby()
	{
		if (isDisplayingEUE)
		{
			CustomPlayerData.setValue(CustomPlayerData.SLOTVENTURES_HAS_SEEN_EUE, true);
		}

		yield return null;

		Audio.play("SelectPremiumAction");

		NGUIExt.disableAllMouseInput();
		yield return StartCoroutine(BlackFaderScript.instance.fadeTo(1.0f));

		Overlay.instance.top.hideLobbyButton();
		yield return StartCoroutine(LobbyLoader.instance.createMainLobby());

		Overlay.instance.topHIR.hideMaxVoltageWinner();
		Destroy(gameObject);

		NGUIExt.enableAllMouseInput();
	}

	private void imageLoadSuccess(string assetPath, Object obj, Dict data = null)
	{
		if (instance == null)
		{
			return;
		}
		
		if (assetPath == PARALLAX_SUPERFOREGROUND)
		{
			(instance as SlotventuresLobby).superForeground.material.mainTexture = obj as Texture2D;
		}
		else if (assetPath == PARALLAX_FOREGROUND)
		{
			(instance as SlotventuresLobby).foreground.material.mainTexture = obj as Texture2D;
		}
		else if (assetPath == PARALLAX_MIDGROUND)
		{
			(instance as SlotventuresLobby).midground.material.mainTexture = obj as Texture2D;
		}
		else if (assetPath == PARALLAX_BACKGROUND)
		{
			(instance as SlotventuresLobby).background.material.mainTexture = obj as Texture2D;
		}

		imagesLoaded++;

		if (imagesLoaded == 4 && prefabsLoaded == prefabsNeededToLoadCount)
		{
			finishLoading();
		}
	}
	// Used by LobbyLoader to preload asset bundle.
	private void bundleLoadSuccess(string assetPath, Object obj, Dict data = null)
	{
		if (assetPath == TOASTER_PATH_ENDING_SOON)
		{
			toasterPrefabEnding = obj as GameObject;
		}
		else if (assetPath == TOASTER_PATH_OVER)
		{
			toasterPrefabEnded = obj as GameObject;
		}
		else if (assetPath == COIN_EXPLOSION_ANIMATION_PATH)
		{
			coinExplosionAnimation = obj as GameObject;
		}
		else if (assetPath == COIN_ATTRACT_ANIMATION_PATH)
		{
			coinAttractObject = obj as GameObject;
		}
		else if (assetPath == FIRST_OPTION_PATH)
		{
			characterInfoPrefab = obj as GameObject;
		}
		else if (assetPath == assetData.optionPrefabPath)
		{
			assetData.optionPrefab = obj as GameObject;	
		}
		else if (assetPath == assetData.jackpotPrefabPath)
		{
			assetData.jackpotPrefab = obj as GameObject;
		}
		else if (assetPath == assetData.sideBarPrefabPath)
		{
			assetData.sideBarPrefab = obj as GameObject;
		}
		
		prefabsLoaded++;
		
		if (imagesLoaded == 4 && prefabsLoaded == prefabsNeededToLoadCount)
		{
			finishLoading();
		}
	}

	// Used by LobbyLoader to preload asset bundle.
	public static void bundleLoadFailure(string assetPath, Dict data = null)
	{
		Debug.LogError("SlotventuresLobby::bundleLoadFailure - Failed to download " + assetPath);
	}

	private void OnDestroy()
	{
		if (attractor != null)
		{
			Destroy(attractor);
		}

		if (explosion != null)
		{
			Destroy(explosion);
		}

		// we didn't add credits yet, don't cause a desync and make sure last known is up to date.
		if (needsToAddCredits)
		{
			if (slotventureCampaign != null && slotventureCampaign.missions != null &&
			    slotventureCampaign.currentEventIndex > 0 &&
			    slotventureCampaign.currentEventIndex <= slotventureCampaign.missions.Count)
			{
				long reward = slotventureCampaign.missions[slotventureCampaign.currentEventIndex - 1].getCreditsReward;
				SlotsPlayer.addFeatureCredits(reward, CREDIT_SOURCE);
			}
		}

		lastKnownIndex = slotventureCampaign.currentEventIndex;

		instance = null;
	}

	protected override void setChallengeCampaign()
	{
		ChallengeLobbyCampaign.currentCampaign = CampaignDirector.find(assetData.campaignName) as ChallengeLobbyCampaign;
	}

	public static void onReload(Dict args = null)
	{
		LobbyLoader.lastLobby = LobbyInfo.Type.SLOTVENTURE;
	}

	private void playEnterLobbyAudio()
	{
		Audio.play(string.Format("EnterNarrativeSlotVentures{0}", assetData.themeName));
	}

	new public static void resetStaticClassData() 
	{
		lastKnownIndex = -1;
		//assetData = null;
	}
}
