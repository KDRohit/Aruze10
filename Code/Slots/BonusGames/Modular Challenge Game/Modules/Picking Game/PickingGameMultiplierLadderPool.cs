using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/* Picking game with a multiplier ladder that increases from a pool
 * Supports increasing multiplier on the ladder, and increasing all values on the ladder
 * Originally for: oz04 (based on gwtw)
 */
public class PickingGameMultiplierLadderPool : PickingGameRevealModule
{
	// tracking for place on ladder / ladder tier values
	private int currentHorizontalIndex = 0;
	private int currentVerticalIndex = 0;

	private int currentMultiplier
	{
		get 
		{
			return pickingVariantParent.outcome.getMultiplierForPickPool(poolKey, currentHorizontalIndex, currentVerticalIndex);
		}
	}

	[SerializeField] private string poolKey; // target pool for this module

	[SerializeField] private string REVEAL_INCREASE_ANIM = "";  // reveal for increasing all ladder values (horizontal)
	[SerializeField] private string REVEAL_INCREASE_AUDIO = "pickem_special2_pick";
	[SerializeField] private string REVEAL_INCREASE_VO = "pickem_special2_vo_pick";

	[SerializeField] private string REVEAL_ADVANCE_ANIM = "";	// reveal for advancing up the ladder (vertical)
	[SerializeField] private string REVEAL_ADVANCE_AUDIO = "pickem_multiplier_pick";
	[SerializeField] private string REVEAL_ADVANCE_VO = "pickem_multiplier_vo_pick";

	[SerializeField] private string REVEAL_END_ANIM = "";		// reveal end value
	[SerializeField] private string REVEAL_END_AUDIO = "pickem_reveal_bad";
	[SerializeField] private string REVEAL_END_VO = "pickem_reveal_bad_vo";

	[SerializeField] private string REVEAL_INCREASE_GRAY_ANIM = ""; 
	[SerializeField] private string REVEAL_ADVANCE_GRAY_ANIM = "";
	[SerializeField] private string REVEAL_END_GRAY_ANIM = "";

	[SerializeField] private float REVEAL_AUDIO_DELAY = 0.0f;
	[SerializeField] private float REVEAL_AUDIO_VO_DELAY = 0.2f;

	[SerializeField] protected string REVEAL_LEFTOVER_AUDIO = "reveal_not_chosen";

	[SerializeField] private LabelWrapperComponent[] ladderMultiplierText;	// collection of text references on the ladder
	[SerializeField] private string ladderMultiplierFormat = "{0}X";	// formatting string for ladder text display

	[SerializeField] private AnimationListController.AnimationInformationList[] ladderAdvanceAnimations; // collection of animators for the ladder
	[SerializeField] private AnimationListController.AnimationInformationList[] ladderDisableAnimations; // animations to reset previous ladder tiers
	[SerializeField] private AnimationListController.AnimationInformationList[] ladderIncreaseAnimations; // animations when increasing ladder values

	[SerializeField] private float LADDER_INCREASE_STAGGER_DELAY = 0.25f; // time to pause between each ladder increase

	[SerializeField] private AnimationListController.AnimationInformationList advanceCelebration;
	[SerializeField] private AnimationListController.AnimationInformationList increaseCelebration;

	[SerializeField] private ParticleTrailController[] ladderParticleControllers; // particle controllers for each ladder level
	[SerializeField] private LabelWrapperComponent[] ladderParticleTrailLabels; // labels for particle controllers to match text


	protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		// if we're advancing or increasing the multipliers
		if (pickData.verticalShift > 0 || pickData.horizontalShift > 0 || pickData.isGameOver)
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	public override bool needsToExecuteOnRoundInit()
	{
		return true;
	}

	public override void executeOnRoundInit(ModularChallengeGameVariant round)
	{
		base.executeOnRoundInit(round);

		StartCoroutine(populateLadderText(currentHorizontalIndex));
		StartCoroutine(updateLadderPosition(currentVerticalIndex));
	}

