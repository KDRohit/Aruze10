using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Fade out an array of objects and their children on the round end.
 */
public class ChallengeGameFadeOnRoundEnd : ChallengeGameModule {


	//[SerializeField] private GameObject[] fadeObjects;
	[SerializeField] private List<FadeObject> fadeObjects = new List<FadeObject>();
	[SerializeField] private float FADE_DURATION = 1.0f;
	[SerializeField] private float FADE_ALPHA_START = 1.0f;
	[SerializeField] private float FADE_ALPHA_END = 0.0f;
	[SerializeField] private bool DeactivateFadeObjectsOnFadeEnd = false;

	public override bool needsToExecuteOnRoundEnd(bool isEndOfGame)
	{
		return true;
	}

	// fade assigned objects on round ending
	public override IEnumerator executeOnRoundEnd(bool isEndOfGame)
	{
		foreach (FadeObject fo in fadeObjects)
		{
			StartCoroutine(CommonGameObject.fadeGameObjectsTo(fo.objectsToFade, 1.0f, 0.0f, FADE_DURATION, false));
		}
		yield return new TIWaitForSeconds(FADE_DURATION);

		foreach (FadeObject fo in fadeObjects)
		{
			foreach (GameObject go in fo.objectsToFade)
			{
				go.SetActive(false);
			}
		}
	}

	[System.Serializable]
	public class FadeObject
	{
		public GameObject[] objectsToFade;
	}
}