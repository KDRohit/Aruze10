using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class WheelTextPositioner : EditorWindow 
{
	private float radius = 300;
	private Object parentObj;

	[MenuItem ("Window/Wheel Text Positioner")]
	static void Init ()
	{
		EditorWindow.GetWindow(typeof (WheelTextPositioner));
	}

	void OnGUI()
	{
		EditorGUILayout.HelpBox("Set the angle for each UiLabel, select the parent object containing the uilabels, pick the distance from center and hit SetPosition", MessageType.Info);
		radius = EditorGUILayout.FloatField("radius", radius);
		if (GUILayout.Button("SetPosition"))
		{
			updatePositions();
		}
	}

	private void updatePositions()
	{
		UILabel[] labels;
		Vector3 tempPos;

		GameObject parentGameObj = Selection.activeObject as GameObject;
		labels = parentGameObj.GetComponentsInChildren<UILabel>();

		for( int i = 0; i <labels.Length; i++ )
		{
			tempPos = Vector3.zero;
			tempPos.x = -Mathf.Sin(Mathf.Deg2Rad * labels[i].gameObject.transform.localEulerAngles.z) * radius;
			tempPos.y = Mathf.Cos(Mathf.Deg2Rad * labels[i].gameObject.transform.localEulerAngles.z) * radius;
			
			labels[i].gameObject.transform.localPosition = tempPos;
		}
	}
}
