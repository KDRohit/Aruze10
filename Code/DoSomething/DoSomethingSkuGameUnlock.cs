using UnityEngine;
using System.Collections;

public class DoSomethingSkuGameUnlock : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		LobbyGame skuGameUnlock = LobbyGame.skuGameUnlock;
		
		if (skuGameUnlock != null)
		{
			Application.OpenURL(skuGameUnlock.xp.xpromoUrl);

#if UNITY_EDITOR
	#if (false)
			// Test what happens if you installed the app.
			PlayerAction.appInstalled(skuGameUnlock.xp.xpromoTarget);
	#endif
#endif
		}
	}

	public override bool getIsValidToSurface(string parameter)
	{
		return false;
	}
}
