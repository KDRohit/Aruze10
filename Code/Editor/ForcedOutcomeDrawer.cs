using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/**
Class to handle custom property handling for the ForcedOutcome class used for forced outcomes,
this will allow the display to be customized based on the state of having and/or using a server
outcome cheat code instead of a tier and outcome data that we used to define on the client.

Original Author: Scott Lepthien
Creation Date: 3/29/2018
*/
[CustomPropertyDrawer (typeof (SlotBaseGame.ForcedOutcome))]
public class ForcedOutcomeDrawer : PropertyDrawer 
{
	// Renders the custom stuff to the gui
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		// BeginProperty will forward the tooltip info to this PropertyDrawer
		label = EditorGUI.BeginProperty(position, label, property);
		
		EditorGUIUtility.labelWidth = 0;
		EditorGUIUtility.fieldWidth = 0;

		SerializedProperty serializedHasServerCheatKey = property.FindPropertyRelative("serverCheatKey");
		string serverCheatKey = serializedHasServerCheatKey.stringValue;
		bool hasServerCheatKey = !string.IsNullOrEmpty(serverCheatKey);
		SerializedProperty serializedIsUsingServerCheat = property.FindPropertyRelative("isUsingServerCheat");
		bool isUsingServerCheat = serializedIsUsingServerCheat.boolValue;

		bool isExpanded = true;
		isExpanded = property.isExpanded;

		List<CommonPropertyDrawer.CustomPropertyBase> customProperties = getCustomPropertyList(property, label, hasServerCheatKey, isUsingServerCheat);

		float standardHeight = base.GetPropertyHeight(property, label);

		Rect posRect = new Rect(position.x, position.y, position.width, standardHeight);

		if (!isExpanded)
		{
			posRect = new Rect(position.x, position.y, position.width, position.height);
		}

		CommonPropertyDrawer.CustomFoldout mainDropdown = new CommonPropertyDrawer.CustomFoldout(property, property.displayName, isExpanded, standardHeight, label.tooltip);
		mainDropdown.drawProperty(ref posRect);

		isExpanded = property.isExpanded = mainDropdown.isExpanded;

		if (isExpanded) // Only show the other fields if it's expanded.
		{
			CommonPropertyDrawer.drawCustomPropertyList(customProperties, ref posRect);
		}

		//This sets the indent level back after the CommonPropertyDrawer.CustomFoldout indents it
		EditorGUI.indentLevel -= 1;

		EditorGUI.EndProperty();
	}

	// Get the list of properties that will be displayed, these are used to determine the total size of all the properties
	// as well as to actually render them
	private List<CommonPropertyDrawer.CustomPropertyBase> getCustomPropertyList(SerializedProperty property, GUIContent label, bool hasServerCheatKey, bool isUsingServerCheat)
	{
		float standardHeight = base.GetPropertyHeight(property, label);
		List<CommonPropertyDrawer.CustomPropertyBase> customProperties = new List<CommonPropertyDrawer.CustomPropertyBase>();

		List<string> propertiesToIncludeList;

		if (hasServerCheatKey && isUsingServerCheat)
		{
			// only show the server cheat toggle
			propertiesToIncludeList = new List<string> {"isUsingServerCheat"};
		}
		else
		{
			// Show everything
			
			// Get the type so we can extract the serialized fields from it
			System.Type targetObjectClassType = CommonReflection.getIndividualElementType(fieldInfo.FieldType);
			propertiesToIncludeList = CommonReflection.getNamesOfAllSerializedFieldsForType(targetObjectClassType);

			if (!hasServerCheatKey)
			{
				// if we don't have a server cheat key don't show the toggle for it
				propertiesToIncludeList.Remove("isUsingServerCheat");
			}
		}

		CommonPropertyDrawer.addPropertiesToCustomList(property, ref customProperties, standardHeight, propertyNameList: propertiesToIncludeList, includeChildren: false);

		return customProperties;
	}

	// Gets the total property height for everything that will be rendered as part of the AnimationListController.AnimationInformation
	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		bool isExpanded = property.isExpanded;

		float standardHeight = base.GetPropertyHeight(property, label);

		if (isExpanded)
		{
			SerializedProperty serializedHasServerCheatKey = property.FindPropertyRelative("serverCheatKey");
			string serverCheatKey = serializedHasServerCheatKey.stringValue;
			bool hasServerCheatKey = !string.IsNullOrEmpty(serverCheatKey);
			SerializedProperty serializedIsUsingServerCheat = property.FindPropertyRelative("isUsingServerCheat");
			bool isUsingServerCheat = serializedIsUsingServerCheat.boolValue;

			// get the property list so we can determine the height
			List<CommonPropertyDrawer.CustomPropertyBase> customProperties = getCustomPropertyList(property, label, hasServerCheatKey, isUsingServerCheat);
			float totalListHeight = CommonPropertyDrawer.getCustomPropertyListHeight(customProperties, standardHeight);

			return standardHeight + totalListHeight;
		}
		else
		{
			return standardHeight;
		}
	}
}
