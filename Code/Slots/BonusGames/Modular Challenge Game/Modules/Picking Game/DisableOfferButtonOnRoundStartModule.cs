using UnityEngine;
using System.Collections;

//This class is used to disable a button in offer games. 
//Pawn01 uses it to disable the "no deal" button on the third round since you have to accept the third round offer.
public class DisableOfferButtonOnRoundStartModule : PickingGameModule
{	
	public UIButtonMessage buttonToDisable;

	//OnRoundStart
	public override bool needsToExecuteOnRoundStart()
	{
		return true;
	}

	public override IEnumerator executeOnRoundStart()
	{
		buttonToDisable.enabled = false;
		yield return null;
	}
}
