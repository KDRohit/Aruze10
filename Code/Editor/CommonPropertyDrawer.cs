using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

/*
Common funcitonality to help with building PropertyDrawer's (which are kind of a pain in the butt, but these will make it easier)
See: AnimationListControllerDrawer.cs for an example
*/
public static class CommonPropertyDrawer
{
	// Add together the property height of each element to get the total height
	public static float getCustomPropertyListHeight(List<CustomPropertyBase> customProperties, float standardHeight)
	{
		float totalHeight = 0;
		for (int i = 0; i < customProperties.Count; i++)
		{
			float currentPropertyHeight = customProperties[i].getPropertyHeight();

			if (currentPropertyHeight == 0)
			{
				// if this element doesn't have a defined height just give it a standard one
				totalHeight += standardHeight;
			}
			else
			{
				totalHeight += currentPropertyHeight;
			}
		}

		return totalHeight;
	}

	// Adds all properties from the passed propertyNameList to the customProperties list to be displayed like normal
	public static List<CustomPropertyBase> addPropertiesToCustomList(SerializedProperty originalProperty, ref List<CustomPropertyBase> customProperties, float standardHeight, List<string> propertyNameList = null, bool includeChildren = false)
	{
		if (customProperties == null)
		{
			Debug.LogError("CommonPropertyDrawer.addPropertiesToCustomList() - customProperties was null!");
			return null;
		}
		else if (propertyNameList == null)
		{
			Debug.LogError("CommonPropertyDrawer.addPropertiesToCustomList() - propertyNameList is null!");
		}

		for (int i = 0; i < propertyNameList.Count; i++)
		{
			SerializedProperty serializedProperty = originalProperty.FindPropertyRelative(propertyNameList[i]);
			if (serializedProperty != null)
			{
				customProperties.Add(new CommonPropertyDrawer.CustomSerializedProperty(originalProperty, serializedProperty, standardHeight));
			}
			else
			{
				Debug.LogError("CommonPropertyDrawer.addPropertiesToCustomList() - Couldn't find property: " + propertyNameList[i]);
			}
		}

		return customProperties;
	}

	// Renders everything from a list of CustomPropertyBase
	public static void drawCustomPropertyList(List<CustomPropertyBase> customProperties, ref Rect posRect)
	{
		for (int i = 0; i < customProperties.Count; i++)
		{
			customProperties[i].drawProperty(ref posRect);
		}
	}

	// Base class for a set of custom properties which will be used as a list to generate what is needed for a PropertyDrawer
	public abstract class CustomPropertyBase
	{
		public enum PropertyEnum 
		{
			NONE = -1, 
			SERIALIZED_PROPERTY = 0, // done
			POPUP = 1, // done
			FOLDOUT = 2, // done
			LABEL = 3, //done
		};

		protected SerializedProperty _parentProperty;
		public SerializedProperty parentProperty
		{
			get { return _parentProperty; }
		}

		protected PropertyEnum _type = PropertyEnum.NONE;
		public PropertyEnum type
		{
			get { return _type; }
		}
		
		protected float standardHeight = 0; // If this isn't an element who's height can be calculated, use this standardHeight from the original Property being rendered
		protected string tooltip = ""; // Used to display a tooltip when displaying this element, used to correctly forward built in Unity tooltips to Custom PropertyDrawers

		public CustomPropertyBase(SerializedProperty parentProperty, float standardHeight, string tooltip)
		{
			this._parentProperty = parentProperty;
			this.standardHeight = standardHeight;
			this.tooltip = tooltip;
		}

		public abstract void drawProperty(ref Rect r);

		public virtual float getPropertyHeight()
		{
			return standardHeight;
		}
	}

	// Custom Property class for a label
	public class CustomLabel : CustomPropertyBase
	{
		private string displayName;

		public CustomLabel(SerializedProperty parentProperty, string displayName, float standardHeight, string tooltip)
			: base(parentProperty, standardHeight, tooltip)
		{
			_type = PropertyEnum.LABEL;
			this.displayName = displayName;
			this.standardHeight = standardHeight;
		}

		public override void drawProperty(ref Rect r)
		{
			EditorGUI.LabelField(r, new GUIContent(displayName, tooltip));
			r.y += getPropertyHeight();
		}
	}

