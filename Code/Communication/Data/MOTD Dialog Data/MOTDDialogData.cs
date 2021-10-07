using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/*
  Data structure for new game and message-of-the-day dialogs. Duh. Also apparently everything that we want to spawn as a dialog to the user.
*/

public class MOTDDialogData : IResetGame
{
	// Short-name of this dialog, used to locat it:
	public string keyName;

	// General control parameters:
	public int sortIndex;
	public string argument;
	public string imageBackground;
	public string appearance;

	// Localization keys:
	public string locTitle;
	public string locSubheading;
	public string locBodyTitle;
	public string locBodyText;
	public string locAction1;
	public string locAction2;

	// Commands - what happens when functionality is triggered:
	public string commandAction1;
	public string commandAction2;

	// Tokens for conditional processing
	public bool isValidTokens = true;
	public bool isCloseHidden = false;

	// New Global MOTD variables.
	public bool shouldShowAppEntry = false;
	public bool shouldShowRTL = false;
	public bool shouldShowVip = false;
	public bool shouldShowPreLobby = false;

	// Variables for Dynamic MOTD
	public int maxViews = 1;
	public string statName;
	public int cooldown = 0;

	// dynamic audio pack
	public string audioPackKey = "";
	public string soundOpen = "";
	public string soundClose = "";
	public string soundOk = "";
	public string soundMusic = "";

	public string otherNoShowReasons = "";	// Other global reasons why this MOTD isn't being shown.

	// A dictionary of all our dialog definitions, from global data.
	private static Dictionary<string, MOTDDialogData> all = null;

	private static MOTDDialogDataNewGame _newGameMotdData;
	public static MOTDDialogDataNewGame newGameMotdData
	{
		get
		{
			if (_newGameMotdData == null)
			{
				_newGameMotdData = (MOTDDialogDataNewGame)find(MOTDDialogDataNewGame.MOTD_KEY);
			}
			return _newGameMotdData;
		}
	}
	
	// Public accessor collection of the keys we have received from the server dat.
	public static Dictionary<string, MOTDDialogData>.KeyCollection motdKeys
	{
		get
		{
			if (all != null)
			{
				return all.Keys;
			}
			else
			{
				return null;
			}
		}
	}

	public MOTDDialogData()
	{
	}

	public MOTDDialogData(JSON item)
	{
		setData(item);
	}

	public virtual void setData(JSON item)
	{
		this.keyName = item.getString("key_name", "");
		this.sortIndex = item.getInt("sort_index", int.MaxValue);
		this.argument = item.getString("argument", "");

		this.appearance = item.getString("loc_key_appearance", "");
		this.imageBackground = item.getString("loc_key_background_img", "");

		this.locTitle = item.getString("loc_key_title", "");
		this.locSubheading = item.getString("loc_key_subheading", "");
		this.locBodyTitle = item.getString("loc_key_body_title", "");
		this.locBodyText = item.getString("loc_key_body_text", "");
		this.locAction1 = item.getString("loc_key_action_1_text", "");
		this.locAction2 = item.getString("loc_key_action_2_text", "");

		this.commandAction1 = item.getString("loc_key_action_1_string", "");
		this.commandAction2 = item.getString("loc_key_action_2_string", "");
		this.maxViews = item.getInt("max_views", 1);
		this.statName = item.getString("stat_name", "");

		if (this.argument.Contains("-noclose"))
		{
			this.isCloseHidden = true;
		}
		else if (this.keyName == "tos_update")
		{
			// We are getting rid of the arugments field with the new MOTD framework, and tos_update 
			// is the only dialog that uses the -noclose argument anyways so we are just going to 
			// have a special client side case for it.
			this.isCloseHidden = true;
		}

		// Scan through our arguments and see if there are reasons to exclude this message:
		isValidTokens = checkForValidTokens(this.argument.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries));

