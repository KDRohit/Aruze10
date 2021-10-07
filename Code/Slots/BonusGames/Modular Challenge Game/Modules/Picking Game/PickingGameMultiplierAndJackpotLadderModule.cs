using UnityEngine;
using System.Collections;

/**
 * Module to handle revealing multipliers during a picking round
 */

public class PickingGameMultiplierAndJackpotLadderModule : PickingGameMultiplierModule 
{
	[SerializeField] private JackpotLadderAnimationData jackpotRanks;
	[SerializeField] protected float timeBetweenJackpotMultiplierAnimations = 0.25f;

	protected long calculateCredits(int rankIndex)
	{
		return 	jackpotRanks.getRankLabel(rankIndex).initialCredits * 
				GameState.baseWagerMultiplier *
				GameState.bonusGameMultiplierForLockedWagers *
				roundVariantParent.gameParent.currentMultiplier;
	}

    protected IEnumerator playMultiplierRevealAnimations(PickingGameBasePickItem pickItem)
	{
		int curRankIndex = jackpotRanks.getCurrentRankIndex();
        ParticleTrailController particleTrailController = ParticleTrailController.getParticleTrailControllerForType(pickItem.gameObject, ParticleTrailController.ParticleTrailControllerType.Multiplier);
        // add all the anim info objects at or above the current rank to the subList
		for (int i = curRankIndex; i < jackpotRanks.getNumRanks(); i++)
		{
            if(particleTrailController != null)
            {
                yield return StartCoroutine(particleTrailController.animateParticleTrail(jackpotRanks.getRankLabel(i).gameObject.transform.position, roundVariantParent.gameObject.transform));
            }
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotRanks.getRankLabel(i).multiplierRevealAnimation));
			jackpotRanks.updateJackpotLabelCredits(i, calculateCredits(i));
			yield return new TIWaitForSeconds(timeBetweenJackpotMultiplierAnimations);
		}
		yield return null;
	}

	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentPickOutcome();

		// play the associated reveal sound
		Audio.playSoundMapOrSoundKey(REVEAL_AUDIO);

		if (!string.IsNullOrEmpty(REVEAL_VO_AUDIO))
		{
			// play the associated audio voiceover
			Audio.playSoundMapOrSoundKeyWithDelay(REVEAL_VO_AUDIO, REVEAL_VO_DELAY);
		}

		//set the multiplier value within the item and the reveal animation
		PickingGameMultiplierPickItem multiplierPick = pickItem.gameObject.GetComponent<PickingGameMultiplierPickItem>();
		// Some multiplier picks are using art which uses static art for the multiplier number instead of a modifiable number, so only set labels if it has the item attached
		if (multiplierPick != null)
		{
			multiplierPick.setMultiplierLabel(currentPick.multiplier);
		}

		pickItem.setRevealAnim(REVEAL_ANIMATION_NAME, REVEAL_ANIMATION_DURATION_OVERRIDE);
		yield return StartCoroutine(executeBasicOnRevealPick(pickItem));

		// update the actual round multiplier
		roundVariantParent.addToCurrentMultiplier(currentPick.multiplier);

		// play an animation flourish if we have one
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(multiplierAmbientAnimations));

		// play any jackpot animations
        yield return StartCoroutine(playMultiplierRevealAnimations(pickItem));
	}
}


