using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/**
Play an animation as soon as the respin will start, this is useful when you need to do a machine wide animation instead of per symbol animation for example.
*/
public class AnimateOnRespinModule : SlotModule 
{
	//[SerializeField] public OneShotAnimation syncAnimator;			// When this animation arrived to the stopState, game logic will continue
	[SerializeField] public List<Animator> asyncAnimators;			// This animators are started at the same time as syncAnimator. However, game logic does not wait for them to finish, they can be looping for instance
	//[SerializeField] public List<TriggeredAnimation> triggerAnimators;		// Specify animations that will be havea trigger at start and end (normally used when we want to have an animator sharing different animation state machines that are selected via the trigger)
	//[SerializeField] public string stopState = "finished"; 		// When this state is achieved, the game will progress
	[SerializeField] public TextMeshPro respinMessageTM;			// Specify if you have a label that has to be modified modified during the respinning
	[SerializeField] public string respinMessage = "Random Spin !";

	private const string RESPIN_MUSIC_KEY = "respin_music";					// Spinning has a different track than the normal background
	private const string RESPIN_SOUND1_KEY = "Haywire01";
	private const string RESPIN_SOUND2_KEY = "Haywire02";
	private const string RESPIN_BONUS_BELL_SOUND = "BonusInitBell01";
	private const string RESPIN_BONUS_BELL_OFF_SOUND = "BonusBellOneOff";

	List<int> cachedInitialStateHash;

	private string cachedMessage;	//Here we will store the message that is contained in the Label before replacing

	public override void Awake()
	{
		base.Awake();

		if(respinMessageTM != null)
			cachedMessage = respinMessageTM.text;

		cachedInitialStateHash = new List<int>();

		//Store the initial state name hash to be able to keep track of the initial state
		foreach(Animator anim in asyncAnimators)
		{
			cachedInitialStateHash.Add(anim.GetCurrentAnimatorStateInfo(0).shortNameHash);
		}
	}

	public override bool needsToExecuteOnReevaluationSpinStartSync()
	{
		return true;
	}
		
	public override IEnumerator executeOnReevaluationSpinStartSync()
	{
		if(respinMessageTM != null)
		{
			respinMessageTM.text = respinMessage;
		}

		int iAsync = 0;
		//ZAnimationUtils.ResetAnimator(syncAnimator.animator);

		//Start all the asynchronous animations
		foreach(Animator anim in asyncAnimators)
		{
			anim.enabled = true;
			anim.Play(cachedInitialStateHash[iAsync],0);
			++iAsync;
		}

		//Start all the triggered animations
		//foreach(TriggeredAnimation anim in triggerAnimators)
		//{
		//	anim.Start();
		//}

		//Start synchronous animation and wait for it to finish until logic continues
		//if(syncAnimator != null && syncAnimator.animator != null)
		//{
			if (reelGame.reevaluationSpinsRemaining == 1)
			{
				Audio.play(RESPIN_SOUND1_KEY);
			}
			else
			{
				Audio.play(RESPIN_SOUND2_KEY);
			}

		//	syncAnimator.animator.enabled = true;
		//	yield return StartCoroutine(ZAnimationUtils.crAnimateAndWait(syncAnimator.animator, syncAnimator.stopState));
		//	syncAnimator.animator.enabled = false;
		//}

		Audio.play(RESPIN_BONUS_BELL_OFF_SOUND);
		Audio.playMusic(Audio.soundMap(RESPIN_MUSIC_KEY));
		yield return null;
	}

	public override bool needsToExecuteOnReevaluationSpinEnd()
	{
		return true;
	}

	public override void executeOnReevaluationSpinEnd()
	{
		//Stop all the asynchronous animations since we finished the respin mode after the big win ended
		//foreach(Animator anim in asyncAnimators)
		//{
		//	ZAnimationUtils.ResetAnimator(anim);
		//}

		//End all the triggered animations
		//foreach(TriggeredAnimation anim in triggerAnimators)
		//{
		//	anim.End();
		//}

		//Restore the UI respin message to its default state in non respin mode
		if(respinMessageTM != null && cachedMessage != null)
		{
			respinMessageTM.text = cachedMessage;
		}
	}
}
