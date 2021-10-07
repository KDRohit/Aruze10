#define USE_LOAD_ON_REQUEST_LOBBY
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.HitItRich.EUE;
using Com.HitItRich.Feature.VirtualPets;
using Com.Scheduler;
using Zynga.Core.Util;

/*
Used to initialize a lobby when loaded.
*/

public class LobbyLoader : MonoBehaviour, IResetGame
{
    public delegate void onLobbyLoad(Dict args = null);

    private delegate void LobbyPrefabLoaded();

    public static event onLobbyLoad lobbyLoadEvent;

    public static LobbyGame.LaunchResult autoLaunchGameResult = LobbyGame.LaunchResult.NO_LAUNCH;
    public static LobbyLoader instance = null;

    private static bool loadedDirectlyOnce;
    private static LobbyPrefabLoaded OnLobbyPrefabLoaded;
    private static LobbyInfo.Type _lastLobby = LobbyInfo.Type.MAIN;

    // Add each to the bundle to download if it needs to be done before loading the lobby.
    private Dictionary<string, AssetBundlePreload> assetBundlesToDownloadFirst =
        new Dictionary<string, AssetBundlePreload>();

    private static bool isLoadingMainLobbyPrefabInProgress = false;
    private static bool isLoadingLobbyBottomOverlayPrefabInProgress = false;
    private static bool wasPausedDuringLoading = false;
    
    private static GameObject _lobbyBottomOverlayPrefab; // used by lobby v3
    private static GameObject _lobbyMainPrefab;

    private int finishedBundleDownloads = 0;
    
    // Add each to the bundle to download if it needs to be done before loading the lobby.
    private List<NonBlockingBundleLoad> secondaryAssetBundlesToCache =  new List<NonBlockingBundleLoad>(); 

    public static LobbyInfo.Type lastLobby
    {
        get { return _lastLobby; }
        set
        {
            _lastLobby = value;
            LobbyInfo.updateLobbyOptionsUnlockedState(value);
            LobbyGame.setSpecificLobbyFeatures();
        }
    }

    public static void resetStaticClassData()
    {
        // czablocki - 2/2020: This is occasionally showing up in BugSnag SIGABRT breadcrumbs:
        // Message: "Glb.reinitializeGame() : Exception during reset of LobbyLoader"
        lastLobby = LobbyInfo.Type.MAIN;
        wasPausedDuringLoading = false;
    }
    
    public static void NotePauseOccurred()
    {
        wasPausedDuringLoading = true;
    }

    // Scheduler function to be called when the dialog is finished closing.
    public static void returnToLobbyAfterDialogCloses(Dict args)
    {
        Scheduler.removeFunction(returnToLobbyAfterDialogCloses);
        if (args != null && args.containsKey(D.TYPE))
        {
            lastLobby = (LobbyInfo.Type)args[D.TYPE];
        }
        Overlay.instance.setButtons(true);
        Overlay.instance.top.clickLobbyButton();
    }

    // Return to the VIP lobby from an open dialog,
    // which may or may not be while a game is loaded.
    public static void returnToNewLobbyFromDialog(bool shouldForceRefresh, LobbyInfo.Type lobbyType)
    {
        if (!shouldForceRefresh && GameState.isMainLobby)
        {
            // If we know it's safe to just go into the new lobby room from the main lobby,
            // without the need to refresh, then let's do it the smooth way.
            if (lastLobby != lobbyType && MainLobby.instance != null)
            {
                if (lobbyType == LobbyInfo.Type.VIP)
                {
                    // At this point, assuming that the only other type of lobby is the main lobby (MainLobby).
                    MainLobby.instance.StartCoroutine(MainLobby.instance.transitionToVIPLobby());
                }
                else if (lobbyType == LobbyInfo.Type.MAX_VOLTAGE)
                {
                    MainLobby.instance.StartCoroutine(MainLobby.instance.transitionToMaxVoltageLobby());
                }
            }
        }
        else
        {
            lastLobby = lobbyType;

            if (GameState.isMainLobby)
            {
                // In the unlikely situation that we're already in the lobby,
                // refresh the lobby to show new lobby games as unlocked,
                // then go straight into the new lobby room.
                Scheduler.addFunction(refreshAndViewAfterDialogCloses);
            }
            else if (GameState.game != null)
            {
                // (more likely) In a game. Go to the new lobby now.
                if (SlotBaseGame.instance != null)
                {
                    // Disable autospin right away, to make sure another spin doesn't auto start.
                    SlotBaseGame.instance.autoSpins = 0;
                }

                // Need to use the Scheduler because trying to call clickLobbyButton() right now will do nothing since the dialog is still technically open.
                Scheduler.addFunction(returnToLobbyAfterDialogCloses);
            }
        }
    }

