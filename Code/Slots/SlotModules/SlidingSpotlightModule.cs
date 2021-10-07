using UnityEngine;
using System.Collections;

/**
 * Moves the spotlight (or any other future effect) and stops based on the spotlightReelStartIndex
 */ 
public class SlidingSpotlightModule : SlotModule 
{
	[SerializeField] private GameObject slidingObject;
	[SerializeField] private GameObject[] stopPositions;	// Array of positions where the spotlight can stop
	[SerializeField] private float SLIDE_SPEED = 1.0f;
	[SerializeField] private int stopPositionIndex = 0;		// Initial position of the spotlight
	[SerializeField] private int preStopSlideCount = 0;		// Number of times to slide the spotlight just before stopping (Modify for desired effect)

	private bool initiateReelStop = false;
	private bool isSliding = false;

	public override bool needsToExecuteOnReelsSpinning()
	{
		return true;
	}

	public override IEnumerator executeOnReelsSpinning()
	{
		initiateReelStop = false;
		StartCoroutine(startSpotlightMovement());
		yield break;
	}

	public IEnumerator startSpotlightMovement()
	{
		while (!initiateReelStop)
		{
			yield return StartCoroutine(moveSpotlight());
		}

		yield break;
	}

	public override bool needsToExecutePreReelsStopSpinning()
	{
		return true;
	}

	public override IEnumerator executePreReelsStopSpinning()
	{
		stopPositionIndex = ReelGame.activeGame.spotlightReelStartIndex;
		initiateReelStop = true;
		int count = preStopSlideCount;

		//Wait for the previous slide to finish. This helps transition smoothly from the slide call from executeOnReelsSpinning() 
		while (isSliding)
		{
			yield return null;
		}

		//Adjust the last few number of slides for desired effect
		while (count > 0)
		{
			yield return StartCoroutine(moveSpotlight());
			count--;
		}
		yield return StartCoroutine(slide(stopPositions[stopPositionIndex]));
	}

	// Move the sliding object depending on the current position
	private IEnumerator moveSpotlight()
	{
		Vector3 currentPosition = slidingObject.transform.position;
		// When the spotlight is at the first position
		if (currentPosition == stopPositions[0].transform.position)
		{
			for (int i = 0; i < stopPositions.Length - 1; i++)
			{
				yield return StartCoroutine(slide(stopPositions[i+1]));

			}
		}
		// When the spotlight is at the last position
		else if (currentPosition == stopPositions[stopPositions.Length - 1].transform.position)
		{
			for (int i = stopPositions.Length - 1; i > 0 ; i--)
			{
				yield return StartCoroutine(slide(stopPositions[i-1]));

			}
		}
		// When the spotlight is at the last stopped position - true on every spin start
		else if (currentPosition == stopPositions[stopPositionIndex].transform.position)
		{
			for (int i = stopPositionIndex; i < stopPositions.Length - 1; i++)
			{
				yield return StartCoroutine(slide(stopPositions[i+1]));

			}
		}
	}

	// Tween the sliding object to a specific target position
	private IEnumerator slide(GameObject target)
	{
		isSliding = true;
		Hashtable tween = iTween.Hash("position", target.transform.position, "speed", SLIDE_SPEED, "islocal", false, "easetype", iTween.EaseType.linear);
		yield return new TITweenYieldInstruction(iTween.MoveTo(slidingObject, tween));
		isSliding = false;
	}
}
