using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Module to handle revealing grouped symbols for matching in a round
 */
public class PickingGameFindAllJackpotsAndAddPickModule : PickingGameFindAllJackpotsModule 
{
	[SerializeField] private bool isMaxVoltageJackpot = false;
	[SerializeField] private string JACKPOT_ROLLUP_SOUND_LOOP_OVERRIDE = "";
	[SerializeField] private string JACKPOT_ROLLUP_SOUND_END_OVERRIDE = "";

	public override void executeOnRoundInit(ModularChallengeGameVariant round)
	{
		base.executeOnRoundInit(round);

		// execute base method first to store outcome & set default label
		foreach (JSON paytableGroup in round.outcome.paytableGroups)
		{
			if (paytableGroup.getString("key_name", "") == "JACKPOT")
			{
				jackpotAmount = paytableGroup.getLong("credits", 0L) * GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;
				jackpotsRequired = paytableGroup.getInt("hits_needed", 0);
			}
		}

		if (jackpotAmount == 0 && isMaxVoltageJackpot)
		{
			jackpotAmount = MaxVoltageTokenCollectionModule.jackpotValue;
			if (jackpotAmount == 0)
			{
				jackpotAmount = ProgressiveJackpot.maxVoltageJackpot.pool;
			}
		}
	}

	public override bool needsToExecuteOnRoundStart ()
	{
		return isMaxVoltageJackpot;
	}

	public override IEnumerator executeOnRoundStart()
	{
		yield return StartCoroutine(base.executeOnRoundStart());
		roundVariantParent.jackpotLabel.text = CreditsEconomy.convertCredits(jackpotAmount);
	}

	protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		if (pickData != null && (pickData.isJackpot || pickData.groupId == "JACKPOT") && pickData.additonalPicks > 0)
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	protected override IEnumerator winningJackpot(ModularChallengeGameOutcomeEntry currentPick, PickingGameBasePickItem pickItem)
	{
		ParticleTrailController particleTrailController = ParticleTrailController.getParticleTrailControllerForType(pickItem.gameObject, ParticleTrailController.ParticleTrailControllerType.Jackpot);

		ParticleTrailController particleTrailControllerWin = null;

		ParticleTrailController increasePicksController = ParticleTrailController.getParticleTrailControllerForType(pickItem.gameObject, ParticleTrailController.ParticleTrailControllerType.IncreasePicks);

		if (jackpotWinEffectAnimator != null)
		{
			particleTrailControllerWin = ParticleTrailController.getParticleTrailControllerForType(jackpotWinEffectAnimator.gameObject, ParticleTrailController.ParticleTrailControllerType.Advance);
		}

		if (particleTrailController != null)
		{            
			if (foundItemTransforms != null && jackpotsFound < foundItemTransforms.Count)
			{                  
				yield return StartCoroutine(particleTrailController.animateParticleTrail(foundItemTransforms[jackpotsFound].position, roundVariantParent.gameObject.transform));
			}		
			else
			{
				if (foundItemTransforms == null)
				{
					Debug.LogError("PickingGameFindAllJackpotsModule.winningJackpot() - The sparkle trail in this module needs the foundItemsTranforms list set up!");
				}
				else if (jackpotsFound < foundItemTransforms.Count)
				{
					Debug.LogError("PickingGameFindAllJackpotsModule.winningJackpot() - The sparkle trail was skipped because jackpotsFound = " + jackpotsFound + "; is out of bounds of foundItemTransforms.Count = " + foundItemTransforms.Count + "!");
				}
			}		
		}

		if (jackpotsFound < foundItems.Count)
		{
			if (foundItems[jackpotsFound].Count > 0)
			{
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(foundItems[jackpotsFound]));
			}
		}
		else
		{
			Debug.LogError("PickingGameFindAllJackpotsModule.winningJackpot() - jackpotsFound = " + jackpotsFound + "; is out of bounds of foundItems.Count = " + foundItems.Count + "!");
		}

		if (increasePicksController != null)
		{
			yield return StartCoroutine(increasePicksController.animateParticleTrail(pickingVariantParent.picksRemainingLabel.gameObject.transform.position, pickItem.gameObject.transform));
		}

		yield return StartCoroutine(pickingVariantParent.gameParent.increasePicks(currentPick.additonalPicks));
		pickingVariantParent.updatePicksRemainingLabel();

		jackpotsFound++;
		if (jackpotsFound == jackpotsRequired)
		{
			if (foundAllAnimator != null && !string.IsNullOrEmpty(FOUND_ALL_ANIMATION_NAME))
			{
				foundAllAnimator.Play(FOUND_ALL_ANIMATION_NAME);
			}

			Audio.play(Audio.soundMap(FOUND_ALL_SOUND_NAME), 1, 0, FOUND_ALL_SOUND_DELAY);
			Audio.tryToPlaySoundMapWithDelay(FOUND_ALL_VO_NAME, FOUND_ALL_VO_DELAY);

			Audio.play(Audio.soundMap(JACKPOT_WIN_SOUND_NAME));

			if (jackpotWinEffectAnimator != null && !string.IsNullOrEmpty(JACKPOT_WIN_ANIMATION_NAME))
			{
				yield return StartCoroutine(CommonAnimation.playAnimAndWait(jackpotWinEffectAnimator, JACKPOT_WIN_ANIMATION_NAME));
			}

			if (particleTrailControllerWin != null)
			{
				if (jackpotWinParticleTrailDestination != null)
				{
					yield return StartCoroutine(particleTrailControllerWin.animateParticleTrail(jackpotWinParticleTrailDestination.position, roundVariantParent.gameObject.transform));
				}
				else
				{
					yield return StartCoroutine(particleTrailControllerWin.animateParticleTrail(winLabel.gameObject.transform.position, roundVariantParent.gameObject.transform));
				}
			}

			if (foundAllAnimator != null && !string.IsNullOrEmpty(FOUND_ALL_PRE_ROLLUP_ANIMATION_NAME))
			{
				foundAllAnimator.Play(FOUND_ALL_PRE_ROLLUP_ANIMATION_NAME);
			}

			if (string.IsNullOrEmpty(JACKPOT_ROLLUP_SOUND_END_OVERRIDE) || string.IsNullOrEmpty(JACKPOT_ROLLUP_SOUND_LOOP_OVERRIDE))
			{
				yield return StartCoroutine(rollupCredits(jackpotAmount));
			}
			else
			{
				long startScore = BonusGamePresenter.instance.currentPayout;
				BonusGamePresenter.instance.currentPayout += jackpotAmount;
				long endScore = BonusGamePresenter.instance.currentPayout;
				yield return StartCoroutine (
					SlotUtils.rollup (
						startScore,
						endScore,
						roundVariantParent.winLabel,
						rollupOverrideSound: Audio.tryConvertSoundKeyToMappedValue(JACKPOT_ROLLUP_SOUND_LOOP_OVERRIDE),
						rollupTermOverrideSound: Audio.tryConvertSoundKeyToMappedValue(JACKPOT_ROLLUP_SOUND_END_OVERRIDE)
					)
				);
			}

			if (foundAllAnimator != null && !string.IsNullOrEmpty(FOUND_ALL_POST_ROLLUP_ANIMATION_NAME))
			{
				foundAllAnimator.Play(FOUND_ALL_POST_ROLLUP_ANIMATION_NAME);
			}
		}
	}
}
