using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/
public class MOTDDialogDataDailyChallenge : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return DailyChallenge.isActive;
		}
	}
	
	public override string noShowReason
	{
		get
		{
			string result = base.noShowReason;

			if (!DailyChallenge.isActive)
			{
				result += "Daily challenge feature is not active.\n";
			}

			return result;
		}
	}

	public override bool show()
	{
		return DailyChallengeMOTD.showDialog(keyName);
	}
	
	new public static void resetStaticClassData()
	{
	}
}