    // Scheduler function to be called when the dialog is finished closing.
    private static void refreshAndViewAfterDialogCloses(Dict args)
    {
        Scheduler.removeFunction(refreshAndViewAfterDialogCloses);
        Loading.show(Loading.LoadingTransactionTarget.LOBBY);
        Glb.loadLobby();
    }

    // It takes a bit to load Main Lobby prefabs, we will load and cache it once
    public static void loadMainLobbyPrefabsAsync()
    {
        // If we have not started loading prefabs yet, do it now.
        // This function might be already called.  We have check here to prevent the code from loading the prefabs multiple times.
        if (_lobbyMainPrefab == null && !isLoadingMainLobbyPrefabInProgress)
        {
            isLoadingMainLobbyPrefabInProgress = true;
            RoutineRunner.instance.StartCoroutine(SkuResources.loadFromMegaBundleWithCallbacksAsync(MainLobbyV3.LOBBY_PREFAB_PATH,
                onMainLobbyLoaded, onMainLobbyFailed));
        }

        if (_lobbyBottomOverlayPrefab == null && !isLoadingLobbyBottomOverlayPrefabInProgress)
        {
            isLoadingLobbyBottomOverlayPrefabInProgress = true;
            RoutineRunner.instance.StartCoroutine(SkuResources.loadFromMegaBundleWithCallbacksAsync(MainLobbyBottomOverlay.prefabPath,
                onMainLobbyLoaded, onMainLobbyFailed));
        }
    }

    // Instantiates the main lobby, which may be automatically done when entering the lobby scene,
    // or when exiting the another lobby and returning to the main lobby.
    public IEnumerator createMainLobby()
    {
        loadMainLobbyPrefabsAsync();
        
        while (_lobbyMainPrefab == null || _lobbyBottomOverlayPrefab == null)
        {
            yield return null;
        }

        CommonGameObject.instantiate(_lobbyMainPrefab);
        // Drop the prefab immediately after instantiating it to release memory hold by it
        _lobbyMainPrefab = null;
        
        CommonGameObject.instantiate(_lobbyBottomOverlayPrefab);
        // Drop the prefab immediately after instantiating it to release memory hold by it
        _lobbyBottomOverlayPrefab = null;
    }

