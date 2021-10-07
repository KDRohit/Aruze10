using UnityEngine;
using System.Collections;

//Simple class to add a delay at the end of freespins games
public class DelayOnGameEndModule : SlotModule
{
	[SerializeField] protected float delayTime = 0.0f;

	public override bool needsToExecuteOnFreespinGameEnd()
	{
		return true;
	}

	public override IEnumerator executeOnFreespinGameEnd()
	{
		yield return new TIWaitForSeconds(delayTime);
	}
}
