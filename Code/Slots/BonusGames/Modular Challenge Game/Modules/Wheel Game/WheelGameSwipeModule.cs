using UnityEngine;
using System.Collections;

/**
 * Module to handle swiping a finger to spin the wheel
 */
public class WheelGameSwipeModule : WheelGameModule {

	public GameObject swipeTarget;
	public GameObject wheelRotationTarget; // the gameobject to rotate during the spin pre-roll

	public bool setSwipeableSizeFromTargetCollider = false;

	[HideInInspector] public SwipeableWheel swipeableWheel;

	public override bool needsToExecuteOnRoundStart()
	{
		return true;
	}
		
	public override IEnumerator executeOnRoundStart()
	{
		// no need to wait for animation manually, modules yield individually
		initSwipeableWheel();
		yield return StartCoroutine(base.executeOnRoundStart());
	}

	public override bool needsToExecuteOnSpin()
	{
		return true;
	}

	public override IEnumerator executeOnSpin()
	{
		//Lets make sure we don't double spin.
		if (swipeableWheel != null)
		{
			swipeableWheel.enableSwipe(false);
		}
		yield break;
	}

	// set up the SwipeableWheel target
	public void initSwipeableWheel()
	{
		if (swipeableWheel == null)
		{
			swipeableWheel = wheelRotationTarget.AddComponent<SwipeableWheel>();
		}

		//float finalDegrees = 0f;

		swipeableWheel.init(
			swipeTarget,
			wheelParent.computeRequiredRotation(),
			onSwipeStart,
			wheelParent.onSpinComplete,
			wheelRotationTarget.transform,
			null,
			false,
			setSwipeableSizeFromTargetCollider);
	}

	// Called when the wheels are swiped
	protected virtual void onSwipeStart()
	{
		bool isClockWise = swipeableWheel.direction < 0;
		wheelParent.spinSwipe(swipeableWheel.angularVelocity, isClockWise);
	}
}
