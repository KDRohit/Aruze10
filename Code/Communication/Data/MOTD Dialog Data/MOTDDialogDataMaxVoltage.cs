using UnityEngine;
using System.Collections;

public class MOTDDialogDataMaxVoltage : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return  SlotsPlayer.instance.socialMember.experienceLevel >= Glb.MAX_VOLTAGE_MIN_LEVEL;
		}
	}

	public override string noShowReason
	{
		get
		{
			string reason = base.noShowReason;
			if (SlotsPlayer.instance.socialMember.experienceLevel < Glb.MAX_VOLTAGE_MIN_LEVEL)
			{
				reason += "User is too low level. Min level is: " + Glb.MAX_VOLTAGE_MIN_LEVEL.ToString();
			}
			return reason;
		}
	}

	public override bool show()
	{
		return MaxVoltageDialog.showDialog(keyName);
	}

	new public static void resetStaticClassData()
	{
	}
}
