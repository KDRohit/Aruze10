// Wrapping this whole file in IF UNITY_EDITOR because we need it to not be in the Editor folder
// so we can call it from game code, but don't want it to get compiled for device builds which
// would break
#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace Zynga.Unity
{
/*
Class made to use refelection in order to get info about and alter the resolution of the GameView through code.
This allows for the simulation of switching between landscape and portrait mode for a device to be done in editor
without having to adjust the dropdown while the game is running.

Original Author: Scott Lepthien
Creation Date: 9/20/2018
*/
public static class GameViewSizeUtils
{
	// Enum representation of a Unity internal enum that tells what the width and height of a GameViewSize represents
	public enum GameViewSizeType
	{
		AspectRatio,
		FixedResolution
	}

	// Class to represent the internal Unity class for GameViewSize
	public class GameViewSize
	{
		public GameViewSize(int width, int height, GameViewSizeType sizeType, string name)
		{
			this.width = width;
			this.height = height;
			this.sizeType = sizeType;
			this.name = name;
		}

		public int width;
		public int height;
		public GameViewSizeType sizeType;
		public string name;
	}

	private static object gameViewSizesInstance;
	private static UnityEditor.EditorWindow gameView;
	private static MethodInfo getGroupMethod;
	private static GameViewSizeGroupType editorGroupType;

	static GameViewSizeUtils()
	{
		// Cache out some of the reflected objects we will be using for calls to this static class
		var sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
		var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
		var instanceProp = singleType.GetProperty("instance");
		getGroupMethod = sizesType.GetMethod("GetGroup");
		gameViewSizesInstance = instanceProp.GetValue(null, null);

		gameView = getMainGameView();

		editorGroupType = getCurrentGroupType();
	}

	// Add the Test Size resolution to the list of selectable resolutions
	[MenuItem("Zynga/GameViewSizeUtils/Add Test Size")]
	public static void addTestSize()
	{
		addCustomSize(GameViewSizeType.FixedResolution, 123, 456, "Test Size");
	}

	// Switch to the Test Size resolution (assuming it exists)
	[MenuItem("Zynga/GameViewSizeUtils/Switch To Test Size")]
	public static void switchToTestSize()
	{
		int idx = findSize(GameViewSizeType.FixedResolution, 123, 456);
		if (idx != -1)
		{
			setSelectedIndex(idx);
		}
	}

	// Remove the Test Size from the resolution list (assuming it exists)
	[MenuItem("Zynga/GameViewSizeUtils/Remove Test Size")]
	public static void removeTestSize()
	{
		removeCustomSize("Test size");
	}

	// Test switching to inverse resolution size
	[MenuItem("Zynga/GameViewSizeUtils/Switch To Inverse Resolution")]
	public static void testSwitchToInverse()
	{
		switchToInverse();
	}

	// Print out the info about the currently selected GameViewSize
	[MenuItem("Zynga/GameViewSizeUtils/Print Current GameViewSize")]
	public static void printCurrentGameViewSize()
	{
		GameViewSize viewSize = getCurrentGameViewSize();
		Debug.Log("GameViewSizeUtils.getCurrentGameViewSize() - name = " + viewSize.name
			+ "; sizeType = " + viewSize.sizeType
			+ "; sizeWidth = " + viewSize.width
			+ "; sizeHeight = " + viewSize.height);
	}

	// Check if the Test Size resolution exists by searching for it by name
	[MenuItem("Zynga/GameViewSizeUtils/Test doesSizeExist (by Name) For TestSize")]
	public static void checkTestSizeExistsByName()
	{
		Debug.Log("GameViewSizeUtils.checkTestSizeExistsByName() - \"Test Size\" exists = " + doesSizeExist("Test Size"));
	}

	// Check if the Test Size resolution exists by searching for it by size
	[MenuItem("Zynga/GameViewSizeUtils/Test doesSizeExist (by Size) For TestSize")]
	public static void checkTestSizeExistsBySize()
	{
		Debug.Log("GameViewSizeUtils.checkTestSizeExistsBySize() - \"Test Size\" exists = " + doesSizeExist(GameViewSizeType.FixedResolution, 123, 456));
	}

	// Log the current group type (iOS, Android, Standalone) that is being used
	[MenuItem("Zynga/GameViewSizeUtils/LogCurrentGroupType")]
	public static void logCurrentGroupType()
	{
		Debug.Log(getCurrentGroupType());
	}

	// Add a new custom size to the GameView resolution selection dropdown in the editor
	public static void addCustomSize(GameViewSizeType viewSizeType, int width, int height, string text)
	{
		var group = getGroup(editorGroupType);
		var addCustomSizeMethod = getGroupMethod.ReturnType.GetMethod("AddCustomSize"); // or group.GetType().
		var gvsType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSize");
		var gvsEnumType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeType");
		var ctor = gvsType.GetConstructor(new System.Type[] { gvsEnumType, typeof(int), typeof(int), typeof(string) });
		var newSize = ctor.Invoke(new object[] { (int)viewSizeType, width, height, text });
		addCustomSizeMethod.Invoke(group, new object[] { newSize });
	}

	// Remove a custom size from the list of selectable sizes
	public static void removeCustomSize(string name)
	{
		int index = findSize(name);
		if (index != -1)
		{
			var group = getGroup(editorGroupType);
			var removeCustomSizeMethod = getGroupMethod.ReturnType.GetMethod("RemoveCustomSize"); // or group.GetType().
			removeCustomSizeMethod.Invoke(group, new object[] { index });
		}
	}

	// Returns if a selectable resoltuion with the passed name exists
	public static bool doesSizeExist(string text)
	{
		return findSize(text) != -1;
	}

	// Find the index for a selectable resolution with the passed name
	// if the name isn't found then we will return -1
	public static int findSize(string text)
	{
		var group = getGroup(editorGroupType);
		var getDisplayTexts = group.GetType().GetMethod("GetDisplayTexts");
		var displayTexts = getDisplayTexts.Invoke(group, null) as string[];
		for (int i = 0; i < displayTexts.Length; i++)
		{
			string display = displayTexts[i];
			// the text we get is "Name (W:H)" if the size has a name, or just "W:H" e.g. 16:9
			// so if we're querying a custom size text we substring to only get the name
			// You could see the outputs by just logging
			// Debug.Log(display);
			display = trimSizeFromSizeDisplayName(display);

			if (display == text)
			{
				return i;
			}
		}
		return -1;
	}

	// Check if a size exists for a given sizeType with the passed width and height
	// i.e. can check if Aspect Ratio 6:19 exists or if Width/Height 1024x768 exists
	public static bool doesSizeExist(GameViewSizeType sizeType, int width, int height)
	{
		return findSize(sizeType, width, height) != -1;
	}

	// Find the index for the passed sizeType with the passed width and height
	// i.e. tries to find the index for something like Aspect Ratio 6:19 or Width/Height 1024x768
	// and returns the selectable index in the dropdown if it exists, otherwise returns -1
	public static int findSize(GameViewSizeType sizeType, int width, int height)
	{
		var group = getGroup(editorGroupType);
		var groupType = group.GetType();
		var getBuiltinCount = groupType.GetMethod("GetBuiltinCount");
		var getCustomCount = groupType.GetMethod("GetCustomCount");
		int sizesCount = (int)getBuiltinCount.Invoke(group, null) + (int)getCustomCount.Invoke(group, null);
		var getGameViewSize = groupType.GetMethod("GetGameViewSize");
		var gvsClassType = getGameViewSize.ReturnType;
		var gvsTypeProp = gvsClassType.GetProperty("sizeType");
		var widthProp = gvsClassType.GetProperty("width");
		var heightProp = gvsClassType.GetProperty("height");
		var displayTextProp = gvsClassType.GetProperty("displayText");
		var indexValue = new object[1];

		// Iterate through the different sizes that are selectable and see if we can find a match
		for (int i = 0; i < sizesCount; i++)
		{
			indexValue[0] = i;
			var size = getGameViewSize.Invoke(group, indexValue);
			int sizeWidth = (int)widthProp.GetValue(size, null);
			int sizeHeight = (int)heightProp.GetValue(size, null);
			GameViewSizeType currentSizeType = (GameViewSizeType)(int)gvsTypeProp.GetValue(size, null);
			string displayText = (string)displayTextProp.GetValue(size, null);
			if (currentSizeType == sizeType && sizeWidth == width && sizeHeight == height)
			{
				// Found a match, returning the index that is required to select it
				return i;
			}
		}

		// The size we were asked to find didn't exist
		return -1;
	}

	// Extract the current group type (i.e. iOS, Android, Standalone) and return it, we'll cache this value to reuse
	// (I'm almost positive it will get rebuilt when switching platforms in the editor)
	public static GameViewSizeGroupType getCurrentGroupType()
	{
		var prop = gameView.GetType().GetProperty("currentSizeGroupType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
		return (GameViewSizeGroupType)(int)prop.GetValue(gameView, new object[0] { });
	}

	// Extract the data about the currently select Editor GameView GameViewSize
	public static GameViewSize getCurrentGameViewSize()
	{
		// Extract the currentGameViewSize by hacking the GameView using reflection
		var prop = gameView.GetType().GetProperty("currentGameViewSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		var gvsize = prop.GetValue(gameView, new object[0] { });
		var gvSizeType = gvsize.GetType();

		// Extract data from the size info
		int sizeWidth = (int)gvSizeType.GetProperty("width", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).GetValue(gvsize, new object[0] { });
		int sizeHeight = (int)gvSizeType.GetProperty("height", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).GetValue(gvsize, new object[0] { });
		GameViewSizeType currentSizeType = (GameViewSizeType)(int)gvSizeType.GetProperty("sizeType", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).GetValue(gvsize, new object[0] { });
		string displayText = (string)gvSizeType.GetProperty("displayText", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).GetValue(gvsize, new object[0] { });

		return new GameViewSize(sizeWidth, sizeHeight, currentSizeType, displayText);
	}

	// Swap to the inverse size of the current GameViewSize, will ignore this call if in Free Aspect and will
	// create the inverse size if it doesn't exist so it can swap to it
	// Returns if a swap occured
	public static bool switchToInverse()
	{
		GameViewSize currentSize = getCurrentGameViewSize();

		// Ignore "Free Aspect" and also cases where the inverse is the already selected resolution
		if (currentSize.name != "Free Aspect" && currentSize.width != currentSize.height)
		{
			int inverseIndex = findSize(currentSize.sizeType, currentSize.height, currentSize.width);
			if (inverseIndex == -1)
			{
				// we need to create the inverse first then swap to it
				string trimmedName = trimSizeFromSizeDisplayName(currentSize.name);
				addCustomSize(currentSize.sizeType, currentSize.height, currentSize.width, trimmedName + " Inverse");
				inverseIndex = findSize(trimmedName + " Inverse");
			}

			if (inverseIndex != -1)
			{
				setSelectedIndex(inverseIndex);
				return true;
			}
			else
			{
				Debug.LogError("GameViewSizeUtils.switchToInverse() - Unable to create Inverse resolution called: " + currentSize.name + " Inverse");
				return false;
			}
		}
		else
		{
			if (currentSize.name == "Free Aspect")
			{
				// Ignoring "Free Aspect"
				Debug.LogWarning("GameViewSizeUtils.switchToInverse() - Cannot switch to inverse of Free Aspect");
			}
			else
			{
				// Width and Height are the same so the inverse is itself
				Debug.LogWarning("GameViewSizeUtils.switchToInverse() - Width and Height both = " + currentSize.width + " Inverse is itself");
			}
			return false;
		}
	}

	// Set what index is currently selected in the GameView resolution selection dropdown
	private static void setSelectedIndex(int index)
	{
		var gvWndType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
		var selectedSizeIndexProp = gvWndType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		var gvWnd = EditorWindow.GetWindow(gvWndType);
		selectedSizeIndexProp.SetValue(gvWnd, index, null);
	}

	// Get the currently select group object (this is the object that contains the list of resolutions for the current platform)
	private static object getGroup(GameViewSizeGroupType type)
	{
		return getGroupMethod.Invoke(gameViewSizesInstance, new object[] { (int)type });
	}

	// Use reflection to hack the Editor GameView so we can call reflected methods on it
	private static UnityEditor.EditorWindow getMainGameView()
	{
		System.Object Res;
#if UNITY_2019_4_OR_NEWER
		System.Type T = System.Type.GetType("UnityEditor.PlayModeView,UnityEditor");
		System.Reflection.MethodInfo GetMainGameView = T.GetMethod("GetMainPlayModeView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
		Res = GetMainGameView.Invoke(null, null);
#else
		System.Type T = System.Type.GetType("UnityEditor.GameView,UnityEditor");
		System.Reflection.MethodInfo GetMainGameView = T.GetMethod("GetMainGameView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
		Res = GetMainGameView.Invoke(null, null);
#endif
		return (UnityEditor.EditorWindow)Res;
	}

	// Remove the size info from the end of a name string, i.e. if full name string is "Test Size (123x346)" this will return "Test Size"
	private static string trimSizeFromSizeDisplayName(string displayName)
	{
		int pren = displayName.IndexOf('(');
		if (pren != -1)
		{
			displayName = displayName.Substring(0, pren - 1); // -1 to remove the space that's before the prens. This is very implementation-depdenent
		}

		return displayName;
	}
}
}
#endif
