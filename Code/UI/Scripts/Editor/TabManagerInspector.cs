using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
#pragma warning disable 0618 // Not using UnityEngine.Object, System.Type, params UnityEngine.GUILayoutOption[]) with the new allowSceneObjects parameter.

[CustomEditor(typeof(TabManager))]
public class TabManagerInspector : Editor
{
	TabManager manager;

	int numTabs = 1;
	int currentlySelected = -1;
	
	public override void OnInspectorGUI()
	{
		
		manager = target as TabManager;

		numTabs = (manager.tabs != null) ? manager.tabs.Length : 0; 

		manager.uiGrid = EditorGUILayout.ObjectField("Grid", manager.uiGrid, typeof(UICenteredGrid)) as UICenteredGrid;
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("-", GUILayout.Width(40f)))
		{
			numTabs--;
			if (numTabs <= 0)
			{
				// Minimum of 1 or this class does nothing.
				numTabs = 1;
			}
			System.Array.Resize<TabSelector>(ref manager.tabs, numTabs);
		}
		GUILayout.Label(numTabs.ToString(), GUILayout.Width(40f));
		if (GUILayout.Button("+", GUILayout.Width(40f)))
		{
			numTabs++;
			System.Array.Resize<TabSelector>(ref manager.tabs, numTabs);
		}		
		GUILayout.EndHorizontal();

		for (int i = 0; i < numTabs; i++)
		{
			GUILayout.BeginHorizontal();
			manager.tabs[i] = EditorGUILayout.ObjectField(string.Format("Tab {0}", i), manager.tabs[i], typeof(TabSelector), true) as TabSelector;
			
			if (GUILayout.Button( (i == currentlySelected) ? "Selected" : "Select"))
			{
				currentlySelected = i;
				manager.tabs[i].selected = true;
				//manager.tabs[i].content.SetActive(true);
				for (int j = 0; j < numTabs; j++)
				{
				    if (currentlySelected != j)
					{
						manager.tabs[j].selected = false;
						//manager.tabs[j].content.SetActive(false);
					}
				}
			}
			GUILayout.EndHorizontal();
		}
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Select None"))
		{
			for (int i = 0; i < numTabs; i++)
			{
				manager.tabs[i].selected = false;
				//manager.tabs[i].content.SetActive(false);
			}
			currentlySelected = -1;
		}
		if (GUILayout.Button("Select All"))
		{
			for (int i = 0; i < numTabs; i++)
			{
				manager.tabs[i].selected = true;
				//manager.tabs[i].content.SetActive(true);
			}
			currentlySelected = -1;
		}		
		GUILayout.EndHorizontal();		
	}
}
