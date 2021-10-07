using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(S3Validator))]
public class S3ValidatorEditor : Editor 
{
	public override void OnInspectorGUI()
	{
		// Draw Action Buttons
		S3ValidatorControlPanel.drawControlPanel();

		// Draw the rest as normal
		DrawDefaultInspector();
	}
}