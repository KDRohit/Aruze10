using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AnimatedValues;

/*
 * Editor class to help with assigning large numbers of labels at one time.
 */

[CustomEditor(typeof(PickingGameCreditPickItem), true), CanEditMultipleObjects]
public class ModularChallengeGameCreditPickItemEditor : ModularChallengeGameBasePickItemEditor
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

		AutoAssignmentTargetInput creditLabelInput = ModularChallengeGameEditorDisplayHelper.createField("credit", typeof(LabelWrapperComponent), "creditLabel", "Credit Label");
		autoInputs.Add(creditLabelInput);

		AutoAssignmentTargetInput grayCreditLabelInput = ModularChallengeGameEditorDisplayHelper.createField("creditGray", typeof(LabelWrapperComponent), "grayCreditLabel", "Gray Credit Label");
		autoInputs.Add(grayCreditLabelInput);

		AutoAssignmentTargetInput creditLabelAbbreviatedInput = ModularChallengeGameEditorDisplayHelper.createField("creditLabelAbbreviated", typeof(LabelWrapperComponent), "creditLabelAbbreviated", "Credit Label Abbreviated");
		autoInputs.Add(creditLabelAbbreviatedInput);

		AutoAssignmentTargetInput grayCreditLabelAbbreviatedInput = ModularChallengeGameEditorDisplayHelper.createField("grayCreditLabelAbbreviated", typeof(LabelWrapperComponent), "grayCreditLabelAbbreviated", "Gray Credit Label Abbreviated");
		autoInputs.Add(grayCreditLabelAbbreviatedInput);

	}

	public override void OnInspectorGUI()
	{
		EditorGUILayout.Separator();

		// Get the list of all the serialized fields to show in the editor
		List<string> propertiesToIncludeList = CommonReflection.getNamesOfAllSerializedFieldsForType(typeof(PickingGameCreditPickItem));
		
		serializedObject.Update();

		for (int i = 0; i < propertiesToIncludeList.Count; i++)
		{
			drawPropertyList(propertiesToIncludeList[i]);
		}

		serializedObject.ApplyModifiedProperties();


		EditorGUILayout.Separator();
		showSetupHelpers.target = EditorGUILayout.ToggleLeft("Show Credit Setup Helpers", showSetupHelpers.target);
		EditorGUILayout.Separator();

		if (EditorGUILayout.BeginFadeGroup(showSetupHelpers.faded))
		{
			renderAutoInputs();

		}
		EditorGUILayout.EndFadeGroup();
	}
}
