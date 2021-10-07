using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataAntisocialPrompt : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return SlotsPlayer.isAnonymous;
		}
	}

	public override string noShowReason
	{
		get
		{
			string reason = base.noShowReason;
			if (!SlotsPlayer.isAnonymous)
			{
				reason += "Player is logged into facebook.\n";
			}
			return reason;
		}
	}

	public override bool show()
	{
		return AntisocialDialog.showDialog(keyName);
	}

	new public static void resetStaticClassData()
	{
	}
}

