using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AnimatedValues;

/*
 * Editor class to help with assigning large numbers of labels at one time.
 */

[CustomEditor(typeof(PickingGameIncreasePicksPickItem), true), CanEditMultipleObjects]
public class ModularChallengeGameIncreasePicksPickItemEditor : ModularChallengeGameBasePickItemEditor
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

		AutoAssignmentTargetInput increaseLabelInput = ModularChallengeGameEditorDisplayHelper.createField("plus_1_pick", typeof(LabelWrapperComponent), "increaseLabel", "Increase Label");
		autoInputs.Add(increaseLabelInput);

		AutoAssignmentTargetInput grayIncreaseLabelInput = ModularChallengeGameEditorDisplayHelper.createField("plus_1_pick", typeof(LabelWrapperComponent), "grayIncreaseLabel", "Gray Increase Label");
		autoInputs.Add(grayIncreaseLabelInput);
	}

	public override void OnInspectorGUI()
	{
		EditorGUILayout.Separator();

		// display only the object properties that we need
		serializedObject.Update();
		drawPropertyList("increaseLabel");
		drawPropertyList("grayIncreaseLabel");

		drawPropertyList("labelFormatString");

		serializedObject.ApplyModifiedProperties();


		EditorGUILayout.Separator();
		showSetupHelpers.target = EditorGUILayout.ToggleLeft("Show Increase Picks Setup Helpers", showSetupHelpers.target);
		EditorGUILayout.Separator();

		if (EditorGUILayout.BeginFadeGroup(showSetupHelpers.faded))
		{
			renderAutoInputs();
		}

		EditorGUILayout.EndFadeGroup();
	}
}
