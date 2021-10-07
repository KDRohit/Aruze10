using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AnimatedValues;

/*
 * Editor class to help with assigning large numbers of labels at one time.
 */
[CustomEditor(typeof(PickingGameMultiCreditPickItem), true), CanEditMultipleObjects]
public class ModularChallengeGameMultiCreditPickItemEditor : ModularChallengeGameCreditPickItemEditor
{
	protected override void OnEnable()
	{
		showSetupHelpers = new AnimBool(false);
		showSetupHelpers.valueChanged.AddListener(Repaint);

		base.OnEnable();
	}

	protected override void createAutoInputs()
	{
		autoInputs = new List<AutoAssignmentTargetInput>();

		AutoAssignmentTargetInput creditLabelInput = ModularChallengeGameEditorDisplayHelper.createField("extraCreditLabels", typeof(LabelWrapperComponent), "extraCreditLabels", "Credit Labels");
		autoInputs.Add(creditLabelInput);

		AutoAssignmentTargetInput grayCreditLabelInput = ModularChallengeGameEditorDisplayHelper.createField("extraGrayCreditLabels", typeof(LabelWrapperComponent), "extraGrayCreditLabels", "Gray Credit Labels");
		autoInputs.Add(grayCreditLabelInput);

	}

	public override void OnInspectorGUI()
	{
		EditorGUILayout.Separator();

		// display only the object properties that we need
		serializedObject.Update();
		drawPropertyList("extraCreditLabels");
		drawPropertyList("extraGrayCreditLabels");
		serializedObject.ApplyModifiedProperties();


		EditorGUILayout.Separator();
		showSetupHelpers.target = EditorGUILayout.ToggleLeft("Show Extra Credit Setup Helpers", showSetupHelpers.target);
		EditorGUILayout.Separator();

		if (EditorGUILayout.BeginFadeGroup(showSetupHelpers.faded))
		{
			renderAutoInputs();

		}
		EditorGUILayout.EndFadeGroup();
	}
}
