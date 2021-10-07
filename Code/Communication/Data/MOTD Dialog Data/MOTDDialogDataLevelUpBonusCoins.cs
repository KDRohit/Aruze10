using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataLevelUpBonusCoins : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return LevelUpBonus.isBonusActive && !LevelUpBonus.isBonusActiveFromPowerup;
		}
	}
	
	public override string noShowReason
	{
		get
		{
			string result = base.noShowReason;
			if (!LevelUpBonus.isBonusActive)
			{
				result += "Level up bonus isn't active.\n";
			}
			return result;
		}
	}

	public override bool show()
	{
		return LevelUpBonusMotd.showDialog(keyName);
	}
	
	new public static void resetStaticClassData()
	{
	}
	
}