    public void Awake()
    {
        // Putting this above setting the instance so the the logic in DoSomethingGame.cs
        // and DoSomethingVipRoom.cs properly set the preferences before the LoaderLoader checks them
        // on a cold start of the game.
        URLStartupManager.Instance.processUrlActions();
        instance = this;

        autoLaunchGameResult = LobbyGame.LaunchResult.NO_LAUNCH;
        string gameKey = "";

        if (ExperimentWrapper.LoadGameFTUE.isInExperiment && GameExperience.totalSpinCount == 0)
        {
            gameKey = ExperimentWrapper.LoadGameFTUE.gameKey;
        }

        if (gameKey == "")
        {
            // If no FTUE game, try the player pref, which could be set in various ways.
            PreferencesBase prefs = SlotsPlayer.getPreferences();
            gameKey = prefs.GetString(Prefs.AUTO_LOAD_GAME_KEY, "");
        }

#if UNITY_EDITOR
        if (gameKey == ""
            && PlayerPrefsCache.GetInt(DebugPrefs.LAUNCH_DIRECTLY_INTO_GAME, 0) > 0
            && (PlayerPrefsCache.GetInt(DebugPrefs.LAUNCH_DIRECTLY_INTO_GAME_ALWAYS, 0) > 0 || !loadedDirectlyOnce))
        {
            loadedDirectlyOnce = true;
            gameKey = PlayerPrefsCache.GetString(DebugPrefs.LAUNCH_DIRECTLY_GAME_KEY);

            LobbyGame game = LobbyGame.find(gameKey);
            if (game == null)
            {
                Debug.LogError("launchDirectlyGameKey '" + gameKey + "' not found in LobbyGame.find()");
            }
        }
#endif

        if (gameKey != "")
        {
            // If a game is to be loaded automatically,
            // then just do that here instead of instantiating a lobby.
            PreferencesBase prefs = SlotsPlayer.getPreferences();
            prefs.SetString(Prefs.AUTO_LOAD_GAME_KEY, "");
            prefs.Save();

            SlotAction.setLaunchDetails("auto_load");

            LobbyGame game = LobbyGame.find(gameKey);
            if (game != null)
            {
                // Update the unlock status before launching in, since it seems like
                // that isn't happening if you don't go to the lobby now.
                game.setIsUnlocked();
                autoLaunchGameResult = game.askInitialBetOrTryLaunch(false);
                NGUIExt.enableAllMouseInput();
            }
        }

        if (ExperimentWrapper.RoyalRush.isInExperiment && RoyalRushEvent.instance.rushInfoList.Count > 0)
        {
            // loading deferred?
            if (!AssetBundleManager.shouldLazyLoadBundle("royal_rush"))
            {
                addSecondaryBundleCache("royal_rush_lobby", false, true);
            }
        }

        if (!AssetBundleManager.shouldLazyLoadBundle("max_voltage"))
        {
            //addSecondaryBundleCache("max_voltage", false, true);
            //addSecondaryBundleCache("main_snd_max_voltage", false);
        }

        if (PremiumSlice.instance != null && PremiumSlice.instance.hasOffer() && !AssetBundleManager.isBundleCached("premium_slice"))
        {
            addSecondaryBundleCache("premium_slice");
        }

        if (EUEManager.isEnabled && !EUEManager.isComplete && !AssetBundleManager.isBundleCached("eue_ftue"))
        {
            addSecondaryBundleCache("eue_ftue", true, true);
        }
        
        if (ExperimentWrapper.Slotventures.isInExperiment &&
            (EueFeatureUnlocks.hasFeatureUnlockData("sv_challenges")|| CampaignDirector.isCampaignEnabled(SlotventuresChallengeCampaign.CAMPAIGN_ID)))
        {
            LobbyAssetData svLobbyAssetData = SlotventuresLobby.assetData;
            addAssetDownload(svLobbyAssetData.portalPrefabPath, svLobbyAssetData.bundleLoadSuccess, svLobbyAssetData.bundleLoadFailure);
            addAssetDownload(svLobbyAssetData.mainLobbyOptionPath, svLobbyAssetData.bundleLoadSuccess, svLobbyAssetData.bundleLoadFailure);

            if (AssetBundleManager.isValidBundle(string.Format("slotventures_sounds_{0}", svLobbyAssetData.themeName)))
            {
                addSecondaryBundleCache(string.Format("slotventures_sounds_{0}", svLobbyAssetData.themeName), true);
            }
            
            addSecondaryBundleCache("slotventures_common_audio", true);
        }

        if(!AssetBundleManager.isBundleCached(NewDailyBonusDialog.NEW_DAILY_BONUS_BUNDLE))
        {
            addSecondaryBundleCache(NewDailyBonusDialog.NEW_DAILY_BONUS_BUNDLE);
        }

        if (VirtualPetsFeature.instance != null && VirtualPetsFeature.instance.isEnabled)
        {
            if (!AssetBundleManager.isBundleCached(VirtualPetsFeature.PET_DAILY_BONUS_BUNDLE))
            {
                addSecondaryBundleCache(VirtualPetsFeature.PET_DAILY_BONUS_BUNDLE);
            }
        }
        // load all assets for challenge lobby campaigns
       foreach (LobbyAssetData assetData in ChallengeLobby.lobbyAssetDataList)
        {
            if (CampaignDirector.isCampaignEnabled(assetData.campaignName) &&
                !AssetBundleManager.shouldLazyLoadBundle(assetData.bundleName))
            {
                addAssetDownload(assetData.lobbyPrefabPath, assetData.bundleLoadSuccess, assetData.bundleLoadFailure);
                addAssetDownload(assetData.optionPrefabPath, assetData.bundleLoadSuccess, assetData.bundleLoadFailure);
                addAssetDownload(assetData.jackpotPrefabPath, assetData.bundleLoadSuccess, assetData.bundleLoadFailure);
                addAssetDownload(assetData.portalPrefabPath, assetData.bundleLoadSuccess, assetData.bundleLoadFailure);
                addAssetDownload(assetData.sideBarPrefabPath, assetData.bundleLoadSuccess, assetData.bundleLoadFailure);

                if (!string.IsNullOrEmpty(assetData.mainLobbyOptionPath))
                {
                    addAssetDownload(assetData.mainLobbyOptionPath, assetData.bundleLoadSuccess,
                        assetData.bundleLoadFailure);
                }

                if (AssetBundleManager.isValidBundle(assetData.bundleName))
                {
                    addSecondaryBundleCache(assetData.bundleName, true);
                }

                if (AssetBundleManager.isValidBundle("main_snd_" + assetData.bundleName))
                {
                    addSecondaryBundleCache("main_snd_" + assetData.bundleName, true);
                }
            }
        }

        if (EliteManager.isActive && !AssetBundleManager.isBundleCached(EliteManager.TRANSITION_BUNDLE_NAME))
        {
            addSecondaryBundleCache(EliteManager.TRANSITION_BUNDLE_NAME, false, true);
        }
        
        addSecondaryBundleCache("weekly_race_sounds", skipBundleMapping:true);
        addSecondaryBundleCache("weekly_race_daily_rival_sounds", skipBundleMapping:true);

        // Start all the downloads.
        foreach (string assetPath in assetBundlesToDownloadFirst.Keys)
        {
            AssetBundleManager.load(this, assetPath, assetBundleSuccess, assetBundleFailure, blockingLoadingScreen:true);
        }

        foreach (NonBlockingBundleLoad secondaryBundle in secondaryAssetBundlesToCache)
        {
            AssetBundleManager.downloadAndCacheBundle(secondaryBundle.bundleName, secondaryBundle.keepCached, false, secondaryBundle.skipBundleMapping);
        }

        if (ToasterManager.instance != null)
        {
            if (!ToasterManager.instance.arePrefabsLoaded)
            {
                ToasterManager.instance.loadPrefabs();
            }
        }
        else
        {
            Debug.LogErrorFormat(
                "LobbyLoader.cs -- Awake() -- trying to call loadPrefabs but the toaster manager hasn't finished loading yet.");
        }

        // This will immediately create the lobby if there are no downloads to do.
        checkForFinishedBundleDownloads();
    }

