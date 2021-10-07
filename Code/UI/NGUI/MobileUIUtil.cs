using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_IOS 
using UnityEngine.iOS;
#endif

/**
Contains a variety of UI helper functions specific to mobile.
*/
using System;


public class MobileUIUtil
{
	private const float MINIMUM_TABLET_INCHES = 3.2f;		// How big is the smaller dimension of the biggest non-tablet?
	private const float DEFAULT_EDITOR_RESOLUTION = 100.0f;	// What DPI and target FPS should we set our default editor to be at?
	private const int DEFAULT_EDITOR_TARGET_FPS = 60;
	public const float MIN_FPS_CUTOFF_60 = 50.0f;
	public const float MIN_FPS_CUTOFF_30 = 21.0f;
	public const int MIN_FPS_LIMIT = 20;
	public static int MAX_FPS_LIMIT = 60;
	public static int MINIMUM_TARGET_REFRESH = 60;
	public static int MAX_TARGET_REFRESH = 120; // most devices do not exceed 120 refresh
	public static int averageFPSOverride { get; private set; }

#if !UNITY_EDITOR
	private static DeviceLayoutSize cachedDeviceSize = DeviceLayoutSize.UNKNOWN;
#endif

	/// Use lower target frame rate on lower-end devices

	private static int cachedTargetFrameRate = 0;
	public static int deviceTargetFrameRate
	{
		get
		{
			int nativeFPS = 0;

			// set the max frame limit to the refresh rate, we shouldn't create any more frames than can be drawn per the refresh rate
			MAX_FPS_LIMIT = Mathf.Max(MINIMUM_TARGET_REFRESH, Screen.currentResolution.refreshRate);

#if UNITY_EDITOR
			string pref = PlayerPrefsCache.GetString(DebugPrefs.TARGET_FRAME_RATE, "").Trim();
			if (!string.IsNullOrEmpty(pref))
			{
				nativeFPS = int.Parse(pref);
			}
#elif UNITY_IPHONE
				switch (Device.generation)
				{
					case DeviceGeneration.iPhone:
					case DeviceGeneration.iPhone3G:
					case DeviceGeneration.iPhone3GS:
					case DeviceGeneration.iPodTouch1Gen:
					case DeviceGeneration.iPodTouch2Gen:
					case DeviceGeneration.iPodTouch3Gen:
					case DeviceGeneration.iPodTouch4Gen:
					case DeviceGeneration.iPad1Gen:
						nativeFPS = 20;
						break;
					
					case DeviceGeneration.iPhone4:
					case DeviceGeneration.iPhone4S:
					case DeviceGeneration.iPhone5:
					case DeviceGeneration.iPhone5C:
					case DeviceGeneration.iPhone5S:
					case DeviceGeneration.iPodTouch5Gen:
					case DeviceGeneration.iPad2Gen:
					case DeviceGeneration.iPad3Gen:
					case DeviceGeneration.iPad4Gen:
					case DeviceGeneration.iPadMini1Gen:
					case DeviceGeneration.iPadMini2Gen:
					case DeviceGeneration.iPadMini3Gen:
					case DeviceGeneration.iPadMini4Gen:
					case DeviceGeneration.iPadAir1:
						nativeFPS = 30;
						break;
					
					case DeviceGeneration.iPhone6:
					case DeviceGeneration.iPhone6Plus:
					case DeviceGeneration.iPhone6S:
					case DeviceGeneration.iPhone6SPlus:
					case DeviceGeneration.iPadAir2:
					case DeviceGeneration.iPadPro1Gen:
					case DeviceGeneration.iPhone8:
					case DeviceGeneration.iPhone8Plus:
					case DeviceGeneration.iPhoneX:
					case DeviceGeneration.iPadPro2Gen:
					case DeviceGeneration.iPadPro10Inch1Gen:
					case DeviceGeneration.iPadPro10Inch2Gen:
						nativeFPS = 60;
						break;

					default:
						// Assume that iOS devices we don't know about are 60 FPS
						nativeFPS = 60;
						break;
				}
#elif UNITY_WEBGL 
/*
We don't want to set FPS for webGL if we want it to run as fast as possible;
"When you don’t want to throttle performance, set this API to the default value of –1,
rather then to a high value. This allows the browser to adjust the frame rate
for the smoothest animation in the browser’s render loop, and may produce better
results than Unity trying to do its own main loop timing to match a target frame rate."
- https://docs.unity3d.com/Manual/webgl-performance.html
*/
//nativeFPS = -1;
#endif
			// Fall-through in case we still don't have a device FPS
			if (nativeFPS == 0)
			{
				if (SystemInfo.graphicsMemorySize < 96 || SystemInfo.systemMemorySize < 1536 || SystemInfo.graphicsShaderLevel < 30)
				{
					nativeFPS = 20;
				}
				else
				{
					if (averageFPSOverride > 0)
					{
						nativeFPS = averageFPSOverride;
					}
					else if (SystemInfo.graphicsMemorySize < 192 || SystemInfo.systemMemorySize < 3584)
					{
						nativeFPS = 30;
					}
					else
					{
						nativeFPS = 60;
					}
				}
					
			}

#if UNITY_EDITOR
			// App is in performance more, so don't go over 30 fps.
			if (PlayerPrefsCache.GetInt(Prefs.PLAYER_PERF, 0) != 0)
			{
				nativeFPS = Mathf.Min(nativeFPS, 30);
			}

			if (PlayerPrefsCache.GetInt("FPS_OVERRIDE", 0) != 0)
			{
				nativeFPS = PlayerPrefsCache.GetInt("FPS_OVERRIDE", 0);
			}
#endif
				
			return cachedTargetFrameRate = nativeFPS;
		}
	}

