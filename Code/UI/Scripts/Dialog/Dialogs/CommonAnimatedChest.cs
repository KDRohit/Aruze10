using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommonAnimatedChest : MonoBehaviour
{
	private enum ChestState
	{
		OFF_SCREEN,
		IDLE,
		OPEN

	}
    [SerializeField] private Animator animator;

    private const string IDLE_ANIMATION = "Chest Idle";
    private const string OPEN_ANIMATION = "Chest Open";
    private const string OPEN_IDLE_ANIMATION = "Chest Open Idle";
    private const string CHEST_DROP_IN = "Chest Drop In";
    private const string CHEST_SLIDE_RIGHT = "Chest Slide Right";
    private const string CHEST_OUTRO = "Chest Outro Option";
    private const string CHEST_OFF = "Chest Off";
    private ChestState currentState = ChestState.OFF_SCREEN;


    public void playIdle(bool forceShow, bool skipDropIn)
    {
	    StopAllCoroutines();
	    StartCoroutine(idleRoutine(currentState, forceShow, skipDropIn));
	    currentState = ChestState.IDLE;
    }

    private IEnumerator idleRoutine(ChestState animState, bool forceShow, bool skipDropIn)
    {

		switch(animState)
		{
			case ChestState.OFF_SCREEN:
				if (skipDropIn)
				{
					animator.Play(IDLE_ANIMATION);
				}
				else
				{
					animator.Play(CHEST_DROP_IN);
					yield return StartCoroutine(playAnimationAfterDelay(0.75f, IDLE_ANIMATION));
				}

				break;

			case ChestState.IDLE:
				if (forceShow)
				{
					animator.Play(IDLE_ANIMATION);
				}
				break;

			default:
				animator.Play(IDLE_ANIMATION);
				break;
		}


    }

    public void playOpenSequence()
    {
	    StopAllCoroutines();
	    animator.Play(OPEN_ANIMATION);
	    StartCoroutine(playAnimationAfterDelay(3.25f, OPEN_IDLE_ANIMATION));
	    currentState = ChestState.OPEN;
    }

    public void playOutro()
    {
	    StopAllCoroutines();
	    animator.Play(CHEST_OUTRO);
	    currentState = ChestState.OFF_SCREEN;
    }

    public void playSlideOff()
    {
	    StopAllCoroutines();
	    animator.Play(CHEST_SLIDE_RIGHT);
	    StartCoroutine(playAnimationAfterDelay(1.5f, CHEST_OFF));
	    currentState = ChestState.OFF_SCREEN;
    }
    
    public void playChestOff()
    {
	    StopAllCoroutines();
	    animator.Play(CHEST_OFF);
    }

    public void playDropIn()
    {
	    StopAllCoroutines();
	    StartCoroutine(dropInRoutine());
	    currentState = ChestState.IDLE;
    }

    private IEnumerator dropInRoutine()
    {
	    animator.Play(CHEST_DROP_IN);
	    yield return StartCoroutine(playAnimationAfterDelay(0.75f, IDLE_ANIMATION));
    }
    private IEnumerator playAnimationAfterDelay(float delay, string animationState)
    {
	    yield return new WaitForSeconds(delay);
	    animator.Play(animationState);
    }

    private void OnDestroy()
    {
	    StopAllCoroutines();
    }
}
