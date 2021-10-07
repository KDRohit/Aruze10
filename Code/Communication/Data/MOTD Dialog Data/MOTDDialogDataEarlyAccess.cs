using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataEarlyAccess : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return
				LobbyGame.vipEarlyAccessGame != null &&
				!LobbyGame.vipEarlyAccessGame.isRecentEarlyAccessGame &&
				SlotsPlayer.instance.vipNewLevel >= VIPLevel.earlyAccessMinLevel.levelNumber &&
				!ExperimentWrapper.VIPLobbyRevamp.isInExperiment;
		}
	}
	
	public override string noShowReason
	{
		get
		{
			string result = base.noShowReason;
			if (LobbyGame.vipEarlyAccessGame == null)
			{
				result += "No early access game specified.\n";
			}
			else if (LobbyGame.vipEarlyAccessGame.isRecentEarlyAccessGame)
			{
				result += LobbyGame.vipEarlyAccessGame.keyName + " is the recently seen early access game.\n";
			}
			if (SlotsPlayer.instance.vipNewLevel < VIPLevel.earlyAccessMinLevel.levelNumber)
			{
				result += "Not high enough level to get early access (" + VIPLevel.earlyAccessMinLevel.levelNumber + ").\n";
			}
			return result;
		}
	}
	
	public override bool show()
	{
		return EarlyAccessDialog.showDialog(keyName);
	}
	
	new public static void resetStaticClassData()
	{
	}

}
