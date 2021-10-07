using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
This overrides the Unity version except where that version is explicitly references (e.g. UnityEngine.Screen.width).
This gives us the ability to virtualize the game into a render texture and fool everything else into thinking the
screen resolution is not what it is.
*/
public static class Screen
{
	public static UnityEngine.ScreenOrientation orientation => UnityEngine.Screen.orientation;

	public static int width
	{
		get
		{
			if (ResolutionChangeHandler.instance != null && ResolutionChangeHandler.instance.virtualScreenMode)
			{
				return ResolutionChangeHandler.instance.virtualWidth;
			}
			return UnityEngine.Screen.width;
		}
	}
	
	public static int height
	{
		get
		{
			if (ResolutionChangeHandler.instance != null && ResolutionChangeHandler.instance.virtualScreenMode)
			{
				return ResolutionChangeHandler.instance.virtualHeight;
			}
			return UnityEngine.Screen.height;
		}
	}
	
	public static Resolution currentResolution
	{
		get
		{
			return UnityEngine.Screen.currentResolution;
		}
	}
	
	public static float dpi
	{
		get
		{
			return UnityEngine.Screen.dpi;
		}
	}

	public static int sleepTimeout
	{
		get
		{
			return UnityEngine.Screen.sleepTimeout;
		}
		set
		{
			UnityEngine.Screen.sleepTimeout = value;
		}
	}

	public static Rect safeArea
	{
		get
		{
			return UnityEngine.Screen.safeArea;
		}
	}

	public static bool fullScreen
	{
		get { return UnityEngine.Screen.fullScreen; }
		set { UnityEngine.Screen.fullScreen = value; }
	}
}
