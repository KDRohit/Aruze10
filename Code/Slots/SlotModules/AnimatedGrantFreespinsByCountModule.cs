using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Module that will play different effects for granting extra spins in freespins depending on how many spins
 * are actually being awarded.  This can be useful if the different amounts have unique presentations.
 *
 * Creation Date: 9/19/2019
 * Original Author: Scott Lepthien
*/
public class AnimatedGrantFreespinsByCountModule : GrantFreespinsModule 
{
	// Class to represent unique animated effects based on how many spins
	// are awarded.
	[System.Serializable]
	private class FreespinGrantEffectsForCount
	{
		[Tooltip("The freespin count being added which will use these effects")]
		[SerializeField] public int freespinCount = 1;
		[Tooltip("Delay before the animation list will be played so that it can be synced however you want with the increment and the particle effect")]
		[SerializeField] public float GRANT_FREESPINS_ANIMATION_LIST_DELAY = 0.0f;
		[Tooltip("Animations that will be played when granting spins")]
		[SerializeField] public AnimationListController.AnimationInformationList grantFreespinsAnimationList;
		[Tooltip("Used as a delay before the spin count is incremented, this allows the count increment to happen during the effects")]
		[SerializeField] public float INCREMENT_FREESPIN_COUNT_DELAY = 0.0f;
		[Tooltip("Labels for the value of the spin count awarded  (in case not all of the effects used baked in numbers)")]
		[SerializeField] public LabelWrapperComponent[] freespinCountAddedTextLabels;
		[Tooltip("If this is true then the RETRIGGER_BANNER_SOUND will play before the animation, instead of when the freespin count is incremented. Otherwise it will play when the count increments.")]
		[SerializeField] public bool isPlayingRetriggerBannerSoundBeforeAnims = true;
		[Tooltip("Delay before the particle effects will be played so that it can be synced however you want with the increment and the animation list")]
		[SerializeField] public float ANIMATED_SPARKLE_TRAIL_DELAY = 0.0f;
		[Tooltip("The particle effect that will be played when granting spins")]
		[SerializeField] public AnimatedParticleEffect animatedParticleEffect;
	}

	[Tooltip("The array of effects that are mapped to the different amount of spins that can be won")]
	[SerializeField] private FreespinGrantEffectsForCount[] freespinGrantEffectsByCount;
	[Tooltip("Turning this on will force reels to be anticipation reels when going to animate bonus symbols to ensure that they animate")]
	[SerializeField] private bool isForcingReelsToBeAnticipationReels = false;
	[Tooltip("Allows this module to play a bonus symbol sound when the bonus symbols animate")]
	[SerializeField] private bool isPlayingBonusSymbolSound = false;

	private FreespinGrantEffectsForCount currentCountEffects = null;
	
	// Overriding this so that we can control when the RETRIGGER_BANNER_SOUND is played
	protected override void incrementFreespinCount()
	{
		if (currentCountEffects != null && !currentCountEffects.isPlayingRetriggerBannerSoundBeforeAnims)
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

	// restores the freespin music track, in case one of the sounds played by the animated presenation aborts it
	private void restoreFreespinMusicTrack()
	{
		if (!Audio.isPlaying(Audio.soundMap("freespinintro")) && !Audio.isPlaying(Audio.soundMap("freespin")))
		{
			Audio.switchMusicKeyImmediate(Audio.soundMap("freespin"), 0.0f);
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
		// Try and grab the set of effects we need to use for the current number of spins being awarded
		currentCountEffects = getFreespinGrantEffectsForCount(numberOfFreeSpins);

		// First animate the bonus symbols on the reel, if we are forcing them to
		if (isAnimatingBonusSymbols)
		{
			yield return StartCoroutine(playAndWaitOnBonusSymbolAnimations());
		}

		if (currentCountEffects != null)
		{
			// Now start the increment of the freespin count with a delay, that way it can trigger during the animation list
			TICoroutine incrementFreespinCountCoroutine = StartCoroutine(incrementFreespinCountAfterDelay(currentCountEffects.INCREMENT_FREESPIN_COUNT_DELAY));

			// Set the amount labels if they are set before we start the animations
			for (int i = 0; i < currentCountEffects.freespinCountAddedTextLabels.Length; i++)
			{
				LabelWrapperComponent currentLabel = currentCountEffects.freespinCountAddedTextLabels[i];
				if (currentLabel != null)
				{
					currentLabel.text = CommonText.formatNumber(numberOfFreeSpins);
				}
			}

			// Play the banner sound here if it is setup to play before the animations
			if (currentCountEffects.isPlayingRetriggerBannerSoundBeforeAnims)
			{
				if (!string.IsNullOrEmpty(RETRIGGER_BANNER_SOUND))
				{
					Audio.tryToPlaySoundMap(RETRIGGER_BANNER_SOUND);
				}
			}
			
			List<TICoroutine> effectsCoroutines = new List<TICoroutine>();
			
			effectsCoroutines.Add(StartCoroutine(playAnimateParticleEffect()));
			effectsCoroutines.Add(StartCoroutine(playGrantFreespinsAnimationList()));

			yield return StartCoroutine(Common.waitForCoroutinesToEnd(effectsCoroutines));

			// Ensure if the freespin looped music track was aborted by a sound played during the animations that we restore it
			restoreFreespinMusicTrack();

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
		}
		else
		{
			// Just make sure the game still performs correctly, an error will be logged about the effects not being found
			incrementFreespinCount();
		}
	}

	private IEnumerator playAnimateParticleEffect()
	{
		if (currentCountEffects != null)
		{
			if (currentCountEffects.ANIMATED_SPARKLE_TRAIL_DELAY > 0.0f)
			{
				yield return new TIWaitForSeconds(currentCountEffects.ANIMATED_SPARKLE_TRAIL_DELAY);
			}
			
			if (currentCountEffects.animatedParticleEffect != null)
			{
				yield return StartCoroutine(currentCountEffects.animatedParticleEffect.animateParticleEffect());
			}
		}
	}

	private IEnumerator playGrantFreespinsAnimationList()
	{
		if (currentCountEffects != null)
		{
			if (currentCountEffects.GRANT_FREESPINS_ANIMATION_LIST_DELAY > 0.0f)
			{
				yield return new TIWaitForSeconds(currentCountEffects.GRANT_FREESPINS_ANIMATION_LIST_DELAY);
			}
			
			if (currentCountEffects.grantFreespinsAnimationList != null && currentCountEffects.grantFreespinsAnimationList.Count > 0)
			{
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(currentCountEffects.grantFreespinsAnimationList));
			}
		}
	}

	// Get the correct effects for the passed count
	private FreespinGrantEffectsForCount getFreespinGrantEffectsForCount(int countToGet)
	{
		for (int i = 0; i < freespinGrantEffectsByCount.Length; i++)
		{
			FreespinGrantEffectsForCount effectsToCheck = freespinGrantEffectsByCount[i];
			if (effectsToCheck.freespinCount == countToGet)
			{
				return effectsToCheck;
			}
		}

		Debug.LogError("AnimatedGrantFreespinsByCountModule.getFreespinGrantEffectsForCount() - Unable to find effects for countToGet = " + countToGet);
		return null;
	}
}
