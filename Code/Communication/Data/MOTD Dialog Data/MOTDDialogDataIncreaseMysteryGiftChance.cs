using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataIncreaseMysteryGiftChance : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return MysteryGift.isIncreasedMysteryGiftChance;
		}
	}
	
	public override string noShowReason
	{
		get
		{
			string result = base.noShowReason;
			if (!MysteryGift.isIncreasedMysteryGiftChance)
			{
				result += "Increased mystery gift chance event isn't active.\n";
			}
			return result;
		}
	}

	public override bool show()
	{
		return IncreaseMysteryGiftChanceMOTD.showDialog(keyName);
	}
	
	new public static void resetStaticClassData()
	{
	}
	
}
