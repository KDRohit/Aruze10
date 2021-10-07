using UnityEngine;
using System.Collections;

// Simple class to take care of the payout stuff
public class gwtw01FreeSpins : FreeSpinGame 
{
	// make sure these are 0 so the freespin game rollup isn't strange
	public override void initFreespins()
	{
		BonusGameManager.instance.finalPayout = 0;
		BonusGamePresenter.instance.currentPayout = 0;
		base.initFreespins();
	}

	/// The free spins game ended. Set the multiBonusGamePayout for multiple bonus game winnings tracking
	protected override void gameEnded()
	{
		BonusGameManager.instance.multiBonusGamePayout += BonusGamePresenter.instance.currentPayout;
		base.gameEnded();
	}
}
