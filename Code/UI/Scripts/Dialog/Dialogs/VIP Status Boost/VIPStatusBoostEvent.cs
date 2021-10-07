using UnityEngine;
using System.Collections.Generic;

// Yes this has buff in the class name but it's not part of the buff system. we could rename it later.
public class VIPStatusBoostEvent
{
	public static List<string> featureList;
	public static int fakeLevel = 0;

	private const string VIP_PANEL_ASSET_PATH = "Features/VIP Status Boost/Prefabs/VIP Panel Boost Animations";
	private const string PROFILE_DIALOG_ASSET_PATH = "Features/VIP Status Boost/Prefabs/VIP Boost Profile Animation";
	private const string MAIN_LOBBY_OVERLAY_ANIMATION_PATH = "Features/VIP Status Boost/Prefabs/Profile Button Gem Animation";
	private const string DAILY_BONUS_ASSET_PATH = "Features/VIP Status Boost/Prefabs/Daily Bonus Boost Meter";
	private const string VIP_LEVEL_UP_DIALOG_ASSET_PATH = "Features/VIP Status Boost/Prefabs/VIP Level Up Meter";

	// It'd be nice to have a central event to fire for things like:
	// onVIPLevelUp
	// onUserLevelUp
	// onEventEnd
	// onMaxLevelReached - (same as on VIP level up with an extra check I guess)
	// and some way to adjust all our UI we affect. Right now things will be fine after the asset reloads, but lets avoid having to actually do that.

	private static GameTimerRange _featureTimer;