    public static void onMainLobbyLoaded(string assetPath, Object obj, Dict data = null)
    {
        if (assetPath == MainLobbyV3.LOBBY_PREFAB_PATH)
        {
            _lobbyMainPrefab = obj as GameObject;
            isLoadingMainLobbyPrefabInProgress = false;
        }

        if (assetPath == MainLobbyBottomOverlay.prefabPath)
        {
            _lobbyBottomOverlayPrefab = obj as GameObject;
            isLoadingLobbyBottomOverlayPrefabInProgress = false;
        }
    }

    public static void onMainLobbyFailed(string assetPath, Dict data = null)
    {
        ExperimentWrapper.LobbyV3.forceEnabled(false);
        if (assetPath == MainLobbyV3.LOBBY_PREFAB_PATH)
        {
            isLoadingMainLobbyPrefabInProgress = false;
        }

        if (assetPath == MainLobbyBottomOverlay.prefabPath)
        {
            isLoadingLobbyBottomOverlayPrefabInProgress = false;
        }
        
        Debug.LogError("Failed to download Lobby Prefab: " + assetPath);
    }

    public void addSecondaryBundleCache(string bundleName, bool keepCached = false, bool skipBundleMapping = false)
    {
        secondaryAssetBundlesToCache.Add(new NonBlockingBundleLoad(bundleName, keepCached, skipBundleMapping));
    }

    // Called whenever an asset bundle was finished downloading, whether successful or not.
    public void finishBundleDownload()
    {
        finishedBundleDownloads++;
        checkForFinishedBundleDownloads();
    }

    // Instantiates the VIP lobby, which may be automatically done when entering the lobby scene,
    // or when exiting the another lobby and going to the VIP lobby.
    public void createVIPLobby()
    {
#if USE_LOAD_ON_REQUEST_LOBBY
        if (ExperimentWrapper.VIPLobbyRevamp.isInExperiment)
        {
            AssetBundleManager.downloadAndCacheBundle("vip_lobby", blockingLoadingScreen:true);
            //Do loading screen before this
            StartCoroutine(WaitForPrefab_VIPLobby());
        }
#else
		if (ExperimentWrapper.VIPLobbyRevamp.isInExperiment && VIPLobbyHIRRevamp.IsActive())
		{
			CommonGameObject.instantiate(VIPLobbyHIRRevamp.lobbyPrefab);
		}
#endif
    }

