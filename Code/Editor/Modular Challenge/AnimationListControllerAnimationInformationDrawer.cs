using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;

/*
Custom handling for AnimationListController.AnimationInformation that turns the animation name into a drop down menu
and only reveals additional options once an animator is set
*/
[CustomPropertyDrawer (typeof (AnimationListController.AnimationInformation))]
public class AnimationListControllerAnimationInformationDrawer : PropertyDrawer 
{
	private const string ANIMATION_NAME_PROPERTY = "Animation State Name";
	private const string PROP_NAME_WHEN_IN_ARRAY = "data";

	private Dictionary<string, float> clipNameMap = new Dictionary<string, float>();
	
	// Renders the custom stuff to the gui
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		// BeginProperty will forward the tooltip info to this PropertyDrawer
		label = EditorGUI.BeginProperty(position, label, property);

		bool isInArray = CommonReflection.isTypeArrayOrList(fieldInfo.FieldType);
		int selectedAnimationIndex = -1;
		List<string> stateNames = new List<string>(); // Keep track of all the possible states that we can pick from.
		EditorGUIUtility.labelWidth = 0;
		EditorGUIUtility.fieldWidth = 0;

		SerializedProperty serializedTargetAnimator = property.FindPropertyRelative("targetAnimator");
		Animator targetAnimator = serializedTargetAnimator.objectReferenceValue as System.Object as Animator;

		bool isExpanded = true;
		isExpanded = property.isExpanded;
		
		SerializedProperty serializedAnimationName = property.FindPropertyRelative("ANIMATION_NAME");
		string animationName = serializedAnimationName.stringValue;

		if (targetAnimator != null && targetAnimator.runtimeAnimatorController != null)
		{
			SerializedProperty serializedStateLayer = property.FindPropertyRelative("stateLayer");
			int layer = serializedStateLayer.intValue;
			AnimatorController targetAnimatorController = targetAnimator.runtimeAnimatorController as AnimatorController;
			AnimatorStateMachine sm = null;
			if (layer >= 0 && layer < targetAnimatorController.layers.Length)
			{ 
				sm = targetAnimatorController.layers[layer].stateMachine;
			}
			else
			{
				serializedStateLayer.intValue = 0;
				sm = targetAnimatorController.layers[0].stateMachine;
			}

			ChildAnimatorState[] childAnimatorStates = sm.states;

			for (int i = 0; i < childAnimatorStates.Length; i++)
			{
				ChildAnimatorState childAnimatorState = childAnimatorStates[i];
				string childAnimatorStateName = childAnimatorState.state.ToString();
				// Get rid of the extra information that Unity adds
				childAnimatorStateName = childAnimatorStateName.Replace(" (UnityEngine.AnimatorState)", ""); // need to remove the extra text at the end of this.

				AnimationClip clip = childAnimatorState.state.motion as AnimationClip;
				if (clip != null)
				{
					clipNameMap[childAnimatorStateName] = clip.length;
				}
				stateNames.Add(childAnimatorStateName);
			}
			
			if (animationName != null)
			{
				// If we have an animation name saved out lets use that here.
				selectedAnimationIndex = stateNames.FindIndex(x => x.Equals(animationName));
			}
		}

		string displayName = "";
		if (isInArray && targetAnimator != null)
		{
			displayName = targetAnimator.name;
		}
		else
		{
			displayName = property.displayName;
		}
		
		if (clipNameMap.ContainsKey(animationName))
		{
			displayName += string.Format(" ({0}; length: {1})", animationName, clipNameMap[animationName]);
		}
		else if (targetAnimator != null)
		{
			displayName += " (" + animationName + ")";
		}

		List<CommonPropertyDrawer.CustomPropertyBase> customProperties = getCustomPropertyList(property, label, selectedAnimationIndex, stateNames);

		float standardHeight = base.GetPropertyHeight(property, label);

		Rect posRect = new Rect(position.x, position.y, position.width, standardHeight);

		if (!isExpanded)
		{
			posRect = new Rect(position.x, position.y, position.width, position.height);
		}

		CommonPropertyDrawer.CustomFoldout mainDropdown = new CommonPropertyDrawer.CustomFoldout(property, displayName, isExpanded, standardHeight, label.tooltip);
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

