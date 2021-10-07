using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System;
using Zynga.Unity.Attributes;

/**
Common functions that are only used by editor scripts.  These are kept separate to avoid the inclusion of bulky
libraries (like System.IO) in the build, as well as to just keep the understanding of what code does what clear.
*/
public static class CommonEditor
{
	public const string RESOURCES_PATH = "/Resources/";									// Path for common resources, relative to Assets
	public const string PROJECT_RESOURCES = "Assets" + CommonEditor.RESOURCES_PATH;		// The project path where resources are stored
	public const string DEFAULT_BUILD_ERROR_LOG_PATH = @"../../build/logs/Errors.Log";	// Default path to use if env var isn't set for the build errors log
	public const string BUILD_ERROR_LOG_ENV_VAR = "ERROR_GATHER_LOG";					// Environment variable name for the path for the build error logs
	private static string buildErrorsLogPath = "";

	/// Returns a list of prefab assets in the form of a list of type GameObject from some starting directory.
	public static List<GameObject> gatherPrefabs(string directory, bool recursive = true, string filePattern = "*.prefab")
	{
		List<GameObject> prefabList = new List<GameObject>();

		// Get all .prefab files in the directory		
		string[] filePaths = Directory.GetFiles(directory, filePattern, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
		foreach (string file in filePaths)
		{
			GameObject asset = AssetDatabase.LoadAssetAtPath(file, typeof(GameObject)) as GameObject;
			if (asset == null)
			{
				// This shouldn't happen unless the file is corrupted/broken, or if someone names a directory something.prefab
				Debug.LogWarning("Asset failed to load as prefab: " + file);
			}
			else
			{
				prefabList.Add(asset);
			}
		}

		return prefabList;
	}
	
	/// Returns a list of all assets of a given type from some starting directory.
	/// Example: List<Texture2D> uiTextures = CommonEditor.gatherAssets<Texture2D>("Assets/Textures/UI");
	public static List<T> gatherAssets<T>(string startPath = "Assets", string omitPath = "") where T : UnityEngine.Object
	{
		List<T> assets = new List<T>();
		recursiveGather(startPath, omitPath, assets);
		return assets;
	}
	
	/// Recursive support function for gatherAssets()
	private static void recursiveGather<T>(string path, string omitPath, List<T> assets) where T : UnityEngine.Object
	{
		foreach (string dir in Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly)) 
		{
			if (string.IsNullOrEmpty(omitPath) || !dir.Contains(omitPath))
			{
				recursiveGather(dir, omitPath, assets);
			}
		}
		foreach (string file in Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly)) 
		{
			if (!file.FastEndsWith(".meta"))
			{
				T asset = AssetDatabase.LoadAssetAtPath(file, typeof(T)) as T;
				if (asset != null)
				{
					assets.Add(asset);
				}
			}
		}
	}
	
	/// Collects AudioClip references from a directory
	public static List<AudioClip> gatherSoundClips(string directory, bool recursive = true)
	{
		List<AudioClip> clips = new List<AudioClip>();
		// Get all .prefab files in the directory		
		
		string[] filePaths = Directory.GetFiles(directory, "*.wav", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
		foreach (string file in filePaths)
		{
			//Debug.Log("asset path = " + file);
			AudioClip asset = AssetDatabase.LoadAssetAtPath(file, typeof(AudioClip)) as AudioClip;
			if (asset == null)
			{
				// This shouldn't happen unless the file is corrupted/broken, or if someone names a directory something.wav
				Debug.LogWarning("Asset(sound) failed to load as prefab: " + file);
			}
			else
			{
				clips.Add(asset);
			}
		}
		
		string[] filePathsA = Directory.GetFiles(directory, "*.ogg", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
		foreach (string file in filePathsA)
		{
			//Debug.Log("asset path = " + file);
			AudioClip asset = AssetDatabase.LoadAssetAtPath(file, typeof(AudioClip)) as AudioClip;
			if (asset == null)
			{
				// This shouldn't happen unless the file is corrupted/broken, or if someone names a directory something.wav
				Debug.LogWarning("Asset(sound) failed to load as prefab: " + file);
			}
			else
			{
				clips.Add(asset);
			}
		}
		
		string[] filePathsB = Directory.GetFiles(directory, "*.mp3", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
		foreach (string file in filePathsB)
		{
			//Debug.Log("asset path = " + file);
			AudioClip asset = AssetDatabase.LoadAssetAtPath(file, typeof(AudioClip)) as AudioClip;
			if (asset == null)
			{
				// This shouldn't happen unless the file is corrupted/broken, or if someone names a directory something.wav
				Debug.LogWarning("Asset(sound) failed to load as prefab: " + file);
			}
			else
			{
				clips.Add(asset);
			}
		}
		return clips;
	}
	
	/// Checks if the given assetPath has an existing meta file
	public static bool hasMetaFile(string assetPath)
	{
		string metaPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6) + assetPath + ".meta";
		return File.Exists(metaPath);
	}
	
	/// Fixes broken hideFlags on a given prefab, returns true if there was a problem
	public static bool fixBrokenHideFlags(GameObject prefab)
	{
		bool hasFixedSomething = false;
		int hideFlags;
		int desiredHideFlags;
		
		foreach (GameObject go in CommonGameObject.findAllChildren(prefab, true))
		{
			hideFlags = (int)go.hideFlags;
			desiredHideFlags = hideFlags & 0x03;
			if (hideFlags != desiredHideFlags)
			{
				go.hideFlags = (HideFlags)desiredHideFlags;
				hasFixedSomething = true;
			}
			
			// Check all components on this gameobject
			foreach (Component component in (go.GetComponents<Component>() as Component[]))
			{
				if (component != null)
				{
					hideFlags = (int)component.hideFlags;
					desiredHideFlags = hideFlags & 0x03;
					if (hideFlags != desiredHideFlags)
					{
						component.hideFlags = (HideFlags)desiredHideFlags;
						hasFixedSomething = true;
					}
				}
			}
		}
		
		return hasFixedSomething;
	}
	
	/// Makes a string a friendly server-key-looking thing
	public static string makeIdentifier(string s)
	{
		List<char> sequence = new List<char>();
		char[] items = s.ToLower().ToCharArray();
		int i = 0;
		
		// Skip to the first letter
		for (; i < items.Length; i++)
		{
			if (char.IsLetter(items[i]))
			{
				break;
			}
		}
		
		// Collect up everything that is valid
		for (; i < items.Length; i++)
		{
			if (char.IsLetterOrDigit(items[i]))
			{
				sequence.Add(items[i]);
			}
		}
		
		return new string(sequence.ToArray());
	}
	
	/// Gets a key name for a generic resource object
	public static string getResourceName(UnityEngine.Object obj)
	{
		string path = AssetDatabase.GetAssetPath(obj).Replace('\\', '/').Replace("Resources/", "*");
		string[] parts = path.Split('*');
		if (parts.Length > 1)
		{
			string name = parts[parts.Length - 1];
			int extIndex = name.LastIndexOf('.');
			if (extIndex > -1)
			{
				name = name.Substring(0, extIndex);
			}
			return name;
		}
		return "";
	}
	
	/// Gets all linked GameObjects from a starting GameObject recursively,
	/// and appends then to the given GameObject set dictionary so that only
	/// unique values are included.
	/// WARNING: This is used by Build.cs to make bundles -> USE CAUTION WHEN EDITING!
	public static void gatherGameObjectLinks(Dictionary<GameObject, bool> gathered, GameObject go)
	{
		if (go == null || gathered.ContainsKey(go))
		{
			return;
		}
		
		fixBrokenHideFlags(go);
		gathered.Add(go, true);
		
		// Add all children
		foreach (Transform child in go.transform)
		{
			gatherGameObjectLinks(gathered, child.gameObject);
		}
		
		// Add all linked GameObjects
		MonoBehaviour[] scripts = go.GetComponents<MonoBehaviour>() as MonoBehaviour[];
		foreach (MonoBehaviour script in scripts)
		{
			if (script == null)
			{
				Debug.LogWarning("GameObject has missing MonoBehaviour: " + AssetDatabase.GetAssetPath(go) + " @ " + CommonGameObject.getObjectPath(go));
			}
			else
			{
				gatherClassLinks(gathered, script);
			}
		}
	}
	
	/// Searches an instance object's properties to see if there are any Objects to add.
	/// WARNING: This is used by Build.cs to make bundles -> USE CAUTION WHEN EDITING!
	public static void gatherClassLinks(Dictionary<GameObject, bool> gathered, object instance)
	{
		BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
		System.Type instanceType = instance.GetType();
		FieldInfo[] fields = instanceType.GetFields(flags);
		
		foreach (FieldInfo fieldInfo in fields)
		{
			gatherInstance(gathered, fieldInfo.GetValue(instance));
		}
	}
	
	/// Tests an object to see if it is a GameObject, or links to GameObjects
	/// Adds any obvious GameObjects linked in the test object to the provided dictionary set.
	/// WARNING: This is used by Build.cs to make bundles -> USE CAUTION WHEN EDITING!
	public static void gatherInstance(Dictionary<GameObject, bool> gathered, object test)
	{
		if (test is GameObject)
		{
			gatherGameObjectLinks(gathered, (GameObject)test);
		}
		else if (test is GameObject[])
		{
			foreach (GameObject arrayGameObject in (GameObject[])test)
			{
				gatherGameObjectLinks(gathered, arrayGameObject);
			}
		}
		else if (test is object[])
		{
			foreach (object arrayObject in (object[])test)
			{
				if (arrayObject != null)
				{
					gatherClassLinks(gathered, arrayObject);
				}
			}
		}
	}
	
	/// Creates a folder structure in its entirety if it doesn't already exist for the asset in the path.
	public static void createNeededFolders(string assetPath)
	{
		string[] pathParts = assetPath.Split('/');
		string fullPath = "";
		string parentFolder = fullPath;
		string newPath;
		
		for (int i = 0; i < pathParts.Length - 1; i++)	// use - 1 since we don't want to use the filename, only the folders.
		{
			newPath = parentFolder;
			if (newPath != "")
			{
				newPath += "/";
			}
			newPath += pathParts[i];
			
			if (!Directory.Exists(newPath))
			{
				AssetDatabase.CreateFolder(parentFolder, pathParts[i]);
			}

			parentFolder = newPath;
		}
	}
	
	// Gathers up all paths, starting at the given path, that match the target criteria.
	public static void recursiveGatherPaths(string target, string path, List<string> paths)
	{
		if (path.Contains(target))
		{
			paths.Add(path);
		}
		else
		{
			foreach (string dir in Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly)) 
			{
				recursiveGatherPaths(target, dir, paths);
			}
		}
	}
	
	/// Cleans out any subdirectories that are empty starting at the given path,
	/// using a depth-first recursive search.
	public static void removeEmptyFolders(string path)
	{
		if (!Directory.Exists(path))
		{
			return;
		}
		
		string[] filePaths;
		string[] folderPaths;
		
		// Process subfolders first
		folderPaths = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
		foreach (string folderPath in folderPaths)
		{
			removeEmptyFolders(folderPath.Replace('\\', '/'));
		}
		
		// See if this folder should be deleted, now that we've done cleanup for all subfolders
		folderPaths = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
		if (folderPaths.Length > 0)
		{
			return;
		}

		filePaths = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly);

		if (filePaths.Length > 0)
		{
			if (filePaths.Length == 1 && filePaths[0].FastEndsWith(".DS_Store"))
			{
				// If the only remaining file is a .DS_Store file, which are hidden system files on Macs, then treat it as an empty folder for deletion.
			}
			else
			{
				return;
			}
		}
		
		// Remove this folder
		AssetDatabase.DeleteAsset(path);
	}
	
	/// Gets the MD5 of a file
	public static string getFileHash(string path)
	{
		if (!File.Exists(path))
		{
			Debug.LogError("Cannot file hash of non-existant file: " + path);
			return "";
		}
	    
	    byte[] bytes = File.ReadAllBytes(path);
	
	    // Encrypt bytes
	    MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
	    byte[] hashBytes = md5.ComputeHash(bytes);
	
	    // Convert the encrypted bytes to a string (base 16)
	    string hashString = "";
	
	    for (int i = 0; i < hashBytes.Length; i++)
	    {
	        hashString += System.Convert.ToString(hashBytes[i], 16).PadLeft(2, '0');
	    }
	
		return hashString.PadLeft(32, '0');
	}
	
	/// Flattens an object hierarchy
	public static void flattenHierarchy(Transform root, bool castShadows, bool receiveShadows)
	{
		MeshRenderer[] renderers = root.gameObject.GetComponentsInChildren<MeshRenderer>(true);
		foreach (MeshRenderer renderer in renderers)
		{
			renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
			renderer.receiveShadows = receiveShadows;
			renderer.transform.parent = root;
		}
		
		Animation[] animations = root.gameObject.GetComponentsInChildren<Animation>(true);
		foreach (Animation animation in animations)
		{
			UnityEngine.Object.DestroyImmediate(animation, true);
		}
		
		Transform[] transforms = root.gameObject.GetComponentsInChildren<Transform>(true);
		foreach (Transform transform in transforms)
		{
			if (transform != null && transform != root && transform.GetComponent<Renderer>() == null)
			{
				UnityEngine.Object.DestroyImmediate(transform.gameObject);
			}
		}
		
		EditorUtility.SetDirty(root);
	}

	//Convinience function for creation of ScriptableObject type asset.
	public static T createScriptableAsset<T>(string name = null) where T : ScriptableObject
	{
		T asset = ScriptableObject.CreateInstance<T> ();
		
		string path = AssetDatabase.GetAssetPath (Selection.activeObject);
		if (path == "") 
		{
			path = "Assets";
		} 
		else if (Path.GetExtension (path) != "") 
		{
			path = path.Replace (Path.GetFileName (AssetDatabase.GetAssetPath (Selection.activeObject)), "");
		}
		
		if (string.IsNullOrEmpty(name))
		{
			name = "New " + typeof(T).ToString();
			
		}
		
		string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath (path + "/" + name + ".asset");
		
		AssetDatabase.CreateAsset (asset, assetPathAndName);
		
		AssetDatabase.SaveAssets ();
		EditorUtility.FocusProjectWindow ();
		Selection.activeObject = asset;
		
		return asset;
	}

	/// <summary>
	/// Makes an array of fields for the array <a href="http://docs.unity3d.com/Documentation/ScriptReference/SerializedProperty.html">SerializedProperty</a> referenced
	/// by the <paramref name="propertyName"/>.
	/// </summary>
	/// <param name="obj">the instance containing the property.</param>
	/// <param name="propertyName">the name of the array property.</param>
	public static void serializedPropertyArray(SerializedObject obj, string propertyName)
	{
		SerializedProperty prop = obj.FindProperty(propertyName);
		if (EditorGUILayout.PropertyField(prop))
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical();
			while (prop.NextVisible(true))
			{
				if (prop.propertyPath != propertyName && !prop.propertyPath.StartsWith(propertyName + "."))
				{
					break;
				}
				EditorGUILayout.PropertyField(prop);
			}
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
		}
	}
	
	/// <summary>
	/// Makes an array of fields for the array <a href="http://docs.unity3d.com/Documentation/ScriptReference/SerializedProperty.html">SerializedProperty</a> referenced
	/// by the <paramref name="propertyName"/>, filtering by the <paramref name="type"/>.
	/// </summary>
	/// <param name="obj">the instance containing the property.</param>
	/// <param name="propertyName">the name of the array property.</param>
	/// <param name="type">the name of the type of the array.</param>
	public static void serializedPropertyArray(SerializedObject obj, string propertyName, string type)
	{
		SerializedProperty prop = obj.FindProperty(propertyName);
		if (EditorGUILayout.PropertyField(prop))
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical();
			
			//length
			if (prop.NextVisible(true))
			{
				if (prop.propertyPath != propertyName && !prop.propertyPath.StartsWith(propertyName + "."))
				{
				}
				else
				{
					EditorGUILayout.PropertyField(prop);
				}
			}
			
			while (prop.NextVisible(true))
			{
				if (prop.propertyPath != propertyName && !prop.propertyPath.StartsWith(propertyName + "."))
				{
					break;
				}
				EditorGUILayout.PropertyField(prop);
				if (prop.objectReferenceValue != null && !prop.objectReferenceValue.ToString().Contains(type))
				{
					prop.objectReferenceValue = null;
				}
			}
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
		}
	}
	
	/// <summary>
	/// Makes an array of fields for the array <a href="http://docs.unity3d.com/Documentation/ScriptReference/SerializedProperty.html">SerializedProperty</a> referenced
	/// by the <paramref name="propertyName"/>, filtering by the <paramref name="type"/>.
	/// </summary>
	/// <param name="obj">the instance containing the property.</param>
	/// <param name="propertyName">the name of the array property.</param>
	/// <param name="type">the type of the array.</param>
	public static void serializedPropertyArray(SerializedObject obj, string propertyName, System.Type type)
	{
		SerializedProperty prop = obj.FindProperty(propertyName);
		if (EditorGUILayout.PropertyField(prop))
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical();
			
			//length
			if (prop.NextVisible(true))
			{
				if (prop.propertyPath != propertyName && !prop.propertyPath.StartsWith(propertyName + "."))
				{
				}
				else
				{
					EditorGUILayout.PropertyField(prop);
				}
			}
			
			while (prop.NextVisible(true))
			{
				if (prop.propertyPath != propertyName && !prop.propertyPath.StartsWith(propertyName + "."))
				{
					break;
				}
				prop.objectReferenceValue = EditorGUILayout.ObjectField(prop.objectReferenceValue, type, true);
			}
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
		}
	}
	
	/// Returns an the root GameObjects in a scene, ONLY WORKS IN THE EDITOR
	public static GameObject[] sceneRoots()
	{
		return UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();	
	}
	
	// // After getting a list of root GameObjects,
	// // this can be called for each root object to add all the children to that list too.
	// public static void addChildrenToList(GameObject rootObject, List<GameObject> objectsList)
	// {
	// 	foreach (Transform child in rootObject.GetComponentsInChildren<Transform>(true))
	// 	{
	// 		objectsList.Add(child.gameObject);
	// 	}
	// }

	/// Returns whether a scripting define symbol is defined for the target's player build settings.
	public static bool IsScriptingDefineSymbolDefinedForGroup(string defineSymbol, BuildTargetGroup targetGroup)
	{
		List<string> defined = new List<string>(PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup).Split(';'));
		return defined.Contains(defineSymbol);
	}

	/// Add a scripting define symbol to the target's player build settings.
	public static void AddScriptingDefineSymbolForGroup(string defineSymbol, BuildTargetGroup targetGroup)
	{
		List<string> defined = new List<string>(PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup).Split(';'));
		if (!defined.Contains(defineSymbol))
		{
			defined.Add(defineSymbol);
			string concatenated = string.Join(";", defined.ToArray());
			PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, concatenated);
		}
	}

	/// Remove a scripting define symbol from the target's player build settings.
	public static void RemoveScriptingDefineSymbolForGroup(string defineSymbol, BuildTargetGroup targetGroup)
	{
		List<string> defined = new List<string>(PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup).Split(';'));
		if (defined.Contains(defineSymbol))
		{
			defined.Remove(defineSymbol);
			string concatenated = string.Join(";", defined.ToArray());
			PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, concatenated);
		}
	}

	/// Toggle (add if it's not there, remove if it's there) a scripting define symbol in the target's player build settings.
	public static void ToggleScriptingDefineSymbolForGroup(string defineSymbol, BuildTargetGroup targetGroup)
	{
		List<string> defined = new List<string>(PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup).Split(';'));
		if (defined.Contains(defineSymbol))
		{
			defined.Remove(defineSymbol);
		}
		else
		{
			defined.Add(defineSymbol);
		}
		string concatenated = string.Join(";", defined.ToArray());
		PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, concatenated);
	}

	/// <summary>
	/// Return SKU being built.
	/// </summary>
	public static string GetBuildSKU(bool upper)
	{
		string sku = SkuResources.skuString;
		if (upper)
		{
			sku = sku.ToUpper();
		}
		else
		{
			sku = sku.ToLower();
		}
		Debug.Log("GetBuildSku: " + sku);
		return sku;
	}
	
	// Copy a component from one object to another, with the same values.
	// If the target object already has this component type, only the values are copied.
	public static void copyComponent<T>(GameObject source, GameObject target) where T : Component
	{
		T component = source.GetComponent<T>();
		if (component != null)
		{
			if (UnityEditorInternal.ComponentUtility.CopyComponent(component))
			{
				T existing = target.GetComponent<T>();
				if (existing == null)
				{
					UnityEditorInternal.ComponentUtility.PasteComponentAsNew(target);
				}
				else
				{
					UnityEditorInternal.ComponentUtility.PasteComponentValues(existing);
				}
			}
		}
		
	}
	
	// Pings and selects the GameObject in a single command.
	// Useful for editor scripts.
	public static void pingAndSelectObject(GameObject go)
	{
		Selection.activeObject = go;
		EditorGUIUtility.PingObject(go);		
	}

	public static void drawDefaultInspectorWithOmit(SerializedObject obj, HashSet<string> omit)
	{
		if (obj == null || omit == null)
		{
			return;
		}
		SerializedProperty props = obj.GetIterator();
		props.Next(true);
		while (props.NextVisible(false))
		{
			if (omit != null && !omit.Contains(props.name))
			{
				EditorGUILayout.PropertyField(props, true);
			}
		}
	}

	// Function that uses reflection to extract a FieldInfo from a class using a propertyPath name.
	// Will search through all derived classes and will also find NonPublic properties.  This is mainly
	// intended for finding serialized fields in order to read out Attribute tags which are defined on them.
	public static System.Reflection.FieldInfo getFieldInfoForSerializedProperty(SerializedObject obj, string propertyPath)
	{
		System.Type targetObjType = obj.targetObject.GetType();

		// Search entire class hierarchy for the property (since it might be in a base class)
		while (targetObjType != null) 
		{
			System.Reflection.FieldInfo propertyFieldInfo = targetObjType.GetField(propertyPath, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);

			if (propertyFieldInfo != null)
			{
				return propertyFieldInfo;
			}

			targetObjType = targetObjType.BaseType; 
		}

		return null;
	}
	
	// Default handling for our new set of custom tags to be used in custom editors.  Right now those
	// tags include grouping tags for displaying serialized fields under group labeled foldouts,
	// and an omit tag for hiding fields in the inspector except when Debug mode is enabled for the inspector.
	public static void drawDefaultInspectorUsingCustomAttributeTags(SerializedObject obj, Dictionary<string, bool> foldoutGroupStatusDictionary)
	{
		if (obj == null)
		{
			return;
		}

		Dictionary<string, List<SerializedProperty>> groupedPropertyLists = new Dictionary<string, List<SerializedProperty>>();
		
		SerializedProperty props = obj.GetIterator();
		props.Next(true);
		while (props.NextVisible(false))
		{
			// Check if this is the reference to the script code file, and if so
			// we'll just go ahead and draw that since you can't attach attributes
			// to the code file
			if (props.propertyPath == "m_Script")
			{
				EditorGUILayout.PropertyField(props, true);
			}
			else
			{
				// This isn't the script code file, so we'll go ahead and check what
				// attributes are on it, and handle them accordingly.
				System.Reflection.FieldInfo propertyFieldInfo = getFieldInfoForSerializedProperty(obj, props.propertyPath);

				bool isShowingProperty = true;
				if (propertyFieldInfo != null)
				{
					// Check for the omit attribute
					OmitFromNonDebugInspector omitAttribute = Attribute.GetCustomAttribute(propertyFieldInfo, typeof(OmitFromNonDebugInspector)) as OmitFromNonDebugInspector;
					if (omitAttribute != null)
					{
						isShowingProperty = false;
					}
				}

				if (isShowingProperty)
				{
					// Determine if this Property has a GroupAttribute associated with it
					FoldoutHeaderGroup groupAttribute = null;

					if (propertyFieldInfo != null)
					{
						groupAttribute = Attribute.GetCustomAttribute(propertyFieldInfo, typeof(FoldoutHeaderGroup)) as FoldoutHeaderGroup;
					}

					// Default group for untagged elements is additional options
					string group = FoldoutHeaderGroup.ADDITIONAL_OPTIONS_GROUP;
					if (groupAttribute != null)
					{
						group = groupAttribute.groupName;
					}

					if (!groupedPropertyLists.ContainsKey(group))
					{
						groupedPropertyLists.Add(group, new List<SerializedProperty>());
					}

					// also create a foldout group status flag if one doesn't exist yet
					if (!foldoutGroupStatusDictionary.ContainsKey(group))
					{
						foldoutGroupStatusDictionary.Add(group, false);
					}

					groupedPropertyLists[group].Add(props.Copy());
				}
			}
		}
		
		// Get and sort the list of keys so that we always display the foldouts
		// in a fixed order
		List<string> propertyKeyList = new List<string>(groupedPropertyLists.Keys);
		propertyKeyList.Sort();
		
		for (int i = 0; i < propertyKeyList.Count; i++)
		{
			List<SerializedProperty> propertyList = groupedPropertyLists[propertyKeyList[i]];
			
			// If we have only one group for everything then we are just going
			// to draw this like normal (since there wouldn't be any real difference
			// other than having everything under a foldout).
			if (propertyKeyList.Count == 1)
			{
				foreach (SerializedProperty prop in propertyList)
				{
					EditorGUILayout.PropertyField(prop, true);
				}
			}
			else
			{
				string groupName = propertyKeyList[i];
				foldoutGroupStatusDictionary[groupName] = EditorGUILayout.Foldout(foldoutGroupStatusDictionary[groupName], groupName);
				if (foldoutGroupStatusDictionary[groupName])
				{
					EditorGUI.indentLevel++;
					foreach (SerializedProperty prop in propertyList)
					{
						EditorGUILayout.PropertyField(prop, true);
					}
					EditorGUI.indentLevel--;
				}
			}
		}
	}
	
	// Utility to get original texture asset width & height; must use undocumented unity API
	public static Vector2int getOriginalTextureSize(UnityEngine.Object textureAsset)
	{
		if (textureAsset is Texture2D)
		{
			string texturePath = AssetDatabase.GetAssetPath(textureAsset);
		
			if (!string.IsNullOrEmpty(texturePath))
			{
				return getOriginalTextureSize(TextureImporter.GetAtPath(texturePath) as TextureImporter);
			}
		}

		// Something failed, so return 0,0.
		return Vector2int.zero;
	}
	
	public static Vector2int getOriginalTextureSize(TextureImporter importer)
	{
		if (importer != null)
		{
			if (getWidthAndHeightDelegate == null)
			{
				var method = typeof(TextureImporter).GetMethod("GetWidthAndHeight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
				getWidthAndHeightDelegate = System.Delegate.CreateDelegate(typeof(GetWidthAndHeight), null, method) as GetWidthAndHeight;
			}
			int width = 0;
			int height = 0;
			getWidthAndHeightDelegate(importer, ref width, ref height);
			return new Vector2int(width, height);
		}
		
		// Something failed, so return 0,0.
		return Vector2int.zero;
	}

	private delegate void GetWidthAndHeight(TextureImporter importer, ref int width, ref int height);
	private static GetWidthAndHeight getWidthAndHeightDelegate;


	// A helper function to override a handful of textureimporter settings, similar to the old deprecated function
	// This will read, enable overrides, modify, and write back platform specific overrides to an importer
	public static void setTextureImporterOverrides(TextureImporter importer, string platform, int maxTextureSize, TextureImporterFormat textureFormat, int quality, bool allowsAlphaSplit)
	{
		// gets the existing platform settinga
		TextureImporterPlatformSettings settings = null;
		if (platform != "default")
		{
			settings = importer.GetPlatformTextureSettings(platform);
		}
		else
		{
			settings = importer.GetDefaultPlatformTextureSettings();
		}

		// set our desired overrides
		settings.overridden = true;
		settings.maxTextureSize = maxTextureSize;
		settings.format = textureFormat;
		settings.compressionQuality = quality;
		settings.allowsAlphaSplitting = allowsAlphaSplit;

		// Set them back to the importer
		importer.SetPlatformTextureSettings(settings);
	}


	// Converts a "BuildTarget" to a "Platform Name" that is appropriate for GetPlatformTextureSettings(...)
	public static string getPlatformNameFromBuildTarget(BuildTarget target)
	{
		switch (target)
		{
			case BuildTarget.Android:
				return "Android";
			case BuildTarget.iOS:
				return "iPhone";
			case BuildTarget.WSAPlayer:
				return "Windows Store Apps";
			case BuildTarget.WebGL:
				return "WebGL";
		}

		Debug.LogError("Unhandled target: " + target);
		return null;
	}


	// An improved implementation to return the immediate children of an asset.
	//
	// This differs from Unity's (poorly documented) API calls in a couple ways:
	//    AssetDatabase.GetDependencies - Returns orphaned & unused material texture refererences and component properties (don't want)
	//    EditorUtility.CollectDependencies - Removes unused properties, but returns material/component properties as siblings AND children (wrong)
	//
	// We can get the best results (proper hierarchy, recursible, no unused properties) by returning the intersection of both those API calls
	// This allows our AssetReports to more closely match actual bundled-contents and avoids false-positive asset inclusion.
	// 
	// How do texture refs get orphaned? You can assign a texture to a material, then switch to a shader without that named texture property,
	// and it the old texture will remain (hidden) in the material.
	public static string[] getDependenciesFixed(string assetPath)
	{
		// gets children dependencies, including dead/unused/orphaned references  :-(
		var paths1 = AssetDatabase.GetDependencies(assetPath, false);

		// gets children dependencies, but includes material & component properties as siblings (breaks recursion)
		var roots = new UnityEngine.Object[] { AssetDatabase.LoadMainAssetAtPath(assetPath) };
		var paths2 = EditorUtility.CollectDependencies( roots ).Select( obj => AssetDatabase.GetAssetPath(obj) );

		// The intersection of the two lists gives us our ideal asset set
		return paths1.Intersect(paths2).ToArray();
	}

	/// <summary>
	/// Menu option to allow for testing of the code that writes to our build Errors.Log file
	/// </summary>
	[MenuItem("Zynga/Test Log Non Fatal Error To Build Errors Log File")]
	private static void testLogNonFatalErrorToBuildErrorsLog()
	{
		logNonFatalErrorToBuildErrorsLog("Logging an error\n", "Logged error to build Errors.Log file.");
	}

	/// <summary>
	/// Setup a static string that contains the path to the build error logs file
	/// </summary>
	private static void initBuildErrorLogPath()
	{
		buildErrorsLogPath = System.Environment.GetEnvironmentVariable(BUILD_ERROR_LOG_ENV_VAR);
		if (string.IsNullOrEmpty(buildErrorsLogPath))
		{
			// If the environment variable isn't set, we'll use a hardcoded path
			buildErrorsLogPath = Path.Combine(Application.dataPath, DEFAULT_BUILD_ERROR_LOG_PATH);
			Debug.LogWarning("CommonEditor.initBuildErrorLogPath() - Unable to find Environment Variable: " + BUILD_ERROR_LOG_ENV_VAR + "; setting build error log path to default hardcoded path: " + buildErrorsLogPath);
		}
	}
	
	/// <summary>
	/// Output a string to our build Errors.Log file.
	/// Doing this will flag the build as non-fatally failed.
	/// </summary>
	/// <param name="fileLog">String that will be output to the file.</param>
	public static void logNonFatalErrorToBuildErrorsLog(string fileLog)
	{
		logNonFatalErrorToBuildErrorsLog(fileLog, fileLog);
	}

	/// <summary>
	/// Output a string to our build Errors.Log file.
	/// Doing this will flag the build as non-fatally failed.
	/// </summary>
	/// <param name="fileLog">String that will be output to the file.</param>
	/// <param name="consoleLog">String that will be output to the console in Unity.</param>
	public static void logNonFatalErrorToBuildErrorsLog(string fileLog, string consoleLog)
	{
		if (string.IsNullOrEmpty(buildErrorsLogPath))
		{
			initBuildErrorLogPath();
		}

		// Make sure we start the error message saying it is non-fatal and ends with a newline character
		// so that the file isn't made difficult to read
		fileLog = "NON-FATAL ERROR: " + fileLog + "\n";

		outputStringToFile(buildErrorsLogPath, fileLog, consoleLog, LogType.Error, ensureUniqueFilename: false, isUnityAssetPath: false);
	}
	
	/// <summary>
	/// Output fileLog to a file at filePath, will create directories if they don't exist and
	/// can make unique named files if a file with the same name already exists.  Can use
	/// consoleLog to add text to the log to identify what was output to the log in the Unity console.
	/// </summary>
	/// <param name="filePath">The path to the file we want to write to.</param>
	/// <param name="fileLog">String that will be output to the file.</param>
	/// <param name="consoleLog">String that will be output to the console in Unity.</param>
	/// <param name="logType">What LogType to use when outputting consoleLog.</param>
	/// <param name="ensureUniqueFilename">If true then the filepath will be modified to a new one if a file at the specified location already exists.</param>
	/// <param name="isUnityAssetPath">If true this will update Unity to make it aware of the new file and link the Unity console log to the newly created file.</param>
	public static void outputStringToFile(string filePath, string fileLog, string consoleLog, LogType logType, bool ensureUniqueFilename, bool isUnityAssetPath)
	{
		// Create the directory if it does not exist
		CommonFileSystem.createDirectoryIfNotExisting(filePath);

		if (ensureUniqueFilename)
		{
			// Make sure the filename is unique i.e. if filePath is writing to file "TextMeshPro_1_4_1_Convert_Results.txt" and that exists
			// it will try "TextMeshPro_1_4_1_Convert_Results 1.txt", if "TextMeshPro_1_4_1_Convert_Results 1.txt" exists it will
			// try "TextMeshPro_1_4_1_Convert_Results 2.txt" and so on.
			filePath = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(filePath);
		}

		System.IO.File.AppendAllText(filePath, fileLog);

		UnityEngine.Object context = null;

		if (isUnityAssetPath)
		{
			// Import the new asset so Unity is aware of it
			UnityEditor.AssetDatabase.ImportAsset(filePath);

			// Get the asset so the Console can highlight the output file in the project when you click the log message
			context = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath);
		}
		
		string finalConsoleLog = "";
		if (!string.IsNullOrEmpty(consoleLog))
		{
			finalConsoleLog += consoleLog + "\n";
		}
		finalConsoleLog += "File: " + filePath;

		if (context != null)
		{
			Debug.unityLogger.Log(logType:logType, message:finalConsoleLog, context:context);
		}
		else
		{
			Debug.unityLogger.Log(logType:logType, message:finalConsoleLog);
		}
	}
}
