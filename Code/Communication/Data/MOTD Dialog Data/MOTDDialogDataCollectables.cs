using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataCollectables : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			// Is in experiement and within the time limit and shit
			// Might need to also check to see if we're eligible for free cards
			return Collectables.isActive();
		}
	}

	public override bool show()
	{
		return CollectablesMOTD.showDialog("pack");
	}

	public override string noShowReason
	{
		get 
		{
			string reason = base.noShowReason;
			if (!Collectables.isActive())
			{
				System.DateTime endTime = Common.convertFromUnixTimestampSeconds(Collectables.endTimeInt);
				bool validTimeRange = endTime > System.DateTime.UtcNow;

				if (!validTimeRange)
				{
					reason += "End time is in the past. Feature is disabled.\n";
				}

				if (Collectables.currentAlbum.IsNullOrWhiteSpace())
				{
					reason += "Collectables didn't receive a current album in Login data and wasn't initialized.\n";
				}
			}
			return reason;
		}
	}

	new public static void resetStaticClassData()
	{
	}
}
