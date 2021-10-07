using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

/**
 * Editor wizard to help fully duplicate and relink a game submodule folder so it is fully stand alone from the submodule it was cloned from.
 *
 * Original Author: Shaun Peoples
 * Creation Date: 6/9/2021
 */
public class GameDeepCopier : EditorWindow
{
	private string selectedGameFolder = string.Empty;
	private string sourceGameKey = string.Empty;                 // the game key for source data
	private string destGameKey = string.Empty;                   // the new game key entered by the user
	private string selectedFolderParentPath = string.Empty;      // parent directory for the new game (either the parent of the selected folder, or a new one chosen in the UI)
	private string newGameFolderOverrideName = string.Empty;     // optional entered name for the new game folder (ie "Assets/Data/Games/THIS_IS_A_NEW_GAME/newGameKey")
	private string newGameFolderFullPath = string.Empty;         // full destination path for the new game folder

	private List<GameObject> prefabGameObjectList = new List<GameObject>();          //stores the above gameobjects to be looped through later
	private List<GameObject> backupPrefabGameObjectList = new List<GameObject>();    //for storing the above prefabGameObjectList as a backup; we need this for holding the place of the override slots & prefab names in the UI

	private bool isValidFolder;
	private bool confirmSelectedFolder;
	private bool isTaggingForAssetBundle = false;
	private const char UNITY_PATH_SEPARATOR = '/'; // Unity for AssetDatabase calls uses a fixed path separator which matches what Mac uses.  We do a few calls in here that use the file system which will need to actually use the correct path separator for the OS though.

	private readonly List<AssetCopyTrackingData> assetCopyTrackingData = new List<AssetCopyTrackingData>();
	// Tracking asset paths that will need to be converted into assetCopyTrackingData, but can't right away since we are batching 
	// AssetDatabase changes the GUIDs we get will not be correct until we complete the full batch. So we do the conversion after copying everything.
	private Dictionary<string, string> assetCopyPathTrackingData = new Dictionary<string, string>();
	
	private class AssetCopyTrackingData
	{
		public string srcGuid;
		public string destGuid;
	}

	/* Draw Editor Menu Item */
	[MenuItem("Zynga/Wizards/Game Deep Copier")]
	static void CreateWizard()
	{
		GameDeepCopier window = (GameDeepCopier) GetWindow(typeof(GameDeepCopier));
		window.titleContent = new GUIContent("Deep Copier");
		window.setDefaultWindowSize();
	}

	// Sets the window to the given size and centers it on the screen.
	private void setWindowSize(int windowWidth, int windowHeight)
	{
		this.position = new Rect((Screen.currentResolution.width - windowWidth) / 2, (Screen.currentResolution.height - windowHeight) / 2, windowWidth, windowHeight);
	}

	private void setDefaultWindowSize()
	{
		setWindowSize(640, 480);
	}

	private string getParentFolder(string folder)
	{
		string selectedFolder = folder;
		string parentFolder = "";

		try
		{
			int trimIndex = selectedFolder.LastIndexOf(UNITY_PATH_SEPARATOR);
			if (trimIndex > 0)
			{
				parentFolder = selectedFolder.Remove(trimIndex);
			}

			string[] selectedFolderParts = selectedFolder.Split(new[] {UNITY_PATH_SEPARATOR}, StringSplitOptions.None);
			sourceGameKey = selectedFolderParts[selectedFolderParts.Length - 1];
		}
		catch(Exception ex)
		{
			Debug.LogError(ex.Message);
		}

		return parentFolder;
	}

	// Returns the folder currently selected in the Project view
	private string getSelectedGameFolder()
	{
		string path = AssetDatabase.GetAssetPath(Selection.activeObject);

		if (AssetDatabase.IsValidFolder(path))
		{
			confirmSelectedFolder = path != "";

			isValidFolder = getParentFolder(getParentFolder(path)) == "Assets" + UNITY_PATH_SEPARATOR + "Data" + UNITY_PATH_SEPARATOR + "Games";

			return path;
		}

		isValidFolder = false;
		return ("");
	}

	private string getFileName(string filePath)
	{
		string fileName = "";
		fileName = filePath.Substring(filePath.LastIndexOf(UNITY_PATH_SEPARATOR) + 1);
		return fileName;
	}

