using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.HitItRich.EUE;
using Com.Scheduler;
using TMPro;
using Facebook.Unity;

/**
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
**/

public abstract class LevelUpDialog : DialogBase
{
	public const string FB_EVENT_LEVELUP = "fb_mobile_level_achieved";
	public const string FB_EVENT_VIP_LEVELUP = "fb_mobile_vip_level_achieved";
	public const string FB_EVENT_PARAM_LEVEL = "fb_level";
	public const string FB_EVENT_PARAM_VIP_LEVEL = "fb_vip_level";
	
	public TextMeshPro levelLabel;
	public TextMeshPro passiveRewardsLabel;
	
	public TextMeshPro collectButtonLabel;

	public TextMeshPro rewardCoinsLabel;
	public TextMeshPro rewardPointsLabel;

	public GameObject rewardCoinContainer;
	public GameObject rewardPointsContainer;
	
	protected bool _isPassiveMode = false;
	protected long startingCoinBeforeLevel = 0;
	protected long bonusCreditsForLevel = 0;
	protected int bonusVipForLevel = 0;
	
	protected int oldLevel;
	protected int newLevel;
	protected long prevMult = 0;
	protected long newMult = 0;
	protected bool _forceClose = false;	///< Let's us know whether the dialog needs to close because it has invalid data.
	
	public static int eventNewLevel = 0; 	///< Stores the new level so we know what level we just got to, since multiple level ups happen in a single event now.
	public static int passiveLevelUps = 0;	///< How many times the player has leveled up passively since looking at the levelup dialog.
	public static bool isDevLevelUp = false;	// Set to true if force-leveling to a specific level from the dev panel, to suppress dialogs.
	
	public delegate void OnLevelUpDelegate(int level);
	private static Dictionary<int, OnLevelUpDelegate> levelUpEventsDict = new Dictionary<int,OnLevelUpDelegate>();
	private static Dictionary<int, OnLevelUpDelegate> vipLevelUpEventsDict = new Dictionary<int,OnLevelUpDelegate>();

	/// Initialization
	public override void init()
	{
		// Show this dialog only once per batch of levelups, even if the player
		// levels up more than once at a time. This means we need to tally up all the rewards
		// for all the levels that were attained during this level up event.

		// Read arguments from the dialog dictionary:
		newLevel = (int)dialogArgs.getWithDefault(D.NEW_LEVEL, 0);
		_isPassiveMode = (newLevel == 0);
		
		oldLevel = SlotsPlayer.instance.socialMember.experienceLevel;

		if (_isPassiveMode)
		{
			// When using passive mode, the leveling up has actually already happened before showing the dialog,
			// so we need to calculate the old level and new level slightly differently.
			newLevel = SlotsPlayer.instance.socialMember.experienceLevel;
			oldLevel -= passiveLevelUps;
		}
			
		getLevelUpBonuses(oldLevel, newLevel, out bonusCreditsForLevel, out bonusVipForLevel);

		if (_isPassiveMode)
		{
			collectButtonLabel.text = Localize.textUpper("ok");
			
			passiveRewardsLabel.gameObject.SetActive(true);
			
			if (passiveLevelUps > 1)
			{
				passiveRewardsLabel.text = Localize.text("passive_rewards_dialog_header_{0}", passiveLevelUps);
			}
			else
			{
				passiveRewardsLabel.text = Localize.text("passive_rewards_dialog_header");
			}
		}
		else
		{
			passiveRewardsLabel.gameObject.SetActive(false);

			// FOR NOW ONLY SHOW THE BUTTON AS COLLECT, since we're not currently sharing anything.
			// Localize the button based on whether the player is logged into facebook or not.
			// if (!SlotsPlayer.isFacebookUser)
			// {
			collectButtonLabel.text = Localize.textUpper("collect");
			// }
			// else
			// {
			// 	collectButtonLabel.text = Localize.textUpper("collect_and_share");
			// }
		}
		
		List<long> prevMults = SlotsWagerMultiplier.getMultipliersAtLevel(oldLevel);
		List<long> newMults = SlotsWagerMultiplier.getMultipliersAtLevel(newLevel);

		if (prevMults.Count > 0)
		{
			prevMult = prevMults[prevMults.Count - 1];
		}
		if (newMults.Count > 0)
		{
			newMult = newMults[newMults.Count - 1];
		}
		
		StatsManager.Instance.LogCount("dialog", "level_up", "", "", "collect", "view", newLevel);
	}

