using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Com.Scheduler;

public class MaxVoltageLobbyHIR : MonoBehaviour, IResetGame
{
	[SerializeField] 
	protected GameObject optionPrefab;
	
	public const string BUNDLE_NAME = "max_voltage";
	public const string LOBBY_PREFAB_PATH = "Features/Max Voltage/Lobby Prefabs/Max Voltage Lobby Panel";
	public const string OPTION_PREFAB_PORTAL_PATH = "Assets/Data/HIR/Bundles/Initialization/Features/Max Voltage/Max Voltage Room Main Lobby Option.prefab";

	public static JSON recentWinnerData;
	public static MaxVoltageLobbyHIR instance = null;
	
	private static bool lobbyLoadReq;

	public ListScroller scroller;
	public TextMeshPro jackpotLabel;

	private int currentMeter = 0;
	private List<ListScrollerItem> itemMap = new List<ListScrollerItem>();
	private List<LobbyOption> options = new List<LobbyOption>();
	private List<MaxVoltageTokenCollectionModule.MaxVoltageTokenProgressBar> meters = new List<MaxVoltageTokenCollectionModule.MaxVoltageTokenProgressBar>();
	private MaxVoltageTokenCollectionModule.MaxVoltageTokenProgressBar bronzeMeter;
	private MaxVoltageTokenCollectionModule.MaxVoltageTokenProgressBar goldMeter;
	private MaxVoltageTokenCollectionModule.MaxVoltageTokenProgressBar silverMeter;
	private SocialMember recentWinnerMember;

	public static bool isBeingLazilyLoaded => AssetBundleManager.shouldLazyLoadBundle(MaxVoltageLobbyHIR.BUNDLE_NAME);

	private LobbyInfo lobbyInfo => LobbyInfo.find(LobbyInfo.Type.MAX_VOLTAGE);

	// Whether transitioning between different parts of the lobby.
	public bool isTransitioning { get; private set; }
	
	public static GameObject optionPrefabPortal
	{
		get
		{
			return SkuResources.getObjectFromMegaBundle<GameObject>(OPTION_PREFAB_PORTAL_PATH);
		}
	}
	

	public static bool IsActive()
	{
		// feature is deferred for loading, so we only need to check if it SHOULD be lazy loaded.
		// shouldLazyLoadBundle() returns true if the feature was missing, and still has yet to be downloaded
		// so if it's not part of lazy loading, then we shouldn't lazy load it, and the feature should be on
		return !AssetBundleManager.shouldLazyLoadBundle(BUNDLE_NAME); 
	}

	// Used by LobbyLoader to preload asset bundle.
	public static void bundleLoadFailure(string assetPath, Dict data = null)
	{
		Debug.LogError("Failed to download max voltage asset: " + assetPath + ".\nMax Voltage lobby option will not appear.");
	}

	public static void onLoadBundleRequest()
	{
		if (AssetBundleManager.isBundleCached("max_voltage") && AssetBundleManager.isBundleCached("main_snd_max_voltage"))
		{
			return;
		}

		AssetBundleManager.downloadAndCacheBundle("max_voltage", keepLoaded: false, true, blockingLoadingScreen:false);
		AssetBundleManager.downloadAndCacheBundle("main_snd_max_voltage", keepLoaded: false, true, blockingLoadingScreen:false);
	}

	public static void onReload(Dict args = null)
	{
		LobbyLoader.lastLobby = LobbyInfo.Type.MAX_VOLTAGE;
	}

	// Used with Scheduler to refresh the lobby safely.
	// This applies to whatever lobby is current loaded, not just main.
	public static void refresh(Dict args)
	{
		Scheduler.removeFunction(MainLobby.refresh);
		Loading.show(Loading.LoadingTransactionTarget.LOBBY);
		Glb.loadLobby();
	}

	public static void resetStaticClassData()
	{
	}

	public static void showJackpot()
	{
		//instance.meters[currentMeter].SetActive(false);
	}

