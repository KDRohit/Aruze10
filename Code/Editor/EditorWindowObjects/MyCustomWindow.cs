using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;


/*

Create a new file and copy the following into it at Unity/Assets/Code/Editor/EditorWindowObjects/gitignore/window_config.txt:

{
	"tools":
	[
		"DialogLoaderObject",
		"AtlasViewerObject"
		"SpriteSwapperObject",
		"SpritePropertyCopierObject",
		"ImageButtonSpriteChangerObject",
		"SwapSpriteAtlasEditorObject"
	]
}

*/
public class MyCustomWindow : EditorWindow
{
	[MenuItem("Zynga/Editor Tools/MyCustomWindow")]
	public static void openMyCustomWindow()
	{
		MyCustomWindow myCustomWindow = (MyCustomWindow)EditorWindow.GetWindow(typeof(MyCustomWindow));
		myCustomWindow.Show();
	}

    [SerializeField] private List<EditorWindowObject> windowObjects;
	[SerializeField] private bool shouldReload = false;
	[SerializeField] private Vector2 scrollPosition = Vector2.zero;
	
	public void OnGUI()
	{
		scrollPosition = GUILayout.BeginScrollView(scrollPosition);
		
		GUILayout.BeginVertical();

		if (GUILayout.Button("Reload List"))
		{
			shouldReload = true;
		}
		
		if (windowObjects == null || shouldReload)
		{
			// Initialize it.
		    init();	
			shouldReload = false;
		}
		if (windowObjects != null)
		{
			for (int i = 0; i < windowObjects.Count; i++)
			{
				if (windowObjects[i] != null)
				{
					windowObjects[i].drawGUI(position);
				}

			}			
		}
		GUILayout.EndVertical();
		GUILayout.EndScrollView();
	}

	private void init()
	{
		windowObjects = new List<EditorWindowObject>();
		TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Code/Editor/EditorWindowObjects/gitignore/window_config.txt");
		JSON json = new JSON(textAsset.text);

		if (!json.isValid)
		{
			Debug.LogErrorFormat("MyCustomWindow.cs -- init -- json is invalid");
			return;
		}

		string[] toolList = json.getStringArray("tools");

		for (int i = 0; i < toolList.Length; i++)
		{
			string className = toolList[i];
		
			System.Type classType = System.Type.GetType(className);
			if (classType == null)
			{
				Debug.LogErrorFormat("MyCustomWindow.cs -- init -- classType was null: {0}", className);
				return;
			}
			EditorWindowObject obj = System.Activator.CreateInstance(classType) as EditorWindowObject;

			if (obj == null)
			{
				Debug.LogErrorFormat("MyCustomWindow.cs -- init -- Could not create editor window object {0}", className);
			}
			windowObjects.Add(obj);			
		}
	}
}

