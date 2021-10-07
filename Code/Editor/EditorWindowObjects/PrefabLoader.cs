//#define LIST_ALL_UILABELS

using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;


public class PrefabLoaderObject : EditorWindowObject
{
	protected override string getButtonLabel()
	{
		return "Prefab Loader";
	}

	protected override string getDescriptionLabel()
	{
		return "Provides easy access to all of the Dialogs in a particular SKU.";
	}

	private const string STARTUP_SCENE_PATH = "Assets/Data/HIR/Scenes/Startup.unity";
	private List<string> dialogNames = new List<string>();
	private List<GameObject> dialogsList = new List<GameObject>();
	private int selectedIndex = -1;
	private int selectedSkuIndex = -1;	
	
	private string[] skuNames = new string[]
	{
		"HIR"
	};
	
	// Convenience getter.
	private string skuName
	{
		get { return skuNames[selectedSkuIndex]; }
	}
		
	public override void drawGuts(Rect position)
	{
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Load Overlay Panel"))
		{
			GameObject overlayPrefab = SkuResources.loadSkuSpecificResourcePrefab("Prefabs/Overlay/Overlay Panel");
			addToScene("2 Overlay Camera", overlayPrefab);
		}
		if (GUILayout.Button("Select Prefab"))
		{
			
			GameObject overlayPrefab = SkuResources.loadSkuSpecificResourcePrefab("Prefabs/Overlay/Overlay Panel");
			Selection.activeObject = overlayPrefab;
			EditorGUIUtility.PingObject(overlayPrefab);
		}
		GUILayout.EndHorizontal();

		
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Load Main Lobby Panel"))
		{
			GameObject lobbyPrefab = SkuResources.loadSkuSpecificResourcePrefab("Prefabs/Lobby/Lobby Main Panel");
			addToScene("0 Camera", lobbyPrefab);
		}
		if (GUILayout.Button("Select Prefab"))
		{
			GameObject lobbyPrefab = SkuResources.loadSkuSpecificResourcePrefab("Prefabs/Lobby/Lobby Main Panel");			
			Selection.activeObject = lobbyPrefab;
			EditorGUIUtility.PingObject(lobbyPrefab);
		}		
		GUILayout.EndHorizontal();
		
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Load Login Panel"))
		{
			GameObject loginPrefab = SkuResources.loadSkuSpecificResourcePrefab("Prefabs/UIRoot/Login Panel");
			addToScene("0 Camera", loginPrefab);
		}
		
		if (GUILayout.Button("Select Prefab"))
		{
			
			GameObject loginPrefab = AssetDatabase.LoadAssetAtPath("Prefabs/UIRoot/Login Panel", typeof(GameObject)) as GameObject;
			Selection.activeObject = loginPrefab;
			EditorGUIUtility.PingObject(loginPrefab);
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Load Friends Tab"))
		{
			GameObject friendsTabPrefab = AssetDatabase.LoadAssetAtPath("Assets/Data/HIR/Bundles/Features/Network Friends/Profile Dialog Bundle/Friends Tab.prefab", typeof(GameObject)) as GameObject;
		    addToDialogPanel(friendsTabPrefab);
		}		
		if (GUILayout.Button("Select Prefab"))
		{
			GameObject friendsTabPrefab = AssetDatabase.LoadAssetAtPath("Assets/Data/HIR/Bundles/Features/Network Friends/Profile Dialog Bundle/Friends Tab.prefab", typeof(GameObject)) as GameObject;
			Selection.activeObject = friendsTabPrefab;
			EditorGUIUtility.PingObject(friendsTabPrefab);
		}
		
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Load Friends Profile Tab"))
		{
			GameObject friendsTabPrefab = AssetDatabase.LoadAssetAtPath("Assets/Data/HIR/Bundles/Features/Network Friends/Profile Dialog Bundle/Friends Profile Tab.prefab", typeof(GameObject)) as GameObject;
		    addToDialogPanel(friendsTabPrefab);
		}		
		if (GUILayout.Button("Select Prefab"))
		{
			GameObject friendsTabPrefab = AssetDatabase.LoadAssetAtPath("Assets/Data/HIR/Bundles/Features/Network Friends/Profile Dialog Bundle/Friends Profile Tab.prefab", typeof(GameObject)) as GameObject;
			Selection.activeObject = friendsTabPrefab;
			EditorGUIUtility.PingObject(friendsTabPrefab);
		}
		
		GUILayout.EndHorizontal();		

	}

	private void addToDialogPanel(GameObject prefab)
	{
		GameObject panel = GameObject.Find("Dialog Panel");
		GameObject go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
		Vector3 scale = go.transform.localScale;
		Vector3 position = go.transform.localPosition;
		go.transform.parent = panel.transform;
		go.transform.localPosition = position;
		go.transform.localScale = scale;
		Selection.activeGameObject = go;		
	}
	
	private void addToScene(string parentName, GameObject prefab)
	{
		GameObject camera = GameObject.Find(parentName);
		Transform anchorCenter = camera.transform.GetChild(0);
		//GameObject go = CommonGameObject.instantiate(prefab, anchorCenter);
		GameObject go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
		Vector3 scale = go.transform.localScale;
		Vector3 position = go.transform.localPosition;
		go.transform.parent = anchorCenter;
		go.transform.localPosition = position;
		go.transform.localScale = scale;
		Selection.activeGameObject = go;
	}
}


public class PrefabLoader : EditorWindow
{
	[MenuItem("Zynga/Editor Tools/Prefab Loader")]
	public static void openPrefabLoader()
	{
		PrefabLoader dialogLoader = (PrefabLoader)EditorWindow.GetWindow(typeof(PrefabLoader));
		dialogLoader.Show();
	}

	private PrefabLoaderObject dialogLoaderObject;
	
	public void OnGUI()
	{
		if (dialogLoaderObject == null)
		{
			dialogLoaderObject = new PrefabLoaderObject();
		}
		dialogLoaderObject.drawGUI(position);
	}
}
