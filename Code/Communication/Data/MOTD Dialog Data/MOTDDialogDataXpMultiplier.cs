using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataXpMultiplier : MOTDDialogData
{

	public override bool shouldShow
	{
		get
		{
			return XPMultiplierEvent.instance.isEnabled && !XPMultiplierEvent.instance.isEnabledByPowerup;
		}
	}

	public override string noShowReason
	{
		get
		{
			string reason = base.noShowReason;
			if (!XPMultiplierEvent.instance.isEnabled)
			{
				reason += XPMultiplierEvent.instance.disabledReason;
			}
			return reason;
		}
	}

	public override bool show()
	{
		return XPMultiplierDialog.showDialog();
	}

	new public static void resetStaticClassData()
	{
	}
	
}
