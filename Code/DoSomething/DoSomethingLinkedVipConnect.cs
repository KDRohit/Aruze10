using UnityEngine;
using System.Collections;

public class DoSomethingLinkedVipConnect : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		// Currently does exactly the same thing as the coins reward.
		DoSomethingLinkedVipConnectCoins.commonAction();
	}
	
	public override bool getIsValidToSurface(string parameter)
	{
		return (LinkedVipProgram.instance.shouldPromptForConnect);
	}
}