	public virtual void Update()
	{
		AndroidUtil.checkBackButton(collectClicked, "dialog", "level_up", "close", "back", "", "");

		if (shouldAutoClose)
		{
			cancelAutoClose();
			closeClicked();
		}
	}

	protected abstract void collectClicked();

    protected virtual void closeClicked()
	{
		cancelAutoClose();
		StatsManager.Instance.LogCount("dialog", "level_up", "close", "click");
		if (_isPassiveMode)
		{
			passiveLevelUps = 0;
		}
		else
		{
			// Passive level ups are already applied before showing the dialog, so only apply here for non-passive mode.
			applyLevelUp(oldLevel, newLevel, bonusCreditsForLevel, bonusVipForLevel);
		}
		Dialog.close();
	}
	
	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
	}
	
	/////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Static methods
	/////////////////////////////////////////////////////////////////////////////////////////////////////////

	/// Gets the total level up bonuses for all the levels between the given old level and the new level.
	public static void getLevelUpBonuses(int oldLevel, int newLevel, out long credits, out int vipPoints)
	{
		credits = 0;
		vipPoints = 0;

		//Go though every level that we leveled up and calculate the amount of points earned.
		for (int i = oldLevel + 1; i <= newLevel; i++)
		{
			ExperienceLevelData curLevel = ExperienceLevelData.find(i);

			if (curLevel == null)
			{
				// Hopefully this will NEVER happen.
				Debug.LogError("ExperienceLevelData not found for level " + i);
				return;
			}
		    credits += curLevel.bonusAmt + curLevel.levelUpBonusAmount;
			vipPoints += curLevel.bonusVIPPoints * curLevel.vipMultiplier;
		}
	}

	/// Applies all bonuses that the player should receive from all levelups from the current level to the new level.
	public static void applyLevelUp(int oldLevel, int newLevel, long credits, int vipPoints)
	{
		// Update a bunch of client-side state so it remains in sync with the server:
		SlotsPlayer.instance.socialMember.experienceLevel = newLevel;

		// SignalManager.instance.dispatchSignal(SignalManager.SET_UI_CREDITS, Data.instance.data.credits.amount + bonusCreditsForLevel);
		if ((!LinkedVipProgram.instance.isEligible || !LinkedVipProgram.instance.isConnected) && !LevelUpUserExperienceFeature.instance.isEnabled)
		{
			// Only add these VIP points if the client is not in the Linked VIP Program, as when they are connected they
			// already have the updated VIP points number from the vip_status event.
			// Increment VIP points:
			SlotsPlayer.instance.addVIPPoints(vipPoints);
		}

		// Don't play the rollup sound when adding credits from a level up,
		// because it usually stomps out other celebretory sounds (especially the terminator,
		// which happens immediately if clicking to close the level up dialog).
		//Don't add the credits if we're using the updated level up sequence, which can't be skipped and shows a rollup
		if (!ExperimentWrapper.RepriceLevelUpSequence.isInExperiment)
		{
			SlotsPlayer.addCredits(credits, "level up", false);
		}
		
		// Update the xp meter to reflect the new relative amount to the next level.
		if (Overlay.instance != null)
		{
			Overlay.instance.top.xpUI.updateXP();
			
			// If the player has reached the minimum level for the level up bonus, make sure it's enabled on the xp meter now.
			Overlay.instance.top.xpUI.checkEventStates();
		}
		
		// If there is a "next unlock" carousel slide, see if it is still valid to show.
		CarouselData slide = CarouselData.findActiveByAction("next_unlock");
		if (slide != null && !slide.getIsValid())
		{
			slide.deactivate();
		}
		
		// We need to update the max bet. That is what updateMaxBet is doing.
		// There's only one wager set in progressive games and mysterygift games in the new system, so it should be updated on level up for all games.
		if (SpinPanel.instance != null)
		{
			SpinPanel.instance.updateMaxBet();
		}

		if (LevelUpUserExperienceFeature.instance.isEnabled)
		{
			for (int level = oldLevel + 1; level <= newLevel; level++)
			{
				// Unlock any games that unlock at all levels above the old one to the new one.
				LobbyGame.unlockGamesForLevel(level, isDevLevelUp);
			}
		}
		
		ExperienceLevelData newLevelData = ExperienceLevelData.find(newLevel);

		if (newLevelData.levelUpBonusAmount > 0)
		{
			long levelUpCredits = newLevelData.bonusAmt;
			int levelUpVipPoints = newLevelData.bonusVIPPoints;
			long totalCredits = levelUpCredits + newLevelData.levelUpBonusAmount;
			if ((SlotBaseGame.instance == null || !SlotBaseGame.instance.isRoyalRushGame) && !ExperimentWrapper.RepriceLevelUpSequence.isInExperiment)
			{
				LevelUpBonusDialog.showDialog(totalCredits, newLevelData.levelUpBonusAmount, levelUpVipPoints, newLevel);
			}
		}

		// Clear it for next time.
		isDevLevelUp = false;

		// We have unlocked some games, so update the SelectGameUnlock game list.
		SelectGameUnlockDialog.setupGameList();
		SelectGameUnlockDialog.isWaitingForLevelUpEvent = false; // Unblock the select game unlock if we were waiting for this event.
	}
	
	public static void registerEventDelegates()
	{	
		// Inbox items are received as several different types of events.
		Server.registerEventDelegate("leveled_up",		levelUpEvent, true);
		Server.registerEventDelegate("vip_level_up",	levelVIPEvent, true);
	}
	
	public static void levelUpEvent(JSON data)
	{
		Debug.Log(string.Format("LevelUpDialog - received leveled_up message for level {0}.", data.getInt("level", 0)));

		// If multiple level up events are received in the same batch,
		// we only care about the last (highest) one here.
		eventNewLevel = data.getInt("level", 0);

		if (LevelUpUserExperienceFeature.instance.isEnabled)
		{
			Overlay.instance.topV2.xpUI.currentState.textCycler.stopAnimation();
		}
		
		GameEvents.trackLevelUp(eventNewLevel);
		UAWrapper.Instance.onLevelUp(eventNewLevel);

		// This is the new way of populating xp levels,
		// which should include the next level after the level that was just achieved.
		ExperienceLevelData.populateAll(data.getJsonArray("xp_levels"));
		
		ExperienceLevelData newLevel = ExperienceLevelData.find(eventNewLevel);
		if (NetworkProfileFeature.instance.isEnabled)
		{
			NetworkProfileFeature.instance.getPlayerProfile(SlotsPlayer.instance.socialMember);
		}

		if (isDevLevelUp)
		{
			// We need to set the player's xp amount to the required amount for the new level,
			// since we didn't do it before sending the add_levels action, because we might
			// not have the ExperienceLevelData for the new level before leveling up to it.
			SlotsPlayer.instance.xp.add(newLevel.requiredXp - SlotsPlayer.instance.xp.amount, "dev level up");
		}

		// LevelUpBonus check for any special events.
		if (data.getBool("level_up_bonus_applied", false))
		{
			// Make sure that the level up bonus coins are taken into account when reaching this level.
			newLevel.levelUpBonusAmount = data.getLong("level_up_bonus_amount", 0);
		}
		
		// FB event tracking
		if (FB.IsLoggedIn)
		{
			Dictionary<string, object> fbEventParams = new Dictionary<string, object>();
			fbEventParams.Add(FB_EVENT_PARAM_LEVEL, eventNewLevel);
			//SIR-9115
			//FB.LogAppEvent(FB_EVENT_LEVELUP, null, fbEventParams);
		}

		// We shouldn't show the level up toaster on special levels (Levels on which you get an inflation factor increase or the level before that)
		bool shouldShowFullscreenInflation = false;
		
		float oldInflationValue = SlotsPlayer.instance.currentBuyPageInflationFactor;
		float oldMaxVoltageValue = SlotsPlayer.instance.currentMaxVoltageInflationFactor;
		JSON currInflationJson = data.getJSON("inflations_current");
		if (currInflationJson != null)
		{
			float currentInflation = currInflationJson.getFloat("buy_page", 1);
			//The inflation value shouldn't actually be decreasing but with the way this gets messed up a little  when leveling up multiple times because of the way the inflations get pre-calculated
			if (currentInflation > oldInflationValue)
			{
				shouldShowFullscreenInflation = true;
				SlotsPlayer.instance.currentBuyPageInflationFactor = currentInflation;
			}
			SlotsPlayer.instance.currentMaxVoltageInflationFactor = currInflationJson.getFloat("power_room", 1);
			SlotsPlayer.instance.currentPjpWagerInflationFactor = currInflationJson.getFloat("pjp_wager", 1);
			SlotsPlayer.instance.currentPjpAmountInflationFactor = currInflationJson.getFloat("pjp_amount", 1);

			if (oldMaxVoltageValue < SlotsPlayer.instance.currentMaxVoltageInflationFactor)
			{
				MaxVoltageTokenCollectionModule.adjustInflationValues(oldMaxVoltageValue);
			}
		}
		SlotsPlayer.instance.currentBuyPageInflationPercentIncrease = data.getFloat("bp_inflation_current_percent", 0);

		JSON nextInflationJson = data.getJSON("inflations_next");
		if (nextInflationJson != null)
		{
			SlotsPlayer.instance.nextBuyPageInflationFactor = nextInflationJson.getFloat("buy_page", 1);

			if (SlotsPlayer.instance.nextBuyPageInflationFactor > SlotsPlayer.instance.currentBuyPageInflationFactor)
			{
				shouldShowFullscreenInflation = true;
			}
		}
		SlotsPlayer.instance.nextBuyPageInflationPercentIncrease = data.getFloat("bp_inflation_next_percent", 0f);

		
#if !ZYNGA_PRODUCTION
		if (DevGUIMenuTools.disableFeatures)
		{
			// Get out of the flow here so we don't break autospins
			return;
		}
#endif

		if (LevelUpUserExperienceFeature.instance.isEnabled && !shouldShowFullscreenInflation)
		{
			long creditsAwarded = newLevel.bonusAmt + newLevel.levelUpBonusAmount;
			long vipPointsAdded = newLevel.bonusVIPPoints * newLevel.vipMultiplier;
			
			SlotsPlayer.addCredits(creditsAwarded, "level up", false);
			SlotsPlayer.instance.addVIPPoints(vipPointsAdded);
			
			Dict args = Dict.create(D.DATA, newLevel,D.KEY, LevelUpUserExperienceToaster.PRESENTATION_TYPE.LEVEL_UP);
			ToasterManager.addToaster(ToasterType.LEVEL_UP, args);
			if (ExperimentWrapper.RoyalRush.isPausingInLevelUps && GameState.game != null)
			{
				string gameKey = GameState.game.keyName;
				RoyalRushInfo rushInfo = RoyalRushEvent.instance.getInfoByKey(gameKey);
				if (rushInfo != null)
				{
					//Unpause instantly on level up if we're showing the toaster instead of game blocking animations
					RoyalRushAction.unPauseLevelUpEvent(gameKey);
				}
			}
		}
		else if (ExperimentWrapper.RepriceLevelUpSequence.isInExperiment)
		{
			if (oldInflationValue < SlotsPlayer.instance.currentBuyPageInflationFactor)
			{
				//Play special level up with increased stuff
				Overlay.instance.topHIR.playLevelUpSequence(true, eventNewLevel);
			}
			else
			{
				//Play normal level up sequence
				Overlay.instance.topHIR.playLevelUpSequence(false, eventNewLevel);
			}
		}

		if (ExperimentWrapper.EUEFeatureUnlocks.isInExperiment)
		{
			if (levelUpEventsDict.ContainsKey(eventNewLevel))
			{
				levelUpEventsDict[eventNewLevel].Invoke(eventNewLevel);
			}
		}

		if (newLevel.level == SocialManager.EMAIL_LOGIN_AUTO_POPUP_LEVEL && PackageProvider.Instance.Authentication.Flow.Account.IsAnonymous)
		{
			if (ExperimentWrapper.ZisPhase2.isInExperiment)
			{
				SocialManager.Instance.CreateAttach(Zynga.Zdk.Services.Identity.AuthenticationMethod.ZyngaEmailUnverified);
			}
		}
	}
	
	public static void levelVIPEvent(JSON data)
	{
		int level = data.getInt("vip_level", 0);
		Debug.Log(string.Format("LevelUpDialog - received vip_level_up message for VIP level {0}.", level));

		// Only show the level dialog if this is actually an increase in the VIP level.
		if (level > SlotsPlayer.instance.vipNewLevel)
		{
			GameEvents.trackVipLevelUp(level);
			long mergeBonus = data.getLong("merge_bonus", 0L);
			if (mergeBonus > 0L)
			{
				// If provided, update this.
				SlotsPlayer.instance.mergeBonus = mergeBonus;
			}
			
			// Advance the VIP level here - so we separate the update functionality from showing a dialog about it:
			VIPLevel oldLevel = VIPLevel.find(SlotsPlayer.instance.vipNewLevel);
			
			SlotsPlayer.instance.vipNewLevel = level;
			
			VIPLevel newLevel = VIPLevel.find(level);
			
			SlotsPlayer.instance.creditsAcceptLimit.setLimit(newLevel.creditsGiftLimit);
			SlotsPlayer.instance.giftBonusAcceptLimit.setLimit(newLevel.freeSpinLimit);

			InboxDialog inbox = Dialog.instance.findOpenDialogOfType("inbox_dialog") as InboxDialog;

			if (inbox != null)
			{
				// If the inbox is currently open, close it to force updated values when reopening it.
				Dialog.close(inbox);
			}

			Overlay.instance.top.setVIPInfo();

			StatsManager.Instance.LogMileStone("vip_status", level);
			StatsManager.Instance.LogMileStone("vip_level", level);
			StatsManager.Instance.LogCount("dialog", "vip_status", "", "view");

			if (MainLobby.instance != null)
			{
				MainLobby.instance.refreshEarlyAccessTag();
			}
			
			if (NetworkProfileFeature.instance.isEnabled)
			{
				NetworkProfileFeature.instance.getPlayerProfile(SlotsPlayer.instance.socialMember);
			}			
			
			DevGUI.isActive = false;
			
			// FB event tracking
			if (FB.IsLoggedIn)
			{
				Dictionary<string, object> fbEventParams = new Dictionary<string, object>();
				fbEventParams.Add(FB_EVENT_PARAM_VIP_LEVEL, level);
				//SIR-9115
				//FB.LogAppEvent(FB_EVENT_VIP_LEVELUP, null, fbEventParams);
			}

			long newMultiplier = data.getLong("gifted_spins_vip_multiplier", 0);
			if (newMultiplier > 0)
			{
				GiftedSpinsVipMultiplier.playerMultiplier = newMultiplier;
			}

			// When we level up lets make sure it gets reflected.
			// VIPStatusBoostEvent.onVIPLevelUp()
			if (ExperimentWrapper.EUEFeatureUnlocks.isInExperiment)
			{
				//If you level up multiple VIP levels at once, we only get one event going straight to the new level
				//Need to check any previous levels and call their unlock events
				for (int i = oldLevel.levelNumber + 1; i <= level; i++)
				{
					if (vipLevelUpEventsDict.ContainsKey(i))
					{
						vipLevelUpEventsDict[i].Invoke(i);
					}
				}
			}
			
			// This dialog may not display immediately if we have a Buy Again dialog open:
			VIPLevelUpDialog.showDialog(level);
		}
	}

	public static void showDialog(int newLevel)
	{
		string imagePath = "";
		
		Dict args = Dict.create(D.NEW_LEVEL, newLevel);
		
		if (imagePath == "")
		{
			Scheduler.addDialog("level_up", args);
		}
		else
		{
			Dialog.instance.showDialogAfterDownloadingTextures("level_up", imagePath, args);
		}
	}

	public static void registerForLevelUpEvent(int level, OnLevelUpDelegate handler)
	{
		OnLevelUpDelegate levelUpDelegate;
		if (levelUpEventsDict.TryGetValue(level, out levelUpDelegate))
		{
			levelUpDelegate -= handler;
        }

		levelUpDelegate += handler;

		levelUpEventsDict[level] = levelUpDelegate;
	}
	
	public static void registerForVIPLevelUpEvent(int level, OnLevelUpDelegate handler)
	{
		OnLevelUpDelegate levelUpDelegate;
		if (vipLevelUpEventsDict.TryGetValue(level, out levelUpDelegate))
		{
			levelUpDelegate -= handler;
		}

		levelUpDelegate += handler;

		vipLevelUpEventsDict[level] = levelUpDelegate;
	}
}
