using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using TMProExtensions;

/*
Animates characters in a TextMeshPro object so that they jiggle rotation.
This is a standalone effect. It can't be combined with other TMPro effects due to needing to
force a mesh update before applying each effect.
*/

public class TMProJiggle : MonoBehaviour
{
	public TextMeshPro tmPro;
	public bool animateNow = false;		// If set true, animation starts with infinite duration. Setting to false stops the animation.
	public float minRotation = 5.0f;
	public float maxRotation = 10.0f;
	public float minSpeed = 50.0f;
	public float maxSpeed = 100.0f;
	
	private bool isAnimating = false;	// Prevent starting another animation if already animating, because shit gets weird if you do.
	
	void Awake()
	{
		if (tmPro == null)
		{
			tmPro = gameObject.GetComponent<TextMeshPro>();
		}
	}
	
	void Update()
	{
		if (tmPro == null)
		{
			return;
		}
		
		if (!isAnimating && animateNow)
		{
			StartCoroutine(doAnimation());
		}
	}
	
	// duration is in seconds. Value of 0 or less means infinite duration.
	public IEnumerator doAnimation(float duration = 0.0f)
	{
		if (isAnimating)
		{
			yield break;
		}
		
		isAnimating = true;
		animateNow = true;	// Setting this to false will stop the animation.

		float elapsed = 0.0f;

		float[] charRotations = new float[0];
		float[] charMaxRotations = new float[0];
		float[] charSpeeds = new float[0];
		int[] charRotationDirs = new int[0];
		
		while (animateNow && (duration <= 0.0f || elapsed < duration))
		{
			charRotations = new float[tmPro.textInfo.characterCount];
			charMaxRotations = new float[tmPro.textInfo.characterCount];
			charSpeeds = new float[tmPro.textInfo.characterCount];
			charRotationDirs = new int[tmPro.textInfo.characterCount];	// -1 and 1 for clockwise and counter-clockwise.

			for (int i = 0; i < charRotations.Length; i++)
			{
				charRotations[i] = 0.0f;
				charMaxRotations[i] = Random.Range(minRotation, maxRotation);
				charSpeeds[i] = Random.Range(minSpeed, maxSpeed);
				charRotationDirs[i] = CommonMath.randomSign;
			}

			// Start with the default positioning of everything by forcing a mesh update first.
			tmPro.ForceMeshUpdate();
	
			for (int i = 0; i < tmPro.textInfo.characterCount; i++)
			{
				if (Mathf.Abs(charRotations[i]) >= charMaxRotations[i])
				{
					// Stay within the max rotation range.
					charRotations[i] = charMaxRotations[i] * charRotationDirs[i];
					// Change rotation direction.
					charRotationDirs[i] *= -1;
				}
				
				charRotations[i] += charSpeeds[i] * charRotationDirs[i] * Time.deltaTime;

				tmPro.transformCharacterRotation(i, charRotations[i]);
			}
	
			yield return null;

			elapsed += Time.deltaTime;
		}
		
		// When stopping animation, tween the rotated letters back to
		// no rotation over a very short time so they don't snap back instantly.
		elapsed = 0.0f;
		duration = 0.25f;
		while (elapsed < duration)
		{
			tmPro.ForceMeshUpdate();
			
			elapsed += Time.deltaTime;

			float normalized = 1.0f - Mathf.Clamp01(elapsed / duration);
			
			for (int i = 0; i < tmPro.textInfo.characterCount; i++)
			{
				if (charRotations.Length > 0)
				{
					charRotations[i] *= normalized;
					tmPro.transformCharacterRotation(i, charRotations[i]);
				}
			}
			
			yield return null;
		}
		
		animateNow = false;
		isAnimating = false;
	}
}
