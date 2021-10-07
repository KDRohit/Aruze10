using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Class made for the Tug of War feature of munsters01 where there is a special multiplier 
applied to a revealed credit value, and then that value rolls up to the spin panel on 
top of the value that was already there.
*/
public class PickingGameCreditsWithJackpotMultiplierForBaseGameModule : PickingGameCreditsModule 
{
	[Tooltip("The icons that will be tweened to the pick to multiply the number up")]
	[SerializeField] private List<ParticleTrailController> multiplierIcons = new List<ParticleTrailController>(); // the icons that will be tweened to the pick to multiply the number up
	[Tooltip("Adds a delay so the player can see the credit amount before the multiplier effects start")]
	[SerializeField] private float PRE_MULTIPLIER_EFFECTS_DELAY = 0.0f; // use to add a delay so the player can see the credit amount before the multiplier effects start
	[Tooltip("Adds a delay before the rollup starts")]
	[SerializeField] private float BEFORE_ROLLUP_DELAY = 0.0f;

	private ModularChallengeGameOutcomeEntry currentPick = null;
	private PickingGameCreditPickItem currentCreditsRevealItem = null;

	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		currentPick = pickingVariantParent.getCurrentPickOutcome();

		// play the associated reveal sound
		Audio.playWithDelay(Audio.soundMap(REVEAL_AUDIO), REVEAL_AUDIO_DELAY);

		if (!string.IsNullOrEmpty(REVEAL_VO_AUDIO))
		{
			// play the associated audio voiceover
			Audio.playSoundMapOrSoundKeyWithDelay(REVEAL_VO_AUDIO, REVEAL_VO_DELAY);
		}
			
		//set the credit value within the item and the reveal animation
		pickItem.setRevealAnim(REVEAL_ANIMATION_NAME, REVEAL_ANIMATION_DURATION_OVERRIDE);

		currentCreditsRevealItem = PickingGameCreditPickItem.getPickingGameCreditsPickItemForType(pickItem.gameObject, PickingGameCreditPickItem.CreditsPickItemType.Default);
		
		// Get the TugOfWarModule and ask it for the hacked credit amount
		List<SlotModule> activeGameModules = ReelGame.activeGame.cachedAttachedSlotModules;
		TugOfWarModule tugOfWarModule = null;
		for (int i = 0; i < activeGameModules.Count; i++)
		{
			SlotModule currentModule = activeGameModules[i];
			if (currentModule is TugOfWarModule)
			{
				tugOfWarModule = currentModule as TugOfWarModule;
				break;
			}
		}

		long fullCreditAmount = 0;
		if (tugOfWarModule != null)
		{
			fullCreditAmount = tugOfWarModule.getPickingGameRawCredits();
			// if no "rawCredits" value was found we will default back to using credits
			// this will allow us at some point hopefully to just update the server data
			// to place the rawCredits value in credits
			if (fullCreditAmount == 0)
			{
				fullCreditAmount = currentPick.baseCredits;
			}
		}
		else
		{
			Debug.LogError("PickingGameCreditsWithJackpotMultiplierForBaseGameModule.executeOnItemClick() - ReelGame didn't have a TugOfWarModule attached!  Using 0 for credit value!");
		}
		
		currentCreditsRevealItem.setCreditLabels(fullCreditAmount);
		//Debug.Log("currentPick.credits = " + fullCreditAmount);
		yield return StartCoroutine(executeBasicOnRevealPick(pickItem));

		if (PRE_MULTIPLIER_EFFECTS_DELAY != 0.0f)
		{
			yield return new TIWaitForSeconds(PRE_MULTIPLIER_EFFECTS_DELAY);
		}

		// Add the multiplier here and calculate the final value we should actually be applying to the player
		if (multiplierIcons.Count > 0)
		{
			// determine which icon is showing
			ParticleTrailController multiplierIconToAnimate = null;

			for (int i = 0; i < multiplierIcons.Count; i++)
			{
				ParticleTrailController particleTrail = multiplierIcons[i];

				if (particleTrail.gameObject.activeInHierarchy && particleTrail.gameObject.layer != Layers.ID_HIDDEN)
				{
					multiplierIconToAnimate = particleTrail;
					break;
				}
			}

			if (multiplierIconToAnimate != null)
			{
				yield return StartCoroutine(multiplierIconToAnimate.animateParticleTrail(currentCreditsRevealItem.getCreditLabelTransform().position, multiplierIconToAnimate.transform, onParticleTrailArrive));
			}
		}

		long awardMultiplier = pickingVariantParent.outcome.newBaseBonusAwardMultiplier;

		if (awardMultiplier != -1)
		{
			long awardMultipliedCredits = fullCreditAmount * awardMultiplier;

			// zero out ReelGame.activeGame.runningPayoutRollupValue so that the rollup doesn't cause the current value to be factored in twice
			long currentSpinPanelWinValue = ReelGame.activeGame.getCurrentRunningPayoutRollupValue();
			ReelGame.activeGame.setRunningPayoutRollupValue(0);

			// add a delay befor the rollup if you want to seperate sounds or ensure the player has a chance to see things
			if (BEFORE_ROLLUP_DELAY != 0.0f)
			{
				yield return new TIWaitForSeconds(BEFORE_ROLLUP_DELAY);
			}

			// rollup with extra animations included
			yield return StartCoroutine(base.rollupCredits(currentSpinPanelWinValue, currentSpinPanelWinValue + awardMultipliedCredits, true));
		}
		else
		{
			Debug.LogError("PickingGameCreditsWithJackpotMultiForBaseGameModule.executeOnItemClick() - Outcome was missing \"award_multiplier\" field!");
		}
	}

	// Called by particle trail when it arrives at its destination, used to sync the credit label change with the particle arrive anim/sounds
	public IEnumerator onParticleTrailArrive()
	{
		long fullCreditAmount = currentPick.credits;
		long awardMultiplier = pickingVariantParent.outcome.newBaseBonusAwardMultiplier;

		if (awardMultiplier != -1)
		{
			long awardMultipliedCredits = fullCreditAmount * awardMultiplier;

			if (currentCreditsRevealItem != null)
			{
				currentCreditsRevealItem.setCreditLabels(awardMultipliedCredits);
			}
		}
		else
		{
			Debug.LogError("PickingGameCreditsWithJackpotMultiForBaseGameModule.onParticleTrailArrive() - Outcome was missing \"award_multiplier\" field!");
		}

		yield break;
	}
}
