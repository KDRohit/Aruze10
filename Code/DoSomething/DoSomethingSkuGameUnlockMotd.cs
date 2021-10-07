using UnityEngine;
using System.Collections;

public class DoSomethingSkuGameUnlockMotd : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		LobbyGame skuGameUnlock = LobbyGame.skuGameUnlock;
		
		if (skuGameUnlock != null)
		{
			MOTDFramework.showMOTD(MOTDDialog.getSkuGameUnlockName());
		}
	}
	
	public override bool getIsValidToSurface(string parameter)
	{
		return (LobbyGame.skuGameUnlock != null);
	}
}
