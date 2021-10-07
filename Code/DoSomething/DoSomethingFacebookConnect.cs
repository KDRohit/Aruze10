using UnityEngine;
using System.Collections;

public class DoSomethingFacebookConnect : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		StatsFacebookAuth.logCarouselCardConnect();
		AntisocialDialog.showDialog();
	}
	
	public override bool getIsValidToSurface(string parameter)
	{
		return !SlotsPlayer.isFacebookUser && !SlotsPlayer.IsFacebookConnected;
	}
}
