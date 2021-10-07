using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayAnimationsBeforeContinueToBaseGameFreespinsModule : SlotModule
{
	[SerializeField] private AnimationListController.AnimationInformationList animationList;
    [SerializeField] private LabelWrapperComponent freeSpinCountText;

    // executeOnContinueToBasegameFreespins() section
    // functions in this section are executed when SlotBaseGame.continueToBasegameFreespins() is called to start freespins in base
    // NOTE: These modules will trigger right at the start of the transition to freespins in base, before the spin panel is changed and the game is fully ready to start freespining
    public override bool needsToExecuteOnContinueToBasegameFreespins()
	{
		return animationList != null && animationList.Count > 0;
	}

	public override IEnumerator executeOnContinueToBasegameFreespins()
	{
        if (freeSpinCountText != null)
        {
            freeSpinCountText.text = CommonText.formatNumber(reelGame.numberOfFreespinsRemaining);
        }
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(animationList));
    }
}
