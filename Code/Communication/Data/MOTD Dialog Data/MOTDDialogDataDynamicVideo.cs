using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Zynga.Core.Util;

/*
Override for special behavior.
*/

public class MOTDDialogDataDynamicVideo : MOTDDialogData
{	
	public override bool shouldShow
	{
		get
		{
			PreferencesBase prefs = SlotsPlayer.getPreferences();
			return !Data.liveData.getBool(VideoDialog.LIVE_DATA_DISABLE_KEY, false) && 
				   ExperimentWrapper.DynamicVideo.isInExperiment && 
			       ExperimentWrapper.DynamicVideo.uniqueId != CustomPlayerData.getInt(CustomPlayerData.LAST_SEEN_DYNAMIC_VIDEO, 0) && 
			       ExperimentWrapper.DynamicVideo.url != prefs.GetString(Prefs.LAST_SEEN_DYNAMIC_VIDEO, "");
		}
	}
	
	public override string noShowReason
	{
		get
		{
			PreferencesBase prefs = SlotsPlayer.getPreferences();
			string reason = base.noShowReason;
			if (!ExperimentWrapper.DynamicVideo.isInExperiment)
			{
				reason += "Not in the experiment.\n";
			}
			if (ExperimentWrapper.DynamicVideo.url == CustomPlayerData.getString(CustomPlayerData.LAST_SEEN_DYNAMIC_VIDEO, "") 
			    || ExperimentWrapper.DynamicVideo.url == prefs.GetString(Prefs.LAST_SEEN_DYNAMIC_VIDEO, ""))
			{
				reason += "Already watched this video.\n";
			}
			return reason;
		}
	}

	public override bool show()
	{
		PreferencesBase prefs = SlotsPlayer.getPreferences();
		prefs.SetString(Prefs.LAST_SEEN_DYNAMIC_VIDEO, ExperimentWrapper.DynamicVideo.url);
		prefs.Save();
		CustomPlayerData.setValue(CustomPlayerData.LAST_SEEN_DYNAMIC_VIDEO, ExperimentWrapper.DynamicVideo.uniqueId);
		return VideoDialog.showDialog(
			ExperimentWrapper.DynamicVideo.url, 
			ExperimentWrapper.DynamicVideo.action, 
			ExperimentWrapper.DynamicVideo.buttonText, 
			ExperimentWrapper.DynamicVideo.statName, 
			ExperimentWrapper.DynamicVideo.closeButtonDelay,
			ExperimentWrapper.DynamicVideo.skipButtonDelay, 
			"hir_dynamic_video_motd",
			ExperimentWrapper.DynamicVideo.imagePath
		);
	}

	// Implements IResetGame
	public static void resetStaticClassData()
	{

	}
}
