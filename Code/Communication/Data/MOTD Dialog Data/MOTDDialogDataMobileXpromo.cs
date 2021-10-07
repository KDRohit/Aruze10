using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataMobileXpromo : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return MobileXpromo.shouldShow(MobileXpromo.SurfacingPoint.RTL);
		}
	}

	public override string noShowReason
	{
		get
		{
			string reason = base.noShowReason;
			if (!MobileXpromo.shouldShow(MobileXpromo.SurfacingPoint.RTL))
			{
				reason += "Xpromo not valid for RTL at this time.";
			}
			return reason;
		}
	}

	public override bool show()
	{
		return MobileXpromo.showXpromo(MobileXpromo.SurfacingPoint.RTL);
	}

	new public static void resetStaticClassData()
	{
	}
}

