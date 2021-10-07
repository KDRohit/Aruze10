using System.Collections;
using UnityEngine;

//
// This module will perform a rollup if the player wins credits from BN symbols
// just before creating a bonus game like freespins.
//
// This uses a trigger symbol to match to the winning scatter pay because some games
// determine the bonus games through a reevaluator and the bonus game name is not
// associated in the paytable with static data.
//
// author : Nick Saito <nsaito@zynga.com>
// date : Oct 13, 2020
// games : orig002
//

public class CarryOverWinningPreBonusGameCreatedModule : CarryoverWinningsModule
{
	[Tooltip("The symbol we are expecting to be used as a trigger for bonus games")]
	[SerializeField] private string bonusTriggerSymbolName;

	public override bool needsToExecuteOnPreBonusGameCreated()
	{
		return true;
	}

	// Rollup any line wins and take them off the bonus games final payout
	public override IEnumerator executeOnPreBonusGameCreated()
	{
		carryoverWinnings = reelGame.outcomeDisplayController.calculateBonusSymbolScatterPayoutWithSymbolName(reelGame.outcome, bonusTriggerSymbolName);
		yield return StartCoroutine(SlotUtils.rollup(0, carryoverWinnings, onRollupPayoutToWinningsOnly));
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