	private IEnumerator populateLadderText(int horizontal)
	{
		Debug.Log("Query pickinfos for pool + " + poolKey);
		List<ModularChallengeGameOutcome.PickPoolInfo> ladderTierInfos = pickingVariantParent.outcome.getPoolInfoForLadderTier(poolKey, horizontal);

		for (int i = 0; i < ladderMultiplierText.Length; i++)
		{
			if (i >= ladderTierInfos.Count)
			{
				Debug.LogError(string.Format("Too many ladder text components {0}, only {1} pool infos available!", ladderMultiplierText.Length, ladderTierInfos.Count));
				break;
			}

			ladderMultiplierText[i].text = string.Format(ladderMultiplierFormat, ladderTierInfos[i].multiplier);
		}

		yield return null;
	}

	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		ModularChallengeGameOutcomeEntry currentPickOutcome = pickingVariantParent.getCurrentPickOutcome();

		// set up proper animation for reveal, update the ladder values if necessary
		if (currentPickOutcome.horizontalShift > 0)
		{
			yield return StartCoroutine(increaseLadder(pickItem, currentPickOutcome));
		}
		else if (currentPickOutcome.verticalShift > 0)
		{
			yield return StartCoroutine(advanceLadder(pickItem, currentPickOutcome));
		}
		else if (currentPickOutcome.isGameOver)
		{
			yield return StartCoroutine(awardFinalValue(pickItem, currentPickOutcome));
		}
	}

	// increase all ladder values
	private IEnumerator increaseLadder(PickingGameBasePickItem pickItem, ModularChallengeGameOutcomeEntry currentPickOutcome)
	{
		pickItem.setRevealAnim(REVEAL_INCREASE_ANIM);
		Audio.playSoundMapOrSoundKeyWithDelay(REVEAL_INCREASE_AUDIO, REVEAL_AUDIO_DELAY);
		Audio.playSoundMapOrSoundKeyWithDelay(REVEAL_INCREASE_VO, REVEAL_AUDIO_VO_DELAY);

		// reveal the item
		yield return StartCoroutine(base.executeOnItemClick(pickItem));

		currentHorizontalIndex += currentPickOutcome.horizontalShift;
		yield return StartCoroutine(increaseLadderValues(currentHorizontalIndex));
	}

	// advance the indicator vertically up the ladder
	private IEnumerator advanceLadder(PickingGameBasePickItem pickItem, ModularChallengeGameOutcomeEntry currentPickOutcome)
	{
		pickItem.setRevealAnim(REVEAL_ADVANCE_ANIM);
		Audio.playSoundMapOrSoundKeyWithDelay(REVEAL_ADVANCE_AUDIO, REVEAL_AUDIO_DELAY);
		Audio.playSoundMapOrSoundKeyWithDelay(REVEAL_ADVANCE_VO, REVEAL_AUDIO_VO_DELAY);

		// reveal the item
		yield return StartCoroutine(base.executeOnItemClick(pickItem));

		currentVerticalIndex += currentPickOutcome.verticalShift;

		// play particle effect from pick to upcoming ladder value
		ParticleTrailController sparkleTrail = pickItem.GetComponent<ParticleTrailController>();
		if (sparkleTrail != null)
		{
			yield return StartCoroutine(sparkleTrail.animateParticleTrail(ladderMultiplierText[currentVerticalIndex].transform.position, pickItem.transform.parent));
		}

		// play the appropriate ladder effects
		yield return StartCoroutine(updateLadderPosition(currentVerticalIndex));
	}

	// award the final credit value with multiplier effects
	private IEnumerator awardFinalValue(PickingGameBasePickItem pickItem, ModularChallengeGameOutcomeEntry currentPickOutcome)
	{
		PickingGameCreditPickItem creditPick = pickItem.GetComponent<PickingGameCreditPickItem>();
		creditPick.setCreditLabels(currentPickOutcome.credits);

		pickItem.setRevealAnim(REVEAL_END_ANIM);
		Audio.playSoundMapOrSoundKeyWithDelay(REVEAL_END_AUDIO, REVEAL_AUDIO_DELAY);
		Audio.playSoundMapOrSoundKeyWithDelay(REVEAL_END_VO, REVEAL_AUDIO_VO_DELAY);


		// reveal the item
		yield return StartCoroutine(base.executeOnItemClick(pickItem));

		// rollup stage 1 (unmultiplied)
		yield return StartCoroutine(base.rollupCredits(currentPickOutcome.credits));

		// sparkle trail from ladder to winbox
		if (ladderParticleControllers.Length > currentVerticalIndex)
		{
			ParticleTrailController sparkleTrail = ladderParticleControllers[currentVerticalIndex];
			if (sparkleTrail != null)
			{
				// if we have a text label on the trail, match it to the current multiplier level
				if (ladderParticleTrailLabels.Length > currentVerticalIndex)
				{
					ladderParticleTrailLabels[currentVerticalIndex].text = ladderMultiplierText[currentVerticalIndex].text;
				}

				yield return StartCoroutine(sparkleTrail.animateParticleTrail(roundVariantParent.winLabel.transform.position, sparkleTrail.transform));
			}
		}

		// determine remaining credits and rollup
		long remainingCredits = (currentPickOutcome.credits * currentMultiplier) - currentPickOutcome.credits;
		yield return StartCoroutine(base.rollupCredits(remainingCredits));
	}

	// update the current multiplier rank on the ladder, disable the previous
	private IEnumerator updateLadderPosition(int verticalIndex)
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(advanceCelebration));

		// if we have a previous rung (index - 1) activated, disable it.
		int previousIndex = verticalIndex - 1;
		if (previousIndex >= 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(ladderDisableAnimations[previousIndex]));
		}

		// play our celebration animation for the current ladder level
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(ladderAdvanceAnimations[verticalIndex]));
	}

	// update the displayed ladder values with effects
	private IEnumerator increaseLadderValues(int horizontalIndex)
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(increaseCelebration));

		List<ModularChallengeGameOutcome.PickPoolInfo> ladderTierInfos = pickingVariantParent.outcome.getPoolInfoForLadderTier(poolKey, horizontalIndex);

		// iterate through the ladder values, increasing each with an animation
		for (int i = 0; i < ladderMultiplierText.Length; i++)
		{
			// play increase animation if we have one
			if (ladderIncreaseAnimations.Length > i)
			{
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(ladderIncreaseAnimations[i]));
			}

			ladderMultiplierText[i].text = string.Format(ladderMultiplierFormat, ladderTierInfos[i].multiplier);
			yield return new TIWaitForSeconds(LADDER_INCREASE_STAGGER_DELAY);
		}
	}

	public override bool needsToExecuteOnRevealLeftover(ModularChallengeGameOutcomeEntry pickData)
	{
		return true;
	}

	public override IEnumerator executeOnRevealLeftover(PickingGameBasePickItem leftover)
	{
		ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentLeftoverOutcome();

		// set up proper animation for reveal
		if (currentPick.horizontalShift > 0)
		{
			leftover.REVEAL_ANIMATION_GRAY = REVEAL_INCREASE_GRAY_ANIM;
		}
		else if (currentPick.verticalShift > 0)
		{
			leftover.REVEAL_ANIMATION_GRAY = REVEAL_ADVANCE_GRAY_ANIM;
		}
		else if (currentPick.isGameOver)
		{
			leftover.GetComponent<PickingGameCreditPickItem>().setCreditLabels(currentPick.credits);
			leftover.REVEAL_ANIMATION_GRAY = REVEAL_END_GRAY_ANIM;
		}

		// play the associated leftover reveal sound
		Audio.playSoundMapOrSoundKey(REVEAL_LEFTOVER_AUDIO);
			
		yield return StartCoroutine(base.executeOnRevealLeftover(leftover));
	}
}
