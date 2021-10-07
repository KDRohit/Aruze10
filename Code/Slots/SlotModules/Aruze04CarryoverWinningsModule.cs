using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Class made to handle the BN symbol payout in aruze04 that happens before entering the bonus

Creation Date: December 7, 2017
Original Author: Scott Lepthien
*/
public class Aruze04CarryoverWinningsModule : CarryoverWinningsModule 
{
	[SerializeField] private string BN_SYMBOL_ANIM_SOUND_NAME = "scatter_symbol_animate";
	[SerializeField] private float ROLLUP_DELAY = 0.0f;
	[SerializeField] private float ROLLUP_LENGTH_OVERRIDE = 0.0f;
	
	private SlotBaseGame slotBaseGame = null;
	private int numFinishedBonusSymbolAnims = 0;
	
	public override void Awake()
	{
		base.Awake();
		if (reelGame is SlotBaseGame)
		{
			slotBaseGame = reelGame as SlotBaseGame;
		}
		else
		{
			Debug.LogWarning("Ainsworth01CarryoverWinningsModule should only be attached to a 'SlotBaseGame'.");
			Destroy(this);
		}
	}
	
	private void onRollupPayoutToWinningsOnly(long payoutValue)
	{
		slotBaseGame.setWinningsDisplay(payoutValue);
	}
	
	private IEnumerator rollupPayoutToWinningsOnly(long start, long end, float winAudioLen)
	{
		if (ROLLUP_DELAY > 0.0f)
		{
			yield return new TIWaitForSeconds(ROLLUP_DELAY);
		}

		long valueAwarded = end - start;
		slotBaseGame.addCreditsToSlotsPlayer(valueAwarded, "BN bonus_credits", shouldPlayCreditsRollupSound: false);
		yield return StartCoroutine(SlotUtils.rollup(start, end, slotBaseGame.onPayoutRollup, false, winAudioLen));
		// trigger the end rollup to move the winnings into the runningPayoutRollupValue
		yield return StartCoroutine(slotBaseGame.onEndRollup(isAllowingContinueWhenReady: false));
	}
	
	private long calculateOutcomeRollup()
	{
		long adjustedMultiplier = slotBaseGame.multiplier;
		if (BonusGameManager.instance.betMultiplierOverride != -1)
		{
			adjustedMultiplier = BonusGameManager.instance.betMultiplierOverride;
		}

		long rollupCredits = 0;
		JSON[] reevaluations = slotBaseGame.outcome.getArrayReevaluations();
		for (int i = 0; i < reevaluations.Length; i++)
		{
			JSON reeval = reevaluations[i];
			long bonusCredits = reeval.getLong("bonus_credits", -1);

			if (bonusCredits != -1)
			{
				rollupCredits += bonusCredits;
			}
		}

		return rollupCredits * adjustedMultiplier;
	}
	
	public override bool needsToExecutePreShowNonBonusOutcomes()
	{
		return true;
	}

	public override void executePreShowNonBonusOutcomes()
	{
		if (carryoverWinnings > 0)
		{
			BonusGameManager.instance.finalPayout = BonusGameManager.instance.finalPayout - carryoverWinnings;
			carryoverWinnings = 0;
		}
	}
	
	public override bool needsToExecutePlayBonusAcquiredEffectsOverride()
	{
		return true;
	}
	
	public override IEnumerator executePlayBonusAcquiredEffectsOverride()
	{
		// Play bonus symbol aniamtion
		int numStartedBonusSymbolAnims = 0;
		numFinishedBonusSymbolAnims = 0;
		SlotReel[] reelArray = reelGame.engine.getReelArray();
		for (int reelIdx = 0; reelIdx < reelArray.Length; reelIdx++)
		{
			// force reels to be anticipation reels since we aren't handling anticipations normally
			reelArray[reelIdx].setAnticipationReel(true);
			numStartedBonusSymbolAnims += reelArray[reelIdx].animateBonusSymbols(onBonusSymbolAnimationDone);
		}

		carryoverWinnings = calculateOutcomeRollup();

		float winAudioLen = ROLLUP_LENGTH_OVERRIDE;
		if (!string.IsNullOrEmpty(BN_SYMBOL_ANIM_SOUND_NAME))
		{
			winAudioLen = Audio.getAudioClipLength(BN_SYMBOL_ANIM_SOUND_NAME);
			Audio.playSoundMapOrSoundKey(BN_SYMBOL_ANIM_SOUND_NAME);
		}
		
		// Pretend to rollup the score BEFORE the freespins games.
		//	This prevents dsync while still showing the rollup sequence to the player.
		yield return StartCoroutine(rollupPayoutToWinningsOnly(0, carryoverWinnings, winAudioLen));

		// Wait for the bonus symbol animations to finish, if they haven't yet
		while (numFinishedBonusSymbolAnims < numStartedBonusSymbolAnims)
		{
			yield return null;
		}

		// mark that we've played the bonus acquired effects so they don't trigger again
		slotBaseGame.isBonusOutcomePlayed = true;
	}

	// Tracks how many of the started bonus symbol animations have completed
	public void onBonusSymbolAnimationDone(SlotSymbol sender)
	{
		numFinishedBonusSymbolAnims++;
	}
}