	public static bool isUltraWide()
	{
		return Screen.width / Screen.height >= 2.0f;
	}

	public static void evaluateFPSTarget(int num20FS, int num30FPS, int num60FPS)
	{
		float totalNumberOfReports = num20FS + num30FPS + num60FPS;

		if (totalNumberOfReports >= 4) // 2 minutes on the same screen?
		{
			float percent60FPS = num60FPS/totalNumberOfReports;
			float percent30FPS = num30FPS/totalNumberOfReports;

			int newTargetFPS = 0;

			// go in descending order from highest frame rate possible
			// we want to give user's devices the benefit of running at 60 fps if they're close to it
			if (cachedTargetFrameRate != 60 && percent60FPS > 0.3f)
			{
				newTargetFPS = 60;
			}
			else if (cachedTargetFrameRate != 30 && percent30FPS > 0.3f)
			{
				newTargetFPS = 30;
			}
			else if (cachedTargetFrameRate != 20)
			{
				newTargetFPS = 20;
			}

			if (newTargetFPS > 0)
			{
				Dictionary<string, string> extraFields = new Dictionary<string, string>();
				extraFields.Add("from", cachedTargetFrameRate.ToString());
				extraFields.Add("to", newTargetFPS.ToString());
				extraFields.Add("numberOfReportsIn60Threshold", num60FPS.ToString());
				extraFields.Add("numberOfReportsIn30Threshold", num30FPS.ToString());
				extraFields.Add("numberOfReportsIn20Threshold", num20FS.ToString());
				if (GameState.game != null)
				{
					extraFields.Add("game_key", GameState.game.keyName);
				}
				SplunkEventManager.createSplunkEvent("fps-change", "dynamic", extraFields);
				averageFPSOverride = newTargetFPS;
				NGUILoader.setVisualQuality();
			}
		}
	}

	public static void updateDynamicFrameRate(int newValue)
	{
		averageFPSOverride = newValue;
		NGUILoader.setVisualQuality();
	}

	/// Are we on a performance-limited device?
	public static bool isSlowDevice
	{
		get
		{
			if (PlayerPrefsCache.GetInt(Prefs.PLAYER_PERF, 0) != 0)
			{
				return true;
			}
			
			return (deviceTargetFrameRate < 30);
		}
	}

	/// Are we on a device that's allowed (for compatibility sake)?
	public static bool isAllowedDevice
	{
		get
		{
#if UNITY_EDITOR
			return true;
#elif UNITY_IPHONE
			switch (Device.generation)
			{
				case DeviceGeneration.iPhone:
				case DeviceGeneration.iPhone3G:
				case DeviceGeneration.iPhone3GS:
				case DeviceGeneration.iPodTouch1Gen:
				case DeviceGeneration.iPodTouch2Gen:
				case DeviceGeneration.iPodTouch3Gen:
				case DeviceGeneration.iPodTouch4Gen:
				case DeviceGeneration.iPad1Gen:
					return false;

				default:
					return true;
			}
#else
			return true;	// Sure, let's always allow Android devices that get past install.
#endif
		}
	}

	// Returns the first digit (the major release?) of the OS. EX: ios Version 9.0.1 returns 9.
	public static int getMajorOSVersion()
	{
		string versNum = "";
		int i = 0;

		while (i < SystemInfo.operatingSystem.Length && !Char.IsNumber(SystemInfo.operatingSystem[i]))
		{
			i++;
		}

		while (i < SystemInfo.operatingSystem.Length && Char.IsDigit(SystemInfo.operatingSystem[i]))
		{
			versNum += SystemInfo.operatingSystem[i];
			i++;
		}

			return int.Parse(versNum);
	}

	public static int getMinorOSVersion()
	{
		string versNum = "";
		int i = 0;
		while (i < SystemInfo.operatingSystem.Length && !Char.IsNumber(SystemInfo.operatingSystem[i]))
		{
			i++;
		}

		while (i < SystemInfo.operatingSystem.Length && Char.IsNumber(SystemInfo.operatingSystem[i]))
		{
			i++;
		}

		while (i < SystemInfo.operatingSystem.Length && !Char.IsNumber(SystemInfo.operatingSystem[i]))
		{
			i++;
		}

		while (i < SystemInfo.operatingSystem.Length && Char.IsDigit(SystemInfo.operatingSystem[i]))
		{
			versNum += SystemInfo.operatingSystem[i];
			i++;
		}

		return int.Parse(versNum);
	}


