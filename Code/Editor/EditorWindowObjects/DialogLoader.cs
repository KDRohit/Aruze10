//#define LIST_ALL_UILABELS

using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;


public class DialogLoaderObject : EditorWindowObject
{
	protected override string getButtonLabel()
	{
		return "Dialog Loader";
	}

	protected override string getDescriptionLabel()
	{
		return "Provides easy access to all of the Dialogs in a particular SKU.";
	}

	[SerializeField] private const string STARTUP_SCENE_PATH = "Assets/Data/HIR/Scenes/Startup.unity";
	[SerializeField] private List<string> dialogNames = new List<string>();
	[SerializeField] private List<GameObject> dialogsList = new List<GameObject>();
	[SerializeField] private int selectedIndex = -1;
	[SerializeField] private int selectedSkuIndex = -1;	
	
	private string[] skuNames = new string[]
	{
		"HIR"
	};
	
	// Convenience getter.
	private string skuName
	{
		get { return skuNames[selectedSkuIndex]; }
	}
	
	private void populateList()
	{
		Debug.Log("populate " + skuName);
		// Populate the dialog prefab list.
		dialogNames.Clear();
		dialogsList.Clear();
		
		TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(string.Format("Assets/Data/{0}/Resources/Data/Dialog Types.txt", skuName));
		if (textAsset == null)
		{
			Debug.LogError("Could not find Dialog Types in Resources for " + skuName);
			return;
		}
		
		JSON json = new JSON(textAsset.text);
		
		if (!json.isValid)
		{
			Debug.LogError("Dialog Types JSON is invalid!");
			return;
		}
		
		foreach (JSON data in json.getJsonArray("dialog_types"))
		{
			string gameFolder = data.getString("game_folder", "");
			bool isBundled = data.getBool("is_bundled", false);
			tryLoadPrefab(gameFolder, data.getString("prefab", ""), isBundled);
			tryLoadPrefab(gameFolder, data.getString("small_device_prefab", ""), isBundled);
		}

		selectedIndex = -1;
	}

#if LIST_ALL_UILABELS
	private void CheckForUILabel(GameObject prefab, string path)
	{
		List<GameObject> allChildren = CommonGameObject.findAllChildren(prefab, true);
		foreach (GameObject child in allChildren)
		{
			if(child.GetComponent<UILabel>() != null)
				Debug.LogFormat("XXX UILabel in prefab: {0}, object: {1}", path, child.name );
		}

	}
#endif

	private void tryLoadPrefab(string gameFolder, string relativePath, bool isBundled = false, bool isInCommonFolder = false)
	{
		if (relativePath == "")
		{
			return;
		}

		//#warning "Double check these paths..."
		string resourcesPath = string.Format("Bundles/Initialization/Prefabs/Dialogs/{0}.prefab", relativePath);  //Dbl-check
		string bundledPath = string.Format("Bundles/Features/{0}.prefab", relativePath); //dbl-check
		string path = "";

		if (gameFolder != "")
		{
			// Some dialogs are stored in their game folders so they can be bundled with the games they go with.
			path = string.Format("Assets/Data/Games/{0}/{1}", gameFolder, resourcesPath);
		}
		else if (isBundled)
		{
			path = string.Format("Assets/Data/{0}/{1}", skuName, bundledPath);
		}
		else
		{
			path = string.Format("Assets/Data/{0}/{1}", skuName, resourcesPath);
		}

		// If this has a path that we edit..
		if (relativePath.Contains("{0}"))
		{
			string basePath = isBundled ? @"Bundles/Features/" : @"Bundles/Initialization/Prefabs/Dialogs/";
			string folderStructure = relativePath.Remove(relativePath.IndexOf("{0}"));

			string pathToSearch = string.Format(@"Assets/Data/{0}/{1}", skuName, basePath + folderStructure);

			DirectoryInfo directory = new DirectoryInfo(pathToSearch);
			DirectoryInfo[] directories = directory.GetDirectories();

			foreach (DirectoryInfo folder in directories)
			{
				string pathToTry = string.Format(relativePath, folder.Name);
				bool foundInCommon = false;
				foundInCommon = (folder.Name == "Common");
		
				tryLoadPrefab(gameFolder, pathToTry, isBundled, foundInCommon);
			}

			// We don't need to go any further than this.
			return;
		}

		GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
		
		if (prefab != null)
		{
			dialogNames.Add(relativePath);
			dialogsList.Add(prefab);
		#if LIST_ALL_UILABELS
			CheckForUILabel(prefab, path);
		#endif
		}
		// Don't throw errors for failing to find prefabs in common folders.
		else if (!isInCommonFolder)
		{
			Debug.LogError("Could not find dialog prefab at: " + relativePath);
		}
	}
	
	public override void drawGuts(Rect position)
	{
		int newSkuIndex = EditorGUILayout.Popup(
		    "Sku:",
			selectedSkuIndex,
			skuNames);
		
		if (newSkuIndex == -1)
		{
			newSkuIndex = 0;

			// set sku index based on current Slots sku setting
			string currentSkuName =	SkuResources.skuString;
			for (int i = 0; i < skuNames.Length; i++)
			{
				if (skuNames[i].ToLower() == currentSkuName)
				{
					newSkuIndex = i;
					break;
				}
			}
		}
		
		if (newSkuIndex != selectedSkuIndex)
		{
			selectedSkuIndex = newSkuIndex;
			populateList();
		}

		selectedIndex = EditorGUILayout.Popup(
		    "Select Dialog:",
			selectedIndex, 
			dialogNames.ToArray());
		
		GUILayout.Space(50);
		
		if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path != STARTUP_SCENE_PATH)
		{
			if (GUILayout.Button("Load the Startup Scene"))
			{
				EditorApplication.isPlaying = false;
				UnityEditor.SceneManagement.EditorSceneManager.OpenScene(STARTUP_SCENE_PATH);
			}
			return;
		}
		else if (selectedIndex > -1 && GUILayout.Button("Load: " + dialogNames[selectedIndex]))
		{
			GameObject dialogPanel = GameObject.Find("Dialog Panel");
			if (dialogPanel == null)
			{
				EditorUtility.DisplayDialog("Missing", "Couldn't find the Dialog Panel in the scene.", "Doh!");	
			}
			else
			{
				GameObject go = PrefabUtility.InstantiatePrefab(dialogsList[selectedIndex]) as GameObject;
				go.transform.parent = dialogPanel.transform;
				go.transform.localPosition = Vector3.zero;
				go.transform.localScale = Vector3.one;
				Selection.activeTransform = go.transform;
			}
		}

		if (GUILayout.Button("Repopulate"))
		{
			populateList();
		}
//		GUILayout.EndHorizontal();
	}
}


public class DialogLoader : EditorWindow
{
	[MenuItem("Zynga/Editor Tools/Dialog Loader")]
	public static void openDialogLoader()
	{
		DialogLoader dialogLoader = (DialogLoader)EditorWindow.GetWindow(typeof(DialogLoader));
		dialogLoader.Show();
	}

	private DialogLoaderObject dialogLoaderObject;
	
	public void OnGUI()
	{
		if (dialogLoaderObject == null)
		{
			dialogLoaderObject = new DialogLoaderObject();
		}
		dialogLoaderObject.drawGUI(position);
	}
}