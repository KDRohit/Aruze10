using UnityEngine;
using System.Collections;

/**
Simple script that translates an object back and forth between two values.
Summer of Love currently uses this for some floating icons.
*/
public class TranslateBetween : TICoroutineMonoBehaviour
{
	/// Pronounced "Oiler"
	public Vector3 eulerAnglesOne;
	public Vector3 eulerAnglesTwo;
	public float duration = 1.0f;

	private bool isTweening = false;
	/// Update is called once per frame
	void Update()
	{
		if (!isTweening)
		{
			StartCoroutine(translateAnimation());
		}
	}

	private IEnumerator translateAnimation()
	{
	    isTweening = true;
		
		iTween.MoveBy(gameObject, iTween.Hash("amount", eulerAnglesOne, "time", duration, "easeType", iTween.EaseType.easeInSine));
		yield return new WaitForSeconds(duration);

		iTween.MoveBy(gameObject, iTween.Hash("amount", eulerAnglesTwo, "time", duration, "easeType", iTween.EaseType.easeInSine));
		yield return new WaitForSeconds(duration);

	    isTweening = false;
	}
}
