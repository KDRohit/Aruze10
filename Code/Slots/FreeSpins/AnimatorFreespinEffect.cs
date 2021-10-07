using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AnimatorFreespinEffect : TICoroutineMonoBehaviour 
{
	public float EFFECT_WAIT_TIME;	
	public List<TweenInstruction> tweenInstructions;
	public AnimatorInstruction spinBoxAnimatorInstruction;
	[HideInInspector] public int numberOfSpinsToAdd = 0; // This number is dynamically set.
	
	private const string SPINS_ADDED_INCREMENT_SOUND_KEY = "freespin_spins_added_increment";
	
	public IEnumerator doAdditionalEffects()
	{
		foreach (TweenInstruction ti in tweenInstructions)
		{
			ti.MoveDestinationGO = SpinPanel.instance.bonusSpinPanel.GetComponent<BonusSpinPanel>().spinCountLabel.gameObject;
			StartCoroutine(ti.executeTween());

			if (ti.shouldWaitToComplete)
			{
				yield return StartCoroutine(waitThenIncrementSpinsNumber(ti.delay + ti.time));
			}
			else
			{
				StartCoroutine(waitThenIncrementSpinsNumber(ti.delay + ti.time));
			}
		}
		if (spinBoxAnimatorInstruction.animator != null)
		{
			yield return new TIWaitForSeconds(spinBoxAnimatorInstruction.DELAY_TIME);
			spinBoxAnimatorInstruction.startingLocation = SpinPanel.instance.bonusSpinPanel.GetComponent<BonusSpinPanel>().spinCountLabel.gameObject;
			spinBoxAnimatorInstruction.animator.transform.position = spinBoxAnimatorInstruction.startingLocation.transform.position;
			spinBoxAnimatorInstruction.animator.Play(spinBoxAnimatorInstruction.ANIMATION_NAME);
			yield return new TIWaitForSeconds(spinBoxAnimatorInstruction.POST_START_ANIMATION_WAIT);
		}
		else
		{
			//If we don't have a spin box explosion then just wait our effect time so we can see the effect before its destroyed. 
			yield return new TIWaitForSeconds (EFFECT_WAIT_TIME);
		}
	}
	
	private IEnumerator waitThenIncrementSpinsNumber(float wait)
	{
		if (numberOfSpinsToAdd > 0)
		{
			numberOfSpinsToAdd--;
			yield return new TIWaitForSeconds(wait);
			Audio.play(Audio.soundMap(SPINS_ADDED_INCREMENT_SOUND_KEY));
			FreeSpinGame.instance.numberOfFreespinsRemaining += 1;
		}
	}
}