	private string removeOldGameKey(string fileName)
	{
		fileName = fileName.Substring(fileName.IndexOf(" ", StringComparison.InvariantCulture) + 1);
		return fileName;
	}

	private void copyAndRenamePrefabs(string prefabFolder, List<string> prefabPathsList)
	{
		foreach (string prefabPath in prefabPathsList)
		{
			string prefabName = getFileName(prefabPath); 
			string removeOldGameKeyName = removeOldGameKey(prefabName);
			string newPrefabName = destGameKey + " " + removeOldGameKeyName;
			string newPath = prefabFolder + UNITY_PATH_SEPARATOR + newPrefabName;
			AssetDatabase.CopyAsset(prefabPath, newPath); 

			string srcGuid = AssetDatabase.AssetPathToGUID(prefabPath);
			string destGuid = AssetDatabase.AssetPathToGUID(newPath);

			if (assetCopyTrackingData.Find(x => x.srcGuid == srcGuid) == null)
			{
				AssetCopyTrackingData ad = new AssetCopyTrackingData { srcGuid = srcGuid, destGuid = destGuid };
				assetCopyTrackingData.Add(ad);
			}
		}
	}

	private List<string> generateFolderPrefabPathList(string folderPath)
	{
		string[] assetGuidList = AssetDatabase.FindAssets("t:Prefab", (new[] {folderPath + UNITY_PATH_SEPARATOR + "Prefabs"})); //Gets a list of the prefabs in the selected game folder (+ meta files)
		List<string> filePathList = new List<string>();

		foreach (string fileGuid in assetGuidList)
		{
			string filePath = AssetDatabase.GUIDToAssetPath(fileGuid);
			filePathList.Add(filePath);
		}

		return filePathList;
	}

