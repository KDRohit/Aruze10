using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Module to handle revealing grouped symbols for matching in a round
 */
public class PickingGameMatchGroupModule : PickingGameRevealModule 
{
	[SerializeField] protected bool AWARD_EACH_CREDIT = false; // if true, every credit revealed will be awarded (not just matches)
	[SerializeField] protected bool shouldDisplayAbbreviatedCredits = false; // Whether or not to display the values of credits as abbreviated format
	[SerializeField] protected bool isFormattingJackpotLabels = false;
	[Tooltip("if true, jackpot credits will only rolling up once.")]
	[SerializeField] protected bool SHOULD_ROLLUP_JACKPOT_CREDITS_TOGETHER = false; // if true, jackpot credits will only rolling up once.
	[SerializeField] protected float REVEAL_AUDIO_DELAY = 0.0f; // delay before playing the reveal audio clip
	[SerializeField] protected float REVEAL_VO_DELAY = 0.2f; // delay before playing the VO clip
	[SerializeField] protected string REVEAL_LEFTOVER_AUDIO = "reveal_not_chosen";
	[SerializeField] protected string REVEAL_MATCH_AUDIO = "pickem_match_made";
	[SerializeField] protected float REVEAL_ROLLUP_DELAY = 0f; // amount of time to delay before rollup after match
	[SerializeField] protected float REVEAL_ANIMAITON_LENGTH_OVERRIDE = -1.0f; // allows the time that the game is locked during an animation to be shortened

	private BuiltInProgressiveJackpotBaseGameModule.BuiltInProgressiveJackpotTierData currentJackpotTierData;
	[SerializeField] private bool useCurrentTierDataForGroupAnimation = true;
	[SerializeField] private string groupAnimationTierReplaceKey;
	[SerializeField] private string groupAnimationTierGrayReplaceKey;
	[SerializeField] private int[] tierRemapping; //used to invert the tier count for animation name order inconsistencies

	// support for mapping per-group animations
	[SerializeField] protected List<PickingGamePickGroupHelper.PickGroupAnimationInfoList> groupAnimationReveals;
	[SerializeField] protected List<PickingGamePickGroupHelper.PickGroupAnimationInfoList> groupAnimationGreyReveals;
	[SerializeField] protected List<PickingGamePickGroupHelper.PickGroupAnimationInfoList> groupAnimationMatchReveals;

	// support for mapping per-group audio keys & VO 
	[SerializeField] protected List<PickingGamePickGroupHelper.PickGroupAnimationInfoList> groupAudioReveals;
	[SerializeField] protected List<PickingGamePickGroupHelper.PickGroupAnimationInfoList> groupAudioVOReveals;

	[SerializeField] protected AnimationListController.AnimationInformationList multiplierBoxAnims;

	// jackpot win information, for versions of this type of game where there are jackpot win indicators and celebration animations after you finish matching
	[SerializeField] protected List<GroupJackpotElements> groupJackpotData;
	
	// This is used for when the animation name to be played isn't set ahead of time, but must be computed based on bet teir data
	[Header("Dynamic Jackpot Animation Name Replacement")] 
	[SerializeField] private string jackpotPipAcquiredAnimationStateName;
	[SerializeField] private string jackpotWinCelebrationAnimationStateName;
	[SerializeField] private List<string> jackpotWinCelebrationAnimationStateNameExclusion;
	[SerializeField] private string jackpotAnimationStateNameReplacementToken;
	[SerializeField] private string jackpotGroupNameToRemap;
	[SerializeField] private ProgressiveJackpotLinkLabels jackpotLinkLabels;

	[Header("Particle Animation for pick to box")] 
	[SerializeField] protected List<AnimatedParticleEffect> pickToEndPointAnimatedParticles;
	[SerializeField] protected List<Transform> pickParticleEndpoints;

	[Tooltip("Allows the BonusGamePresenter multiplier value to be changed if this game doesn't use multiplied values on the summary screen. -1 is default (no override). 1 is no multiplier")]
	[SerializeField] private int bonusGamePresenterBetMultiplierOverride = -1; 
	
