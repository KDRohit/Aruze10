//#define LOCAL_TESTING_FRAMERATE

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This class exists to provide a more reliable alternative to Time.frameCount.
// Time.frameCount only updates after Awake() functions in the Unity execution order,
// but this class will update the frame count at the end of the frame to ensure that the
// count is correct throughout the entirety of the next frame. This is especially important
// for TICoroutine to ensure that no coroutine moves twice in the same frame, which was
// causing issues before. 
// 
// WARNING: This could potentially be wrong if another class is using WaitForEndOfFrame() or OnDisable().
[ExecuteInEditMode]
public class FrameReporter : MonoBehaviour
{
	// =============================
	// PUBLIC
	// =============================
	// Number of frames that have passed.
	public static int frameCount = 0;
	public static int currentFrameCount = 0;
	public static FrameReporter instance { get; private set; }
	public int currentFPSTarget { get; private set; }

	// =============================
	// PRIVATE
	// =============================
	private static int[] fpsList = new int[TRACK_LIMIT]; 
	private static int fpsListIndex = 0;
	private static System.DateTime startTime;
	private static float duration = 0;

	// fps tracking, and throttling
	private static float totalTime = 0f;
	private SmartTimer throttleTimer = null;
	private bool monitorFPS = false; // set to true to begin monitoring the user's FPS as we increase the target
	private bool shouldThrottleFPS = true;
	private int minimumStableFPS = 60;
	private float monitorDuration = 0f;
	private List<int> throttledFrameRates = new List<int>();
	private Dictionary<int, int> temporaryFrameRates = new Dictionary<int, int>();

	// =============================
	// CONST
	// =============================
#if LOCAL_TESTING_FRAMERATE
	private const float UPDATE_RATE = 1.0f; // 1 second, used for tracking frames per second
	private const float SANITY_LIMIT = 0.55f; // value used to determine consistencies between throttled frame rates
	private const int MONITOR_DURATION_LIMIT = 10; // When we monitor for FPS throttling, duration before we disable monitoring
	private const int THROTTLE_FPS_DELAY = 2; // Seconds before we attempt to increase users frame rate
	public const int THROTTLE_TRACK_LIMIT = 30; // 30 seconds of attempting to throttle the fps
	public const int TRACK_LIMIT = 30; // How many UPDATE_RATE slices to record at a time.
#else
	private const float UPDATE_RATE = 1.0f; // 1 second, used for tracking frames per second
	private const float SANITY_LIMIT = 0.55f; // value used to determine consistencies between throttled frame rates
	private const int MONITOR_DURATION_LIMIT = 10; // When we monitor for FPS throttling, duration in seconds before we disable monitoring
	private const int THROTTLE_FPS_DELAY = 15; // Seconds before we attempt to increase users frame rate
	public const int THROTTLE_TRACK_LIMIT = 20; // 5 minutes of attempting to throttle the fps
	public const int TRACK_LIMIT = 30; // How many UPDATE_RATE slices to record at a time.

#endif

	// While throttling, if the user's frame rate is greater than or equal to SAFE_FPS_RANGE percentage of the target frame rate
	// we consider it "safe" to continue to increase their target frame rate
	private const decimal SAFE_FPS_RANGE = 0.85m;

	public void Awake()
	{
		instance = this;

		// Initialize this object and start the frame counting coroutine.
		frameCount = 0;
		startTime = System.DateTime.Now;

		throttleTimer = new SmartTimer(THROTTLE_FPS_DELAY, true, throttleFPS, "fps_timer");

		minimumStableFPS = MobileUIUtil.MAX_FPS_LIMIT;
		currentFPSTarget = Mathf.Max(MobileUIUtil.MIN_FPS_LIMIT, MobileUIUtil.deviceTargetFrameRate);

		if (Application.isPlaying)
		{
			DontDestroyOnLoad(gameObject);

			if (MobileUIUtil.deviceTargetFrameRate != -1)
			{
				throttleTimer.start();
			}
		}
	}

	// timer callback
	private void throttleFPS()
	{
		if (shouldThrottleFPS)
		{
			currentFPSTarget = Mathf.Max(MobileUIUtil.MIN_FPS_LIMIT, MobileUIUtil.deviceTargetFrameRate);
			monitorFPS = true;
		}
		else
		{
			throttleTimer.destroy();
		}
	}

	void Update()
	{
		if (Application.isPlaying)
		{
			if (monitorFPS)
			{
				updateDynamicFPS();
			}

			trackFrameRate();
		}
	}

	public void overrideCurrentTarget(int newValue)
	{
		currentFPSTarget = Mathf.Max(MobileUIUtil.MIN_FPS_LIMIT, newValue);
	}

	public void clear()
	{
		monitorDuration = 0;
		temporaryFrameRates.Clear();
		throttledFrameRates.Clear();
	}

