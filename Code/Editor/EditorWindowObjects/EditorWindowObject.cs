using UnityEngine;
using UnityEditor;

/*
	Class Name: EditorWindowObject.cs
	Author: Michael Christensen-Calvin <mchristensencalvin@zynga.com>
	Description: Base class for EditorWindowObjects, so that an array of them can be used to draw a series of them in a custom window.
*/

public class EditorWindowObject
{
    [SerializeField] private bool doShowContents = false;
	[SerializeField] private bool doShowDescription = false;

	protected virtual string getButtonLabel()
	{
		return "Button";
	}


	protected virtual string getDescriptionLabel()
	{
		return "Description Needed";
	}

	public virtual void drawGUI(Rect position)
	{
		drawButton();
		if (doShowContents)
		{
			doShowDescription = EditorGUILayout.Foldout(doShowDescription, "Show Description");
			if (doShowDescription)
			{
				EditorGUILayout.LabelField(getDescriptionLabel());
			}
			drawGuts(position);
		}
	}

	public virtual void drawGuts(Rect position)
	{
		// Should be overridden by subclass to display the contents.
	}

	public virtual void drawButton()
	{
		GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
		buttonStyle.normal.textColor = Color.white;
		GUI.backgroundColor = doShowContents ? Color.red : Color.green;
		string buttonLabel = string.Format(getButtonLabel() + ": {0}", showOrHide(doShowContents));

		if (GUILayout.Button(buttonLabel, buttonStyle))
		{
			doShowContents = !doShowContents;
		}
		GUI.backgroundColor = Color.white;
	}

	private string showOrHide(bool toggled)
	{
		return toggled ? "Hide" : "Show";
	}

	// Show a green/red button with Hide or Show appeneded to the end of the label.
	// By default value is assumed to be positive when showing content.
	protected bool showHideButton(bool value, string label, out bool newValue, bool swapValueMeaning = false)
	{
		bool result = value;
		Color originalColor = GUI.backgroundColor;
		bool tweakedValue = swapValueMeaning ? !value : value;
		GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
		buttonStyle.normal.textColor = Color.white;
		GUI.backgroundColor = tweakedValue ? Color.red : Color.green;
		string showHide = tweakedValue ? "Hide" : "Show";
		string buttonLabel = string.Format(label + " : " + showHide);
		if (GUILayout.Button(buttonLabel, buttonStyle))
		{
			result = !value;
		}
		GUI.backgroundColor = originalColor;
		newValue = result;
		return newValue;
	}

	protected string getInputFilter(string label, string currentValue, out string outputValue)
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label(label);
		string result = GUILayout.TextField(currentValue);
		GUILayout.EndHorizontal();
		outputValue = result;
		return outputValue;
		//return result;
	}

}
