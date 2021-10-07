using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Elvira01 : SlotBaseGame
{
	public GameObject[] transitionPrefabs;
	public Transform transitionParent;
	[SerializeField] private float transitionSoundDelay = 0.0f;
	[SerializeField] private bool isUsingTransitionModule = false;

	private const string FREESPIN_TRANSITION_SOUND_KEY = "bonus_freespin_wipe_transition";
	
	protected override void reelsStoppedCallback()
	{
		// Must use the RoutineRunner.instance to start this coroutine,
		// since this gameObject gets disabled before the coroutine can finish.
		RoutineRunner.instance.StartCoroutine(reelsStoppedCoroutine());
	}
	
	private IEnumerator reelsStoppedCoroutine()
	{
		if (_outcome.isGifting)
		{
			yield return StartCoroutine(doFreeSpinsTransition());
		}
		
		base.reelsStoppedCallback();
	}

	protected override IEnumerator onBonusGameEndedCorroutine()
	{
		yield return StartCoroutine(base.onBonusGameEndedCorroutine());

		if (transitionParent != null && GameState.game.keyName != "osa04")
		{
			// Now clean it up.
			for (int i = 0; i < transitionParent.childCount; i++)
			{
				Destroy(transitionParent.GetChild(i).gameObject);
			}
		}
	}
	
	/// Do the transition before heading to free spins game.
	private IEnumerator doFreeSpinsTransition()
	{
		// Break if there's already a module handling the freespins transition.
		if (isUsingTransitionModule)
		{
			yield break;
		}

		// handle playing this early, so that it happens before the transition starts
		yield return StartCoroutine(doPlayBonusAcquiredEffects());

		float duration = 2.0f;
		// Instantiate a bunch of transition objects and make them fly toward the camera (or appear to, at least).
		if (GameState.game.keyName == "osa04")
		{
			Audio.play("TransitionToFreespinLion");
			duration = 2.5f;
		}

		Audio.tryToPlaySoundMapWithDelay(FREESPIN_TRANSITION_SOUND_KEY, transitionSoundDelay);

		if (transitionPrefabs.Length > 0)
		{
			for (int i = 0; i < 20; i++)
			{
				GameObject go = CommonGameObject.instantiate(transitionPrefabs[Random.Range(0, transitionPrefabs.Length)]) as GameObject;
				CommonGameObject.setLayerRecursively(go, Layers.ID_SLOT_OVERLAY);
				go.transform.parent = transitionParent;
				go.transform.localScale = Vector3.one;
		
				switch (Random.Range(0, 3))
				{
					case 0:	// From the top edge.
						go.transform.localPosition = new Vector3(
							Random.Range(-12.5f, 12.5f),
							Random.Range(8.5f, 12f),
							0
						);
						break;
				
					case 1:	// From the left edge.
						go.transform.localPosition = new Vector3(
							Random.Range(-12.5f, -15f),
							Random.Range(-8.5f, 8.5f),
							0
						);
						break;

					case 2:	// From the right edge.
						go.transform.localPosition = new Vector3(
							Random.Range(12.5f, 15f),
							Random.Range(-8.5f, 8.5f),
							0
						);
						break;
				}
		
				// Start at a random animation frame.
				Animation anim = go.GetComponent<Animation>();
				if (anim != null)
				{
					anim["Take 001"].time = Random.Range(0, anim["Take 001"].clip.length);
				}
					
				iTween.MoveTo(go, iTween.Hash("x", Random.Range(-12.5f, 12.5f), "y", Random.Range(-10f, 5f), "time", duration, "islocal", true, "easetype", iTween.EaseType.easeOutQuad));
				iTween.ScaleTo(go, iTween.Hash("scale", Vector3.one * 50, "time", duration, "islocal", true, "easetype", iTween.EaseType.easeInExpo));
			}
		}
	
		if (GameState.game.keyName == "osa04")
		{
			yield return new WaitForSeconds(duration * 0.5f);
		}
		else
		{
			yield return new WaitForSeconds(duration);
			yield return null;	// Wait one more frame to make sure the iTweens are done.
		}
	}
}