using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataJackpotUnlockGame : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return
				ProgressiveJackpot.doesGameUnlockProgressiveExist &&
				SelectGameUnlockDialog.gamesToDisplay != null &&
				SelectGameUnlockDialog.gamesToDisplay.Count > 0;
		}
	}
	
	public override bool show()
	{
		return JackpotUnlockGameMotd.showDialog(keyName);
	}
	
	new public static void resetStaticClassData()
	{
	}
	
}
