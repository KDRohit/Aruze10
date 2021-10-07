using UnityEngine;
using System.Collections;

/**
 * Module to shake the pointer while the wheel spins, rotating on the Z axis
 */
public class WheelGamePointerShakeModule : WheelGameModule 
{
	[SerializeField] private Transform targetObject;
	private WheelSpinner wheelSpinner;
	[SerializeField] private float maxRotationAngle = 35.0f;
	[SerializeField] private float smoothing = 3.0f;

	private Quaternion originalRotation;
	private Quaternion maxRotation;
	private Quaternion randomRotation;

	private bool isShaking = false;
	private float shakeIntensity = 1.0f; // a scaling value from 0.0-1.0 to adjust the shake intensity

	public override bool needsToExecuteOnRoundInit()
	{
		return true;
	}

	public override void executeOnRoundInit(ModularWheelGameVariant roundVariant, ModularWheel wheel)
	{
		originalRotation = targetObject.transform.localRotation;
		base.executeOnRoundInit(roundVariant, wheel);
	}

	public override bool needsToExecuteOnSpin()
	{
		return true;
	}

	public override IEnumerator executeOnSpin()
	{
		// assign the values & start the pointer shaking
		isShaking = true;
		wheelSpinner = getWheelSpinner();

		if (isSpinningClockwise())
		{
			maxRotation = Quaternion.AngleAxis(maxRotationAngle, Vector3.back);
		}
		else
		{
			maxRotation = Quaternion.AngleAxis(-maxRotationAngle, Vector3.back);
		}

		StartCoroutine(shakePointer());
		yield return StartCoroutine(base.executeOnSpin());
	}

	private IEnumerator shakePointer()
	{
		while(isShaking == true)
		{
			// TODO-TE: undesireable, the spinner isn't assigned until the spinning starts
			if (wheelSpinner == null && getWheelSpinner() != null)
			{
				wheelSpinner = getWheelSpinner();
			}

			if (wheelSpinner != null)
			{
				// compute the intensity based on wheel speed
				shakeIntensity = wheelSpinner.AngularVelocity / wheelSpinner.maxAngularVelocity;

				// generate a random rotation within the range specified
				randomRotation = Quaternion.Slerp(originalRotation, maxRotation, Random.Range(0f, shakeIntensity));

				// smoothly rotate closer to the random rotation
				targetObject.localRotation = Quaternion.Slerp(targetObject.localRotation, randomRotation, Time.deltaTime * smoothing);
			}

			yield return null;
		}
	}

	public override bool needsToExecuteOnSpinComplete()
	{
		return true;
	}

	public override IEnumerator executeOnSpinComplete()
	{
		isShaking = false;
		wheelSpinner = null;
		yield return StartCoroutine(base.executeOnSpinComplete());
	}

	private bool isSpinningClockwise()
	{
		return wheelParent.isSpinningClockwise;
	}

	private WheelSpinner getWheelSpinner()
	{
		return wheelParent.wheelSpinner;
	}
}
