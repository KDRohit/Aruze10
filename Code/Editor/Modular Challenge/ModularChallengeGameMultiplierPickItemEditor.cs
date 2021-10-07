using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AnimatedValues;

/*
 * Editor class to help with assigning large numbers of labels at one time.
 */

[CustomEditor(typeof(PickingGameMultiplierPickItem), true), CanEditMultipleObjects]
public class ModularChallengeGameMultiplierPickItemEditor : ModularChallengeGameBasePickItemEditor
{
	new protected void OnEnable()
	{
		showSetupHelpers = new AnimBool(false); 
		showSetupHelpers.valueChanged.AddListener(Repaint);

		base.OnEnable();
	}

	protected override void createAutoInputs()
	{
		autoInputs = new List<AutoAssignmentTargetInput>();

		AutoAssignmentTargetInput multiplierLabelInput = ModularChallengeGameEditorDisplayHelper.createField("multiplierLabel", typeof(LabelWrapperComponent), "multiplierLabel", "Multiplier Label");
		autoInputs.Add(multiplierLabelInput);

		AutoAssignmentTargetInput grayMultiplierLabelInput = ModularChallengeGameEditorDisplayHelper.createField("grayMultiplierLabel", typeof(LabelWrapperComponent), "grayMultiplierLabel", "Gray Multiplier Label");
		autoInputs.Add(grayMultiplierLabelInput);
	}

	public override void OnInspectorGUI()
	{
		EditorGUILayout.Separator();

		// display only the object properties that we need
		serializedObject.Update();
		drawPropertyList("multiplierLabel");
		drawPropertyList("grayMultiplierLabel");
		serializedObject.ApplyModifiedProperties();


		EditorGUILayout.Separator();
		showSetupHelpers.target = EditorGUILayout.ToggleLeft("Show Multipier Setup Helpers", showSetupHelpers.target);
		EditorGUILayout.Separator();

		if (EditorGUILayout.BeginFadeGroup(showSetupHelpers.faded))
		{
			renderAutoInputs();

		}
		EditorGUILayout.EndFadeGroup();
	}
}
