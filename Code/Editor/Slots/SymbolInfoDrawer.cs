using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;

[CustomPropertyDrawer (typeof (SymbolInfo))]
public class SymbolInfoDrawer : PropertyDrawer
{
	// Renders the custom stuff to the gui
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		// BeginProperty will forward the tooltip info to this PropertyDrawer
		label = EditorGUI.BeginProperty(position, label, property);

		EditorGUIUtility.labelWidth = 0;
		EditorGUIUtility.fieldWidth = 0;

		SerializedProperty serializedIsUsingNameArray = property.FindPropertyRelative("isUsingNameArray");
		bool isUsingNameArray = serializedIsUsingNameArray.boolValue;

		bool isExpanded = true;
		isExpanded = property.isExpanded;

		List<CommonPropertyDrawer.CustomPropertyBase> customProperties = getCustomPropertyList(property, label, isUsingNameArray);

		float standardHeight = base.GetPropertyHeight(property, label);

		Rect posRect = new Rect(position.x, position.y, position.width, standardHeight);

		if (!isExpanded)
		{
			posRect = new Rect(position.x, position.y, position.width, position.height);
		}
		
		string foldoutName = getFoldoutName(property, isUsingNameArray);

		CommonPropertyDrawer.CustomFoldout mainDropdown = new CommonPropertyDrawer.CustomFoldout(property, foldoutName, isExpanded, standardHeight, label.tooltip);
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
	private List<CommonPropertyDrawer.CustomPropertyBase> getCustomPropertyList(SerializedProperty property, GUIContent label, bool isUsingNameArray)
	{
		float standardHeight = base.GetPropertyHeight(property, label);
		List<CommonPropertyDrawer.CustomPropertyBase> customProperties = new List<CommonPropertyDrawer.CustomPropertyBase>();

		List<string> propertiesToIncludeList;
		
		// Show everything
			
		// Get the type so we can extract the serialized fields from it
		System.Type targetObjectClassType = CommonReflection.getIndividualElementType(fieldInfo.FieldType);
		propertiesToIncludeList = CommonReflection.getNamesOfAllSerializedFieldsForType(targetObjectClassType);

		if (Application.isPlaying)
		{
			// Don't show this option when the game is running.  Changing it would
			// have no effect, since everything it is used for will already be built.
			propertiesToIncludeList.Remove("isUsingNameArray");
		}

		if (isUsingNameArray)
		{
			propertiesToIncludeList.Remove("name");
			if (!Application.isPlaying)
			{
				SerializedProperty serializedName = property.FindPropertyRelative("name");
				serializedName.stringValue = "";
			}
		}
		else
		{
			propertiesToIncludeList.Remove("nameArray");
			if (!Application.isPlaying)
			{
				SerializedProperty serializedNameArray = property.FindPropertyRelative("nameArray");
				serializedNameArray.arraySize = 0;
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
			SerializedProperty serializedIsUsingNameArray = property.FindPropertyRelative("isUsingNameArray");
			bool isUsingNameArray = serializedIsUsingNameArray.boolValue;

			// get the property list so we can determine the height
			List<CommonPropertyDrawer.CustomPropertyBase> customProperties = getCustomPropertyList(property, label, isUsingNameArray);
			float totalListHeight = CommonPropertyDrawer.getCustomPropertyListHeight(customProperties, standardHeight);

			return standardHeight + totalListHeight;
		}
		else
		{
			return standardHeight;
		}
	}

	// Determines what name to used based on which type of SymbolInfo name is being used.
	private string getFoldoutName(SerializedProperty property, bool isUsingNameArray)
	{
		if (isUsingNameArray)
		{
			SerializedProperty serializedNameArray = property.FindPropertyRelative("nameArray");
			if (serializedNameArray.arraySize > 0)
			{
				StringBuilder nameArrayStringBuilder = new StringBuilder("[");
				for (int i = 0; i < serializedNameArray.arraySize; i++)
				{
					if (i > 0)
					{
						nameArrayStringBuilder.Append(", ");
					}
					
					nameArrayStringBuilder.Append(serializedNameArray.GetArrayElementAtIndex(i).stringValue);
				}
				nameArrayStringBuilder.Append("]");
				return nameArrayStringBuilder.ToString();
			}
			else
			{
				return "";
			}
		}
		else
		{
			SerializedProperty serializedName = property.FindPropertyRelative("name");
			return serializedName.stringValue;
		}
	}
}
