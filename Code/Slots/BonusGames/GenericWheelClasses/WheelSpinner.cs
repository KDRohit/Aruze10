using UnityEngine;
using System.Collections;

public class WheelSpinner
{
	protected GameObject _wheel;

	// motion variables.
	protected float acceleration = 0f;
	protected float angularVelocity = 0f;
	protected float rotationAngle = 0f;

	// time step
	protected float frameTime = 1.0f / 30.0f;

	protected int prevTimeMs;
	protected int timeMs = 0;

	// variables for State.ACCELERATE_START
	// This occurs in the very beginning a slow start acceleration before the
	// wheel begins to spin fast
	public float accelerationSlowStartSeconds = 0.25f;
	public float accelerationSlowStart = 40f;	// degrees per second squared.

	// variables for State.ACCELERATE
	// This is when the wheel picks up speed.
	// It will have this acceleration until the maxAngularVelocity is met
	public float accelerationStart = 800f;	// degrees per second squared.
	public float maxAngularVelocity = 500.0f;	// degrees per second.

	// variables for the State.CONSTANT_VELOCITY
	// This is where the wheel will at a constant maxAngularVelocity
	// will spin for constantVelocitySeconds seconds.
	public float constantVelocitySeconds = 0.5f;

	// variables for the State.DECELERATE
	public float deceleration = -80.0f;

	// variables for the State.DECELERATE_FINISH
	// this is the time at which the wheel having stopped decelerating
	// will spin at minAngularVelocity for slowVelocitySeconds seconds.
	public float minAngularVelocity = 18.0f;
	public float slowVelocitySeconds = .5f;

	// Cached input values.
	protected float _desiredDegrees = 0.0f;	// Degrees to stop the wheel spin.
	protected bool _ccw = false;			// Counter clockwise. Use with care. Joe doesn't like it.

	protected float time;
	protected float _slowdownRotation;
	protected float _targetAngle;
	
	protected bool isDialogWheel = false;
	protected bool isDialogUsingCustomSounds = false; //Used for dialog wheels that have custom sound choreography
	protected bool useCustomStopSound = false; //Used for dialog wheels that have custom sound choreography
	protected bool isPlayingCrowdNoises = true;


	protected string wheelStart = "wheel_start";
	protected string wheelLoop = "wheel_loop";
	protected string wheelDecelerateThreeSecs = "wheel_decelerate_3sec";
	protected string wheelStop = "wheel_stop";
	protected string wheelSlowMusic = "";
	protected bool isPlayingWheelStartSounds = true;
	protected bool isPlayingWheelLoopSound = true;
	protected string wheelSlowsToStopSoundKey = WHEEL_SLOWS_TO_STOP_SOUND_KEY;

	protected bool handleWheelStopSoundsInModule = false;
	
	protected string wowWheelSpin = "WoW_spin_wheel";
	protected string wheelDecelerate = "wheel_decelerate";

	protected PlayingAudio wheelDeceleratePlayingAudio;

	public const string WHEEL_SLOWS_TO_STOP_SOUND_KEY = "wheel_slows_to_stop";
	
	// Wheel Spin States
	public enum State
	{
		IDLE = 0,
		ACCELERATE_START = 1,	// Used for a slow start acceleration period
		ACCELERATE = 2,			// Used for a faster acceleration period
		CONSTANT_VELOCITY = 3,	// Used to for a time where the wheel can spin cv.
		SEEK_ANGLE = 4,			// Used to sync up the end rotation.
		DECELERATE = 5,			// Used to decelerate using constant acc.
		DECELERATE_FINISH = 6,	// Used to have a constant velocity period at the end.
		LOCK_INTO_PLACE = 7		// Not implemented yet.
	}

	protected GenericDelegate _callback;

	public string getDecelSound
	{
		get {return wheelDecelerate;}
	}

	public float AngularVelocity
	{
		get { return angularVelocity; }
		protected set { angularVelocity = value; }
	}
	public float RotationAngle
	{
		get { return rotationAngle; }
		protected set { rotationAngle = value; }
	}

	protected const float FAST_DECELERATION_THRESHOLD = -300.0f; // The speed at which we consider a deceleration to be 'fast' so we can play the right sound clip.

	protected bool isSpecialWinDialogShowing
	{
		get
		{
			return MysteryGiftDialog.isShowing || BigSliceDialog.isShowing;
		}
	}

	// I can just use this constructor in sub classes?
	public WheelSpinner()
	{
	}
	