	/// Is this device so bad we need to cut back symbol animations and other features?
	public static bool isCrappyDevice
	{
		get
		{
			// App is in performance mode, so everything is crappy.
			if (PlayerPrefsCache.GetInt(Prefs.PLAYER_PERF, 0) != 0)
			{
				return true;
			}
			
#if UNITY_EDITOR
			if (PlayerPrefsCache.GetInt(DebugPrefs.IS_CRAPPY_DEVICE, 0) != 0)
			{
				return true;
			}
#elif UNITY_IPHONE
			switch (Device.generation)
			{
				case DeviceGeneration.iPhone:
				case DeviceGeneration.iPhone3G:
				case DeviceGeneration.iPhone3GS:
				case DeviceGeneration.iPhone4:
				case DeviceGeneration.iPodTouch1Gen:
				case DeviceGeneration.iPodTouch2Gen:
				case DeviceGeneration.iPodTouch3Gen:
				case DeviceGeneration.iPodTouch4Gen:
				case DeviceGeneration.iPodTouch5Gen:
				case DeviceGeneration.iPad1Gen:
					return true;
				
				case DeviceGeneration.iPhone4S:
				case DeviceGeneration.iPhone5:
				case DeviceGeneration.iPhone5C:
				case DeviceGeneration.iPhone5S:
				case DeviceGeneration.iPhone6:
				case DeviceGeneration.iPhone6Plus:
				case DeviceGeneration.iPad2Gen:
				case DeviceGeneration.iPad3Gen:
				case DeviceGeneration.iPad4Gen:
				case DeviceGeneration.iPad5Gen:
				case DeviceGeneration.iPadAir2:
				case DeviceGeneration.iPadMini1Gen:
				case DeviceGeneration.iPadMini2Gen:
				case DeviceGeneration.iPadMini3Gen:
					return false;
			}
#elif UNITY_WEBGL
			// Chuck wanted to force high-performing device for his WebGL demo
			return false;
#endif
			if (SystemInfo.graphicsMemorySize < 96 ||
				SystemInfo.systemMemorySize < 768 ||
				SystemInfo.graphicsShaderLevel < 30 ||
				SystemInfo.operatingSystem.FastStartsWith("Android OS 4.0"))
			{
				return true;
			}
			return false;
		}
	}

	/// Are we using a small mobile device?
	public static bool isSmallMobile
	{
		get
		{
#if !UNITY_EDITOR
			if (MobileUIUtil.cachedDeviceSize == DeviceLayoutSize.UNKNOWN)
			{
				MobileUIUtil.cachedDeviceSize = getCurrentDeviceResolution();
			}
			return MobileUIUtil.cachedDeviceSize == DeviceLayoutSize.SMALL;
#else
			return getCurrentDeviceResolution() == DeviceLayoutSize.SMALL;
#endif
		}
	}

	// Get the width/height ratio of the device, helpful for smaller width screen specific behaviour.
	public static float getCurrentDeviceResolutionRatio()
	{	
		return (float)Screen.width / (float)Screen.height;
	}
	
	/// Get back an enum indicating if one should use a large device layout or a small device layout
	public static DeviceLayoutSize getCurrentDeviceResolution()
	{
		float smallerInches = Mathf.Min(Screen.width, Screen.height) / getDotsPerInch();
		return (smallerInches < MINIMUM_TABLET_INCHES) ? DeviceLayoutSize.SMALL : DeviceLayoutSize.LARGE;
	}

	/// Gets the # of dots per inch for rendering.  Note that this value is inaccurate for the editor since it can't determine it, so I am using a default DPI.
	/// Unity forums indicate that 160.0f is a safe default value for that environment.  It can be overwritten in the editor login settings.
	public static float getDotsPerInch()
	{
#if UNITY_EDITOR
		string pref = PlayerPrefsCache.GetString(DebugPrefs.DOTS_PER_INCH, "").Trim();
		if (pref == "")
		{
			return DEFAULT_EDITOR_RESOLUTION;
		}
		return float.Parse(pref);
#else
		// Only use native dpi when on a device.
		return (float)Screen.dpi;
#endif
	}

	/// An enum for device layout sizes we have built for.  We should not have to add more device size layouts, but in case we do, logic
	/// would need to be added in the functions above to account for it, and the enum would be added here.
	public enum DeviceLayoutSize
	{
		SMALL,
		LARGE,
		UNKNOWN
	}

	public static bool shouldUseLowRes
	{
		get
		{
			return (NGUIExt.pixelFactor < 0.75f);
		}
	}
}