	private void generatePrefabGameObjectList()
	{
		if (prefabGameObjectList.Count > 0)
		{
			prefabGameObjectList.Clear();
			backupPrefabGameObjectList.Clear();
		}

		List<string> prefabStringList = generateFolderPrefabPathList(selectedGameFolder);

		if (AssetDatabase.IsValidFolder(selectedGameFolder + UNITY_PATH_SEPARATOR + "Prefabs"))
		{
			foreach (string prefabPath in prefabStringList)
			{
				GameObject obj = (GameObject) AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject));
				prefabGameObjectList.Add(obj);
				backupPrefabGameObjectList.Add(obj);
			}
		}
	}

	private void updateNewGameFolderPath()
	{
		switch (newGameFolderOverrideName)
		{
			case (""):
				newGameFolderFullPath = selectedFolderParentPath + UNITY_PATH_SEPARATOR + destGameKey;
				break;

			default:
				newGameFolderFullPath = selectedFolderParentPath + UNITY_PATH_SEPARATOR + newGameFolderOverrideName + UNITY_PATH_SEPARATOR + destGameKey;
				break;
		}
	}

	private void updateFullNewGameFolderPathForUIDisplay()
	{
		string defaultPath = "Assets" + UNITY_PATH_SEPARATOR + "Data" + UNITY_PATH_SEPARATOR + "Games";
		string returnPath = "";
		string gameKeyDisplayString = destGameKey != "" ? destGameKey : "???";

		if (newGameFolderOverrideName != "")
		{
			returnPath = defaultPath + UNITY_PATH_SEPARATOR + newGameFolderOverrideName;
		}
		else
		{
			returnPath = getParentFolder(selectedGameFolder);
		}

		newGameFolderFullPath = returnPath + UNITY_PATH_SEPARATOR + gameKeyDisplayString;
	}

	// selection change handler
	void OnSelectionChange()
	{
		selectedGameFolder = getSelectedGameFolder();
		selectedFolderParentPath = getParentFolder(selectedGameFolder);

		updateFullNewGameFolderPathForUIDisplay();
		
		Repaint();
	}

	void OnEnable()
	{
		selectedGameFolder = getSelectedGameFolder();
		selectedFolderParentPath = getParentFolder(selectedGameFolder);
		generatePrefabGameObjectList(); 
		updateNewGameFolderPath();
		updateFullNewGameFolderPathForUIDisplay();
	}

	void OnGUI()
	{
		//makes sure we have a selected folder
		if (selectedGameFolder == "")
		{
			selectedGameFolder = getSelectedGameFolder();
		}

		GUILayout.Space(5.0f);

		if (selectedGameFolder != "" && isValidFolder)
		{
			GUI.color = Color.cyan;
		}
		else
		{
			GUI.color = Color.red;
		}

		EditorGUILayout.LabelField("Selected Game Folder:", selectedGameFolder);

		if (selectedGameFolder != "" && isValidFolder)
		{
			GUI.enabled = true;
		}
		else
		{
			GUI.enabled = false;
		}

		GUI.color = Color.white;
		GUILayout.Space(5.0f);
	   
		GUI.enabled = true;
		GUILayout.Space(8.0f);
		GUILayout.BeginHorizontal();

		GUI.color = Color.white;
		EditorGUILayout.LabelField("Enter New GameKey:", "", GUILayout.Width(145));
		destGameKey = EditorGUILayout.TextField("", destGameKey, GUILayout.Height(20));
		GUILayout.EndHorizontal();

		if (GUI.changed && confirmSelectedFolder) //redraw the UI if the new game key text is changed
		{
			updateFullNewGameFolderPathForUIDisplay();
		}

		GUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Tag For Asset Bundle:", "", GUILayout.Width(145));
		isTaggingForAssetBundle = EditorGUILayout.Toggle(isTaggingForAssetBundle, GUILayout.Width(20));
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();

		GUILayout.EndHorizontal();
		GUI.color = Color.white;
		GUILayout.Space(10.0f);

		if (destGameKey != "" && selectedGameFolder != "" && isValidFolder)
		{
			GUI.color = Color.green;
		}
		else if (destGameKey != "" && selectedGameFolder == "")
		{
			GUI.color = Color.red;
		}
		else
		{
			GUI.color = Color.red;
		}

		if (selectedGameFolder == "")
		{
			newGameFolderFullPath = "";
		}

		Repaint();

		EditorGUILayout.LabelField("New Project Name: ", newGameFolderFullPath);

		GUI.color = Color.white;
		GUILayout.Space(4.0f);

		if (confirmSelectedFolder && isValidFolder && destGameKey != "")
		{
			GUI.enabled = true;
			GUI.color = Color.green;
		}
		else
		{
			GUI.enabled = false;
			GUI.color = Color.white;
		}

		if (GUILayout.Button("Begin Deep Copy", GUILayout.Height(30)))
		{
			string finalFolderPath = "";

			bool doStartCopy = EditorUtility.DisplayDialog("Start Deep Copy?", ("Are you sure you want to deep copy the following folder? This can be quite slow.\n\n" + selectedGameFolder), "Yes", "Cancel");

			if (doStartCopy)
			{
				// Clear these each time we run to ensure they don't hang onto anything
				assetCopyTrackingData.Clear();
				assetCopyPathTrackingData.Clear();

				string gameDataPath = "Assets" + UNITY_PATH_SEPARATOR + "Data" + UNITY_PATH_SEPARATOR + "Games";
				string newGameParentFolderPath;
				if (newGameFolderOverrideName != "")
				{
					if (!AssetDatabase.IsValidFolder(gameDataPath + UNITY_PATH_SEPARATOR + newGameFolderOverrideName))
					{
						string newGameParentFolderPathGuid = AssetDatabase.CreateFolder(gameDataPath, newGameFolderOverrideName);
						newGameParentFolderPath = AssetDatabase.GUIDToAssetPath(newGameParentFolderPathGuid);
					}
					else 
					{
						newGameParentFolderPath = gameDataPath + UNITY_PATH_SEPARATOR + newGameFolderOverrideName;
					}
				}
				else
				{
					newGameParentFolderPath = selectedFolderParentPath;
				}

				if (AssetDatabase.IsValidFolder(newGameParentFolderPath + UNITY_PATH_SEPARATOR + destGameKey))
				{
					EditorUtility.DisplayDialog("Game Folder Already Exists", ("The game folder \"" + destGameKey + "\" already exists at this location.\n\nPlease choose a different name for this folder."), "Ok");
				}
				else
				{
					//Generate the new game folders
					string finalFolderGuid = AssetDatabase.CreateFolder(newGameParentFolderPath, destGameKey);
					finalFolderPath = AssetDatabase.GUIDToAssetPath(finalFolderGuid);
					
					string[] gameGroupNameParts = newGameParentFolderPath.Split(UNITY_PATH_SEPARATOR);
					string gameGroupName = gameGroupNameParts[gameGroupNameParts.Length - 1].ToLower();
					string assetBundleName = "game/" + gameGroupName + "/" + destGameKey.ToLower();

					string prefabFolderGuid = AssetDatabase.CreateFolder(finalFolderPath, "Prefabs");
					string prefabFolderPath = AssetDatabase.GUIDToAssetPath(prefabFolderGuid);
					//set folder asset bundle name and tags
					if (isTaggingForAssetBundle)
					{
						AssetImporter assetImporter = AssetImporter.GetAtPath(prefabFolderPath);
						assetImporter.SetAssetBundleNameAndVariant(assetBundleName, "None");
						UnityEngine.Object folderObject = AssetDatabase.LoadMainAssetAtPath(prefabFolderPath);
						AssetDatabase.SetLabels(folderObject, new[] {"HIR"});
					}

					try
					{
						//this is where we copy all the source prefabs and files to the new folder
						AssetDatabase.StartAssetEditing();
						folderAndFileCopy(selectedFolderParentPath + UNITY_PATH_SEPARATOR+ sourceGameKey + UNITY_PATH_SEPARATOR + sourceGameKey + " Stuff", finalFolderPath + UNITY_PATH_SEPARATOR + destGameKey + " Stuff");
						AssetDatabase.StopAssetEditing();
						
						// Determine the new guids from all the assets we copied now that we are done calling AssetDatabase.StopAssetEditing();
						foreach (KeyValuePair<string, string> kvp in assetCopyPathTrackingData)
						{
							string srcGuid = AssetDatabase.AssetPathToGUID(kvp.Key);
							string destGuid = AssetDatabase.AssetPathToGUID(kvp.Value);
							
							if (assetCopyTrackingData.Find(x => x.srcGuid == srcGuid) == null && !string.IsNullOrEmpty(srcGuid))
							{
								AssetCopyTrackingData ad = new AssetCopyTrackingData { srcGuid = srcGuid, destGuid = destGuid };
								assetCopyTrackingData.Add(ad);
							}
						}
				   
						List<string> selectFolderPrefabPathsList = generateFolderPrefabPathList(selectedGameFolder);
						copyAndRenamePrefabs(prefabFolderPath, selectFolderPrefabPathsList);
					}
					catch
					{
						AssetDatabase.StopAssetEditing();
					}
				}
			}

			//remap all the guids in the copied files
			try
			{
				AssetDatabase.StartAssetEditing();
				// At this point convert finalFolderPath to use OS DirectorySeparatorChar, because everything in findAndReplaceGuids
				// uses file system calls and not Unity's AssetDatabase.
				if (UNITY_PATH_SEPARATOR != Path.DirectorySeparatorChar)
				{
					finalFolderPath = finalFolderPath.Replace(UNITY_PATH_SEPARATOR, Path.DirectorySeparatorChar);
				}
				findAndReplaceGuids(finalFolderPath);
				AssetDatabase.StopAssetEditing();
			}
			catch
			{
				AssetDatabase.StopAssetEditing();
			}
			
			AssetDatabase.Refresh();
		}

		//pins the version label to the lower-right side of the window underneath the Go button
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
	}
	
	private void folderAndFileCopy(string sourceDirPath, string destDirPath)
	{
		// Ensure that the sourceDirPath uses OS path separators rather than the Unity ones,
		// since DirectoryInfo isn't part of the AssetDatabase that uses the Unity style paths
		string operatingSytemSpecificDirPath;
		if (UNITY_PATH_SEPARATOR != Path.DirectorySeparatorChar)
		{
			operatingSytemSpecificDirPath = sourceDirPath.Replace(UNITY_PATH_SEPARATOR, Path.DirectorySeparatorChar);
		}
		else
		{
			operatingSytemSpecificDirPath = sourceDirPath;
		}

		DirectoryInfo dir = new DirectoryInfo(operatingSytemSpecificDirPath);
		if (dir == null || !dir.Exists)
		{
			Debug.LogError($"GameDeepCopier.folderAndFileCopy() - Unable to find operatingSytemSpecificDirPath = {operatingSytemSpecificDirPath}");
			throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + operatingSytemSpecificDirPath);
		}

		string parentFolder = destDirPath.Remove(destDirPath.LastIndexOf(UNITY_PATH_SEPARATOR));
		string[] pathParts = destDirPath.Split(new[] {UNITY_PATH_SEPARATOR}, StringSplitOptions.None);
		string folderName = pathParts[pathParts.Length - 1];
		AssetDatabase.CreateFolder(parentFolder, folderName);

		DirectoryInfo[] dirs = dir.GetDirectories();
		
		// Get the files in the directory and copy them to the new location.
		FileInfo[] files = dir.GetFiles();
		foreach (FileInfo file in files)
		{
			//don't copy meta files, Unity will create new ones.
			if (file.Name.EndsWith("meta"))
			{
				continue;
			}
			
			string destPath = destDirPath + UNITY_PATH_SEPARATOR + file.Name;

			if (!destPath.StartsWith("Assets"))
			{
				destPath = "Assets" + UNITY_PATH_SEPARATOR + destPath.Split(new[] {"Assets" + UNITY_PATH_SEPARATOR}, StringSplitOptions.None)[1];
			}
			
			string srcPath = sourceDirPath + UNITY_PATH_SEPARATOR + file.Name;
			if (!srcPath.StartsWith("Assets"))
			{
				srcPath = "Assets" + UNITY_PATH_SEPARATOR + srcPath.Split(new[] {"Assets" + UNITY_PATH_SEPARATOR}, StringSplitOptions.None)[1];
			}
			
			AssetDatabase.CopyAsset(srcPath, destPath); //copies the files to the new Prefabs folder
			AssetDatabase.ImportAsset(destPath);

			if (!assetCopyPathTrackingData.ContainsKey(srcPath) && !string.IsNullOrEmpty(srcPath) && !string.IsNullOrEmpty(destPath))
			{
				assetCopyPathTrackingData.Add(srcPath, destPath);
			}
		}

		foreach (DirectoryInfo subDir in dirs)
		{
			string tempPath = destDirPath + UNITY_PATH_SEPARATOR + subDir.Name;
			
			// Need to ensure that what we pass into folderAndFileCopy() uses the unity specific file separator
			// and not the DirectoryInfo's version which will use the OS separator which will be wrong on Windows
			string sourceUnityPath;
			if (UNITY_PATH_SEPARATOR != Path.DirectorySeparatorChar)
			{
				sourceUnityPath = subDir.FullName.Replace(Path.DirectorySeparatorChar, UNITY_PATH_SEPARATOR);
			}
			else
			{
				sourceUnityPath = subDir.FullName;
			}
			
			folderAndFileCopy(sourceUnityPath, tempPath);
		}
	}
	
	private void findAndReplaceGuids(string sourceDirPath)
	{
		DirectoryInfo dir = new DirectoryInfo(sourceDirPath);
		if (dir == null || !dir.Exists)
		{
			Debug.LogError($"GameDeepCopier.findAndReplaceGuids() - Unable to find sourceDirPath = {sourceDirPath}");
			throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirPath);
		}

		DirectoryInfo[] dirs = dir.GetDirectories();
		
		// Get the files in the directory
		FileInfo[] files = dir.GetFiles();
		foreach (FileInfo file in files)
		{
			//don't check meta files for guid values to change
			if (file.Name.EndsWith("meta") || file.Name.EndsWith("png") || file.Name.EndsWith("fbx") || file.Name.EndsWith("jpg"))
			{
				continue;
			}
			
			// Ensure the path is relative to the Assets folder
			string srcPath = sourceDirPath + Path.DirectorySeparatorChar + file.Name;
			if (!srcPath.StartsWith("Assets"))
			{
				srcPath = "Assets" + Path.DirectorySeparatorChar + srcPath.Split(new[] {"Assets" + Path.DirectorySeparatorChar}, StringSplitOptions.None)[1];
			}

			string fileText = File.ReadAllText(file.FullName);
			foreach (AssetCopyTrackingData ad in assetCopyTrackingData.Where(ad => fileText.Contains(ad.srcGuid)))
			{
				fileText = fileText.Replace(ad.srcGuid, ad.destGuid);
				File.WriteAllText(srcPath, fileText);
			}
		}

		foreach (DirectoryInfo subDir in dirs)
		{
			findAndReplaceGuids(subDir.FullName);
		}
	}
}
