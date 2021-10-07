using UnityEngine;
using System.Collections;

/**
Wraps handling the animation stuff for the well symbol of the FarmVille2 game
*/
public class Zynga01WaterwellSymbolAnimator : TICoroutineMonoBehaviour 
{
	private const string BUCKET_SPLASH_ANIM_NAME = "bucket_action";
	private const string WELL_ANTICIPATION_ANIM_NAME = "anticipation";

	[SerializeField] private Animation wellAnimation = null; // Animation container for the well symbol of the Free Spin game

	/**
	Play the anticipation for the well, may not use this, or may be able to hook it up to happen through 3D symbol animator
	*/
	public void PlayAnticipationAnimation()
	{
		wellAnimation.Play(WELL_ANTICIPATION_ANIM_NAME);
	}

	/**
	Special animation that triggers when the water drops are spawned
	*/
	public void PlayBucketSplashAnimation()
	{
		wellAnimation.Play(BUCKET_SPLASH_ANIM_NAME);
	}
}
