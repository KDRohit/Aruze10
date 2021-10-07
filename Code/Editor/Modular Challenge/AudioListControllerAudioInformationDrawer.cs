﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/**
Class to handle custom property handling for the AudioInformation used by AudioListController, 
right now this is just used to auto trim the SOUND_NAME to catch and prevent user input errors.

Original Author: Scott Lepthien
Creation Date: May 23, 2017
*/
[CustomPropertyDrawer (typeof (AudioListController.AudioInformation))]
public class AudioListAudioInformationControllerDrawer : PropertyDrawer 
{
	// Renders the custom stuff to the gui
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		trimSoundName(property);
		
		// BeginProperty will forward the tooltip info to this PropertyDrawer
		label = EditorGUI.BeginProperty(position, label, property);
		
		EditorGUIUtility.labelWidth = 0;
		EditorGUIUtility.fieldWidth = 0;
		
		SerializedProperty serializedIsDoingSwitchMusicKey = property.FindPropertyRelative("isDoingSwitchMusicKey");
		bool isDoingSwitchMusicKey = serializedIsDoingSwitchMusicKey.boolValue;

		bool isExpanded = property.isExpanded;

		List<CommonPropertyDrawer.CustomPropertyBase> customProperties = getCustomPropertyList(property, label, isDoingSwitchMusicKey);

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

	private void trimSoundName(SerializedProperty property)
	{
		SerializedProperty serializedSoundName = property.FindPropertyRelative("SOUND_NAME");
		if (serializedSoundName != null)
		{
			string soundName = serializedSoundName.stringValue;
			string trimmedSoundName = soundName.Trim();
			if (soundName != trimmedSoundName)
			{
				Debug.LogWarning("AudioListAudioInformationControllerDrawer.OnGUI() - trimmed soundName = \"" + soundName + "\" to trimmedSoundName = \"" + trimmedSoundName + "\"");
				serializedSoundName.stringValue = trimmedSoundName;
			}
		}
	}
	
	// Get the list of properties that will be displayed, these are used to determine the total size of all the properties
	// as well as to actually render them
	private List<CommonPropertyDrawer.CustomPropertyBase> getCustomPropertyList(SerializedProperty property, GUIContent label, bool isDoingSwitchMusicKey)
	{
		float standardHeight = base.GetPropertyHeight(property, label);
		List<CommonPropertyDrawer.CustomPropertyBase> customProperties = new List<CommonPropertyDrawer.CustomPropertyBase>();
		
		// Get the type so we can extract the serialized fields from it
		System.Type targetObjectClassType = CommonReflection.getIndividualElementType(fieldInfo.FieldType);
		List<string> propertiesToIncludeList = CommonReflection.getNamesOfAllSerializedFieldsForType(targetObjectClassType);

		if (isDoingSwitchMusicKey)
		{
			// Play music, so need to hide some things for playing sounds
			
			// Switching the music key doesn't have the concept of blocking so hide and disable it
			propertiesToIncludeList.Remove("isBlockingModule");
			SerializedProperty isBlockingModuleProperty = property.FindPropertyRelative("isBlockingModule");
			isBlockingModuleProperty.boolValue = false;
		}
		else
		{
			// Play sound, so need to hide stuff for switching the looped music key
			
			propertiesToIncludeList.Remove("musicFadeTime");
			propertiesToIncludeList.Remove("isNotPlayingMusicImmediate");
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
			SerializedProperty serializedIsDoingSwitchMusicKey = property.FindPropertyRelative("isDoingSwitchMusicKey");
			bool isDoingSwitchMusicKey = serializedIsDoingSwitchMusicKey.boolValue;

			// get the property list so we can determine the height
			List<CommonPropertyDrawer.CustomPropertyBase> customProperties = getCustomPropertyList(property, label, isDoingSwitchMusicKey);
			float totalListHeight = CommonPropertyDrawer.getCustomPropertyListHeight(customProperties, standardHeight);

			return standardHeight + totalListHeight;
		}
		else
		{
			return standardHeight;
		}
	}
}
