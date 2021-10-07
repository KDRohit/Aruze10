using UnityEngine;
using System.Collections;

/**
Attach to a visual pickem object with a shaking animation you can assign to this script
and this script will handle automatically shaking it at random intervals between.

Most art is now provided with Animators instead of the old Animations. If using Animator,
then you need to set up your animator so that there is a transition between the shake animation
and an idle animation. Otherwise, the Animator stays in the shake animation state after it's finished playing,
and the script will never animate the button again. I recommend creating the transition with no animation overlap,
since the shake animation probably starts and ends in the same position as the idle state anyway.
*/
public class PickemButtonShaker : TICoroutineMonoBehaviour 
{
	[SerializeField] private Animation shakeAnimation = null; 			// Animation that shakes the object
	[SerializeField] private Animator animator = null; 					// Only used if shakeAnimation isn't. (See notes at top)
	[SerializeField] private string animatorShakeAnimationName = "";	// Used for Animators instead of old-style Animation.
	[SerializeField] private float minTimeBetweenShakes = 5.0f;			// Minimum time an animation might take to play next
	[SerializeField] private float maxTimeBetweenShakes = 15.0f;		// Maximum time an animation might take to play next
	[SerializeField] private string audioKey = "";						// Play this sound whenever a shake happens.

	private GameObject animatedGameObject;		// The GameObject we're animating.
	private bool isPlayingAnim = false;			// Flag to tell if the animation is already playing
	private float animationTimer = 0;			// Used to track tha time till the animation is next played

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
		
		if (shakeAnimation != null)
		{
			animatedGameObject = shakeAnimation.gameObject;
		}
		else if (animator != null && animatorShakeAnimationName != "")
		{
			animatedGameObject = animator.gameObject;
		}
	}
	
	// Update is called once per frame
	private void Update() 
	{
		// check if the object the animation is attached to is still active so we don't animate a hidden object
		if (animatedGameObject.activeInHierarchy && !disableShaking)
		{
			animationTimer -= Time.deltaTime;

			if (!isPlayingAnim)
			{
				if (animationTimer <= 0)
				{
					if (shakeAnimation != null)
					{
						isPlayingAnim = true;
						shakeAnimation.Play();
					}
					else if (animator != null && animatorShakeAnimationName != "")
					{
						isPlayingAnim = true;
						animator.Play(animatorShakeAnimationName);
					}
					
					if (isPlayingAnim && audioKey != "")
					{
						Audio.play(audioKey);
					}
				}
			}
			else
			{
				// check if the animation is done
				if (shakeAnimation != null && !shakeAnimation.isPlaying)
				{
					isPlayingAnim = false;
				}
				else if (animator != null && !animator.GetCurrentAnimatorStateInfo(0).IsName(animatorShakeAnimationName))
				{
					isPlayingAnim = false;
				}

				// get a new time till animation
				resetTimer();
			}
		}
	}
	
	private void resetTimer()
	{
		animationTimer = Random.Range(minTimeBetweenShakes, maxTimeBetweenShakes);	
	}
}
