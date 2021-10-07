using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Basic module you can attach to setup a sliding reel game

Original Author: Scott Lepthien
*/
public class BasicSlidingGameModule : SlotModule 
{
	[SerializeField] protected ParticleSystem slideStoppedFlourish;
	[SerializeField] protected Animator slideStoppedAnimator;
	[SerializeField] protected Animator[] slideStoppedAnimators; // quick and dirty since we don't want to break other connections.
	[SerializeField] private string SLIDE_LEFT_ANIMATION_NAME;
	[SerializeField] private string SLIDE_RIGHT_ANIMATION_NAME;
	[SerializeField] private string SLIDE_STOPPED_ANIMATION_NAME;
	[SerializeField] private float TIME_TO_SLIDE_FOREGROUND = 1.0f;

	private const string FOREGROUND_SLIDE_SOUND_KEY = "sliding_reels_slide";
	private const string FOREGROUND_SLIDE_VO_SOUND_KEY = "sliding_reels_VO";
	private const string FOREGROUND_LOCK_SOUND_KEY = "sliding_reels_lock";

	private enum SlideDirection
	{
		STOPPED = 0,
		RIGHT 	= 1,
		LEFT 	= 2
	}

	/// Calls the IEnumerator that will call the game specific sliding functions.
	public override bool needsToExecuteOnReelsSlidingCallback()
	{
		return true;
	}

	/// Handle sliding of the reels
	public override IEnumerator executeOnReelsSlidingCallback()
	{
		bool didSlide = false;
		bool slideLeft = false;
		int numberOfSlides = 0;
		SlotReel[] reelArray = reelGame.engine.getReelArray();

		for (int i = 0; i < reelArray.Length; i++)
		{
			SlotReel reel = reelArray[i];
			if (reel.visibleSymbols.Length == 5)
			{
				// This is one of the reels that we need to slide.
				Transform posToSlideTo = reelGame.engine.getReelRootsAt(i, -1, 0).transform;
				slideLeft = (reel.getReelGameObject().transform.position.x - posToSlideTo.position.x) > 0.1;
				if (Mathf.Abs(reel.getReelGameObject().transform.position.x - posToSlideTo.position.x) > 0.1)
				{
					numberOfSlides++;
					iTween.MoveTo(reel.getReelGameObject(), iTween.Hash("x", posToSlideTo.position.x, "time", TIME_TO_SLIDE_FOREGROUND, "islocal", false, "easetype", iTween.EaseType.linear));
					didSlide = true;
				}
			}
		}
		if (didSlide)
		{
			Audio.play(Audio.soundMap(FOREGROUND_SLIDE_VO_SOUND_KEY));
			Audio.play(Audio.soundMap(FOREGROUND_SLIDE_SOUND_KEY));
			if (numberOfSlides != reelGame.engine.getReelRootsLength(1))
			{
				Debug.LogError("Didn't slide all " + reelGame.engine.getReelRootsLength(1) + " foreground reels, only moved" + numberOfSlides);
			}
			if (slideLeft)
			{
				onSlidingLeft();
			}
			else
			{
				onSlidingRight();
			}
			yield return new TIWaitForSeconds(TIME_TO_SLIDE_FOREGROUND);
			onSlidingStopped();
		}
	}

	protected virtual void onSlidingLeft()
	{
		slide(SlideDirection.LEFT);
	}

	protected virtual void onSlidingRight()
	{
		slide(SlideDirection.RIGHT);
	}

	protected virtual void onSlidingStopped()
	{
		if (slideStoppedFlourish != null)
		{
			slideStoppedFlourish.Play();
		}

		slide(SlideDirection.STOPPED);

		Audio.play(Audio.soundMap(FOREGROUND_LOCK_SOUND_KEY));
	}

	private void slide(SlideDirection direction)
	{
		string animationName = "";
		switch (direction)
		{
			case SlideDirection.LEFT:
				animationName = SLIDE_LEFT_ANIMATION_NAME;
				break;
			case SlideDirection.RIGHT:
				animationName = SLIDE_RIGHT_ANIMATION_NAME;
				break;
			case SlideDirection.STOPPED:
				animationName = SLIDE_STOPPED_ANIMATION_NAME;
				break;
		}
		if (!string.IsNullOrEmpty(animationName))
		{
			if (slideStoppedAnimator != null)
			{
				slideStoppedAnimator.Play(animationName);
			}
			if (slideStoppedAnimators != null)
			{
				foreach (Animator animator in slideStoppedAnimators)
				{
					animator.Play(animationName);
				}
			}
		}
	}

// executeOnReelsStoppedCallback() section
// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return true;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		yield break;
	}
}
