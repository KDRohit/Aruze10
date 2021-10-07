using UnityEngine;
using System.Collections;

/**
 * Module to handle revealing "bad" picks during a picking round
 */
public class PickingGameBadPickModule : PickingGameRevealModule 
{
	[SerializeField] protected string REVEAL_ANIMATION_NAME = "revealBad";
	[SerializeField] protected string REVEAL_GRAY_ANIMATION_NAME = "revealBadGray";
	
	[SerializeField] protected string REVEAL_AUDIO = "pickem_reveal_bad";
	[SerializeField] protected float REVEAL_AUDIO_DELAY = 0.0f;
	
	[SerializeField] protected string REVEAL_AUDIO_VO = "pickem_reveal_bad_vo";
	[SerializeField] protected float REVEAL_VO_DELAY = 0.0f;
	
	[SerializeField] protected string REVEAL_LEFTOVER_AUDIO = "reveal_not_chosen";

	protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		// detect pick type & whether to handle with this module
		return pickData != null && (pickData.isGameOver || pickData.isBad) && !pickData.isCollectAll;
	}

	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		// play the associated reveal sound
		Audio.playWithDelay(Audio.soundMap(REVEAL_AUDIO), REVEAL_AUDIO_DELAY);
		Audio.playWithDelay(Audio.soundMap(REVEAL_AUDIO_VO), REVEAL_VO_DELAY);

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