    public void createMaxVoltageLobby()
    {
#if USE_LOAD_ON_REQUEST_LOBBY
        AssetBundleManager.load(MaxVoltageLobbyHIR.LOBBY_PREFAB_PATH, maxVoltageLoadSuccess, maxVoltageLoadFailed,
            isSkippingMapping: true, fileExtension: ".prefab", blockingLoadingScreen:true);
        AssetBundleManager.downloadAndCacheBundle("main_snd_max_voltage");
#else
		if (MaxVoltageLobbyHIR.IsActive())
		{
			CommonGameObject.instantiate(MaxVoltageLobbyHIR.lobbyPrefab);
		}
#endif
    }

    public void createSlotventureLobby()
    {
        //CommonGameObject.instantiate(SlotventuresLobby.assetData.lobbyPrefab);
        AssetBundleManager.load(SlotventuresLobby.assetData.lobbyPrefabPath, svLobbyLoadSuccess, svLobbyLoadFailed, blockingLoadingScreen:true);
    }
    
    private void svLobbyLoadSuccess(string assetPath, Object obj, Dict data = null)
    {
        CommonGameObject.instantiate(obj);
    }

    // Used by LobbyLoader to preload asset bundle.
    private void svLobbyLoadFailed(string assetPath, Dict data = null)
    {
        Debug.LogError("Failed to download max voltage asset: " + assetPath + ".\nMax Voltage lobby option will not appear.");
    }

    public void createLOZLobby()
    {
        Instantiate(ChallengeLobby.assetData.lobbyPrefab);
    }

    public void createChallengeLobby(string campaignName)
    {
        LobbyAssetData assetData = ChallengeLobby.findAssetDataForCampaign(campaignName);
        CommonGameObject.instantiate(assetData.lobbyPrefab);
    }

    // Creates the necessary lobby when everything is ready.
    private IEnumerator createLobby()
    {
        // This will be useful if we want to say, set the lobby type at the last possible moment or something.
        // Or do something right before we attempt to load
        if (lobbyLoadEvent != null)
        {
            lobbyLoadEvent();
            lobbyLoadEvent = null;
        }

        // The result could be ASK_INITIAL_BET, which means the dialog has been shown,
        // which means the lobby needs to load and the loading screen has to be hidden
        // before the bet can be chosen and the game actually launched.

        // Need to check the Gamestate also for a race condition where the slot game finshed loading before we hit this coroutine.
        // SlotStartup.Awake resets autoLaunchGameResult to be NO_LAUNCH which leads to the lobby being created on top of the slot game we just loaded.
        if (autoLaunchGameResult != LobbyGame.LaunchResult.LAUNCHED)
        {
            switch (lastLobby)
            {
                case LobbyInfo.Type.MAIN:
                    yield return StartCoroutine(createMainLobby());
                    break;

                case LobbyInfo.Type.VIP:
                    createVIPLobby();
                    break;

                case LobbyInfo.Type.MAX_VOLTAGE:
                    createMaxVoltageLobby();
                    break;

                case LobbyInfo.Type.SLOTVENTURE:
                    createSlotventureLobby();
                    break;

                case LobbyInfo.Type.LOZ:
                    if (GameState.game == null)
                    {
                        createLOZLobby();
                    }

                    break;

                case LobbyInfo.Type.SIN_CITY:
                    // We can't fall through to the default because at this point the current campaign isn't set, no could it be
                    // if we're logging in.
                    if (GameState.game == null)
                    {
                        createChallengeLobby("challenge_sin_city_strip");
                    }

                    break;

                default:
                    if (ChallengeLobbyCampaign.currentCampaign != null)
                    {
                        createChallengeLobby(ChallengeLobbyCampaign.currentCampaign.campaignID);
                    }
                    // what happened...
                    else
                    {
                        yield return StartCoroutine(createMainLobby());
                    }

                    break;
            }

            if (RateMe.pendingPurchasePrompt)
            {
                RateMe.checkAndPrompt(RateMe.RateMeTrigger.PURCHASE);
            }

            Userflows.logWebGlLoadingStep("end"); // The final WebGLLoading step
            StatsManager.Instance.LogStartUpStep("LobbyDisplayComplete");
            StatsManager.Instance.FlushLoadTimeLog(wasPausedDuringLoading);

            // If there is not a game to load immediately, hide the loading screen,
            // otherwise we still need it for loading the game that is being autoloaded.
            // Wait a couple frames to make sure the lobby has finished instantiating and initializing,
            // which is particularly important on slow devices.
            yield return null;
            yield return null;
            
            Loading.hide(Loading.LoadingTransactionResult.SUCCESS);
            AssetBundleManager.loadMissingFeatures();
            if (Collectables.missingBundles != null && Collectables.missingBundles.Count > 0)
            {
                Collectables.loadCollectionsBundles(true);
            }
            if (PowerupsManager.isPowerupsEnabled)
            {
                PowerupsManager.preLoadPrefabs();
            }
        }
    }

