using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Lets you enabled a list of animators while the reels are spinning, and disable them when the reels finish spinning.

Original Author: Chad McKinney
*/
public class ToggleOnSpinningModule : SlotModule 
{
	[SerializeField] protected List<GameObject> gameObjects;

	protected override void OnEnable()
	{
		EnableGameObjects(false);
    }

// executeOnReelsSpinning() section
// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) immediately after the reels start spinning
	public override bool needsToExecuteOnReelsSpinning()
	{
		return true;
	}

	public override IEnumerator executeOnReelsSpinning()
    {
		EnableGameObjects (true);
        yield break;
	}

// executeOnReelsStoppedCallback() section
// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return true;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
    {
		EnableGameObjects (false);
		yield break;
	}

	protected void EnableGameObjects(bool enable)
	{
		foreach (GameObject go in gameObjects)
		{
			go.SetActive(enable);
		}
	}
}
