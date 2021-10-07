using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/**
 * PropertyDrawer to allow SlotModule.SortedSlotModuleEventType to be sorted when displayed in the Unity inspector.
 * Has to handle correctly mapping the Enum value so that it gets serialized correctly.
 *
 * Original Author: Scott Lepthien
 * Creation Date: 12/9/2019
 */
[CustomPropertyDrawer (typeof (SlotModule.SortedSlotModuleEventType))]
public class SortedSlotModuleEventTypeDrawer : PropertyDrawer
{
	private static List<string> sortedEnumValueNames = new List<string>();

	// Renders the custom stuff to the gui
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		// BeginProperty will forward the tooltip info to this PropertyDrawer
		label = EditorGUI.BeginProperty(position, label, property);
		
		EditorGUIUtility.labelWidth = 0;
		EditorGUIUtility.fieldWidth = 0;
		
		bool isExpanded = true;
		isExpanded = property.isExpanded;
		
		List<CommonPropertyDrawer.CustomPropertyBase> customProperties = getCustomPropertyList(property, label);
		float standardHeight = base.GetPropertyHeight(property, label);
		
		Rect posRect = new Rect(position.x, position.y, position.width, standardHeight);

		if (!isExpanded)
		{
			posRect = new Rect(position.x, position.y, position.width, position.height);
		}

		CommonPropertyDrawer.CustomFoldout mainDropdown = new CommonPropertyDrawer.CustomFoldout(property, label.text, isExpanded, standardHeight, label.tooltip);
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
	private List<CommonPropertyDrawer.CustomPropertyBase> getCustomPropertyList(SerializedProperty property, GUIContent label)
	{
		float standardHeight = base.GetPropertyHeight(property, label);
			
		List<string> propertiesToIncludeList = new List<string>();

		List<CommonPropertyDrawer.CustomPropertyBase> customProperties = new List<CommonPropertyDrawer.CustomPropertyBase>();
		
		SerializedProperty slotEventProperty = property.FindPropertyRelative("slotEvent");
		int selectedSlotEventEnumIndex = slotEventProperty.enumValueIndex;
		SlotModule.SlotModuleEventType selectedSlotEvent = getEnumTypeForValueIndex(selectedSlotEventEnumIndex);

		// Make sure that the sorted list of enum value names is created.  Storing it in a static
		// since we should only need to build it once each time we recompile.
		if (sortedEnumValueNames.Count <= 0)
		{
			foreach (string enumName in Enum.GetNames(typeof(SlotModule.SlotModuleEventType)))
			{
				sortedEnumValueNames.Add(enumName);
			}

			sortedEnumValueNames.Sort();
		}
		
		// Convert selectedSlotEvent to the correct Sorted index
		int sortedSelectedSlotEventIndex = getSortedIndexForSlotModuleEventTypeName(Enum.GetName(typeof(SlotModule.SlotModuleEventType), selectedSlotEvent));

		customProperties.Add(new CommonPropertyDrawer.CustomPopup(property, slotEventProperty.name, sortedSelectedSlotEventIndex, sortedEnumValueNames.ToArray(), standardHeight, "", onSlotEventValueChanged));

		CommonPropertyDrawer.addPropertiesToCustomList(property, ref customProperties, standardHeight, propertyNameList: propertiesToIncludeList, includeChildren: false);
		
		return customProperties;
	}
	
	// Triggered when the enum value is changed
	public void onSlotEventValueChanged(CommonPropertyDrawer.CustomPopup popup, int selectedIndex)
	{
		SerializedProperty property = popup.parentProperty;
		SerializedProperty slotEventProperty = property.FindPropertyRelative("slotEvent");

		int newEnumValueIndex = getEnumValueIndexForEnumType(sortedEnumValueNames[selectedIndex]);

		if (newEnumValueIndex != slotEventProperty.enumValueIndex)
		{
			slotEventProperty.enumValueIndex = newEnumValueIndex;
		}
	}

	// Get the index in the sorted list for the passed in enum name
	private static int getSortedIndexForSlotModuleEventTypeName(string enumValueName)
	{
		for (int i = 0; i < sortedEnumValueNames.Count; i++)
		{
			if (sortedEnumValueNames[i] == enumValueName)
			{
				return i;
			}
		}
		
		Debug.LogWarning("SortedSlotModuleEventTypeDrawer.getSortedIndexForSlotModuleEventTypeName() - Unable to find match for enumValueName = " + enumValueName + "; returning -1!");
		return -1;
	}

	// The way Unity stores the enum is not by the enum values, but by index into
	// the array of enum values.
	private static SlotModule.SlotModuleEventType getEnumTypeForValueIndex(int index)
	{
		int currentIndex = 0;
		foreach (SlotModule.SlotModuleEventType typeEnum in Enum.GetValues(typeof(SlotModule.SlotModuleEventType)))
		{
			if (currentIndex == index)
			{
				return typeEnum;
			}

			currentIndex++;
		}
		
		// If we can't match the index it might be a default and unset (-1), so just return a default starting value.
		return SlotModule.SlotModuleEventType.OnSlotGameStartedNoCoroutine;
	}

	// Get the value to be serialized for the Enum entry with the passed in name.
	// The serialized value is actually an index into the array of values from code.
	private static int getEnumValueIndexForEnumType(string sortedEnumValueName)
	{
		SlotModule.SlotModuleEventType selectedEnum = SlotModule.SlotModuleEventType.OnSlotGameStartedNoCoroutine;
		
		try
		{
			selectedEnum = (SlotModule.SlotModuleEventType)Enum.Parse(typeof(SlotModule.SlotModuleEventType), sortedEnumValueName);
		}
		catch (ArgumentException e)
		{
			Debug.LogWarning("SortedSlotModuleEventTypeDrawer.getCodeIndexForSlotModuleEventTypeName() - Exception! e.Message = " + e.Message + "; selectedEnum will be left as SlotModule.SlotModuleEventType.OnSlotGameStartedNoCoroutine");
		}
		
		int currentIndex = 0;
		foreach (SlotModule.SlotModuleEventType typeEnum in Enum.GetValues(typeof(SlotModule.SlotModuleEventType)))
		{
			if (typeEnum == selectedEnum)
			{
				return currentIndex;
			}

			currentIndex++;
		}
		
		return 0;
	}

	// Gets the total property height for everything that will be rendered as part of the AnimationListController.AnimationInformation
	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		bool isExpanded = property.isExpanded;
		float standardHeight = base.GetPropertyHeight(property, label);

		if (isExpanded)
		{
			List<CommonPropertyDrawer.CustomPropertyBase> customProperties = getCustomPropertyList(property, label);
			float totalListHeight = CommonPropertyDrawer.getCustomPropertyListHeight(customProperties, standardHeight);

			return standardHeight + totalListHeight;
		}
		else
		{
			return standardHeight;
		}
	}
}
