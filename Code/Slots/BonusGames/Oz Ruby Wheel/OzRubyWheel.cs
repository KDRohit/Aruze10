using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class OzRubyWheel : ChallengeGame 
{

	public UIButton spinButton;
	public TweenColor spinButtonFlasher;
	public GameObject wheelParent;
	public UISprite wheelSprite; //Used to get the size for the swipeToSpin Feature.
	public GameObject wheelPointer;
	public UILabel[] wheelText;	// To be removed when prefabs are updated.
	public LabelWrapperComponent[] wheelTextWrapperComponent;

	public List<LabelWrapper> wheelTextWrapper
	{
		get
		{
			if (_wheelTextWrapper == null)
			{
				_wheelTextWrapper = new List<LabelWrapper>();

				if (wheelTextWrapperComponent != null && wheelTextWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in wheelTextWrapperComponent)
					{
						_wheelTextWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in wheelText)
					{
						_wheelTextWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _wheelTextWrapper;
		}
	}
	private List<LabelWrapper> _wheelTextWrapper = null;	
	
	public GameObject startSpinParticles;
	public GameObject wheelWinParticles;
	public GameObject[] wheelRevealParticles;
	public UILabel finalWinText;	// To be removed when prefabs are updated.
	public LabelWrapperComponent finalWinTextWrapperComponent;

	public LabelWrapper finalWinTextWrapper
	{
		get
		{
			if (_finalWinTextWrapper == null)
			{
				if (finalWinTextWrapperComponent != null)
				{
					_finalWinTextWrapper = finalWinTextWrapperComponent.labelWrapper;
				}
				else
				{
					_finalWinTextWrapper = new LabelWrapper(finalWinText);
				}
			}
			return _finalWinTextWrapper;
		}
	}
	private LabelWrapper _finalWinTextWrapper = null;
	
	public GameObject[] slipperGraphicObjects;
	public UILabel titleLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent titleLabelWrapperComponent;

	public LabelWrapper titleLabelWrapper
	{
		get
		{
			if (_titleLabelWrapper == null)
			{
				if (titleLabelWrapperComponent != null)
				{
					_titleLabelWrapper = titleLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_titleLabelWrapper = new LabelWrapper(titleLabel);
				}
			}
			return _titleLabelWrapper;
		}
	}
	private LabelWrapper _titleLabelWrapper = null;
	

	private bool isClockWise = true;				// Used to deterimine the direction of the spin for the flippper.
	private WheelSpinner spinner;
	private WheelOutcome outcome;
	private long[] sliceOutcomes;
	private WheelPick wheelPick;
	private long winningAmount = 0;
	private float sliceDegreeTotal;
	private SwipeableWheel swipeableWheel;

	// Constant variables
	private const float WHEEL_ANGLE_PEG_START = 22.5f;
	private const float WHEEL_ANGLE_PEG_END = 44f;
	private const float POINTER_ANGLE_MAX = 35f;
	private const float MUSIC_AFTER_SPIN_DELAY = 1.0f;						// How long to wait after the spin to change the music playing.
	private const float TIME_BETWEEN_REVEALS = 0.5f;						// How much time we wait in between each reveal to speed up the game.
	private const float TIME_AFTER_GAME = 0.5f;								// How long we wait once the game ends before we pop up the summary icon.
	private const int FIRST_SLIPPER_INDEX = 3;								// The index of the first slipper.
	private const int SECOND_SLIPPER_INDEX = 7;								// The index of the second slipper.
	private const float WHEEL_FINAL_ANGLE = 225.0f;							// The amount that we add to each pick to get the correct spot.
	private const float DEGREES_PER_SLICE = 45.0f;							// Number of slices (360 / 8)
	// Sound names
	private const string BACKGROUND_MUSIC = "ECwheelclick2spin0";			// Sound name for the background music played on init.
	private const string SPINNING_MUSIC = "ECspinwheel0";					// Spinning has a different track than the normal background
	private const string ENDING_MUSIC = "ECwheelclick2spin0";				// Name of music played after the spin stops.
	private const string UPGRADE_LAND_SOUND = "ECrubyslipperupgrade0";		// Name of the sound played when we land on the upgrade slice.
	private const string UPGRADE_SOUND = "wheelspokesparklies";				// Name of sound played when each value gets upgraded.
	
	public override void init() 
	{		
		outcome = (WheelOutcome)BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE];
		wheelPick = outcome.getNextEntry();

		sliceOutcomes = new long[wheelPick.wins.Count];
		
		//is equated from degrees in a cirle / numbers of slice on the wheel
		sliceDegreeTotal = 360 / sliceOutcomes.Length;

		for(int j = 0; j < wheelPick.wins.Count; j++)
		{
			if(wheelPick.wins[j].credits == 0)
			{
				continue;
			}
			wheelTextWrapper[j].text = CommonText.makeVertical(CreditsEconomy.convertCredits(wheelPick.wins[j].credits, false));
		}
		enableSpinButton(true);

		Audio.switchMusicKeyImmediate(BACKGROUND_MUSIC);

		_didInit = true;
		isClockWise = false;
	}

	public void onWheelSpinComplete()
	{
		// Make sure that the sound stops.
		Audio.stopSound(Audio.findPlayingAudio("wheel_decelerate"));
		disableSwipeToSpin();
		//Cleanup spinner
		spinner = null;
		
		//Update wheel picks
		wheelPick = outcome.getNextEntry();

		toggleWinSlice(true);

		if (wheelPick != null)
		{
			StartCoroutine(doRevealSliceParticles());
		}
		else
		{
			BonusGamePresenter.instance.currentPayout = winningAmount;
			StartCoroutine(rollupAndEnd());
		}
		// Play the music after the spin after a short delay
		Audio.switchMusicKey(ENDING_MUSIC);
		Audio.playMusic(ENDING_MUSIC, MUSIC_AFTER_SPIN_DELAY);
	}

	// Called when the wheels are swiped
	private void onSwipeStart()
	{
		this.spinner = swipeableWheel.wheelSpinner;
		enableSpinButton(false);
		isClockWise = swipeableWheel.direction < 0;
		processSpin();
	}

	private void enableSwipeToSpin()
	{
		float finalDegrees = wheelPick.winIndex * DEGREES_PER_SLICE + WHEEL_FINAL_ANGLE;
		swipeableWheel = wheelParent.GetComponent<SwipeableWheel>();
		if (swipeableWheel == null)
		{
			swipeableWheel = wheelParent.AddComponent<SwipeableWheel>();
			swipeableWheel.init(wheelParent,finalDegrees, 
				onSwipeStart,onWheelSpinComplete,wheelSprite);
		}
		else
		{
			spinner = null;
			swipeableWheel.enableSwipe(true);
			swipeableWheel.resetSpinResultInfo(wheelParent,finalDegrees, 
				onSwipeStart,onWheelSpinComplete);
		}
	}

	private void disableSwipeToSpin()
	{
		swipeableWheel = wheelParent.GetComponent<SwipeableWheel>();
		if(swipeableWheel != null)
		{
			swipeableWheel.enableSwipe(false);
		}
	}

	private void enableSpinButton(bool state)
	{
		if (state)
		{
			enableSwipeToSpin();
		}
		spinButton.isEnabled = state;
		spinButtonFlasher.enabled = state;
	}
	
	//Called by NGUI spin button message.
	private void spinClicked()
	{
		if (spinButton.isEnabled)
		{
			isClockWise = true;
			enableSpinButton(false);
			// We don't want to have two wheels being updated.
			disableSwipeToSpin();
			float finalDegrees = wheelPick.winIndex * DEGREES_PER_SLICE + WHEEL_FINAL_ANGLE;
			spinner = new WheelSpinner(wheelParent, finalDegrees, onWheelSpinComplete);
			processSpin();
		}
	}

	// Does the meat of the spin call so that swipe and pressing the spin button do the same thing.
	private void processSpin()
	{
		titleLabelWrapper.text = Localize.textUpper("good_luck");
		StartCoroutine(doStartSpinParticle());
		winningAmount = wheelPick.wins[wheelPick.winIndex].credits;
		Audio.switchMusicKey(SPINNING_MUSIC);
		// We want to play this music right away and avoid the fade out.
		Audio.playMusic(SPINNING_MUSIC, 0);
	}
	
	private IEnumerator doStartSpinParticle()
	{
		startSpinParticles.SetActive(true);
		Animation anim = startSpinParticles.GetComponentInChildren<Animation>();
		yield return new TIWaitForSeconds(anim.clip.length);
		startSpinParticles.SetActive(false);
	}

	private IEnumerator revealSlice(LabelWrapper label, long value, GameObject particleEffect)
	{
		particleEffect.SetActive(true);
		Animation anim = particleEffect.GetComponentInChildren<Animation>();
		label.text =  CommonText.makeVertical(CreditsEconomy.convertCredits(value, false));
		Audio.play(UPGRADE_SOUND);
		yield return new TIWaitForSeconds(anim.clip.length);
		particleEffect.SetActive(false);
		Audio.play(UPGRADE_SOUND);
	}

	private IEnumerator doRevealSliceParticles()
	{
		Audio.play(UPGRADE_LAND_SOUND);

		for (int i = 0; i < wheelPick.wins.Count; i++)
		{
			long winValue = wheelPick.wins[i].credits;
			// If the value is 0 then it's a slipper before the last pick.
			if (winValue == 0)
			{
				continue;
			}
			// When you hit the 4th slipper we want to get rid of the slippers.
			// While turning on the text underneath the slipper
			if (i == FIRST_SLIPPER_INDEX)
			{
				wheelTextWrapper[i].gameObject.SetActive(true);
				slipperGraphicObjects[0].SetActive(false);
			}
			else if (i == SECOND_SLIPPER_INDEX)
			{
				wheelTextWrapper[i].gameObject.SetActive(true);
				slipperGraphicObjects[1].SetActive(false);
			}
			StartCoroutine(revealSlice(wheelTextWrapper[i], winValue, wheelRevealParticles[i]));
			yield return new TIWaitForSeconds(TIME_BETWEEN_REVEALS);
		}

		titleLabelWrapper.text = Localize.textUpper("touch_spin_to_start_wheel");
		enableSpinButton(true);
		toggleWinSlice(false);
	}

	private void toggleWinSlice(bool isActive)
	{
		wheelWinParticles.SetActive(isActive);
	}

	/// Do the winnings rollup then end the game.
	private IEnumerator rollupAndEnd()
	{
		yield return StartCoroutine(SlotUtils.rollup(0, BonusGamePresenter.instance.currentPayout, finalWinTextWrapper));

		yield return new TIWaitForSeconds(TIME_AFTER_GAME);

		BonusGamePresenter.instance.gameEnded();
	}

	protected override void Update()
	{
		base.Update();
		if (swipeableWheel != null && spinner == null)
		{
			spinner = swipeableWheel.wheelSpinner;
		}

		if (spinner != null)
		{
			float pointerAngle = 0f;
			
			spinner.updateWheel();
			//After updating, if spinner is null, it is complete.
			if (spinner != null)
			{
				// Figure out the pointer's angle.
				float wheelAngle = Mathf.Abs(spinner.RotationAngle % sliceDegreeTotal);
												
				if (wheelAngle >= WHEEL_ANGLE_PEG_START && wheelAngle <= WHEEL_ANGLE_PEG_END)
				{
					// The pointer is caught on a peg, so the peg angle is based on the wheel angle.
					// How far into the peg range is the wheel?
					float normalized = CommonMath.normalizedValue(WHEEL_ANGLE_PEG_START, WHEEL_ANGLE_PEG_END, wheelAngle);
					
					// Use the same normalized value to determine the pointer angle.
					pointerAngle = Mathf.Lerp(0, POINTER_ANGLE_MAX, normalized);
					
					// Adjust for the direction the wheel is spinning.
					if (!isClockWise)
					{
						pointerAngle = -pointerAngle;
					}
				}
			}

			// Set this outside of all the logic above, to make sure the pointer is straight
			// when the wheel is done spinning.
			wheelPointer.transform.localEulerAngles = new Vector3(0, 0, pointerAngle);
		}
	}
}

