using UnityEngine;
using System.Collections;

/**
 * Module to handle revealing credits during a picking round
 */
public class PickingGameCreditModuleWithJackpotLadder : PickingGameCreditsModule
{
	[SerializeField] private JackpotLadderAnimationData jackpotRanks;

	protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		// detect pick type & whether to handle with this module
		// note: jackpot pickitems also have credits assigned, thus the groupId check
		if(
			(pickData != null) &&
			(pickData.credits > 0) &&
			!pickData.canAdvance &&
			(pickData.additonalPicks == 0) &&
			(pickData.extraRound == 0) &&
			(
				(!pickData.isGameOver) ||
				(
					pickData.isGameOver &&
					(
						roundVariantParent.roundIndex == roundVariantParent.gameParent.pickingRounds.Count-1 ||
						pickingVariantParent.gameParent.getDisplayedPicksRemaining() >= 0
					)
				)
			)
		)
		{
			return true;
		}
		else
		{
			return false;
		}
	}


    protected IEnumerator playParticleTrail(PickingGameBasePickItem pickItem)
    {
        ParticleTrailController particleTrailController = ParticleTrailController.getParticleTrailControllerForType(pickItem.gameObject, ParticleTrailController.ParticleTrailControllerType.Multiplier);
        // add all the anim info objects at or above the current rank to the subList
        if(particleTrailController != null)
        {
            yield return StartCoroutine(particleTrailController.animateParticleTrail(jackpotRanks.getCurrentRankLabel().gameObject.transform.position, roundVariantParent.gameObject.transform));
        }
        yield return null;
    }
        
    public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
    {
        ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentPickOutcome();

        // play the associated reveal sound
        Audio.playWithDelay(Audio.soundMap(REVEAL_AUDIO), REVEAL_AUDIO_DELAY);

        if (!string.IsNullOrEmpty(REVEAL_VO_AUDIO))
        {
            // play the associated audio voiceover
            Audio.playSoundMapOrSoundKeyWithDelay(REVEAL_VO_AUDIO, REVEAL_VO_DELAY);
        }

        //set the credit value within the item and the reveal animation
        pickItem.setRevealAnim(REVEAL_ANIMATION_NAME, REVEAL_ANIMATION_DURATION_OVERRIDE);

        PickingGameCreditPickItem creditsRevealItem = PickingGameCreditPickItem.getPickingGameCreditsPickItemForType(pickItem.gameObject, PickingGameCreditPickItem.CreditsPickItemType.Default);

        // adjust with bonus multiplier if necessary
        long credits = currentPick.credits;
        if (pickingVariantParent.gameParent.currentMultiplier > 0)
        {
            credits *= pickingVariantParent.gameParent.currentMultiplier;
        }
        creditsRevealItem.setCreditLabels(credits);
        yield return StartCoroutine(base.executeBasicOnRevealPick(pickItem));

        // play a particle trail to the jackpot
        yield return StartCoroutine(playParticleTrail(pickItem));

        // do the jackpot animation
        JackpotLabel label = jackpotRanks.getCurrentRankLabel();
        if(label != null)
        {
            label.isAvailable = false;
            yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(label.creditWinRevealAnimation));
        }

        // rollup with extra animations included
        yield return StartCoroutine(base.rollupCredits(credits));
    }
}
