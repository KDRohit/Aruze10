using UnityEngine;
using System.Collections;

/**
File to control the fish which animates in the water of the zynga01 free spin game
*/
public class Zynga01FishAnimator : TICoroutineMonoBehaviour 
{
	[SerializeField] private Animation fishAnimation = null;					// Animation of the fish jumpin
	[SerializeField] private Animation firstSplashAnimation = null;				// First splash animaiton for fish leaving water
	[SerializeField] private Animation secondSplashAnimation = null;			// Second splash animation for fish re-entering water
	[SerializeField] private Animator firstSplashRingAnimator = null;			// First splash ring for fish leaving water
	[SerializeField] private Animator secondSplashRingAnimator = null;			// Second splash ring for fish re-entering water

	private bool isPlayingAnim = false;		// Flag to tell if the animation is already playing
	private float animationTimer = 0;		// Used to track tha time till the animation is next played

	private const float const_MIN_TIME_TO_ANIM = 6.0f;		// Minimum time an animation might take to play next
	private const float const_MAX_TIME_TO_ANIM = 20.0f;		// Maximum time an animation might take to play next

	// Use this for initialization
	private void Awake () 
	{
		// set the initial time till animation
		animationTimer = Random.Range(const_MIN_TIME_TO_ANIM, const_MAX_TIME_TO_ANIM);
	}
	
	// Update is called once per frame
	private void Update () 
	{
		animationTimer -= Time.deltaTime;

		if (!isPlayingAnim)
		{
			if (animationTimer <= 0)
			{
				isPlayingAnim = true;

				// just play everything at once, since the animations were setup with blank space so they could all at once
				fishAnimation.Play();
				firstSplashAnimation.Play();
				secondSplashAnimation.Play();
				firstSplashRingAnimator.Play("splash_ring");
				secondSplashRingAnimator.Play("splash_ring 2");

			}
		}
		else
		{
			// check if the animation is done
			if (secondSplashRingAnimator.GetCurrentAnimatorStateInfo(0).IsName("splash_ring 2") == false)
			{
				isPlayingAnim = false;
			}

			// get a new time till animation
			animationTimer = Random.Range(const_MIN_TIME_TO_ANIM, const_MAX_TIME_TO_ANIM);
		}
	}
}
