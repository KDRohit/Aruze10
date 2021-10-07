using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/**
Class to handle custom property handling for the SerializedForcedOutcomeData class used for forced outcomes,
right now this is just used to display the key associated with the outcome type currently selected.

Original Author: Scott Lepthien
Creation Date: June 7, 2017
*/
[CustomPropertyDrawer (typeof (SlotBaseGame.SerializedForcedOutcomeData))]
public class SerializedForcedOutcomeDataDrawer : PropertyDrawer 
{
	// Renders the custom stuff to the gui
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		SerializedProperty serializedForcedOutcomeType = property.FindPropertyRelative("forcedOutcomeType");
		if (serializedForcedOutcomeType != null)
		{
			SlotBaseGame.ForcedOutcomeTypeEnum forcedOutcomeType = (SlotBaseGame.ForcedOutcomeTypeEnum)serializedForcedOutcomeType.enumValueIndex;
			label.text = SlotBaseGame.SerializedForcedOutcomeData.getKeyCodeForForcedOutcomeType(forcedOutcomeType, isForTramp: false) + " - " + forcedOutcomeType;
		}

		EditorGUI.PropertyField(position, property, label, true);
	}

	// Gets the total property height for everything that will be rendered as part of the AnimationListController.AnimationInformation
	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		return EditorGUI.GetPropertyHeight(property);
	}
}
