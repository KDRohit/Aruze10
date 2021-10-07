using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;

public class DialogTypeEditor : EditorWindow {

	enum SKU { NONE = 0, HIR = 1 };

	private const string DATA_PATH = "/Data/{0}/Resources/Data/Dialog Types.txt";
	private const string PREFAB_PATH_ROOT = "Assets/Data/{0}/Resources/Prefabs/Dialogs/";
	private const string BUNDLE_PATH_ROOT = "Assets/Data/{0}/Bundles/Features/";
	private const string NEW_PREFIX = "new";

	private SKU sku = SKU.NONE;
	private string dataPath;
	private JSON fileData;
	private List<DialogDataType> dialogTypes;
	private DialogDataType expandedType = null;
	private string filterName = "";
	private Vector2 scroll = Vector2.zero;

	[MenuItem("Zynga/Wizards/Dialog Type Editor")]
	public static void ShowWindow()
	{
		EditorWindow.GetWindow<DialogTypeEditor>("DialogType Editor");
	}

	void OnGUI()
	{
		drawBasicMenu();
		drawFolder();
	}

	public void OnInspectorUpdate() 
	{
		this.Repaint();
	}

	void drawBasicMenu()
	{
		EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
		EditorGUIUtility.labelWidth = 120f;

		SKU oldSelected = sku;
		sku = (SKU)EditorGUILayout.EnumPopup(
			"SelectGameKey:",
			sku);
		
		if (sku != oldSelected)
		{
			if (sku == SKU.NONE && dialogTypes != null)
			{
				dialogTypes.Clear();
			}
		}

		if (GUILayout.Button("Load", EditorStyles.toolbarButton) && sku != SKU.NONE) 
		{
			ClearLog();
			loadData(sku);
			filterName = "";
		}

		filterName = EditorGUILayout.TextField("Name Filter", filterName);

		if (GUILayout.Button("New", EditorStyles.toolbarButton) && sku != SKU.NONE) 
		{
			if (dialogTypes == null || dialogTypes.Count < 1)
			{
				return;
			}

			if (dialogTypes.Find(x => x.keyName == NEW_PREFIX) != null)
			{
				Debug.LogError("Duplicated Keyname \"" + NEW_PREFIX + "\""); 
				return;
			}
			
			DialogDataType type = new DialogDataType { 
				keyName = NEW_PREFIX, 
				prefab = "", 
				smallDevicePrefab = "",
				isPurchaseDialog = false,
				isBundled = false
			};
			
			dialogTypes.Add(type);
			sortTypes();
			setExpanded(type);
		}
		if (GUILayout.Button("Save", EditorStyles.toolbarButton) && sku != SKU.NONE) 
		{
			for (int i = 0; i < dialogTypes.Count; i++)
			{
				if (!checkValidation(dialogTypes[i]))
				{
					Debug.LogError("Please check data. Processing About!");
					return;
				}
			}
			ClearLog();
			saveData(sku, jsonSerialize(dialogTypes));
		}

		EditorGUILayout.EndHorizontal();
	}

