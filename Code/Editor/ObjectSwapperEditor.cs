using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ObjectSwapperAttribute))]
[System.Obsolete("Animation list should be used instead of object swapper")]
public class ObjectSwapperEditor : PropertyDrawer
{
	private string lastState = "";
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		ObjectSwapper swapper = property.serializedObject.targetObject as ObjectSwapper;

		if (swapper != null && swapper.currentStates.Count > 0)
		{
			List<string> states = new List<string>();
			swapper.currentStates.ForEach(item => { states.Add(item); });

			for (int i = 0; i < states.Count; ++i)
			{
				states[i] = TrimText(states[i]);
			}

			int index = Mathf.Max(0, Array.IndexOf(states.ToArray(), property.stringValue));
			index = EditorGUI.Popup(position, TrimText(property.displayName), index, states.ToArray());

			if (property.propertyType == SerializedPropertyType.String)
			{
				property.stringValue = states[index];
			}
		}
		else
		{
			EditorGUI.PropertyField(position,property);
		}
	}

	private static string TrimText(string stateText)
	{
		Regex rex = new Regex(@"(.*)\s+\((\d+)(\s+)?occurrences.*");
		stateText = rex.Replace(stateText, "$1");
		return stateText;
	}
}