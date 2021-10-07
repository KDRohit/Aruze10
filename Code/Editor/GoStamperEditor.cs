using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(GoStamper), true), CanEditMultipleObjects]
public class GoStamperEditor : Editor
{
	public override void OnInspectorGUI()
	{
		GUILayout.BeginHorizontal();
		
		if (GUILayout.Button("Delete Objects"))
		{
			GoStamper goStamper = target as GoStamper;
			goStamper.deleteAllObjects();
		}
		
		if (GUILayout.Button("Stamp Objects"))
		{	
			GoStamper goStamper = target as GoStamper;
			goStamper.stampAllObjects();
		}
		
		if (GUILayout.Button("Assign Prefab"))
		{	
			GoStamper goStamper = target as GoStamper;
			goStamper.assignPrefab();
		}
		
		GUILayout.EndHorizontal();
		DrawDefaultInspector();
	}
}
