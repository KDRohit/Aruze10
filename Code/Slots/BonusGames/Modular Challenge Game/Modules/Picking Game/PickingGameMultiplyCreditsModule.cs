using UnityEngine;
using System.Collections;

/*
If the picks in a round award multipliers,
then you can set-up mini-events to reveal the picks.
*/

// For example, this is gen26 round 2.
//	1) Reveal multiplier increase if there is one abd shoot sparkle trail to multiplier label
//	2) Show the credits un-multiplied
//	3) If multiplier > 1 shoot sparke trail at credits.
//	4) When trail arrives update credits to multiplied value.
//	5) Play special VO ("There thar's better")
//  6) Rollup credits to actual winnings total

public class PickingGameMultiplyCreditsModule : PickingGameRevealModule
{
	protected enum MultiplierMiniEvent
	{
		RevealPickemMultiplier,      // Reveal the multiplier on the pickem (eg +1X).
		IncreaseCurrentMultiplier,   // Increase the current multiplier (eg from 1X to 2X).
		RevealBaseCredits,           // Reveal the base credits on the pickem (eg 100).
		MultiplyPickemCredits,       // Multiply the credits on the pickem (eg 100 to 200).
		RollupTotalCredits,          // Rollup your total credits (eg 0 to 200).
		RevealMultiplierAndOrCredits // Reveal the multiplier and credits (eg +1 and 100) or just the credits (eg 100).
	}

	protected enum PickMultiplierSourceEnum
	{
		ChallengeGameCurrentMultiplier,		// Set the value using the challenge game's cumulative currentMultiplier value
		PickMultiplierValue					// Set the value using the current picks multiplier value
	}

	// What order should it play the mini-events?
	[SerializeField] protected MultiplierMiniEvent[] multiplierMiniEvents =
	{
		MultiplierMiniEvent.RevealPickemMultiplier,
		MultiplierMiniEvent.IncreaseCurrentMultiplier,
		MultiplierMiniEvent.RevealBaseCredits,
		MultiplierMiniEvent.MultiplyPickemCredits,
		MultiplierMiniEvent.RollupTotalCredits
	};
	
	[SerializeField] private string REVEAL_CREDITS_AUDIO = "pickem_credits_pick";
    [SerializeField] private float REVEAL_CREDITS_DELAY = 0.0f; // delay before playing the reveal clip
    [SerializeField] private string REVEAL_CREDITS_VO_AUDIO = "pickem_credits_vo_pick";
	[SerializeField] private float REVEAL_CREDITS_VO_DELAY = 0.2f; // delay before playing the VO clip
	[SerializeField] private float TIME_TO_DELAY_BETWEEN_REVEALS = 1.0f; // delay before playing the VO clip
	[SerializeField] private string REVEAL_MULTIPLIER_AUDIO = "pickem_multiplier_pick";
	[SerializeField] private string REVEAL_MULTIPLIER_VO_AUDIO = "pickem_multiplier_vo_pick";
	[SerializeField] private float REVEAL_MULTIPLIER_VO_DELAY = 0.2f; // delay before playing the VO clip
	[SerializeField] private string REVEAL_LEFTOVER_AUDIO = "reveal_not_chosen";

	// Reveal Pickem Multiplier
	
	[SerializeField] private string REVEAL_MULTIPLIER_ANIMATION_NAME = "";
	[SerializeField] private string REVEAL_GRAY_MULTIPLIER_ANIMATION_NAME = "";
	
	// Increase Current Multiplier
	[SerializeField] private PickMultiplierSourceEnum pickMultiplierValueSource = PickMultiplierSourceEnum.ChallengeGameCurrentMultiplier;
	[SerializeField] private bool useSourceForRollup = false;

	// If the artists build the new multiplier into the art and the increase multiplier animation, then assign it here.
	[SerializeField] private LabelWrapperComponent newMultiplierLabel;
	
	[SerializeField] private AnimationListController.AnimationInformationList increaseMultiplierAnimations; // Before the particle trails happen, trigger an animation
	[SerializeField] private AnimationListController.AnimationInformationList afterTrailArrivesAtMultiplierIncreaseAnimations; // After the particle trail to the multiplier happens, you can play animations before the particle trail is sent back
	[SerializeField] private float WAIT_TO_ADD_TO_CURRENT_MULTIPLIER_DUR = -1.0f; // -1.0 means wait until after animation and particle trail.
	private bool isWaitingToAddToCurrentMultiplier = false;
	
	// Reveal Base Credits
	
