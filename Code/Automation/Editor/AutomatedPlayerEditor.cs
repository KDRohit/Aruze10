using UnityEngine;
using UnityEditor;
using System.Collections;

#if ZYNGA_TRAMP
[CustomEditor(typeof(AutomatedPlayer))]
public class AutomatedPlayerEditor : Editor 
{
	public override void OnInspectorGUI()
	{
		AutomatedPlayer ap = (AutomatedPlayer)target;

		// Draw Action Buttons
		AutomatedPlayerControlPanel.DrawControlPanel(ap);

		// Draw the rest as normal
		DrawDefaultInspector();
	}
}
#endif