	// attempts to throttle users frame rate
	public void updateDynamicFPS(float deltaOverride = 0, bool forceTrack = false)
	{
		float delta = Time.unscaledDeltaTime;

		if (!Application.isPlaying && deltaOverride > 0)
		{
			delta = deltaOverride;
		}

		// don't waste calculations during the loading screen
		if (Loading.isLoading)
		{
			return;
		}

		monitorDuration += delta;
		decimal targetFrameTime = 1m / Mathf.Min(currentFPSTarget, minimumStableFPS);
		decimal currentFrameTime = (decimal)Mathf.Max(delta, 1/(float)MobileUIUtil.MAX_TARGET_REFRESH);

		// if user is getting a frame rate that's within 80% of the target
		bool isWithinSafeRange = targetFrameTime / currentFrameTime >= SAFE_FPS_RANGE;

		if (isWithinSafeRange)
		{
			currentFPSTarget += 1;
		}
		else
		{
			currentFPSTarget -= 1;
		}

		// clamp it
		currentFPSTarget = Mathf.Clamp(currentFPSTarget, MobileUIUtil.MIN_FPS_LIMIT, MobileUIUtil.MAX_FPS_LIMIT);

		Common.logVerbose("FrameReporter: Changed to --> " + currentFPSTarget);

		// update it
		if (MobileUIUtil.deviceTargetFrameRate != currentFPSTarget)
		{
			MobileUIUtil.updateDynamicFrameRate(currentFPSTarget);
		}

		// store their current throttled frame rate value
		if (temporaryFrameRates.ContainsKey(currentFPSTarget))
		{
			temporaryFrameRates[currentFPSTarget]++;
		}
		else
		{
			temporaryFrameRates.Add(currentFPSTarget, 1);
		}

		// after X seconds of throttling, check what we landed on for frame rate
		if (forceTrack || monitorDuration >= MONITOR_DURATION_LIMIT)
		{
			int totalFramesCount = 0;

			// retrieve the highest frame rate count, and total the amount of frame rates we adjusted
			KeyValuePair<int, int> mostCommonFrameRate = new KeyValuePair<int, int>();
			foreach (KeyValuePair<int, int> entry in temporaryFrameRates)
			{
				totalFramesCount += entry.Value;

				if (entry.Value > mostCommonFrameRate.Value)
				{
					mostCommonFrameRate = entry;
				}
			}

			temporaryFrameRates.Clear();

			// check if user is retaining this frame rate more than X% of the time
			isWithinSafeRange = (float)mostCommonFrameRate.Value / (float)totalFramesCount >= SANITY_LIMIT;

			// store their common frame rate here
			if (isWithinSafeRange || mostCommonFrameRate.Key == MobileUIUtil.MIN_FPS_LIMIT || mostCommonFrameRate.Key == MobileUIUtil.MAX_FPS_LIMIT)
			{
				throttledFrameRates.Add(mostCommonFrameRate.Key);
				sanitizeFrameRate();
			}

			monitorDuration = 0f;
			monitorFPS = false;
		}
	}

	// used for tracking average fps in the end
	private void trackFrameRate()
	{
		frameCount++;
		currentFrameCount++;
		duration += Time.unscaledDeltaTime;

		if (duration >= UPDATE_RATE)
		{
			duration = 0f;

			if (currentFrameCount > 0)
			{
				fpsList[fpsListIndex] = currentFrameCount;
				currentFrameCount = 0;
				fpsListIndex++;

				if (fpsListIndex >= TRACK_LIMIT)
				{
					fpsListIndex = 0;
				}
			}
		}
	}

	// After we he have tracked a user's frame rate change for 5 minutes worth of throttling
	// we want to see if we notice any patterns, and stop attempts to throttle if it's not necessary
	private void sanitizeFrameRate()
	{
		if (throttledFrameRates.Count >= THROTTLE_TRACK_LIMIT)
		{
			Dictionary<int, int> frameRateCounts = new Dictionary<int, int>();
			for (int i = throttledFrameRates.Count; --i >= 0;)
			{
				int frameRate = throttledFrameRates[i];
				if (frameRateCounts.ContainsKey(frameRate))
				{
					frameRateCounts[frameRate]++;
				}
				else
				{
					frameRateCounts.Add(frameRate, 1);
				}
			}

			// retrieve the highest frame rate count
			KeyValuePair<int, int> mostCommonFrameRate = new KeyValuePair<int, int>();
			foreach (KeyValuePair<int, int> entry in frameRateCounts)
			{
				if (entry.Value > mostCommonFrameRate.Value)
				{
					mostCommonFrameRate = entry;
				}
			}

			// once we have the highest frame rate, see if this value was counted for X% or more of the entries
			// if it was, then we are seeing some consistency, and should stop evaluating their FPS for this session
			if ((float)mostCommonFrameRate.Value / throttledFrameRates.Count >= SANITY_LIMIT)
			{
				shouldThrottleFPS = false;
				currentFPSTarget = mostCommonFrameRate.Key;

				// finally update it, this will remain our target frame rate
				MobileUIUtil.updateDynamicFrameRate(currentFPSTarget);
				Common.logVerbose("FrameReporter: Frame rate throttled --> " + currentFPSTarget);
			}
			else
			{
				// user has semi unstable frame rates, we are going to set the minimum to whatever they received the most
				Common.logVerbose("FrameReporter: Unstable framerate, setting new minimum --> " + mostCommonFrameRate.Key);
				minimumStableFPS = Mathf.Clamp(mostCommonFrameRate.Key, MobileUIUtil.MIN_FPS_LIMIT, MobileUIUtil.MAX_FPS_LIMIT);
			}

			throttledFrameRates = new List<int>();
		}
	}

	public static float averageFPS
	{
		get
		{
			float fpsSum = 0;
			for (int i = 0; i < fpsList.Length; ++i)
			{
				if (fpsList[i] > 0)
				{
					fpsSum += fpsList[i];
				}
			}

			float targetFPS;
#if UNITY_WEBGL
			targetFPS = 60f;
#else
			targetFPS = Application.targetFrameRate;
			if (targetFPS <= 0.0f)
			{
				targetFPS = 60f;
			}
#endif

			float average = fpsSum <= 0 ? targetFPS : fpsSum / fpsList.Length;

			return average;
		}
	}
}
