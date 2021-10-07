using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataBuyPagePerk : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return BuyPagePerk.isActive;
		}
	}

	public override bool show()
	{
		return BuyPagePerkMOTD.showDialog(false, keyName);
	}

	new public static void resetStaticClassData()
	{
	}
}
