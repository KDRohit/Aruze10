using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataUnlockAllGames : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return UnlockAllGamesFeature.instance != null && UnlockAllGamesFeature.instance.isEnabled;
		}
	}
	
	public override bool show()
	{
		return UnlockAllGamesMotd.showDialog(keyName);
	}
	
	new public static void resetStaticClassData()
	{
	}
	
}
