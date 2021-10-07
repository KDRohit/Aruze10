using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using Com.HitItRich.EUE;
using Zynga.Core.Util;
using Zynga.Zdk;
using Facebook.Unity;
using Zynga.Zdk.Services.Identity;
using Zynga.Core.Tasks;

/**
This is part of the Loading scene, which is like a springboard for loading the rest of the game.
*/
public class GameLoader : IDependencyInitializer
{
	private static GameLoader _instance;
	public static GameLoader Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new GameLoader();
			}
			return _instance;
		}
	}
	public GameObject persistentObjects = null; ///< This object persists throughout the life of the game, until resetting and returning to Startup Logic scene.

	private InitializationManager _initMgr;

	private const string HAS_OFFERS_ADVERTISER_ID = "881";
	private const string HAS_OFFERS_CONVERSION_KEY = "e6e4a7f821d4db7d704bae674686277d";

	private const float TIMEOUT_WAIT = 20.0f; // 20 seconds

	public static void init()
	{
		Bugsnag.LeaveBreadcrumb("GameLoader - init() beginning");
		
		GameTimer.init();

		// Register some persistent server event delegates.
		Server.registerEventDelegates();
		SlotsPlayer.registerEventDelegates();
		SlotBaseGame.registerEventDelegates();
		LevelUpDialog.registerEventDelegates();
		CollectReward.registerEventDelegates();
		VIPPhoneCollectDialog.registerEventDelegates();
		GenericRewardGrantDialog.registerEventDelegates();
		Buff.registerEventDelegates();
		BuffDef.registerEventDelegates();
		ReactivateFriend.registerReactivateFriendDelegates();
		LuckyDealDialog.registerEventDelegates();
		FlashWebGLThankYouDialog.registerEventDelegates();
		LowWalletCoinGrantDialog.registerEventDelegates();
		
		// always add server handlers, in case there's a "lost" event we need to display
		CampaignDirector.addServerHandlers();
		WeeklyRaceDirector.registerEvents();

		// Setup support for pasting values in webgl. Requires JS assistance.
		WebglPasteSupport.Init();

		Bugsnag.LeaveBreadcrumb("GameLoader - init() finished");
	}
	
	void Initialize()
	{
		// Data.debugMode is a transient dependency via BasicInfoLoader which calls
		// LoadBasicData.getBasicGameData() which calls Data.loadConfig()
		if (Data.debugMode)
		{
			DevGUIMenu.populateAll();
		}

		if (Application.isEditor || Application.genuine)
		{
#if UNITY_EDITOR || UNITY_WEBGL
			// This has not worked at all on Android or iOS since Unity 5.1, and now crashes iOS when called.
			Application.runInBackground = true;
#endif
			beginGameStartup();
			Bugsnag.LeaveBreadcrumb("Starting the game cold");
		}
		else
		{
			// The application binary has been editor post-build!
			Debug.LogError("Game has been altered after compilation!");
		}
	}
	
	/// Begins the game's startup process
	private void beginGameStartup()
	{		
		if (Application.internetReachability != NetworkReachability.NotReachable)
		{
			login();
		
			StatsManager.Instance.LogStartUpStep("PreLoadStart");
			_initMgr.InitializationComplete(this);
		}
		else
		{
			Debug.LogWarning(string.Format("No network connection detected. <{0}>", Application.internetReachability));
			GenericDialog.showDialog(
				Dict.create(
					D.TITLE, Localize.text("mobile_web_connection_failed_title"),
					D.MESSAGE,  Localize.text("mobile_web_connection_failed_message"),
					D.REASON, "game-loader-no-internet",
					D.CALLBACK, new DialogBase.AnswerDelegate( (args) => { Glb.resetGame("No internet connection detected."); } )
				),
				SchedulerPriority.PriorityType.IMMEDIATE
			); 
		}
	}

	/// Gets the player data and global static data, then loads the player's Munchkinland.
	public void login()
	{
		if (!MobileUIUtil.isAllowedDevice)
		{
			// Don't login or do anything else if not on a compatible device.
			// The dialog showing a friendly message has already been shown.
			return;
		}
		
		string zid = ZdkManager.Instance.Zsession.Zid + "";
		if (!string.IsNullOrEmpty(Data.overrideZid) && Data.overrideZid != "none")
		{
			zid = Data.overrideZid;
		}
		// Start up requests to the server to get the response data
		LoadPlayerData.getLoginData(zid, ZdkManager.Instance.Zsession, LoginCallback);
	}

	private void acceptTOSCallback(Dict args)
	{
		Loading.show(Loading.LoadingTransactionTarget.LOBBY);
		RoutineRunner.instance.StartCoroutine(finishLoading());
	}
	
	/// To be called when logging in is done. Have to do this since we can't use coroutines in anonymous functions in LoadPlayerData
	public void LoginCallback()
	{
		// Only flag the user loading as done if it succeeded,
		// which is indicated by whether a facebook object exists for it.
		// If it gets here and failed, then the whole game is botched anyway,
		// and an error dialog has already been shown.

		PreferencesBase preferences = SlotsPlayer.getPreferences();
		
		//set login timestamp in user activity manager
		UserActivityManager.instance.onLogin();

		//dt_GPTODO: need to update this for stats
		if (Data.isGlobalDataSet && SlotsPlayer.instance.socialMember != null) 
		{
			if (SlotsPlayer.instance.hasCOPPADeleteRequest)
			{
				Debug.LogWarning("Has requested COPPA delete");
				GDPRDialog.showCOPPADeleteDialog(
					new DialogBase.AnswerDelegate( (args) => {
						Common.QuitApp();
					}));
			}
			else if (Data.liveData.getBool("GDPR_CLIENT_ENABLED", false))	
			{
				if (SlotsPlayer.instance.isGDPRSuspend)
				{
					Debug.LogWarning("Has requested data delete");
					GDPRDialog.showUserSuspendDialog(
						new DialogBase.AnswerDelegate( (args) => {
							Common.QuitApp();
						}),
						SchedulerPriority.PriorityType.IMMEDIATE);
				}
				else if (SlotsPlayer.instance.hasGDPRDeleteRequest)
				{
					Debug.LogWarning("Has requested data delete");
					GDPRDialog.showUserDeleteDialog(
						new DialogBase.AnswerDelegate( (args) => {
							Common.QuitApp();
						}),
						SchedulerPriority.PriorityType.IMMEDIATE);
				}
				else if (SlotsPlayer.instance.allowedAccess)
				{
					if (SlotsPlayer.instance.didAcceptTOS)
					{
						//server says we accepted the tos
						acceptTOSCallback(null);
					}
					else
					{
#if UNITY_WEBGL && !UNITY_EDITOR
						TOSDialog.showDialog(acceptTOSCallback, false);
#else
						int tosVersion = preferences.GetInt(Prefs.GDPR_TOS_VIEWED);
						if (tosVersion == 0 || tosVersion != Data.liveData.getInt("TOS_UPDATE_RUNTIME_VERSION", 0))
						{
							TOSDialog.showDialog(acceptTOSCallback, false);
						}
						else
						{
							//the cached version of the tos we displayed before we got here was correct, accept the tos.
							//This case should only occur when the user accepts the tos, then restarts the application before logging in (the tos accept will not go through in that case).
							PlayerAction.acceptTermsOfService();
							acceptTOSCallback(null);
						}
#endif
					}
				}
				else
				{
					showUserNotAllowedAccess();
				}
			}
			else if (SlotsPlayer.instance.allowedAccess)
			{
				acceptTOSCallback(null);
			}
			else
			{
				showUserNotAllowedAccess();
			}

		}
		else
		{
			Debug.LogError("Error in login flow.");
		}

		if (Glb.isNew && (SlotsPlayer.isFacebookUser || SlotsPlayer.IsAppleLoggedIn) && !Data.webPlatform.IsDotCom)
		{
			ZisAccountCreatedDialog.showDialog();
		}

		if (Glb.showEmailOptIn != 0)
		{
			Debug.LogFormat("AppleLogin: Show email optin reward {0}", Glb.showEmailOptIn);
			ZisEmailOptInDialog.showDialog(Dict.create(
				D.EMAIL, PackageProvider.Instance.Authentication.Flow.Account.UserAccount.Email.Id,
				D.AMOUNT, Glb.showEmailOptIn 
				)
			);
		}
		else
		{
			Debug.Log("AppleLogin: Not showing email optin reward");
		}

		// This dialog pops up when the user  is FB logged in  and not email verified

		if (Glb.showVerifyEmailDialog && SlotsPlayer.isFacebookUser)
		{
			if (PackageProvider.Instance.Authentication.Flow != null && PackageProvider.Instance.Authentication.Flow.Account.UserAccount.Email != null && !PackageProvider.Instance.Authentication.Flow.Account.UserAccount.Email.Verified)
			{
				string email = PackageProvider.Instance.Authentication.Flow.Account.UserAccount.Email.Id;
				if (ExperimentWrapper.ZisPhase2.isInExperiment && Glb.showEditButton)
				{
					GenericDialog.showDialog(
							   Dict.create(
									   D.TITLE, Localize.textOr("Verify Your Email", "Verify Your Email"),
									   D.MESSAGE, "Go to " + email + " and verify your email",
									   D.REASON, "social-manager-connection-error",
									   D.OPTION1, Localize.textOr("Resend Verification", "Resend Verification"),
									   D.OPTION2, Localize.textOr("Edit email", "Edit email"),
									   D.CALLBACK, new DialogBase.AnswerDelegate((args) =>
									   {
										   if (args != null)
										   {
											   if ((string)args.getWithDefault(D.ANSWER, "") == "1")
											   {
												   //Resend verification
												   logSplunk("verify-dialog-startup", "email-verify-pressed", email);
												   SocialManager.Instance.onVerifyPressed();


											   }
											   else if ((string)args.getWithDefault(D.ANSWER, "") == "2")
											   {
												   //startover again
												   logSplunk("verify-dialog-startup", "start-over-pressed", "email-change");
												   SocialManager.Instance.emailChangePressed();
											   }
										   }
									   }
									   )
								   ),
								   SchedulerPriority.PriorityType.IMMEDIATE
							   );
				}
				else
				{
					GenericDialog.showDialog(
							   Dict.create(
									   D.TITLE, Localize.textOr("Verify Your Email", "Verify Your Email"),
									   D.MESSAGE, "Go to " + email + " and verify your email",
									   D.REASON, "social-manager-connection-error",
									   D.OPTION1, Localize.textOr("Resend Verification", "Resend Verification"),
									   D.CALLBACK, new DialogBase.AnswerDelegate((args) =>
									   {
										   if (args != null)
										   {
											   if ((string)args.getWithDefault(D.ANSWER, "") == "1")
											   {
												   //Resend verification
												   logSplunk("verify-dialog-startup-noexp", "email-verify-pressed-noexp", email);
												   SocialManager.Instance.onVerifyPressed();


											   }
										   }
									   }
									   )
								   ),
								   SchedulerPriority.PriorityType.IMMEDIATE
							   );
				}
			}
		}

	}

	// Method for logging channel is being logged into
	private static void logSplunk(string name, string key, string value)
	{
		Dictionary<string, string> extraFields = new Dictionary<string, string>();
		extraFields.Add(key, value);
		SplunkEventManager.createSplunkEvent("GameLoader", name, extraFields);
	}

	private void showUserNotAllowedAccess()
	{
		Debug.LogWarning("Player is prohibited from playing. Will quit.");
		GenericDialog.showDialog(
			Dict.create(
				D.TITLE, Localize.textOr("access_denied", "Access Denied"),
				D.MESSAGE, Localize.textOr("access_denied_locked_cs", "This account is currently blocked from playing. Please contact customer support.")
					+ "\n\nZID " + SlotsPlayer.instance.socialMember.zId,
				D.OPTION1, Localize.textOr("help_support_button", "Support"),
				D.OPTION2, Localize.textOr("quit", "Quit"),
				D.REASON, "game-loader-player-prohibited",
				D.CALLBACK, new DialogBase.AnswerDelegate( (args) => {
					if ((string)(args[D.ANSWER]) == "1")
					{
						Common.openSupportUrl(Glb.HELP_LINK_SUPPORT);
					}
					Common.QuitApp();
				} )
			),
			SchedulerPriority.PriorityType.BLOCKING
		); 	
	}
	/// <summary>
	/// To be called when Glb.resetGame() is called from initial loading failures. i.e. the loading screen timed out
	/// </summary>
	public void abortLoading()
	{
		RoutineRunner.instance.StopCoroutine("finishLoading");
	}
	
	/// Both the player and global data are finished loading.
	/// Do stuff that has to happen after both are finished.
	private IEnumerator finishLoading()
	{
		Debug.LogFormat($"ZAPLOG -- finishLoading -- here! at time = {Time.realtimeSinceStartup}");
		PreferencesBase preferences = SlotsPlayer.getPreferences();
		if (!isValidSession("Session is invalid at start of finishLoading()."))
		{
			Debug.LogFormat("ZAPLOG -- finishLoading -- invalid session!");
			yield break;
		}
		
		if (Overlay.instance != null)
		{
			// Prevent double-logins from both being processed on the client.
			Debug.LogFormat("ZAPLOG -- finishLoading -- overlay instance is null!");
			yield break;
		}

		Userflows.logWebGlLoadingStep("gameloader_running");
		
		// send the static buff definition server action so we get an event containing the buff definitions as soon as we can
		// allowing us to late apply the active local buffs for the player
		BuffDef.init();
		PowerupsManager.init();
		ServerAction.processPendingActions(true);
		
		ExperienceLevelData.populateStartup();	// Must do this before instantiating the overlay, since the overlay uses this data in updateXP().
		// Doing this pretty early on in case anything reads this data.
		VIPLevel.setMaxLevel();
		int minRequiredLevel = -1;

		// VIP Micro event stuff:
	    VIPStatusBoostEvent.setup();
		
		// MCC -- moving this data instantiation to before the overlay so that we have the correct data.
		STUDAction.populateAll(Data.login.getJSON("stud"));
		PurchaseFeatureData.populateAll();
		STUDSale.populateAll(); // Now that we have loaded the STUD Actions, build the STUD Sale objects.

		if (ExperimentWrapper.FlashSale.enabled)
		{
			FlashSaleManager.init();
		}
		if (ExperimentWrapper.StreakSale.enabled)
		{
			StreakSaleManager.init();
		}

		// Instantiate the overlay UI after data is retrieved and processed,
		// so localization of static text can be done.
		yield return RoutineRunner.instance.StartCoroutine(Overlay.createOverlay());
		Bugsnag.LeaveBreadcrumb("GameLoader.finishLoading::Added the overlay prefab to the scene");
		// Wait one frame to guarantee that the Overlay.Awake() has been called to make sure the instance variables are defined.
		yield return null;
		
		BuyPagePerk.init();    // call after overlay is created since it affect UI.
		BuyPageDynamic.init(); // initializes the custom buy page surfacing.
		
		if (LoLa.versionUrl != "")
		{
			// This must be retrieved after experiment data, since it uses EOS for LoLa versioning.
			StatsManager.Instance.LogLoadTimeStart("GL_LOLARequest");
			yield return RoutineRunner.instance.StartCoroutine(LoLa.getDataFromS3());
			StatsManager.Instance.LogLoadTimeEnd("GL_LOLARequest");
		}

		if (ExperimentWrapper.DynamicMotdV2.isInExperiment)
		{
			StatsManager.Instance.LogLoadTimeStart("GL_MOTDRequest");
			yield return RoutineRunner.instance.StartCoroutine(DynamicMOTDFeature.instance.getDataFromS3());
			StatsManager.Instance.LogLoadTimeEnd("GL_MOTDRequest");
		}
		
		// Populate Game experience data.
		GameExperience.populateAll(Data.login.getJsonArray("player.slots_games"));

		// Must be done before initializing LoLa, so we know whether to create the LOZ Lobby.
		JSON[] baseChallenges = Data.login.getJsonArray("challenges", returnNullIfMissing: true);
		JSON richPassJSON = Data.login.getJSON("rich_pass");
		JSON[] allChallenges = null;
		if (richPassJSON != null)
		{
			if (baseChallenges == null || baseChallenges.Length == 0)
			{
				allChallenges = new JSON[1] { richPassJSON };
			}
			else
			{
				allChallenges = new JSON[baseChallenges.Length + 1];
				baseChallenges.CopyTo(allChallenges, 0);
				allChallenges[baseChallenges.Length] = richPassJSON;
			}	
		}
		else
		{
			allChallenges = baseChallenges;
		}
		
		//Collectables needs to init prior to challenges since challenges need to verify that collections is active to be able to award card packs
		Collectables.Instance.initPlayerCards(Data.login.getJSON("collectibles"));
		CampaignDirector.populateAll(allChallenges);
		// ZTrack calls at session start. Should be moved if a session ever bypasses this flow.
		StatsManager.Instance.LogStartUpStep("DataComplete");
		StatsManager.Instance.LogCount("start_session", "vip_status", VIPLevel.find(SlotsPlayer.instance.vipNewLevel).trackingName, "", "", "", SlotsPlayer.instance.vipPoints);
		StatsManager.Instance.LogCount("start_session", "sound", Audio.muteSound ? "off" : "on");

		// Make a nop call to awaken the AssetBundleManager.
		AssetBundleManager.unloadBundle("");
		//Do a game center auth check
		GameCenterManager.checkAndAuth();

		//Pop the FB connect dialog if we've tried to login and failed or if we see that we were previously connected but aren't anymore
		if (preferences.GetInt(Prefs.FACEBOOK_CONNECT_FAILED) == 1)
		{
			ZisFacebookConnectDialog.showDialog(null);
		}
		else if (preferences.GetInt(Prefs.HAS_FB_CONNECTED_SUCCESSFULLY) == 1 && (!SlotsPlayer.isFacebookUser || SlotsPlayer.instance.facebook == null))
		{
			ZisFacebookConnectDialog.showDialog(null);
		}

		if (SlotsPlayer.isAnonymous && LinkedVipProgram.instance.isConnected)
		{
			if (ExperimentWrapper.ZisPhase2.isInExperiment)
			{
				SocialManager.Instance.CreateAttach(AuthenticationMethod.ZyngaEmailUnverified);
			}
		}
	


#if UNITY_IPHONE
		if (NotificationManager.InitialPrompt)
		{
			// If this is the Initial Prompt then we want to only call that here if we are not in the
			// new user daily bonus experiment.
			if (!ExperimentWrapper.DailyBonusNewInstall.isInExperiment)
			{
				NotificationManager.ShowPushNotifSoftPrompt(true);
			}
		}
		else
		{
			// If this isnt the first prompt, then want the normal funcitonality where we call it here.
			NotificationManager.ShowPushNotifSoftPrompt(true);
		}
#else
		// For non-iOS Push Notifs we just register at load.
		NotificationManager.ShowPushNotifSoftPrompt(true);
#endif
		if (StatsManager.Instance != null)
		{
			StatsManager.Instance.LogVisit();
		}

		ProgressiveJackpot.update();

		Bugsnag.LeaveBreadcrumb("GameLoader.finishLoading::Waiting for the economy to finish loading");
		// Wait for the economy manager to finish loading.
		// This will happen when the 'FirstLoad' flag is cleared.
		// Alternatively, timeout.
		StatsManager.Instance.LogLoadTimeStart("GL_PaymentsLoad");
		float startWaitTime = Time.realtimeSinceStartup;
		
		while (Packages.PaymentsFirstLoad() && (Time.realtimeSinceStartup - startWaitTime < TIMEOUT_WAIT))
		{
			yield return null;
		}
		StatsManager.Instance.LogLoadTimeEnd("GL_PaymentsLoad");

		if (!isValidSession("Session is invalid after waiting for EconomyManager."))
		{
			Debug.LogFormat("ZAPLOG -- finishLoading -- ivalid economy manager!");
			yield break;
		}
		
		if (!Packages.PaymentsManagerEnabled())
		{
			Debug.LogError("Economy has not finished initializing. Game may be unstable. Continuing anyway.");

			// CRC - This is an incredible hack - but if we make it here, the economy has not yet loaded
			// properly and MECO hasn't had a chance to initialize the items yet.
			// So we force this data to get loaded - even though it's going to be incomplete.
			// The game should start for users - but if they try to purchase they will get the
			// "products not loaded" error message.

			// CRC - This is the new purchasable data:
			List<JSON> allPackages = new List<JSON>();
			allPackages.AddRange(Glb.popcornSalePackages);
			if (Glb.richPassPackages != null)
			{
				allPackages.AddRange(Glb.richPassPackages);
			}
			if (Glb.premiumSlicePackages != null)
			{
				allPackages.AddRange(Glb.premiumSlicePackages);
			}
			if (Glb.bonusGamePackages != null)
			{
				allPackages.AddRange(Glb.bonusGamePackages);
			}
			PurchasablePackage.populateAll(allPackages.ToArray());
		}

		// Only allow these systems to be populated once we've made a fair attempt to initialize the economy.
		// Economy data that these classes access (prices, etc) may not be correct or may be missing
		// if the economy failed to correctly initialize above.
		Bugsnag.LeaveBreadcrumb("GameLoader.finishLoading::Populating things like jackpot and game data");

		// ProgressiveJackpot data must be done after populating LobbyGameGroups (from global data) and Login data.
		ProgressiveJackpot.populateAll(Glb.progressiveJackpots);

		// Set sale notification.
		PurchaseFeatureData.setSaleNotification();

		// Bind the GameExperience object to each LobbyGame after it has been processed from player data.
		LobbyGame.bindGameExperience();

		// Do this before populating lobby options and removing unknown progressive games,
		// and before setAllLobbyFeatures() so we know that VIP game are progressive.
		VIPLevel.defineAvailableGames();

		// Must be called after bindGameExperience() and VIPLevel.defineAvailableGames().
		// BY 10-4-2018 removing this, once lastlobby is set we setup all the features, so this isn't needed
		//LobbyGame.setAllLobbyFeatures();

		// Get progress for all campaigns
		CampaignDirector.getProgress();
		// Get all weekly race data
		WeeklyRaceDirector.init(Data.login.getJSON("player.weekly_race"));

		// Do this after populating SlotResourceMap and binding game experience and LoLa.
		ProgressiveJackpot.removeUnknownGames();
	    LobbyGame.removeUnknownMysteryGiftGames();
		// Populate the lobby options with some of this data.
		// We no longer use SCAT lobby definitions.
		LobbyOption.populateAll(SlotResourceMap.map);

		//Needs the lobby options to be populated first
		if (CampaignDirector.richPass != null && CampaignDirector.richPass.isActive)
		{
			CampaignDirector.richPass.unlockSilverGames();
		}
		// Carousel data must be populated after STUDActions and any other data
		// that may affect the validity of a slide from being included.
		CarouselData.loginInit();
		
		// Get the friends now, since it didn't come in as part of the login data.
		if (!SocialMember.isFriendsPopulated)
		{
			// Get the friends now.
			PlayerAction.getFriendsList(SocialMember.populateAll);
		}

		// Process the MOTD data, which may mark some MOTD's as seen via server actions.
		MOTDFramework.processMotdQueue(Data.login.getJsonArray("motd"));

		Bugsnag.LeaveBreadcrumb("GameLoader.finishLoading::Getting VIP Status");
		NetworkAction.getVipStatus();

		// Post an empty actions batch to force retrieval of inbox items ASAP.
		// This must be done the first time after loading lobby data, or errors happen.
		// This will also force the above getFriendsList action,
		// getRainyDayData action, and marking seen MOTD's actions to be posted.
		ServerAction.processPendingActions(true);

		if (!BuffDef.isStaticBuffDataValid || !Buff.isPlayerBuffsApplied)
		{
			startWaitTime = Time.realtimeSinceStartup;
			while ((!BuffDef.isStaticBuffDataValid || !Buff.isPlayerBuffsApplied) && 
			       (Time.realtimeSinceStartup - startWaitTime < TIMEOUT_WAIT))
			{
				yield return null;
			}
			
			if (!BuffDef.isStaticBuffDataValid || !Buff.isPlayerBuffsApplied)
			{
				Glb.resetGame("Did not recieve Static Buff Defintions, must restart");
				Debug.LogFormat("ZAPLOG -- finishLoading -- static buff definitions issue!");
				yield break;
			}							
		}
		
		// Don't add auto-game-loading stuff here. That's handled in LobbyLoader.
		Glb.loadLobby();
		// Wait LobbyLoader.Awake() has been called to make sure the instance variables are defined.  so that download
		// requests for all necessary asset bundles for lobby can be made before moving on for better performance.
		// more asset bundles might be requested to download after this point, so we would like download requests for
		// lobby to start with priority.
		while (LobbyLoader.instance == null)
		{
			yield return null;
		}

		// Start this userflow here, tracks when the game is running (resets when game is reset)
		Userflows.flowStart("run_time");

		// Check for changed basicData URL.
		string currentBasicDataUrl = Data.basicDataUrl;
		string previousBasicDataUrl = preferences.GetString(Prefs.PREVIOUS_DATA_URL, "");
		if (!string.IsNullOrEmpty(previousBasicDataUrl) && previousBasicDataUrl != currentBasicDataUrl)
		{
			string message = string.Format(
				"We have detected you are upgrading across environments. Your Old Version is {0} and New Version is {1}. If you experience any issues, please uninstall and reinstall the application.", 
				preferences.GetString(Prefs.PREVIOUS_APP_VERSION, ""), Glb.clientVersion);
			// If we are changing evironments, pop a dialog telling the user that.
			GenericDialog.showDialog(
				Dict.create(
					D.TITLE, "Environment Changing",
					D.MESSAGE, message,
					D.REASON, "game-loader-env-changing",
					D.CALLBACK, new DialogBase.AnswerDelegate(
						(args) =>
						{
							preferences.SetString(Prefs.PREVIOUS_DATA_URL, currentBasicDataUrl);
							preferences.SetString(Prefs.PREVIOUS_APP_VERSION, Glb.clientVersion);
							preferences.Save();
						})),
					SchedulerPriority.PriorityType.IMMEDIATE
			);
		}
		else
		{
			// Set the pref to be the current url.
			preferences.SetString(Prefs.PREVIOUS_DATA_URL, currentBasicDataUrl);
			preferences.SetString(Prefs.PREVIOUS_APP_VERSION, Glb.clientVersion);
			preferences.Save();
		}


		// check for version discrepencies with the incentivized app update option
		IncentivizedUpdate.init();
		
		// Handy testing code.
		// Force it on or off artificially to test spinning with it in the opposite state as the backend.
//		string jsonString = "{\"show_dialog\": false, \"multiplier\": 3, \"end_time\": 1458179773 }";
//		SlotsPlayer.xpMultiplierOnEvent(new JSON(jsonString));
//		SlotsPlayer.xpMultiplierOffEvent(null);

#if UNITY_EDITOR && !ZYNGA_PRODUCTION
		int shouldZapResume = SlotsPlayer.getPreferences().GetInt(Zap.Automation.ZAPPrefs.SHOULD_RESUME, 0);
		int shouldZapAutomateOnPlay = SlotsPlayer.getPreferences().GetInt(Zap.Automation.ZAPPrefs.SHOULD_AUTOMATE_ON_PLAY, 0);
		
		Debug.LogFormat("ZAPLOG -- finishLoading -- about to do zap checks. shouldZapResume = " + shouldZapResume + "; shouldZapAutomateOnPlay = " + shouldZapAutomateOnPlay);
		// MCC -- At this point we have all of the data that we need to run the game.
		// If we want ZAP to run, this is good time to check for that and kick if off.
		if (shouldZapResume == 1)
		{
			Debug.LogFormat("ZAPLOG -- resuming ZAP");
			// If we want to resume.
			SlotsPlayer.getPreferences().SetInt(Zap.Automation.ZAPPrefs.SHOULD_RESUME, 0);
			Zap.Automation.ZyngaAutomatedPlayer.instance.resumeAutomation();
			
		}
		else if (shouldZapAutomateOnPlay == 1)
		{
			Debug.LogFormat("ZAPLOG -- starting ZAP");
			// Assume that we have put a test plan into the player prefs to run here.
			SlotsPlayer.getPreferences().SetInt(Zap.Automation.ZAPPrefs.SHOULD_AUTOMATE_ON_PLAY, 0);
			Zap.Automation.ZyngaAutomatedPlayer.instance.startAutomation();
		}
		else
		{
			Debug.LogFormat("ZAPLOG -- doing nothing with ZAP.");
		}
		Debug.LogFormat($"ZAPLOG -- finishLoading -- EOF{Time.realtimeSinceStartup}");
#endif
		Dictionary<string, string> extraFields = new Dictionary<string, string>();
		extraFields.Add("isAppleSignedIn", SlotsPlayer.IsAppleLoggedIn.ToString());
		extraFields.Add("isFacebookSignedIn", SlotsPlayer.isFacebookUser.ToString());
		extraFields.Add("isFacebookConnected", SlotsPlayer.IsFacebookConnected.ToString());
		SplunkEventManager.createSplunkEvent("SIWA Connections", "SIWA-connections", extraFields);

		//run EUE if necessary
		EUEManager.showLobbyEueFtue();
	}
	
	private bool isValidSession(string message)
	{
		string fullMessage = "";
		
		if (!Data.isGlobalDataSet)
		{
			fullMessage = "GameLoader.finishLoading(): " + message + ". Found null for Data.global.";
		}

		if (Data.login == null)
		{
			fullMessage = "GameLoader.finishLoading(): " + message + ". Found null for Data.login.";
		}
		
		if (fullMessage != "")
		{
			Glb.resetGame(fullMessage);
			return false;
		}

		return true;
	}

	
#region ISVDependencyInitializer implementation
	/// <summary>
	/// The AuthManager is dependent on GameSession
	/// </summary>
	/// <returns>
	/// Type[] array
	/// </returns>
	public System.Type[] GetDependencies() {
		return new System.Type[] { typeof(SocialManager), typeof(BasicInfoLoader) } ;	
	}

	/// <summary>
	/// Initializes the AuthManager
	/// </summary>
	/// <param name='mgr'>
	/// Manager instance to call once intialization is complete.
	/// </param>
	public void Initialize(InitializationManager mgr) {
		_initMgr = mgr;
		Initialize();
	}
	
	// short description of this dependency for debugging purposes
	public string description()
	{
		return "GameLoader";
	}

#endregion
}
