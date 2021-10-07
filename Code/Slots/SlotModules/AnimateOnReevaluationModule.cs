using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This module can be used to play an animation on reevaluations
public class AnimateOnReevaluationModule : SlotModule 
{
	private const string REEVALUATION_SOUND_KEY = "respin_music";

	[SerializeField] private GameObject reevaluationAwardAnimation;				// Animation that plays over the main game area (reels)
	[SerializeField] private GameObject winBoxAnimation;					// Animation that plays on the Spin Panel
	[SerializeField] private float delayBeforeProceeding = 1.0f;			// Delay before the effect is over and the game proceeds
	[SerializeField] private bool instantiatePrefab = true;
	[SerializeField] private Vector3 awardAnimationOffset = new Vector3();

	private GameObject currentReevaluationAwardAnimation;


	public override void Awake()
	{
		base.Awake();
		if (instantiatePrefab)
		{
			currentReevaluationAwardAnimation = (GameObject)CommonGameObject.instantiate(reevaluationAwardAnimation);
			currentReevaluationAwardAnimation.transform.parent = gameObject.transform;
			currentReevaluationAwardAnimation.transform.localPosition = Vector3.zero;
			currentReevaluationAwardAnimation.transform.localScale = Vector3.one;
			currentReevaluationAwardAnimation.SetActive(false);
		}

		if (currentReevaluationAwardAnimation != null)
		{
			Vector3 pos = currentReevaluationAwardAnimation.transform.localPosition;
			pos.x += awardAnimationOffset.x;
			pos.y += awardAnimationOffset.y;
			pos.z += awardAnimationOffset.z;
			currentReevaluationAwardAnimation.transform.localPosition = pos;
		}
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		if (reelGame.outcome.getArrayReevaluations() != null &&
			reelGame.outcome.getArrayReevaluations().Length > 0)
		{
			return true;
		}

		return false;
	}
		
	public override IEnumerator executeOnReelsStoppedCallback()
	{
		if (instantiatePrefab)
		{
			currentReevaluationAwardAnimation.SetActive(true);
		}
		else
		{
			reevaluationAwardAnimation.SetActive(true);
		}

		string soundKey = REEVALUATION_SOUND_KEY;

		if (FreeSpinGame.instance != null)
		{
			soundKey += "_freespin";
		}

		if (Audio.canSoundBeMapped(soundKey))
		{
			Audio.play(Audio.soundMap(soundKey));
		}

		yield return new TIWaitForSeconds(delayBeforeProceeding);

		if(winBoxAnimation != null)
		{
			winBoxAnimation.SetActive(false);
		}

		if (instantiatePrefab)
		{
			currentReevaluationAwardAnimation.SetActive(false);
		}
		else
		{
			reevaluationAwardAnimation.SetActive(false);
		}

		yield break;
	}
}