	private static IEnumerator cleanupAssetsAsync()
	{
		yield return null;
		
		AssetBundleManager.unloadBundleImmediately("max_voltage");
		AssetBundleManager.unloadBundleImmediately("main_snd_max_voltage");
		
		yield return null;

		Glb.cleanupMemoryAsync();
	}

	// Play lobby music. Allows for SKU overrides.
	public virtual void playLobbyInstanceMusic()
	{
		if (!Audio.isPlaying("MVLobbyBg"))
		{
			Audio.switchMusicKeyImmediate("MVLobbyBg");
		}
	}

	// NGUI button callback.
	public void backClicked()
	{
		if (isTransitioning)
		{
			return;
		}

		if (Overlay.instance != null && Overlay.instance.jackpotMystery != null)
		{
			Overlay.instance.jackpotMystery.hide();
		}
		
		StartCoroutine(transitionToMainLobby());
	}

	public void applyMeterSetup()
	{
		if (MaxVoltageTokenCollectionModule.instance == null)
		{
			return;
		}

		MaxVoltageTokenCollectionModule.instance.jackpotAndCoin.SetActive(false);
		
		bronzeMeter = MaxVoltageTokenCollectionModule.instance.bronzeProgressBar;
		silverMeter = MaxVoltageTokenCollectionModule.instance.silverProgressBar;
		goldMeter = MaxVoltageTokenCollectionModule.instance.goldProgressBar;

		if (bronzeMeter == null || silverMeter == null || goldMeter == null)
		{
			return;
		}

		MaxVoltageTokenCollectionModule.instance.fillMeter.SetActive(true);

		meters.Add(bronzeMeter);
		meters.Add(silverMeter);
		meters.Add(goldMeter);

		bronzeMeter.topBarParentObject.SetActive(false);
		silverMeter.topBarParentObject.SetActive(false);
		goldMeter.topBarParentObject.SetActive(false);
		
		cycleMeters();
	}

	public void setRecentWinner()
	{
		if (recentWinnerData == null)
		{
			return;
		}

		long credits = recentWinnerData.getLong( "credits", 0L);
		string fbId = recentWinnerData.getString("fbid", "");
		string firstName = recentWinnerData.getString("first_name", "");
		string lastName = recentWinnerData.getString("last_name", "");
		string zid = recentWinnerData.getString("zid", "");
		long achievementScore = recentWinnerData.getLong("achievement_score", -1);

		// Try and find an existing facebookMember first for this.
		recentWinnerMember = CommonSocial.findOrCreate(
			zid: zid,
			fbid: fbId,
			firstName: firstName,
			lastName: lastName,
			achievementScore:achievementScore);
			
		if (recentWinnerMember == null)
		{
			Debug.LogErrorFormat("MaxVoltageLobbyHIR.cs -- setRecentWinner -- recentWinnerMember is null even though we just created it...not sure how this happened but lets bail out.");
			return;
		}
			
		recentWinnerMember.addScore(SocialMember.ScoreType.MAX_VOLTAGE_RECENT_WINNER, CreditsEconomy.multipliedCredits(credits));
		if (Overlay.instance != null && Overlay.instance.topHIR != null)
		{
			// If the overlay is not instantiated, then we don't want to try and show anything.
			// Pass the credits just in case the member is null and we want a value displayed.
			Overlay.instance.topHIR.showMaxVoltageWinner(recentWinnerMember, CreditsEconomy.multipliedCredits(credits));
		}
	}

	// Manually call this method before we will explicitly destroy the lobby
	// before going into a game. This is necessary instead of using OnDestroy()
	// because OnDestroy() also gets called during shutdown, and the order in which
	// things are destroyed is unpredictable, causing some gameObjects to already
	// be null, resulting in NRE's that don't seem to make sense.
	public virtual void cleanupBeforeDestroy()
	{
	}

