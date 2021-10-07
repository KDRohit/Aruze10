using System.Collections;
using UnityEngine;

/*
 * Module for the picking game of gen97 Cash Tower that goes before the freespins where you reveal how many spins you will get,
 * with a bad reveal which will end the picking game and proceed into the freespins.  This class will also be extended with
 * PickingGameIncreaseFreespinCountWithSuperBonus which will additionally handle the Super Bonus meter that gen97 Cash Tower has
 * as well as launching into the Super Bonus and returning to the same state of the picking game when you return so you can
 * finish it and launch the Standard Freespins.
 *
 * Creation Date: 2/4/2020
 * Original Author: Scott Lepthien
 */
public class PickingGameIncreaseFreespinCountModule : PickingGameRevealModule
{
	[Header("Reveal Spins Settings")]
	[Tooltip("Use this is every spin count value that can be revealed has a different animation that needs to be played.")]
	[SerializeField] protected AnimationDataBySpinCount[] revealSpinCountAnimDataByValueArray;
	[Tooltip("Label for label UI element which shows how many spins the player as been awarded so far.")]
	[SerializeField] protected LabelWrapperComponent wonSpinAmountLabel;

	[Tooltip("Particle effect(s) played when a spin value amount is revealed.")]
	[SerializeField] protected AnimatedParticleEffect spinCountRevealedParticleEffect = null;
	[Tooltip("Tells if the spinCountRevealedParticleEffect should start from the revealed pick object.  Since original game just wanted a burst on the win meter this is set to false by default.")]
	[SerializeField] protected bool isStartingSpinCountRevealedParticleEffectAtPickObject = false;
	
	[Header("Reveal Gameover Settings")]
	[SerializeField] protected string REVEAL_GAMEOVER_ANIMATION_NAME = "revealPlayFreespins";
	[SerializeField] protected AudioListController.AudioInformationList revealGameoverAudio;
	[SerializeField] protected float REVEAL_GAMEOVER_ANIMATION_DURATION_OVERRIDE = -1.0f;

	[Header("Data Settings")] 
	[SerializeField] private bool usesFreeSpinMeter = true;
	[SerializeField] private bool handleEndGamePick = true;
	[SerializeField] private bool pickDataIncreasesSpins = false;
	[SerializeField] private float increaseSpinCountDelay = 0;
	private const string FREESPIN_METER_JSON_KEY = "free_spin_meter";
	private const string FREESPIN_METER_REEVAL_TYPE = "cash_tower"; // @todo : Consider making this settable so that another game using a different reevaluator but similar data could reuse this class
	
	private int freespinCount = 0;
	private JSON freeSpinMeterJson;
	
	public override bool needsToExecuteOnRoundInit()
	{
		return true;
	}

	public override void executeOnRoundInit(ModularChallengeGameVariant round)
	{
		base.executeOnRoundInit(round);
		freespinCount = 0;
		
		if(usesFreeSpinMeter)
		{
			// Extract out the freespin info for how many spin were awarded from the symbols that triggered the bonus.
			JSON[] reevaluations = SlotBaseGame.instance.outcome.getArrayReevaluations();
			foreach (JSON reevalJson in reevaluations)
			{
				string reevalType = reevalJson.getString("type", "");
				if (reevalType == FREESPIN_METER_REEVAL_TYPE)
				{
					freeSpinMeterJson = reevalJson.getJSON(FREESPIN_METER_JSON_KEY);
				}
			}

			if (freeSpinMeterJson == null)
			{
				Debug.LogError("PickingGameIncreaseFreespinCountModule.executeOnRoundInit() - Unable to find: " + FREESPIN_METER_JSON_KEY + "; field in reevalType: " + FREESPIN_METER_REEVAL_TYPE);
				return;
			}
			freespinCount = freeSpinMeterJson.getInt("new", 0);
		}

		if (wonSpinAmountLabel != null)
		{
			wonSpinAmountLabel.text = CommonText.formatNumber(freespinCount);
		}
	}

	protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		// We'll handle both the added spins and the bad pick which ends the game inside this one module
		return (pickData != null && pickData.spins > 0);
	}
	
	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentPickOutcome();
		
		if (currentPick == null || pickItem == null)
		{
			yield break;
		}

		if (currentPick.isGameOver)
		{
			// Handle revealing the "PLAY" gameover reveal and transitioning to freespins
			yield return StartCoroutine(revealGameOverPick(pickItem, currentPick));
		}
		else
		{
			// Handle adding the spins here
			yield return StartCoroutine(revealSpinCountPick(pickItem, currentPick));
		}
	}

	protected IEnumerator revealGameOverPick(PickingGameBasePickItem pickItem, ModularChallengeGameOutcomeEntry currentPick)
	{
		// Play the associated reveal sound
		if (revealGameoverAudio.Count > 0)
		{
			yield return StartCoroutine(AudioListController.playListOfAudioInformation(revealGameoverAudio));
		}
		
		pickItem.setRevealAnim(REVEAL_GAMEOVER_ANIMATION_NAME, REVEAL_GAMEOVER_ANIMATION_DURATION_OVERRIDE);
		
		yield return StartCoroutine(base.executeOnItemClick(pickItem));
	}

	protected virtual IEnumerator revealSpinCountPick(PickingGameBasePickItem pickItem, ModularChallengeGameOutcomeEntry currentPick)
	{
		AnimationDataBySpinCount animData = getAnimationDataForSpinCount(currentPick.spins);
	
		// Play the associated reveal sounds
		if (animData != null && animData.revealSpinsAudio.Count > 0)
		{
			yield return StartCoroutine(AudioListController.playListOfAudioInformation(animData.revealSpinsAudio));
		}
		
		// Set the multiplier value within the item and the reveal animation
		PickingGameSpinCountPickItem spinCountPick = pickItem.gameObject.GetComponent<PickingGameSpinCountPickItem>();

		// Some multiplier picks are using art which uses static art for the multiplier number instead of a modifiable number, so only set labels if it has the item attached
		if (spinCountPick != null)
		{
			spinCountPick.setSpinCountLabel(currentPick.spins);
		}

		if (animData != null)
		{
			pickItem.setRevealAnim(animData.REVEAL_SPINS_ANIMATION_NAME, animData.REVEAL_SPINS_ANIMATION_DURATION_OVERRIDE);
		}

		yield return StartCoroutine(base.executeOnItemClick(pickItem));
		
		if (animData != null && animData.spinCountAnimationList.animInfoList.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(animData.spinCountAnimationList));
		}
		
		// Play a reveal particle effect if one is setup
		if (spinCountRevealedParticleEffect != null)
		{
			if (isStartingSpinCountRevealedParticleEffectAtPickObject)
			{
				yield return StartCoroutine(spinCountRevealedParticleEffect.animateParticleEffect(pickItem.gameObject.transform));
			}
			else
			{
				yield return StartCoroutine(spinCountRevealedParticleEffect.animateParticleEffect());
			}
		}

		if (increaseSpinCountDelay > 0)
		{
			yield return new WaitForSeconds(increaseSpinCountDelay);
		}

		// Update the awarded spin count
		freespinCount += currentPick.spins;
		if (wonSpinAmountLabel != null)
		{
			wonSpinAmountLabel.text = CommonText.formatNumber(freespinCount);
		}

		if (!currentPick.isGameOver && pickDataIncreasesSpins && FreeSpinGame.instance != null)
		{
			FreeSpinGame.instance.numberOfFreespinsRemaining += currentPick.spins;
		}
	}
	
	public override bool needsToExecuteOnRevealLeftover(ModularChallengeGameOutcomeEntry pickData)
	{
		// We'll handle both the added spins and the bad pick which ends the game inside this one module
		return (pickData != null && pickData.spins > 0);
	}
	
	public override IEnumerator executeOnRevealLeftover(PickingGameBasePickItem leftover)
	{
		// set the credit value from the leftovers
		ModularChallengeGameOutcomeEntry leftoverOutcome = pickingVariantParent.getCurrentLeftoverOutcome();

		if (leftoverOutcome != null)
		{
			// Set the multiplier value within the item and the reveal animation
			PickingGameSpinCountPickItem spinCountPick = leftover.gameObject.GetComponent<PickingGameSpinCountPickItem>();

			// Some multiplier picks are using art which uses static art for the multiplier number instead of a modifiable number, so only set labels if it has the item attached
			if (spinCountPick != null)
			{
				spinCountPick.setSpinCountLabel(leftoverOutcome.spins);
			}
			
			AnimationDataBySpinCount animationProperties = getAnimationDataForSpinCount(leftoverOutcome.spins);
			if (animationProperties != null)
			{
				leftover.REVEAL_ANIMATION_GRAY = animationProperties.REVEAL_SPINS_ANIMATION_GRAY_NAME;
			}
			else
			{
				Debug.LogError("PickingGameCreditsWithGroupIdModule.executeOnRevealLeftover() - leftover item didn't have any animationProperties!");
			}
			
			if (animationProperties.revealSpinsAudio != null)
			{
				// play the associated leftover reveal sound
				AudioListController.playListOfAudioInformation(animationProperties.revealSpinsAudio);
			}
		}

		yield return StartCoroutine(base.executeOnRevealLeftover(leftover));
	}

	protected AnimationDataBySpinCount getAnimationDataForSpinCount(int spinCount)
	{
		foreach (AnimationDataBySpinCount animData in revealSpinCountAnimDataByValueArray)
		{
			if (animData.spinCount == spinCount)
			{
				return animData;
			}
		}
		
#if UNITY_EDITOR
		Debug.LogError("PickingGameIncreaseFreespinCountModule.getAnimationDataForSpinCount() - Couldn't find entry for spinCount = " + spinCount + "; will cause visual issues.");
#endif
		return null;
	}

	[System.Serializable]
	protected class AnimationDataBySpinCount
	{
		[Tooltip("The amount of spins which this set of animation data should be used for")]
		[SerializeField] public int spinCount = -1;
		[Tooltip("The animation that will be played")]
		[SerializeField] public string REVEAL_SPINS_ANIMATION_NAME = "revealFreespinCount";
		[Tooltip("Sounds to accompany the animation")]
		[SerializeField] public AudioListController.AudioInformationList revealSpinsAudio;
		[Tooltip("Override for how long the animation is.")]
		[SerializeField] public float REVEAL_SPINS_ANIMATION_DURATION_OVERRIDE = -1.0f;
		[Tooltip("The animation that will be played on left over reveal")]
		[SerializeField] public string REVEAL_SPINS_ANIMATION_GRAY_NAME = "";
		[Tooltip("Sounds to accompany the gray reveal animation")]
		[SerializeField] public AudioListController.AudioInformationList revealSpinsGrayAudio;
		[Tooltip("Animations not attached to the spin count pick item to be played when this item is picked (if any)")]
		public AnimationListController.AnimationInformationList spinCountAnimationList;
	}
}
