using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

/*
	Class Name: UIEditor
	Author: Michael Christensen-Calvin <mchristensencalvin@zynga.com>
	Description: 
*/
public class UIEditor : EditorWindow
{
	[MenuItem("Zynga/Editor Tools/Editor Window Objects/UIEditor")]
	public static void openUIEditor()
	{
		UIEditor uiEditor = (UIEditor)EditorWindow.GetWindow(typeof(UIEditor));
		uiEditor.Show();
	}

	private UIEditorObject editorObject;
	public void OnGUI()
	{
		if (editorObject == null)
		{
			editorObject = new UIEditorObject();
		}
		editorObject.drawGUI(position);
	}
}

public class UIEditorObject : EditorWindowObject
{

	protected override string getButtonLabel()
	{
		return "UIEditor";
	}

	protected override string getDescriptionLabel()
	{
		return "Provides easy access to a bunch of common UI element templates.";
	}
	
	private bool isLoaded = false;
	private Dictionary<string, GameObject> templates;
	private List<string> templateList= new List<string>()
	{
		"Text Mesh Pro Label",
		"Button",
		"Image Button",
		"Base Dialog"
	};
	
	private void loadAll()
	{
		templates = new Dictionary<string, GameObject>();
		GameObject prefab;
		string key;
		for (int i = 0; i < templateList.Count; i++)
		{
			key = templateList[i];
			prefab = loadPrefab(key);
			if (prefab != null)
			{
				templates.Add(key, prefab);
			}
			else
			{
				Debug.LogErrorFormat("UIEditor.cs -- loadAll -- Failed to load template: {0}", key);
			}
		}
	}
	
	private GameObject loadPrefab(string name)
	{
		string path = string.Format("Assets/Data/HIR/Prefabs/Templates/{0}.prefab", name);
		return AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
	}
	
	public override void drawGuts(Rect position)
	{
		if (!isLoaded || templates == null || GUILayout.Button("Refresh Templates"))
		{
			loadAll();
			isLoaded = true;
		}
		GameObject parentObject = null;
		if (Selection.activeGameObject != null)
		{
	   		parentObject = (GameObject)Selection.activeObject;
		}
		
		if (parentObject == null)
		{
			// Don't even show buttons if there is no object selected.
			GUIStyle redTextStyle = new GUIStyle(EditorStyles.textField);
			redTextStyle.normal.textColor = Color.red;
			GUILayout.Label("Select a parent object in the scene", redTextStyle);
			return;
		}
		Transform parent = parentObject.transform;
		GUILayout.BeginVertical();
		foreach (KeyValuePair<string, GameObject> pair in templates)
		{
			if (GUILayout.Button(string.Format("Add {0}", pair.Key)))
			{
				NGUITools.AddChild(parentObject, pair.Value);
			}
		}
		GUILayout.EndVertical();
	}
}