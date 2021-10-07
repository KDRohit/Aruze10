using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/*
Custom handling for AnimationListController.AnimationInformationList that controls what is visible based
on what is checked on this serialized object.
*/
[CustomPropertyDrawer (typeof (AnimationListController.AnimationInformationList))]
public class AnimationListControllerAnimationInformationListDrawer : PropertyDrawer 
{
	// Renders the custom stuff to the gui
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		// BeginProperty will forward the tooltip info to this PropertyDrawer
		label = EditorGUI.BeginProperty(position, label, property);

		EditorGUIUtility.labelWidth = 0;
		EditorGUIUtility.fieldWidth = 0;

		bool isExpanded = property.isExpanded;
		
		bool isAllowingTapToSkip = property.FindPropertyRelative("isAllowingTapToSkip").boolValue;

		List<CommonPropertyDrawer.CustomPropertyBase> customProperties = getCustomPropertyList(property, label, isAllowingTapToSkip);

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
	private List<CommonPropertyDrawer.CustomPropertyBase> getCustomPropertyList(SerializedProperty property, GUIContent label, bool isAllowingTapToSkip)
	{
		float standardHeight = base.GetPropertyHeight(property, label);
		List<CommonPropertyDrawer.CustomPropertyBase> customProperties = new List<CommonPropertyDrawer.CustomPropertyBase>();
		
		// Get the type so we can extract the serialized fields from it
		System.Type targetObjectClassType = CommonReflection.getIndividualElementType(fieldInfo.FieldType);
		List<string> propertiesToIncludeList = CommonReflection.getNamesOfAllSerializedFieldsForType(targetObjectClassType);

		if (!isAllowingTapToSkip)
		{
			// only show the server cheat toggle
			propertiesToIncludeList.Remove("skippedAnimsFinalStatesList");
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
			bool isAllowingTapToSkip = property.FindPropertyRelative("isAllowingTapToSkip").boolValue;
			
			// get the property list but don't worry about the animation names as they aren't needed to determine the height
			List<CommonPropertyDrawer.CustomPropertyBase> customProperties = getCustomPropertyList(property, label, isAllowingTapToSkip);
			float totalListHeight = CommonPropertyDrawer.getCustomPropertyListHeight(customProperties, standardHeight);

			return standardHeight + totalListHeight;
		}
		else
		{
			return standardHeight;
		}
	}
}