	[SerializeField] private string REVEAL_BASE_CREDITS_ANIMATION_NAME = "";
	[SerializeField] private float REVEAL_BASE_CREDITS_ANIM_OVERRIDE_DUR = -1.0f;
	[SerializeField] private string CHANGE_MULTIPLIER_TO_BASE_CREDITS_ANIMATION_NAME = "";
	[SerializeField] private string REVEAL_GRAY_CREDITS_ANIMATION_NAME = "";
	
	// Multiply Pickem Credits
	[SerializeField] private AnimationListController.AnimationInformationList creditMultiplyAnimations;
	[SerializeField] private bool usePickItemMultiplyCreditAnimations = false; // Use custom animation lists for each pick item on multiply.
	
	[SerializeField] private string MULTIPLY_CREDITS_ANIMATION_NAME = "";
	[SerializeField] private float REVEAL_MULTIPLY_CREDITS_ANIM_OVERRIDE_DUR = -1.0f;
	[SerializeField] private float WAIT_TO_CHANGE_BASE_CREDITS_TO_MULTIPLIED_CREDITS_DUR = -1.0f; // -1.0f means wait until after animation and particle trail
	[SerializeField] private bool isParticleTrailFromMultiplierGoingToPickCreditsLabel = false; // Makes the particle trail target the label instead of just the pick object, can be more accurate

	private bool isWaitingToChangeBaseCreditsToMultipliedCredits;

	// Rollup Total Credits
	
	[SerializeField] protected bool SHOULD_WAIT_FOR_ROLLUP_TOTAL_CREDITS = true;
	[SerializeField] protected bool SHOULD_WAIT_FOR_ROLLUP_TO_END = false;
	[SerializeField] protected bool particleToWinBox = false;
	[SerializeField] protected float ROLLUP_DELAY = 0f;

	[SerializeField] private bool USE_BAD_AND_GAMEOVER_PICKS = false; // Some games, like elvis01, mark picks using BAD group_code for some reason, so use this if you need to ignore those group_code's

	[SerializeField] protected string ADVANCE_MULTIPLIER_SOUND = "";
	[SerializeField] private float advanceMultiplierSoundDelay = 0.0f;
	// Item Click

	protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		if (pickData != null && (USE_BAD_AND_GAMEOVER_PICKS || (!pickData.isBad && !pickData.isGameOver)) &&
		    (pickData.credits > 0 || pickData.multiplier > 0))
		{
			return true;
		}
		else
		{
			return false;
		}
	}
	
	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		ModularChallengeGameOutcomeEntry outcomeEntry = pickingVariantParent.getCurrentPickOutcome();
			
		PickingGameCreditPickItem creditsPickItem =
			pickItem.gameObject.GetComponent<PickingGameCreditPickItem>();
			
		creditsPickItem.setCreditLabels(outcomeEntry.credits);
	
		PickingGameMultiplierPickItem multiplierPickItem =
			pickItem.gameObject.GetComponent<PickingGameMultiplierPickItem>();
		
		if (multiplierPickItem != null)
		{
			switch (pickMultiplierValueSource)
			{
				case PickMultiplierSourceEnum.ChallengeGameCurrentMultiplier:
					multiplierPickItem.setMultiplierLabel(roundVariantParent.gameParent.currentMultiplier);
					break;

				case PickMultiplierSourceEnum.PickMultiplierValue:
					multiplierPickItem.setMultiplierLabel(outcomeEntry.multiplier);
					break;
			}
			
		}
		
		foreach (MultiplierMiniEvent miniEvent in multiplierMiniEvents)
		{
			switch (miniEvent)
			{
				case MultiplierMiniEvent.RevealPickemMultiplier:
					if (shouldRevealPickemMultiplier(outcomeEntry))
					{
						yield return StartCoroutine(revealPickemMultiplier(pickItem, outcomeEntry));
					}
					break;
					
				case MultiplierMiniEvent.IncreaseCurrentMultiplier:
					if (shouldIncreaseCurrentMultiplier(outcomeEntry))
					{
						yield return StartCoroutine(increaseCurrentMultiplier(pickItem, outcomeEntry));
					}
					
					break;
				
				case MultiplierMiniEvent.RevealBaseCredits:
					if (shouldRevealBaseCredits(outcomeEntry))
					{
						yield return StartCoroutine(revealBaseCredits(pickItem, outcomeEntry));
					}
					
					break;
					
				case MultiplierMiniEvent.MultiplyPickemCredits:
					if (shouldMultiplyPickemCredits(outcomeEntry))
					{
						yield return StartCoroutine(multiplyPickemCredits(pickItem, outcomeEntry));
					}
					break;
					
				case MultiplierMiniEvent.RollupTotalCredits:
					if (shouldRollupTotalCredits(outcomeEntry))
					{
						if (shouldWaitForRollupTotalCredits())
						{
							yield return StartCoroutine(rollupTotalCredits(pickItem, outcomeEntry));
						}
						else
						{                          
							StartCoroutine(rollupTotalCredits(pickItem, outcomeEntry));
						}
					}
					break;

				case MultiplierMiniEvent.RevealMultiplierAndOrCredits:
					if (shouldRevealPickemMultiplier(outcomeEntry))
					{
						yield return StartCoroutine(revealPickemMultiplier(pickItem, outcomeEntry));
					}
					else if (shouldRevealBaseCredits(outcomeEntry))
					{
						yield return StartCoroutine(revealBaseCredits(pickItem, outcomeEntry));
					}
					
					break;
			}
		}
	}

	// Allowing this to be overriden if a game has a special case where most 
	// times the rollup isn't blocking, but for the last rollup it needs to block
	protected virtual bool shouldWaitForRollupTotalCredits()
	{
		return SHOULD_WAIT_FOR_ROLLUP_TOTAL_CREDITS;
	}
	
	// Reveal Pickem Multiplier
	
	protected virtual bool shouldRevealPickemMultiplier(ModularChallengeGameOutcomeEntry outcomeEntry)
	{
		if (outcomeEntry.multiplier > 0)
		{
			return true;
		}
		
		return false;
	}
	
	protected IEnumerator revealPickemMultiplier(PickingGameBasePickItem pickItem, ModularChallengeGameOutcomeEntry outcomeEntry)
	{
		Audio.play(Audio.soundMap(REVEAL_MULTIPLIER_AUDIO));
		if (!string.IsNullOrEmpty(REVEAL_MULTIPLIER_VO_AUDIO))
		{
			Audio.playWithDelay(Audio.soundMap(REVEAL_MULTIPLIER_VO_AUDIO), REVEAL_MULTIPLIER_VO_DELAY);
		}

		if (!string.IsNullOrEmpty(REVEAL_MULTIPLIER_ANIMATION_NAME))
		{
			pickItem.setRevealAnim(REVEAL_MULTIPLIER_ANIMATION_NAME);
			yield return StartCoroutine(pickItem.revealPick(outcomeEntry));
		}
	}
	
	// Increase Current Multiplier
	
	protected virtual bool shouldIncreaseCurrentMultiplier(ModularChallengeGameOutcomeEntry outcomeEntry)
	{
		if (outcomeEntry.multiplier > 0)
		{
			return true;
		}
		
		return false;
	}
	
	protected IEnumerator increaseCurrentMultiplier(PickingGameBasePickItem pickItem, ModularChallengeGameOutcomeEntry outcomeEntry)
	{
	    int multiplier = outcomeEntry.multiplier + getBonusMultiplier(outcomeEntry);
		
		if (newMultiplierLabel != null)
		{
			newMultiplierLabel.text = Localize.text("{0}X", roundVariantParent.gameParent.currentMultiplier + multiplier);
		}
		
		isWaitingToAddToCurrentMultiplier = true;
		StartCoroutine(waitToAddToCurrentMultiplier(multiplier));
		if (!string.IsNullOrEmpty(ADVANCE_MULTIPLIER_SOUND))
		{
			Audio.playSoundMapOrSoundKeyWithDelay(ADVANCE_MULTIPLIER_SOUND, advanceMultiplierSoundDelay);
		}

		if (increaseMultiplierAnimations != null && increaseMultiplierAnimations.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(increaseMultiplierAnimations));
		}

		ParticleTrailController particleTrailController =
			ParticleTrailController.getParticleTrailControllerForType(
				pickItem.gameObject,
				ParticleTrailController.ParticleTrailControllerType.Multiplier);
				
		if (particleTrailController != null && !particleToWinBox)
		{
			yield return StartCoroutine(
				particleTrailController.animateParticleTrail(
					roundVariantParent.multiplierLabel.gameObject.transform.position,
					roundVariantParent.gameObject.transform));
		}
        else if (particleTrailController != null && particleToWinBox)
        {
            yield return StartCoroutine(
                    particleTrailController.animateParticleTrail(
                        roundVariantParent.winLabel.gameObject.transform.position,
                        roundVariantParent.gameObject.transform));
        }

        if (afterTrailArrivesAtMultiplierIncreaseAnimations != null && afterTrailArrivesAtMultiplierIncreaseAnimations.Count > 0)
        {
        	yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(afterTrailArrivesAtMultiplierIncreaseAnimations));
        }

		isWaitingToAddToCurrentMultiplier = false;
		yield return new TIWaitForSeconds(TIME_TO_DELAY_BETWEEN_REVEALS);
	}

	protected virtual int getBonusMultiplier(ModularChallengeGameOutcomeEntry outcomeEntry)
	{
		return 0;
	}
	
	protected IEnumerator waitToAddToCurrentMultiplier(int multiplier)
	{
		if (WAIT_TO_ADD_TO_CURRENT_MULTIPLIER_DUR > 0.0f)
		{
			yield return new TIWaitForSeconds(WAIT_TO_ADD_TO_CURRENT_MULTIPLIER_DUR);
			isWaitingToAddToCurrentMultiplier = false;
		}
		
		while (isWaitingToAddToCurrentMultiplier)
		{
			yield return null;
		}
		
		roundVariantParent.addToCurrentMultiplier(multiplier);		
	}

	// Reveal Base Credits
	
	protected virtual bool shouldRevealBaseCredits(ModularChallengeGameOutcomeEntry outcomeEntry)
	{
		if (outcomeEntry.credits > 0)
		{
			return true;
		}
		
		return false;
	}
	
	protected IEnumerator revealBaseCredits(PickingGameBasePickItem pickItem, ModularChallengeGameOutcomeEntry outcomeEntry)
	{
		Audio.playWithDelay(Audio.soundMap(REVEAL_CREDITS_AUDIO), REVEAL_CREDITS_DELAY);
		
		if (!string.IsNullOrEmpty(REVEAL_CREDITS_VO_AUDIO))
		{
			Audio.playSoundMapOrSoundKeyWithDelay(REVEAL_CREDITS_VO_AUDIO, REVEAL_CREDITS_VO_DELAY);
		}

		if (outcomeEntry.multiplier > 0 && !string.IsNullOrEmpty(CHANGE_MULTIPLIER_TO_BASE_CREDITS_ANIMATION_NAME))
		{
			pickItem.setRevealAnim(CHANGE_MULTIPLIER_TO_BASE_CREDITS_ANIMATION_NAME);
		}
		else if (!string.IsNullOrEmpty(REVEAL_BASE_CREDITS_ANIMATION_NAME))
		{
			pickItem.setRevealAnim(REVEAL_BASE_CREDITS_ANIMATION_NAME, REVEAL_BASE_CREDITS_ANIM_OVERRIDE_DUR);
		}

		yield return StartCoroutine(base.executeOnItemClick(pickItem));
	}

	// Multiply Pickem Credits
	
	protected virtual bool shouldMultiplyPickemCredits(ModularChallengeGameOutcomeEntry outcomeEntry)
	{
		if (outcomeEntry.credits > 0)
		{
			return true;
		}
		
		return false;
	}
	
	protected IEnumerator multiplyPickemCredits(PickingGameBasePickItem pickItem, ModularChallengeGameOutcomeEntry outcomeEntry)
	{
		PickingGameCreditPickItem creditsPickItem =
			pickItem.gameObject.GetComponent<PickingGameCreditPickItem>();

		long multipliedCredits = getMultipliedCredits(outcomeEntry);
			
		if (roundVariantParent.gameParent.currentMultiplier > 1 ||
			(useSourceForRollup && pickMultiplierValueSource == PickMultiplierSourceEnum.PickMultiplierValue && outcomeEntry.multiplier > 0))
		{
			isWaitingToChangeBaseCreditsToMultipliedCredits = true;

			StartCoroutine(
				waitToChangeBaseCreditsToMultipliedCredits(
					creditsPickItem,
					multipliedCredits));
			
			yield return StartCoroutine(
				AnimationListController.playListOfAnimationInformation(
					creditMultiplyAnimations));

			// use custom per-pick animation lists when multiplying
			if (usePickItemMultiplyCreditAnimations)
			{
				PickingGameMultipliedCreditsPickItem pickItemCreditMultiply = 
					pickItem.gameObject.GetComponent<PickingGameMultipliedCreditsPickItem>();

				if (pickItemCreditMultiply != null)
				{
					yield return StartCoroutine(pickItemCreditMultiply.playMultipliedEffects(roundVariantParent.gameParent.currentMultiplier));
				}
			}

			if (roundVariantParent.multiplierLabel != null)
			{
				ParticleTrailController particleTrailController =
					ParticleTrailController.getParticleTrailControllerForType(
						roundVariantParent.multiplierLabel.gameObject,
						ParticleTrailController.ParticleTrailControllerType.Multiplier);

				if (particleTrailController != null)
				{
					if (isParticleTrailFromMultiplierGoingToPickCreditsLabel)
					{
						if (creditsPickItem != null)
						{
							GameObject labelObject = creditsPickItem.getLabelGameObject();

							if (labelObject != null)
							{
								yield return StartCoroutine(
									particleTrailController.animateParticleTrail(
										labelObject.transform.position, roundVariantParent.gameObject.transform));
							}
							else
							{
								Debug.LogError(
									"PickingGameMultiplyCreditsModule.multiplyPickemCredits() - Tried to send particle trail to credits label location, but labelObject was null!");
							}
						}
						else
						{
							Debug.LogError(
								"PickingGameMultiplyCreditsModule.multiplyPickemCredits() - Tried to send particle trail to credits label location, but PickingGameCreditPickItem wasn't found on pick object!");
						}
					}
					else
					{
						yield return StartCoroutine(
							particleTrailController.animateParticleTrail(
								pickItem.gameObject.transform.position, roundVariantParent.gameObject.transform));
					}

				}
			}

			if (!string.IsNullOrEmpty(MULTIPLY_CREDITS_ANIMATION_NAME))
			{
				pickItem.setRevealAnim(MULTIPLY_CREDITS_ANIMATION_NAME, REVEAL_MULTIPLY_CREDITS_ANIM_OVERRIDE_DUR);
				yield return StartCoroutine(pickItem.revealPick(outcomeEntry));
			}	
		}
        isWaitingToChangeBaseCreditsToMultipliedCredits = false;
	}
	
	protected IEnumerator waitToChangeBaseCreditsToMultipliedCredits(PickingGameCreditPickItem creditsPickItem, long multipliedCredits)
	{
		if (WAIT_TO_CHANGE_BASE_CREDITS_TO_MULTIPLIED_CREDITS_DUR > 0.0f)
		{
			yield return new TIWaitForSeconds(WAIT_TO_CHANGE_BASE_CREDITS_TO_MULTIPLIED_CREDITS_DUR);
			isWaitingToChangeBaseCreditsToMultipliedCredits = false;
		}
		
		while (isWaitingToChangeBaseCreditsToMultipliedCredits)
		{
			yield return null;
		}
		
		creditsPickItem.setCreditLabels(multipliedCredits);
	}

	// Rollup Total Credits
	
	protected virtual bool shouldRollupTotalCredits(ModularChallengeGameOutcomeEntry outcomeEntry)
	{
		if (outcomeEntry.credits > 0)
		{
			return true;
		}
		
		return false;
	}

	// Allowing this to be overriden if a game has a special case where most 
	// times the rollup isn't blocking, but for the last rollup it needs to block
	protected virtual bool shouldWaitForRollupToEnd()
	{
		return SHOULD_WAIT_FOR_ROLLUP_TO_END;
	}
	
	protected IEnumerator rollupTotalCredits(PickingGameBasePickItem pickItem, ModularChallengeGameOutcomeEntry outcomeEntry)
	{
		long multipliedCredits = getMultipliedCredits(outcomeEntry);

		yield return new TIWaitForSeconds(ROLLUP_DELAY);
		if (shouldWaitForRollupToEnd())
		{
			yield return StartCoroutine(rollupCredits(multipliedCredits));
		}
		else
		{
			StartCoroutine(rollupCredits(multipliedCredits));
		}
	}

	private long getMultipliedCredits(ModularChallengeGameOutcomeEntry outcomeEntry)
	{
		if (useSourceForRollup && pickMultiplierValueSource == PickMultiplierSourceEnum.PickMultiplierValue)
		{
			return outcomeEntry.multiplier * outcomeEntry.credits;
		}
		
		return roundVariantParent.gameParent.currentMultiplier * outcomeEntry.credits;
	}

	// Leftovers
	
	public override IEnumerator executeOnRevealLeftover(PickingGameBasePickItem leftover)
	{
		ModularChallengeGameOutcomeEntry leftoverOutcome = pickingVariantParent.getCurrentLeftoverOutcome();
		
		PickingGameCreditPickItem creditsLeftoverItem = leftover.gameObject.GetComponent<PickingGameCreditPickItem>();
		creditsLeftoverItem.setCreditLabels(leftoverOutcome.credits);

		if (leftoverOutcome.multiplier > 0)
		{
			leftover.REVEAL_ANIMATION_GRAY = REVEAL_GRAY_MULTIPLIER_ANIMATION_NAME;
		}
		else if (leftoverOutcome.credits > 0)
		{
			leftover.REVEAL_ANIMATION_GRAY = REVEAL_GRAY_CREDITS_ANIMATION_NAME;
		}
		
		Audio.play(Audio.soundMap(REVEAL_LEFTOVER_AUDIO));
		yield return StartCoroutine(base.executeOnRevealLeftover(leftover));
	}
}
