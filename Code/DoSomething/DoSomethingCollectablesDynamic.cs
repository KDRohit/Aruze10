using UnityEngine;
using System.Collections;

public class DoSomethingCollectablesDynamic : DoSomethingAction
{
	public override bool getIsValidToSurface(string parameter)
	{
		// Only show the Collections carousel if collections is active
		return Collectables.isActive();
	}

	public override void doAction(string parameter)
	{
		if (MOTDDialogDataDynamic.instance != null)
		{
			MOTDDialogDataDynamic.instance.show();
		}
	}
}