	/*=========================================================================================
	METER ANIMATIONS
	=========================================================================================*/
	public void cycleMeters()
	{
		if (GameState.game != null)
		{
			return;
		}

		MaxVoltageTokenCollectionModule.MaxVoltageTokenProgressBar meter = meters[currentMeter];
		meter.topBarParentObject.SetActive(true);
		meter.topBarProgressSprite.color = Color.white;
		meter.topBarProgressBackingSprite.color = Color.white;
		meter.topBarProgressGlowSprite.color = Color.white;
		meter.topBarProgressFrame.color = Color.white;
		meter.picksText.color = Color.white;

		iTween.ValueTo
		(
			gameObject, iTween.Hash
			(
				"from"
				, 1.0f
				, "to"
				, 0.0f
				, "time"
				, 0.5
				, "onComplete"
				, "nextMeter"
				, "delay"
				, 1.5f
				, "onupdate"
				, "onMeterFadeUpdate"
			)
		);
	}

	public void onMeterFadeUpdate(float alphaValue)
	{
		MaxVoltageTokenCollectionModule.MaxVoltageTokenProgressBar meter = meters[currentMeter];
		Color color = CommonColor.adjustAlpha( meter.topBarProgressSprite.color, alphaValue );
		meter.topBarProgressSprite.color = color;
		meter.topBarProgressBackingSprite.color = color;
		meter.topBarProgressGlowSprite.color = color;
		meter.topBarProgressFrame.color = color;
		meter.picksText.color = color;
	}

	public void nextMeter()
	{
		if (GameState.game != null)
		{
			return;
		}

		// turn off the old meter
		MaxVoltageTokenCollectionModule.MaxVoltageTokenProgressBar meter = meters[currentMeter];
		meter.topBarParentObject.SetActive(false);

		// move to the next meter
		currentMeter = (++currentMeter) % meters.Count;
		meter = meters[currentMeter];

		// set the alpha on the new meter to 0
		meter.topBarParentObject.SetActive(true);

		Color faded = CommonColor.adjustAlpha(meter.topBarProgressSprite.color, 0f);
		meter.topBarProgressSprite.color = faded;
		meter.topBarProgressBackingSprite.color = faded;
		meter.topBarProgressGlowSprite.color = faded;
		meter.topBarProgressFrame.color = faded;
		meter.picksText.color = faded;

		iTween.ValueTo
		(
			gameObject, iTween.Hash
			(
				"from"
				, 0f
				, "to"
				, 1f
				, "time"
				, 0.5
				, "onComplete"
				, "cycleMeters"
				, "onupdate"
				, "onMeterFadeUpdate"
			)
		);
	}

	private IEnumerator finishTransition()
	{
		if (itemMap.Count > 0)
		{
			scroller.scrollToItem(itemMap[itemMap.Count - 1]);
		}

		yield return StartCoroutine(BlackFaderScript.instance.fadeTo(0.0f));

		yield return StartCoroutine(animateScroll());
	}

	private IEnumerator animateScroll()
	{
		yield return StartCoroutine(scroller.animateScroll(0, 1.0f));
	}

	private IEnumerator configureOption(ListScrollerItem item)
	{
		LobbyOption option = item.data as LobbyOption;
		LobbyOptionButton button = item.panel.GetComponent<LobbyOptionButton>();

		button.setup(option);
		StartCoroutine(option.loadImages());

		yield break;
	}

	private IEnumerator transitionToMainLobby()
	{
		yield return null;

		isTransitioning = true;
		Audio.play("SelectPremiumAction");
		
		NGUIExt.disableAllMouseInput();

		yield return null;

		yield return StartCoroutine(BlackFaderScript.instance.fadeTo(1.0f));

		Overlay.instance.top.hideLobbyButton();
		yield return StartCoroutine(LobbyLoader.instance.createMainLobby());

		if (Overlay.instance.jackpotMystery != null && Overlay.instance.jackpotMystery.tokenBar != null)
		{
			Server.unregisterEventDelegate(Overlay.instance.jackpotMystery.tokenBar.tokenServerEventName, true); //unregister this to prevent multiple calls to the event if we return to the lobby later
			Destroy(Overlay.instance.jackpotMystery.tokenBar.gameObject);
			Overlay.instance.jackpotMystery.tokenBar = null;
		}
		
		Overlay.instance.topHIR.hideMaxVoltageWinner();

		meters.Clear();
		gameObject.transform.parent = null;
		Destroy(gameObject);

		instance = null;
		
		NGUIExt.enableAllMouseInput();

		yield return RoutineRunner.instance.StartCoroutine(cleanupAssetsAsync());
	}