	// Triggered when the animation dropdown is changed to update the serialized value
	public void onAnimationStateNameChanged(CommonPropertyDrawer.CustomPopup popup, int selectedIndex)
	{
		SerializedProperty property = popup.parentProperty;

		List<string> stateNames = new List<string>(); // Keep track of all the possible states that we can pick from.
		int selectedAnimationIndex = -1;

		SerializedProperty serializedTargetAnimator = property.FindPropertyRelative("targetAnimator");
		Animator targetAnimator = serializedTargetAnimator.objectReferenceValue as System.Object as Animator;

		SerializedProperty serializedAnimationName = property.FindPropertyRelative("ANIMATION_NAME");
		string animationName = serializedAnimationName.stringValue;

		if (targetAnimator != null && targetAnimator.runtimeAnimatorController != null)
		{
			SerializedProperty serializedStateLayer = property.FindPropertyRelative("stateLayer");
			int layer = serializedStateLayer.intValue;
			AnimatorController targetAnimatorController = targetAnimator.runtimeAnimatorController as AnimatorController;
			AnimatorStateMachine sm = null;
			if (layer >= 0 && layer < targetAnimatorController.layers.Length)
			{ 
				sm = targetAnimatorController.layers[layer].stateMachine;
			}
			else
			{
				serializedStateLayer.intValue = 0;
				sm = targetAnimatorController.layers[0].stateMachine;
			}
			ChildAnimatorState[] childAnimatorStates = sm.states;

			for (int i = 0; i < childAnimatorStates.Length; i++)
			{
				ChildAnimatorState childAnimatorState = childAnimatorStates[i];
				string childAnimatorStateName = childAnimatorState.state.ToString();
				// Get rid of the extra information that Unity adds
				childAnimatorStateName = childAnimatorStateName.Replace(" (UnityEngine.AnimatorState)", ""); // need to remove the extra text at the end of this.

				AnimationClip clip = childAnimatorState.state.motion as AnimationClip;
				if (clip != null)
				{
					clipNameMap[childAnimatorStateName] = clip.length;
				}
				stateNames.Add(childAnimatorStateName);
			}
			
			if (animationName != null)
			{
				// If we have an animation name saved out lets use that here.
				selectedAnimationIndex = stateNames.FindIndex(x => x.Equals(animationName));
			}
		}

		if (selectedIndex != selectedAnimationIndex)
		{
			if (selectedIndex < stateNames.Count)
			{
				serializedAnimationName.stringValue = stateNames[selectedIndex];
			}
			else
			{
				// invalid index, probably means the user is removing an animator so just make the value an empty string
				serializedAnimationName.stringValue = "";
			}
		}
	}

	// Get the list of properties that will be displayed, these are used to determine the total size of all the properties
	// as well as to actually render them
	private List<CommonPropertyDrawer.CustomPropertyBase> getCustomPropertyList(SerializedProperty property, GUIContent label, int selectedAnimaitonIndex, List<string> stateNames)
	{
		float standardHeight = base.GetPropertyHeight(property, label);
		List<CommonPropertyDrawer.CustomPropertyBase> customProperties = new List<CommonPropertyDrawer.CustomPropertyBase>();

		SerializedProperty serializedTargetAnimator = property.FindPropertyRelative("targetAnimator");
		Animator targetAnimator = serializedTargetAnimator.objectReferenceValue as System.Object as Animator;

		customProperties.Add(new CommonPropertyDrawer.CustomSerializedProperty(property, serializedTargetAnimator, standardHeight));

		if (targetAnimator != null)
		{
			customProperties.Add(new CommonPropertyDrawer.CustomPopup(property, ANIMATION_NAME_PROPERTY, selectedAnimaitonIndex, stateNames.ToArray(), standardHeight, "", onAnimationStateNameChanged));

			// Get the type so we can extract the serialized fields from it
			System.Type targetObjectClassType = CommonReflection.getIndividualElementType(fieldInfo.FieldType);
			List<string> propertiesToIncludeList = CommonReflection.getNamesOfAllSerializedFieldsForType(targetObjectClassType);

			// ignore properties we're handling in specific ways
			propertiesToIncludeList.Remove("targetAnimator");
			propertiesToIncludeList.Remove("ANIMATION_NAME");

			// show cross fade info if it is checked
			SerializedProperty serializedIsCrossFading = property.FindPropertyRelative("isCrossFading");
			bool isCrossFading = serializedIsCrossFading.boolValue;
			if (!isCrossFading)
			{
				// If we aren't crossfading then hide these options
				propertiesToIncludeList.Remove("fixedCrossFadeTransitionDuration");
				propertiesToIncludeList.Remove("normalizedCrossFadeTransitionTime");
			}

			// add the remaining properties skipping the ones we are handling seperately
			CommonPropertyDrawer.addPropertiesToCustomList(property, ref customProperties, standardHeight, propertyNameList: propertiesToIncludeList, includeChildren: false);
		}

		return customProperties;
	}

	// Gets the total property height for everything that will be rendered as part of the AnimationListController.AnimationInformation
	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		bool isExpanded = property.isExpanded;

		float standardHeight = base.GetPropertyHeight(property, label);

		if (isExpanded)
		{
			// get the property list but don't worry about the animation names as they aren't needed to determine the height
			List<CommonPropertyDrawer.CustomPropertyBase> customProperties = getCustomPropertyList(property, label, -1, new List<string>());
			float totalListHeight = CommonPropertyDrawer.getCustomPropertyListHeight(customProperties, standardHeight);

			return standardHeight + totalListHeight;
		}
		else
		{
			return standardHeight;
		}
	}
}
