using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Generic Free spins add the inital bet amount to the payout. This class should handle that.
*/

public class GenFreeSpins : FreeSpinGame
{
	protected override void startGame()
	{
		long startingAmount = slotGameData.baseWager * slotGameData.baseWagerMultiplier; // Base bet.
		if (SlotBaseGame.instance != null)
		{
			startingAmount *= SlotBaseGame.instance.multiplier; // Add in the multiplier
		}
		if (GameState.game.keyName == "gen04")
		{
			startingAmount = 0;
		}
		runningPayoutRollupValue = startingAmount;
		BonusGamePresenter.instance.currentPayout = startingAmount;
		BonusSpinPanel.instance.winningsAmountLabel.text = CreditsEconomy.convertCredits(BonusGamePresenter.instance.currentPayout);
		base.startGame();
	} 

	protected override void startSpinReels()
	{
		engine.spinReelsAlternatingDirection();
	}
}
