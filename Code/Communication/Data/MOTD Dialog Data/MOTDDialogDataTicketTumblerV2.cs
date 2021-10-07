using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataTicketTumblerV2 : MOTDDialogData
{
	public int showGrant;   // tbd replace with login value once server code is working

	public override bool shouldShow
	{
		get
		{
			return TicketTumblerFeature.instance.isEnabled;
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
		if (TicketTumblerFeature.instance.isEnabled)
		{
			return TicketTumblerMOTD.showDialog();
		}
		return false;
	}

	new public static void resetStaticClassData()
	{
	}
}
