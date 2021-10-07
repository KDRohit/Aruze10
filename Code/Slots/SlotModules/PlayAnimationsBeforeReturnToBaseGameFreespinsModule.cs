using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* executeOnReturnToBasegameFreespins is the inverse of executeOnContinueToBaseGameFreespins()
	 these will trigger right at the start of the transition from freespins back to base, before spin panel transitions and any big win starts

	 Author: Jake Smith
	 Date: 7/26/18
*/
public class PlayAnimationsBeforeReturnToBaseGameFreespinsModule : SlotModule
{
	[SerializeField] private AnimationListController.AnimationInformationList animationList;
    [SerializeField] private LabelWrapperComponent amountWonText;

    public override bool needsToExecuteOnReturnToBasegameFreespins()
	{
		return animationList != null && animationList.Count > 0;
	}

	public override IEnumerator executeOnReturnToBasegameFreespins()
	{
        if (amountWonText != null)
        {
            amountWonText.text = CreditsEconomy.convertCredits(BonusGameManager.instance.currentGameFinalPayout);
        }

        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(animationList));
	}
}