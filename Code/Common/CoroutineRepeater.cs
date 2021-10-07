using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//
// A class that makes it easy to control a coroutine that you want to be called after a certian delay.
// This is commonly used for the pickme animations.
//

public class CoroutineRepeater
{
	private float age = 0;
	private float waitTime = 0;
	TICoroutine coroutine = null;

	// Constant variables
	private readonly float MIN_TIME;
	private readonly float MAX_TIME;
	private readonly CoroutineCallback coroutineCallback;

	public CoroutineRepeater(float minTime, float maxTime, CoroutineCallback coroutineCallback)
	{

		MIN_TIME = minTime;
		MAX_TIME = maxTime;
		if (minTime > maxTime)
		{
			Debug.LogWarning("minTime is less than maxTime");
		}
		else if (minTime == maxTime)
		{
			Debug.LogWarning("minTime = maxTime, there will be no range for random.");
		}
		if (coroutineCallback == null)
		{
			Debug.LogError("coroutineCallback is null.");
		}
		else
		{
			this.coroutineCallback = coroutineCallback;
		}

		// Set the required variables
		waitTime = Random.Range(MIN_TIME, MAX_TIME);
		age = 0;
	}


	// Needs to be called every frame while you want the coroutine to have a chance to be called.
	public void update()
	{
		// These is nothing to do if there is no coroutineCallback.
		if (coroutineCallback == null)
		{
			return;
		}


		// Need to wait for the pickMeRoutine to end.
		if (coroutine != null && !coroutine.finished)
		{
			return;
		}
		// Check and see if we need to do the update.
		if (age > waitTime)
		{
			// Do the callback
			coroutine = RoutineRunner.instance.StartCoroutine(coroutineCallback());
			waitTime = Random.Range(MIN_TIME, MAX_TIME);
			age = 0;
		}
		age += Time.deltaTime;
	}

	/// Reset the timer, may need to be done if update() isn't called for a bit so it doesn't cause a pick me right away on update resuming
	public void reset()
	{
		age = 0;
	}

	public delegate IEnumerator CoroutineCallback();
}
