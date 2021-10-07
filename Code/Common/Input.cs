
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
This is basically the Unity Version, but gives us more control around automation.
*/
public static class Input
{
	private static bool simulating = false;

	public static bool drawColliderVisualizer = false;
	private static Vector2? thisFrameMousePosition = null;
	private static bool clickDown = false;
	private static bool clickUp = false;
	private static Touch? thisFrameTouch = null;
	private static KeyCode keyPressed = KeyCode.None;
	private static bool keyDown = false;
	private static bool keyUp = false;

	private static TICoroutineMonoBehaviour routineRunnerBehaviour 
	{
		get
		{
#if UNITY_EDITOR
			return CommonAutomation.routineRunnerBehaviour;
#else
			return RoutineRunner.instance;
#endif
		}
	}

	public static IEnumerator simulateMouseClickOn(Component component, float timeDown = 0.0f, Camera cameraForButton = null)
	{
		if (component == null)
		{
			yield break;
		}
		if (simulateMouseWithTouches)
		{
			yield return routineRunnerBehaviour.StartCoroutine(simulateTouchOn(component, timeDown, cameraForButton));
		}
		else
		{
			if (simulating)
			{
				Debug.LogWarning("Trying to simulate more than one click at a time.");
				yield break;
			}
			simulating = true;

			if (cameraForButton == null)
			{
				cameraForButton = NGUIExt.getObjectCamera(component.gameObject);
			}

			if (cameraForButton != null)
			{
				if (drawColliderVisualizer)
				{
					if (component is Collider)
					{
						ColliderVisualizer.drawVisualCollider((component as Collider), true, true);
					}
					else if (component is SwipeArea)
					{
						SwipeArea area = component as SwipeArea;
						Bounds areaBounds = new Bounds();
						areaBounds.center = area.center;
						areaBounds.extents = area.size;
						ColliderVisualizer.drawVisualBounds(area.gameObject, areaBounds, true, true);
					}
					else
					{
						// Try to get this component's object's bounds using getObjectBounds()
						Bounds bounds = CommonGameObject.getObjectBounds(component.gameObject);
						ColliderVisualizer.drawVisualBounds(component.gameObject, bounds, true, true, true);
					}
				}

				Vector3 center = Vector3.zero;

				if (component is Collider)
				{
					CommonGameObject.getColliderWorldCenter(component as Collider, out center);
				}
				else if (component is SwipeArea)
				{
					Vector3 localCenter = (component as SwipeArea).center;
					center = component.transform.TransformPoint(localCenter);
				}

				Vector2 position2d = cameraForButton.WorldToScreenPoint(center);
				//Debug.Log("Clicking " + position2d);
				thisFrameMousePosition = position2d; // Down
				clickDown = true;
				clickUp = false;
				yield return new WaitForSeconds(timeDown);
				thisFrameMousePosition = position2d; // up
				clickDown = false;
				clickUp = true;
				yield return null;
				thisFrameMousePosition = null; // Done.
				clickUp = false;
				yield return null;
			}
			else
			{
				Debug.LogError("Can't click on " + component + " because I can't find it's camera.");
			}
			simulating = false;
		}
	}

