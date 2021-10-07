using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Module to play a collection of animator states concurrently on a target on round end
 * Special version for the ModularPickPortal which takes the revealed state into account and plays the correct list
 */
public class PickingGamePortalAnimateOnRoundEndModule : PickingGameModule
{
	[Header("Freespins")]
	[SerializeField] private AnimationListController.AnimationInformationList freespinsAnimationInformation;

	[Header("Picking")]
	[SerializeField] private AnimationListController.AnimationInformationList pickingAnimationInformation;

	[Header("Credits")]
	[SerializeField] private AnimationListController.AnimationInformationList creditsAnimationInformation;

	[Header("General")]
	[Tooltip("This is used as a fallback if a specific type isn't defined but the general animaiton are.")]
	[SerializeField] private AnimationListController.AnimationInformationList generalAnimationInformation;

	// Enable round end action
	public override bool needsToExecuteOnRoundEnd(bool isEndOfGame)
	{
		if (isEndOfGame)
		{
			return true;
		}
		else
		{
			return false;
		}
	}
	
	// Executes the defined animation on round end
	public override IEnumerator executeOnRoundEnd(bool isEndOfGame)
	{
		AnimationListController.AnimationInformationList animationListToPlay = null;

		ModularChallengeGameOutcomeEntry selected = pickingVariantParent.getLastPickOutcome();

		if (selected != null)
		{
			switch (selected.groupId)
			{
				case ModularPickPortal.FREESPINS_GROUP_ID:
					animationListToPlay = freespinsAnimationInformation;
					break;

				case ModularPickPortal.PICKING_GAME_GROUP_ID:
					animationListToPlay = pickingAnimationInformation;
					break;

				case ModularPickPortal.CREDITS_BONUS_GROUP_ID:
					animationListToPlay = creditsAnimationInformation;
					break;

				default:
					Debug.LogError("PickingGamePortalAnimateOnRoundEndModule.executeOnRoundEnd() - Unhandled groupId! selected.groupId = " + selected.groupId);
					break;
			}

			// if there isn't anything in the specific list try the generic list as a fallback
			if (animationListToPlay != null && animationListToPlay.Count == 0)
			{
				animationListToPlay = generalAnimationInformation;
			}
		}
		else
		{
			Debug.LogError("PickingGamePortalAnimateOnRoundEndModule.executeOnRoundEnd() - Couldn't get a valid selected item using getLastPickOutcome()!");
		}

		if (animationListToPlay != null && animationListToPlay.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(animationListToPlay));
		}
	}
}
