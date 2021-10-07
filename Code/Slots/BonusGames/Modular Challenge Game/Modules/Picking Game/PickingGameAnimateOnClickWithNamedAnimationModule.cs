using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 *  Used to fire off an animation when a pick item is clicked and keyed off the "group" value of that pick
 *  to trigger an animation linked to a key and name
 *
 *  Used By: billions02
 */
public class PickingGameAnimateOnClickWithNamedAnimationModule : PickingGameRevealModule
{
	[SerializeField] protected bool USE_BASE_CREDIT_AMOUNT = false;
	
	[SerializeField] private List<PickAnimationAudioSet> pickAnimationsSets;
	private Dictionary<string, PickAnimationAudioSet> groupIdToPickAnimationsSetMap;

	[Serializable]
	public class PickAnimationAudioSet
	{
		public string groupId;
		public string REVEAL_ANIMATION_NAME = "revealCredit";
		public float REVEAL_ANIMATION_DURATION_OVERRIDE;
		public string REVEAL_GRAY_ANIMATION_NAME = "revealCreditGray";
		public string REVEAL_AUDIO = "pickem_credits_pick";
		public string REVEAL_VO_AUDIO = "pickem_credits_vo_pick";
		public float REVEAL_AUDIO_DELAY = 0.0f; // delay before playing the reveal clip
		public float REVEAL_VO_DELAY = 0.2f; // delay before playing the VO clip
		public string REVEAL_AUDIO_GRAY = "reveal_not_chosen";
	}
	
	public override bool needsToExecuteOnRoundInit()
	{
		return true;
	}
	
	protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		return pickData != null &&
		       !pickData.canAdvance &&
		       pickData.additonalPicks == 0 &&
		       pickData.extraRound == 0 && 
		       (!pickData.isGameOver || pickData.isGameOver && ( roundVariantParent.roundIndex == roundVariantParent.gameParent.pickingRounds.Count-1 || pickingVariantParent.gameParent.getDisplayedPicksRemaining() >= 0));
	}

	public override void executeOnRoundInit(ModularChallengeGameVariant round)
	{
		base.executeOnRoundInit(round);

		//create a dictionary lookup from the list of id to animation name mappings
		//so we don't have to do a linear lookup each time
		groupIdToPickAnimationsSetMap = new Dictionary<string, PickAnimationAudioSet>();
		foreach (PickAnimationAudioSet ntam in pickAnimationsSets)
		{
			groupIdToPickAnimationsSetMap.Add(ntam.groupId, ntam);
		}
	}

	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentPickOutcome();
		PickingGameCreditPickItem creditsRevealItem = PickingGameCreditPickItem.getPickingGameCreditsPickItemForType(pickItem.gameObject, PickingGameCreditPickItem.CreditsPickItemType.Default);

		creditsRevealItem.setCreditLabels(currentPick.credits);

		yield return StartCoroutine(base.executeOnItemClick(pickItem));
	}

	public override IEnumerator executeOnRevealPick(PickingGameBasePickItem pickItem)
	{
		ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentPickOutcome();
		PickAnimationAudioSet pickAnimationAudioSet = getPickAnimationSet(currentPick);
		
		pickItem.REVEAL_ANIMATION = pickAnimationAudioSet.REVEAL_ANIMATION_NAME;
		pickItem.REVEAL_ANIMATION_GRAY = pickAnimationAudioSet.REVEAL_GRAY_ANIMATION_NAME;
		
		// play the associated leftover reveal vo
		if (!string.IsNullOrEmpty(pickAnimationAudioSet.REVEAL_VO_AUDIO))
		{
			// play the associated audio voiceover
			Audio.playSoundMapOrSoundKeyWithDelay(pickAnimationAudioSet.REVEAL_VO_AUDIO, pickAnimationAudioSet.REVEAL_VO_DELAY);
		}
		
		// play the associated leftover reveal sound
		if (!string.IsNullOrEmpty(pickAnimationAudioSet.REVEAL_AUDIO))
		{
			// play the associated sound
			Audio.playSoundMapOrSoundKeyWithDelay(pickAnimationAudioSet.REVEAL_AUDIO, pickAnimationAudioSet.REVEAL_AUDIO_DELAY);
		}
		
		yield return StartCoroutine(base.executeOnRevealPick(pickItem));
	}

	public override IEnumerator executeOnRevealLeftover(PickingGameBasePickItem leftover)
	{
		// set the credit value from the leftovers
		ModularChallengeGameOutcomeEntry leftoverOutcome = pickingVariantParent.getCurrentLeftoverOutcome();
		
		PickAnimationAudioSet pickAnimationAudioSet = getPickAnimationSet(leftoverOutcome);

		//set the credits label 
		PickingGameCreditPickItem creditsLeftOver = PickingGameCreditPickItem.getPickingGameCreditsPickItemForType(leftover.gameObject, PickingGameCreditPickItem.CreditsPickItemType.Default);
		creditsLeftOver.setCreditLabels(leftoverOutcome.credits);
		
		//set the animation name
		creditsLeftOver.REVEAL_ANIMATION_GRAY = pickAnimationAudioSet.REVEAL_GRAY_ANIMATION_NAME;
		leftover.REVEAL_ANIMATION_GRAY = pickAnimationAudioSet.REVEAL_GRAY_ANIMATION_NAME;
		
		// play the associated leftover reveal sound
		if (!string.IsNullOrEmpty(pickAnimationAudioSet.REVEAL_AUDIO_GRAY))
		{
			// play the associated audio voiceover
			Audio.playSoundMapOrSoundKey(pickAnimationAudioSet.REVEAL_AUDIO_GRAY);
		}

		yield return StartCoroutine(base.executeOnRevealLeftover(leftover));
	}
	
	private PickAnimationAudioSet getPickAnimationSet(ModularChallengeGameOutcomeEntry currentPick)
	{
		string groupId = currentPick.groupId;

		if (!string.IsNullOrEmpty(groupId) && groupIdToPickAnimationsSetMap.ContainsKey(groupId))
		{
			return groupIdToPickAnimationsSetMap[groupId];
		}
		
		Debug.LogError("Couldn't map animation name to to set for groupId "  + groupId);	
		return null;
	}
}
