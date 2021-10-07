using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataSpinPanelV2 : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return true;
		}
	}

	public override string noShowReason
	{
		get
		{
			string result = base.noShowReason;

			return result;
		}
	}

	public override bool show()
	{
		return SpinPanelV2MOTD.showDialog();
	}

	new public static void resetStaticClassData()
	{
	}
}
