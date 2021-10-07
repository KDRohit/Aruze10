using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Handles throbbing a game object at defined intervals, forever.
*/

public class Throbamatic
{
	// A sequence of delay amounts between throbs.
	// Many times Tim J. likes to do two throbs in a row,
	// with severals seconds of pause between each pair of throbs.
	// Using this list allows that to be sequenced.
	// If you only want one throb every so often, then this list
	// only needs a single value.
	private float[] delays = null;
	private int currentDelayIndex = 0;
	
	// The duration and scale of each throb. All throbs use the same duration and scale.
	// If you want to get fancy-pants and allow different durations and scales per throb in a sequence,
	// then a more complex list will be required, but who needs that shit?
	private float duration = 1.0f;
	private float scale = 1.0f;
	
	// The script that hosts the update call.
	private MonoBehaviour host = null;
	
	// The GameObject that throbs.
	private GameObject go = null;
	
	// The timer tells us when it's time to throb again.
	private GameTimer timer = null;
	
	// Constructor for a single delay value.
	public Throbamatic(MonoBehaviour host, GameObject go, float delay, float scale, float duration)
	{
		init(host, go, new float[] { delay }, scale, duration);
	}
	
	// Constructor for an array of delay values.
	public Throbamatic(MonoBehaviour host, GameObject go, float[] delays, float scale, float duration)
	{
		init(host, go, delays, scale, duration);
	}
	
	// Common initialization.
	private void init(MonoBehaviour host, GameObject go, float[] delays, float scale, float duration)
	{
		this.host = host;
		this.go = go;
		this.delays = delays;
		this.scale = scale;
		this.duration = duration;
		
		startNextDelay();
	}
	
	// Starts the next delay before the next throb.
	private void startNextDelay()
	{
		currentDelayIndex = (currentDelayIndex + 1) % delays.Length;
		timer = new GameTimer(delays[currentDelayIndex]);
	}
	
	// update() must be called from a host MonoBehaviour's Update() method to stay updated.
	// We do this instead of making this a MonoBehaviour to avoid extra overhead
	// of unnecessary Update() calls, plus it makes this a 100% code-based solution.
	public void update()
	{
		if (timer != null && timer.isExpired)
		{
			timer = null;
			host.StartCoroutine(throb());
		}
	}

	// Do the throb, then start the next delay until the next throb.
	private IEnumerator throb()
	{
		yield return host.StartCoroutine(CommonEffects.throb(go, scale, duration));
		startNextDelay();
	}
}
