using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyAttributeDrawer : PropertyDrawer
{
	public override void OnGUI(Rect rect, UnityEditor.SerializedProperty prop, GUIContent label)
	{
		bool wasEnabled = GUI.enabled;
		GUI.enabled = false;
		EditorGUI.PropertyField(rect,prop);
		GUI.enabled = wasEnabled;
	}
}