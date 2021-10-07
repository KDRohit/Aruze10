using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UseBonusSpinPanelWagerDisplay : SlotModule
{
	public override bool needsToExecuteOnSlotGameStartedNoCoroutine (JSON reelSetDataJson)
	{
		return BonusSpinPanel.instance != null;
	}

	public override void executeOnSlotGameStartedNoCoroutine (JSON reelSetDataJson)
	{
		BonusSpinPanel.instance.turnOnBetAmountBox(reelGame);
	}
}

