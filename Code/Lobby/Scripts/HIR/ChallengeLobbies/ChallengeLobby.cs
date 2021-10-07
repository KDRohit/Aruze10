using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

public abstract class ChallengeLobby : MonoBehaviour, IResetGame
{
	// =============================
	// PROTECTED
	// =============================
	protected LobbyInfo lobbyInfo = null;
	protected static int pageBeforeGame = -1;		// Remember what lobby page the player is on so we can return to it from a game.
	protected List<LobbyOption> options = new List<LobbyOption>();
	protected List<ListScrollerItem> itemMap = null;
	protected LobbyOptionButtonChallengeLobby jackpotOptionButton = null;
	protected static LobbyInfo.Type currentLobby;
	
	// =============================
	// PUBLIC
	// =============================
	public ListScroller scroller;
	public GameObject scrollerViewportSizer;
	public Renderer backgroundRenderer;
	public TextMeshPro lobbyText;
	public static LobbyAssetData assetData = null;
	public static bool isFirstTime = true;
	public static GameObject sideBarUI = null;
	public static ChallengeLobby instance { get; protected set; }

	// add all challenge lobbies asset data here
	public static List<LobbyAssetData> lobbyAssetDataList = new List<LobbyAssetData>()
	{
		{ SinCityLobby.setAssetData() },
		//{ LOZLobby.setAssetData() }
	};

	// =============================
	// CONST
	// =============================
	protected const int MAIN_BUTTON_SPOTS_PER_PAGE = MainLobby.MAIN_BUTTON_SPOTS_PER_ROW * 1;	
	protected const float BACKGROUND_TINT_TIME = 1.0f;
	protected const float BACKGROUND_TINT = 0.47f;		// The darkened background tint value.
	protected const float SCROLL_TIME = 2.0f;
	protected const float JACKPOT_OPTION_ROLLUP_TIME = 3.5f;

	/*=========================================================================================
	ENTER/EXIT
	=========================================================================================*/
	void Awake()
	{
		instance = this;
		setLobbyData();

		// after setting lobby data, determine if this is the first time the user is viewing the lobby
		isFirstTime = currentLobby != LobbyLoader.lastLobby;
		currentLobby = LobbyLoader.lastLobby;

		preAwake();
		postAwake();
		setChallengeCampaign();
	}

	/// <summary>
	///   Abstract method, override in subclass to set last lobby and lobbyInfo instance
	///   e.g.
	///     LobbyLoader.lastLobby = LobbyInfo.Type.SIN_CITY;
	/// 	lobbyInfo = LobbyInfo.find(LobbyInfo.Type.SIN_CITY);
	/// </summary>
	protected abstract void setLobbyData();
	protected abstract void setChallengeCampaign();

	// Do stuff that has to happen first during Awake().
	public virtual void preAwake()
	{
		NGUIExt.attachToAnchor(gameObject, NGUIExt.SceneAnchor.CENTER, transform.localPosition);
				
		DisposableObject.register(gameObject);
		
		organizeOptions();
				
		Overlay.instance.top.hideLobbyButton();

		if (!Audio.isPlaying("lobbyambienceloop0"))
		{
			// Playing ambience aborts the lobby music loop,
			// so only play this if it's not already playing,
			// since the lobby music loop can start playing
			// from Data.cs right after global data is set,
			// way before the lobby is instantiated.
			Audio.play("lobbyambienceloop0");
		}
		MainLobby.playLobbyMusic();
	}

	// Do stuff that has to happen after certain other things in Awake().
	protected virtual void postAwake()
	{
		if (!isFirstTime)
		{
			// Return to the scroll position the player was on when launching a game.
			restorePreviousScrollPosition();
		}
	
		NGUIExt.enableAllMouseInput();

		// Make sure the original material doesn't get modified when tinting.
		backgroundRenderer.sharedMaterial = new Material(backgroundRenderer.sharedMaterial);
		
		didFinishIntro = false;

		// just incase these weren't set before due to race condition
		foreach ( LobbyOption option in options )
		{
			if ( option.game != null )
			{
				option.game.setIsUnlocked();
			}
		}

		StartCoroutine(finishTransition());
	}

