using UnityEngine;
using System.Collections;


/**
 * Module to slide an object into position with a tween on round start
 */
public class ChallengeGameSlideInOnStart : ChallengeGameModule
{
	[SerializeField] private GameObject targetObject;
	[SerializeField] private float duration = 1.0f;
	[SerializeField] private Vector3 initialOffset;
	[SerializeField] private bool isBlocking = true;

	private Vector3 originalPosition;

	public override bool needsToExecuteOnRoundInit()
	{
		return true;
	}

	// Perform the offset on init to eliminate flicker
	public override void executeOnRoundInit(ModularChallengeGameVariant round)
	{
		originalPosition = targetObject.transform.localPosition;
		targetObject.transform.localPosition += initialOffset;
	}

	// Enable round start action
	public override bool needsToExecuteOnRoundStart()
	{
		return true;
	}
	
	// On round start, slide the object back to the original position.
	public override IEnumerator executeOnRoundStart()
	{
		if (isBlocking)
		{
			yield return new TITweenYieldInstruction(iTween.MoveTo(targetObject, iTween.Hash("position", originalPosition, "time", duration, "islocal", true, "easetype", iTween.EaseType.linear)));
		}
		else
		{
			iTween.MoveTo(targetObject, iTween.Hash("position", originalPosition, "time", duration, "islocal", true, "easetype", iTween.EaseType.linear));
		}

	}

}