	public static IEnumerator simulateTouchOn(Component component, float timeDown = 0.0f, Camera cameraForButton = null)
	{
		if (component == null)
		{
			yield break;
		}
		if (simulating)
		{
			Debug.LogWarning("Trying to simulate more than one click at a time.");
			yield break;
		}
		simulating = true;

		if (cameraForButton == null)
		{
			cameraForButton = NGUIExt.getObjectCamera(component.gameObject);
		}

		if (cameraForButton != null)
		{
			if (drawColliderVisualizer)
			{
				if (component is Collider)
				{
					ColliderVisualizer.drawVisualCollider((component as Collider), true, true);
				}
				else if (component is SwipeArea)
				{
					SwipeArea area = component as SwipeArea;
					Bounds areaBounds = new Bounds();
					areaBounds.center = area.center;
					areaBounds.extents = area.size;
					ColliderVisualizer.drawVisualBounds(area.gameObject, areaBounds, true, true);
				}
				else
				{
					Bounds bounds = CommonGameObject.getObjectBounds(component.gameObject);
					ColliderVisualizer.drawVisualBounds(component.gameObject, bounds, true, true, true);
				}
			}
			Vector3 center = Vector3.zero;

			if (component is Collider)
			{
				CommonGameObject.getColliderWorldCenter(component as Collider, out center);
			}
			else if (component is SwipeArea)
			{
				Vector3 localCenter = (component as SwipeArea).center;
				center = component.transform.TransformPoint(localCenter);
			}

			Vector2 position2d = cameraForButton.WorldToScreenPoint(center);
			thisFrameTouch = new Touch(Vector2.zero, Time.deltaTime, 1, TouchPhase.Began, position2d, 1);
			yield return null;
			if (timeDown > 0.0f)
			{
				thisFrameTouch = new Touch(Vector2.zero, Time.deltaTime, 1, TouchPhase.Stationary, position2d, 1);
				yield return new WaitForSeconds(timeDown);
			}
			thisFrameTouch = new Touch(Vector2.zero, Time.deltaTime, 1, TouchPhase.Ended, position2d, 1);
			yield return null;
			thisFrameTouch = null;
			yield return null;
		}
		else
		{
			Debug.LogError("Can't touch " + component + " because I can't find it's camera.");
		}
		simulating = false;
	}

	private static KeyCode getKeyCodeFromString(string key)
	{
		switch (key)
		{
			case "0":
				return KeyCode.Alpha0;
			case "1":
				return KeyCode.Alpha1;
			case "2":
				return KeyCode.Alpha2;
			case "3":
				return KeyCode.Alpha3;
			case "4":
				return KeyCode.Alpha4;
			case "5":
				return KeyCode.Alpha5;
			case "6":
				return KeyCode.Alpha6;
			case "7":
				return KeyCode.Alpha7;
			case "8":
				return KeyCode.Alpha8;
			case "9":
				return KeyCode.Alpha9;
			case ";":
				return KeyCode.Semicolon;
			case "'":
				return KeyCode.Quote;
			case ",":
				return KeyCode.Comma;
			case ".":
				return KeyCode.Period;
			default:
				try
				{
					return (KeyCode)System.Enum.Parse(typeof(KeyCode), key, true);
				}
				catch
				{
					Debug.LogError("Trying to press a key that doesn't exist " + key);
					return KeyCode.None;
				}
		}
	}

	public static IEnumerator simulateKeyPress(string key, float timeDown = 0.0f)
	{

		KeyCode thisKeyCode = getKeyCodeFromString(key);

		if (thisKeyCode != KeyCode.None)
		{
			yield return routineRunnerBehaviour.StartCoroutine(simulateKeyPress(thisKeyCode, timeDown));
		}	
	}

	public static IEnumerator simulateKeyPress(KeyCode key, float timeDown = 0.0f)
	{

		if (key == KeyCode.None)
		{
			yield break;
		}
		if (simulating)
		{
			Debug.LogWarning("Trying to simulate more than one key at a time.");
			yield break;
		}
		simulating = true;

		keyPressed = key;
		keyDown = true; //down
		keyUp = false;

		yield return new TIWaitForSeconds(timeDown);

		keyDown = false; // up
		keyUp = true;

		yield return null;

		keyUp = false; // done
		yield return null;

		simulating = false;
	}


	//////////////////////////////////
	///////////Properties/////////////
	//////////////////////////////////
	public static Vector3 acceleration
		{ get { return UnityEngine.Input.acceleration; } }

	public static int accelerationEventCount
		{ get { return UnityEngine.Input.accelerationEventCount; } }

	public static AccelerationEvent[] accelerationEvents
		{ get { return UnityEngine.Input.accelerationEvents; } }

	public static bool anyKey
	{
		get
		{
			return thisFrameMousePosition != null || thisFrameTouch != null || keyPressed != KeyCode.None || UnityEngine.Input.anyKey;
		}
	}

	public static bool anyKeyDown
	{
		get
		{
			bool touchDown = thisFrameTouch != null && ((Touch)thisFrameTouch).phase == TouchPhase.Began;
			bool mouseDown = thisFrameMousePosition != null && clickDown;
			bool keyboardDown = keyPressed != KeyCode.None && keyDown;
			return touchDown || mouseDown || keyboardDown || UnityEngine.Input.anyKeyDown;
		}
	}

