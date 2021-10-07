using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Lets you play a list of particleSystems while the reels are spinning, and stops them when the reels finish spinning.

Original Author: Chad McKinney
*/
public class PlayParticlesOnSpinningModule : SlotModule 
{
	[SerializeField] protected List<ParticleSystem> particleSystems;

// executeOnReelsSpinning() section
// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) immediately after the reels start spinning
	public override bool needsToExecuteOnReelsSpinning()
	{
		return true;
	}

	public override IEnumerator executeOnReelsSpinning()
    {
        playParticles();
        yield break;
	}
	
// executeOnReelsStoppedCallback() section
// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return true;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
    {
		pauseParticles();
		yield break;
	}

    protected void playParticles()
	{
		foreach (ParticleSystem particleSystem in particleSystems)
		{
            particleSystem.Play();
        }
    }

    protected void pauseParticles()
	{
		foreach (ParticleSystem particleSystem in particleSystems)
		{
            particleSystem.Pause();
        }
	}
}
