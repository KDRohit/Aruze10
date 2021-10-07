using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataIncreaseBigSliceChance : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return MysteryGift.isIncreasedBigSliceChance;
		}
	}
	
	public override string noShowReason
	{
		get
		{
			string result = base.noShowReason;
			if (!MysteryGift.isIncreasedBigSliceChance)
			{
				result += "Increased big slice chance event isn't active.\n";
			}
			return result;
		}
	}

	public override bool show()
	{
		return IncreaseBigSliceChanceMOTD.showDialog(keyName);
	}
	
	new public static void resetStaticClassData()
	{
	}
	
}
