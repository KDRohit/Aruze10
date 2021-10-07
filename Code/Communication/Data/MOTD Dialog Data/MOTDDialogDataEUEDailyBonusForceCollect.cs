using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataEUEDailyBonusForceCollect : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return DailyBonusForcedCollection.instance.shouldForceCollect();
		}
	}

	public override string noShowReason
	{
		get
		{
			string reason = base.noShowReason;
			if (!DailyBonusForcedCollection.instance.shouldForceCollect())
			{
				reason += DailyBonusForcedCollection.instance.motdNoShowReason;
			}
			return reason;
		}
	}

	public override bool show()
	{
		return DailyBonusForceCollectionDialog.showDialog(keyName);
	}

	new public static void resetStaticClassData()
	{
	}
}

