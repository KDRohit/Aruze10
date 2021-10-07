using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(UIPanelModiferTool))]
public class UIPanelModiferToolInspector : TICoroutineMonoBehaviourEditor
{
	private bool isActive = false;

	public override void OnInspectorGUI()
	{

		UIPanelModiferTool myTarget = (UIPanelModiferTool)target;
		GUILayout.BeginHorizontal();
		GUILayout.Label("Mode", GUILayout.Width(76.0f));
		if (!isActive && GUILayout.Toggle(myTarget.state == UIPanelModiferTool.ExpansionState.NONE, "Nothing", "ButtonLeft"))
		{
			myTarget.state = UIPanelModiferTool.ExpansionState.NONE;
		}
		if (GUILayout.Toggle(myTarget.state == UIPanelModiferTool.ExpansionState.SQUASH, "Squash", "ButtonMid"))
		{
			myTarget.state = UIPanelModiferTool.ExpansionState.SQUASH;
			isActive = true;
		}
		if (GUILayout.Toggle(myTarget.state == UIPanelModiferTool.ExpansionState.EXPAND, "Expand", "ButtonRight"))
		{
			myTarget.state = UIPanelModiferTool.ExpansionState.EXPAND;
			isActive = true;
		}
		GUILayout.EndHorizontal();

		if (myTarget.state == UIPanelModiferTool.ExpansionState.NONE)
		{
			// If you're in the nothing state lets just hide the other settings.
			return;
		}

		KeyValuePair<Material, int> newEntry = new KeyValuePair<Material, int>();
		foreach(KeyValuePair<Material, int> kvp in myTarget.materialToDepthDic)
		{
			int newNumber = EditorGUILayout.IntField(kvp.Key.name, kvp.Value);
			if (newNumber != kvp.Value)
			{
				newEntry = new KeyValuePair<Material, int>(kvp.Key, newNumber);
			}
		}
		if (!newEntry.Equals(default(KeyValuePair<Material, int>)))
		{
			myTarget.materialToDepthDic[newEntry.Key] = newEntry.Value;
		}
	}
}