using UnityEngine;
using System.Collections;

/**
 * Module to handle revealing bonus game pick during a picking round
 */
public class PickingGameBonusGameRevealModule : PickingGameRevealModule 
{ 
	[SerializeField] protected string REVEAL_ANIMATION_NAME = ""; 
	[SerializeField] protected float REVEAL_ANIMATION_DURATION_OVERRIDE = 0.0f; 
	[SerializeField] private AudioListController.AudioInformationList revealAudioInfo;

    protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		if (pickData != null && pickData.nestedBonusOutcome != null)
		{
			return true;
		}

		return false;
	}

	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		// play the associated reveal sound
		yield return StartCoroutine(AudioListController.playListOfAudioInformation(revealAudioInfo));

		//set the credit value within the item and the reveal animation
		pickItem.setRevealAnim(REVEAL_ANIMATION_NAME, REVEAL_ANIMATION_DURATION_OVERRIDE);
		
		yield return StartCoroutine(base.executeOnItemClick(pickItem));
	}
}
