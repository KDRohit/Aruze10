using System;
using UnityEngine;

public class MemoryWarningHandler: MonoBehaviour
{
	private static MemoryWarningHandler _instance = null;
	public static MemoryWarningHandler instance
	{
		get
		{
			if (_instance == null && !isUnityQuitting)
			{
				// Make a MemoryWarningHandler (should only be needed when not using the Startup scene
				// like when Animators are testing using the Art Setup scenes for instance)
				GameObject memoryWarningHandlerObj = new GameObject("MemoryWarningHandler");
				_instance = memoryWarningHandlerObj.AddComponent<MemoryWarningHandler>();
			}

			return _instance;
		}
	}

	private static bool isUnityQuitting = false;

	private const float _cleanupDelayDuration = 10.0f;

	private float _nextCleanupTime = 0.0f;
	private OnMemoryWarningDelegate onMemoryWarningDelegate = null;

	// Respond to iOS memory warning
	public void onMemoryWarning()
	{
		Debug.LogWarning("Received memory warning");
		DevGUIMenuPerformance.onMemoryWarning(); // Update dev panel counter.
		if (Time.time > _nextCleanupTime)
		{
			if (onMemoryWarningDelegate != null)
			{
				onMemoryWarningDelegate();
			}

			Glb.emptyGameSymbolCache();
			Glb.cleanupMemoryAsync();
			_nextCleanupTime = Time.time + _cleanupDelayDuration;
		}
	}

	private void Awake()
	{
		if (_instance != null)
		{
			Debug.LogError("MemoryWarningHandler.Awake() - More than one MemoryWarningHandler is being initialized!");
		}

		isUnityQuitting = false;
		_instance = this;
		Application.lowMemory += onMemoryWarning;
		DontDestroyOnLoad(transform.gameObject);
	}
	
	private void OnApplicationQuit() 
	{
		isUnityQuitting = true;
	}

	private void OnDestroy()
	{
		Application.lowMemory -= onMemoryWarning;
		_instance = null;
	}

	// Allow a memory warning to be forced
	public static void forceMemoryWarning()
	{
		GameObject go = GameObject.Find("MemoryWarningHandler");
		if (go)
		{
			go.SendMessage("onMemoryWarning", "");
			Debug.LogWarning("Just forced a memory cleanup.");
		}
		else
		{
			Debug.LogError("Unable to find MemoryWarningHandler game object. Check your persistent objects.");
		}
	}

	// Add a single OnMemoryWarningDelegate
	public void addOnMemoryWarningDelegate(OnMemoryWarningDelegate newDelegate)
	{
		onMemoryWarningDelegate += newDelegate;
	}

	// Remove a single OnMemoryWarningDelegate
	public void removeOnMemoryWarningDelegate(OnMemoryWarningDelegate delegateToRemove)
	{
		onMemoryWarningDelegate -= delegateToRemove;
	}

	// Clear all of the OnMemoryWarningDelegate
	public void clearOnMemoryWarningDelegates()
	{
		onMemoryWarningDelegate = null;
	}

	public delegate void OnMemoryWarningDelegate();
}
