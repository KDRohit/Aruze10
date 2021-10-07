using System.Collections;
using System.Collections.Generic;
using Zynga.Core.Util;
using UnityEngine;

public class DailyBonusForcedCollection : FeatureBase
{
	public static DailyBonusForcedCollection instance
	{
		get
		{
			return FeatureDirector.createOrGetFeature<DailyBonusForcedCollection>("daily_bonus_forced_collection");
		}
	}

	private const string MOTD_KEY = "eue_daily_bonus_force_collect";

	private PreferencesBase _prefs = null;
	private PreferencesBase prefs
	{
		get
		{
			if (_prefs == null)
			{
				_prefs = SlotsPlayer.getPreferences();
			}
			return _prefs;
		}
	}

	public bool shouldForceCollect()
	{
		bool wasMarkedAsNew = prefs.GetInt(Prefs.DAILY_BONUS_FORCED_WAS_NEW_PLAYER, 0) == 1;
		bool hasCollectedDailyBonus = CustomPlayerData.getBool(CustomPlayerData.DAILY_BONUS_COLLECTED, false);
		bool usesNewFTUE = ExperimentWrapper.EueFtue.isInExperiment;
		return isEnabled && wasMarkedAsNew && !hasCollectedDailyBonus && !usesNewFTUE;
	}

	public string motdNoShowReason
	{
		get
		{
			System.Text.StringBuilder builder = new System.Text.StringBuilder();
			bool wasMarkedAsNew = prefs.GetInt(Prefs.DAILY_BONUS_FORCED_WAS_NEW_PLAYER, 0) == 1;
			bool hasCollectedDailyBonus = CustomPlayerData.getBool(CustomPlayerData.DAILY_BONUS_COLLECTED, false);
			if (isEnabled)
			{
				builder.AppendLine("Feature was disabled.");
			}
			if (!wasMarkedAsNew)
			{
				builder.AppendLine("User was not marked as new previously");
			}
			if (hasCollectedDailyBonus)
			{
				builder.AppendLine("User has already collected the daily bonus");
			}
		    return builder.ToString();
		}
	}

	public void doLobbyCheck()
	{
		if (isEnabled)
		{
			bool wasMarkedAsNew = prefs.GetInt(Prefs.DAILY_BONUS_FORCED_WAS_NEW_PLAYER, 0) == 1;
			bool hasCollectedDailyBonus = CustomPlayerData.getBool(CustomPlayerData.DAILY_BONUS_COLLECTED, false);

			if (hasCollectedDailyBonus)
			{
				// If the user has already collected their daily bonus, force mark the MOTD as seen so we never check it again.
				if (MOTDFramework.sortedMOTDQueue.Contains(MOTD_KEY))
				{
					// Make sure we dont double mark this.
					PlayerAction.markMotdSeen(MOTD_KEY, true);
				}
			}
			else if ((NotificationManager.DayZero || GameExperience.totalSpinCount == 0) && !wasMarkedAsNew)
			{
				// If this is the players first day or they still have 0 spins recorded, and we havent marked them as new, then do so now.
				prefs.SetInt(Prefs.DAILY_BONUS_FORCED_WAS_NEW_PLAYER, 1);
				prefs.Save();
			}
		}
	}

#region FeatureBase overrides
	public override  bool isEnabled
	{
		get
		{
			return base.isEnabled && ExperimentWrapper.DailyBonusNewInstall.isInExperiment;
		}
	}

	public override void drawGuts()
	{
		bool wasMarkedAsNew = prefs.GetInt(Prefs.DAILY_BONUS_FORCED_WAS_NEW_PLAYER, 0) == 1;
		bool hasCollectedDailyBonus = CustomPlayerData.getBool(CustomPlayerData.DAILY_BONUS_COLLECTED, false);
		GUILayout.BeginVertical();
		GUILayout.Label(string.Format("Is Enabled: {0}", isEnabled));
		GUILayout.Label(string.Format("Player marked as new: {0}", wasMarkedAsNew));
		GUILayout.Label(string.Format("isFirstLobby: {0}", MainLobby.isFirstTime));
		GUILayout.Label(string.Format("Is Day Zero: {0}", NotificationManager.DayZero));
		GUILayout.Label(string.Format("Has player collected DailyBonus: {0}", hasCollectedDailyBonus));
		GUILayout.Label(string.Format("Should Force Collect: {0}", shouldForceCollect()));
		if (GUILayout.Button("Show Force Dialog"))
		{
			DailyBonusForceCollectionDialog.showDialog();
		}
		GUILayout.EndVertical();
	}
#endregion
}
