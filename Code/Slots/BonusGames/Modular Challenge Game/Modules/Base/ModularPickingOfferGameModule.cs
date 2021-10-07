using UnityEngine;
using System.Collections;

public class ModularPickingOfferGameModule : PickingGameModule
{
	// execute when the offer is accepted in an offer challenge game
	public virtual bool needsToExecuteOnOfferAccepted()
	{
		return false;
	}

	public virtual IEnumerator executeOnOfferAccepted()
	{
		yield break;
	}

	// execute when the offer is declined in an offer challenge game
	public virtual bool needsToExecuteOnOfferDeclined()
	{
		return false;
	}

	public virtual IEnumerator executeOnOfferDeclined()
	{
		yield break;
	}
}
