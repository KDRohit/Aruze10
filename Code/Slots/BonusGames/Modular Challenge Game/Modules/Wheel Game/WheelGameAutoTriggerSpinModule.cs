using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Module made to auto trigger a wheel spinning after a set amount of time.  Intended
for cases where the player isn't expected to press a spin button or swipe a wheel in
order to start a spin.

Creation Date: 8/22/2018
Original Author: Scott Lepthien
*/
public class WheelGameAutoTriggerSpinModule : WheelGameModule 
{
	[SerializeField] private float WAIT_TIME_TO_SPIN_START = 1.0f;
	
	// executeOnRoundStarted() section
	// executes right when a round starts or finishes initing.
	public override bool needsToExecuteOnRoundStart()
	{
		return true;
	}

	public override IEnumerator executeOnRoundStart()
	{
		// Start a non-blocking coroutine that will trigger the wheel spin after the set amount of time
		StartCoroutine(waitAndStartWheelSpin());
		yield break;
	}
	
	private IEnumerator waitAndStartWheelSpin()
	{
		yield return new TIWaitForSeconds(WAIT_TIME_TO_SPIN_START);
		wheelRoundVariantParent.spinAllWheelsButtonPressed();
	}
}