	public static GameTimerRange featureTimer
	{
		get
		{
			if (PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_VIP_BOOSTS_KEY))
			{
				return PowerupsManager.getActivePowerup(PowerupBase.POWER_UP_VIP_BOOSTS_KEY).runningTimer;
			}
			return _featureTimer;
		}
		set { _featureTimer = value; }
	}

	public static bool isEnabled()
	{
		// The feature timer is the only thing we should have to worry about besides being in experiment
		return (PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_VIP_BOOSTS_KEY) || ExperimentWrapper.VIPLevelUpEvent.isInExperiment &&
			featureTimer != null &&
			featureTimer.isActive) &&
			SlotsPlayer.instance != null &&
			VIPLevel.maxLevel != null &&
			SlotsPlayer.instance.vipNewLevel < VIPLevel.maxLevel.levelNumber;
	}

	public static bool isEnabledByPowerup()
	{
		// The feature timer is the only thing we should have to worry about besides being in experiment
		return PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_VIP_BOOSTS_KEY);
	}

	public static int getAdjustedLevel()
	{
		return SlotsPlayer.instance.vipNewLevel + fakeLevel < VIPLevel.maxLevel.levelNumber ? SlotsPlayer.instance.vipNewLevel + fakeLevel : VIPLevel.maxLevel.levelNumber;	
	}

	// Can activate via a timer or just on startup.
	public static void setup(Dict args = null, GameTimerRange sender = null)
	{
		int startTime = 0;
		int endTime = 0;

		// Make sure we're clean every time.
		if (_featureTimer != null)
		{
			_featureTimer.clearEvent();
			_featureTimer.clearLabels();
			_featureTimer.clearSubtimers();
		}

		if (ExperimentWrapper.VIPLevelUpEvent.isInExperiment)
		{
			fakeLevel = ExperimentWrapper.VIPLevelUpEvent.boostAmount;
		    startTime = ExperimentWrapper.VIPLevelUpEvent.startTime;
		    endTime = ExperimentWrapper.VIPLevelUpEvent.endTime;

			_featureTimer = new GameTimerRange (startTime, endTime);
			_featureTimer.registerFunction(onFeatureEnd);
			if (fakeLevel != 0 && fakeLevel < VIPLevel.maxLevel.levelNumber && startTime < GameTimer.currentTime && endTime > GameTimer.currentTime)
			{
				JSON featureListJSON = new JSON (ExperimentWrapper.VIPLevelUpEvent.featureList);
				VIPLevel fakedLevel = VIPLevel.find(getAdjustedLevel());

				setInboxLimits(fakedLevel.creditsGiftLimit, fakedLevel.freeSpinLimit);
			}
			else if (startTime > GameTimer.currentTime)
			{
				_featureTimer.registerFunction(setup);
			}
		}

		if (PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_VIP_BOOSTS_KEY))
		{
			PowerupBase powerup = PowerupsManager.getActivePowerup(PowerupBase.POWER_UP_VIP_BOOSTS_KEY);
			onPowerupActivated(powerup);
		}
		else if (PowerupsManager.isPowerupsEnabled)
		{
			PowerupsManager.addEventHandler(onPowerupActivated);
		}
	}

	private static void onPowerupActivated(PowerupBase powerup)
	{
		if (powerup.name == PowerupBase.POWER_UP_VIP_BOOSTS_KEY)
		{
			VIPBoostPowerup vipPowerup = powerup as VIPBoostPowerup;
			if (vipPowerup != null)
			{
				int boostAmount = vipPowerup.boostAmount >= ExperimentWrapper.VIPLevelUpEvent.boostAmount
					? vipPowerup.boostAmount
					: ExperimentWrapper.VIPLevelUpEvent.boostAmount;
				fakeLevel = boostAmount;
				powerup.runningTimer.registerFunction(onFeatureEnd);

				VIPLevel fakedLevel = VIPLevel.find(getAdjustedLevel());

				setInboxLimits(fakedLevel.creditsGiftLimit, fakedLevel.freeSpinLimit);
			}
		}
	}

	protected static void setInboxLimits(int creditGiftLimit, int freeSpinLimit)
	{
		SlotsPlayer.instance.creditsAcceptLimit.setLimit(creditGiftLimit);
		SlotsPlayer.instance.giftBonusAcceptLimit.setLimit(freeSpinLimit);
	}

	private static void onFeatureEnd(Dict args = null, GameTimerRange sender = null)
	{
		// Null checked in case of any craziness.
		if (SlotsPlayer.instance != null && !isEnabled())
		{
			VIPLevel actualLevel = VIPLevel.find(SlotsPlayer.instance.vipNewLevel);

			if (actualLevel != null)
			{
				setInboxLimits(actualLevel.creditsGiftLimit, actualLevel.freeSpinLimit);
			}
		}
	}

	#region ASSET_LOADING
	public static void loadVIPRoomAssets()
	{
		AssetBundleManager.load(VIP_PANEL_ASSET_PATH, loadVIPRoomAssetsSuccess, vipStatusEventAssetLoadFail);
	}

    public static void loadNewVIPRoomAssets(GameObject parent)
	{
        Dict args = Dict.create(D.DATA, parent);

        // Re-use this success callback and path since it does what we want anyway.
		AssetBundleManager.load(MAIN_LOBBY_OVERLAY_ANIMATION_PATH, loadVIPLevelUpAssetSuccess, vipStatusEventAssetLoadFail, args);
	}

	public static void loadProfileDialogAssets()
	{
		AssetBundleManager.load(PROFILE_DIALOG_ASSET_PATH, loadProfileDialogAssetsSuccess, vipStatusEventAssetLoadFail);
	}

	public static void loadMainLobbyAssets()
	{
		AssetBundleManager.load(MAIN_LOBBY_OVERLAY_ANIMATION_PATH, loadMainLobbyAssetsSuccess, vipStatusEventAssetLoadFail);
	}

	public static void loadVIPLevelUpAssets(GameObject parentObject)
    {
		Dict args = Dict.create(D.DATA, parentObject);
		AssetBundleManager.load(VIP_LEVEL_UP_DIALOG_ASSET_PATH, loadVIPLevelUpAssetSuccess, vipStatusEventAssetLoadFail, args);
	}
	#endregion

	#region ASSET_LOADING_CALLBACKS
	private static void loadVIPRoomAssetsSuccess(string assetPath, Object obj, Dict data = null)
	{
		GameObject animation = obj as GameObject;
		NGUITools.AddChild((VIPLobby.instance as VIPLobbyHIR).microVIPEventAnchor, animation);
	}

	private static void loadProfileDialogAssetsSuccess(string assetPath, Object obj, Dict data = null)
	{
		NetworkProfileDialog dialogToUse = Dialog.instance.findOpenDialogOfType("network_profile") as NetworkProfileDialog;

		if (dialogToUse != null)
		{
			GameObject animation = obj as GameObject;
			NGUITools.AddChild(dialogToUse.vipStatusBoostAnchor, animation);
		}
		else
		{
			Debug.LogWarning("loadProfileDialogAssetsSuccess - NetworkProfileDialog was null when we loaded");
		}
	}

	private static void loadMainLobbyAssetsSuccess(string assetPath, Object obj, Dict data = null)
	{
		if (Overlay.instance.topHIR != null)
		{
			GameObject animation = obj as GameObject;
			NGUITools.AddChild(Overlay.instance.topHIR.profileButton.gemCycleAnchor, animation);
		}
	}

	private static void loadVIPLevelUpAssetSuccess(string assetPath, Object obj, Dict data = null)
	{
		GameObject parent = (GameObject)data.getWithDefault(D.DATA, null);

		if (parent != null)
		{
			GameObject animation = obj as GameObject;
			NGUITools.AddChild(parent, animation);
		}
	}

	#endregion

	// Generic fail callback.
	private static void vipStatusEventAssetLoadFail(string assetPath, Dict data = null)
	{
		Debug.LogError("Failed to load object at path " + assetPath);
	}
}
