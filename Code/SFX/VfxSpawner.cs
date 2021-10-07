using UnityEngine;
using System.Collections;

public class VfxSpawner : TICoroutineMonoBehaviour
{
	public GameObject vfxPrefab;
	public float waitTimeMin = 0;
	public float waitTimeMax = 0;
	public bool looping = false;
	
	private float waitTimeRemaining;
	
	void Start ()
	{
		waitTimeRemaining = RandomWaitTime();
	}
	
	void Update ()
	{
		if(waitTimeRemaining >= 0)
		{
			waitTimeRemaining -= Time.deltaTime;
			if(waitTimeRemaining <= 0)
			{
				VisualEffectComponent.Create(vfxPrefab, this.gameObject);
				if(looping)
				{
					waitTimeRemaining = RandomWaitTime();
				}
			}
		}
	}
	
	private float RandomWaitTime()
	{
		return Random.Range(waitTimeMin, waitTimeMax);
	}
}