    private IEnumerator WaitForPrefab_VIPLobby()
    {
        while (VIPLobbyHIRRevamp.lobbyPrefab == null)
        {
            yield return null;
        }

        CommonGameObject.instantiate(VIPLobbyHIRRevamp.lobbyPrefab);

        if (OnLobbyPrefabLoaded != null)
        {
            OnLobbyPrefabLoaded();
            OnLobbyPrefabLoaded = null;
        }

        yield break;
    }

    private void addAssetDownload(string assetPath, AssetLoadDelegate successCallback, AssetFailDelegate failCallback)
    {
        if (!assetBundlesToDownloadFirst.ContainsKey(assetPath))
        {
            assetBundlesToDownloadFirst.Add(assetPath, new AssetBundlePreload(
                successCallback,
                failCallback
            ));
        }
        else
        {
            Bugsnag.LeaveBreadcrumb("LobbyLoader.Awake - Trying to double load: " + assetPath);
        }
    }

    private void assetBundleSuccess(string assetPath, Object obj, Dict data = null)
    {
        if (this == null)
        {
            return;
        }

        AssetBundlePreload preload = null;
        if (assetBundlesToDownloadFirst.TryGetValue(assetPath, out preload))
        {
            preload.successCallback(assetPath, obj, data);
        }

        finishBundleDownload();
    }

    private void assetBundleFailure(string assetPath, Dict data = null)
    {
        AssetBundlePreload preload = null;
        if (assetBundlesToDownloadFirst.TryGetValue(assetPath, out preload))
        {
            preload.failCallback(assetPath, data);
        }

        finishBundleDownload();
    }

    // Call this immediately and whenever an asset bundle has finished downloading.
    private void checkForFinishedBundleDownloads()
    {
        if (finishedBundleDownloads != -1 && finishedBundleDownloads >= assetBundlesToDownloadFirst.Count)
        {
            finishedBundleDownloads = -1; // Indicates that this has already happened, to prevent multiple calls.
            // Finished downloading all the necessary bundles. Now create the lobby.
            StartCoroutine(createLobby());
        }
    }

    private void maxVoltageLoadSuccess(string assetPath, Object obj, Dict data = null)
    {
        CommonGameObject.instantiate(obj);

        if (OnLobbyPrefabLoaded != null)
        {
            OnLobbyPrefabLoaded();
            OnLobbyPrefabLoaded = null;
        }
    }

    // Used by LobbyLoader to preload asset bundle.
    private void maxVoltageLoadFailed(string assetPath, Dict data = null)
    {
        Debug.LogError("Failed to download max voltage asset: " + assetPath +
                       ".\nMax Voltage lobby option will not appear.");
    }

    // Simple data structure to hold info about asset bundles to preload.
    private class AssetBundlePreload
    {
        public AssetFailDelegate failCallback = null;
        public AssetLoadDelegate successCallback = null;

        public AssetBundlePreload(AssetLoadDelegate successCallback, AssetFailDelegate failCallback)
        {
            this.successCallback = successCallback;
            this.failCallback = failCallback;
        }
    }

    private class NonBlockingBundleLoad
    {
        public bool keepCached = false;
        public bool skipBundleMapping = false;
        public string bundleName = "";

        public NonBlockingBundleLoad(string _bundleName, bool _keepCached = false, bool _skipBundleMapping = false)
        {
            bundleName = _bundleName;
            keepCached = _keepCached;
            skipBundleMapping = _skipBundleMapping;
        }
    }
}