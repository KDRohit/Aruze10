using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Module to handle revealing several jackpot symbols to obtain a jackpot
 */
public class PickingGameFindAllJackpotsModule : PickingGameJackpotModule
{
	protected int jackpotsRequired = 0;
	
	[SerializeField] protected string[] REVEAL_ANIMATION_NAMES;
	[SerializeField] protected string[] REVEAL_GRAY_ANIMATION_NAMES;
	
	[SerializeField] protected List<AnimationListController.AnimationInformationList> foundItems = new List<AnimationListController.AnimationInformationList>();
	[SerializeField] protected List<Transform> foundItemTransforms = new List<Transform>();
	[SerializeField] protected Animator foundAllAnimator;
	[SerializeField] protected string FOUND_ALL_ANIMATION_NAME;
	[SerializeField] protected string FOUND_ALL_SOUND_NAME;
	[SerializeField] protected float FOUND_ALL_SOUND_DELAY;
	
	[SerializeField] protected string FOUND_ALL_VO_NAME;
	[SerializeField] protected float FOUND_ALL_VO_DELAY;
	
	[SerializeField] protected string FOUND_ALL_PRE_ROLLUP_ANIMATION_NAME;
	[SerializeField] protected string FOUND_ALL_POST_ROLLUP_ANIMATION_NAME;
	[SerializeField] protected Transform jackpotWinParticleTrailDestination = null;

	protected int jackpotsFound = 0;

	public override bool needsToExecuteOnRoundInit()
	{
		return true;
	}

	public override void executeOnRoundInit(ModularChallengeGameVariant round)
	{
		// execute base method first to store outcome & set default label
		base.executeOnRoundInit(round);

		jackpotsFound = 0;
		if (round.outcome.paytableGroups != null)
		{
			foreach (JSON paytableGroup in round.outcome.paytableGroups)
			{
				jackpotAmount = paytableGroup.getLong("credits", 0L) * GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;
				if (jackpotAmount == roundVariantParent.highestCreditRevealAmount)
				{
					jackpotsRequired = paytableGroup.getInt("cards_required", 0);
					jackpotLabel.text = CreditsEconomy.convertCredits(jackpotAmount);
				}
			}
		}
	}

	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		//make the item unclickable
		pickItem.setClickable(false);

		// retrieve current pick data
		ModularChallengeGameOutcomeEntry currentPick = (roundVariantParent as ModularPickingGameVariant).getCurrentPickOutcome();
		
		if (REVEAL_ANIMATION_NAMES.Length > 0 && jackpotsFound < REVEAL_ANIMATION_NAMES.Length)
		{
			REVEAL_ANIMATION_NAME = REVEAL_ANIMATION_NAMES[jackpotsFound];
		}
		
		yield return StartCoroutine(base.executeOnItemClick(pickItem));			

	}

	protected override IEnumerator winningJackpot(ModularChallengeGameOutcomeEntry currentPick, PickingGameBasePickItem pickItem)
	{
		ParticleTrailController particleTrailController = ParticleTrailController.getParticleTrailControllerForType(pickItem.gameObject, ParticleTrailController.ParticleTrailControllerType.Jackpot);
        
        ParticleTrailController particleTrailControllerWin = null;
        
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
			
			yield return StartCoroutine(rollupCredits(currentPick.credits));
            
			if (foundAllAnimator != null && !string.IsNullOrEmpty(FOUND_ALL_POST_ROLLUP_ANIMATION_NAME))
			{
				foundAllAnimator.Play(FOUND_ALL_POST_ROLLUP_ANIMATION_NAME);
			}
		}
	}

	public override IEnumerator executeOnRevealPick(PickingGameBasePickItem pickItem)
	{
		yield return StartCoroutine(base.executeOnRevealPick(pickItem));
	}

	public override IEnumerator executeOnRevealLeftover(PickingGameBasePickItem leftover)
	{
		if (REVEAL_GRAY_ANIMATION_NAMES.Length > 0 && jackpotsFound < REVEAL_GRAY_ANIMATION_NAMES.Length)
		{
			REVEAL_GRAY_ANIMATION_NAME = REVEAL_GRAY_ANIMATION_NAMES[jackpotsFound];
		}

		yield return StartCoroutine(base.executeOnRevealLeftover(leftover));
	}
}
