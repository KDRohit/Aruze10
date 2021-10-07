using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataCreditSweepstakes : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return CreditSweepstakes.isActive;
		}
	}
	
	public override bool show()
	{
		return CreditSweepstakesMOTD.showDialog(keyName);
	}
	
	new public static void resetStaticClassData()
	{
	}

}
