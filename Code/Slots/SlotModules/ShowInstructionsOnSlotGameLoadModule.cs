using UnityEngine;
using System.Collections;

/**
Shows instructional info when the game finishes loading, and then hides it after the player starts spinning (or possibly a set time if the object is animated)
*/
public class ShowInstructionsOnSlotGameLoadModule : HideGameObjectsOnPrespinModule 
{
	public override bool needsToExecuteAfterLoadingScreenHidden()
	{
		return objectsToHideOnPrespin.Count > 0;
	}

	public override IEnumerator executeAfterLoadingScreenHidden()
	{
		for (int i = 0; i < objectsToHideOnPrespin.Count; i++)
		{
			if (objectsToHideOnPrespin[i] != null)
			{
				objectsToHideOnPrespin[i].SetActive(true);
			}
		}

		yield break;
	}
}
