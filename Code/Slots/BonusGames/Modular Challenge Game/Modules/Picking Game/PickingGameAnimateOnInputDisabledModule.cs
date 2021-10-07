using UnityEngine;
using System.Collections;

public class PickingGameAnimateOnInputDisabledModule : PickingGameModule
{
	// Play this animation on each disabled (unpicked) pickem.
	[SerializeField] protected string disablePickemAnimName = "";

	public override bool needsToExecuteOnInputDisabled()
	{
		return true;
	}

	public override IEnumerator executeOnInputDisabled()
	{
		if (!string.IsNullOrEmpty(disablePickemAnimName))
		{
			foreach (PickingGameBasePickItem pickMe in pickingVariantParent.pickmeItemList)
			{
				pickMe.pickAnimator.Play(disablePickemAnimName);
			}
		}
		
		yield break;
	}
}