	public WheelSpinner(GameObject wheel, 
			float desiredDegrees, GenericDelegate callback, 
			bool ccw = false, 
			float desiredDeceleration = -80.0f, 
			float secondSoundDelays = 0.0f, 
			bool playCrowdNoises = true, 
			bool handleWheelStopSoundsInModule = false, 
			bool dialogWheelHandlesCustomSounds = false,
			bool isPlayingWheelStartSounds = true,
			bool isPlayingWheelLoopSound = true,
			string wheelSlowsToStopSoundKey = WHEEL_SLOWS_TO_STOP_SOUND_KEY,
			bool useCustomStopSound = false)
	{
		// Object to manipulate.
		_wheel = wheel;
		_callback = callback;
		deceleration = desiredDeceleration;

		isPlayingCrowdNoises = playCrowdNoises;
		this.handleWheelStopSoundsInModule = handleWheelStopSoundsInModule;
		this.isPlayingWheelStartSounds = isPlayingWheelStartSounds;
		this.isPlayingWheelLoopSound = isPlayingWheelLoopSound;
		this.wheelSlowsToStopSoundKey = wheelSlowsToStopSoundKey;
		this.useCustomStopSound = useCustomStopSound;

		//Contents of start spin are merely just copied below
		_ccw = ccw;
		_desiredDegrees = -desiredDegrees;
		_desiredDegrees = -desiredDegrees;
	
		acceleration = accelerationSlowStart;
		
		// If there is a dialog open when this wheel is spun, then it must be a special
		// dialog that isn't associated with a particular game,
		// so play standard sounds instead of sound mapping.
		isDialogWheel = (Dialog.instance.currentDialog != null);

		if (isPlayingWheelStartSounds)
		{
			if (isDialogWheel)
			{
				isDialogUsingCustomSounds = dialogWheelHandlesCustomSounds;
				if (!isDialogUsingCustomSounds)
				{
					// For daily bonus games.
					Audio.play(wheelStart);
					Audio.play(wowWheelSpin);
					Audio.play("null_fx");
				}
			}
			else // For games and bonus games.
			{
				Audio.play(Audio.soundMap("wheel_spin_animation"));
				Audio.play(Audio.soundMap("wheel_spin_start"), 1.0f, 0.0f, secondSoundDelays);
				if (!isSpecialWinDialogShowing)
				{
					// Special win dialogs handle their own special sound here.
					Audio.playMusic(Audio.soundMap("wheel_slows_music"), 0.0f, secondSoundDelays);
				}
			}
		}

		state = State.ACCELERATE_START;
		time = 0;
		// Get the angle the wheel is already rotated.
		rotationAngle = _wheel.transform.eulerAngles.z;
		// Set the rotation with this weird function.
		rotationAngle = rotFromDeg(-rotationAngle);

		prevTimeMs = (int)(Time.time * 1000);
		timeMs = 0;
	}

	/// Coroutine to wait until the wheel has stopped spinning.
	/// This is useful if you want to spin a wheel in a coroutine
	/// instead of using a callback when it finishes.
	public IEnumerator waitToStop()
	{
		yield return null;	// Always wait at least one frame.
		while (state != State.IDLE)
		{
			updateWheel();
			yield return null;
		}
	}

	/// velocity should be > 0
	public void setStartingAngularVelocity(float velocity)
	{
		state = State.ACCELERATE_START;
		angularVelocity = velocity;
		acceleration = accelerationStart;
	}

	public void updateWheel()
	{
		//Debug.Log("Wheel Spinner angularVelocity = " + (angularVelocity * frameTime));
		// Count the amount of time that has passed since last update
		int currentTimeMs = (int)(Time.time * 1000);//realtimeSinceStartup;
		int diffMs = currentTimeMs - prevTimeMs;
		timeMs += diffMs;
		//Debug.Log("TimeMS:" + timeMs);

		int frameTimeMs = (int)(frameTime * 1000);
		//Debug.Log("FrameTimeMS:" + frameTimeMs);

		int steps = timeMs / frameTimeMs;
		for (int i = 0; i < steps; i++)
		{
			frameStep();
			timeMs -= frameTimeMs;
		}

		prevTimeMs = currentTimeMs;
	}

	protected State state
	{
		get
		{
			return _state;
		}

		set
		{
			_state = value;
		}
	}
	protected State _state;