	public static Compass compass
		{ get { return UnityEngine.Input.compass; } }

	public static bool compensateSensors
	{
		get { return UnityEngine.Input.compensateSensors; }
		set { UnityEngine.Input.compensateSensors = value; }
	}

	public static Vector2 compositionCursorPos
	{
		get { return UnityEngine.Input.compositionCursorPos; }
		set { UnityEngine.Input.compositionCursorPos = value; }
	}

	public static string compositionString
		{ get { return UnityEngine.Input.compositionString; } }

	public static DeviceOrientation deviceOrientation
		{ get { return UnityEngine.Input.deviceOrientation; } }

	public static Gyroscope gyro
		{ get { return UnityEngine.Input.gyro; } }

	public static IMECompositionMode imeCompositionMode
	{
		get { return UnityEngine.Input.imeCompositionMode; }
		set { UnityEngine.Input.imeCompositionMode = value; }
	}

	public static bool imeIsSelected
		{ get { return UnityEngine.Input.imeIsSelected; } }

	public static string inputString
		{ get { return UnityEngine.Input.inputString; } }

	// This is intentionally commented out because if it is referenced during
	// device build compilation, then it includes app permissions that we do
	// not wait to require for the app.
	// public static LocationService location
		// { get { return UnityEngine.Input.location; } }

	public static Vector3 mousePosition
	{
		get
		{
			if (thisFrameMousePosition != null)
			{
				Vector2 overloadedMousePosition = (Vector2)thisFrameMousePosition;
				return new Vector3(overloadedMousePosition.x, overloadedMousePosition.y);
			}
			if (ResolutionChangeHandler.instance != null && ResolutionChangeHandler.instance.virtualScreenMode == true)
			{
				return ResolutionChangeHandler.instance.getMousePosition();
			}
			else
			{
				return UnityEngine.Input.mousePosition;
			}
		}
	}

	public static Vector2 mouseScrollDelta
		{ get { return UnityEngine.Input.mouseScrollDelta; } }

	public static bool multiTouchEnabled
	{
		get { return UnityEngine.Input.multiTouchEnabled; }
		set { UnityEngine.Input.multiTouchEnabled = value; }
	}

	public static bool simulateMouseWithTouches
	{
		get { return UnityEngine.Input.simulateMouseWithTouches; }
		set { UnityEngine.Input.simulateMouseWithTouches = value; }
	}

	public static int touchCount
	{
		get
		{
			if (thisFrameTouch != null)
			{
				return 1;
			}
			return UnityEngine.Input.touchCount;
		}
	}

	public static Touch[] touches
	{
		get
		{
			if (thisFrameTouch != null)
			{
				return new Touch[]{(Touch) thisFrameTouch};
			}
			Touch[] overridenTouches = new Touch[UnityEngine.Input.touches.Length];
			for (int i = 0; i < overridenTouches.Length; i++)
			{
				overridenTouches[i] = new Touch(UnityEngine.Input.touches[i]);
			}
			return overridenTouches;
		}
	}

	public static bool touchSupported
		{ get { return UnityEngine.Input.touchSupported; } }

	//////////////////////////////////
	////////////Functions/////////////
	//////////////////////////////////

	public static AccelerationEvent GetAccelerationEvent(int index)
	{
		return UnityEngine.Input.GetAccelerationEvent(index);
	}

	public static bool GetButton(string buttonName)
	{
		return UnityEngine.Input.GetButton(buttonName);
	}

	public static bool GetButtonDown(string buttonName)
	{
		return UnityEngine.Input.GetButtonDown(buttonName);
	}

	public static bool GetButtonUp(string buttonName)
	{
		return UnityEngine.Input.GetButtonUp(buttonName);
	} 

	public static string[] GetJoystickNames()
	{
		return UnityEngine.Input.GetJoystickNames();
	}

	public static bool GetKey(string name)
	{
		if (keyPressed != KeyCode.None)
		{
			try
			{
				KeyCode thisKeyCode = getKeyCodeFromString(name);
				return keyPressed == thisKeyCode;
			}
			catch
			{
				return false;
			}
		}
		return UnityEngine.Input.GetKey(name);
	}

