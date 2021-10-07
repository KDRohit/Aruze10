using UnityEngine;
using System.Collections;

/**
 A particle system effect that oscillates the color of a text label between two or more provided colors
 */
public class LoopingParticleWithDelay : TICoroutineMonoBehaviour
{
	public ParticleSystem system;
	private bool playing = false;

	void Awake()
	{
		if (system == null)
		{
			system = gameObject.GetComponent<ParticleSystem>();
		}
	}

	void Update()
	{
		if (system != null)
		{
			if (!system.IsAlive())
			{
				if (!playing)
				{
					playing = true;
					Invoke("restartSystem",Random.Range(0.5f, 10.0f));
				}
			}
			else
			{
				playing = false;
			}
		}
	}

	private void restartSystem()
	{
		system.Play();
	}
	
}

