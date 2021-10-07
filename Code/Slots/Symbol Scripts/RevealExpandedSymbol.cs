using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
This script is for revealing an expanded symbol, such as the wild on oz00, where it is a wipe-on effect from the bottom up.
This is intended to be a one-off use, so it disables itself when done.
*/

public class RevealExpandedSymbol : TICoroutineMonoBehaviour
{
	public Renderer fadeRenderer;
	public ParticleSystem sparkles;
	public bool reverse = false;
	public float duration = 1;
	
	// void Awake()	// For testing.
	// {
	// 	fadeRenderer.enabled = false;
	// }
	
	void Start()
	{
		// Alfred - Commenting out the below for now, since it often just makes it so that the expanded symbol doesn't appear properly.
		//setRevealAmount(0);	// Might not really need this, but just making sure it looks clean.
		//StartCoroutine(doReveal());
	}
		
	private IEnumerator doReveal()
	{
//		yield return new WaitForSeconds(1);	// For testing.
//		fadeRenderer.enabled = true;		// For testing.
		
		sparkles.Play();
		float age = 0;
		
		while (age < duration)
		{
			age += Time.deltaTime;
			yield return null;
			setRevealAmount(age / duration);
		}
		
		setRevealAmount(1);
		sparkles.Stop();
		enabled = false;
	}
	
	private void setRevealAmount(float amount)
	{
		if (reverse)
		{
			amount = 1f - amount;
		}
		
		fadeRenderer.material.SetFloat("_Fade", amount);
		
		CommonTransform.setY(sparkles.transform, Mathf.Lerp(fadeRenderer.bounds.max.y, fadeRenderer.bounds.min.y, amount));
	}
}
