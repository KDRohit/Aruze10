using System.Collections;

//
// This module will perform rollups if the player wins credits from BN symbols
// or a line win just before activating going into a bonus game like freespins.
// Inheriting from PlayBonusAcquiredEffectsBaseModule allows us to play the
// normal bonus acquired effects after going the pre bonus game rollup.
//
// author : Nick Saito <nsaito@zynga.com>
// date : July 11, 2019
// games : gen86
//

public class CarryOverWinningPreBonusRollupModule : PlayBonusAcquiredEffectsBaseModule
{
	private bool didRollup; // stops rollups from happening twice if bonus aquired effect is called multiple times
	protected long carryoverWinnings = 0;

	public override bool needsToGetCarryoverWinnings()
	{
		return true;
	}

	public override long executeGetCarryoverWinnings()
	{
		return carryoverWinnings;
	}

	public override bool needsToExecuteOnPreSpinNoCoroutine()
	{
		return true;
	}

	// Initialize values at the start of each spin
	public override void executeOnPreSpinNoCoroutine()
	{
		carryoverWinnings = 0;
		didRollup = false;
	}

	public override bool needsToExecutePlayBonusAcquiredEffectsOverride()
	{
		return !didRollup;
	}

	// Rollup any line wins and take them off the bonus games final payout
	public override IEnumerator executePlayBonusAcquiredEffectsOverride()
	{
		didRollup = true;
		carryoverWinnings = reelGame.outcomeDisplayController.calculateBonusSymbolScatterPayout(reelGame.outcome);

		yield return StartCoroutine(SlotUtils.rollup(0, carryoverWinnings, onRollupPayoutToWinningsOnly));
		yield return StartCoroutine(base.executePlayBonusAcquiredEffectsOverride());
		SlotBaseGame.instance.isBonusOutcomePlayed = true;
	}

	// Sets the value in the winbox as the rollup happens.
	private void onRollupPayoutToWinningsOnly(long payoutValue)
	{
		reelGame.setWinningsDisplay(payoutValue);
	}
	
	public override bool needsToExecutePreShowNonBonusOutcomes()
	{
		return carryoverWinnings > 0;
	}

	public override void executePreShowNonBonusOutcomes()
	{
		// Since we do a rollup before going to a bonus game, we need to remove the carryoverWinnings from the
		// final payout. Otherwise, the amount of the rollup in the bonusgame will not match the payout of a
		// big win or the amount in the win box when we come back.
		// Need to remove this after the bonus game payout has actually been calculated or else the carryOverWinnings can be added twice
		BonusGameManager.instance.finalPayout = BonusGameManager.instance.finalPayout - carryoverWinnings;
		carryoverWinnings = 0;
	}
}
