using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Controls time delays that can be skipped by touching anywhere.
*/

public class SkippableWait
{
	public bool isSkipping { get; private set; }
	
	public SkippableWait()
	{
		// Set default to false just to be sure.
		isSkipping = false;
	}
	
	public IEnumerator wait(float time)
	{
		if (!isSkipping)
		{
			GameTimer waitTimer = new GameTimer(time);
			while (!isSkipping && !waitTimer.isExpired)
			{
				isSkipping = isSkipping || TouchInput.didTap;
				yield return null;
			}
		}
	}
	
	// Some games have multiple stages of picks & reveals,
	// so the flag needs to be reset for each stage,
	// to prevent skipping reveals for all future stages of the game.
	public void reset()
	{
		isSkipping = false;
	}
}
