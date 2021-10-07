using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataReducedDailyBonusTime : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return DailyBonusReducedTimeEvent.isActive;
		}
	}
	
	public override string noShowReason
	{
		get
		{
			string result = base.noShowReason;
			if (!DailyBonusReducedTimeEvent.isActive)
			{
				result += "Reduced daily bonus time event isn't active.\n";
			}
			return result;
		}
	}

	public override bool show()
	{
		return DailyBonusReducedTimeMOTD.showDialog(keyName);
	}
	
	new public static void resetStaticClassData()
	{
	}
	
}