	public static bool GetKey(KeyCode key)
	{
		if (keyPressed != KeyCode.None)
		{
			return keyPressed == key;
		}
		return UnityEngine.Input.GetKey(key);
	}

	public static bool GetKeyDown(string name)
	{
		if (keyPressed != KeyCode.None && keyDown)
		{
			try
			{
				KeyCode thisKeyCode = getKeyCodeFromString(name);
				return keyPressed == thisKeyCode;
			}
			catch
			{
				return false;
			}
		}
		return UnityEngine.Input.GetKeyDown(name);
	}

	public static bool GetKeyDown(KeyCode key)
	{
		if (keyPressed != KeyCode.None && keyDown)
		{
			return keyPressed == key;
		}
		return UnityEngine.Input.GetKeyDown(key);
	}

	public static bool GetKeyUp(string name)
	{
		if (keyPressed != KeyCode.None && keyUp)
		{
			KeyCode thisKeyCode = getKeyCodeFromString(name);
			return keyPressed == thisKeyCode;
		}
		return UnityEngine.Input.GetKeyUp(name);
	}

	public static bool GetKeyUp(KeyCode key)
	{
		if (keyPressed != KeyCode.None && keyUp)
		{
			return keyPressed == key;
		}
		return UnityEngine.Input.GetKeyUp(key);
	}

	public static bool GetMouseButton(int button)
	{
		if (thisFrameMousePosition != null && button == 0) // Left clicks
		{
			return true;
		}
		return UnityEngine.Input.GetMouseButton(button);
	}

	public static bool GetMouseButtonDown(int button)
	{
		return clickDown || UnityEngine.Input.GetMouseButtonDown(button);
	}

	public static bool GetMouseButtonUp(int button)
	{
		return clickUp || UnityEngine.Input.GetMouseButtonUp(button);
	}

	public static Touch GetTouch(int index)
	{
		if (thisFrameTouch != null)
		{
			return (Touch)thisFrameTouch;
		}
		if (ResolutionChangeHandler.instance != null && ResolutionChangeHandler.instance.virtualScreenMode == true)
		{
			return ResolutionChangeHandler.instance.getTouch(index);
		}
		else
		{
			return new Touch(UnityEngine.Input.GetTouch(index));
		}
	}

	public static void ResetInputAxes()
	{
		UnityEngine.Input.ResetInputAxes();
	}


}

// A custom Touch class so we can emulate touches on the screen.
public struct Touch
{
	public Vector2 deltaPosition
	{
		get { return _deltaPosition; }
		private set { _deltaPosition = value; }
	}
	private Vector2 _deltaPosition;

	public float deltaTime
	{
		get { return _deltaTime; }
		private set { _deltaTime = value; }
	}
	private float _deltaTime;

	public int fingerId
	{
		get { return _fingerId; }
		private set { _fingerId = value; }
	}
	private int _fingerId;

	public TouchPhase phase
	{
		get { return _phase; }
		private set { _phase = value; }
	}
	private TouchPhase _phase;

	public Vector2 position
	{
		get { return _position; }
		set { _position = value; }
	}
	private Vector2 _position;

	public int tapCount
	{
		get { return _tapCount; }
		private set { _tapCount = value; }
	}
	private int _tapCount;

	public Touch(UnityEngine.Touch builtInTouch)
	{
		_deltaPosition = builtInTouch.deltaPosition;
		_deltaTime = builtInTouch.deltaTime;
		_fingerId = builtInTouch.fingerId;
		_phase = builtInTouch.phase;
		_position = builtInTouch.position;
		_tapCount = builtInTouch.tapCount;
	}

	public Touch(Vector2 deltaPosition, float deltaTime, int fingerId, TouchPhase phase, Vector2 position, int tapCount)
	{
		_deltaPosition = deltaPosition;
		_deltaTime = deltaTime;
		_fingerId = fingerId;
		_phase = phase;
		_position = position;
		_tapCount = tapCount;
	}
	
	public void overridePosition(Vector2 pos)
	{
		_position = pos;
	}
	
	public void cancel()
	{
		phase = TouchPhase.Canceled;
	}
}
