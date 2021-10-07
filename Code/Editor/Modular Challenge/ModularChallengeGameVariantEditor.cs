using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

/**
 * Stub class to allow multi-edit inspectors for picking rounds.
 */
[CustomEditor(typeof(ModularChallengeGameVariant), true), CanEditMultipleObjects]
public class ModularChallengeGameVariantEditor : Editor 
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		if(GUILayout.Button("Select Anchor Array"))
		{
			selectAnchorArray();
		}

		if(GUILayout.Button("Select Anchor Array Child+1"))
		{
			selectAnchorArrayChildren();
		}

		EditorGUILayout.Separator();

		GUI.backgroundColor = Color.cyan;
		if(GUILayout.Button("Select Child PickItems"))
		{
			selectChildPickItems();
		}

		EditorGUILayout.Separator();
		EditorGUILayout.Separator();

		GUI.backgroundColor = Color.red;
		if(GUILayout.Button("Clear Child PickItems"))
		{
			clearChildPickItems();
		}
	}

	// select all gameobjects currently in the anchor array
	private void selectAnchorArray()
	{
		Selection.objects = ((ModularPickingGameVariant)target).pickAnchors.ToArray();
	}

	// select the immediate child of each anchor
	private void selectAnchorArrayChildren()
	{
		Object[] anchors = ((ModularPickingGameVariant)target).pickAnchors.ToArray();
		List<Object> anchorChildren = new List<Object>();
		foreach (Object anchor in anchors)
		{
			// get first child and add
			anchorChildren.Add((anchor as GameObject).transform.GetChild(0).gameObject);
		}
		Selection.objects = anchorChildren.ToArray();
	}

	// select all child gameObjects with a BasePickItem derived component
	private void selectChildPickItems()
	{
		// find all children
		PickingGameBasePickItem[] pickItems = ((ModularChallengeGameVariant)target).GetComponentsInChildren<PickingGameBasePickItem>();

		List<GameObject> goList = new List<GameObject>();
		foreach (PickingGameBasePickItem item in pickItems)
		{
			goList.Add(item.gameObject);
		}

		Selection.objects = goList.ToArray();
	}

	// remove all components from children
	private void clearChildPickItems()
	{
		PickingGameBasePickItem[] pickItems = ((ModularChallengeGameVariant)target).GetComponentsInChildren<PickingGameBasePickItem>();
		foreach (PickingGameBasePickItem item in pickItems)
		{
			DestroyImmediate(item);
		}
	}
}
