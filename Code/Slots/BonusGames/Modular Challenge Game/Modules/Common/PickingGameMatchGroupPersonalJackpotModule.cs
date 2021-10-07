using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * PickingGameRevealModule that handles awarding personal jackpot credits from a selected item. Links jackpot animations
 * to groupId instead of jackpot key because personal jackpot data isn't included for unselected items.
 * First used in gen93 Meet the Bigfoots Pick Game
 * Author: Caroline 02/2020
 */
public class PickingGameMatchGroupPersonalJackpotModule : PickingGameRevealModule
{
	[Serializable]
	private class GroupPersonalJackpotData
	{
		[Tooltip("The pick group id")]
		public string groupId = "";
		[Tooltip("The jackpot key that the pick group maps to, replaces {0} with the game key")]
		public string jackpotKey = "";

		[Tooltip("Jackpot amount label")]
		public LabelWrapperComponent jackpotLabel;
		
		[Tooltip("Tells if the jackpotLabel should be displayed abbreviated")]
		public bool isAbbreviatingJackpotLabel;
		
		// jackpot won animations
		[Tooltip("Jackpot animation to play on symbol revealed. Overrides rollupAnimations field")]
		public AnimationListController.AnimationInformationList jackpotRollupAnimations = null;
		[Tooltip("Jackpot animation end after rollup complete. Overrides rollupFinishedAnimations field")]
		public AnimationListController.AnimationInformationList jackpotRollupEndAnimations = null;

		// pick item reveal audio
		[Tooltip("Audio to play on jackpot symbol picked reveal")]
		public AudioListController.AudioInformationList jackpotRevealAudio;
		[Tooltip("Jackpot rollup override sound key")]
		public string jackpotRollupLoopSoundOverride;
		[Tooltip("Jackpot rollup end override sound key")]
		public string jackpotRollupEndSoundOverride;
		[Tooltip("Audio to play on jackpot symbol leftover reveal")]
		public AudioListController.AudioInformationList jackpotLeftoverRevealAudio = null;
		
		// pick item reveal animations
		[Tooltip("Pick item reveal animation")]
		public string REVEAL_ANIMATION_NAME = "revealJackpot";
		[Tooltip("Pick item reveal leftover animation")]
		public string REVEAL_GRAY_ANIMATION_NAME = "revealJackpotGray";

		[NonSerialized] public long jackpotCredits;
	}
	
	[Tooltip("Link groupIds to jackpot animation data")]
	[SerializeField] private List<GroupPersonalJackpotData> groupJackpotAnimationData;

	// map groups for quick lookup
	private Dictionary<string, GroupPersonalJackpotData> groupJackpotAnimationDataDictionary = new Dictionary<string,GroupPersonalJackpotData>();
	
	public override bool needsToExecuteOnRoundInit()
	{
		return true;
	}

	public override void executeOnRoundInit(ModularChallengeGameVariant round)
	{
		base.executeOnRoundInit(round);
		Dictionary<string, ModularChallengeGameOutcome.PickPersonalJackpotInfo> personalJackpotInfo = round.outcome.pickPersonalJackpots;
		ReevaluationPersonalJackpotReevaluator personaJackpotReevaluator = getPersonalJackpotReevaluator();

		foreach (GroupPersonalJackpotData group in groupJackpotAnimationData)
		{
			if (!groupJackpotAnimationDataDictionary.ContainsKey(group.groupId))
			{
				groupJackpotAnimationDataDictionary[group.groupId] = group;
			}

			// add game key to jackpot name
			group.jackpotKey = string.Format(group.jackpotKey, GameState.game.keyName);
			if (personalJackpotInfo.ContainsKey(group.jackpotKey))
			{
				// save out jackpot value
				group.jackpotCredits = personalJackpotInfo[group.jackpotKey].credits;
			}

			if (group.jackpotLabel != null)
			{
				if (group.jackpotCredits > 0)
				{
					// use group data as source of truth since reevaluator has post-reset amount
					if (group.isAbbreviatingJackpotLabel)
					{
						group.jackpotLabel.text = CreditsEconomy.multiplyAndFormatNumberAbbreviated(group.jackpotCredits);
					}
					else
					{
						group.jackpotLabel.text = CreditsEconomy.convertCredits(group.jackpotCredits);
					}
				}
				else
				{
					// jackpot not awarded in this game, use reevaluator data for labels
					foreach (ReevaluationPersonalJackpotReevaluator.PersonalJackpot reevaluatorJackpot in personaJackpotReevaluator.personalJackpotList)
					{
						if (reevaluatorJackpot.name.Equals(group.jackpotKey))
						{
							long jackpotCredits = reevaluatorJackpot.basePayout * ReelGame.activeGame.multiplier + reevaluatorJackpot.contributionBalance;
							if (group.isAbbreviatingJackpotLabel)
							{
								group.jackpotLabel.text = CreditsEconomy.multiplyAndFormatNumberAbbreviated(jackpotCredits);
							}
							else
							{
								group.jackpotLabel.text = CreditsEconomy.convertCredits(jackpotCredits);
							}
							// jackpot label populated, can break
							break;
						}
					}
				}
			}
		}
	}
	
	protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		return pickData != null && !string.IsNullOrEmpty(pickData.groupId) && groupJackpotAnimationDataDictionary.ContainsKey(pickData.groupId);
	}

	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentPickOutcome();
		GroupPersonalJackpotData jackpotData = getJackpotAnimationDataForGroup(currentPick.groupId);
		if (jackpotData != null)
		{
			// play the associated reveal sound
			yield return StartCoroutine(AudioListController.playListOfAudioInformation(jackpotData.jackpotRevealAudio));
			pickItem.REVEAL_ANIMATION = jackpotData.REVEAL_ANIMATION_NAME;
		}

		yield return StartCoroutine(base.executeOnItemClick(pickItem));
		
		// get jackpot credits value
		long credits = 0;
		if (jackpotData != null)
		{
			credits = jackpotData.jackpotCredits;
		}

		if (credits > 0)
		{
			// play jackpot won animation and rollup winnings
			if (jackpotData != null)
			{
				yield return StartCoroutine(rollupCredits(jackpotData.jackpotRollupAnimations,
					jackpotData.jackpotRollupEndAnimations, BonusGamePresenter.instance.currentPayout, 
					BonusGamePresenter.instance.currentPayout + credits, true, 
					jackpotData.jackpotRollupLoopSoundOverride, jackpotData.jackpotRollupEndSoundOverride));
			}
			else
			{
				yield return StartCoroutine(rollupCredits(credits));
			}
		}
		else
		{
			Debug.LogError("Picking Game Personal Jackpot item had credits value of 0");
		}
	}
	
	public override IEnumerator executeOnRevealLeftover(PickingGameBasePickItem leftover)
	{
		// set the credit value from the leftovers
		ModularChallengeGameOutcomeEntry leftoverOutcome = pickingVariantParent.getCurrentLeftoverOutcome();
		if (leftoverOutcome != null)
		{
			GroupPersonalJackpotData jackpotAnimationData = getJackpotAnimationDataForGroup(leftoverOutcome.groupId);
			if (jackpotAnimationData != null)
			{
				leftover.REVEAL_ANIMATION_GRAY = jackpotAnimationData.REVEAL_GRAY_ANIMATION_NAME;
				// play the associated leftover reveal sound
				yield return StartCoroutine(AudioListController.playListOfAudioInformation(jackpotAnimationData.jackpotLeftoverRevealAudio));
			}
		}

		yield return StartCoroutine(base.executeOnRevealLeftover(leftover));
	}

	private GroupPersonalJackpotData getJackpotAnimationDataForGroup(string groupName)
	{
		if (groupJackpotAnimationDataDictionary.ContainsKey(groupName))
		{
			return groupJackpotAnimationDataDictionary[groupName];
		}

		return null;
	}

	private ReevaluationPersonalJackpotReevaluator getPersonalJackpotReevaluator()
	{
		JSON[] arrayReevaluations = ReelGame.activeGame.outcome.getArrayReevaluations();
		for (int i = 0; i < arrayReevaluations.Length; i++)
		{
			string reevalType = arrayReevaluations[i].getString("type", "");
			if (reevalType == "personal_jackpot_reevaluator")
			{
				return new ReevaluationPersonalJackpotReevaluator(arrayReevaluations[i]);
			}
		}

		return null;
	}
	
}