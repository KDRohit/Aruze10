using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Class created to handle the workings of a single wheel in a ModularWheelGameVariant
This allows this class to handle modules that should only affect one wheel in a game
that has more than one wheel in it.

Creation Date: 2/8/2018
Original Author: Scott Lepthien
*/
public class ModularWheel : TICoroutineMonoBehaviour 
{
	[SerializeField] private GameObject wheelToSpin; // gameobject to spin with the WheelSpinner
	[SerializeField] private int WHEEL_SLICES; // number of slices for rotation offset
	[HideInInspector] [SerializeField] private int OLD_WHEEL_SLICES; // Used to keep track if the number of wheel slices has changed since this was updated.
	[SerializeField] private AnimationListController.AnimationInformationList wheelStartAnimations;
	[SerializeField] private bool handleWheelStopSoundsInModule = false;
	[SerializeField] private bool playCrowdNoises = false; // for the most part Joe wanted these taken out a while back, but it seems like some TV sound spec games want it back, so I'm adding a way to toggle it
	[Tooltip("Controls if the wheel plays the wheel start sounds, useful if more than one wheel is spinning and we only want one to play the start sounds.")]
	[SerializeField] private bool isPlayingWheelStartSounds = true;
	[Tooltip("Controls if the wheel click looping sound should play, useful if more than one wheel is spinning and we only want one to play the loop sound.")]
	[SerializeField] private bool isPlayingWheelLoopSound = true;
	[Tooltip("Changes the wheel slowing sound key.")]
	[SerializeField] private string wheelSlowsToStopSoundKey = WheelSpinner.WHEEL_SLOWS_TO_STOP_SOUND_KEY;
	[Tooltip("Use this to add an additional offset to every stop angle to make the art land in the middle.")]
	[SerializeField] private float stopAngleOffset = 0.0f;
	[Tooltip("Time the wheel will remain spinning at a constant velocity before calculating how to slow the wheel to a stop.")]
	[SerializeField] private float constantVelocitySpinTime = 0.5f;
	[Tooltip("Controls if the dialog is playing its own stop sound for the wheel.")]
	[SerializeField] private bool useCustomStopSound = false;

	[System.NonSerialized] public WheelSpinner wheelSpinner;
	[System.NonSerialized] public bool isSpinComplete = false;
	[System.NonSerialized] public ModularWheelGameVariant parentWheelGameVariant = null;

	private List<WheelGameModule> cachedAttachedWheelModules = new List<WheelGameModule>();
	private bool isSpinStarted = false;
	private bool _isSpinningClockwise = true;

	public bool isSpinningClockwise
	{
		get { return _isSpinningClockwise; }
	}

	public bool isSpinning
	{
		get { return isSpinStarted && !isSpinComplete; }
	}

	public void init(ModularWheelGameVariant wheelGameVariant)
	{
		WheelGameModule[] wheelGameModulesArray = GetComponents<WheelGameModule>();
        //Clear cached modules first
        cachedAttachedWheelModules.Clear();

		for (int i = 0; i < wheelGameModulesArray.Length; i++)
		{
			cachedAttachedWheelModules.Add(wheelGameModulesArray[i]);
		}
		
		parentWheelGameVariant = wheelGameVariant;

		for (int i = 0; i < cachedAttachedWheelModules.Count; i++)
		{
			WheelGameModule module = cachedAttachedWheelModules[i];

			if (module.needsToExecuteOnRoundInit())
			{								
				module.executeOnRoundInit(wheelGameVariant, this);
			}
		}

		isSpinStarted = false;
	}

	public IEnumerator start()
	{
		for (int i = 0; i < cachedAttachedWheelModules.Count; i++)
		{
			WheelGameModule module = cachedAttachedWheelModules[i];

			if (module.needsToExecuteOnRoundStart())
			{
				yield return StartCoroutine(module.executeOnRoundStart());
			}
		}
	}

