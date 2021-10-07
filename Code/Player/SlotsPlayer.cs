using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using UnityEngine;
using Zynga.Zdk;
using Zynga.Core.Util;
using Zynga.Core.UnityUtil;
using Zynga.Payments.IAP;
using Zynga.Zdk.Services.Identity;

/**
Singleton class.
Contains non-technical gameplay information specific to the current user only,
and isn't already contained in the FacebookMember class.
*/

public class SlotsPlayer : IResetGame
{
	public SocialMember socialMember = null;
	public int firstPlayTime;
	public string country;
	public int creditSendLimit = 0;
	public event GenericDelegate onVipLevelUpdated;
	private int _vipNewLevel = 0;
	private static bool isAppleLoggedIn = false;
	private static bool isFacebookConnected = false;

	public int vipNewLevel
	{
		// Making this a property so that we can set update any
		// references to it whenever we change it.
		get { return _vipNewLevel; }
		set
		{
			if (_vipNewLevel != value && socialMember != null)
			{
				socialMember.setUpdated();
			}

			_vipNewLevel = value;
			if (GiftChestOffer.instance.isEnabled)
			{
				GiftChestOffer.instance.refreshOfferData();
			}

			if (onVipLevelUpdated != null)
			{
				onVipLevelUpdated();
			}
		}
	}

	public int adjustedVipLevel
	{
		get
		{
			if (VIPStatusBoostEvent.isEnabled() || PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_VIP_BOOSTS_KEY))
			{
				return VIPStatusBoostEvent.getAdjustedLevel();
			}

			return _vipNewLevel;
		}
	}

	public bool isMaxVipLevel
	{
		get
		{
			if (VIPLevel.maxLevel != null)
			{
				return _vipNewLevel == VIPLevel.maxLevel.levelNumber;
			}

			return false;
		}
	}

	public CoinGiftLimit creditsAcceptLimit;
	public FreeSpinGiftLimit giftBonusAcceptLimit;

	public long vipPoints { get; private set; }
	public long mergeBonus = 0; // The amount of credits the player receives when they log into facebook.
	public bool allowedAccess = true; // Set to false if this player has been banned from the game.
	public bool isGDPRSuspend = false; // Set to true if the users gdpr delete request has been completed
	public bool hasGDPRDeleteRequest = false; //Set to true if the user has requested their data be deleted.
	public bool hasCOPPADeleteRequest = false; 
	public bool allowedPayments = true; // Set to false if this player is no longer permitted to make payments.
	public bool forceAllLogging = false;

	public bool didAcceptTOS = false; // Has the user accepted the updated Terms of Service yet
	public GameTimerRange jackpotDaysTimeRemaining = null;
	public string networkID;
	public int vipTokensCollected = 0;
	public float currentBuyPageInflationFactor = 1f;
	public float nextBuyPageInflationFactor = 1f;
	public float currentPjpWagerInflationFactor = 1f;
	public float currentPjpAmountInflationFactor = 1f;
	public long reprice2019CreditsGrant = 0;
	public float currentBuyPageInflationPercentIncrease = 0f;
	public float nextBuyPageInflationPercentIncrease = 0f;
	public float currentMaxVoltageInflationFactor = 1f;
	public int dailyBonusDuration = 120;
#if RWR
	public bool isRWRSweepstakesEnabled = false; // are real world rewards enabled?
	public GameTimer rwrTimer = null; // how much time is left for real world rewards?
	public bool isEnteredInRWRSweepstakes = false; // has player entered the rwr sweepstakes by entering their email?
	public string rwrSweepstakesEmail = ""; // player's rwr sweepstakes email
