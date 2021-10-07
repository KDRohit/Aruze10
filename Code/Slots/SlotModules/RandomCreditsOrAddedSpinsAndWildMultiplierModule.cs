using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Module created originally for gen88 Sweet Souls.  This module triggers an animation sequence
 * which can award credits or in freespins additional spins and increased WD symbol multiplier amounts.
 *
 * Original Author: Scott Lepthien
 * Creation Date: 6/10/2019
 */
public class RandomCreditsOrAddedSpinsAndWildMultiplierModule : SlotModule
{
	[Header("Credits Win")]
	[SerializeField] private AnimationListController.AnimationInformationList creditRewardAnims;
	[SerializeField] private LabelWrapperComponent creditValueLabel;
	[SerializeField] private bool isDisplayingAbbreviatedCreditValue = true;
	[Tooltip("Adds a delay before the credit rollup start so that it can be synced with animations in creditRewardAnims.")]
	[SerializeField] private float delayBeforeCreditRollupStart = 0.0f;

	[Header("Multiplier and Extra Spins")]
	//[SerializeField] private AnimationListController.AnimationInformationList extraSpinsAndWildMultiplierIncreaseAnims;
	[SerializeField] private MultiplierAndExtraSpinsAnimData[] multiplierAndExtraSpinsAnimData;
	[SerializeField] private LabelWrapperComponent extraSpinsAmountLabel;
	[Tooltip("Start location for spinIncrementParticleTrail.")]
	[SerializeField] private Transform spinIncrementParticleTrailStartLocation;
	[Tooltip("Particle trail used when adding additional spins to the spin count meter.")]
	[SerializeField] private AnimatedParticleEffect spinIncrementParticleTrail;
	[Tooltip("Adds a delay before the spin number on the spin panel updates so that it can be synced with animations.")]
	[SerializeField] private float delayBeforeAddingAdditionalSpin = 0.0f;
	[Tooltip("Adds a delay before the WD symbols start changing to the updated multiplier so that it can be synced with animations.")]
	[SerializeField] private float delayBeforeUpdatingWildSymbolMultipliers = 0.0f;

	[SerializeField] private string SPINS_ADDED_INCREMENT_SOUND_KEY = "freespin_spins_added_increment";

	[System.Serializable]
	private class MultiplierAndExtraSpinsAnimData
	{
		public int multiplier;
		public AnimationListController.AnimationInformationList awardAnimations;
	}

	private int currentWdMultiplier = 1;
	
	private const string OUTCOME_TYPE_RANDOM_CREDIT_AWARD = "random_credit_award";
	private const string FIELD_REWARD_AMOUNT = "reward_amount";
	private const string OUTCOME_TYPE_RANDOM_ADVANCE_AWARD = "random_advance_award";
	private const string FIELD_EXTRA_SPINS_AWARDED = "extra_spins_awarded";
	private const string FIELD_WD_MULTIPLIER_INCREMENT = "wd_multiplier_increment";
	
	public override bool needsToExecutePreReelsStopSpinning()
	{
		JSON[] reevals = reelGame.outcome.getArrayReevaluations();
		return (reevals != null && reevals.Length > 0);
	}