	// Step frameTime time.
	protected void frameStep()
	{
		switch(state)
		{
			case State.ACCELERATE_START:
				time += frameTime;
				if (time > accelerationSlowStartSeconds)
				{
					acceleration = accelerationStart;
					state = State.ACCELERATE;
				}
				break;

			case State.ACCELERATE:
				if (angularVelocity >= maxAngularVelocity)
				{
					acceleration = 0;
					angularVelocity = maxAngularVelocity;
					if (isDialogWheel)
					{
						if (!isDialogUsingCustomSounds && isPlayingWheelLoopSound)
						{
							Audio.play(wheelLoop, 1, 0, 0, float.PositiveInfinity);
						}
					}
					else
					{
						if (isPlayingWheelLoopSound)
						{
							Audio.play(Audio.soundMap("wheel_loop_fast"), 1, 0, 0, float.PositiveInfinity);
						}
					}
					state = State.CONSTANT_VELOCITY;
					time = 0;
				}
				break;

			case State.CONSTANT_VELOCITY:
				time += frameTime;
				if (time > constantVelocitySeconds)
				{
					_targetAngle = (rotationAngle + getAdjustDegrees(deceleration));
					state = State.SEEK_ANGLE;
				}
				break;

			case State.SEEK_ANGLE:
				if ((rotationAngle + (angularVelocity * frameTime))  >= _targetAngle)
				{
					acceleration = deceleration;
					rotationAngle = _targetAngle;
					if (isDialogWheel)
					{
						if (!isDialogUsingCustomSounds)
						{
							if (deceleration < FAST_DECELERATION_THRESHOLD) // We are going very slowly
							{
								Audio.play(wheelDecelerateThreeSecs);
							}
							else
							{
								if (wheelSlowMusic != "")
								{
									Audio.playMusic(wheelSlowMusic);
								}
								else
								{
									Audio.play(wheelDecelerate);
								}
							}
						}
					}
					else
					{
						wheelDeceleratePlayingAudio = Audio.play(Audio.soundMap(wheelSlowsToStopSoundKey));
						// Start anticipation sound two seconds after decelerate occurs.

						if (isPlayingCrowdNoises)
						{
							Audio.play(Audio.soundMap("wheel_crowd_anticipation"), 1, 0, 3.5f);
						}
					}

					state = State.DECELERATE;
				}
				break;

			case State.DECELERATE:
				if (angularVelocity <= minAngularVelocity)
				{
					acceleration = 0;
					angularVelocity = minAngularVelocity;
					time = 0;

					state = State.DECELERATE_FINISH;

				}
				break;

			case State.DECELERATE_FINISH:
				time += frameTime;
				if (time > slowVelocitySeconds)
				{
					acceleration = 0;
					angularVelocity = 0;
					
					if (isDialogWheel && !string.IsNullOrEmpty(wheelLoop))
					{
						if (!isDialogUsingCustomSounds && !useCustomStopSound)
						{
							Audio.play(wheelStop);
						}
					}
					else
					{
						if (!isSpecialWinDialogShowing)
						{
							// Special win dialogs handle their own finish sound playing.
							if (!handleWheelStopSoundsInModule)
							{	
								string stopSound = Audio.soundMap("wheel_stops");
								string defaultStopSound = wheelStop; 
								if (stopSound == defaultStopSound || !Audio.doesAudioClipHaveChannelTag(stopSound, Audio.MUSIC_CHANNEL_KEY))
								{
									Audio.play(stopSound);
									
									if (wheelDeceleratePlayingAudio != null)
									{
										wheelDeceleratePlayingAudio.stop();
									}
								}
								else
								{
									Audio.switchMusicKeyImmediate(stopSound);
								}
							}
							if (isPlayingCrowdNoises)
							{
								Audio.play(Audio.soundMap("wheel_crowd_cheer"));
							}
						}
					}
					state = State.IDLE;

					if (_callback != null)
					{
						_callback();
					}
				}

				break;
		}

		rotationAngle += angularVelocity * frameTime;
		angularVelocity += acceleration * frameTime;

		// normalize and set rotation angle.
		if (!_ccw)
		{
			_wheel.transform.eulerAngles =  new Vector3 (0,0,rotFromDeg(-rotationAngle));
		}
		else
		{
			_wheel.transform.eulerAngles = new Vector3 (0,0,rotFromDeg(rotationAngle));
		}
	}

	protected float rotFromDeg(float degInput)
	{
		// There should be a better way to do this...
		float rotOutput = degInput;
		while ( rotOutput > 180 )
		{
			rotOutput -= 360;
		}
		while ( rotOutput < -180 )
		{
			rotOutput += 360;
		}
		return rotOutput;
	}

	protected float getAdjustDegrees(float deceleration)
	{
		float vel = angularVelocity;
		float ang = rotationAngle;
		float acc = deceleration;

		_slowdownRotation = rotationAngle;

		// Get timing for deceleration
		while (vel > minAngularVelocity)
		{
			ang += vel * frameTime;
			vel += acc * frameTime;
		}

		// add in slow tracking time.
		float t = 0;
		acc = 0;
		vel = minAngularVelocity;
		while ( t <=  slowVelocitySeconds)
		{
			ang += vel * frameTime;
			vel += acc * frameTime;
			t += frameTime;
		}

		float delta = ang - _slowdownRotation;

		// calculate degrees to adjust to
		float degs = rotationAngle + delta;
		int rots = (int)(degs / 360.0f);
		float normalizedDegrees = degs - (rots * 360.0f);

		float adjustDegrees = _desiredDegrees - normalizedDegrees;
		if (adjustDegrees < 0) adjustDegrees += 360;

		return adjustDegrees;
	}

	public void stopWheelImmediate()
	{
		state = State.IDLE;
		_wheel.transform.eulerAngles = new Vector3 (0,0,-_desiredDegrees);
	}
}