	public IEnumerator onRoundEnd(bool isEndOfGame)
	{
		for (int i = 0; i < cachedAttachedWheelModules.Count; i++)
		{
			WheelGameModule module = cachedAttachedWheelModules[i];

			if (module.needsToExecuteOnRoundEnd(isEndOfGame))
			{
				yield return StartCoroutine(module.executeOnRoundEnd(isEndOfGame));
			}
		}
	}

	// Callback for spin clicks from a button.
	public void spinButtonPressed()
	{
		if (isSpinStarted)
		{
			// ignore this, don't spin again!
			return;
		}

		_isSpinningClockwise = getSpinDirection(true);

		isSpinStarted = true;

		// start the actual spin
		StartCoroutine(spinStart(computeRequiredRotation()));
	}

	// We have to make sure we have initialized our cachedAttachedWheelModules
	// before allowing any spin to happen.
	public bool canPressSpinButton()
	{
		if (isSpinStarted || cachedAttachedWheelModules == null)
		{
			return false;
		}

		return true;
	}

	// start a spin from a swipe with a custom start velocity
	public void spinSwipe(float startingVelocity, bool passedIsSpinningClockwise)
	{
		// ignore double-spin actions
		if (isSpinStarted)
		{
			return;
		}

		_isSpinningClockwise = getSpinDirection(passedIsSpinningClockwise);

		isSpinStarted = true;

		StartCoroutine(spinStart(computeRequiredRotation(), startingVelocity));

		// Tell the parent that a swipe spin occured so it can spin other reels if they also need to spin
		parentWheelGameVariant.spinAllWheelsFromSwipe(this, startingVelocity, isSpinningClockwise);
	}

	// perform the spin via a WheelSpinner
	private IEnumerator spinStart(float winRotation, float startingVelocity = 0.0f)
	{
		bool wheelSpunFromModule = false;

		if (wheelStartAnimations != null && wheelStartAnimations.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(wheelStartAnimations));
		}

		// activate spin modules
		bool wasSwipeToSpin = false;
		for (int i = 0; i < cachedAttachedWheelModules.Count; i++)
		{
			WheelGameModule module = cachedAttachedWheelModules[i];

			if (module.needsToExecuteOnSpin())
			{								
				yield return StartCoroutine(module.executeOnSpin());
			}

			// check for the WheelGameSwipeModule at the same time
			// need to cast here, since foreach nulls if trying to filter by derived type.
			WheelGameSwipeModule swipeModule = module as WheelGameSwipeModule;
			if (swipeModule != null && swipeModule.swipeableWheel != null && swipeModule.swipeableWheel.wheelSpinner != null)
			{
				wasSwipeToSpin = true;
				wheelSpinner = swipeModule.swipeableWheel.wheelSpinner;
			}
		}

