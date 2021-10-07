using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PickingGameIncreaseJackpotAndCreditsModule : PickingGameRevealModule
{
	[SerializeField] private List<JackpotRevealInformation> jackpotIncreaseRevealInfo = new List<JackpotRevealInformation>();
	
	[SerializeField] private string REVEAL_AUDIO = "pickem_advance_multiplier";
	[SerializeField] private float REVEAL_AUDIO_DELAY = 0.0f;
	[SerializeField] private string REVEAL_VO = "pickem_advance_multiplier_vo";
	[SerializeField] private float REVEAL_VO_DELAY = 0.0f;
	
	[SerializeField] private string REVEAL_LEFTOVER_AUDIO = "reveal_not_chosen";
	
	[SerializeField] private string MAX_MULTIPLIER_REACHED_AUDIO = "pickem_reached_max_multiplier";
	[SerializeField] private float MAX_MULTIPLIER_REACHED_AUDIO_DELAY = 0.0f;
	[SerializeField] private string MAX_MULTIPLIER_REACHED_VO = "pickem_reached_max_multiplier_vo";
	[SerializeField] private float MAX_MULTIPLIER_REACHED_VO_DELAY = 0.0f;
	[SerializeField] private bool useMultiplierLabel = false;

	[SerializeField] private AnimationListController.AnimationInformationList increaseJackpotAnimationList;
	
    private int maxNumberOfMultipliers = 0;

	private JackpotRevealInformation currentRevealInfo = null;
	private int currentNumberOfMultiplierRevealed = 0;
	private long currentJackpotValue = 0;

	public override bool needsToExecuteOnRoundInit()
	{
		return true;
	}

	public override void executeOnRoundInit(ModularChallengeGameVariant round)
	{
		base.executeOnRoundInit(round);
		currentJackpotValue = roundVariantParent.outcome.jackpotBaseValue;
		foreach (ModularChallengeGameOutcomeEntry entry in roundVariantParent.getCurrentRoundOutcome().entries)
		{
			if (entry.pickemPick.jackpotIncrease > 0)
			{
				maxNumberOfMultipliers++;
			}
		}

		foreach (ModularChallengeGameOutcomeEntry reveal in roundVariantParent.getCurrentRoundOutcome().reveals)
		{
			if (reveal.pickemPick.jackpotIncrease > 0)
			{
				maxNumberOfMultipliers++;
			}
		}
	}

	protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		if (pickData == null)
		{
			Debug.LogError("PickingGameJackpotModule.shouldHandle() - pickData is null!");
			return false;
		}

		if (pickData.pickemPick.jackpotIncrease > 0)
		{
			return true;
		}
		return false;
	}

	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		currentRevealInfo = getCurrentRevealInfo();
		currentNumberOfMultiplierRevealed++;
		ModularChallengeGameOutcomeEntry currentPick = (roundVariantParent as ModularPickingGameVariant).getCurrentPickOutcome();

		if (currentNumberOfMultiplierRevealed >= maxNumberOfMultipliers)
		{
			Audio.playSoundMapOrSoundKeyWithDelay(MAX_MULTIPLIER_REACHED_AUDIO, MAX_MULTIPLIER_REACHED_AUDIO_DELAY);
			Audio.tryToPlaySoundMapWithDelay(MAX_MULTIPLIER_REACHED_VO, MAX_MULTIPLIER_REACHED_VO_DELAY);
		}
		else
		{
			Audio.playSoundMapOrSoundKeyWithDelay(REVEAL_AUDIO, REVEAL_AUDIO_DELAY);
			Audio.tryToPlaySoundMapWithDelay(REVEAL_VO, REVEAL_VO_DELAY);
		}
		
		if (currentRevealInfo != null)
		{
			if (!string.IsNullOrEmpty(currentRevealInfo.REVEAL_VO_AUDIO))
			{
				Audio.playSoundMapOrSoundKeyWithDelay(currentRevealInfo.REVEAL_VO_AUDIO, currentRevealInfo.REVEAL_VO_DELAY);
			}
			// set the animation within the item and the reveal animation
			pickItem.REVEAL_ANIMATION = currentRevealInfo.REVEAL_ANIM_NAME;
			yield return StartCoroutine(base.executeOnItemClick(pickItem));

			//Do sparkle Trail stuff
			ParticleTrailController particleTrailController = ParticleTrailController.getParticleTrailControllerForType(pickItem.gameObject, ParticleTrailController.ParticleTrailControllerType.Jackpot);
			
			if (particleTrailController != null)
			{
				yield return StartCoroutine(particleTrailController.animateParticleTrail(roundVariantParent.jackpotLabel.gameObject.transform.position, roundVariantParent.gameObject.transform));
			}
			
			if (increaseJackpotAnimationList != null && increaseJackpotAnimationList.Count > 0)
			{
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(increaseJackpotAnimationList));
			}

			currentJackpotValue += currentPick.jackpotIncrease;
			roundVariantParent.jackpotLabel.text = CreditsEconomy.convertCredits(currentJackpotValue);

			if (currentRevealInfo.REVEAL_CREDITS_DELAY > 0.0f)
			{
				yield return new TIWaitForSeconds(currentRevealInfo.REVEAL_CREDITS_DELAY);
			}

			pickItem.REVEAL_ANIMATION = currentRevealInfo.REVEAL_CREDITS_ANIM_NAME;
		}
		
		if (!string.IsNullOrEmpty(pickItem.REVEAL_ANIMATION))
		{
			if (!useMultiplierLabel)
			{
				PickingGameCreditPickItem creditsRevealItem = PickingGameCreditPickItem.getPickingGameCreditsPickItemForType(pickItem.gameObject, PickingGameCreditPickItem.CreditsPickItemType.Default);
				creditsRevealItem.setCreditLabels(currentPick.credits);
			}
			else
			{
				PickingGameCreditPickItem multiplierRevealItem = PickingGameCreditPickItem.getPickingGameCreditsPickItemForType(pickItem.gameObject, PickingGameCreditPickItem.CreditsPickItemType.Multiplier);
				multiplierRevealItem.setCreditLabels(currentPick.credits);
			}
            yield return StartCoroutine(base.executeOnItemClick(pickItem));
			yield return StartCoroutine(base.rollupCredits(currentPick.credits));
		}
	}

	public override IEnumerator executeOnRevealPick(PickingGameBasePickItem pickItem)
	{
		yield return StartCoroutine(base.executeOnRevealPick(pickItem));
	}

	public override IEnumerator executeOnRevealLeftover(PickingGameBasePickItem leftover)
	{
		#pragma warning disable 219 // The variable 'action' is assigned but its balue is never used (CS0219)
		ModularChallengeGameOutcomeEntry currentPick = (roundVariantParent as ModularPickingGameVariant).getCurrentLeftoverOutcome();
		#pragma warning restore 219

		currentRevealInfo = getCurrentRevealInfo();
		if (currentRevealInfo != null)
		{
			Audio.playSoundMapOrSoundKey(REVEAL_LEFTOVER_AUDIO);
			leftover.REVEAL_ANIMATION_GRAY = currentRevealInfo.REVEAL_GRAY_ANIM_NAME;
			yield return StartCoroutine(base.executeOnRevealLeftover(leftover));
		}
	}

	protected virtual JackpotRevealInformation getCurrentRevealInfo()
	{
		int randomRevealIndex = Random.Range(0, jackpotIncreaseRevealInfo.Count-1);
		if (jackpotIncreaseRevealInfo[randomRevealIndex] == null)
		{
			Debug.LogError("PickingGameIncreaseJackpotAndCreditsModule.getCurrentRevealInfo currentInfo is null!");
			return null;
		}
		JackpotRevealInformation currentInfo = jackpotIncreaseRevealInfo[randomRevealIndex];
		jackpotIncreaseRevealInfo.RemoveAt(randomRevealIndex); //There should only be one of each type of jackpot increase reveal. 
		return currentInfo;
	}

	[System.Serializable]
	protected class JackpotRevealInformation
	{
		public string REVEAL_ANIM_NAME = "";
		public string REVEAL_GRAY_ANIM_NAME = "";

		public float REVEAL_CREDITS_DELAY = 0.0f; // Wait this long before playing hte reveal credits anim.
		public string REVEAL_CREDITS_ANIM_NAME = "";
		
		public string REVEAL_VO_AUDIO = "";
		public float REVEAL_VO_DELAY = 0.25f;
	}
}