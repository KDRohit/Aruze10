using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/*
Basic editor to control what is drawn by the OversizedSymbolDisplayModule so
that extra options are hidden unless they are enabled.

Original Author: Scott Lepthien
Creation Date: 10/25/2018
*/
[CustomEditor(typeof(OversizedSymbolDisplayModule))]
public class OversizedSymbolDisplayModuleEditor : Editor 
{
	private SerializedProperty isHidingAndShowingBufferSymbolsProp;
	private SerializedProperty isRelayeringSymbolsProp;
	private SerializedProperty setToReelLayerOnSpinProp;
	private SerializedProperty overlayLayerProp;

	public override void OnInspectorGUI()
	{
		// Initialize the custom sounds property so we can show it.
		initSerializedProperties();
		serializedObject.Update();
		
		EditorGUILayout.PropertyField(isHidingAndShowingBufferSymbolsProp, true);
		EditorGUILayout.PropertyField(isRelayeringSymbolsProp, true);
		if (isRelayeringSymbolsProp.boolValue)
		{
			EditorGUILayout.PropertyField(setToReelLayerOnSpinProp, true);
			EditorGUILayout.PropertyField(overlayLayerProp, true);
		}
		
		// Save it out
		serializedObject.ApplyModifiedProperties();
	}
	
	private void initSerializedProperties()
	{
		isHidingAndShowingBufferSymbolsProp = serializedObject.FindProperty("isHidingAndShowingBufferSymbols");
		isRelayeringSymbolsProp = serializedObject.FindProperty("isRelayeringSymbols");
		setToReelLayerOnSpinProp = serializedObject.FindProperty("setToReelLayerOnSpin");
		overlayLayerProp = serializedObject.FindProperty("overlayLayer");
	}
}