	void drawFolder()
	{
		if (dialogTypes == null || dialogTypes.Count < 1) 
		{
			return;
		}

		EditorGUILayout.BeginHorizontal();
		scroll = EditorGUILayout.BeginScrollView(scroll);

		for (int i = 0; i < dialogTypes.Count; i++)
		{
			DialogDataType type = dialogTypes[i];
			
			if (expandedType != null)
			{
				if (expandedType != type)
				{
					// If one is expanded, then only show the expanded one.
					continue;
				}
			}
			else
			{
				// If one isn't expanded, then apply the filter.
				if (!type.keyName.Contains(filterName))
				{
					continue;
				}
			}

			// Only one dialog type can be expanded at a time.
			type.isExpanded = EditorGUILayout.Foldout(type.isExpanded, type.keyName);
			
			if (type.isExpanded)
			{
				expandedType = type;
			}
			else if (type == expandedType)
			{
				// Collapsed the currently expanded type.
				expandedType = null;
			}
			
			if (type.isExpanded)
			{
				EditorGUIUtility.labelWidth = 120f;
				GUILayout.BeginHorizontal();
				{
					type.keyName = EditorGUILayout.TextField("KeyName", type.keyName.ToLower());
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				{
					EditorGUILayout.LabelField("Normal Prefab Path", type.prefab);
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				{
					GameObject go = (GameObject)EditorGUILayout.ObjectField("Normal Size Prefab", type.normalGameObject, typeof(GameObject), true);
					if (go != null)
					{
						ClearLog();
						
						type.prefab = getPrefabName(type, go);
					}
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				{
					EditorGUILayout.LabelField("Small Prefab Path", type.smallDevicePrefab);
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				{
					GameObject go = (GameObject)EditorGUILayout.ObjectField("Small Size Prefab", type.smallGameObject, typeof(GameObject), true);
					if (go != null)
					{
						type.smallDevicePrefab = getPrefabName(type, go);
					}
				}
				GUILayout.EndHorizontal();

				// add animation section
				if (type.animation != null)
				{
					GUILayout.BeginHorizontal();
					type.animation.inTime = float.Parse(EditorGUILayout.TextField("Animation In Time: ", type.animation.inTime.ToString()));
					type.animation.outTime = float.Parse(EditorGUILayout.TextField("Animation Out Time: ", type.animation.outTime.ToString()));
					type.animation.animInEase = (Dialog.AnimEase)System.Enum.Parse(typeof(Dialog.AnimEase), EditorGUILayout.TextField("Animation In Ease: ", type.animation.animInEase.ToString().ToUpper()));
					type.animation.animOutEase = (Dialog.AnimEase)System.Enum.Parse(typeof(Dialog.AnimEase), EditorGUILayout.TextField("Animation Out Ease: ", type.animation.animOutEase.ToString().ToUpper()));
					type.animation.animInPos = (Dialog.AnimPos)System.Enum.Parse(typeof(Dialog.AnimPos), EditorGUILayout.TextField("Animation In Position: ", type.animation.animInPos.ToString().ToUpper()));
					type.animation.animOutPos = (Dialog.AnimPos)System.Enum.Parse(typeof(Dialog.AnimPos), EditorGUILayout.TextField("Animation Out Position: ", type.animation.animOutPos.ToString().ToUpper()));
					type.animation.animInScale = (Dialog.AnimScale)System.Enum.Parse(typeof(Dialog.AnimScale), EditorGUILayout.TextField("Animation In Scale: ", type.animation.animInScale.ToString().ToUpper()));
					type.animation.animOutScale = (Dialog.AnimScale)System.Enum.Parse(typeof(Dialog.AnimScale), EditorGUILayout.TextField("Animation Out Scale: ", type.animation.animOutScale.ToString().ToUpper()));
					GUILayout.EndHorizontal();
				}
				else
				{
					type.animation = new AnimationType();
				}
				
				GUILayout.BeginHorizontal();
				{
					EditorGUIUtility.labelWidth = 100f;
					GUILayout.BeginVertical();
					{
						type.isPurchaseDialog = EditorGUILayout.Toggle("Purchase Dialog", type.isPurchaseDialog);
					}
					GUILayout.EndVertical();

					GUILayout.BeginVertical();
					{
						type.isBundled = EditorGUILayout.Toggle("Bundle Dialog", type.isBundled);
					}
					GUILayout.EndVertical();

					GUILayout.BeginVertical();
					{
						if (GUILayout.Button("Done", EditorStyles.miniButton))
						{
							if (checkValidation(type))
							{
								ClearLog();
								sortTypes();
								expandedType = null;
								type.isExpanded = false;
							}
						}
					}
					GUILayout.EndVertical();

					GUILayout.BeginVertical();
					{
						if (GUILayout.Button("Delete it", EditorStyles.miniButton))
						{
							removeData(i);
							expandedType = null;
						}
					}
					GUILayout.EndVertical();
				}
				GUILayout.EndHorizontal();
			}
		}
		EditorGUILayout.EndScrollView();
		EditorGUILayout.EndHorizontal();
	}
	
	// Gets the path and name of the prefab that's linked in the inspector.
	private string getPrefabName(DialogDataType type, GameObject prefab)
	{
		if (prefab != null)
		{
			string fullPath = AssetDatabase.GetAssetPath(prefab);
			string folderName = Path.GetDirectoryName(fullPath);
			string fileName = Path.GetFileNameWithoutExtension(fullPath);
			if (!Regex.IsMatch(folderName, sku.ToString()) ||
				!(Regex.IsMatch(folderName, string.Format(BUNDLE_PATH_ROOT, sku)) ||
				  Regex.IsMatch(folderName, string.Format(PREFAB_PATH_ROOT, sku))))
			{
				Debug.LogError("You must assign prefab on correct folder.");
				return "";
			}

			if (Regex.IsMatch(folderName, string.Format(BUNDLE_PATH_ROOT, sku)))
			{
				type.isBundled = true;
				return folderName.Replace(string.Format(BUNDLE_PATH_ROOT, sku), "") + "/" + fileName;
			}
			else
			{
				type.isBundled = false;
				return folderName.Replace(string.Format(PREFAB_PATH_ROOT, sku), "") + "/" + fileName;
			}
		}
		return "";
	}

	void loadData(SKU gameKey)
	{
		dataPath = Application.dataPath + string.Format(DATA_PATH, gameKey);
		if (!File.Exists(dataPath)) 
		{
			Debug.LogError("File not found!");
			return;
		}
		fileData = new JSON(File.ReadAllText(dataPath));
		JSON[] data = fileData.getJsonArray("dialog_types");
		dialogTypes = new List<DialogDataType>();
		foreach (JSON json in data) 
		{
			DialogDataType d = new DialogDataType();
			d.keyName = json.getString("key_name", "");
			d.prefab = json.getString("prefab", "");
			d.smallDevicePrefab = json.getString("small_device_prefab", "");
			d.isPurchaseDialog = json.getBool("is_purchase_dialog", false);
			d.isBundled = json.getBool("is_bundled", false);
			JSON animationData = json.getJSON("animation");
			if (animationData != null) 
			{
				AnimationType at = new AnimationType();
				at.inTime = animationData.getFloat("in_time", 0.25f);
				at.outTime = animationData.getFloat("out_time", 0.25f);
				at.animInPos = (Dialog.AnimPos)animationData.getInt("in_position", 1);
				at.animInScale = (Dialog.AnimScale)animationData.getInt("in_scale", 1);
				at.animInEase = (Dialog.AnimEase)animationData.getInt("in_ease", 0);
				at.animOutPos = (Dialog.AnimPos)animationData.getInt("out_position", 2);
				at.animOutScale = (Dialog.AnimScale)animationData.getInt("out_scale", 2);
				at.animOutEase = (Dialog.AnimEase)animationData.getInt("out_ease", 0);
				d.animation = at;
			}
			dialogTypes.Add(d);
		}
		sortTypes();
	}

	void saveData(SKU gameKey, string data)
	{
		dataPath = Application.dataPath + string.Format(DATA_PATH, gameKey);
		File.WriteAllText(dataPath, data);
	}

	void removeData(int index)
	{
		dialogTypes.RemoveAt(index);
		sortTypes();
	}

	void sortTypes()
	{
		dialogTypes.Sort(delegate(DialogDataType x, DialogDataType y)
			{
				return x.prefab.CompareTo(y.prefab);
			}
		);
	}

	bool checkValidation(DialogDataType type)
	{
		type.keyName = Regex.Replace(type.keyName, @"\s+", "");

		if (dialogTypes.FindAll(x => x.keyName == type.keyName).Count > 1)
		{
			Debug.LogError("Duplicated Key name : " + type.keyName);
			setExpanded(type);
			return false;
		}
		if (type.keyName.Length < 1)
		{
			Debug.LogError("Key name value is empty. Please input key name.");
			setExpanded(type);
			return false;
		}
		/*
		Example correct Key pattern : "abc", "abc_def", "abc_def1"
		 */
		if (!(Regex.IsMatch(type.keyName, "^[a-z]") && Regex.IsMatch(type.keyName, "(_)?[a-z0-9]+$"))) 
		{
			Debug.LogError("Key name is invalid : " + type.keyName);
			setExpanded(type);
			return false;
		}

		if (type.prefab.Length < 1)
		{
			Debug.LogError("Normal dialog path value is empty. Please assign it.");
			setExpanded(type);
			return false;
		}
		return true;
	}

	// Sets a certain dialog type to be expanded in the UI, and all others are collapsed.
	void setExpanded(DialogDataType type)
	{
		for (int i = 0; i < dialogTypes.Count; i++)
		{
			dialogTypes[i].isExpanded = false;
		}
		type.isExpanded = true;
		expandedType = type;
	}

	string jsonSerialize(List<DialogDataType> list)
	{
		StringBuilder jsonForm = new StringBuilder();
		jsonForm.AppendLine("{");
		jsonForm.AppendLine("\t\"dialog_types\" : [");
		for (int i = 0; i < list.Count; i++)
		{
			jsonForm.AppendLine("\t\t{");
			JSON.buildJsonStringLine(jsonForm, 3, "key_name",				list[i].keyName, true);
			JSON.buildJsonStringLine(jsonForm, 3, "prefab",					list[i].prefab, true);
			JSON.buildJsonStringLine(jsonForm, 3, "small_device_prefab",	list[i].smallDevicePrefab, true);
			JSON.buildJsonStringLine(jsonForm, 3, "is_purchase_dialog",		list[i].isPurchaseDialog, true);
			JSON.buildJsonStringLine(jsonForm, 3, "is_bundled",				list[i].isBundled, list[i].animation != null);

			if (list[i].animation != null)
			{
				jsonForm.AppendLine("\t\t\t\"animation\" :\n\t\t\t{");
				JSON.buildJsonStringLine(jsonForm, 4, "in_time",		list[i].animation.inTime, true);
				JSON.buildJsonStringLine(jsonForm, 4, "out_time",		list[i].animation.outTime, true);
				JSON.buildJsonStringLine(jsonForm, 4, "in_position",	(int)list[i].animation.animInPos, true);
				JSON.buildJsonStringLine(jsonForm, 4, "in_scale",		(int)list[i].animation.animInScale, true);
				JSON.buildJsonStringLine(jsonForm, 4, "in_ease",		(int)list[i].animation.animInEase, true);
				JSON.buildJsonStringLine(jsonForm, 4, "out_position",	(int)list[i].animation.animOutPos, true);
				JSON.buildJsonStringLine(jsonForm, 4, "out_scale",		(int)list[i].animation.animOutScale, true);
				JSON.buildJsonStringLine(jsonForm, 4, "out_ease",		(int)list[i].animation.animOutEase, false);
				jsonForm.AppendLine("\t\t\t}");
			}
			else
			{
				jsonForm.AppendLine("");
			}

			if (i < list.Count - 1) 
			{
				jsonForm.AppendLine("\t\t},");
			} 
			else 
			{
				jsonForm.AppendLine("\t\t}");
			}
		}
		jsonForm.Append("\t]"+ System.Environment.NewLine + "}");
		return jsonForm.ToString();
	}

	public static void ClearLog()
	{
		/*var assembly = Assembly.GetAssembly(typeof(UnityEditor.ActiveEditorTracker));
		var type = assembly.GetType("UnityEditorInternal.LogEntries");
		var method = type.GetMethod("Clear");
		method.Invoke(new object(), null);*/
	}
}

public class DialogDataType
{
	public bool isExpanded;
	public string keyName;
	public string prefab;
	public GameObject normalGameObject;
	public string smallDevicePrefab;
	public GameObject smallGameObject;
	public bool isPurchaseDialog;
	public bool isBundled;
	public AnimationType animation;
	public DialogDataType() 
	{
	}
}

public class AnimationType
{
	public float inTime = 0.25f;
	public float outTime = 0.25f;
	public Dialog.AnimPos animInPos = Dialog.AnimPos.TOP;
	public Dialog.AnimPos animOutPos = Dialog.AnimPos.BOTTOM;	
	public Dialog.AnimScale animInScale = Dialog.AnimScale.FULL;
	public Dialog.AnimScale animOutScale = Dialog.AnimScale.FULL;
	public Dialog.AnimEase animInEase = Dialog.AnimEase.BACK;
	public Dialog.AnimEase animOutEase = Dialog.AnimEase.BACK;
}
