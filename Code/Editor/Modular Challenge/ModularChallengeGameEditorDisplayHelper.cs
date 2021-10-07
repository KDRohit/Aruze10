using UnityEngine;
using UnityEditor;
using System.Collections;

/*
 * Data class for autoAssignment fields
 */
public class AutoAssignmentTargetInput
{
	public string targetName;		// name of the child transform to get component from
	public System.Type targetType;	// type of component
	public string targetField;		// field on the pick item to fill with this field

	public string fieldLabel;		// label for the field
	public bool useFirstFound;		// use the first item found?

	public string buttonLabel;		// label for the button
	public Color buttonColor;		// color for the button

	public delegate void buttonCallback(Object target, AutoAssignmentTargetInput input); // callback function for clicking / assignin
	public buttonCallback clickFunction;

	public bool includeAddButton = false;
}

// more generic button class for use with non auto-assign functions
public class ModularChallengeGameCustomEditorButton
{
	public string buttonLabel;		// label for the button
	public Color buttonColor;		// color for the button

	public delegate void ButtonCallback(Object target); // callback function for clicking / assignin
	public ButtonCallback clickFunction;
}


/**
 * Helper class for editor functions commonly used in ModularChallengeGame setup inspector creation
 */
public class ModularChallengeGameEditorDisplayHelper : ScriptableObject
{
	public static AutoAssignmentTargetInput renderField(AutoAssignmentTargetInput input, Object[] targets)
	{
		// main box indent
		EditorGUILayout.Separator();
		EditorGUI.indentLevel++;

		// item target name
		input.targetName = EditorGUILayout.TextField(input.fieldLabel, input.targetName);

		// item first found boolean
		EditorGUI.indentLevel++;
		input.useFirstFound = EditorGUILayout.Toggle("Use First Found", input.useFirstFound);
		EditorGUI.indentLevel--;


		Color previousBackgroundColor = GUI.color;

		// render button
		GUI.color = input.buttonColor;
		if (GUILayout.Button(input.buttonLabel))
		{
			// execute for each target
			foreach (Object target in targets)
			{
				input.clickFunction(target, input);
			}
		}
		GUI.color = previousBackgroundColor;

		if (input.includeAddButton)
		{
			string addButtonLabel = "Add " + input.targetType + " Components";
			if (GUILayout.Button(addButtonLabel))
			{
				// execute for each target
				foreach (Object target in targets)
				{
					ModularChallengeGameEditorHelper.addComponentOnTargetChildDynamic(target, input.targetType, input.targetName, !input.useFirstFound);
				}
			}
		}

		// main box outdent
		EditorGUI.indentLevel--;
		EditorGUILayout.Separator();
		return input;
	}

	// Create an auto assignment input for use
	public static AutoAssignmentTargetInput createField(string targetName, System.Type targetType, string targetField, string fieldLabel, bool useFirstFound = true, Color buttonColor = default(Color), string buttonLabel = null, AutoAssignmentTargetInput.buttonCallback callback = null)
	{
		AutoAssignmentTargetInput input = new AutoAssignmentTargetInput();

		input.targetName = targetName;
		input.targetType = targetType;
		input.targetField = targetField;

		input.fieldLabel = fieldLabel;
		input.useFirstFound = useFirstFound;

		// default to assignment label, allowing custom button callback labelling
		if (buttonLabel == null)
		{
			buttonLabel = "Assign " + fieldLabel + " to " + targetField;
		}
		input.buttonLabel = buttonLabel;

		// default color property is invisible.
		if (buttonColor == default(Color))
		{
			buttonColor = Color.green;
		}
		input.buttonColor = buttonColor;

		// default to auto-assignment, allowing custom button callbacks
		if (callback == null)
		{
			callback = ModularChallengeGameEditorHelper.autoSetFieldProperty;
		}

		input.clickFunction += callback;

		return input;
	}

	// create a custom clear component button shortcut
	public static void renderClearComponentsButton<T>(Object[] targets) where T : Component
	{
		Color originalBackground = GUI.backgroundColor;

		GUI.backgroundColor = Color.red;
		if (GUILayout.Button("Clear " + typeof(T) + " Components on Children"))
		{
			ModularChallengeGameEditorHelper.clearComponents<T>(targets);
		}
		GUI.backgroundColor = originalBackground;
		
	}
}
