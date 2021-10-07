using UnityEngine;
using UnityEditor;
using NUnit.Framework;

//This class will simplify the use of a list of SallyAnimationInfo 
[CustomPropertyDrawer(typeof(SallyAnimationInfo))]
public class SallyAnimationInfoDrawer : PropertyDrawer
{
	// Draw the property inside the given rect
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		// Using BeginProperty / EndProperty on the parent property means that		
		EditorGUI.BeginProperty(position, label, property);

		// Draw label
		position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
		
		var indent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;

		var symbolNameRect = new Rect(position.x, position.y, 150, position.height);
		var animationNameRect = new Rect(position.x + 150, position.y, 150, position.height);		
		
		// Draw fields - passs GUIContent.none to each so they are drawn without labels
		EditorGUI.PropertyField(symbolNameRect, property.FindPropertyRelative("symbolName"), GUIContent.none);
		EditorGUI.PropertyField(animationNameRect, property.FindPropertyRelative("animationName"), GUIContent.none);		

		// Set indent back to what it was
		EditorGUI.indentLevel = indent;

		EditorGUI.EndProperty();
	}
}
