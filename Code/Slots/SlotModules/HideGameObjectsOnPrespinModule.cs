using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Allows objects like instructive text to be hidden when a game starts spinning
For example in munsters01 there is an animation explaining the feature which should hide once the player starts spinning
*/
public class HideGameObjectsOnPrespinModule : SlotModule 
{
	[SerializeField] protected List<GameObject> objectsToHideOnPrespin = new List<GameObject>();

// executeOnPreSpin() section
// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) before the reels spin
	public override bool needsToExecuteOnPreSpin()
	{
		return objectsToHideOnPrespin.Count > 0;
	}

	public override IEnumerator executeOnPreSpin()
	{
		for (int i = 0; i < objectsToHideOnPrespin.Count; i++)
		{
			if (objectsToHideOnPrespin[i] != null)
			{
				objectsToHideOnPrespin[i].SetActive(false);
			}
		}

		yield break;
	}
}
