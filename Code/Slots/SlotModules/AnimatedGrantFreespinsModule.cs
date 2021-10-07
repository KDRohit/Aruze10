using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This module can be used on FreeSpin games which grant freespins on certain outcomes and need to display an animation.
public class AnimatedGrantFreespinsModule : GrantFreespinsModule 
{
	[SerializeField] private AnimationListController.AnimationInformationList grantFreespinsAnimationList;
	[Tooltip("Used as a delay before the spin count is incremented, this allows the count increment to happen during the animation list.")]
	[SerializeField] private float INCREMENT_FREESPIN_COUNT_DELAY = 0.0f;
	[Tooltip("Labels for the value of the spin count awarded (not all games use this).")]
	[SerializeField] private LabelWrapperComponent amountTextLabel;
	[Tooltip("Labels for the value of the spin count awarded (not all games use this).")]
	[SerializeField] private LabelWrapperComponent amountTextLabelShadow;
	[Tooltip("Labels for the value of the spin count awarded when there are burst animations (not all games use this).")]
	[SerializeField] private LabelWrapperComponent burstAmountTextLabel;
	[Tooltip("If this is true then the RETRIGGER_BANNER_SOUND will play before the animation, instead of when the freespin count is incremented. Otherwise it will play when the count increments.")]
	[SerializeField] private bool isPlayingRetriggerBannerSoundBeforeAnims = true;
	[SerializeField] private AnimatedParticleEffect animatedSparkleTrail;
	[SerializeField] private Transform sparkleStartPosition;
	[SerializeField] private Transform meterBurstTransform;
	[SerializeField] private AnimationListController.AnimationInformationList meterBurstAnimations;
	[SerializeField] private bool isForcingReelsToBeAnticipationReels = false; // Turning this on will force reels to be anticipation reels when going to animate bonus symbols to ensure that they animate
	[SerializeField] private bool isPlayingBonusSymbolSound = false; // Adding this in because this module didn't use to play this sound, so to make sure I don't break any games I'm leaving it off by default
	[Tooltip("Add a delay after all the effects have played and the value has been updated.  Can be useful if you want the player to see the value before another spin starts.")]
	[SerializeField] private float waitTimeAfterAllEffects = 0.0f;

	// Overriding this so that we can control when the RETRIGGER_BANNER_SOUND is played
	protected override void incrementFreespinCount()
	{
		if (!isPlayingRetriggerBannerSoundBeforeAnims)
		{
			// if we aren't playing the retrigger sound before the animations then go ahead and play it here
			if (!string.IsNullOrEmpty(RETRIGGER_BANNER_SOUND))
			{
				Audio.tryToPlaySoundMap(RETRIGGER_BANNER_SOUND);
			}
		}
		
		Audio.play(Audio.soundMap(SPINS_ADDED_INCREMENT_SOUND_KEY));
		reelGame.numberOfFreespinsRemaining += numberOfFreeSpins;
	}

	// create a delay before the count is incremented, this will allow the count to increment during the animation list
	// if you want the count to increment at the end of all the animations, then this delay should be the total of all
	// of the animation times that will be played
	private IEnumerator incrementFreespinCountAfterDelay(float delay)
	{
		yield return new TIWaitForSeconds(delay);
		incrementFreespinCount();
	}

	// restores the looped music track, in case one of the sounds played by the animated presenation aborts it
	private void restoreOriginalMusicTrack(string originalMusicKey)
	{
		if (Audio.defaultMusicKey != originalMusicKey)
		{
			if (Audio.soundMap("freespinintro") == originalMusicKey)
			{
				// Special case to handle an edge case where freespinintro was playing but gets swapped out
				// for the freespin tune while it is doing this grant.  In that case we will just switch to
				// freespin instead of restoring the intro tune.
				Audio.switchMusicKeyImmediate(Audio.soundMap("freespin"), 0.0f);
			}
			else
			{
				// Just restore what was playing before
				Audio.switchMusicKeyImmediate(originalMusicKey, 0.0f);
			}
		}
	}