#endif
	public DailyBonusGameTimer dailyBonusTimer = null;

	public ProgressivePools progressivePools = null; // Personal progressive pools. Seems odd to have this here, but putting it here to be consistent with Flash version.

	private PlayerResource credits = null;
	public PlayerResource xp = null;

	private static SlotsPlayer _instance = null;
	public bool isPayerWeb = false; // Has the player spent any money on web.
	public bool isPayerMobile = false; // Has the player spent any money on mobile.

	public int questCollectibles = -1; // The number of collectibles the player has for the current quest.

	public int vipSpendTier = -1; // VIP spend tiers (defined by Gemma)
	public bool spent200OrMore = false; // Did they spend $200+?
	public int secondsSinceLastPlayed = -1; // seconds since last played
	public string vipStatus = "Inactive"; // Have they played in the last 30 days?
	public bool playedInTheLast30Days = false; // Have they played in the last 30 days?

	public static System.DateTime loginTime; // The time when the last login happened, doesn't matter whether anonymous or social network.
	// Only used for determining whether to reset game after unpausing.

	public static bool isLoggedIn // Lets us know whether a user is logged in, doesn't matter whether anonymouse or social network.
	{
		get { return _isLoggedIn; }
		set { _isLoggedIn = value; }
	}

	private static bool _isLoggedIn = false;

	private SlotsPlayer()
	{
	}

	public static SlotsPlayer instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new SlotsPlayer();
			}

			return _instance;
		}
	}

	public SocialMember facebook
	{
		get { return socialMember; }
	}

	// Convenience getter.
	public bool isMaxLevel
	{
		get { return (socialMember.experienceLevel == ExperienceLevelData.maxLevel); }
	}

	public void init()
	{
		JSON login = Data.login; // Shorthand.
		//Debug.Log(login) -- let's not tostring this entire json if we don't have to --- that's 3mb of allocation
		JSON playerJSON = Data.player;

		// Time!
		firstPlayTime = playerJSON.getInt("started_playing_at_timestamp", 0);
		if (ExperimentWrapper.GlobalMaxWager.isInExperiment)
		{
			if (Glb.wagerUnlockData.ContainsKey(ExperimentWrapper.GlobalMaxWager.variantName))
			{
				SlotsWagerSets.WagerValue.populateAllWagerValues(Glb.wagerUnlockData[ExperimentWrapper.GlobalMaxWager.variantName]);
			}
			else
			{
				Debug.LogErrorFormat("In the global max wager experiment but variant {0} wasn't found in Global Data. Falling back to the default unlock levels", ExperimentWrapper.GlobalMaxWager.variantName);
				SlotsWagerSets.WagerValue.populateAllWagerValues(Glb.wagerUnlockData["defaultWagers"]);
			}
		}
		else if (!SlotsWagerSets.hasLevelData)
		{
			SlotsWagerSets.WagerValue.populateAllWagerValues(Glb.wagerUnlockData["defaultWagers"]);
		}

		Glb.wagerUnlockData = null; //Don't need to hold onto this JSON anymore once we've populated our wager values list

		JSON personalizedContentData = playerJSON.getJSON("personalized");
		if (personalizedContentData != null && ExperimentWrapper.PersonalizedContent.isInExperiment)
		{
			// We default to nothing here...maybe we can default to something else.
			string key = personalizedContentData.getString("slots_game", "");
			PersonalizedContentLobbyOptionDecorator1x2.gameKey = key;
		}

		// User-specific gameplay data is stored in the User class.
		vipPoints = playerJSON.getLong("vip_points", 0);
		vipNewLevel = playerJSON.getInt("vip_new_level", 0);
		country = playerJSON.getString("country", "");

		if (Data.debugMode)
		{
			string countryCode = PlayerPrefs.GetString(DebugPrefs.DEVGUI_COUNTRY_CODE, "");
			if (countryCode.Length > 0)
			{
				country = countryCode;
				GenericDialog.showDialog(
					Dict.create(
						D.TITLE, "Overide Warning!",
						D.MESSAGE, "Using country code override : " + country,
						D.SHOULD_HIDE_LOADING, false,
						D.REASON, "Go to tools/players in dev panel to reset"
					), SchedulerPriority.PriorityType.IMMEDIATE
				);
			}
		}

		int creditsAccepted = playerJSON.getInt("request_accepted.send_credits", 0);
		int spinsAccepted = playerJSON.getInt("request_accepted.send_gift_bonus", 0);
		VIPLevel currentVipLevel = VIPLevel.find(vipNewLevel);

		creditsAcceptLimit = new CoinGiftLimit(currentVipLevel.creditsGiftLimit, currentVipLevel.creditsGiftLimit - creditsAccepted);
		giftBonusAcceptLimit = new FreeSpinGiftLimit(currentVipLevel.freeSpinLimit, currentVipLevel.freeSpinLimit - spinsAccepted);
		creditSendLimit = playerJSON.getInt("request_limits.send_credits_sends", 0);
		didAcceptTOS = (playerJSON.getInt("tos_accepted", 0) != 0); // tos_accepted will be the version number the player has accepted, 0 if they are not up to date.
		mergeBonus = playerJSON.getLong("merge_bonus", 5000L);
		vipTokensCollected = login.getInt("vip_revamp_tokens", 0);
		networkID = playerJSON.getString("network_state.network_id", "");
		jackpotDaysTimeRemaining = GameTimerRange.createWithTimeRemaining(login.getInt("buypage_progressive_time_remaining", 0));
		if (Data.debugMode && country == "")
		{
			Data.showIssue("Player's country data is empty! This will result in no regional game locking.");
		}

		JSON currInflationJson = login.getJSON("inflations_current");
		if (currInflationJson != null)
		{
			currentPjpWagerInflationFactor = currInflationJson.getFloat("pjp_wager", 1f);
			currentPjpAmountInflationFactor = currInflationJson.getFloat("pjp_amount", 1f);
			currentBuyPageInflationFactor = currInflationJson.getFloat("buy_page", 1f);
			currentMaxVoltageInflationFactor = currInflationJson.getFloat("power_room", 1f);

		}

		currentBuyPageInflationPercentIncrease = login.getFloat("bp_inflation_current_percent", 0f);

		JSON nextInflationJson = login.getJSON("inflations_next");
		if (nextInflationJson != null)
		{
			nextBuyPageInflationFactor = nextInflationJson.getFloat("buy_page", 1f);
		}

		nextBuyPageInflationPercentIncrease = login.getFloat("bp_inflation_next_percent", 0f);

		JSON maxVoltageJson = login.getJSON("max_voltage");
		if (maxVoltageJson != null)
		{
			SlotsWagerSets.parseMaxVoltageLoginData(maxVoltageJson);
			MaxVoltageTokenCollectionModule.initTokenInfo(maxVoltageJson);
		}

		CustomPlayerData.populateAll(playerJSON.getJSON("custom_data"));

		JSON timestamps = playerJSON.getJSON("my_timestamps");

		allowedAccess = playerJSON.getBool("allowed_access", true);
		allowedPayments = playerJSON.getBool("allowed_payments", true);
		string gdprStatus = playerJSON.getString("gdpr_status", "");
		gdprStatus = gdprStatus.ToLower().Trim();
		switch (gdprStatus)
		{
			case "suspended":
				isGDPRSuspend = true;
				break;

			case "pending_delete":
				hasGDPRDeleteRequest = true;
				break;
		}

		string coppaStatus = playerJSON.getString("coppa_status", "");
		hasCOPPADeleteRequest = !string.IsNullOrEmpty(coppaStatus) && coppaStatus == "pending_delete";
		
		// If this player has been marked in their blob, turn on logging for everything that they do:
		forceAllLogging = playerJSON.getBool("log_everything", false);
		if (!forceAllLogging && timestamps != null)
		{
			// If we havent turned on force logging for everyone, then check for player specific logging.
			string forceLoggingTimestamp = timestamps.getString("force_logging", "");
			if (!string.IsNullOrEmpty(forceLoggingTimestamp))
			{
				// If a timestamp exists, then we manually set it from the admin tool, so turn it on.
				forceAllLogging = true;
				Debug.Log("turning on player specific logging");
			}
		}

		if (forceAllLogging)
		{
			Glb.serverLogErrors = true;
			Glb.serverLogWarnings = true;
			Glb.serverLogPayments = true;
		}

		isPayerWeb = (playerJSON.getInt("spend_count", 0) > 0);
		isPayerMobile = (playerJSON.getInt("spend_count_mobile", 0) > 0);

		//Sets Invite rewards
		InviteRewards.acceptedInvites = playerJSON.getInt("accepted_invites", 0);

		JSON incentivizedUpdatesJSON = playerJSON.getJSON("incentivized_invites");
		if (incentivizedUpdatesJSON != null)
		{
			//Number of incentivized friend invites accepted so far
			InviteRewards.acceptedIncentiveInvites = incentivizedUpdatesJSON.getInt("accept_invite_count", 0);
		}

		JSON requestAcceptedJSON = playerJSON.getJSON("request_accepted");
		if (requestAcceptedJSON != null)
		{
			//Number of incentivized friend invites claimed today
			InviteRewards.incentivizeClaimsAccepted = requestAcceptedJSON.getInt("friend_invite_incentive_claim", 0);
		}

		JSON requestLimitsJSON = playerJSON.getJSON("request_limits");
		if (requestLimitsJSON != null)
		{
			//Number of incentivized friend invite claims remaining today
			InviteRewards.incentivizeClaimsRemaining = requestLimitsJSON.getInt("friend_invite_incentive_claim", 0);
		}
#if RWR
		int endTimestamp = Data.liveData.getInt("RWR_PROMO_END_TS", 0);

		isRWRSweepstakesEnabled = playerJSON.getBool("is_rwr_enabled", false);
		if (isRWRSweepstakesEnabled && endTimestamp > 0)
		{
			rwrTimer = GameTimer.createWithEndDateTimestamp(endTimestamp);
		}

		JSON rwrEnteredSweepstakes = login.getJSON("rwr_entered_sweepstakes");
		if (rwrEnteredSweepstakes != null)
		{
			JSON packageJSON = rwrEnteredSweepstakes.getJSON(Glb.RWR_ACTIVE_PROMO);

			if (packageJSON != null)
			{
				isEnteredInRWRSweepstakes = packageJSON.getBool("entered", false);
				rwrSweepstakesEmail = packageJSON.getString("email", "");
			}
		}
#endif

		UnlockAllGamesFeature.instance.addTimeRange( Glb.UNLOCK_ALL_GAMES_START_TIME,
												Glb.UNLOCK_ALL_GAMES_END_TIME,
												UnlockAllGamesFeature.Source.LiveData,
										Glb.UNLOCK_ALL_GAMES || ExperimentWrapper.UnlockAllGames.isInExperiment);

		// Add resources.
		credits = PlayerResource.createResource("credits", playerJSON.getLong("credits", 0));
		xp = PlayerResource.createResource("xp", playerJSON.getLong("experience", 0));
		int xpLevel = playerJSON.getInt("experience_level", 1);
		PlayerPrefsCache.SetInt(Prefs.PLAYER_LEVEL, xpLevel);

		JSON walletInflation = login.getJSON("wallet_inflation");
		if (walletInflation != null)
		{
			long oldCoins = walletInflation.getLong("old_coins", 0L);
			long newCoins = walletInflation.getLong("new_coins", 0L);
			reprice2019CreditsGrant = walletInflation.getLong("coins_added", 0L); //Subtract this from the player resource so we can readd it later
			reprice2019CreditsGrant /= CreditsEconomy.economyMultiplier; //This comes down pre-multiplied so dividing it here

			if (ExperimentWrapper.RepriceVideo.isInExperiment)
			{
				VideoDialog.queueRepriceVideo();
			}
			else
			{
				//Add the coins without showing the video if the experiment isn't on
				SlotsPlayer.addCredits(SlotsPlayer.instance.reprice2019CreditsGrant, "Reprice 2019 Video FTUE");
				SlotsPlayer.instance.reprice2019CreditsGrant = 0;
			}
		}
		
		// *) HelpShift metadata for VIP Team:
		// 1) VIP spend tiers defined by Gemma
		// -- 0 is for spend of	$0 -  199.99
		// -- 1 is for spend of	$200 -  999.99
		// -- 2 is for spend of $1000 - 4999.99
		// -- 3 is for spend of $5000 - 9999.99
		// -- 4 is for spend of $10000+
		vipSpendTier = playerJSON.getInt("vip_spend_tier", -1);

		// 2) Flag if spent $200 or more
		spent200OrMore = vipSpendTier > 0;
		Debug.LogFormat("HelpShift> vip_spend_tier = {0} spent200OrMore = {1}", vipSpendTier, spent200OrMore);

		// 3) seconds since last played
		secondsSinceLastPlayed = playerJSON.getInt("time_since_last_played", -1);

		// 4) Active if played in last 30 days
		// 5) Flag if played in the last 30 days
		const int secondsIn30Days = 60 * 60 * 24 * 30;
		if (SlotsPlayer.instance.secondsSinceLastPlayed > secondsIn30Days)
		{
			playedInTheLast30Days = false;
			vipStatus = "Inactive";
		}
		else
		{
			playedInTheLast30Days = true;
			vipStatus = "Active";
		}

		Debug.LogFormat("HelpShift> secondsSinceLastPlayed = {0} vipStatus = {1} playedInTheLast30Days = {2}", secondsSinceLastPlayed, vipStatus, playedInTheLast30Days);

		foreach (JSON timer in playerJSON.getJsonArray("timers"))
		{
			if (ExperimentWrapper.NewDailyBonus.isInExperiment && ExperimentWrapper.NewDailyBonus.bonusKeyName == timer.getString("key_name", ""))
			{
				dailyBonusTimer = new DailyBonusGameTimer(timer);
			}

			switch (timer.getString("key_name", ""))
			{
				case "bonus":
					if (!ExperimentWrapper.NewDailyBonus.isInExperiment)
					{
						dailyBonusTimer = new DailyBonusGameTimer(timer);
					}

					break;

				case "sir_bonus":
					dailyBonusTimer = new DailyBonusGameTimer(timer);
					break;
			}
		}

		GiftedSpinsVipMultiplier.playerMultiplier = login.getLong("gifted_spins_vip_multiplier", 1);

		// Find out if this is the player's first visit.
		// The play/session times aren't reliable for anonymous users,
		// ...so we also need to check the player's XP and starting credits.

#if UNITY_EDITOR
		if (firstPlayTime == GameTimer.sessionStartTime)
		{
			Debug.LogWarning("Client received identical play/session timestamps: " + firstPlayTime.ToString() + " / " + GameTimer.sessionStartTime.ToString());
		}
#endif

		bool hasZeroXp = (xp.amount == 0);
		//UA Wrapper inits:
		SlotsPlayer.uaWrapperInit(hasZeroXp);

		int lastQuestCounter = PlayerPrefsCache.GetInt(Prefs.LAST_QUEST_COUNTER, 0); // Not sure if this should be a CustomPlayerData instead.

		JSON dailyChallenge = login.getJSON("quests.daily_challenge");
		if (dailyChallenge != null)
		{
			DailyChallenge.didWin = (dailyChallenge.getInt("inventory", 0) > 0);

			// Note SlotsPlayer.questCollectibles is set elsewhere because EOS experiments are needed to initialize it,
			// and EOS experiment are not initialized yet
			Quest.resetCounter = dailyChallenge.getInt("reset_counter", 0);

			if (lastQuestCounter != Quest.resetCounter)
			{
				// A new quest started, so reset a flag for if the user won.
				CustomPlayerData.setValue(CustomPlayerData.DAILY_CHALLENGE_DID_WIN, false);
			}
		}
		else
		{
			// If the quest is over, then read the latest from custom player data.
			DailyChallenge.didWin = CustomPlayerData.getBool(CustomPlayerData.DAILY_CHALLENGE_DID_WIN, false);
		}

		// Get the timestamp data for when the dialogs were last seen.
		if (timestamps != null)
		{
			string announceDateString = timestamps.getString(DailyChallenge.LAST_SEEN_MOTD_TIMESTAMP_KEY, "");
			if (!string.IsNullOrEmpty(announceDateString))
			{
				System.DateTime announceDateTime;
				if (System.DateTime.TryParse(announceDateString, out announceDateTime))
				{
					int lastSeenAnnouncementDialog = Common.convertToUnixTimestampSeconds(announceDateTime);
					DailyChallenge.lastSeenAnnouncementDialog = lastSeenAnnouncementDialog;
				}
			}

			string overDateString = timestamps.getString(DailyChallenge.LAST_SEEN_OVER_TIMESTAMP_KEY, "");
			if (!string.IsNullOrEmpty(overDateString))
			{
				System.DateTime overDateTime;
				if (System.DateTime.TryParse(overDateString, out overDateTime))
				{
					int lastSeenOverDialog = Common.convertToUnixTimestampSeconds(overDateTime);
					DailyChallenge.lastSeenOverDialog = lastSeenOverDialog;
				}
			}

			GiftedSpinsVipMultiplier.timestampValue = timestamps.getString(GiftedSpinsVipMultiplier.TIMESTAMP_KEY, "");
		}


		PlayerPrefsCache.SetInt(Prefs.LAST_QUEST_COUNTER, Quest.resetCounter); // Not sure if this should be a CustomPlayerData instead.
		PlayerPrefsCache.Save();

		Buff.init(login.getJsonArray("buffs"));

		loginTime = System.DateTime.Now;
		isLoggedIn = true;
		Bugsnag.LeaveBreadcrumb("Player logged in");

//#if UNITY_WEBGL
		//string currencyCode = playerJSON.getString("user_currency", "USD");
		//if (currencyCode != Packages.Singleton.inAppPurchaseSettings.FacebookCurrency)
		//{
		//PlayerAction.updateCurency("DZD");
		//}
//#endif
	}

	public void onUnlockAllGames()
	{
		// Reset all lobby game's isUnlocked.
		foreach (LobbyGame lg in LobbyGame.getAll())
		{
			lg.setIsUnlocked();
		}
	}

	public static void uaWrapperInit(bool hasZeroXp)
	{
		if (hasZeroXp)
		{
			//Registration
			UAWrapper.Instance.OnRegistration();
			UAWrapper.Instance.onLevelUp(PlayerPrefsCache.GetInt(Prefs.PLAYER_LEVEL, 1));
			//TODO: Set Flag for UA Wrapper FTUE after first spin completes
			PlayerPrefsCache.SetInt(Prefs.UA_FTUE_COMPLETED, 1);
			PlayerPrefsCache.Save();
		}

		// Current version
		string currentVersion = Glb.clientVersion;

		// The last version we saved
		string lastKnownClientVersion = PlayerPrefsCache.GetString(Prefs.CLIENT_VERSION, "");

		// First time there will be no stored version.
		// If we do have a version though...
		if (!string.IsNullOrEmpty(lastKnownClientVersion))
		{
			if (lastKnownClientVersion != currentVersion)
			{
				//Store current version in PlayerPrefsCache.
				PlayerPrefsCache.SetString(Prefs.CLIENT_VERSION, currentVersion);
			}
		}
		else // We did not have a saved version, so save one now.
		{
			PlayerPrefsCache.SetString(Prefs.CLIENT_VERSION, currentVersion);
		}
	}

	public void addVIPPoints(long amount)
	{
		vipPoints += amount;

		if (GameState.isMainLobby && VIPLobby.instance != null)
		{
			VIPLobby.instance.refreshUI();
		}
	}

	public void setVIPPoints(long amount)
	{
		vipPoints = amount;
		if (GameState.isMainLobby && VIPLobby.instance != null)
		{
			VIPLobby.instance.refreshUI();
		}
	}

	public void setNetworkVipPoints(long updatedPointsAmount, int updatedVipLevel, bool allowDecrease = false)
	{
		long previousVipLevel = vipNewLevel;

		if (allowDecrease)
		{
			// If we explicitly want to allow a decrease (only happens on a network disconnect)
			// then dont do the greater than checks.
			setVIPPoints(updatedPointsAmount);
			vipNewLevel = updatedVipLevel;
		}
		else
		{
			// Only update if it is greater than what is there
			// and use the established setters.
			if (updatedPointsAmount > vipPoints)
			{
				setVIPPoints(updatedPointsAmount);
			}

			if (updatedVipLevel > vipNewLevel)
			{
				vipNewLevel = updatedVipLevel;
			}
		}

		if (previousVipLevel != vipNewLevel)
		{
			// we need to setup the locked state of VIP Lobby games again, because new ones may be unlocked if linked-vip-level is > than vip level stored in player blob
			LobbyInfo.updateLobbyOptionsUnlockedState(LobbyInfo.Type.VIP);
		}
	}

	private void onUnlockAllGamesExpiration(Dict args = null, GameTimerRange parent = null)
	{
		UnlockAllGamesMotd.showDialog("", true);
	}

	/// Is the current player in anonymous mode? (no Facebook linkage)
	public static bool isAnonymous
	{
		get
		{
			if(PackageProvider.Instance.Authentication.Flow.Account == null)
			{
				// isAnonymous only returns true if we have an anonymous user.
				// In this scenario, we don't have a zis account yet.
				// This happens on dotcom-webgl, because we're logging into
				// email without an existing account.
				return false;
			}
			return PackageProvider.Instance.Authentication.Flow.Account.IsAnonymous;
		}
	}

	public static bool IsEmailLoggedIn
	{
		get
		{
			if (Application.isPlaying)
			{
				if (PackageProvider.Instance.Authentication.Flow.Account != null && PackageProvider.Instance.Authentication.Flow.Account.UserAccount != null)
				{
					var securedByEmail = PackageProvider.Instance.Authentication.Flow.Account.UserAccount?.Email != null;
					if (securedByEmail)
					{
						return true;
					}
				}
			}
			return false;
		}
	}

	public static bool IsAppleLoggedIn
	{
		get
		{
			if (Application.isPlaying)
			{
				if (PackageProvider.Instance.Authentication.Flow.Account != null && PackageProvider.Instance.Authentication.Flow.Account.UserAccount != null)
				{
					var securedBySiwa = PackageProvider.Instance.Authentication.Flow.Account.UserAccount?.Siwa != null;
					if (securedBySiwa)
					{
						return true;
					}
				}
			}
			return false;

		}

		set { isAppleLoggedIn = value; }
	}

	public static bool IsFacebookConnected
	{
		get
		{
			if (Application.isPlaying)
			{
				return isFacebookConnected;
			}

			return true;
		}

		set { isFacebookConnected = value; }
	}

	public static bool isFacebookUser
	{
		get
		{
			if (Application.isPlaying)
			{
				// MCC -- Wrapping this in Application.isPlaying so that UnitTests dont break.
				if (PackageProvider.Instance.Authentication.Flow.Account != null && PackageProvider.Instance.Authentication.Flow.Account.UserAccount != null)
				{
					var securedByFacebook = PackageProvider.Instance.Authentication.Flow.Account.UserAccount?.Facebook != null;
					if (securedByFacebook)
					{
						return true;
					}
				}
				return false;
			}
			else
			{
				return true;
			}
		}

	}

	public static bool isSocialFriendsEnabled
	{
		get { return isFacebookUser || NetworkFriends.instance.isEnabled || IsFacebookConnected; }
	}

	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		// Moved isLoggedIn = false to Glb.reinitializeGame().
		_instance = null;
		Bugsnag.LeaveBreadcrumb("Player logged out");
	}

	public static void registerEventDelegates()
	{
		Server.registerEventDelegate("contact_customer_support", customerSupportEvent, true);
		Server.registerEventDelegate("select_game_unlock", selectGameUnlockEvent, true);
#if UNITY_WSA_10_0 && NETFX_CORE
		Server.registerEventDelegate("game_unlock", WindowEconomyManager.creditPurchase, true);
#else
		Server.registerEventDelegate("game_unlock", NewEconomyManager.creditPurchase, true);
#endif

		Server.registerEventDelegate("economy_multiplier", markReprice2018Seen);
	}

	//We shouldn't be getting this server event anymore, all client assets have been removed.
	//Still want to handle seeing the action incase somehow its triggered and we need to clear this from the player
	private static void markReprice2018Seen(JSON data)
	{
		string eventID = data.getString("event", "");
		RepricingAction.markRepricingFtueSeen(eventID); //Let the server know we saw the repricing ftue
	}

	// Callback for the Server event recieved after unlocking a game.
	public static void unlockGame(string eventName, string gameKey)
	{
		LobbyOption option = LobbyOption.activeGameOption(gameKey);
		if (option != null && !option.game.isUnlocked)
		{
			// Unlock the game for the player.
			option.game.xp.isPermanentUnlock = true;
			option.game.xp.isPendingPlayerUnlock = false;
			option.game.setIsUnlocked();

			// Remove the unlocked game from the list of available unlocks.
			SelectGameUnlockDialog.removeGameFromLists(option.game);

			// Reorganize the lobby
			LobbyInfo main = LobbyInfo.find(LobbyInfo.Type.MAIN);
			if (main != null)
			{
				main.organizeOptions();
			}

			// We have unlocked some games, so update the SelectGameUnlock game list.
			SelectGameUnlockDialog.setupGameList();

			// challenges game controls showing this dialog
			if (!option.game.isChallengeLobbyGame)
			{
				GameUnlockedDialog.showDialog(option.game, null);
			}
		}

		if (!string.IsNullOrEmpty(eventName))
		{
			// Send decline_event action
			RequestAction.declineEvent(eventName);
		}
	}

	// Event callback for the select game event.
	public static void selectGameUnlockEvent(JSON unlockData)
	{
		RoutineRunner.instance.StartCoroutine(unlockEvent(unlockData));
	}

	// In-game or lobby-wait unlock events.
	private static IEnumerator unlockEvent(JSON unlockData)
	{
		// Wait if it's loading.
		if (GameState.isMainLobby)
		{
			while (MainLobby.instance == null)
			{
				yield return null;
			}
		}

		string feature = unlockData.getString("feature_name", "generic");
		string eventId = unlockData.getString("event", "");
		if (SelectGameUnlockDialog.shouldShowDialog(feature, eventId))
		{
			SelectGameUnlockDialog.showDialog(feature, eventId);
		}
	}


	public static void customerSupportEvent(JSON csData)
	{
		string title = Localize.text(csData.getString("title_loc", ""));
		string body = Localize.text(csData.getString("body_loc", ""));

		if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(body))
		{
			GenericDialog.showDialog(
				Dict.create(
					D.TITLE, title,
					D.MESSAGE, body,
					D.REASON, "slots-player-customer-support-event"
				),
				SchedulerPriority.PriorityType.IMMEDIATE
			);
		}
	}