	public override IEnumerator executePreReelsStopSpinning()
	{
		JSON[] reevals = reelGame.outcome.getArrayReevaluations();
		
		for (int i = 0; i < reevals.Length; i++)
		{
			JSON currentReevalJson = reevals[i];
			string reevalType = currentReevalJson.getString(SlotOutcome.FIELD_TYPE, "");
			if (reevalType == OUTCOME_TYPE_RANDOM_CREDIT_AWARD)
			{
				long creditValue = currentReevalJson.getLong(FIELD_REWARD_AMOUNT, 0);
				creditValue *= reelGame.multiplier;

				if (creditValueLabel != null)
				{
					if (isDisplayingAbbreviatedCreditValue)
					{
						creditValueLabel.text = CreditsEconomy.multiplyAndFormatNumberAbbreviated(creditValue, shouldRoundUp:false);
					}
					else
					{
						creditValueLabel.text = CreditsEconomy.convertCredits(creditValue);
					}
				}

				TICoroutine creditAwardCoroutine = null;

				if (creditRewardAnims.Count > 0)
				{
					creditAwardCoroutine = StartCoroutine(AnimationListController.playListOfAnimationInformation(creditRewardAnims));
				}

				if (delayBeforeCreditRollupStart > 0.0f)
				{
					yield return new TIWaitForSeconds(delayBeforeCreditRollupStart);
				}
				
				yield return StartCoroutine(reelGame.rollupCredits(0, 
					creditValue, 
					ReelGame.activeGame.onPayoutRollup, 
					isPlayingRollupSounds: true,
					specificRollupTime: 0.0f,
					shouldSkipOnTouch: true,
					allowBigWin: false,
					isAddingRollupToRunningPayout: true));

				// In freespins don't add the credits to the player, just add it to the bonus game amount that will be paid out when returning from freespins
				if (reelGame.hasFreespinGameStarted)
				{
					yield return StartCoroutine(reelGame.onEndRollup(false));
				}
				else
				{
					// Base game, go ahead and pay this out right now
					reelGame.addCreditsToSlotsPlayer(creditValue, "random credits or added spins and wild multiplier award", shouldPlayCreditsRollupSound: false);
				}

				if (creditAwardCoroutine != null)
				{
					while (!creditAwardCoroutine.finished)
					{
						yield return null;
					}
				}
			}
			else if (reevalType == OUTCOME_TYPE_RANDOM_ADVANCE_AWARD)
			{
				// Make sure the reevaluation actually contains data, if not we'll just ignore it.
				// The server team felt like it would be bad to remove this indicator that a reevaluation
				// is operating on every spin, even though the client doesn't require that info.
				// NOTE : We check for 1 for empty data, because the type will be the only field the reevaluation contains.
				if (currentReevalJson.getKeyList().Count > 1)
				{
					int extraSpinsAwarded = currentReevalJson.getInt(FIELD_EXTRA_SPINS_AWARDED, 0);
					if (extraSpinsAmountLabel != null)
					{
						extraSpinsAmountLabel.text = CommonText.formatNumber(extraSpinsAwarded);
					}

					int wdMultiplierIncrement = currentReevalJson.getInt(FIELD_WD_MULTIPLIER_INCREMENT, 0);
					int wdMultiplierToLookup = currentWdMultiplier + wdMultiplierIncrement;

					MultiplierAndExtraSpinsAnimData animData = getMultiplierAndExtraSpinsAnimDataForMultiplier(wdMultiplierToLookup);
					
					List<TICoroutine> updateSpinsAndMultiplierWithAnimsCoroutines = new List<TICoroutine>();

					if (animData != null && animData.awardAnimations.Count > 0)
					{
						updateSpinsAndMultiplierWithAnimsCoroutines.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(animData.awardAnimations)));
					}

					updateSpinsAndMultiplierWithAnimsCoroutines.Add(StartCoroutine(updateMultiplierForSymbolCreation(wdMultiplierIncrement)));
					updateSpinsAndMultiplierWithAnimsCoroutines.Add(StartCoroutine(addAdditionalSpins(extraSpinsAwarded)));

					if (updateSpinsAndMultiplierWithAnimsCoroutines.Count > 0)
					{
						yield return StartCoroutine(Common.waitForCoroutinesToEnd(updateSpinsAndMultiplierWithAnimsCoroutines));
					}
				}
			}
		}
	}

	// Update the multiplier used when creating the symbols, supports a 
	// delay so that this can be synced with animations which present the
	// change
	private IEnumerator updateMultiplierForSymbolCreation(int wdMultiplierIncrement)
	{
		if (delayBeforeUpdatingWildSymbolMultipliers > 0.0f)
		{
			yield return new TIWaitForSeconds(delayBeforeUpdatingWildSymbolMultipliers);
		}
		
		// Update the currentWdMultiplier which will change the symbols spinning on the reels
		currentWdMultiplier += wdMultiplierIncrement;
	}

	// Update the spin count with support for an added delay so that this can
	// be synced with animations which present the change
	private IEnumerator addAdditionalSpins(int extraSpinsAwarded)
	{
		if (delayBeforeAddingAdditionalSpin > 0.0f)
		{
			yield return new TIWaitForSeconds(delayBeforeAddingAdditionalSpin);
		}
		
		if (spinIncrementParticleTrail != null && spinIncrementParticleTrailStartLocation != null)
		{
			yield return StartCoroutine(spinIncrementParticleTrail.animateParticleEffect(spinIncrementParticleTrailStartLocation));
		}

		// Add the extra spins to the reel game
		Audio.playSoundMapOrSoundKey(SPINS_ADDED_INCREMENT_SOUND_KEY);
		reelGame.numberOfFreespinsRemaining += extraSpinsAwarded;
	}
	
	// executeAfterSymbolSetup() section
	// Functions in this section are called once a symbol has been setup.
	public override bool needsToExecuteAfterSymbolSetup(SlotSymbol symbol)
	{
		// check if the current skin has symbol overrides that need to apply to this symbol
		if (symbol.isWildSymbol)
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	public override void executeAfterSymbolSetup(SlotSymbol symbol)
	{
		// swap the wild symbol for the current multiplier version
		string newSymbolName;

		if (currentWdMultiplier == 1)
		{
			newSymbolName = "WD";
		}
		else
		{
			newSymbolName = "W" + currentWdMultiplier;
		}
		
		if (symbol.isFlattenedSymbol)
		{
			// need to convert to the flattened version of this WD version
			Vector2 symbolSize = symbol.getWidthAndHeightOfSymbol();
			newSymbolName = SlotSymbol.constructNameFromDimensions(newSymbolName + SlotSymbol.FLATTENED_SYMBOL_POSTFIX, (int)symbolSize.x, (int)symbolSize.y);
		}

		if (symbol.name != newSymbolName)
		{
			symbol.mutateTo(newSymbolName, null, false, true);
		}
	}

	private MultiplierAndExtraSpinsAnimData getMultiplierAndExtraSpinsAnimDataForMultiplier(int multiplier)
	{
		for (int i = 0; i < multiplierAndExtraSpinsAnimData.Length; i++)
		{
			MultiplierAndExtraSpinsAnimData currentAnimData = multiplierAndExtraSpinsAnimData[i];
			if (currentAnimData.multiplier == multiplier)
			{
				return currentAnimData;
			}
		}

		return null;
	}
}
