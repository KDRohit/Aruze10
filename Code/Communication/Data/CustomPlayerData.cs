using UnityEngine;
using System;
using System.Collections.Generic;
using Zynga.Core.Util;

public class CustomPlayerData : IResetGame
{
	// Keys - Let's keep these in alphabetical order.
	public const string ACHIEVEMENTS_NEWEST_SEEN_VERSION = "achievements_newest_seen_version"; // The newest trophy version we have seen.
	public const string ACHIEVEMENTS_FTUE_1 = "achievement_ftue_1"; // The achievement ftue steps from 0 to 4
	public const string ACHIEVEMENTS_FTUE_2 = "achievement_ftue_2";
	public const string ACHIEVEMENTS_FTUE_3 = "achievement_ftue_3";
	public const string ACHIEVEMENTS_FTUE_4 = "achievement_ftue_4";
	public const string ACHIEVEMENTS_FTUE_0 = "achievement_ftue_0";
	public const string BUNDLE_SALE_PURCHASE_AMOUNT = "bundle_sale_purchase_amount";
	public const string BUNDLE_SALE_COOLDOWN_START_TIME = "bundle_sale_purchase_cooldown_start";
	public const string BUNDLE_SALE_BUFF_END = "bundle_sale_buff_end";
	public const string CASINO_EMPIRE_BOARD_GAME_FTUE_SEEN = "casino_empire_board_game_ftue_seen";
	public const string CASINO_EMPIRE_BOARD_GAME_SEEN_VERSION = "casino_empire_board_game_seen_version";
	public const string CASINO_EMPIRE_BOARD_GAME_SELECTED_TOKEN = "casino_empire_board_game_selected_token";
	public const string COLLECTED_UPDATE_REWARD = "collected_update_reward";
	public const string DAILY_BONUS_COLLECTED = "daily_bonus_collected";
	public const string DYNAMIC_BUY_PAGE_LAST_SEEN = "buy_page_dynamic_last_seen";
	public const string CHARM_WITH_BUY_VIEWED_MOTD_VERSION = "charm_with_buy_viewed_motd_version";	// End timestamp of last event where the MOTD was seen.
	public const string DAILY_CHALLENGE_DID_WIN = "daily_challenge_did_win"; // Whether or not the user has won the current daily challenge quest.
	public const string DAILY_RIVALS_LAST_SEEN = "daily_rivals_last_seen"; // Start time stamp of when the user last saw the daily rivals pairing dialog
	public const string DOUBLE_FREE_SPINS_MOTD_LAST_SEEN = "double_free_spins_last_seen";	// The hash of the last game key used for the double free spins MOTD.
	public const string DYNAMIC_MOTD_VIEW_COUNT = "dynamic_motd_view_count";
	public const string DYNAMIC_MOTD_LAST_SHOW_TIME = "dynamic_motd_last_show_time";
	public const string DYNAMIC_MOTD_LAST_DATA = "dynamic_motd_last_data"; // The unique string of this MOTD.
	public const string ECONOMY_MIGRATION_BUY_SEEN = "economy_migration_buy_seen"; 	// Whether or not the user has seen the hyper economy buy button intro.
	public const string ECONOMY_MIGRATION_BET_SEEN = "economy_migration_bet_seen";  // Whether or not the user has seen the hyper economy initial bet or in-game bet intro.
	public const string EUE_ACTIVE_DISCOVERY_FRIENDS = "eue_active_discovery_friends"; //whether the player has actively discovered friends tab in player profile
	public const string EUE_ACTIVE_DISCOVERY_LOYALTY_LOUNGE = "eue_active_discovery_loyalty_lounge"; //whether the player has actively discovered cross-promo games in player profile
	public const string EUE_ACTIVE_DISCOVERY_TROPHIES = "eue_active_discovery_trophies"; //whether the player has actively discovered trophies in player profile
	public const string EUE_ACTIVE_DISCOVERY_WEEKLY_RACE = "eue_active_discovery_weekly_race"; //whether the player has actively discovered weekly race
	public const string FLASH_SALE_LAST_SALE_START_DATE = "flash_sale_last_sale_start_date"; //Date when the last flash sale started
	public const string FLASH_SALE_LAST_SALE_END_DATE = "flash_sale_last_sale_end_date"; //Date when the last flash sale ended
	public const string FLASH_SALE_IN_PROGRESS = "flash_sale_in_progress"; //Was a flash sale started last session that is still goin on?
	public const string FTUE_COLLECTED_DAILY_BONUS = "ftue_collected_daily_bonus"; // Whether or not the user has collected their daily bonus before.
	public const string HAS_CLICKED_FIRST_PTR_NODE = "ptr_clicked_first_node";
	public const string HAS_SEEN_RIVAL_LOST = "has_seen_rival_lost";
	public const string HAS_SEEN_RIVAL_PAIRING = "has_seen_rival_pairing";
	public const string LOZ_LOBBY_COMPLETE_SEEN = "loz_lobby_complete_seen";
	public const string MUTE_MUSIC = "preferences_mute_music";
	public const string MUTE_FX = "preferences_mute_fx";
	public const string MOBLE_XPROMO_LAST_VARIANT = "xpromo_last_variant"; //last variant user was in xpromo exp
	public const string PATH_TO_RICHES_SEEN_CHALLENGE_OVER_DIALOG = "path_to_riches_seen_challenge_over"; // Whether or not the user has seen PTR challenge-over dialog
	public const string PATH_TO_RICHES_SEEN_FINAL_WHEEL = "path_to_riches_seen_final_wheel"; // Whether or not the user has seen PTR final wheel dialog
	public const string PLAYER_LOVE_WEEK_LAST_DAY_SEEN_DIALOG = "player_love_week_last_day_seen_dialog"; // The last day (int) that the player viewed the dialog for.
	public const string PLAYER_LOVE_WEEK_LAST_DAY_REWARD_COLLECTED = "player_love_week_last_day_reward_collected"; // The last day (int) that the player collected the reward for.
	public const string PLAYER_LOVE_WEEK_CURRENT_START = "player_love_week_current_start";
	public const string POWERUPS_FTUE_SEEN = "powerups_ftue_seen";
	public const string PROFILES_FOR_EVERYONE_HAS_DONE_NAME_FIX_CHECK = "profiles_for_everyone_has_done_name_fix_check";
	public const string RECENTLY_VIEWED_NEW_GAME_MOTD_MOBILE = "recently_viewed_new_game_motd_mobile";
	public const string SHOWN_INCENTIVE_INVITE_INTRO = "incentivized_friend_invite_seen";
	public const string SLOTVENTURES_CURRENT_LOBBY_LOAD = "slotventures_current_lobby_load";
	public const string SLOTVENTURES_HAS_SEEN_EUE = "slotventures_has_seen_eue_dialog";
	public const string SHOWN_VIP_WELCOME_DIALOG = "shown_vip_welcome_dialog";
	public const string STUD_SALE_VIEWED_HAPPY_HOUR = "stud_sale_viewed_happy_hour";
	public const string STUD_SALE_VIEWED_PAYER_REACTIVATION = "stud_sale_viewed_payer_reactivation";
	public const string STUD_SALE_VIEWED_POPCORN = "stud_sale_viewed_popcorn";
	public const string STUD_SALE_LAST_VIEWED_POPCORN = "stud_sale_last_viewed_popcorn";            
	public const string STUD_SALE_VIEWED_VIP_SALE = "stud_sale_viewed_vip_sale";
	public const string SHOW_AGE_GATE = "show_age_gate"; // Whether or not we should show the age gate dialog to the player (set to 0 when they have passed the check)
	public const string SEEN_SUBSCRIBER_FTUE = "seen_subscriber_ftue"; // Whether or not we should show the subscription FTUE
	public const string SEEN_ACHIEVEMENTS_FTUE = "seen_achievements_ftue"; // Whether or not we should show the achievement FTUE
	public const string SEEN_TERMS_OF_SERVICE = "seen_terms_of_service"; //user has run through the tos at least once
	// Mobile to Mobile Xpromo storage.
	public const string AUTO_POP_XPROMO_OOC_COUNT = "auto_pop_xpromo_ooc_count";
	public const string XPROMO_ART_CHANGE_COUNT = "xpromo_art_change_count";
	public const string XPROMO_ART_VIEW_COUNT = "xpromo_art_view_count";
	public const string AUTO_POP_XPROMO_RTL_COUNT = "auto_pop_xpromo_rtl_count";
	public const string AUTO_POP_XPROMO_LAST_SHOW_TIME = "auto_pop_xpromo_last_shown_time";
	public const string SHOW_COLLECT_ALERTS = "show_collect_alerts";
	public const string LAST_SEEN_QFC_COMPETITION_ID = "last_seen_qfc_competition_id";
	public const string LAST_SEEN_DYNAMIC_VIDEO = "last_seen_dynamic_video";
	public const string LAST_SEEN_RICH_PASS_START_TIME = "last_seen_rich_pass_start_time"; //Timestamp of last known rich pass to determine if a new pass has started
	public const string LAST_SEEN_RICH_PASS_CHALLENGES_TIME = "last_seen_rich_pass_challenges_unlock_time"; //The previous known date of what rich pass challenges have been unlocked
	public const string LAST_SEEN_PRIZE_POP_OOP_TIME = "prize_pop_oop_last_seen";
	public const string LAST_SEEN_PRIZE_POP_INTRO_VIDEO = "prize_pop_video_last_seen";
	public const string USED_ALL_PET_COLLECT_TIMESTAMP = "used_all_pet_collects_timestamp";
	public const string USER_ACTIVITY_TIMESTAMP = "user_activity_timestamp";
	public const string SILENT_FEED_PET = "virtual_pet_silent_feed_pet";
	
