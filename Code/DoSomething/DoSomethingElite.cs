using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoSomethingElite : DoSomethingAction
{
	public override bool getIsValidToSurface(string parameter)
	{
		return EliteManager.isActive;
	}

	public override void doAction(string parameter)
	{
		if (EliteManager.isActive)
		{
			EliteDialog.showDialog();
		}
	}
}
