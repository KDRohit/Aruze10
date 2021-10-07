using UnityEngine;
using System.Collections;

public class WheelSpinner3d
{
	private GameObject _wheel; //This object should be the one whose material needs to be offsetted.

	// motion variables.
	private float acceleration = 0f;
    private float angularVelocity = 0f;
    private float rotationAngle = 0f;

	// time step
	private float frameTime = 1.0f / 30.0f;

	private int prevTimeMs;
	private int timeMs = 0;

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
	public float deceleration = -160.0f;

	// variables for the State.DECELERATE_FINISH
	// this is the time at which the wheel having stopped decelerating
	// will spin at minAngularVelocity for slowVelocitySeconds seconds.
	public float minAngularVelocity = 18.0f;
	public float slowVelocitySeconds = .5f;

	// Cached input values.
	private float _desiredDegrees = 0.0f;	// Degrees to stop the wheel spin.
	private bool _ccw = false;			// Counter clockwise. Use with care. Joe doesn't like it.

	private float time;
	private float _slowdownRotation;
	private float _targetAngle;

	private int spinTimer;		// debug time for entire spin
	private int stateTimer;	// debug time for when the game is in the State.ACCELERATE state

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

	private GenericDelegate _callback;


    public float AngularVelocity
    {
        get { return angularVelocity; }
        private set { angularVelocity = value; }
    }
    public float RotationAngle
    {
        get { return rotationAngle; }
        private set { rotationAngle = value; }
    }

	public WheelSpinner3d(GameObject wheel, float desiredDegrees, GenericDelegate callback, bool ccw = false)
	{
		// Object to manipulate.
		_wheel = wheel;
		_callback = callback;

		stateTimer = (int)(Time.time * 1000);
		Debug.Log("SpinTimer: " + spinTimer + ", StateTimer:" + stateTimer);

		//Contents of start spin are merely just copied below

		_ccw = ccw;
		//_spinCompleteCallback = spinCompleteCallback;
		//_spinCompleteArgs = spinCompleteArgs;
		_desiredDegrees = -desiredDegrees;
		//if (!_ccw)
		//{
			_desiredDegrees = -desiredDegrees;
		//}

		acceleration = accelerationSlowStart;
		if (GameState.game != null) // For games and bonus games.
		{
			Audio.play(Audio.soundMap("wheel_spin_start"));
			Audio.play(Audio.soundMap("wheel_spin_animation"));
			Audio.playMusic(Audio.soundMap("wheel_slows_music"), 0.0f);
		}
		else // For daily bonus games.
		{
			Audio.play("wheel_Start");
			Audio.play("WoW_spin_wheel");
			Audio.play("null_fx");
		}

		state = State.ACCELERATE_START;
		time = 0;
		// Get the angle the wheel is already rotated.
		//rotationAngle = _wheel.transform.eulerAngles.z;
		rotationAngle = _wheel.GetComponent<Renderer>().materials[0].mainTextureOffset.x * 360;
		// Set the rotation with this weird function.
		rotationAngle = rotFromDeg(-rotationAngle);

		prevTimeMs = (int)(Time.time * 1000);
		timeMs = 0;

		spinTimer = (int)(Time.time * 1000);
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

	private State state
	{
		get
		{
			return _state;
		}

		set
		{
			if (_state != value)
			{
				stateTimer = (int)(Time.time * 1000);
			}
			_state = value;
		}
	}
	private State _state;

	// Step frameTime time.
	private void frameStep()
	{
		switch(state)
		{
			case State.ACCELERATE_START:
				time += frameTime;
				if (time > accelerationSlowStartSeconds)
				{
					//Debug.Log("About to jump to accelerate state");
					acceleration = accelerationStart;
					state = State.ACCELERATE;
				}
				break;

			case State.ACCELERATE:
				if (angularVelocity >= maxAngularVelocity)
				{
					//Debug.Log("About to jump to constant velocity");
					acceleration = 0;
					angularVelocity = maxAngularVelocity;
					if (GameState.game != null)
					{
						Audio.play(Audio.soundMap("wheel_loop_fast"), 1, 0, 0, float.PositiveInfinity);
					}
					else
					{
						Audio.play("wheel_loop", 1, 0, 0, float.PositiveInfinity);
					}
					state = State.CONSTANT_VELOCITY;
					time = 0;
				}
				break;

			case State.CONSTANT_VELOCITY:
				time += frameTime;
				if (time > constantVelocitySeconds)
				{
					_targetAngle = rotationAngle + getAdjustDegrees(deceleration);
					state = State.SEEK_ANGLE;
				}
				break;

			case State.SEEK_ANGLE:
				if ((rotationAngle + (angularVelocity * frameTime))  >= _targetAngle)
				{
					acceleration = deceleration;
					rotationAngle = _targetAngle;
					if (GameState.game != null)
					{
						Audio.play(Audio.soundMap("wheel_slows_to_stop"));
					}
					else
					{
						Audio.play("wheel_decelerate");
					}
					// Start anticipation sound one second after decelerate occurs.
					//playDictionaryAudio(WHEEL_AUDIO_KEY_ANTICIPATION, 2000);
					Audio.play("anticipation_long", 1, 0, 2f);

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
					if (GameState.game != null)
					{
						Audio.play(Audio.soundMap("wheel_stops"));
					}
					else
					{
						Audio.play("wheel_stop");
					}
					state = State.IDLE;

					if (_callback != null)
					{
						_callback();
					}
					//Debug.Log("Done spinning!");

					// Play the more exciting cheer cs collection
					//playDictionaryAudio(WHEEL_AUDIO_KEY_CHEER);
					Audio.play("cheer_c");
				}

				break;
		}

		rotationAngle += angularVelocity * frameTime;
		angularVelocity += acceleration * frameTime;

		// normalize and set rotation angle.
		if (!_ccw)
		{
			//_wheel.transform.rotation = rotFromDeg(-rotationAngle);
			//_wheel.transform.eulerAngles =  new Vector3 (0,0,rotFromDeg(-rotationAngle));
			_wheel.GetComponent<Renderer>().materials[0].mainTextureOffset = new Vector2(rotFromDeg(-rotationAngle)/360, 0);
		}
		else
		{
			//_wheel.transform.rotation = rotFromDeg(rotationAngle);
			//_wheel.transform.eulerAngles = new Vector3 (0,0,rotFromDeg(rotationAngle));
			_wheel.GetComponent<Renderer>().materials[0].mainTextureOffset = new Vector2(rotFromDeg(rotationAngle)/360, 0);
		}
	}
	private float rotFromDeg(float degInput)
	{
		// There should be a better way to do this...
		float rotOutput = degInput;
		while ( rotOutput > 360 )
		{
			rotOutput -= 360;
		}
		while ( rotOutput < 0 )
		{
			rotOutput += 360;
		}
		return rotOutput;
	}

	private float getAdjustDegrees(float deceleration)
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
}