	// quest for chest
	public const string QFC_PLAYER_LAST_SEEN_POSITION = "qfc_player_last_seen_position";
		
	// Keys - Let's keep these in alphabetical order.


	///////////////////////////////////////////////////////////////////////////////////////////////

	public string keyName;
	public string lastUpdated;
	public string value;
	public bool isPlayerPref = false;	// true if the key doesn't exist in SCAT, so we're using a local PlayerPref instead.

	private static Dictionary<string, CustomPlayerData> all = new Dictionary<string, CustomPlayerData>();
	private static List<string> validFields = new List<string>();
	
	public CustomPlayerData(string name, string updated, string val)
	{
		keyName = name;
		lastUpdated = updated;
		value = val;
		if (all.ContainsKey(name))
		{
			all[name] = this;
		}
		else
		{
			all.Add(name, this);
		}
	}

	public static void populateFields(JSON[] data)
	{
		foreach (JSON json in data)
		{
			string keyName = json.getString("key_name", "");
			if (!string.IsNullOrEmpty(keyName))
			{
				validFields.Add(keyName);
			}
		}
	}
	
	public static void populateAll(JSON data)
	{
		if (data == null)
		{
			// We won't always have these coming down so this is a valid situation.
			return;
		}
		foreach (string key in data.getKeyList())
		{
			if (validFields.Contains(key))
			{
				JSON blob = data.getJSON(key);
				new CustomPlayerData(
					key,
					blob.getString("last_updated", DateTime.Now.ToString()),
					blob.getString("value", "")
				);
			}
		}
	}
	
