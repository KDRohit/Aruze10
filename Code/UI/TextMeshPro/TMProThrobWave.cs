using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using TMProExtensions;

/*
Animates characters in a TextMeshPro object so that they throb from left to right like a wave.
This is a standalone effect. It can't be combined with other TMPro effects due to needing to
force a mesh update before applying each effect.
*/

public class TMProThrobWave : MonoBehaviour
{
	private enum ThrobMode
	{
		WAITING = 0,
		UP = 1,
		DOWN = -1,
		FINISHED = -2
	};
	
	public TextMeshPro tmPro;
	public bool animateNow = false;	// When set to true, the animation happens once.

	public float throbScale = 2.0f;
	public float throbSpeed = 5.0f;		// Higher = faster throb.
	public float waveDuration = 0.5f;	// Lower = faster wave.
	
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
		
		if (animateNow)
		{
			StartCoroutine(doAnimation());
		}
	}
	
	public IEnumerator doAnimation()
	{
		animateNow = false;
		
		if (isAnimating)
		{
			yield break;
		}
		
		isAnimating = true;
		
		int finishedCount = 0;
		float elapsed = 0.0f;
		
		ThrobMode[] charThrobMode = new ThrobMode[tmPro.textInfo.characterCount];
		float[] charScales = new float[tmPro.textInfo.characterCount];
		for (int i = 0; i < charScales.Length; i++)
		{
			charThrobMode[i] = ThrobMode.WAITING;
			charScales[i] = 1.0f;
		}
		
		while (finishedCount < charScales.Length)
		{
			// Start with the default positioning of everything by forcing a mesh update first.
			tmPro.ForceMeshUpdate();

			float scaleAdjust = throbSpeed * Time.deltaTime;
			float normalizedElapsed = elapsed / waveDuration;

			int nextCharIndex = Mathf.FloorToInt(normalizedElapsed * tmPro.textInfo.characterCount);
			
			for (int i = 0; i < tmPro.textInfo.characterCount; i++)
			{
				if (charThrobMode[i] == ThrobMode.WAITING && i <= nextCharIndex)
				{
					// Start throbbing this character.
					charThrobMode[i] = ThrobMode.UP;
				}
				
				switch (charThrobMode[i])
				{
					case ThrobMode.UP:
						charScales[i] += scaleAdjust;
						if (charScales[i] >= throbScale)
						{
							// Reached max throb scale. Start going back down.
							charScales[i] = throbScale;
							charThrobMode[i] = ThrobMode.DOWN;
						}
						tmPro.transformCharacterScale(i, charScales[i]);
						break;
					case ThrobMode.DOWN:
						charScales[i] -= scaleAdjust;
						if (charScales[i] <= 1.0f)
						{
							// Finished throbbing.
							charScales[i] = 1.0f;
							charThrobMode[i] = ThrobMode.FINISHED;
							finishedCount++;
						}
						tmPro.transformCharacterScale(i, charScales[i]);
						break;
				}
			}
			
			elapsed += Time.deltaTime;
			yield return null;
		}

		isAnimating = false;
	}
}
