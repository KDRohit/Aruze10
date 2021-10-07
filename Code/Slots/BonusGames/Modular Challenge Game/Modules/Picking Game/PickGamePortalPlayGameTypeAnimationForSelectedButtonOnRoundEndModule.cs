using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PickGamePortalPlayGameTypeAnimationForSelectedButtonOnRoundEndModule : PickingGameModule
{
	/*
		Plays a list on animations based on which button was selected and what the reveal was.
		(eg. Specific transitions that depend on which button was selected)
	*/
	[SerializeField] private List<PickItemAnimationInformation> pickItemAnimationBehaviorList = new List<PickItemAnimationInformation>();
	private int pickedItemIndex = -1;

	// Enable round end action
	public override bool needsToExecuteOnRoundEnd(bool isEndOfGame)
	{
		if (isEndOfGame && pickedItemIndex != -1)
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

				animationListToPlay = pickItemAnimationBehaviorList[pickedItemIndex].freespinsAnimationInformation;
				break;

			case ModularPickPortal.PICKING_GAME_GROUP_ID:
				animationListToPlay = pickItemAnimationBehaviorList[pickedItemIndex].pickingAnimationInformation;
				break;

			case ModularPickPortal.CREDITS_BONUS_GROUP_ID:
				animationListToPlay = pickItemAnimationBehaviorList[pickedItemIndex].creditsAnimationInformation;
				break;

			default:
				Debug.LogError("PickingGamePortalAnimateOnRoundEndModule.executeOnRoundEnd() - Unhandled groupId! selected.groupId = " + selected.groupId);
				break;
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

	public override bool needsToExecuteOnItemClick (ModularChallengeGameOutcomeEntry pickData)
	{
		return true;
	}

	public override IEnumerator executeOnItemClick (PickingGameBasePickItem pickItem)
	{
		for (int i = 0; i < pickItemAnimationBehaviorList.Count; i++)
		{
			if (pickItem == pickItemAnimationBehaviorList[i].pickItem)
			{
				pickedItemIndex = i;
				break;
			}
		}
		return base.executeOnItemClick (pickItem);
	}

	[System.Serializable]
	private class PickItemAnimationInformation
	{
		//The item that we will want to play this list of animations for.
		[Header("Pick Item")]
		[SerializeField] public PickingGameBasePickItem pickItem;

		//Play these animations if this selected item revealed freespins.
		[Header("Freespins")]
		[SerializeField] public AnimationListController.AnimationInformationList freespinsAnimationInformation;

		//Play these animations if this selected item revealed a picking/challenge game.
		[Header("Picking")]
		[SerializeField] public AnimationListController.AnimationInformationList pickingAnimationInformation;

		//Play these animations if this selected item revealed credits. 
		[Header("Credits")]
		[SerializeField] public AnimationListController.AnimationInformationList creditsAnimationInformation;
	}
}
