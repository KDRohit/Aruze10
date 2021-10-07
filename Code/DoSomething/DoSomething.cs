using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using QuestForTheChest;

/*
Common function for doing something that is in the form of a string that gets parsed.
This is used by MOTD, carousel, HyperLinker, and more.
*/

public static class DoSomething
{
	private delegate void DoSomethingDelegate(string parameter);

	public const string GAME_PREFIX = "game";				// Prefix for any game string that requires a game key parameter
	public const string GIFT_PREFIX = "gift";				// Prefix for the gift chest to decide which tab to open.
	public const string MOTD_PREFIX = "motd";				// Prefix for any MOTD string that requires a MOTD key parameter
	public const string URL_PREFIX = "url";				    // Prefix for any url string that requires a URL key parameter
	public const string CASE_SENSITIVE_URL_PREFIX = "cs_url";  //Prefix for any case sensitive url string that requres a URL key parameter
	public const string XPROMO_PREFIX = "xpromo";			// Prefix for any xpromo string that requires a xpromo key parameter
	public const string STARTER_PREFIX = "starter_pack";	// Prefix for any starter pack string that requires a starter pack key parameter
	public const string LIFECYCLE_SALE_PREFIX = "lifecycle";	// Prefix for any lifecycle sale string that requires a lifecycle key parameter
	public const string XP_MULTIPLIER_PREFIX = "xp_multiplier";	// Prefix for any xp multiplier event. The parameter is the multiplier.
	public const string REWARD_PREFIX = "reward"; // Prefix for any reward action.
	public const string TROPHY_PREFIX = "trophy"; //Prefix for any achievement key  
	public const string RECOMMENDED_GAME_PREFIX = "ds_recommended";  // prefix for any recommended game action
	public const string FAVORITE_GAME_PREFIX = "ds_favorite";        // prefix for any favorite game action
	