	public static string getString(string key, string defaultValue)
	{
		CustomPlayerData data = getData(key, defaultValue);
		if (data != null)
		{
			return data.value;
		}
		return defaultValue;
	}
	
	public static bool getBool(string key, bool defaultValue)
	{
		CustomPlayerData data = getData(key, defaultValue.ToString());
		return getBool(key, defaultValue, data);
	}

	public static bool getBool(string key, bool defaultValue, CustomPlayerData data)
	{
		if (data != null)
		{
			string s = data.value;
			if (s.Length > 0)
			{
				switch (s[0])
				{
					case '0':
					case 'f':
					case 'F':
						return false;

					case '1':
					case '2':
					case '3':
					case '4':
					case '5':
					case '6':
					case '7':
					case '8':
					case '9':
					case 't':
					case 'T':
						return true;
				}
			}
		}
		return defaultValue;
	}

	public static int getInt(string key, int defaultValue)
	{
		CustomPlayerData data = getData(key, defaultValue.ToString());
		if (data != null)
		{
			string str = data.value;
			if (str.Contains('.'))
			{
				// If there is a decimal point, only use the part before it,
				// since int.TryParse() fails on decimal strings.
				str = str.Substring(0, str.IndexOf('.'));
			}
			int value;
			if (int.TryParse(str, out value))
			{
				return value;
			}
		}
		return defaultValue;
	}

