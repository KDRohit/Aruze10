using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Module for Bettie page pulleys that slide at the top of the screen when
The sliding layer is moving.

Original Author: Leo Schnee
*/
public class Bettie01SlidingGameModule : BasicSlidingGameModule 
{

	[SerializeField] private Animator pulleyAnimator;
	[SerializeField] private Animation[] framePulleyAnimators;

	private const string PULLEY_STILL = "bettie01_Reel_pulleySystem_Still";
	private const string PULLEY_RIGHT = "bettie01_Reel_pulleySystem_Right";
	private const string PULLEY_LEFT = "bettie01_Reel_pulleySystem_Left";
	private const string FRAME_PULLEY_STILL = "FramePulleys_Still";
	private const string FRAME_PULLEY_RIGHT = "FramePulleys_Right";
	private const string FRAME_PULLEY_LEFT = "FramePulleys_Left";

	private enum SlideDirection
	{
		STILL 	= 0,
		RIGHT 	= 1,
		LEFT 	= 2
	}

	protected override void onSlidingLeft()
	{
		slidePulleys(framePulleyAnimators, pulleyAnimator, SlideDirection.LEFT);
	}

	protected override void onSlidingRight()
	{
		slidePulleys(framePulleyAnimators, pulleyAnimator, SlideDirection.RIGHT);
	}

	protected override void onSlidingStopped()
	{
		slidePulleys(framePulleyAnimators, pulleyAnimator, SlideDirection.STILL);
	}

	private void slidePulleys(Animation[] framePulleyAnimators, Animator pulleyAnimator, SlideDirection direction)
	{
		string frameAnimationName = "";
		string pulleyAnimationName = "";
		switch (direction)
		{
			case SlideDirection.STILL:
				frameAnimationName = FRAME_PULLEY_STILL;
				pulleyAnimationName = PULLEY_STILL;
				break;

			case SlideDirection.RIGHT:
				frameAnimationName = FRAME_PULLEY_RIGHT;
				pulleyAnimationName = PULLEY_RIGHT;
				break;

			case SlideDirection.LEFT:
				frameAnimationName = FRAME_PULLEY_LEFT;
				pulleyAnimationName = PULLEY_LEFT;
				break;
		}
		foreach (Animation framePulley in framePulleyAnimators)
		{
			framePulley.Play(frameAnimationName);
		}
		if (pulleyAnimator != null)
		{
			pulleyAnimator.Play(pulleyAnimationName);
		}
	}
}
