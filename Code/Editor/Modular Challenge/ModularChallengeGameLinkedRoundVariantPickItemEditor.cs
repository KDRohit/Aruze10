using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AnimatedValues;

/*
 * Editor class to help with assigning large numbers of labels at one time.
 */

[CustomEditor(typeof(PickingGameLinkedRoundVariantPickItem), true), CanEditMultipleObjects]
public class ModularChallengeGameLinkedRoundVariantPickItemEditor : ModularChallengeGameBasePickItemEditor
{
	new protected void OnEnable()
	{
		base.OnEnable();
	}
	public override void OnInspectorGUI()
	{
		EditorGUILayout.Separator();
		// display only the object properties that we need
		serializedObject.Update();
		drawPropertyList("_linkedRound");
		serializedObject.ApplyModifiedProperties();
	}
}
