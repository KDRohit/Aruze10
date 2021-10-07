using UnityEngine;
using System.Collections;

public class DoSomethingTosAccept : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		PlayerAction.acceptTermsOfService();
		AnalyticsManager.Instance.LogTOSAccept();
	}

	public override bool getIsValidToSurface(string parameter)
	{
		return false;
	}
}