		// New MOTD Framework Setup
		this.shouldShowAppEntry = item.getBool("show_location_entry", false);
		this.shouldShowRTL = item.getBool("show_location_rtl", false);
		this.shouldShowVip = item.getBool("show_location_vip", false);
		this.shouldShowPreLobby = item.getBool("show_location_prelobby", false);
	}

	// Each subclass should override this property and return base.noShowReason + its own reasons.
	public virtual string noShowReason
	{
		get
		{
			return MOTDFramework.noShowStartupDialogsReason + otherNoShowReasons + MOTDFramework.findNoShowReason(keyName);
		}
	}

	public virtual bool shouldShow
	{
		get
		{
			LobbyGame skuGameUnlock = LobbyGame.skuGameUnlock;
			if (skuGameUnlock != null)
			{
				if (keyName == MOTDDialog.getSkuGameUnlockName())
				{
					return !AppsManager.isBundleIdInstalled(AppsManager.WOZ_SLOTS_ID);
				}
				else if (keyName == "new_game_" + skuGameUnlock.keyName)
				{
					return false;
				}
			}
			
			// Always allow new game MOTD's, otherwise don't show unrecognized keys.
			return false;
		}
	}

	public virtual bool show()
	{
		return MOTDDialog.showDialog(this);
	}

	// If this dialog has a game for commandAction1, get the LobbyGame that it's for.
	public LobbyGame action1Game
	{
		get
		{
			if (_action1Game == null && commandAction1 != null && commandAction1.StartsWith(DoSomething.GAME_PREFIX))
			{
				_action1Game = LobbyGame.find(commandAction1.Substring(DoSomething.GAME_PREFIX.Length + 1));
			}
			return _action1Game;
		}
	}
	
	private LobbyGame _action1Game = null;
	
	public virtual void markSeen()
	{
		PlayerAction.markMotdSeen(keyName, true);
	}

	// All data is added through this function to make sure the dictionary exists in a single code location.
	private static void addMotdData(string keyName, MOTDDialogData data)
	{
		if (all == null)
		{
			all = new Dictionary<string, MOTDDialogData>();
		}
		all.Add(keyName, data);
	}

	// factory method populating the all dictionary with classes of invite reward tiers from global data.
	public static void populateAll(JSON[] items)
	{
		// First create objects for specific motd keys that require special behavior or conditions.
		addMotdData("achievements",						new MOTDDialogDataAchievements());
		addMotdData("achievements_no_rewards",			new MOTDDialogDataAchievementsNoRewards());
		addMotdData("achievements_loyalty_lounge",		new MOTDDialogDataAchievementsLoyaltyLounge());
		addMotdData("age_gate",							new MOTDDialogDataAgeGate());
		addMotdData("antisocial_prompt",				new MOTDDialogDataAntisocialPrompt());
		addMotdData("boardgame_video",				    new MOTDDialogDataBoardGameVideo());
		addMotdData("bundle_sale",                 new MOTDDialogBundleSale());
		addMotdData("buy_page_perk",					new MOTDDialogDataBuyPagePerk());
		addMotdData("buy_page_dynamic",					new MOTDDialogDataBuyPageDynamic());
		addMotdData("coin_sweepstakes_motd",			new MOTDDialogDataCreditSweepstakes());
		addMotdData("collectables_motd", 				new MOTDDialogDataCollectables());
		addMotdData("collectables_rewards_increased",	new MOTDDialogDataCollectablesRewardsIncreased());
		addMotdData("collectables_end", 				new MOTDDialogDataCollectablesEnded());
		addMotdData("daily_challenge", 					new MOTDDialogDataDailyChallenge());
		addMotdData("daily_rival_ftue", 				new MOTDDialogDataDailyRivals());
		addMotdData("deluxe_games",						new MOTDDialogDataDeluxeGames());
		addMotdData("dynamic_motd",						new MOTDDialogDataDynamic());
		addMotdData("dynamic_motd_v2", 					new MOTDDialogDataDynamicMOTDV2());
		addMotdData("early_access",						new MOTDDialogDataEarlyAccess());
		addMotdData("eue_daily_bonus_force_collect",	new MOTDDialogDataEUEDailyBonusForceCollect());
		addMotdData("ftue_royal_rush", 					new MOTDDialogDataRoyalRush());
		addMotdData("first_purchase_offer_motd",		new MOTDDialogDataFirstPurchaseOffer());
		addMotdData("happy_hour_sale",					new MOTDDialogDataHappyHourSale());
		addMotdData("hir_dynamic_video_motd",			new MOTDDialogDataDynamicVideo());
		addMotdData("increase_mystery_gift_chance",		new MOTDDialogDataIncreaseMysteryGiftChance());
		addMotdData("increase_big_slice_chance",		new MOTDDialogDataIncreaseBigSliceChance());
		addMotdData("jackpot_unlock_game",				new MOTDDialogDataJackpotUnlockGame());
		addMotdData("level_up_bonus_coins",				new MOTDDialogDataLevelUpBonusCoins());
		addMotdData("linked_vip_program",				new MOTDDialogDataLinkedVIPProgram());
		addMotdData("lucky_deal",						new MOTDDialogLuckyDeal());
		addMotdData("max_voltage_lobby",				new MOTDDialogDataMaxVoltage());
		addMotdData("max_voltage_unlock",				new MOTDDialogDataMaxVoltage());
		addMotdData("mfs_ask_for_credits",				new MOTDDialogDataMFSAskForCredits());
		addMotdData("mobile_xpromo_rtl",				new MOTDDialogDataMobileXpromo());
		addMotdData("motd_daily_challenge", 			new MOTDDialogDataDailyChallenge());
		addMotdData("motd_doublespinvip",				new MOTDDialogDataDoubleFreeSpins());
		addMotdData("motd_jackpot_days",				new MOTDDialogDataJackpotDays());
		addMotdData("motd_multiprogressive_jackpot",	new MOTDDialogDataMultiProgressive());
		addMotdData("motd_new_loz",						new MOTDDialogDataLOZ());
		addMotdData("motd_spin_panel_v2",				new MOTDDialogDataSpinPanelV2());
		addMotdData("motd_slotventures", 				new MOTDDialogDataSlotventures());
		addMotdData("motd_slotventures_complete", 		new MOTDDialogDataSlotventures());
		addMotdData("motd_slotventures_ended", 			new MOTDDialogDataSlotventures());		
		addMotdData("network_profile_1.5",				new MOTDDialogDataNetworkProfileNew());
		addMotdData("network_profile_tooltip",			new MOTDDialogDataNetworkProfileTooltip());
		addMotdData("network_friends",					new MOTDDialogDataNetworkFriends());
		addMotdData("network_friends_reminder",			new MOTDDialogDataNetworkFriendsReminder());
		addMotdData("new_game_mvz", 					new MOTDDialogDataMaxVoltageNewGame());
		addMotdData(MOTDDialogDataNewGame.MOTD_KEY,		new MOTDDialogDataNewGame());
		addMotdData("partner_powerup",					new MOTDDialogPartnerPowerup());
		addMotdData("payer_reactivation_sale",			new MOTDDialogDataPayerReactivationSale());
		addMotdData("popcorn_sale",						new MOTDDialogDataPopcornSale());
		addMotdData("post_purchase_challenge",          new MOTDDialogDataPostPurchaseChallenge());
		addMotdData("prize_pop", 						new MOTDDialogDataPrizePop());
		addMotdData("prize_pop_video", 				new MOTDDialogDataPrizePopVideo());
		addMotdData("quest_for_the_chest",				new MOTDDialogDataQFC());
#if RWR
		addMotdData("real_world_rewards",				new MOTDDialogDataRealWorldRewards());
#endif
		addMotdData("reduced_daily_bonus_time",			new MOTDDialogDataReducedDailyBonusTime());
		addMotdData("rich_pass",						new MOTDDialogDataRichPass());
		addMotdData("robust_challenges", 				new MOTDDialogDataRobustChallenges());
		addMotdData("software_update",					new MOTDDialogDataSoftwareUpdate());
		addMotdData("lifecycle_dialog",					new MOTDDialogDataLifecycleSales());
		addMotdData("starter_dialog",					new MOTDDialogDataStarterDialog());
		addMotdData("starter_pack:design1",				new MOTDDialogDataStarterDialog());
		addMotdData("starter_pack:design2",				new MOTDDialogDataStarterDialog());
		addMotdData("ticket_tumbler",					new MOTDDialogTicketTumbler());
		addMotdData("ticket_tumbler_motd",				new MOTDDialogDataTicketTumblerV2());
		addMotdData("tos_update",						new MOTDDialogDataTOSUpdate());
		addMotdData("two_for_one_sale",					new MOTDDialogDataTwoForOneSale());
		addMotdData("unlock_all_games",					new MOTDDialogDataUnlockAllGames());
		addMotdData("vip_new_lobby",					new MOTDDialogDataVIPRevamp());
		addMotdData("vip_new_lobby_game",				new MOTDDialogDataVIPRevampNewGame());		
		addMotdData("vip_phone_collection",				new MOTDDialogDataVIPPhone());
		addMotdData("vip_sale",							new MOTDDialogDataVIPSale());
		addMotdData("vip_status_boost",					new MOTDDialogDataVIPStatusEvent());
		addMotdData("watch_to_earn",					new MOTDDialogDataWatchToEarn());
		addMotdData("weekly_race_motd",					new MOTDDialogDataWeeklyRace());
		addMotdData("welcome_journey",					new MOTDDialogDataWelcomeJourney());
		addMotdData("xp_multiplier",					new MOTDDialogDataXpMultiplier());
#if !ZYNGA_PRODUCTION
		// Testing MOTDs
		//addMotdData("test_timeout_motd",				new MOTDDialogDataTestTimeout());
#endif

		JSON item;
		for (int i = 0; i < items.Length; i++)
		{
			item = items[i];
			// Do device-specific validation all right here in one place.
			// Only create the object if it is valid for the device,
			// so we don't have to validate for device anywhere else.
			bool isValidForDevice = false;

#if UNITY_IPHONE
			if (item.getBool("show_device_ios", false))
			{
				isValidForDevice = true;
			}
#elif ZYNGA_KINDLE
			if (item.getBool("show_device_kindle", false))
			{
				isValidForDevice = true;
			}
#elif UNITY_ANDROID
			if (item.getBool("show_device_android", false))
			{
				isValidForDevice = true;
			}
#elif UNITY_WEBGL
			if (item.getBool("show_device_unityweb", false))
			{
				isValidForDevice = true;
			}
#elif UNITY_WSA_10_0 //SMP this may need to be WSA specific
			if (item.getBool("show_device_windows", false))
			{
				isValidForDevice = true;
			}
#else
			// This shouldn't happen, unless we're targeting some new platform that isn't yet handled above.
			isValidForDevice = true;
#endif

			if (isValidForDevice && SkuResources.isCorrectSku(item.getInt("sku_id", -1)))
			{
				MOTDDialogData data = find(item.getString("key_name", ""));

				if (data == null)
				{
					// Not in the custom list above, so create a base object now.
					data = new MOTDDialogData(item);

					if (all.ContainsKey(data.keyName))
					{
						Debug.LogWarning("Duplicate MOTDDialogData key: " + data.keyName);
					}
					else
					{
						addMotdData(data.keyName, data);
					}
				}
				else
				{
					// The object already exists from the custom list above.
					data.setData(item);
				}

				if (!data.isValidTokens)
				{
					all.Remove(data.keyName);
				}
			}
		}
	}

	private static bool inRange(string range, int value)
	{
		range = range.Trim('[',']');
		int separator = range.IndexOf(':');

		int min = Convert.ToInt32(range.Substring(0, separator));
		int max = Convert.ToInt32(range.Substring(separator + 1));

		return ((min <= value) && (value <= max));
	}

	// Returns true if the MOTD data is valid for display to the user, false otherwise
	private bool checkForValidTokens(string[] tokens)
	{
		for (int i = 0; i < tokens.Length; i++)
		{
			string token = tokens[i].ToLower();
			bool isValid = true;

			// Filter anonymous players:
			if (token == "-loggedin")
			{
				if (SlotsPlayer.isAnonymous)
				{
					//Debug.LogWarning("OMITTED -loggedin : " + keyName);
					isValid = false;
				}
			}
			else if (token == "-anonymous")
			{
				if (!SlotsPlayer.isAnonymous)
				{
					//Debug.LogWarning("OMITTED -anonymous : " + keyName);
					isValid = false;
				}
			}
			// Filter payers / non-payers:
			else if (token == "-freeplayer")
			{
				if (SlotsPlayer.instance.isPayerMobile)
				{
					//Debug.LogWarning("OMITTED -freeplayer : " + keyName);
					isValid = false;
				}
			}
			else if (token == "-paidplayer")
			{
				if (!SlotsPlayer.instance.isPayerMobile)
				{
					//Debug.LogWarning("OMITTED -paidplayer : " + keyName);
					isValid = false;
				}
			}
			// Filter players that have rated the game:
			else if (token == "-ifnotrated")
			{
				if (RateMe.hasPromptBeenAccepted)
				{
					//Debug.LogWarning("OMITTED -ifnotrated : " + keyName);
					isValid = false;
				}
			}
			// Filter based on level/vip level:
			else if (token.FastStartsWith("-level"))
			{
				string range = token.Substring(6);
				if (!string.IsNullOrEmpty(range) && !inRange(range, SlotsPlayer.instance.socialMember.experienceLevel))
				{
					//Debug.LogWarning("OMITTED -level : " + keyName);
					isValid = false;
				}
			}
			else if (token.FastStartsWith("-viplevel"))
			{
				string range = token.Substring(9);
				if (!string.IsNullOrEmpty(range) && !inRange(range, SlotsPlayer.instance.vipNewLevel))
				{
					//Debug.LogWarning("OMITTED -viplevel : " + keyName);
					isValid = false;
				}
			}
			// Filter based on IDFA value.
			else if (token.FastStartsWith("-idfa"))
			{
				string idfa = token.Substring(5);
				if (getIDFAGroup().ToString() != idfa)
				{
					isValid = false;
				}
			}
			// Filter based on minimum build version.
			else if (token.FastStartsWith("-minversion"))
			{
				int minVersion = 0;

				try
				{
					minVersion = int.Parse(token.Substring(11));
				}
				catch
				{
					Debug.LogError("Invalid MOTD argument: " + token);
					isValid = false;
				}

				if (isValid)
				{
					// We know the version is in the format X.X.X but we only need the last part.
					string[] parts = Glb.clientVersion.Split('.');
					int version = int.Parse(parts[parts.Length - 1]);

					if (version < minVersion)
					{
						isValid = false;
					}
				}
			}
			// Filter based on start/end time:
			else if (token.FastStartsWith("-timestart"))
			{
				string dateTimeParam = token.Substring(10);
				if (!string.IsNullOrEmpty(dateTimeParam))
				{
					// Prepare to make a comparison:
					dateTimeParam = dateTimeParam.Trim('[',']');
					string timeNow = DateTime.Now.ToString("yyyyMMddHHmmss");

					// If we're before the start time, ignore this dialog:
					if (String.Compare(timeNow, dateTimeParam) < 0)
					{
						//Debug.LogWarning("OMITTED -timestart : " + keyName);
						isValid = false;
					}
				}
			}
			else if (token.FastStartsWith("-timeend"))
			{
				string dateTimeParam = token.Substring(8);
				if (!string.IsNullOrEmpty(dateTimeParam))
				{
					// Prepare to make a comparison:
					dateTimeParam = dateTimeParam.Trim('[',']');
					string timeNow = DateTime.Now.ToString("yyyyMMddHHmmss");

					// If we're *after* the end time, ignore this dialog:
					if (String.Compare(timeNow, dateTimeParam) > 0)
					{
						//Debug.LogWarning("OMITTED -timeend : " + keyName);
						isValid = false;
					}
				}
			}	

			if (!isValid)
			{
				otherNoShowReasons += "invalid token: " + token + "\n";
				return false;
			}
		}

		return true;
	}

	public static void checkForDynamicMotd()
	{
		// Now looks for the dynamic MOTD.
		if (ExperimentWrapper.SegmentedDynamicMOTD.isInExperiment)
		{
			if (CustomPlayerData.getInt(CustomPlayerData.DYNAMIC_MOTD_LAST_DATA, 0) != ExperimentWrapper.SegmentedDynamicMOTD.uniqueId)
			{
				// If this is a new dynamic MOTD, then store it in prefs, and reset any prefs for the last dynamic MOTD.
				CustomPlayerData.setValue(CustomPlayerData.DYNAMIC_MOTD_LAST_DATA, ExperimentWrapper.SegmentedDynamicMOTD.uniqueId);
				CustomPlayerData.setValue(CustomPlayerData.DYNAMIC_MOTD_LAST_SHOW_TIME, 0);
				CustomPlayerData.setValue(CustomPlayerData.DYNAMIC_MOTD_VIEW_COUNT, 0);	  
			}

			if (MOTDDialogDataDynamic.instance != null)
			{
				ExperimentWrapper.SegmentedDynamicMOTD.setDialogData(MOTDDialogDataDynamic.instance);
			}
			else
			{
				Debug.Log("MOTDialogData -- populateAll -- no dynamic_motd found");
			}
		}
	}
	// Consistently returns either 1 or 2 for users with the same IDFA
	// Returns 0 for users with no IDFA
	public static int getIDFAGroup()
	{
#if UNITY_IPHONE
		if (UnityEngine.iOS.Device.advertisingIdentifier != null && UnityEngine.iOS.Device.advertisingIdentifier.Length > 0)
		{
			return ((int)UnityEngine.iOS.Device.advertisingIdentifier[0] % 2) + 1;
		}
#endif
		return 0;
	}

	public static MOTDDialogData find(string key)
	{
		if (key == null)
		{
			Debug.LogError("MOTDDialogData.cs -- find -- Tried to find a dialog with a null value for the key");
			return null;
		}
		string sanitizedKey = key.Trim();
		MOTDDialogData data; 
		if (all != null && all.TryGetValue(sanitizedKey, out data))
		{
			return data;
		}
		return null;
	}

	// Implements IResetGame
	public static void resetStaticClassData()
	{
		all = null;
	}
}