	void Awake()
	{
		setup();
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

	private void setup()
	{
		instance = this;

		LobbyLoader.lastLobby = LobbyInfo.Type.MAX_VOLTAGE;

		if (lobbyInfo == null)
		{
			Debug.LogError("No Max Voltage Lobby Data Found.");
		}

		NGUIExt.attachToAnchor(gameObject, NGUIExt.SceneAnchor.CENTER, transform.localPosition);
		
		DisposableObject.register(gameObject);
		
		organizeOptions();
		
		Overlay.instance.top.hideLobbyButton();
		
		if (Overlay.instance != null)
		{
			if (Overlay.instance.jackpotMystery != null)
			{
				if (Overlay.instance.jackpotMysteryHIR.tokenBar == null)
				{
					Overlay.instance.jackpotMysteryHIR.setUpTokenBar();
					if (Overlay.instance.jackpotMysteryHIR.tokenBar != null)
					{
						Overlay.instance.jackpotMysteryHIR.tokenAnchor.SetActive(true);
						Overlay.instance.jackpotMysteryHIR.tokenBar.setupBar();
					}
				}
				else if (Overlay.instance.jackpotMysteryHIR.tokenBar as MaxVoltageTokenCollectionModule == null)
				{
					Destroy(Overlay.instance.jackpotMysteryHIR.tokenBar.gameObject);
					Overlay.instance.jackpotMysteryHIR.tokenBar = null;
					Overlay.instance.jackpotMysteryHIR.setUpTokenBar();
					if (Overlay.instance.jackpotMysteryHIR.tokenBar != null)
					{
						Overlay.instance.jackpotMysteryHIR.tokenAnchor.SetActive(true);
						Overlay.instance.jackpotMysteryHIR.tokenBar.setupBar();
					}
				}
				else
				{
					Overlay.instance.jackpotMysteryHIR.tokenAnchor.SetActive(true);
					Overlay.instance.jackpotMysteryHIR.tokenBar.setupBar();
				}
			}
			else
			{
				Overlay.instance.pendingTokenBar = true;
				Overlay.instance.addJackpotOverlay();
			}
		}

		if (!Audio.isPlaying("lobbyambienceloop0"))
		{
			// Playing ambience aborts the lobby music loop, so only play this if it's not already playing, since
			// the lobby music loop can start playing from Data.cs right after global data is set, way before the
			// lobby is instantiated.
			Audio.play("lobbyambienceloop0");
		}
		
		MainLobby.playLobbyMusic();

		NGUIExt.enableAllMouseInput();
		
		Overlay.instance.topHIR.hideMaxVoltageWinner();
		
		if (ProgressiveJackpot.maxVoltageJackpot != null)
		{
			ProgressiveJackpot.maxVoltageJackpot.registerLabel(jackpotLabel);
			setRecentWinner();
		}

		applyMeterSetup();
	}

	// Do some stuff to get the menu options organized for display.
	private void organizeOptions()
	{
		itemMap = new List<ListScrollerItem>();

		// All options in the MVZ lobby should be unpinned, so only add those.
		lobbyInfo.unpinnedOptions.Sort(LobbyOption.sortByOrder);
		options.AddRange(lobbyInfo.unpinnedOptions);

		for (int i = 0; i < options.Count; ++i)
		{
			options[i].lobbyPosition = 10 * (i + 1);
			itemMap.Add(new ListScrollerItem(optionPrefab, configureOption, options[i]));
		}

		scroller.setItemMap(itemMap);
	}

	private void OnDestroy()
	{
		if (Overlay.instance != null && Overlay.instance.jackpotMystery != null)
		{
			Overlay.instance.jackpotMystery.hide();
		}
	}
}