	private static Dictionary<string, DoSomethingAction> validStrings = new Dictionary<string, DoSomethingAction>()
	{
		// Note: If adding or removing strings, please also update the wiki appropriately:
		// https://wiki.corp.zynga.com/display/hititrich/Carousel+Slides+2.0
		{ "appstore",                           new DoSomethingAppstore() },
		{ "buycoins_new",                       new DoSomethingBuyCoinsNew() },
		{ "buycoins_new_sale",                  new DoSomethingBuyCoinsNewSale() },
		{ "buycoins_more_cards",                new DoSomethingMoreCards() },
		{ "buycoins_more_rare_cards",           new DoSomethingMoreRareCards() },
		{ "bundle_sale",                        new DoSomethingBundleSale()},
		{ "credits_sweepstakes",                new DoSomethingCreditsSweepstakes() },
		{ "collectables_album",                 new DoSomethingCollectablesAlbum() },
		{ "collectables_dynamic",               new DoSomethingCollectablesDynamic() },
		{ "daily_bonus",						new DoSomethingDailyBonus() }, 
		{ "daily_bonus_reduced_time",           new DoSomethingDailyBonusReducedTime() },
		{ "daily_challenge",                    new DoSomethingDailyChallenge() },
		{ "daily_rival_ftue",                   new DoSomethingDailyRivalsFTUE() },
		{ "dynamic_motd",                       new DoSomethingDynamicMOTD() },
		{ "dynamic_motd_v2",                    new DoSomethingDynamicMOTDV2() },
		{ "dynamic_video",                      new DoSomethingDynamicVideo() },
		{ "open_elite_pass",                    new DoSomethingElite() },
		{ "elite_ftue",                         new DoSomethingEliteFTUE() },
		{ "facebook_connect",                   new DoSomethingFacebookConnect() },
		{ "flash_sale",                         new DoSomethingFlashSale() },
		{ "zis_sign_in",                        new DoSomethingZIS() },
		{ "first_purchase_offer",               new DoSomethingFirstPurchaseOffer() },
		{ "happy_hour_sale",                    new DoSomethingHappyHourSale() },
		{ "invite",                             new DoSomethingInvite() },
		{ "jackpot_days",                       new DoSomethingJackpotDays() },
		{ "linked_vip_connect",                 new DoSomethingLinkedVipConnect() },
		{ "linked_vip_connect_coins",           new DoSomethingLinkedVipConnectCoins() },
		{ "linked_vip_connect_rewards",         new DoSomethingLinkedVipConnectRewards() },
		{ "loz_lobby",                          new DoSomethingLOZLobby() },
		{ "vip_lobby",                          new DoSomethingVIPRevampLobby() },
		{ "max_voltage_lobby",                  new DoSomethingMaxVoltageLobby() },
		{ "sin_city_strip_lobby",               new DoSomethingSinCityLobby() },
		{ "next_unlock",                        new DoSomethingNextUnlock() },
		{ "payer_reactivation_sale",            new DoSomethingPayerReactivationSale() },
		{ "popcorn_sale",                       new DoSomethingPopcornSale() },
		{ "post_purchase_challenge",            new DoSomethingPostPurchaseChallenge() },
		{ "powerups_ftue",                      new DoSomethingPowerupsFTUE() },
		{ "prize_pop",                      	new DoSomethingPrizePop() },
		{ "proton_feature_dialog",              new DoSomethingProtonFeatureDialog() },
		{ "proton_feature_video",               new DoSomethingProtonFeatureVideo() },
		{ "wheel_deal",                         new DoSomethingLuckyDeal() },
		{ "ppu_portal",                         new DoSomethingPartnerPowerup() },
		{ "personalized_content",               new DoSomethingPersonalizedContent() },
		{ "quest_for_the_chest",                new DoSomethingQFC() },
		{ "reprice_video",                      new DoSomethingRepriceVideo() },
		{ "rich_pass",                          new DoSomethingRichPass() },
		{ "royal_rush",                         new DoSomethingRoyalRush() },
		{ ReactivateFriend.CAROUSEL_ACTION,     new DoSomethingReactivateFriend() },
		{ "robust_challenges_motd",             new DoSomethingRobustChallenges() },
		{ "robust_challenges_eue_motd",         new DoSomethingRobustChallengesEUE() },
		{ "slotventure",                        new DoSomethingSlotventure() },
		{ "sku_game_unlock_motd",               new DoSomethingSkuGameUnlockMotd() },
		{ "sku_game_unlock",                    new DoSomethingSkuGameUnlock() },
		{ "streak_sale",						new DoSomethingStreakSale() },
		{ "tos_accept",                         new DoSomethingTosAccept() },
		{ "ticket_tumbler",                     new DoSomethingTicketTumbler() },
		{ "url_support",                        new DoSomethingUrlSupport() },
		{ "url_terms",                          new DoSomethingUrlTerms() },
		{ "url_privacy",                        new DoSomethingUrlPrivacy() },
		{ "lifecycle_dialog",                   new DoSomethingLifecycleSales() },
	#if RWR
		{ "url_rwr_sweepstakes_legal",			new DoSomethingUrlRwrSweepstakesLegal() },
	#endif
		{ "vip_emerald_game",                   new DoSomethingVipEmeraldGame() },
		{ "vip_room",                           new DoSomethingVipRoom() },
		{ "vip_sale",                           new DoSomethingVipSale() },
		{ "vip_status_boost",                   new DoSomethingVIPStatusBoost() },
		{ "virtual_pet",                        new DoSomethingVirtualPet()},
		{ "watch_to_earn",                      new DoSomethingWatchToEarn() },
		{ "zade",                               new DoSomethingZade() },
		{ CarouselPanelZade.DO_SOMETHING,       new DoSomethingZadeXpromoCarousel() },
		{ UnlockAllGamesMotd.DO_SOMETHING,      new DoSomethingUnlockAllGames() },
		{ GAME_PREFIX,                          new DoSomethingGame() },
		{ REWARD_PREFIX,                        new DoSomethingReward() },
		{ MOTD_PREFIX,                          new DoSomethingMotd() },
		{ STARTER_PREFIX,                       new DoSomethingStarterPack() },
		{ URL_PREFIX,                           new DoSomethingUrl()},
		{ CASE_SENSITIVE_URL_PREFIX,            new DoSomethingUrl()},
		{ XP_MULTIPLIER_PREFIX,                 new DoSomethingXpMultiplier() },
		{ XPROMO_PREFIX,                        new DoSomethingXpromo() },
		{ TROPHY_PREFIX,                        new DoSomethingTrophies() },
		{ "weekly_race",                        new DoSomethingWeeklyRace() },
		{ "xpromo_v2",                          new DoSomethingXpromoV2() },
		{ "welcome_journey",                    new DoSomethingWelcomeJourney() },
		{ "welcome_back_journey",               new DoSomethingWelcomeBackJourney() },
		{ RECOMMENDED_GAME_PREFIX,              new DoSomethingRecommendedGame() },
		{ FAVORITE_GAME_PREFIX,               	new DoSomethingFavoriteGame() },
		{ "",									new DoSomethingNothing() }
		// Note: If adding or removing strings, please also update the wiki appropriately:
		// https://wiki.corp.zynga.com/display/hititrich/Carousel+Slides+2.0
	};

	// this version takes the already-split action string and simply returns validity
	// parameters are read-only in-only, unlike the other overrides
	public static bool isValidString(string doString, string parameter)
	{
		DoSomethingAction action;
		return isValidString(doString, parameter, out action);
	}

	public static bool splitActionString(ref string actionString, out string parameter)
	{
		int colonIndex = actionString.IndexOf(':');
		if (colonIndex!=-1)
		{
			parameter = actionString.Substring(colonIndex + 1).Trim();   // action string should really not have any whitespace at end, that is user error
			if (string.IsNullOrEmpty(parameter))
			{
				// If there is a colon, then a parameter is expected.
				return false;
			}
			actionString = actionString.Substring(0, colonIndex);  // Note: colon is NOT included.
		}
		else
		{
			parameter = "";
		}
		return true;
	}

