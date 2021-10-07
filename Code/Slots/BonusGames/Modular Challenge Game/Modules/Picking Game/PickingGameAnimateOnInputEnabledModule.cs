using UnityEngine;
using System.Collections;

public class PickingGameAnimateOnInputEnabledModule : PickingGameModule
{
	// Play these animation on each enabled (unpicked) pickem.
	[SerializeField] protected bool shouldPlayPickemAnimsOnFirstEnable = false;
	[SerializeField] protected string enablePickemAnimName = ""; // Play this the rest of the times picks are enabled.
	
	private bool isFirstEnable = true;
		
	// Execute On Input Enable
	
	public override bool needsToExecuteOnInputEnabled()
	{
		return true;
	}

	public override IEnumerator executeOnInputEnabled()
	{
		if (!isFirstEnable || shouldPlayPickemAnimsOnFirstEnable)
		{
			if (!string.IsNullOrEmpty(enablePickemAnimName))
			{
				foreach (PickingGameBasePickItem pickMe in pickingVariantParent.pickmeItemList)
				{
					pickMe.pickAnimator.Play(enablePickemAnimName);
				}
			}
		}
		
		isFirstEnable = false;
		yield break;
	}
}
