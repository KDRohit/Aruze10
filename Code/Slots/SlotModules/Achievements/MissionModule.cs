using UnityEngine;
using System.Collections;
//TV code. Might be unused.
public class MissionModule : SlotModule
{
	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		return true;
	}

	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		//MissionAction.getMissionUpdate(GameState.game.keyName);
	}
}