	protected override IEnumerator playAndWaitOnBonusSymbolAnimations()
	{
		numberOfBonusSymbolsAnimating = 0;

		// animate bonus symbols on reels for freespin awards
		SlotReel[] allReels = this.reelGame.engine.getAllSlotReels();
		for (int i = 0; i < allReels.Length; i++)
		{
			if (isForcingReelsToBeAnticipationReels)
			{
				// force reels to be anticipation reels since we aren't handling anticipations normally
				allReels[i].setAnticipationReel(true);
			}

			numberOfBonusSymbolsAnimating += allReels[i].animateBonusSymbols(onBonusSymbolAnimDone);
		}

		if (isPlayingBonusSymbolSound)
		{
			if (numberOfBonusSymbolsAnimating > 0 && !string.IsNullOrEmpty(BONUS_SYMBOL_SOUND))
			{
				Audio.playSoundMapOrSoundKey(BONUS_SYMBOL_SOUND);
			}
		}

		while (numberOfBonusSymbolsAnimating > 0)
		{
			yield return null;
		}
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		// Save out the current music track so we can restore it in case
		// it gets changed for this grant presentation.
		string originalMusicKey = Audio.defaultMusicKey;
		
		// First animate the bonus symbols on the reel, if we are forcing them to
		if (isAnimatingBonusSymbols)
		{
			yield return StartCoroutine(playAndWaitOnBonusSymbolAnimations());
		}

		// Now start the increment of the freespin count with a delay, that way it can trigger during the animation list
		TICoroutine incrementFreespinCountCoroutine = StartCoroutine(incrementFreespinCountAfterDelay(INCREMENT_FREESPIN_COUNT_DELAY));

		// Set the amount labels if they are set before we start the animations
		if (amountTextLabel != null)
		{
			amountTextLabel.text = CommonText.formatNumber(numberOfFreeSpins);
		}

		if (amountTextLabelShadow != null)
		{
			amountTextLabelShadow.text = CommonText.formatNumber(numberOfFreeSpins);
		}

		if (burstAmountTextLabel != null)
		{
			burstAmountTextLabel.text = CommonText.formatNumber(numberOfFreeSpins);
		}

		// Play the banner sound here if it is setup to play before the animations
		if (isPlayingRetriggerBannerSoundBeforeAnims)
		{
			if (!string.IsNullOrEmpty(RETRIGGER_BANNER_SOUND))
			{
				Audio.tryToPlaySoundMap(RETRIGGER_BANNER_SOUND);
			}
		}

		if (animatedSparkleTrail != null)
		{
			yield return StartCoroutine(animatedSparkleTrail.animateParticleEffect());
		}

		// play a particle trail if one exists for adding picks
		ParticleTrailController particleTrailController = ParticleTrailController.getParticleTrailControllerForType(gameObject, ParticleTrailController.ParticleTrailControllerType.IncreasePicks);
		if (particleTrailController != null)
		{
			StartCoroutine(particleTrailController.animateParticleTrail(BonusSpinPanel.instance.spinCountLabel.transform.position, sparkleStartPosition));
		}

        // Position the meter burst over the spin panel and play its animation.
        // Note the prefab should be on the NGUI_OVERLAY layer.
        if (meterBurstTransform != null && meterBurstAnimations != null && meterBurstAnimations.Count > 0)
        {
            meterBurstTransform.position = BonusSpinPanel.instance.spinCountLabel.transform.position;
            StartCoroutine(AnimationListController.playListOfAnimationInformation(meterBurstAnimations));
        }

		// Now play the animation list, which should include any sounds needed to be played in it
		if (grantFreespinsAnimationList != null && grantFreespinsAnimationList.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(grantFreespinsAnimationList));
		}

		// Ensure if the looped music track was aborted by a sound played during the animations that we restore it
		restoreOriginalMusicTrack(originalMusicKey);

		if (bonusCreditAmount > 0)
		{
			// bonus credits were also won as part of this freespin award, so we need to roll those up
			bonusCreditAmount *= reelGame.multiplier;
			yield return StartCoroutine(SlotUtils.rollup(0, bonusCreditAmount, reelGame.onPayoutRollup, isPlayingRollupSoundsForBonusCredits));
			// trigger the end rollup to move the winnings into the runningPayoutRollupValue
			yield return StartCoroutine(reelGame.onEndRollup(isAllowingContinueWhenReady: false));
			bonusCreditAmount = -1;
		}

		// make sure that the increment coroutine has finished before we stop blocking
		if (incrementFreespinCountCoroutine != null)
		{
			while (!incrementFreespinCountCoroutine.isFinished)
			{
				yield return null;
			}
		}

		if (waitTimeAfterAllEffects > 0.0f)
		{
			yield return new TIWaitForSeconds(waitTimeAfterAllEffects);
		}
	}
}