	// Custom Property calss for a toggle (i.e. checkbox)
	public class CustomToggle : CustomPropertyBase
	{
		private string displayName;
		private bool isChecked;

		public CustomToggle(SerializedProperty parentProperty, string displayName, bool isChecked, float standardHeight, string tooltip)
			: base(parentProperty, standardHeight, tooltip)
		{
			_type = PropertyEnum.LABEL;
			this.displayName = displayName;
			this.isChecked = isChecked;
			this.standardHeight = standardHeight;
		}

		public override void drawProperty(ref Rect r)
		{
			EditorGUI.Toggle(r, new GUIContent(displayName, tooltip), isChecked);
			r.y += getPropertyHeight();
		}
	}

	// Custom Property class for a serialized value that will be drawn as usual
	public class CustomSerializedProperty : CustomPropertyBase
	{
		private SerializedProperty property;

		public CustomSerializedProperty(SerializedProperty parentProperty, SerializedProperty property, float standardHeight)
			: base(parentProperty, standardHeight, "")
		{
			_type = PropertyEnum.SERIALIZED_PROPERTY;
			this.property = property;
			this.standardHeight = standardHeight;
		}

		public override float getPropertyHeight()
		{
			if (property != null)
			{
				return EditorGUI.GetPropertyHeight(property);
			}
			else
			{
				return standardHeight;
			}
		}

		public override void drawProperty(ref Rect r)
		{
			EditorGUI.BeginChangeCheck();
			EditorGUI.PropertyField(r, property, new GUIContent(property.displayName), true);
			EditorGUI.EndChangeCheck();

			r.y += getPropertyHeight();
		}
	}

	// Custom Property class for a section control that expands and collapses
	public class CustomFoldout : CustomPropertyBase
	{
		public bool isExpanded;
		private string displayName;

		public CustomFoldout(SerializedProperty parentProperty, string displayName, bool isExpanded, float standardHeight, string tooltip)
			: base(parentProperty, standardHeight, tooltip)
		{
			_type = PropertyEnum.FOLDOUT;
			this.displayName = displayName;
			this.isExpanded = isExpanded;
			this.standardHeight = standardHeight;
		}

		public override void drawProperty(ref Rect r)
		{
			EditorGUI.BeginChangeCheck();
			isExpanded = EditorGUI.Foldout(r, isExpanded, new GUIContent(displayName, tooltip), true);
			EditorGUI.EndChangeCheck();

			r.y += getPropertyHeight();
			EditorGUI.indentLevel += 1;
		}
	}

	// Custom Property class for a drop down like menu where you select an object from a list
	public class CustomPopup : CustomPropertyBase
	{
		public delegate void OnCustomPopupValueChangeDelegate(CustomPopup popup, int selectedIndex);

		private string displayName;
		private int selectedIndex;
		private string[] displayedOptions;
		private OnCustomPopupValueChangeDelegate onValueChangeCallback = null;

		public CustomPopup(SerializedProperty parentProperty, string displayName, int selectedIndex, string[] displayedOptions, float standardHeight, string tooltip, OnCustomPopupValueChangeDelegate onValueChangeCallback)
			: base(parentProperty, standardHeight, tooltip)
		{
			_type = PropertyEnum.POPUP;
			this.displayName = displayName;
			this.selectedIndex = selectedIndex;
			this.displayedOptions = displayedOptions;
			this.standardHeight = standardHeight;
			this.onValueChangeCallback = onValueChangeCallback;
		}

		public override void drawProperty(ref Rect r)
		{
			GUIContent[] displayedOptionsAsGuiContent = new GUIContent[displayedOptions.Length];
			for (int i = 0; i < displayedOptionsAsGuiContent.Length; i++)
			{
				displayedOptionsAsGuiContent[i] = new GUIContent(displayedOptions[i]);
			}
			
			EditorGUI.BeginChangeCheck();
			selectedIndex = EditorGUI.Popup(r, new GUIContent(displayName, tooltip), selectedIndex, displayedOptionsAsGuiContent);
			EditorGUI.EndChangeCheck();

			r.y += getPropertyHeight();

			if (onValueChangeCallback != null)
			{
				onValueChangeCallback(this, selectedIndex);
			}
		}
	}
}
