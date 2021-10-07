using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(GoStamp), true), CanEditMultipleObjects]
public class GoStampEditor : Editor
{
	public override void OnInspectorGUI()
	{		
		GUILayout.BeginHorizontal();
	
		if (GUILayout.Button("Delete Object"))
		{
			GoStamp goStamp = target as GoStamp;
			goStamp.deleteObject();
		}
		
		if (GUILayout.Button("Stamp Object"))
		{	
			GoStamp goStamp = target as GoStamp;
			goStamp.stampObject();
		}
		
		if (GUILayout.Button("Apply Changes"))
		{	
			GoStamp goStamp = target as GoStamp;
			goStamp.applyChanges();
		}
		
		GUILayout.EndHorizontal();
		DrawDefaultInspector();
	}
}
