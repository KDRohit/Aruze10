using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
#pragma warning disable 0618 // Not using UnityEngine.Object, System.Type, params UnityEngine.GUILayoutOption[]) with the new allowSceneObjects parameter.
[CustomEditor(typeof(DynamicTabManager))]
public class DynamicTabManagerInspector : Editor
{
	DynamicTabManager manager;

	int numTabs = 1;
	int currentlySelected = -1;
	
	public override void OnInspectorGUI()
	{
		
		manager = target as DynamicTabManager;

		numTabs = (manager.tabs != null) ? manager.tabs.Length : 0; 

		manager.uiGrid = EditorGUILayout.ObjectField("Grid", manager.uiGrid, typeof(UICenteredGrid)) as UICenteredGrid;

		if (manager.uiGrid == null)
		{
			// The Dynamic version needs a UIGrid
			GUI.contentColor = Color.red;
			GUILayout.Label("No UIGrid found! This is neccssary for this class.");
			GUI.contentColor = Color.white;
		}

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

		int numActiveTabs = 0;
		for (int i = 0; i < numTabs; i++)
		{
			GUILayout.BeginHorizontal();
			manager.tabs[i] = EditorGUILayout.ObjectField(string.Format("Tab {0}", i), manager.tabs[i], typeof(TabSelector), true) as TabSelector;
			
			if (GUILayout.Button( (i == currentlySelected) ? "Selected" : "Select"))
			{
				currentlySelected = i;
				manager.tabs[i].selected = true;
				for (int j = 0; j < numTabs; j++)
				{
				    if (currentlySelected != j)
					{
						manager.tabs[j].selected = false;
					}
				}
			}
			if (manager.tabs[i] != null && manager.tabs[i].gameObject.activeSelf){ numActiveTabs++; }

			if (manager.tabs[i] != null &&
				manager.tabs[i].targetSprite != null &&
				manager.tabs[i].targetSprite.type != UISprite.Type.Sliced)
			{
				
				GUI.contentColor = Color.yellow;
				GUILayout.Label("Target sprite should be sliced for dynamic sizing.");
				GUI.contentColor = Color.white;
			}

			GUILayout.EndHorizontal();
		}
		if (numActiveTabs != numTabs)
		{
			GUI.contentColor = Color.red;
			GUILayout.Label("Not all tabs active! Make sure that you configure the UIGrid for max tabs before saving.");
			GUI.contentColor = Color.white;
		}
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Select None"))
		{
			for (int i = 0; i < numTabs; i++)
			{
				manager.tabs[i].selected = false;
			}
			currentlySelected = -1;
		}

		if (GUILayout.Button("Select All"))
		{
			for (int i = 0; i < numTabs; i++)
			{
				manager.tabs[i].selected = true;
			}
			currentlySelected = -1;
		}		
		GUILayout.EndHorizontal();		
	}
}
