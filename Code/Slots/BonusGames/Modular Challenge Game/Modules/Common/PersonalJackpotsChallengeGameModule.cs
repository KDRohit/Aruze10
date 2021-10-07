using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//
// This module is used for progressive scatter symbol jackpots where the player personally
// increases their jackpot totals by spinning the game. This is almost entirely copied from
// ProgressiveScatterJackpotsModule so we can have the same thing in ChallengeGames.
//
// games : bettie02 wheelgame
// Date : Aug 28th, 2019
// Author : Nick Saito <nsaito@zynga.com>
//
public class PersonalJackpotsChallengeGameModule : ChallengeGameModule
{
	[SerializeField] private List<JackpotContainer> jackpotContainers = new List<JackpotContainer>();
	[SerializeField] private bool useMultiplier;

	private long credits;
	private string personalJackpotWonKey;

	public override bool needsToExecuteOnRoundInit()
	{
		return true;
	}

	public override void executeOnRoundInit(ModularChallengeGameVariant round)
	{
		base.executeOnRoundInit(round);

		Dictionary<string, ModularChallengeGameOutcome.PickPersonalJackpotInfo> personalJackpotInfo = round.outcome.pickPersonalJackpots;
		if (personalJackpotInfo != null)
		{
			foreach (var kvp in personalJackpotInfo)
			{
				credits = kvp.Value.credits;
				personalJackpotWonKey = kvp.Key;
				// this module only supports one jackpot, break after finding one
				break;
			}
		}

		ReevaluationPersonalJackpotReevaluator personalJackpotReeval = getPersonalJackpotReeval();

		if (personalJackpotReeval == null)
		{
			return;
		}

		foreach (ReevaluationPersonalJackpotReevaluator.PersonalJackpot personalJackpot in personalJackpotReeval.personalJackpotList)
		{
			JackpotContainer jackpotContainer = getJackpotContainerWithKey(personalJackpot.name);

			if (jackpotContainer != null)
			{
				initializeJackpotContainer(jackpotContainer, personalJackpot);
			}
		}
	}

	public override bool needsToExecuteOnRoundEnd(bool isEndOfGame)
	{
		// If personal_jackpot_outcome is not present in the suboutcomes for ModularChallengeGameOutcome,
		// then credits will definitely be 0, so this is a reasonable check to make.
		if (isEndOfGame && credits > 0)
		{
			return true;
		}

		return false;
	}

	public override IEnumerator executeOnRoundEnd(bool isEndOfGame)
	{
		JackpotContainer jackpotContainer = getJackpotContainerWithKey(personalJackpotWonKey);
		BonusGamePresenter.instance.useMultiplier = useMultiplier;
		long startValue = BonusGamePresenter.instance.currentPayout;
		long endValue = startValue + credits;
		yield return StartCoroutine(rollupCredits(jackpotContainer.rollupAnimations, jackpotContainer.rollupFinishedAnimations, startValue, endValue, addCredits:true, rollupSoundLoopOverride:jackpotContainer.rollupLoopSoundOverride, rollupSoundEndOverride:jackpotContainer.rollupEndSoundOverride));
	}

	// Get personal jackpot data from reevaluations.
	private ReevaluationPersonalJackpotReevaluator getPersonalJackpotReeval()
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

	// Set the values of the personal jackpots
	private void initializeJackpotContainer(JackpotContainer jackpotContainer,
		ReevaluationPersonalJackpotReevaluator.PersonalJackpot personalJackpot)
	{
		jackpotContainer.personalContributionAmount = personalJackpot.contributionAmount;
		jackpotContainer.personalContributionBalance = personalJackpot.contributionBalance;
		jackpotContainer.baseJackpotPayout = personalJackpot.basePayout;

		if (jackpotContainer.jackpotLabel != null)
		{
			long totalProgressiveAmount = (jackpotContainer.baseJackpotPayout * ReelGame.activeGame.multiplier) + jackpotContainer.personalContributionBalance;
			if (jackpotContainer.isAbbreviatingJackpotLabel)
			{
				jackpotContainer.jackpotLabel.text = CreditsEconomy.multiplyAndFormatNumberAbbreviated(totalProgressiveAmount);
			}
			else
			{
				jackpotContainer.jackpotLabel.text = CreditsEconomy.convertCredits(totalProgressiveAmount);
			}
		}
	}

	private JackpotContainer getJackpotContainerWithKey(string jackpotKey)
	{
		foreach (JackpotContainer jackpotContainer in jackpotContainers)
		{
			if (jackpotContainer.jackpotKeyValue == jackpotKey)
			{
				return jackpotContainer;
			}
		}

		return null;
	}

	// Container for jackpot names and labels so they can be set when the game starts.
	[System.Serializable]
	private class JackpotContainer
	{
		//We will want a way to match mutation data since the jackpots may not come down in order or with numerically parsable keys in the future
		public string jackpotKeyValue = "";

		//The label that holds current value of the jackpot
		public LabelWrapperComponent jackpotLabel;

		public AnimationListController.AnimationInformationList rollupAnimations;
		public AnimationListController.AnimationInformationList rollupFinishedAnimations;
		public string rollupLoopSoundOverride;
		public string rollupEndSoundOverride;
		[Tooltip("Tells if the jackpotLabel should be displayed abbreviated")]
		public bool isAbbreviatingJackpotLabel;

		//Data variables to be updated on each spin and on the initial game load
		[HideInInspector] public long baseJackpotPayout;
		[HideInInspector] public long personalContributionAmount;
		[HideInInspector] public long personalContributionBalance;
	}
}


