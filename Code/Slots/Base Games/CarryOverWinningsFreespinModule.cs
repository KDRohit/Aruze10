using UnityEngine;
using System.Collections;

public class CarryOverWinningsFreespinModule : SlotModule 
{
	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		return BonusGamePresenter.instance.currentPayout != 0;
	}

	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		BonusSpinPanel.instance.winningsAmountLabel.text = CreditsEconomy.convertCredits(BonusGamePresenter.instance.currentPayout);
		FreeSpinGame.instance.setRunningPayoutRollupValue(BonusGamePresenter.instance.currentPayout);
	}
}