	void Update()
	{
		if (!isTransitioning &&
			(Dialog.instance != null && !Dialog.instance.isShowing) &&
		    !DevGUI.isActive &&
			!CustomLog.Log.isActive) // only perform back button functionality on Lobby if no dialog is open
		{
			AndroidUtil.checkBackButton(backClicked);
		}
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

	/*=========================================================================================
	TRANSITIONS/SETUP
	=========================================================================================*/
	// Do some stuff to get the menu options organized for display.
	protected void organizeOptions()
	{
		if (lobbyInfo == null)
		{
			return;
		}

		// All options in the LOZ lobby should be unpinned, so only add those.
		options.AddRange(lobbyInfo.unpinnedOptions);

		itemMap = new List<ListScrollerItem>();
		
		foreach (LobbyOption option in options)
		{
			if ( option.game == null || CampaignDirector.findWithGame(option.game.keyName) != null )
			{				
				itemMap.Add(new ListScrollerItem(lobbyAssetData.optionPrefab, configureOptionPanel, option));
			}
		}

		// Add the jackpot lobby option after all the games.
		itemMap.Add(new ListScrollerItem(lobbyAssetData.jackpotPrefab, configureOptionPanel, null));
		
		scroller.setItemMap(itemMap);
	}

	private IEnumerator configureOptionPanel(ListScrollerItem item)
	{
		LobbyOption option = item.data as LobbyOption;
		LobbyOptionButton button = item.panel.GetComponent<LobbyOptionButton>();
		
		if (item == itemMap[itemMap.Count - 1])
		{
			// If the last item in the list, then it's the jackpot item.
			// Store a reference to the option button to use later.
			jackpotOptionButton = (button as LobbyOptionButtonChallengeLobby);
		}

		button.setup(option);

		if (option != null)		// LobbyOption will be null for the jackpot option.
		{
			option.button = button;
			StartCoroutine(option.loadImages());
		}

		yield break;
	}
	
	// Return to the page the player was on when launching a game.
	protected void restorePreviousScrollPosition()
	{
		scroller.normalizedScroll = 0.001f * pageBeforeGame;
	}

	public virtual int getTrackedScrollPosition()
	{
		return 0;
	}

	// Play lobby music. Allows for SKU overrides.
	public virtual void playLobbyInstanceMusic()
	{
		if (!Audio.isPlaying(lobbyAssetData.audioMap[LobbyAssetData.MUSIC]))
		{
			Audio.switchMusicKeyImmediate(lobbyAssetData.audioMap[LobbyAssetData.MUSIC]);
		}
	}

	// The main call to go into the main Lobby from here.
	public virtual IEnumerator transitionToMainLobby()
	{
		yield return null;

		isTransitioning = true;
		Audio.play("SelectPremiumAction");
		
		NGUIExt.disableAllMouseInput();

		yield return StartCoroutine(BlackFaderScript.instance.fadeTo(1.0f));

		if (scroller != null)
		{
			scroller.gameObject.SetActive(false);
		}

		yield return StartCoroutine(LobbyLoader.instance.createMainLobby());

		yield return null;

		cleanupBeforeDestroy();
		Destroy(gameObject);
		NGUIExt.enableAllMouseInput();
	}

	// Animate the background from full bright to darkened tint,
	// because we need it darkened to make the lobby options stand out
	// but want it full bright when first seeing it.
	protected virtual IEnumerator finishTransition()
	{
		ChallengeLobbyCampaign campaign = CampaignDirector.find(lobbyAssetData.campaignName) as ChallengeLobbyCampaign;
		
		float currentScroll = 0.0f;
		
		yield return null;	// Give the ListScroller a frame to initialize before calling scrollToItem().
		
		// First set to the current option.
		// If the first time here in this session, we will jump to the last option then scroll back to this.
		int currentOption = getLastUnlockedGameIndex(campaign);
		
		scroller.scrollToItem(itemMap[currentOption]);

		if (isFirstTime || doShowCelebration)
		{
			currentScroll = scroller.normalizedScroll;
			// Jump to the last option.
			scroller.scrollToItem(itemMap[itemMap.Count - 1]);
		}

		yield return StartCoroutine(BlackFaderScript.instance.fadeTo(0.0f));
		
		iTween.ValueTo(gameObject,
			iTween.Hash(
				"from", 1.0f,
				"to", BACKGROUND_TINT,
				"time", BACKGROUND_TINT_TIME,
				"onupdate", "updateBackgroundColor"
			)
		);

		if (campaign.isComplete)
		{
			yield return doRollup();
		}
		
		if (doShowCelebration || isFirstTime)
		{
			yield return animateScroller(currentScroll);
		}

		didFinishIntro = true;
		isFirstTime = false;
	}

	protected virtual IEnumerator animateScroller(float currentScroll)
	{
		if (currentScroll != scroller.normalizedScroll)
		{
			yield return new WaitForSeconds(0.5f);
			StartCoroutine(scroller.animateScroll(currentScroll, SCROLL_TIME));
			while (scroller.isAnimatingScroll)
			{
				yield return null;
			}
		}
	}

	protected virtual IEnumerator doRollup()
	{
		yield return null;
		// does nothing by default. some challenge lobbies have tiers, and will need to implement this
		// @see LOZLobby.doRollup()
	}

	private void updateBackgroundColor(float value)
	{
		backgroundRenderer.sharedMaterial.color = new Color(value, value, value);
	}

	// Used with Scheduler to refresh the lobby safely.
	// This applies to whatever lobby is current loaded, not just main.
	public static void refresh(Dict args)
	{
		Scheduler.removeFunction(MainLobby.refresh);
		Loading.show(Loading.LoadingTransactionTarget.LOBBY);
		Glb.loadLobby();
	}

	/*=========================================================================================
	ANCILLARY
	=========================================================================================*/
	/// <summary>
	/// Returns a challenge lobby campaign asset data instance
	/// </summary>
	public static LobbyAssetData findAssetDataForCampaign(string campaignName)
	{
		foreach (LobbyAssetData lobbyAssetData in lobbyAssetDataList)
		{
			if (lobbyAssetData.campaignName == campaignName)
			{
				return lobbyAssetData;
			}
		}

		return null;
	}

	public virtual int getLastUnlockedGameIndex(ChallengeLobbyCampaign campaign)
	{
		for (int i = campaign.missions.Count; --i >= 0; )
		{
			string gameKey = campaign.missions[i].objectives[0].game;
			if (LobbyGame.find(gameKey).isUnlocked)
			{
				return i;
			}
		}
		return 0;
	}

	public static string getAudioByKey(string key)
	{
		if (instance != null)
		{
			return instance.lobbyAssetData.getAudioByKey(key);
		}
		return null;
	}

	/*=========================================================================================
	GETTERS
	=========================================================================================*/
	public virtual LobbyAssetData lobbyAssetData
	{
		get
		{
			throw new UnityException("ChallengeLobby.assetData is not set up. assetData should be initialized in subclasses");
		}
	}

	public bool didFinishIntro { get; protected set; }
	public bool isTransitioning { get; protected set; }		// Whether transitioning between different parts of the lobby.
	public virtual bool doShowCelebration
	{
		get
		{
			ChallengeLobbyCampaign campaign = CampaignDirector.find(lobbyAssetData.campaignName) as ChallengeLobbyCampaign;
			return campaign.isComplete;
		}
	}

	/*=========================================================================================
	IResetGame Contracts
	=========================================================================================*/
	public static void resetStaticClassData()
	{
		pageBeforeGame = -1;
		isFirstTime = true;
	}
	
	// Manually call this method before we will explicitly destroy the lobby
	// before going into a game. This is necessary instead of using OnDestroy()
	// because OnDestroy() also gets called during shutdown, and the order in which
	// things are destroyed is unpredictable, causing some gameObjects to already
	// be null, resulting in NRE's that don't seem to make sense.
	public virtual void cleanupBeforeDestroy()
	{
		// Since pageBeforeGame is an int, but the ListScroller uses a float, multiply it by 1000
		// to make it a somewhat accurate integer, which we multiply by 0.001 when restoring.
		if (scroller != null)
		{
			pageBeforeGame = Mathf.RoundToInt(scroller.normalizedScroll * 1000.0f);
		}
	}
}