using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class gwtw01TestPickem : PickingGame<WheelOutcome>
{

	public override void init()
	{		
		init(BonusGameManager.instance.outcomes[BonusGameType.PORTAL] as WheelOutcome);
		WheelPick pick = outcome.getNextEntry();
		while (pick != null)
		{
			long total = (pick.credits) * GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;
			BonusGamePresenter.instance.currentPayout += total;
			pick = outcome.getNextEntry();
		}

		BonusGameManager.instance.multiBonusGamePayout += BonusGamePresenter.instance.currentPayout * BonusGameManager.instance.currentMultiplier;
		BonusGamePresenter.instance.gameEnded();
	}

	protected override IEnumerator pickemButtonPressedCoroutine(GameObject button)
	{
		yield return null;
	}

	
	protected override IEnumerator pickMeAnimCallback()
	{
		yield return null;
	}
}