	// this version assumes doString is pre-split, does not contain parameter
	public static bool isValidString(string doString, string parameter, out DoSomethingAction action)
	{
		if (!validStrings.TryGetValue(doString, out action))
		{
			return false;
		}

		return action.getIsValidParameter(parameter);
	}
		
	// Parses the string for a possible parameter and validates that it is a valid string.
	// this version takes the original string like 'actionName1' or 'actionName2:paramName'
	// and returns the possibly-modified result
	public static bool isValidString(ref string doString, out string parameter, out DoSomethingAction action)
	{
		action = null;

		if (!doString.FastStartsWith(REWARD_PREFIX) && !doString.FastStartsWith(CASE_SENSITIVE_URL_PREFIX))
		{
			// Adding a special case here for the incentivized link action, which has case sensitive parameters.
			doString = doString.ToLower();
		}

		if(!splitActionString(ref doString, out parameter))
		{
			return false;
		}
			
		if (!validStrings.TryGetValue(doString, out action))
		{
			return false;
		}

		return action.getIsValidParameter(parameter);
	}
	
	
	public static bool isValidString(ref string doString, out string parameter)
	{
		return isValidString(ref doString, out parameter, out DoSomethingAction action);
	}
	
	public static bool shouldCloseInbox(string doString)
	{
		DoSomethingAction action;
		string parameter;
		if (!isValidString(ref doString, out parameter, out action))
		{
			return false;
		}

		return action.shouldCloseInbox(parameter);
	}
	

	// Overload to use when we don't care about getting the doString/parameter strings back.
	public static bool isValidString(string doString)
	{
		string parameter;
		return isValidString(ref doString, out parameter);
	}

	// assumes string is pre-split into action and parameter
	public static void now(string doString, string parameter)
	{
		DoSomethingAction action;
		if (!isValidString(doString, parameter, out action))
		{
			return;
		}

		action.doAction(parameter);
	}
	
	public static void now(string doString)
	{
		DoSomethingAction action;
		string parameter;
		// Split the doString's parameter into a separate variable if applicable,
		// and tell us whether the doString is valid.
		if (!isValidString(ref doString, out parameter, out action))
		{
			return;
		}
		
		action.doAction(parameter);
	}

	// Callback of deactivating a carousel panel.
	public static void onDeactivateCarouselSlide(CarouselData carouselData)
	{
		DoSomethingAction action;
		if (!isValidString(carouselData.actionName, carouselData.actionParameter, out action))
		{
			return;
		}
		action.onDeactivateCarouselSlide(carouselData.actionParameter);
	}

	// Callback of activating a carousel panel.
	public static void onActivateCarouselSlide(CarouselData carouselData)
	{
		DoSomethingAction action;
		if (!isValidString(carouselData.actionName, carouselData.actionParameter, out action))
		{
			return;
		}

	    action.onActivateCarouselSlide(carouselData.actionParameter);
	}

	public static bool getIsValidToSurface(CarouselData carouselData)
	{
		DoSomethingAction action;
		if (!isValidString(carouselData.actionName, carouselData.actionParameter, out action))
		{
			return false;
		}

		return action.getIsValidToSurface(carouselData.actionParameter);
	}

	public static bool getIsValidToSurface(string action)
	{
		string parameter = "";
		// Split the doString's parameter into a separate variable if applicable,
		// and tell us whether the doString is valid.
		if (!isValidString(ref action, out parameter))
		{
			return false;
		}
		
		return validStrings[action].getIsValidToSurface(parameter);
	}

	public static GameTimer getTimer(string doString)
	{
		DoSomethingAction action;
		string parameter;
		// Split the doString's parameter into a separate variable if applicable,
		// and tell us whether the doString is valid.
		if (!isValidString(ref doString, out parameter, out action))
		{
			return null;
		}

		return action.getTimer(parameter);
	}

	public static string getValue(string doString, string key)
	{
		DoSomethingAction action;
		string parameter;
		// Split the doString's parameter into a separate variable if applicable,
		// and tell us whether the doString is valid.
		if (!isValidString(ref doString, out parameter, out action))
		{
			return "";
		}

		return action.getValue(parameter, key);
	}
}

// Base class for all actions to be used with the DoSomething class.
public abstract class DoSomethingAction
{
	public abstract void doAction(string parameter);

	public virtual GameTimer getTimer(string parameter)
	{
		return null;
	}
	
	public virtual bool getIsValidToSurface(string parameter)
	{
		// Default this to true, override if this shouldn't have a carousel slide, or has special logic.
		return true;
	}

	public virtual bool getIsValidToSurface(string actionstring, string eosExperiment="", string[] variantNames=null)
	{
		return true;
	}

	public virtual bool getIsValidParameter(string parameter)
	{
		return true;
	}

	public virtual void onDeactivateCarouselSlide(string parameter)
	{
	}

	public virtual void onActivateCarouselSlide(string parameter)
	{
	}
	
	public virtual string getValue(string parameter, string key)
	{
		return "";
	}

	public virtual bool shouldCloseInbox(string parameter)
	{
		return true;
	}
}
