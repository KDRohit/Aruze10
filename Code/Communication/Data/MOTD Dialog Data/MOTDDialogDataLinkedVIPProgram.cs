using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.HitItRich.EUE;

/*
Override for special behavior.
*/

public class MOTDDialogDataLinkedVIPProgram : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return LinkedVipProgram.instance.isEligible &&
			       !ExperimentWrapper.NetworkProfile.activeDiscoveryEnabled;
		}
	}
	
	public override string noShowReason
	{
		get
		{
			string result = base.noShowReason;
			if (!LinkedVipProgram.instance.isEligible)
			{
				result += "Ineligible for Linked VIP program.\n";
			}
			return result;
		}
	}

	public override bool show()
	{
		return LinkedVipProgramDialog.showDialog(keyName);
	}
	
	new public static void resetStaticClassData()
	{
	}
	
}
