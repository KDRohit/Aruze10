using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//This class is for offer games that need to reveal offer items on click 
public class PickingGameOfferItemsRevealModule : PickingGameRevealModule
{
	[SerializeField] protected string REVEAL_ANIMATION_NAME = "";
	[SerializeField] protected float REVEAL_ANIMATION_DURATION_OVERRIDE = 0.0f;
	[SerializeField] protected AudioListController.AudioInformationList revealSounds;
	
	protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		if(pickData == null)
		{
			Debug.LogError("Pick data was null.  Check this outcome to make sure its in the correct format for an offer game.");
			return false;
		}
		//As of right now we only handle credits
		return (pickData.credits > 0);
	}

	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentPickOutcome();

		// play the associated reveal sound
		yield return StartCoroutine(AudioListController.playListOfAudioInformation(revealSounds));

		//set the credit value within the item and the reveal animation
		pickItem.setRevealAnim(REVEAL_ANIMATION_NAME, REVEAL_ANIMATION_DURATION_OVERRIDE);
		
		yield return StartCoroutine(base.executeOnItemClick(pickItem));

		// add credits
		BonusGamePresenter.instance.currentPayout += currentPick.credits;
	}
}