	public static float getFloat(string key, float defaultValue)
	{
		CustomPlayerData data = getData(key, defaultValue.ToString());
		if (data != null)
		{
			float value;
			if (float.TryParse(data.value, out value))
			{
				return value;
			}
		}
		return defaultValue;
	}
	
	public static void setValue(string key, bool value)
	{
		if (setValueLocal(key, value.ToString()))
		{
			CustomPlayerDataAction.setCustomPlayerField(key, value);
		}
	}

	public static void setValue(string key, int value)
	{
		if (setValueLocal(key, value.ToString()))
		{
			CustomPlayerDataAction.setCustomPlayerField(key, value);
		}
	}

	public static void setValue(string key, float value)
	{
		if (setValueLocal(key, value.ToString()))
		{
			CustomPlayerDataAction.setCustomPlayerField(key, value);
		}
	}

	public static void setValue(string key, string value)
	{
		if (setValueLocal(key, value.ToString()))
		{
			CustomPlayerDataAction.setCustomPlayerField(key, value);
		}
	}

	// Sets the local value of the custom player data.
	private static bool setValueLocal(string key, string value)
	{
		if (!validFields.Contains(key))
		{
			Debug.LogWarning("CustomPlayerData field " + key + " does not exist in global data.");
		}
		
		CustomPlayerData data = getData(key, value);
		data.value = value;
		data.lastUpdated = DateTime.Now.ToString();
		
		if (data.isPlayerPref)
		{
			PreferencesBase preferences = SlotsPlayer.getPreferences();
			preferences.SetString(key, value);
			preferences.Save();
			return false;
		}
		
		return true;
	}

	private static CustomPlayerData getData(string key, string defaultValue)
	{
		CustomPlayerData data;

		if (all.TryGetValue(key, out data))
		{
			return data;
		}
		else
		{
			data = new CustomPlayerData(key, DateTime.Now.ToString(), defaultValue);
			
			if (!validFields.Contains(key))
			{
				// UsePlayerPrefsCache. as a fallback, but having a custom player data entry in SCAT is preferred.
				data.isPlayerPref = true;
				PreferencesBase preferences = SlotsPlayer.getPreferences();
				data.value = preferences.GetString(key, defaultValue);

				if (validFields.Count == 0)
				{
					Debug.LogWarning("Attempting to save key " + key + " before we know what valid keys we have for custom player data. It will be treated as a local pref");
				}
				else
				{
					Debug.LogWarning("Using PlayerPref value " + data.value + " for key " + key + " because the key doesn't exist for CustomPlayerData.");
				}
			}
			return data;
		}
	}
	
	public static void resetStaticClassData()
	{
		all = new Dictionary<string, CustomPlayerData>();
	}
}
