using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/**
Class to handle custom display for the features that are part of UnlockingFreespinFeaturesFreespinModule first used by
elvis04.  This will allow only certain things to be shown depending on what type of reward type is selected (to reduce
showing stuff that isn't important for the current type).

Original Author: Scott Lepthien
Creation Date: 9/11/2019
*/
[CustomPropertyDrawer (typeof (UnlockingFreespinFeaturesFreespinModule.UnlockingFreespinFeatureData))]
public class UnlockingFreespinFeatureDataDrawer : PropertyDrawer 
{
	// Renders the custom stuff to the gui
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		// BeginProperty will forward the tooltip info to this PropertyDrawer
		label = EditorGUI.BeginProperty(position, label, property);
		
		EditorGUIUtility.labelWidth = 0;
		EditorGUIUtility.fieldWidth = 0;

		SerializedProperty serializedFeatureType = property.FindPropertyRelative("featureType");
		UnlockingFreespinFeaturesFreespinModule.UnlockingFreespinFeatureData.FeatureTypeEnum featureType = (UnlockingFreespinFeaturesFreespinModule.UnlockingFreespinFeatureData.FeatureTypeEnum)serializedFeatureType.enumValueIndex;

		bool isExpanded = true;
		isExpanded = property.isExpanded;

		List<CommonPropertyDrawer.CustomPropertyBase> customProperties = getCustomPropertyList(property, label, featureType);

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
	private List<CommonPropertyDrawer.CustomPropertyBase> getCustomPropertyList(SerializedProperty property, GUIContent label, UnlockingFreespinFeaturesFreespinModule.UnlockingFreespinFeatureData.FeatureTypeEnum featureType)
	{
		float standardHeight = base.GetPropertyHeight(property, label);
		List<CommonPropertyDrawer.CustomPropertyBase> customProperties = new List<CommonPropertyDrawer.CustomPropertyBase>();

		// Get the type so we can extract the serialized fields from it
		System.Type targetObjectClassType = CommonReflection.getIndividualElementType(fieldInfo.FieldType);
		List<string> propertiesToIncludeList = CommonReflection.getNamesOfAllSerializedFieldsForType(targetObjectClassType);

		if (featureType != UnlockingFreespinFeaturesFreespinModule.UnlockingFreespinFeatureData.FeatureTypeEnum.ExtraFreespins)
		{
			// Hide the properties for adding extra spins
			propertiesToIncludeList.Remove("incrementFreespinsDelay");
			propertiesToIncludeList.Remove("numberOfFreeSpinsAwarded");

			// clear out the properties related to ExtraFreespins so they aren't saved with non-default values that can't be seen
			SerializedProperty serializedIncrementFreespinsDelay = property.FindPropertyRelative("incrementFreespinsDelay");
			serializedIncrementFreespinsDelay.floatValue = 0.0f;
			SerializedProperty serializedNumberOfFreeSpinsAwarded = property.FindPropertyRelative("numberOfFreeSpinsAwarded");
			serializedNumberOfFreeSpinsAwarded.intValue = 0;
		}

		// Handle data that is shown if this is one of the extra wilds types
		if (featureType != UnlockingFreespinFeaturesFreespinModule.UnlockingFreespinFeatureData.FeatureTypeEnum.ExtraWilds && featureType != UnlockingFreespinFeaturesFreespinModule.UnlockingFreespinFeatureData.FeatureTypeEnum.Added2XWilds)
		{
			propertiesToIncludeList.Remove("reelStripReplacements");
			propertiesToIncludeList.Remove("restoreOriginalReelStripsDelay");

			// clear out the properties related to extra wilds so they aren't saved with non-default values that can't be seen
			SerializedProperty serializedReelStripReplacements = property.FindPropertyRelative("reelStripReplacements");
			serializedReelStripReplacements.arraySize = 0;
			SerializedProperty serializedRestoreOriginalReelStripsDelay = property.FindPropertyRelative("restoreOriginalReelStripsDelay");
			serializedRestoreOriginalReelStripsDelay.floatValue = 0.0f;
		}

		// Handle data that only shows for the 2X Wilds
		if (featureType != UnlockingFreespinFeaturesFreespinModule.UnlockingFreespinFeatureData.FeatureTypeEnum.Added2XWilds)
		{
			propertiesToIncludeList.Remove("standardWildSymbolName");
			propertiesToIncludeList.Remove("twoTimesMultiplierWildSymbolName");

			// Reset properties to default values so they are what you would expect if you change the type away and then back
			SerializedProperty serializedStandardWildSymbolName = property.FindPropertyRelative("standardWildSymbolName");
			serializedStandardWildSymbolName.stringValue = "WD";
			SerializedProperty serializedTwoTimesMultiplierWildSymbolName = property.FindPropertyRelative("twoTimesMultiplierWildSymbolName");
			serializedTwoTimesMultiplierWildSymbolName.stringValue = "W2";
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
			SerializedProperty serializedFeatureType = property.FindPropertyRelative("featureType");
			UnlockingFreespinFeaturesFreespinModule.UnlockingFreespinFeatureData.FeatureTypeEnum featureType = (UnlockingFreespinFeaturesFreespinModule.UnlockingFreespinFeatureData.FeatureTypeEnum)serializedFeatureType.enumValueIndex;

			// get the property list so we can determine the height
			List<CommonPropertyDrawer.CustomPropertyBase> customProperties = getCustomPropertyList(property, label, featureType);
			float totalListHeight = CommonPropertyDrawer.getCustomPropertyListHeight(customProperties, standardHeight);

			return standardHeight + totalListHeight;
		}
		else
		{
			return standardHeight;
		}
	}
}
