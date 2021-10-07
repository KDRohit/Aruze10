using UnityEngine;
using System.Collections;

public class DoSomethingReactivateFriend : DoSomethingAction 
{
	public override void doAction(string parameter)
	{
		ReactivateFriendSenderOfferDialog.showDialog(ReactivateFriend.offerData);
	}

	public override bool getIsValidToSurface(string parameter)
	{
		return ReactivateFriend.isActive;
	}
}