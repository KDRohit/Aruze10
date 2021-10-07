using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WheelGame : ChallengeGame 
{
	[SerializeField] protected GameObject wheel = null;						// The wheel itself
	[SerializeField] protected GameObject wheelPointer = null;				// The pointer that shows which slice is currently selected
	[SerializeField] protected UILabel[] wheelTexts = null;					// Text on the wheel slices
	[SerializeField] protected UIButton spinButton = null;					// The button to trigger the wheel to spin
	[SerializeField] protected TweenColor spinButtonFlasher = null;
	[SerializeField] protected UILabel winLabel;							// Label for the win amount
	[SerializeField] protected UILabel winText;								// Text expressing the number of coins won
	[SerializeField] protected GameObject winBox;
	[SerializeField] protected UILabel[] progressivePoolTexts;
	[SerializeField] protected GameObject[] progressivePoolEffects;
	[SerializeField] protected int numSlices = 10;							// Number of slices on the wheel
	[SerializeField] protected GameObject wheelStartAnimationObject;		// Animation played on the wheel when it starts spinning
	[SerializeField] protected GameObject wheelWinBoxAnimation;				// Animation for the win box
	[SerializeField] protected UISprite spinBoxWhiteTexture;				// Fade in texture for slices
	[SerializeField] protected GameObject winSliceAnimation;				// Animation played on the slice that is hit
	[SerializeField] protected GameObject topPointerAnimation;				// Animation played on the pointer once the user spins
	[SerializeField] protected GameObject wheelParent;						// Parent of the wheel, used to attach the SwipeableWheel script to
	[SerializeField] protected Transform wheelTransform; 					// Used to get the size for the swipeToSpin Feature.
	[SerializeField] protected float wheelPointerAngleStart = 22.5f;
	[SerializeField] protected float wheelPointerAngelEnd = 44f;
	[SerializeField] protected float pointerAngleMax = 35f;
	[SerializeField] protected bool isPointerMovedWithSpin = false;		// Controls if the pointer will be moved like it is hitting pegs, some wheel games have fixed pointers
	[SerializeField] protected bool isPlayingCrowdNoises = true;		// Controls if crowd noises are played, Joe doesn't seem to like these much anymore
	[SerializeField] protected bool playWheelStartAnimationBeforeWheelSpin = false;
	[SerializeField] protected bool setSwipeableSizeFromTargetCollider = false;	// Controls whether to use the local transform scale on the wheelTransform target or an attached BoxCollider size

	protected WheelPick wheelPick = null;				// The wheel pick, data used for the wheel spin
	protected List<long> progressivePoolValues = new List<long>();		// Stores out the win values of the progressives
	protected float degreesPerSlice = 0;				// The number of degrees per slice on the wheel
	protected long finalPayout = 0;						// The final payout of this wheel game, may be a combination if the wheel game has multiple spins

	private WheelOutcome outcome;						// Outcome for this bonus game
	private SwipeableWheel swipeableWheel;				// Stores the controller for swipping and spinning the wheel
	private WheelSpinner spinner;
	private bool isClockWise = true;					// Used to deterimine the direction of the spin for the flippper.
	
	private const float TIME_TO_SHOW_WINNING_SLICE_ANIM = 2.0f;		// The time to show the winning slice animation

	public override void init() 
	{
		degreesPerSlice = 360 / numSlices;

		outcome = (WheelOutcome)BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE];
		wheelPick = outcome.getNextEntry();

		int wheelIdx = 0;
		for (int j = 0; j < wheelPick.wins.Count; j++)
		{
			long credits = wheelPick.wins[j].credits;
			if (credits == 0)
			{
				continue;
			}
			wheelTexts[wheelIdx].text = CommonText.makeVertical(CreditsEconomy.convertCredits(credits, false));
			wheelIdx++;
		}

		if (winText != null)
		{
			winText.text = "0";
		}

		if (winBox != null)
		{
			winBox.SetActive(false);
		}

		if (wheelWinBoxAnimation != null)
		{
			wheelWinBoxAnimation.SetActive(false);
		}
		
		JSON[] progressivePoolsJSON = SlotBaseGame.instance.slotGameData.progressivePools; // These pools are from highest to lowest.

		if (progressivePoolsJSON.Length > 0)
		{
			for (int i = 0; i < progressivePoolsJSON.Length; i++)
			{
				progressivePoolValues.Add(SlotsPlayer.instance.progressivePools.getPoolCredits(progressivePoolsJSON[i].getString("key_name", ""), SlotBaseGame.instance.multiplier, false));
			}
		}
		else
		{
			// If it didn't the progressive pools from the slot game data,
			// then try to get them from the wheel pick.
			foreach (WheelPick wheelPickWin in wheelPick.wins)
			{
				if (wheelPickWin.extraData != "")
				{
					long aProgPoolAmount = long.Parse(wheelPickWin.extraData);
					aProgPoolAmount *= GameState.baseWagerMultiplier;
					aProgPoolAmount *= SlotBaseGame.instance.multiplier;
					
					progressivePoolValues.Add(aProgPoolAmount);
				}
			}
			
			progressivePoolValues.Sort();
			progressivePoolValues.Reverse();
		}

		// fill in the text for the progressives
		if (progressivePoolValues.Count == progressivePoolTexts.Length)
		{
			for (int i = 0; i < progressivePoolValues.Count; i++)
			{
				progressivePoolTexts[i].text = CreditsEconomy.convertCredits(progressivePoolValues[i]);
			}
		}

		_didInit = true;
	}

	/// Starts the game after initialization is finished.
	protected override void startGame()
	{
		base.startGame();
		enableSpinButton(true); // Needs to go in the Start functuion for Swipe to spin to work b/c camera may not be set in awake.
	}

	/// Turn the spin button on
	protected void enableSpinButton(bool state)
	{
		if (state)
		{
			enableSwipeToSpin();
		}
		if (spinButton != null)
		{
			spinButton.isEnabled = state;
		}

		if (spinButtonFlasher != null)
		{
			spinButtonFlasher.enabled = state;
		}
	}

	protected override void Update()
	{
		base.Update();

		if (!_didInit)
		{
			return;
		}

		if (swipeableWheel != null && spinner == null)
		{
			spinner = swipeableWheel.wheelSpinner;
		}

		if (spinner != null)
		{
			float pointerAngle = 0f;
			
			spinner.updateWheel();
			//After updating, if spinner is null, it is complete.
			if (spinner != null && isPointerMovedWithSpin)
			{
				// Figure out the pointer's angle.
				float wheelAngle = Mathf.Abs(spinner.RotationAngle % degreesPerSlice);
												
				if (wheelAngle >= wheelPointerAngleStart && wheelAngle <= wheelPointerAngelEnd)
				{
					// The pointer is caught on a peg, so the peg angle is based on the wheel angle.
					// How far into the peg range is the wheel?
					float normalized = CommonMath.normalizedValue(wheelPointerAngleStart, wheelPointerAngelEnd, wheelAngle);
					
					// Use the same normalized value to determine the pointer angle.
					pointerAngle = Mathf.Lerp(0, pointerAngleMax, normalized);
					
					// Adjust for the direction the wheel is spinning.
					if (!isClockWise)
					{
						pointerAngle = -pointerAngle;
					}
				}
			}

			// Set this outside of all the logic above, to make sure the pointer is straight
			// when the wheel is done spinning.
			if (wheelPointer != null)
			{
				wheelPointer.transform.localEulerAngles = new Vector3(0, 0, pointerAngle);
			}
		}
	}
	
	/// Called when the wheel stops spinnin
	protected virtual void onWheelSpinComplete()
	{
		//Cleanup spinner
		spinner = null;

		if (outcome.entryCount == 0)
		{
			StartCoroutine(rollupAndEnd());
		}
		else
		{
			// more spins still, so rollup and then wait for the next spin
			StartCoroutine(rollupAndWaitForNextSpin());
		}
	}

	/// Rollup the payout
	protected virtual IEnumerator animateScore()
	{
		long payout = wheelPick.wins[wheelPick.winIndex].credits;

		if (payout > 0)
		{
			if (wheelWinBoxAnimation != null)
			{
				wheelWinBoxAnimation.SetActive(true);
			}

			if (winLabel != null)
			{
				winLabel.gameObject.SetActive(true);
			}

			BonusGamePresenter.instance.currentPayout = finalPayout + payout;
			if (winText != null)
			{
				winText.gameObject.SetActive(true);
				winText.enabled = true;
				yield return StartCoroutine(SlotUtils.rollup(finalPayout, BonusGamePresenter.instance.currentPayout, winText));
			}

			finalPayout += payout;
		}
	}

	/// Rollup the amount and then do the next spin
	protected virtual IEnumerator rollupAndWaitForNextSpin()
	{
		yield return StartCoroutine(showWinSliceAnimation());

		yield return StartCoroutine(animateScore());

		// enable spinning again
		enableSpinButton(true);
	}

	/// Show the slice winning animation, if there is one
	protected IEnumerator showWinSliceAnimation()
	{
		if (winSliceAnimation != null)
		{
			winSliceAnimation.SetActive(true);
			yield return new TIWaitForSeconds(TIME_TO_SHOW_WINNING_SLICE_ANIM);
			winSliceAnimation.SetActive(false);
		}
		else
		{
			yield break;
		}
	}

	// For the last spin, or single spin on single spin wheels, rollup and end the game
	protected virtual IEnumerator rollupAndEnd()
	{
		yield return StartCoroutine(showWinSliceAnimation());

		yield return StartCoroutine(animateScore());
		
		yield return new WaitForSeconds(0.5f);
		BonusGamePresenter.instance.gameEnded();
	}

	/// Get the final degrees of the wheel, virtual in case you need to apply an adjustment for your game
	protected virtual float getFinalSpinDegress()
	{
		return wheelPick.winIndex * degreesPerSlice;
	}

	/// Enable the use of swiping the wheel to spin it
	private void enableSwipeToSpin()
	{
		float finalDegrees = getFinalSpinDegress();

		swipeableWheel = wheelParent.GetComponent<SwipeableWheel>();
		if (swipeableWheel == null)
		{
			initSwipeableWheel();
		}
		else
		{
			spinner = null;
			swipeableWheel.enableSwipe(true);
			swipeableWheel.resetSpinResultInfo(wheelParent, finalDegrees, 
				onSwipeStart, onWheelSpinComplete);
		}
	}

	/// Allows the swipeable wheel to be reinitialized, needing in some wheel games that move the wheel onto the screen as an intro
	protected void initSwipeableWheel()
	{
		float finalDegrees = getFinalSpinDegress();

		if (swipeableWheel == null)
		{
			swipeableWheel = wheelParent.AddComponent<SwipeableWheel>();
		}

		swipeableWheel.init(wheelParent, finalDegrees, 
			onSwipeStart, onWheelSpinComplete, wheelTransform, null, isPlayingCrowdNoises, setSwipeableSizeFromTargetCollider);
	}

	// Called when the wheels are swiped
	protected virtual void onSwipeStart()
	{
		spinner = swipeableWheel.wheelSpinner;
		enableSpinButton(false);
		isClockWise = swipeableWheel.direction < 0;
		StartCoroutine(processSpin());
	}

	/// Handles changing out what is visible after the spin button has been pressed
	protected virtual IEnumerator processSpin()
	{
		//Audio.play(Audio.soundMap("wheel_spin_animation"));
		if(playWheelStartAnimationBeforeWheelSpin == true)
		{
			playWheelStartAnimation();
			yield return new TIWaitForSeconds(0.5f);
		}

		if (winBox != null)
		{
			winBox.SetActive(true);
		}

		if (topPointerAnimation != null)
		{
			topPointerAnimation.SetActive(true);
		}

		yield return new TIWaitForSeconds(0.5f);

		if (playWheelStartAnimationBeforeWheelSpin == false)
		{
			playWheelStartAnimation();
		}

		yield return new TIWaitForSeconds(0.5f);

		yield return new TIWaitForSeconds(1.5f);
	}

	private void playWheelStartAnimation()
	{
		if (wheelStartAnimationObject != null)
		{
			// flip the animaiton if the the direction is different, caused by a swip to spin
			if (!isClockWise)
			{
				Vector3 currentScale = wheelStartAnimationObject.transform.localScale;
				wheelStartAnimationObject.transform.localScale = new Vector3(-currentScale.x, currentScale.y, currentScale.z);
			}
			
			wheelStartAnimationObject.SetActive(true);
		}
	}

	/// Removes the swipeablewheel object from wheel. 
	private void disableSwipeToSpin()
	{
		//Remove the swipeable wheel because we shouldn't be able to move it anymore.
		// First check to see if it's attached to the wheel
		SwipeableWheel swipeableWheel = wheel.GetComponent<SwipeableWheel>() ;
		
		// If we didn't find it...
		if(swipeableWheel == null)
		{
			// Try the wheel parent...
			swipeableWheel = wheelParent.GetComponent<SwipeableWheel>();
			
			// NOTE: This may be a bug. Above we attach the swipeablewheel component to the wheelParent NOT the wheel!
		}
		
		if (swipeableWheel != null)
		{
			Destroy(swipeableWheel);
		}
	}

	/// Called by NGUI spin button message.
	private void spinClicked()
	{
		if (spinButton.isEnabled)
		{
			isClockWise = true;
			enableSpinButton(false);

			// We don't want to have two wheels being updated.
			disableSwipeToSpin();

			StartCoroutine(onSpinClickedCoroutine());
		}
	}

	/// Coroutine for clicking the NGUI spin button, allows for elements that need timing
	/// Always call StartCoroutine(startSpinFromClickCoroutine) at the end of your function if you override this function
	protected virtual IEnumerator onSpinClickedCoroutine()
	{
		Audio.play(Audio.soundMap("wheel_click_to_spin"));

		StartCoroutine(startSpinFromClickCoroutine());

		yield break;
	}

	protected IEnumerator startSpinFromClickCoroutine()
	{
		spinner = new WheelSpinner(wheelParent, getFinalSpinDegress(), onWheelSpinComplete, false, -80.0f, 0.0f, isPlayingCrowdNoises);
		yield return StartCoroutine(processSpin());
	}
}