	// class for defining jackpot labels, pip animations, and win celebration animations
	[System.Serializable]
	protected class GroupJackpotElements
	{
		public string groupName;
		public AnimationListController.AnimationInformationList jackpotWinCelebrationAnims;
		public AnimationListController.AnimationInformationList jackpotWinCelebrationOutroAnims;
		public List<AnimationListController.AnimationInformationList> pipsAcquiredAnimsList;
		public List<GroupJackpotHitCountSpecificPipAcquiredSounds> hitCountSpecificPipsAcquiredSounds; // special sounds that play with the pips acquired anims that are based on the hit count
		public List<LabelWrapperComponent> jackpotLabels; // may be more than one label, for instance the jackpot indicator might have one and the celebration banner might have a different one
	}

	[System.Serializable]
	protected class GroupJackpotHitCountSpecificPipAcquiredSounds
	{
		public int hitCount = 1;
		public AudioListController.AudioInformationList pipAcquiredSounds;
	}

	protected Dictionary<string, PickGroupState> groupMatchData = new Dictionary<string, PickGroupState>();

	// class for tracking status of matched pick group items
	protected class PickGroupState
	{
		public int hitCount = 0;
		public List<PickingGameBasePickItem> revealedMatchItems = new List<PickingGameBasePickItem>();
		public List<string> matchCelebrationAnimations = new List<string>(); // matching list to revealedMatchItems, that contains the celebration animation names for those items, since they may be different if there are items worth different number of pips
	}

	public override bool needsToExecuteOnRoundInit()
	{
		return true;
	}

	// Overrides HAVE TO call base.executeOnRoundInit!
	public override void executeOnRoundInit(ModularChallengeGameVariant round)
	{
		base.executeOnRoundInit(round);
		
		// fill in the jackpot value texts if this game is using them
		for (int i = 0; i < groupJackpotData.Count; i++)
		{
			GroupJackpotElements groupElements = groupJackpotData[i];

			if (groupElements.jackpotLabels != null && groupElements.jackpotLabels.Count > 0)
			{
				// retrieve the group data from the stored paytable info so we can find out the credits the group is worth
				ModularChallengeGameOutcome.PickGroupInfo currentPickGroup = roundVariantParent.outcome.getPickInfoForGroup(groupElements.groupName);
				long credits = currentPickGroup.credits * roundVariantParent.gameParent.currentMultiplier;
				for (int k = 0; k < groupElements.jackpotLabels.Count; k++)
				{
					if (groupElements.jackpotLabels[k] != null)
					{
						if (roundVariantParent.useMultipliedCreditValues)
						{
							groupElements.jackpotLabels[k].text = formatCredits(credits * BonusGameManager.instance.currentMultiplier);
						}
						else
						{
							groupElements.jackpotLabels[k].text = formatCredits(credits);
						}
					}
				}
			}

			//default is -1. First used in Zynga06. Pickig game doesn't use post-multiplied values, but the wheel games does and
			//bonus game presenter is set to "Use Multiplier"
			if (bonusGamePresenterBetMultiplierOverride > 0)
			{
				BonusGameManager.instance.betMultiplierOverride = bonusGamePresenterBetMultiplierOverride;
			}
		}

		if (!useCurrentTierDataForGroupAnimation)
		{
			return;
		}
		
		currentJackpotTierData = BuiltInProgressiveJackpotBaseGameModule.getCurrentTierData();

		if (currentJackpotTierData == null)
		{
			Debug.LogWarning("Jackpot Tier Data is null");
		}

		foreach (GroupJackpotElements groupJackpotElements in groupJackpotData)
		{
			if (groupJackpotElements.groupName != jackpotGroupNameToRemap || currentJackpotTierData == null)
			{
				continue;
			}
			
			int tierNum = tierRemapping.Length > 0 ? tierRemapping[currentJackpotTierData.progressiveTierNumber - 1] : currentJackpotTierData.progressiveTierNumber;
			
			//only do the dynamic name replacement if there is a state name and token value set to do replacing
			if (!string.IsNullOrEmpty(jackpotPipAcquiredAnimationStateName) && !string.IsNullOrEmpty(jackpotAnimationStateNameReplacementToken))
			{
				foreach (AnimationListController.AnimationInformationList animList in groupJackpotElements.pipsAcquiredAnimsList)
				{
					foreach (AnimationListController.AnimationInformation anim in animList.animInfoList)
					{
						anim.ANIMATION_NAME = jackpotPipAcquiredAnimationStateName.Replace(jackpotAnimationStateNameReplacementToken, tierNum.ToString());
					}
				}
			}
			
			if (!string.IsNullOrEmpty(jackpotWinCelebrationAnimationStateName) && !string.IsNullOrEmpty(jackpotAnimationStateNameReplacementToken))
			{
				foreach (AnimationListController.AnimationInformation animInformation in groupJackpotElements.jackpotWinCelebrationAnims.animInfoList)
				{
					string newAnimationName = jackpotWinCelebrationAnimationStateName.Replace(jackpotAnimationStateNameReplacementToken, tierNum.ToString());

					if(jackpotWinCelebrationAnimationStateNameExclusion == null || !jackpotWinCelebrationAnimationStateNameExclusion.Contains(animInformation.ANIMATION_NAME))
					{ 
						animInformation.ANIMATION_NAME = newAnimationName;
					}
				}
			}

			break;
		}
	}