#if RWR
	public bool getIsRWRSweepstakesActive()
	{
		return isRWRSweepstakesEnabled && rwrTimer != null && !rwrTimer.isExpired;
	}

	public int getNumRWRSweepstakesTickets()
	{
		int numTickets = 0;

		if (getIsRWRSweepstakesActive())
		{
			foreach (LobbyGame game in LobbyGame.getAll())
			{
				if (game.isRWRSweepstakes &&
				   (game.xp.rwrSweepstakesMeterCount == game.rwrSweepstakesMeterMax))
				{
					numTickets++;
				}
			}
		}

		return numTickets;
	}
#endif

	public static PreferencesBase getPreferences()
	{
#if UNITY_WEBGL && !UNITY_EDITOR
		PreferencesBase preferences = null;
		if (WebGLFunctions.isLocalStorageAvailable())
		{
			return new LocalStoragePreferences();
		}
		else
		{
			return new UnityPreferences();
		}

#else
		return new UnityPreferences();
#endif
	}

	public static void facebookLogin(DialogBase.AnswerDelegate originatorCallback = null)
	{
		if (!isFacebookConnected)
		{
			NotificationManager.DeRegisterFromPN();
			SocialManager.Instance.CreateAttach(AuthenticationMethod.Facebook);
			/*PreferencesBase UnityPrefs = getPreferences();
			NotificationManager.DeRegisterFromPN();

			if (Data.liveData != null && Data.liveData.getBool("MOBILE_CLEAR_FB_PREFS", false))
			{
				// Regardless at this point, we know that two PlayerPref keys got to go.
				Debug.LogWarning("Attempting to clear all  preferences");
				UnityPrefs.DeleteAll();
				UnityPrefs.Save();
			}

			// Begin the facebook login process
			Loading.instance.isLoggingIn = true;
			Loading.show(Loading.LoadingTransactionTarget.LOBBY_LOGIN);
			UnityPrefs.SetInt(SocialManager.kLoginPreference, (int) SocialManager.SocialLoginPreference.Facebook);
			UnityPrefs.SetInt(SocialManager.kUpgradeZid, 1);
			UnityPrefs.SetString(Prefs.ANONYMOUS_ZID, SlotsPlayer.instance.socialMember.zId);
			UnityPrefs.SetInt(Prefs.USER_SELECTED_LOGOUT, 0);
			UnityPrefs.Save();
			if (isAppleLoggedIn)
			{
				SlotsPlayer.IsAppleLoggedIn = false;
			}

			Glb.resetGame("Connecting to facebook.");
		}
		else if (IsAppleLoggedIn)
		{
			SocialManager.Instance.FBConnect();*/
		}
		else
		{
			Debug.LogError("Somehow a non-google, non-guest, player attempted to do a Facebook login.");
		}
	}

	public static void finishAppleLogin(DialogBase.AnswerDelegate originatorCallback = null)
	{
		if (isAnonymous)
		{
			PreferencesBase UnityPrefs = getPreferences();
			NotificationManager.DeRegisterFromPN();

			if (Data.liveData != null && Data.liveData.getBool("MOBILE_CLEAR_FB_PREFS", false))
			{
				// Regardless at this point, we know that two PlayerPref keys got to go.
				Debug.LogWarning("Attempting to clear all  preferences");
				UnityPrefs.DeleteAll();
				UnityPrefs.Save();
			}

			// Begin the facebook login process
			Loading.instance.isLoggingIn = true;
			Loading.show(Loading.LoadingTransactionTarget.LOBBY_LOGIN);
			UnityPrefs.SetInt(SocialManager.kLoginPreference, (int) SocialManager.SocialLoginPreference.Apple);
			UnityPrefs.SetString(Prefs.ANONYMOUS_ZID, SlotsPlayer.instance.socialMember.zId);
			//kUpgradeZid sends up zid_to_upgrade param from previous session zid for migration
			UnityPrefs.SetInt(SocialManager.kUpgradeZid, 1);
			UnityPrefs.Save();
			Glb.resetGame("Connecting to apple.");
		}
		else
		{
			Debug.LogError("Somehow a non-google, non-guest, player attempted to do a Facebook login.");
		}
	}

	public static void facebookReconnect()
	{
		if (!isAnonymous)
		{
			PreferencesBase UnityPrefs = getPreferences();
			NotificationManager.DeRegisterFromPN();

			// Regardless at this point, we know that two PlayerPref keys got to go.
			Debug.LogWarning("Attempting to clear all  preferences");
			UnityPrefs.DeleteAll();
			UnityPrefs.Save();

			// Begin the facebook login process
			Loading.instance.isLoggingIn = true;
			Loading.show(Loading.LoadingTransactionTarget.LOBBY_LOGIN);
			UnityPrefs.SetInt(SocialManager.kLoginPreference, (int) SocialManager.SocialLoginPreference.Facebook);
			UnityPrefs.SetInt(SocialManager.kUpgradeZid, 1);
			UnityPrefs.SetString(Prefs.ANONYMOUS_ZID, SlotsPlayer.instance.socialMember.zId);
			UnityPrefs.Save();
			Glb.resetGame("Connecting to facebook.");
		}
	}

	public static void facebookLogout(bool userSelectedLogout = false)
	{
		resetLocalSettings();

		Loading.show(Loading.LoadingTransactionTarget.LOBBY_LOGIN);
		NotificationManager.DeRegisterFromPN();
		SocialManager.Instance.Logout(false, userSelectedLogout);
		NotificationManager.RegisteredForPushNotifications = false;
		NotificationManager.RegisterPNAttempted = false;
		if (LinkedVipProgram.instance.isConnected && NetworkProfileFeature.instance.isForEveryone)
		{
			Debug.LogFormat("SlotsPlayer.cs -- facebookLogout -- resetting the player profile becuase they are LL connected.");
			// If they have a network profile, then reset the photoURL and name at this point.
			NetworkProfileFeature.instance.resetProfile(new List<string>() {"photo_url", "name"});
		}
	}

	// Some settings that are saved locally don't make sense when switching between accounts,
	// so clear them here if necessary.
	public static void resetLocalSettings()
	{
		PlayerPrefsCache.SetInt(Prefs.HAS_WOZ_SLOTS_INSTALLED_BEFORE_CHECK, -1);
		PlayerPrefsCache.SetString(Prefs.LAST_SEEN_NEW_VIP_GAME, "");
		PlayerPrefsCache.SetInt(Prefs.LAST_SHOWED_RAINY_DAY_STARTUP_BUY_TIME, 0);

		PlayerPrefsCache.SetInt(Prefs.FORCED_COLLECT_COUNT_KEY, 0);
		PlayerPrefsCache.SetInt(Prefs.HARD_PROMPT_ACCEPTED_KEY, 0);
		PlayerPrefsCache.SetInt(Prefs.ENABLE_PN_SOFT_PROMPT_SEEN_COUNT, 0);
		PlayerPrefsCache.SetInt(Prefs.DEEP_LINK_PN_PROMPT_COUNT_KEY, 0);
		PlayerPrefsCache.SetInt(Prefs.COLLECT_COUNT_KEY, 0);

		PlayerPrefsCache.SetString(CustomPlayerData.RECENTLY_VIEWED_NEW_GAME_MOTD_MOBILE, "");

		PlayerPrefsCache.Save();
	}

	/*=========================================================================================
	MODIFYING CREDIT RESOURCES
	=========================================================================================*/
	public static long addCredits
	(
		long value,
		string source,
		bool playCreditsRollupSound = true,
		bool reportToGameCenterManager = true,
		bool shouldSkipOnTouch = true,
		float rollupTime = 0,
		string rollupOverride = "",
		string rollupTermOverride = ""
	)
	{
		if (instance != null && instance.credits != null)
		{
			return instance.credits.add(value, source, playCreditsRollupSound, reportToGameCenterManager, shouldSkipOnTouch, rollupTime, rollupOverride, rollupTermOverride);
		}

		Server.sendLogInfo("credit_change_error", 
			"addCredits() called before SlotsPlayer instance or credits Resource was initialized", 
			new Dictionary<string, string>
			{
				{"value", value.ToString()},
				{"source", source},
				{"instance_null", (instance == null).ToString()},
				{"credits_null", (instance == null || instance != null && instance.credits == null).ToString()}
			});
		return 0L;
	}

	public static long subtractCredits(long value, string source)
	{
		if (instance != null && instance.credits != null)
		{
			return instance.credits.subtract(value, source);
		}

		Server.sendLogInfo("credit_change_error", 
			"subtractCredits() called before SlotsPlayer instance or credits Resource was initialized", 
			new Dictionary<string, string>
			{
				{"value", value.ToString()},
				{"source", source},
				{"instance_null", (instance == null).ToString()},
				{"credits_null", (instance == null || instance != null && instance.credits == null).ToString()}
			});
		return 0L;
	}

	//This was broken out into two functions along with addFeatureCredits because the old function's bool param
	//that toggled the functionality was always called explicitly with true or false (many places passed nothing, but 
	//the param defaulted to true). This is the "false" case.
	public static long addNonpendingFeatureCredits(long amount, string source, bool playRollupSounds = true)
	{
		if (amount <= 0)
		{
			Server.sendLogInfo("credit_change_error", 
				"addNonpendingFeatureCredits() called with a non-positive value", 
				new Dictionary<string, string>
				{
					{"amount", amount.ToString()},
					{"source", source}
				});
			
			Debug.LogError($"addNonpendingFeatureCredits() called with a non-positive credit amount={amount} for source={source}");	
		}
		
		return addCredits(amount, source, playRollupSounds);
	}

	//This was broken out into two functions along with addNonpendingFeatureCredits because the old function's bool param
	//that toggled the functionality was always called explicitly with true or false (many places passed nothing, but 
	//the param defaulted to true). This is the "true" case.
	public static long addFeatureCredits(long amount, string source)
	{
		if (amount <= 0)
		{
			Server.sendLogInfo("credit_change_error", 
				"addFeatureCredits() called with a non-positive value", 
				new Dictionary<string, string>
				{
					{"amount", amount.ToString()},
					{"source", source}
				});
			
			Debug.LogError($"addFeatureCredits() called with a non-positive credit amount={amount} for source={source}");	
		}
		
		//internally addCredits() can call Server.resetPendingCredits();
		addCredits(amount, source);
		
		//addCredits() uses Server.totalPendingCredits to detect desyncs, which handlePendingCreditsSurfaced() modifies. 
		Server.handlePendingCreditsSurfaced(source, amount);
		return amount;
	}

	public static long creditAmount
	{
		get
		{
			if (instance != null && instance.credits != null)
			{
				return instance.credits.amount;
			}

			return 0L;
		}
	}

}
