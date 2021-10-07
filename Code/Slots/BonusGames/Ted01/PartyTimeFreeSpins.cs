using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
PartyTime free spins has a special thing (wilds on each side) that requires the normal FreeSpinGame class to be overridden.
*/

public class PartyTimeFreeSpins : FreeSpinGame
{
	public GameObject leftWildSymbol;
	public GameObject rightWildSymbol;
	private int moveCounter;
	
	public override void initFreespins()
	{
		base.initFreespins();
		BonusGamePresenter.instance.setGameScreenForFreeSpins();
		SlotBaseGame.instance.shouldUseBaseWagerMultiplier = true;
		// Set the initial position of the wild to reel 5 (0-based index).
		//CommonTransform.setX(expandedWildSymbol.transform, reelRoots[4].transform.position.x, Space.World);
		
		// move counter
		moveCounter = 0;

		BonusGamePresenter.instance.currentPayout = BonusGamePresenter.portalPayout;

		// we need to apply the partial bonus multiplication that was done 
		// in the picking game so that the final math produces a value that isn't a desync
		multiplier = GameState.bonusGameMultiplierForLockedWagers;

		// We need to store this here to prevent desyncs, otherwise the current payout will be reset to zero in the ReelGame class
		runningPayoutRollupValue = BonusGamePresenter.instance.currentPayout;

		BonusSpinPanel.instance.winningsAmountLabel.text = CreditsEconomy.convertCredits(BonusGamePresenter.instance.currentPayout);

		Audio.switchMusicKeyImmediate("KaraokeLoopIdle");
	}
	
	protected override void Update()
	{
		base.Update();
	}
	
	protected override void startNextFreespin()
	{
		base.startNextFreespin();
		
		if (numberOfFreespinsRemaining < 0)
		{
			Debug.Log("We're below our threshold, time to bail.");
			return;
		}

		// Setting the wild index so we can skip the paylines animations later.
		engine.wildReelIndexes = new List<int>();
		engine.wildReelIndexes.Add(0);
		engine.wildReelIndexes.Add(4);
		
		if (moveCounter > 0 && moveCounter <= 4)
		{	
			// Sound Call for Move and Click on Expanded Wild Based on 1 second tween time
			Audio.play("WildSymbolMoves1");
			Audio.play("WildSymbolStopsClick", 1f, 0f, 1f);
		}
		
		// increment move/click counter
		moveCounter++;
	}
}