		if (wheelToSpin != null)
		{
			if (!wasSwipeToSpin)
			{
				// No animator for spinning the wheel, use a WheelSpinner 
				wheelSpinner = new WheelSpinner(wheelToSpin, 
					winRotation, 
					onSpinComplete, 
					!isSpinningClockwise, 
					-80.0f, 0.0f, 
					playCrowdNoises: playCrowdNoises, 
					handleWheelStopSoundsInModule: handleWheelStopSoundsInModule,
					dialogWheelHandlesCustomSounds: false,
					isPlayingWheelStartSounds: isPlayingWheelStartSounds,
					isPlayingWheelLoopSound: isPlayingWheelLoopSound,
					wheelSlowsToStopSoundKey: wheelSlowsToStopSoundKey,
					useCustomStopSound: useCustomStopSound);
				wheelSpinner.setStartingAngularVelocity(startingVelocity);
			}

			wheelSpinner.constantVelocitySeconds = constantVelocitySpinTime;

			yield return StartCoroutine(wheelSpinner.waitToStop());
		}
		else
		{
			onSpinComplete();
		}
	}

	// Some games may want to override how to get the angles for each slice.
	private float getStopAngle(ModularChallengeGameOutcomeRound currentRound)
	{
		for (int i = 0; i < cachedAttachedWheelModules.Count; i++)
		{
			// need to cast here, since foreach nulls if trying to filter by derived type.
			WheelGameModule wheelModule = cachedAttachedWheelModules[i];
			if (wheelModule.needsToSetCustomAngleForFinalStop(currentRound))
			{
				return wheelModule.executeSetCustomAngleForFinalStop(currentRound) + stopAngleOffset;
			}
		}

		if (currentRound != null && currentRound.entries != null && currentRound.entries.Count > 0)
		{
			return ((360.0f / WHEEL_SLICES) * currentRound.entries[0].wheelWinIndex) + stopAngleOffset;
		}
		else
		{
			Debug.LogError("ModularWheel.getStopAngle() - Issue reading data from currentRound!  Returning 0.");
			return 0;
		}
	}

	// Computes the required rotation for the wheel to stop on a winning slice.
	public float computeRequiredRotation()
	{
		ModularChallengeGameOutcomeRound currentRound = null;
		if (parentWheelGameVariant.outcome != null)
		{
			currentRound = parentWheelGameVariant.outcome.getCurrentRound();
		}
		
		float finalAngle = getStopAngle(currentRound);
		if (!isSpinningClockwise)
		{
			finalAngle = 360.0f - finalAngle;
		}

		return finalAngle;
	}

	// Called when the spin has finished via the delegate
	public void onSpinComplete()
	{
		StartCoroutine(spinComplete());
	}

	// Coroutine when spin complete
	private IEnumerator spinComplete()
	{
		for (int i = 0; i < cachedAttachedWheelModules.Count; i++)
		{
			// need to cast here, since foreach nulls if trying to filter by derived type.
			WheelGameModule wheelModule = cachedAttachedWheelModules[i];
			if (wheelModule.needsToExecuteOnSpinComplete())
			{								
				yield return StartCoroutine(wheelModule.executeOnSpinComplete());
			}
		}

		// clear the stored spinner variable
		wheelSpinner = null;

		isSpinComplete = true;

		parentWheelGameVariant.checkForAllWheelsComplete();
	}

	// Return the wheel slices, useful for calculating the stop location in a module
	public int getNumberOfWheelSlices()
	{
		return WHEEL_SLICES;
	} 

	// Get the spin direction, allowing for a module to override the spin direction
	private bool getSpinDirection(bool isCurrentSpinDirectionClockwise)
	{
		for (int i = 0; i < cachedAttachedWheelModules.Count; i++)
		{
			WheelGameModule module = cachedAttachedWheelModules[i];

			if (module.needsToExecuteOnOverrideSpinDirection(isCurrentSpinDirectionClockwise))
			{
				return module.executeOnOverrideSpinDirection(isCurrentSpinDirectionClockwise);
			}
		}

		return isCurrentSpinDirectionClockwise;
	}
	
	// Allows for modules to be fully removed form the cached list if they are destroyed
	public void removeWheelGameModule(WheelGameModule module)
	{
		if (module != null)
		{
			cachedAttachedWheelModules.Remove(module);
		}
	}

	///// Editor Update Loops ///////
	private void OnValidate()
	{
		if (WHEEL_SLICES != OLD_WHEEL_SLICES)
		{
			// need to get new modules becuase the game might not be running.
			foreach (WheelGameModule wheelModule in GetComponents<WheelGameModule>())
			{
				if (wheelModule.needsToExecuteOnNumberOfWheelSlicesChanged(WHEEL_SLICES))
				{
					wheelModule.executeOnNumberOfWheelSlicesChanged(WHEEL_SLICES);
				}
			}
			if (Application.isPlaying)
			{
				Debug.LogErrorFormat("# of wheel slices changes from {0} to {1} while the game was running.", OLD_WHEEL_SLICES, WHEEL_SLICES);
			}
			OLD_WHEEL_SLICES = WHEEL_SLICES;
		}
	}
}
