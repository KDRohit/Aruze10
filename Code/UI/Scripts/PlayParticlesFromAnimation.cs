using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Attach to an object with an animator so you can call function to play particles from the animation.
Probably mostly useful for particle systems that play a burst of particles rather than a stream.
*/

public class PlayParticlesFromAnimation : MonoBehaviour
{
	public ParticleSystem particles;
	
	public void playParticles()
	{
		particles.Play();
	}
}
