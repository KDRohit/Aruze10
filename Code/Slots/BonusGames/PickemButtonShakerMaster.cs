using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Similar to PickemButtonShaker, except that it's assigned to a master object that controls shaking a group of linked animators.
This allows us to animate only one at a time.

Most art is now provided with Animators instead of the old Animations. If using Animator,
then you need to set up your animator so that there is a transition between the shake animation
and an idle animation. Otherwise, the Animator stays in the shake animation state after it's finished playing,
and the script will never animate the button again. I recommend creating the transition with no animation overlap,
since the shake animation probably starts and ends in the same position as the idle state anyway.
*/
public class PickemButtonShakerMaster : TICoroutineMonoBehaviour 
{
	[SerializeField] private List<Animator> animators = null; 			// Animators that shakes the objects
	[SerializeField] private string animatorShakeAnimationName = "";	// Used for Animators instead of old-style Animation.
	[SerializeField] private float minTimeBetweenShakes = 5.0f;			// Minimum time an animation might take to play next
	[SerializeField] private float maxTimeBetweenShakes = 15.0f;		// Maximum time an animation might take to play next
	[SerializeField] private string audioKey = "";						// Play this sound whenever a shake happens.

	private GameTimer animationTimer = null;	// Used to track tha time till the animation is next played
	private Animator currentAnimator = null;

	// Flag which can be set by the pickem game to tell objects to cancel animating if reveals are starting 
	public bool disableShaking
	{
		get { return _disableShaking; }
		
		set
		{
			_disableShaking = value;
			
			if (!_disableShaking)
			{
				// Whenever shaking is re-enabled, also reset the animation timer so it doesn't shake immediately.
				resetTimer();
			}
		}
	}
	private bool _disableShaking = false;

	// Use this for initialization
	private void Awake() 
	{
		// set the initial time till animation
		resetTimer();
	}
	
	// Update is called once per frame
	private void Update() 
	{
		// check if the object the animation is attached to is still active so we don't animate a hidden object
		if (!disableShaking)
		{
			if (currentAnimator == null)
			{
				if (animationTimer.isExpired)
				{
					if (animators.Count > 0 && animatorShakeAnimationName != "")
					{
						currentAnimator = animators[Random.Range(0, animators.Count)];
						currentAnimator.Play(animatorShakeAnimationName);

						if (audioKey != "")
						{
							Audio.play(audioKey);
						}
					}
				}
			}
			else
			{
				// check if the animation is done
				if (currentAnimator != null && !currentAnimator.GetCurrentAnimatorStateInfo(0).IsName(animatorShakeAnimationName))
				{
					currentAnimator = null;
				}

				// get a new time till animation
				resetTimer();
			}
		}
	}
	
	// Manually add an animator. Usually necessary if pickem options
	// are created in code instead of as part of a prefab.
	public void addAnimator(Animator animator)
	{
		animators.Add(animator);
	}

	// Removes an Animator from the list so it never animates again.
	// Usually necessary when a pick is revealed.
	public void removeAnimator(Animator animator)
	{
		animators.Remove(animator);
	}
	
	private void resetTimer()
	{
		if (animationTimer == null)
		{
			animationTimer = new GameTimer(0);
		}
		animationTimer.startTimer(Random.Range(minTimeBetweenShakes, maxTimeBetweenShakes));
	}
}
