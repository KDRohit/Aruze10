using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LoadingFactory : IResetGame
{
	// =============================
	// PRIVATE
	// =============================
	// THEMES
	private static Dictionary<string, string> themePaths = new Dictionary<string, string>()
	{
		{ "max_voltage", "Features/Max Voltage/Loading Prefab/Max Voltage Loading" },
		{ "slotventures", "Features/Slotventures/Loading Assets/Slotventures Loading" },
		{ COLLECTABLES_LOADING_THEME_KEY, "Features/Collections/Prefabs/Loading/Collections Loading" },
		{ "elite_pass", "Features/Collections/Prefabs/Loading/Collections Loading" }
	};

	// WIDGETS
	private static Dictionary<string, string> widgetPaths = new Dictionary<string, string>()
	{
		{ "logo", "Loading Widgets/Logo Widget" },
		{ "game_image", "Loading Widgets/Game Image Widget" }
	};

	private static Dictionary<string, LoadingWidget> _widgets = new Dictionary<string, LoadingWidget>();


	public static GameObject currentTheme { get; private set; }
	public static LoadingTheme themeObject { get; private set; }

	public const string COLLECTABLES_LOADING_THEME_KEY = "collectables";

	/*=========================================================================================
	THEMES
	=========================================================================================*/
	/// <summary>
	///   Returns the prefab path for a specified theme name. If no theme is found, returns null.
	/// </summary>
	public static string getThemePrefab(string themeName)
	{
		if (themePaths.ContainsKey(themeName))
		{
			return themePaths[themeName];
		}
		
		return null;
	}

	public static void loadTheme(string themeName)
	{
		string path = getThemePrefab(themeName);

		if (!string.IsNullOrEmpty(path))
		{
			AssetBundleManager.load(path, onThemeLoaded, onThemeFailed);
		}
		else
		{
			Debug.LogError("LoadingFactory: No path set for loading theme: " + themeName);
		}
	}

	private static void onThemeLoaded(string assetPath, Object obj, Dict data = null)
	{
		if (currentTheme != null)
		{
			GameObject.Destroy(currentTheme);
		}
		currentTheme = obj as GameObject;
		GameObject go = CommonGameObject.instantiate(currentTheme) as GameObject;
		themeObject = go.GetComponent(typeof(LoadingTheme)) as LoadingTheme;

		// turn it off by default
		if (themeObject != null)
		{
			themeObject.hide();
		}
	}

	private static void onThemeFailed(string assetPath, Dict data = null)
	{
		Debug.LogError("LoadingFactory: Failed to download loading theme at: " + assetPath);
	}

	/*=========================================================================================
	WIDGETS
	=========================================================================================*/
	public static string getWidgetPath(string widgetName)
	{
		if (widgetPaths.ContainsKey(widgetName))
		{
			return widgetPaths[widgetName];
		}

		return null;
	}

	public static void loadWidget(string widgetName)
	{
		string path = getWidgetPath(widgetName);

		if (!string.IsNullOrEmpty(path))
		{
			AssetBundleManager.load(path, onWidgetLoaded, onWidgetFailed, Dict.create(D.WIDGET, widgetName));
		}
		else
		{
			Debug.LogError("LoadingWidgetFactory: No path set for loading widget: " + widgetName);
		}
	}

	private static void onWidgetLoaded(string assetPath, Object obj, Dict data = null)
	{
		GameObject currentWidget = obj as GameObject;
		GameObject go = CommonGameObject.instantiate(currentWidget) as GameObject;
		LoadingWidget widget = go.GetComponent(typeof(LoadingWidget)) as LoadingWidget;

		// turn it off by default
		if (widget != null)
		{
			widget.hide();
		}

		if (!widgets.ContainsValue(widget))
		{
			widgets.Add((string)data.getWithDefault(D.WIDGET, assetPath), widget);
		}
	}

	private static void onWidgetFailed(string assetPath, Dict data = null)
	{
		Debug.LogError("LoadingFactory: Failed to download loading theme at: " + assetPath);
	}

	public static Dictionary<string, LoadingWidget> widgets
	{
		get
		{
			return _widgets;
		}
	}

	public static void resetStaticClassData()
	{
		_widgets = new Dictionary<string, LoadingWidget>();
		themeObject = null;
		currentTheme = null;
	}
}