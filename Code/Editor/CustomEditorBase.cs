using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Base class for custom editors that provides easily serialized properties
// that are properly saved out in prefab mode. Helper functions are available
// to make drawing labels more concise as well as methods for drawing headers
// and help sections.
//
// Author : Nick Saito <nsaito@zynga.com>
// Oct 19, 2020
//
public class CustomEditorBase<T> : Editor where T : class
{
	// the main object that we need to serialize
	protected SerializedObject targetSerializedObject;

	// direct link to the module so we can update lists and objects easily
	protected T targetInstance;

	// Serialized Properties on the SerializedObject
	protected Dictionary<string, SerializedProperty> serializedProperties;

	// you can turn the help on/off completely
	protected bool enableHelp;

	// toggle for the showing the help
	protected bool showHelp;

	public override void OnInspectorGUI()
	{
		// Initialize the custom sounds property so we can show it.
		initSerializedProperties();
		targetSerializedObject.Update();

		drawGUIGuts();

		if (enableHelp)
		{
			drawHelpButton();
		}

		// Save it out
		if (GUI.changed)
		{
			targetSerializedObject.ApplyModifiedProperties();
		}
	}

	protected virtual void drawGUIGuts()
	{
		// override this to draw you GUI or override OnInspectorGUI and handle it yourself.
	}

	protected virtual void initSerializedProperties()
	{
		// Get the instance we are doing all this custom editor for
		targetInstance = target as T;

		if (targetSerializedObject == null)
		{
			targetSerializedObject = new SerializedObject(targetInstance as Object);
			serializedProperties = new Dictionary<string, SerializedProperty>();

			SerializedProperty serializedProperty = serializedObject.GetIterator();
			serializedProperty.Reset();

			do
			{
				if (!serializedProperties.ContainsKey(serializedProperty.name))
				{
					serializedProperties.Add(serializedProperty.name, targetSerializedObject.FindProperty(serializedProperty.name));
				}
			} while (serializedProperty.Next(true));

			serializedProperty.Reset();
		}
	}

	protected virtual void drawHeader(string text)
	{
		EditorGUILayout.LabelField("");
		EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
	}

	protected virtual void drawHelpButton()
	{
		string helpText = showHelp ? "Hide Help" : "Show Help";
		if (GUILayout.Button(helpText))
		{
			showHelp = !showHelp;
		}
	}

	protected virtual void drawHelp(string helpMessage)
	{
		if (showHelp)
		{
			EditorGUILayout.HelpBox(helpMessage, MessageType.Info);
		}
	}

	protected void drawFloat(string key, string label)
	{
		SerializedProperty serializedProperty;
		if (serializedProperties.TryGetValue(key, out serializedProperty))
		{
			serializedProperty.floatValue = EditorGUILayout.FloatField(label, serializedProperty.floatValue);
		}
	}
	
	protected void drawInt(string key, string label)
	{
		SerializedProperty serializedProperty;
		if (serializedProperties.TryGetValue(key, out serializedProperty))
		{
			serializedProperty.intValue = EditorGUILayout.IntField(label, serializedProperty.intValue);
		}
	}

	protected void drawBool(string key, string label)
	{
		SerializedProperty serializedProperty;
		if (serializedProperties.TryGetValue(key, out serializedProperty))
		{
			serializedProperty.boolValue = EditorGUILayout.Toggle(label, serializedProperty.boolValue);
		}
	}
	
	protected void drawString(string key, string label)
	{
		SerializedProperty serializedProperty;
		if (serializedProperties.TryGetValue(key, out serializedProperty))
		{
			serializedProperty.stringValue = EditorGUILayout.TextField(label, serializedProperty.stringValue);
		}
	}
	
	protected void drawArrayElementAtIndex(string key, string label, int index, GUILayoutOption[] option = null)
	{
		SerializedProperty serializedProperty;
		if (serializedProperties.TryGetValue(key, out serializedProperty))
		{
			if (index < serializedProperty.arraySize)
			{
				switch (serializedProperty.arrayElementType)
				{
					case "int":
						serializedProperty.GetArrayElementAtIndex(index).intValue = 
							EditorGUILayout.IntField(label, serializedProperty.GetArrayElementAtIndex(index).intValue, option);
						break;
					case "Vector3Int":
						serializedProperty.GetArrayElementAtIndex(index).vector3IntValue = 
							EditorGUILayout.Vector3IntField(label, serializedProperty.GetArrayElementAtIndex(index).vector3IntValue);
						break;
					default:
						serializedProperty.GetArrayElementAtIndex(index).stringValue = 
							EditorGUILayout.TextField(label, serializedProperty.GetArrayElementAtIndex(index).stringValue);
						break;
				}
			}
		}
	}
	
	protected void drawProperty(string key)
	{
		SerializedProperty serializedProperty;
		if (serializedProperties.TryGetValue(key, out serializedProperty))
		{
			EditorGUILayout.PropertyField(serializedProperty, true);
		}
	}

	protected void drawObject<T>(string key, string label) where T : class
	{
		SerializedProperty serializedProperty;
		if (serializedProperties.TryGetValue(key, out serializedProperty))
		{
			serializedProperty.objectReferenceValue = EditorGUILayout.ObjectField(label, serializedProperty.objectReferenceValue, typeof(T), true);
		}
	}

	protected void drawVector3(string key, string label)
	{
		SerializedProperty serializedProperty;
		if (serializedProperties.TryGetValue(key, out serializedProperty))
		{
			serializedProperty.vector3Value = EditorGUILayout.Vector3Field(label, serializedProperty.vector3Value);
		}
	}
}