	private string formatCredits(long credits)
	{
		string creditText = "";
		//Determine how the text looks
		if (shouldDisplayAbbreviatedCredits)
		{
			creditText = CommonText.formatNumberAbbreviated(CreditsEconomy.multipliedCredits(credits));
		}
		else
		{
			creditText = CreditsEconomy.convertCredits(credits, isFormattingJackpotLabels);
		}
		return creditText;
	}
	
	protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		return pickData != null && !string.IsNullOrEmpty(pickData.newBaseBonusGamePickGroupId);
	}

	protected PickGroupState getCurrentGroupState(string keyName)
	{
		if (!groupMatchData.ContainsKey(keyName))
		{
			groupMatchData.Add(keyName, new PickGroupState());
		}

		return groupMatchData[keyName];
	}

	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		ModularChallengeGameOutcomeEntry currentPickOutcome = pickingVariantParent.getCurrentPickOutcome();
		ModularChallengeGameOutcome.PickGroupInfo currentPickGroup = roundVariantParent.outcome.getPickInfoForGroup(currentPickOutcome.newBaseBonusGamePickGroupId);

		long credits = calculateCredits(currentPickGroup);
		updateCreditLabels(pickItem, credits);
		
		//this sets the payout from the ProgressiveJackpot value pool because we won the jackpot associated
		//with the currently selected jackpot tier
		if (currentJackpotTierData != null && currentPickGroup.credits == 0 && currentPickOutcome.groupId == jackpotGroupNameToRemap)
		{
			JSON pjp = currentPickOutcome.newBaseBonusGamePick.pjp;

			if (pjp != null)
			{
				credits = pjp.getLong("running_total", 0);
				jackpotLinkLabels.setValueAndUnlink(CreditsEconomy.convertCredits(credits, isFormattingJackpotLabels));
				ProgressiveJackpot jackpot = ProgressiveJackpot.find(currentJackpotTierData.getProgressiveKeyName());
				jackpot.reset();
			}
		}
		
		// Play the animations and sounds for the item the player picked.
		setPickItemRevealAnimation(pickItem, currentPickOutcome, currentPickOutcome.groupId);
		playRevealSounds(currentPickOutcome, currentPickOutcome.groupId);
		yield return StartCoroutine(base.executeOnItemClick(pickItem));
				
		yield return StartCoroutine(animatePickParticleTrails(pickItem));

		// retried or add the current group state
		PickGroupState currentPickGroupState = getCurrentGroupState(currentPickOutcome.newBaseBonusGamePickGroupId);
		
		// animate jackpot and pips progress
		yield return StartCoroutine(animateJackpotProgress(currentPickOutcome.newBaseBonusGamePickGroupId, currentPickOutcome.newBaseBonusGameGroupHits, currentPickGroupState));
		
		// update our match count
		currentPickGroupState.hitCount += currentPickOutcome.newBaseBonusGameGroupHits;

		// store the item for later match animation
		storeMatchedItemForGroup(pickItem, currentPickGroupState, currentPickOutcome.newBaseBonusGamePickGroupId, currentPickOutcome.newBaseBonusGameGroupHits);

		// test for enough matches
		if (currentPickGroupState.hitCount >= currentPickGroup.hitsNeeded)
		{
			// animate the previously matched items
			StartCoroutine(animateMatchedItems(currentPickGroupState));
			yield return StartCoroutine(playRevealMatchAudioAndWait());
			yield return StartCoroutine(animateJackpotWin(currentPickOutcome.newBaseBonusGamePickGroupId));
			yield return StartCoroutine(base.rollupCredits(credits));
			yield return StartCoroutine(animateMultiplierBoxes());
			yield return StartCoroutine(animateMultipierParticleTrails(pickItem));
			yield return StartCoroutine(rollupCredits(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout * roundVariantParent.gameParent.currentMultiplier));
		}
		else
		{
			yield return StartCoroutine(awardCredits(credits));
		}
	}

	// custom per-group / symbol reveal audio
	protected virtual void playRevealSounds(ModularChallengeGameOutcomeEntry outcome, string groupName)
	{
		string groupRevealAudio = PickingGamePickGroupHelper.getAudioKeyForGroupInList(groupName, outcome.newBaseBonusGameGroupHits, groupAudioReveals);
		if (!string.IsNullOrEmpty(groupRevealAudio))
		{
			Audio.playSoundMapOrSoundKeyWithDelay(groupRevealAudio, REVEAL_AUDIO_DELAY);
		}

		string groupRevealVO = PickingGamePickGroupHelper.getAudioKeyForGroupInList(groupName, outcome.newBaseBonusGameGroupHits, groupAudioVOReveals);
		if (!string.IsNullOrEmpty(groupRevealVO))
		{
			Audio.playSoundMapOrSoundKeyWithDelay(groupRevealVO, REVEAL_VO_DELAY);
		}
	}
	
	// Set the appropriate group animation if required
	// and the reveal anim length override if we want to have stuff happen sooner than waiting for the whole animation
	protected virtual void setPickItemRevealAnimation(PickingGameBasePickItem pickItem, ModularChallengeGameOutcomeEntry outcome, string groupName)
	{
		pickItem.REVEAL_ANIMATION = PickingGamePickGroupHelper.getAnimationNameForGroupInList(groupName, outcome.newBaseBonusGameGroupHits, groupAnimationReveals);

		if (currentJackpotTierData != null && pickItem.REVEAL_ANIMATION == groupAnimationTierReplaceKey)
		{
			int tierNum = tierRemapping.Length > 0 ? tierRemapping[currentJackpotTierData.progressiveTierNumber - 1] : currentJackpotTierData.progressiveTierNumber;
			pickItem.REVEAL_ANIMATION = groupAnimationTierReplaceKey + tierNum;
		}
		
		pickItem.REVEAL_ANIM_OVERRIDE_DUR = REVEAL_ANIMAITON_LENGTH_OVERRIDE;
	}

	protected virtual long calculateCredits(ModularChallengeGameOutcome.PickGroupInfo currentPickGroup)
	{
		// get the credit value for this group
		long credits = currentPickGroup.credits;

		if (roundVariantParent.useMultipliedCreditValues)
		{
			credits *= BonusGameManager.instance.currentMultiplier;
		}

		return credits;
	}

	protected virtual void updateCreditLabels(PickingGameBasePickItem pickItem, long credits)
	{
		//set the credit value within the item and the reveal animation
		PickingGameCreditPickItem creditsRevealItem = PickingGameCreditPickItem.getPickingGameCreditsPickItemForType(pickItem.gameObject, PickingGameCreditPickItem.CreditsPickItemType.Default);

		// set the credit amount on the pick item if this game shows values that way
		if (creditsRevealItem != null)
		{
			creditsRevealItem.setCreditLabels(credits);
		}
	}

	protected IEnumerator animateJackpotProgress(string groupId, int hits, PickGroupState currentPickGroupState)
	{
		// prior to updating the match count update pips if those are setup
		// note that making your animation list will cause these to be staggered
		GroupJackpotElements jackpotData = getGroupJackpotElementsForGroup(groupId);
		if (jackpotData != null && jackpotData.pipsAcquiredAnimsList != null && jackpotData.pipsAcquiredAnimsList.Count > 0)
		{
			// play the special hit count specific pips acquired sound if that is defined
			if (jackpotData.hitCountSpecificPipsAcquiredSounds != null
				&& jackpotData.hitCountSpecificPipsAcquiredSounds.Count > 0)
			{
				// search for the correct sound to play
				for (int i = 0; i < jackpotData.hitCountSpecificPipsAcquiredSounds.Count; i++)
				{
					GroupJackpotHitCountSpecificPipAcquiredSounds pipAcquiredSoundData = jackpotData.hitCountSpecificPipsAcquiredSounds[i];
					if (pipAcquiredSoundData.hitCount == hits)
					{
						yield return StartCoroutine(AudioListController.playListOfAudioInformation(pipAcquiredSoundData.pipAcquiredSounds));
					}
				}
			}

			List<AnimationListController.AnimationInformationList> pipAnimList = jackpotData.pipsAcquiredAnimsList;

			// loop for the number of group hits awarded by the reveal
			for (int i = currentPickGroupState.hitCount; i < currentPickGroupState.hitCount + hits; i++)
			{
				if (i < pipAnimList.Count && pipAnimList[i] != null && pipAnimList[i].Count > 0)
				{
					yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(pipAnimList[i]));
				}
			}
		}
	}

	// Store the item for later match animation
	// This keeps a list of which pickItems contributed to the final jackpot.
	protected virtual void storeMatchedItemForGroup(PickingGameBasePickItem pickItem, PickGroupState currentPickGroupState, string groupId, int hits) {
		currentPickGroupState.revealedMatchItems.Add(pickItem);
		string matchAnimation = PickingGamePickGroupHelper.getAnimationNameForGroupInList(groupId, hits, groupAnimationMatchReveals);
		currentPickGroupState.matchCelebrationAnimations.Add(matchAnimation);
	}

	// animate the previously matched items
	protected IEnumerator animateMatchedItems(PickGroupState currentPickGroupState)
	{
		for (int i = 0; i < currentPickGroupState.revealedMatchItems.Count; i++)
		{
			PickingGameBasePickItem item = currentPickGroupState.revealedMatchItems[i];
			string currentMatchAnimation = currentPickGroupState.matchCelebrationAnimations[i];
			if (!string.IsNullOrEmpty(currentMatchAnimation))
			{
				yield return StartCoroutine(CommonAnimation.playAnimAndWait(item.pickAnimator, currentMatchAnimation));
			}
		}
	}

	protected IEnumerator playRevealMatchAudioAndWait()
	{
		if (!string.IsNullOrEmpty(REVEAL_MATCH_AUDIO))
		{
			// play the associated audio for a match
			Audio.playSoundMapOrSoundKey(REVEAL_MATCH_AUDIO);
		}

		// Add a delay before playing the jackpot celebration and rolling up if needed
		yield return new TIWaitForSeconds(REVEAL_ROLLUP_DELAY);
	}

	// if we have jackpot win celebration animations defined, we should go ahead and play them
	protected virtual IEnumerator animateJackpotWin(string groupId)
	{
		GroupJackpotElements jackpotData = getGroupJackpotElementsForGroup(groupId);
		if (jackpotData != null && jackpotData.jackpotWinCelebrationAnims != null && jackpotData.jackpotWinCelebrationAnims.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotData.jackpotWinCelebrationAnims));
		}
	}

	protected virtual IEnumerator animateJackpotWinOutro(string groupId)
	{
		GroupJackpotElements jackpotData = getGroupJackpotElementsForGroup(groupId);
		if (jackpotData != null && jackpotData.jackpotWinCelebrationOutroAnims != null && jackpotData.jackpotWinCelebrationOutroAnims.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotData.jackpotWinCelebrationOutroAnims));
		}
	}

	protected virtual IEnumerator animateMultiplierBoxes()
	{
		// perform the multiplier additions
		if (multiplierBoxAnims != null && multiplierBoxAnims.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(multiplierBoxAnims));
		}
	}

	protected virtual IEnumerator animateMultipierParticleTrails(PickingGameBasePickItem pickItem)
	{
		// sparkle trail from multiplier to win box, if this game is doing multiplier stuff
		ParticleTrailController particleTrailController = null;
		if (roundVariantParent.multiplierLabel != null)
		{
			particleTrailController = ParticleTrailController.getParticleTrailControllerForType(roundVariantParent.multiplierLabel.gameObject, ParticleTrailController.ParticleTrailControllerType.Multiplier);
		}

		// If the multiplier label didn't have a particle trail, then get it from the pickem.
		if (particleTrailController == null)
		{
			particleTrailController = ParticleTrailController.getParticleTrailControllerForType(pickItem.gameObject, ParticleTrailController.ParticleTrailControllerType.Multiplier);
		}

		if (particleTrailController != null)
		{
			yield return StartCoroutine(particleTrailController.animateParticleTrail(roundVariantParent.multiplierLabel.gameObject.transform.position, roundVariantParent.winLabel.gameObject.transform.position, roundVariantParent.gameObject.transform));
		}
	}
	
	protected IEnumerator animatePickParticleTrails(PickingGameBasePickItem pickItem)
	{
		AnimatedParticleEffect animatedParticleEffect = null;
		Transform particleEndpoint = null;
		for (int i = 0; i < pickToEndPointAnimatedParticles.Count; i++)
		{
			if (pickToEndPointAnimatedParticles[i].name == pickItem.REVEAL_ANIMATION)
			{
				animatedParticleEffect = pickToEndPointAnimatedParticles[i];
				particleEndpoint = pickParticleEndpoints[i];
				
				break;
			}
		}
		
		if (animatedParticleEffect != null)
		{
			yield return StartCoroutine(animatedParticleEffect.animateParticleEffect(pickItem.transform, particleEndpoint));
		}
	}

	// award every credit if desired
	protected virtual IEnumerator awardCredits(long credits)
	{
		if (AWARD_EACH_CREDIT)
		{
			yield return StartCoroutine(base.rollupCredits(credits));
		}
	}

	public override IEnumerator executeOnRevealLeftover(PickingGameBasePickItem leftover)
	{
		// set the credit value from the leftovers
		ModularChallengeGameOutcomeEntry leftoverOutcome = pickingVariantParent.getCurrentLeftoverOutcome();
		ModularChallengeGameOutcome.PickGroupInfo currentPickGroup = roundVariantParent.outcome.getPickInfoForGroup(leftoverOutcome.groupId);
		yield return StartCoroutine(executeOnRevealLeftoverWithGroupName(leftover, leftoverOutcome.groupId));
	}
	
	

	protected virtual IEnumerator executeOnRevealLeftoverWithGroupName(PickingGameBasePickItem leftover, string groupName) 
	{
		// set the credit value from the leftovers
		ModularChallengeGameOutcomeEntry leftoverOutcome = pickingVariantParent.getCurrentLeftoverOutcome();

		PickingGameCreditPickItem creditsLeftOver = PickingGameCreditPickItem.getPickingGameCreditsPickItemForType(leftover.gameObject, PickingGameCreditPickItem.CreditsPickItemType.Default);

		ModularChallengeGameOutcome.PickGroupInfo currentPickGroup = roundVariantParent.outcome.getPickInfoForGroup(groupName);

		if (leftoverOutcome != null)
		{
			if (creditsLeftOver != null)
			{
				// get the credit value for this group
				long credits = currentPickGroup.credits;

				if (roundVariantParent.useMultipliedCreditValues)
				{
					credits *= BonusGameManager.instance.currentMultiplier;
				}
				creditsLeftOver.setCreditLabels(credits);
			}
		}
			
		// play the associated leftover reveal sound
		Audio.playSoundMapOrSoundKey(REVEAL_LEFTOVER_AUDIO);

		// play the appropriate group animation if required
		leftover.REVEAL_ANIMATION_GRAY = PickingGamePickGroupHelper.getAnimationNameForGroupInList(groupName, leftoverOutcome.newBaseBonusGameGroupHits, groupAnimationGreyReveals);
		
		if (currentJackpotTierData != null && leftover.REVEAL_ANIMATION_GRAY == groupAnimationTierGrayReplaceKey)
		{
			int tierNum = tierRemapping.Length > 0 ? tierRemapping[currentJackpotTierData.progressiveTierNumber - 1] : currentJackpotTierData.progressiveTierNumber; 
			leftover.REVEAL_ANIMATION_GRAY = groupAnimationTierReplaceKey + tierNum + " Gray";
		}

		yield return StartCoroutine(base.executeOnRevealLeftover(leftover));
	}

	// Grab the specific group jackpot (animation and text field) data for a passed in group
	private GroupJackpotElements getGroupJackpotElementsForGroup(string groupName)
	{
		for (int i = 0; i < groupJackpotData.Count; i++)
		{
			if (groupJackpotData[i].groupName == groupName)
			{
				return groupJackpotData[i];
			}
		}

		return null;
	}

	// Function added for a helper module for switching the music key when a group has 1 pick remaining
	public bool hasGroupWithHitsRemaining(int numberHitsRemaining)
	{
		foreach (KeyValuePair<string, PickGroupState> groupStatePair in groupMatchData)
		{
			// retrieve the group data from the stored paytable info
			ModularChallengeGameOutcome.PickGroupInfo currentPickGroup = roundVariantParent.outcome.getPickInfoForGroup(groupStatePair.Key);
			if (currentPickGroup.hitsNeeded - groupStatePair.Value.hitCount == numberHitsRemaining)
			{
				return true;
			}
		}

		return false;
	}
}
