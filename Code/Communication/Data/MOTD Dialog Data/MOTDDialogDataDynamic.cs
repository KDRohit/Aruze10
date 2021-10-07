using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataDynamic : MOTDDialogData
{
	public static bool hasShown = false;
	public static MOTDDialogData instance = null;
	
	public MOTDDialogDataDynamic() : base()
	{
		instance = this;
	}
	
	public override bool shouldShow
	{
		get
		{
			return
				ExperimentWrapper.SegmentedDynamicMOTD.isInExperiment &&
				!hasShown &&
				!string.IsNullOrEmpty(imageBackground) &&
				!isOnCooldown &&
				(maxViews == -1 || CustomPlayerData.getInt(CustomPlayerData.DYNAMIC_MOTD_VIEW_COUNT, 0) < maxViews);
		}
	}

	public bool isOnCooldown
	{
		get
		{
		    if (cooldown == 0)
			{
				return false;
			}
			else
			{
				int lastShown = CustomPlayerData.getInt(CustomPlayerData.DYNAMIC_MOTD_LAST_SHOW_TIME, 0);
			    int difference = (GameTimer.currentTime - lastShown);
				return difference < cooldown;
			}
		}
	}
	
	public override string noShowReason
	{
		get
		{
			string reason = base.noShowReason;
			if (hasShown)
			{
				reason += "Has already been shown.\n";
			}
			if (isOnCooldown)
			{
				reason += "Is on cooldown.\n";
			}
			if (maxViews != -1 && CustomPlayerData.getInt(CustomPlayerData.DYNAMIC_MOTD_VIEW_COUNT, 0) >= maxViews)
			{
				reason += "Already viewed max times: " + CustomPlayerData.getInt(CustomPlayerData.DYNAMIC_MOTD_VIEW_COUNT, 0)  + ".\n";
			}
			if (string.IsNullOrEmpty(imageBackground))
			{
				reason += "Empty background texture image.\n";
			}
			return reason;
		}
	}

	public override bool show()
	{
		if (instance == null || instance.keyName == null)
		{
			Debug.LogError("MOTDDialogDataDynamic.cs -- show -- trying to show the dynamic MOTD when it is not setup.");
			return false;
		}

		ExperimentWrapper.SegmentedDynamicMOTD.setDialogData(this);

		if (MOTDDialog.showDialog(this))
		{
			hasShown = true;
			int currentViewCount = CustomPlayerData.getInt(CustomPlayerData.DYNAMIC_MOTD_VIEW_COUNT, 0);
			currentViewCount++;
			CustomPlayerData.setValue(CustomPlayerData.DYNAMIC_MOTD_VIEW_COUNT, currentViewCount); // Increment the view count.
			CustomPlayerData.setValue(CustomPlayerData.DYNAMIC_MOTD_LAST_SHOW_TIME, GameTimer.currentTime); // Mark the last show time on the player blob.
			return true;
		}
		else
		{
			return false;
		}
	}
	
	new public static void resetStaticClassData()
	{
		hasShown = false;
		instance = null;
	}
	
}
