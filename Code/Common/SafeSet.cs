using UnityEngine;
using TMPro;
using System.Collections.Generic;

/*
 * A common class for performing actions on an object that may be null (and should be null in those cases).
 */

public static class SafeSet
{
	// Setting a UILabel's text attribute.
	public static bool labelText(UILabel label, string text, bool shouldLog = false)
	{
		if (label == null)
		{
			return logError("SafeSet -- trying to set null label", shouldLog);
		}
		label.text = text;
		return true;
	}

	// Setting a TextMeshPro's text attribute.
	public static bool labelText(TextMeshPro label, string text, bool shouldLog = false)
	{
		if (label == null)
		{
			return logError("SafeSet -- trying to set null label", shouldLog);
		}
		label.text = text;
		return true;
	}

	// Setting a LabelWrapper's text attribute.
	public static bool labelText(LabelWrapper label, string text, bool shouldLog = false)
	{
		if (label == null)
		{
			return logError("SafeSet -- trying to set null label", shouldLog);
		}
		label.text = text;
		return true;
	}

	// Setting a TextMeshPro's color attribute.
	public static bool labelColor(TextMeshPro label, Color color, bool shouldLog = false)
	{
		if (label == null)
		{
			return logError("SafeSet -- trying to set null color", shouldLog);
		}
		label.color = color;
		return true;
	}

	// Setting a gameObject to be active or inactive.
	public static bool gameObjectActive(GameObject go, bool isActive, bool shouldLog = false)
	{
		if (go == null)
		{
			return logError("SafeSet -- trying to SetActive on null GameObject", shouldLog);
		}
		go.SetActive(isActive);
		return true;
	}

    // Takes a list of GameObjects, and sets each to active or inactive
    public static void gameObjectListActive(List<GameObject> gameObjectList, bool active)
	{
		if (gameObjectList != null)
		{
			foreach (GameObject gameObject in gameObjectList)
			{
				SafeSet.gameObjectActive(gameObject, active);
			}
		}
	}

	// Setting a gameObject to be active or inactive.
	public static bool componentGameObjectActive(Component component, bool isActive, bool shouldLog = false)
	{
		if (component == null)
		{
			return logError("SafeSet -- trying to SetActive on null Component", shouldLog);
		}
		component.gameObject.SetActive(isActive);
		return true;
	}
	
	// Setting the level of a vipIconLevel with a level integer.
	public static bool vipIconLevel(VIPNewIcon icon, int level, bool shouldLog = false)
	{
		if (icon == null)
		{
			return logError("SafeSet -- trying to setLevel on null VIPNewIcon", shouldLog);
		}
		icon.setLevel(level);
		return true;
	}

	// Setting the level of a vipIconLevel with a VIPLevel object.
	public static bool vipIconLevel(VIPNewIcon icon, VIPLevel level, bool shouldLog = false)
	{
		if (icon == null)
		{
			return logError("SafeSet -- trying to setLevel on null VIPNewIcon", shouldLog);
		}
		icon.setLevel(level);
		return true;
	}

	public static bool registerEventDelegate(ClickHandler handler, ButtonHandler.onClickDelegate function, Dict args = null)
	{
		if (handler != null)
		{
			handler.registerEventDelegate(function, args);
			return true;
		}
		return false;
	}

	public static void spriteSize(UISprite sprite, float targetWidth, float targetHeight)
	{
		if (sprite != null)
		{
			Vector3 scale = sprite.transform.localScale;
			scale.x = targetWidth;
			scale.y = targetHeight;
			sprite.transform.localScale = scale;
		}
	}

	public static void spriteWidth(UISprite sprite, float targetWidth)
	{
		if (sprite != null)
		{
			Vector3 scale = sprite.transform.localScale;
			scale.x = targetWidth;
			sprite.transform.localScale = scale;
		}
	}

	public static void spriteSize(UISprite sprite, float targetHeight)
	{
		if (sprite != null)
		{
			Vector3 scale = sprite.transform.localScale;
			scale.y = targetHeight;
			sprite.transform.localScale = scale;
		}
	}

	// Logs an error if necessary, and returns false so that calling functions can log and return in one line.
	private static bool logError(string message, bool shouldLog)
	{
		if (shouldLog)
		{
			Debug.LogError(message);
		}
		return false;
	}
}
