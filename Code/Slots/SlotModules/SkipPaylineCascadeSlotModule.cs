using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SkipPaylineCascadeSlotModule : SlotModule
{
	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		return true;
	}

	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		reelGame.showPaylineCascade = false;
		yield break;
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return true;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		reelGame.showPaylineCascade = false;
		yield break;
	}
}
