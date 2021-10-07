using UnityEngine;
using System.Collections;

/**
 * Module to handle revealing Advance outcomes during a picking round
 */
public class PickingGameAdvanceModule : PickingGameRevealModule 
{
	[SerializeField] protected string REVEAL_ANIMATION_NAME = "revealAdvance";
	[SerializeField] protected string REVEAL_GRAY_ANIMATION_NAME = "revealAdvanceGray";
	[SerializeField] protected string REVEAL_AUDIO = "pickem_pick_advance_selected";
	[SerializeField] protected float REVEAL_AUDIO_DELAY = 0.0f; // delay to ensure that the pick object is fully revealed
	[SerializeField] protected string REVEAL_LEFTOVER_AUDIO = "reveal_not_chosen";
	[SerializeField] protected string REVEAL_VO_AUDIO = "pickem_advance_vo_pick";
	[SerializeField] protected float REVEAL_VO_DELAY = 0.2f; // delay before playing the VO clip

	protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		// detect pick type & whether to handle with this module
		return pickData != null && pickData.canAdvance;
	}

	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		// play the associated reveal sound
		if (!string.IsNullOrEmpty(REVEAL_AUDIO))
		{
			Audio.playSoundMapOrSoundKeyWithDelay(REVEAL_AUDIO, REVEAL_AUDIO_DELAY);
		}


		if (!string.IsNullOrEmpty(REVEAL_VO_AUDIO))
		{
			// play the associated audio voiceover
			Audio.playSoundMapOrSoundKeyWithDelay(REVEAL_VO_AUDIO, REVEAL_VO_DELAY);
		}

		//set the credit value within the item and the reveal animation
		pickItem.REVEAL_ANIMATION = REVEAL_ANIMATION_NAME;
		yield return StartCoroutine(base.executeOnItemClick(pickItem));
	}

	public override IEnumerator executeOnRevealLeftover(PickingGameBasePickItem leftover)
	{
		// set the credit value from the leftovers
		ModularChallengeGameOutcomeEntry leftoverOutcome = pickingVariantParent.getCurrentLeftoverOutcome();
		
		if(leftoverOutcome != null)
		{
			leftover.REVEAL_ANIMATION_GRAY = REVEAL_GRAY_ANIMATION_NAME;
		}
			
		// play the associated leftover reveal sound
		Audio.play(Audio.soundMap(REVEAL_LEFTOVER_AUDIO));

		yield return StartCoroutine(base.executeOnRevealLeftover(leftover));
	}
}
