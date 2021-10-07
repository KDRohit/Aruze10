using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using Object = UnityEngine.Object;

[ExecuteInEditMode]
public class PropertyFinder : EditorWindow
{
	private const string propertyFindFieldName = "PROPERTY_FINDER_FIELD";
	private const string objectReferenceFindFieldName = "PROPERTY_FINDER_OBJ_FIELD";

	private class PropertyDisplay
	{
		public Type Type { get; set; }
		public SerializedObject SerilizedObj { get; set; }
		public List<Object> ObjectList { get; set; }
		public HashSet<string> PropertiesPathsHash { get; set; }
		public List<PropertyDisplay> ChildPropertyDisplayList { get; set; }
		public Editor CustomEditor { get; set; }
		public int HeaderButtonID { get { return InstanceID * 99999; } }
		public bool HasAppliableChanges { get; set; }

		public PropertyDisplay()
		{
			ObjectList = new List<Object>();
			PropertiesPathsHash = new HashSet<string>();
			ChildPropertyDisplayList = new List<PropertyDisplay>();
		}

		public int InstanceID
		{
			get { return ObjectList[0].GetInstanceID() * 99999; }
		}

		public bool SerializedObjectNeedsUpdate()
		{
			if (SerilizedObj.targetObjects.Length > 0)
			{
				for (int i = 0; i < SerilizedObj.targetObjects.Length; ++i)
				{
					if (SerilizedObj.targetObjects[i] == null)
					{
						return true;
					}
				}
			}
			return SerilizedObj.targetObject != null;
		}
	}

	private bool utilityWindowStyle { get; set; }
	private List<PropertyDisplay> propertyDisplayList = new List<PropertyDisplay>();
	private bool currentlySearching;
	private double searchStartTime;
	private double searchDT;

	private Vector2 currentScrollPosition = Vector2.zero;

	private bool needSearchBoxFocusUpdate = false;
	private string searchQueryPrevious = string.Empty;
	private string searchQuery = string.Empty;

	private bool allRowsExpanded;
	private bool currentSelectionLocked;
	private Dictionary<int, bool> headerExpandedDictionary = new Dictionary<int, bool>();

	private SearchType searchType;
	private enum SearchType
	{
		FieldValue,
		FieldType,
		FieldNameContains,
		FieldNameStartsWith,
		FieldNameEndsWith,
		FieldNameIs,
	}

	private List<Object> lockedObjects = new List<Object>();
	public Object[] objectsSelected
	{
		get
		{
			if (currentSelectionLocked)
			{
				lockedObjects.RemoveAll(item => item == null);
				return lockedObjects.ToArray();
			}
			return Selection.objects;
		}
	}

	[MenuItem("Zynga/Editor Tools/Property Finder")]
	private static void Init()
	{
		PropertyFinder window = CreateInstance<PropertyFinder>();
		window.utilityWindowStyle = true;
		SetupInfo(window);
		window.ShowUtility();
	}

	static void SetupInfo(PropertyFinder propertyFinderWindow)
	{
		propertyFinderWindow.needSearchBoxFocusUpdate = true;
		propertyFinderWindow.wantsMouseMove = true;
		propertyFinderWindow.autoRepaintOnSceneChange = true;
		propertyFinderWindow.UpdatePropertiesView();
	}

	private void DrawSearchField()
	{
		GUI.SetNextControlName(propertyFindFieldName);

		GUILayout.BeginVertical("In BigTitle");
		{
			EditorGUILayout.BeginHorizontal();
			{
				searchType = (SearchType)EditorGUILayout.EnumPopup("Search Criteria", searchType, GUILayout.MaxWidth(800));
				GUILayout.FlexibleSpace();

				if (GUILayout.Toggle(currentSelectionLocked, new GUIContent("", "Lock selection independent of hierarchy"), "IN LockButton") != currentSelectionLocked)
				{
					currentSelectionLocked = !currentSelectionLocked;
					lockedObjects = Selection.objects.ToList();
					UpdatePropertiesView();
				}
				GUILayout.Space(4);
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			{
				GUI.SetNextControlName(propertyFindFieldName);

				string search = EditorGUILayout.TextField(searchQuery, (GUIStyle)"ToolbarSeachTextField", GUILayout.Width(position.width - 25));
				if (search != searchQuery)
				{
					searchDT = EditorApplication.timeSinceStartup + 0.333;
					searchQuery = search;
				}
				if (GUILayout.Button(GUIContent.none, string.IsNullOrEmpty(searchQuery) ? "ToolbarSeachCancelButtonEmpty" : "ToolbarSeachCancelButton"))
				{
					searchQuery = string.Empty;
					GUIUtility.keyboardControl = 0;
				}

			}
			EditorGUILayout.EndHorizontal();
			GUILayout.Space(3);

			if (GUILayout.Button(allRowsExpanded ? "\u25BA" : "\u25BC", GUILayout.MaxWidth(25)))
			{
				for (int i = 0; i < propertyDisplayList.Count; ++i)
				{
					SetHeaderExpandedState(propertyDisplayList[i].InstanceID, allRowsExpanded);
				}
				allRowsExpanded = !allRowsExpanded;
			}
			GUILayout.Space(2);
		}
		GUILayout.EndVertical();

		UpdatePropertiesView();
	}

	void Update()
	{
		if (currentlySearching)
		{
			return;
		}

		if (searchQueryPrevious != searchQuery)
		{
			if (EditorApplication.timeSinceStartup >= searchDT || string.IsNullOrEmpty(searchQuery))
			{
				UpdatePropertiesView();
				searchQueryPrevious = searchQuery;
			}
		}
	}

	void OnSelectionChange()
	{
		CheckForPrefabChangesAll();
		UpdatePropertiesView();

		DoHistoryOnSelectionChange();
		Repaint();
	}

	void OnInspectorUpdate()
	{
		Repaint();
	}

	void OnFocus()
	{
		UpdatePropertiesView();
		Repaint();
		needSearchBoxFocusUpdate = true;
	}

	private void OnGUI()
	{
		DrawSearchField();

		if (needSearchBoxFocusUpdate)
		{
			EditorGUI.FocusTextInControl(propertyFindFieldName);
			needSearchBoxFocusUpdate = false;
		}

		for (int i = propertyDisplayList.Count - 1; i >= 0; --i)
		{
			if (propertyDisplayList[i].SerializedObjectNeedsUpdate())
			{
				UpdatePropertiesView();
				break;
			}
			UpdateProperties(propertyDisplayList[i]);
		}

		EditorGUILayout.BeginVertical();
		{
			currentScrollPosition = EditorGUILayout.BeginScrollView(currentScrollPosition, GUILayout.Width(position.width), GUILayout.Height(position.height * 0.6f));
			for (int i = 0; i < propertyDisplayList.Count; ++i)
			{
				if (DrawObjectHeader(propertyDisplayList[i]))
				{
					EditorGUILayout.BeginHorizontal("textField", GUILayout.MinHeight(10f));
					GUILayout.BeginVertical();

					GUILayout.Space(3);
					DrawObject(propertyDisplayList[i]);

					for (int j = 0; j < propertyDisplayList[i].ChildPropertyDisplayList.Count; ++j)
					{
						if (DrawObjectHeader(propertyDisplayList[i].ChildPropertyDisplayList[j], true))
						{
							EditorGUILayout.BeginHorizontal("textField", GUILayout.MinHeight(10f));
							GUILayout.BeginVertical();

							GUILayout.Space(3);

							DrawObject(propertyDisplayList[i].ChildPropertyDisplayList[j]);

							GUILayout.Space(3);

							GUILayout.EndVertical();
							EditorGUILayout.EndHorizontal();

							if (StopDrawingOffscreenProperties())
							{
								break;
							}
						}
					}
					GUILayout.Space(3);
					GUILayout.EndVertical();
					EditorGUILayout.EndHorizontal();
					GUILayout.Space(3);

					if (StopDrawingOffscreenProperties())
					{
						break;
					}
				}
			}
			EditorGUILayout.EndScrollView();
		}
		EditorGUILayout.EndVertical();

		GUILayout.Space(6);
		DrawHistory();
	}

	private bool DrawObjectHeader(PropertyDisplay property, bool useTypeAsName = false)
	{
		string typeName = property.Type != null ? property.Type.Name : property.ObjectList[0].GetType().Name;
		string name = useTypeAsName ? property.ObjectList[0].GetType().Name : property.ObjectList[0].name;

		Action buttonClickCallback = null;
		buttonClickCallback = GetSelectObjectsAction(property);

		bool expanded = GetHeaderIsExpanded(property.InstanceID);

		GUILayout.Space(3);
		if (!expanded)
		{
			GUI.backgroundColor = Color.gray;
		}

		GUILayout.BeginHorizontal();
		{
			expanded = EditorGUILayout.Foldout(expanded, name, true, "FoldoutPreDrop");
			GUILayout.BeginHorizontal();
			{
				if (property.HasAppliableChanges)
				{
					GUILayout.Space(250);
					GUILayout.Label("Prefab", GUILayout.ExpandWidth(false));
					if (GUILayout.Button(new GUIContent("Apply", tooltip: "Apply any changes to prefab"), "miniButtonLeft", GUILayout.Width(40)))
					{
						ApplyPrefabChanges(property);
					}

					if (GUILayout.Button(new GUIContent("Revert", tooltip: "Revert any changes to prefab"), "miniButtonRight", GUILayout.Width(50)))
					{
						RevertPrefabChanges(property);
					}
				}
			}
			GUILayout.EndHorizontal();

			GUIContent buttonContent = new GUIContent(EditorGUIUtility.Load("icons/" + (EditorGUIUtility.isProSkin ? "d_" : "") + "winbtn_win_rest.png") as Texture2D, "Select Component View");
			if (buttonClickCallback != null && GUILayout.Button(buttonContent, "miniButton", GUILayout.Width(25), GUILayout.MaxHeight(15)))
			{
				buttonClickCallback();
			}

			SetHeaderExpandedState(property.InstanceID, expanded);
			GUILayout.Space(2);
		}
		GUILayout.EndHorizontal();

		if (!expanded)
		{
			GUILayout.Space(3);
		}
		GUI.backgroundColor = Color.white;

		return expanded;
	}

	private void DrawObject(PropertyDisplay property)
	{
		SerializedObject serializedObjectChild = property.SerilizedObj;
		EditorGUI.BeginChangeCheck();

		if (string.IsNullOrEmpty(searchQuery))
		{
			float originalLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = position.width / 4;

			if (property.CustomEditor == null)
			{
				property.CustomEditor.OnInspectorGUI();
			}
			else
			{
				property.CustomEditor = Editor.CreateEditor(property.SerilizedObj.targetObject);
				property.CustomEditor.DrawDefaultInspector();
			}
			EditorGUIUtility.labelWidth = originalLabelWidth;
		}
		else
		{
			foreach (var serializedProperty in property.PropertiesPathsHash)
			{
				SerializedProperty childProp = serializedObjectChild.FindProperty(serializedProperty);
				if (childProp == null)
				{
					continue;
				}
				EditorGUILayout.PropertyField(childProp, true);
			}
		}

		if (EditorGUI.EndChangeCheck())
		{
			serializedObjectChild.ApplyModifiedProperties();
		}
	}

	private void UpdatePropertiesView()
	{
		propertyDisplayList.Clear();

		if (string.IsNullOrEmpty(searchQuery))
		{
			BasicInspectorsView();
		}
		else
		{
			SelectedInspectorsView();
		}

		CheckForPrefabChangesAll();
	}

	private void BasicInspectorsView()
	{
		currentlySearching = true;

		for (int i = objectsSelected.Length - 1; i >= 0; --i)
		{
			PropertyDisplay propDisplay = new PropertyDisplay();
			propDisplay.ObjectList.Add(objectsSelected[i]);
			propDisplay.SerilizedObj = new SerializedObject(objectsSelected[i]);
			propertyDisplayList.Add(propDisplay);

			GameObject currentGameObject = objectsSelected[i] as GameObject;
			if (currentGameObject != null)
			{
				Component[] components = currentGameObject.GetComponents<Component>();

				for (int currentComponentIndex = 0; currentComponentIndex < components.Length; ++currentComponentIndex)
				{
					PropertyDisplay childPropertyDisplay = new PropertyDisplay();
					childPropertyDisplay.ObjectList.Add(components[currentComponentIndex]);
					childPropertyDisplay.SerilizedObj = new SerializedObject(components[currentComponentIndex]);
					propDisplay.ChildPropertyDisplayList.Add(childPropertyDisplay);
				}
			}
		}

		for (int i = 0; i < propertyDisplayList.Count; ++i)
		{
			CreateCustomEditorForProperty(propertyDisplayList[i]);
		}

		currentlySearching = false;
	}

	private void SelectedInspectorsView()
	{
		searchStartTime = EditorApplication.timeSinceStartup;
		currentlySearching = true;

		for (int i = objectsSelected.Length - 1; i >= 0; --i)
		{
			searchQuery = (searchType != SearchType.FieldNameContains) ? searchQuery.ToLower() : searchQuery;

			SerializedObject serializedObject = new SerializedObject(objectsSelected[i]);
			SerializedProperty iterator = serializedObject.GetIterator();

			SerializedObject childSerializedObject = new SerializedObject(objectsSelected[i]);
			SerializedProperty childIterator = childSerializedObject.GetIterator();

			PropertyDisplay displayObj = new PropertyDisplay();
			displayObj.ObjectList.Add(objectsSelected[i]);
			displayObj.SerilizedObj = childSerializedObject;

			ScopeToProperties(null, displayObj, serializedObject, iterator, searchQuery);

			if (UpdateProgressBarDisplay(i / objectsSelected.Length))
			{
				break;
			}

			GameObject currentGameObject = objectsSelected[i] as GameObject;
			if (currentGameObject != null)
			{
				Component[] components = currentGameObject.GetComponents<Component>();

				for (int componentIndex = 0; componentIndex < components.Length; ++componentIndex)
				{
					ScopeToObject(displayObj, components[componentIndex], searchQuery);
				}
			}

			propertyDisplayList.Add(displayObj);

			if (UpdateProgressBarDisplay(i / objectsSelected.Length))
			{
				break;
			}
		}

		currentlySearching = false;

		EditorUtility.ClearProgressBar();
	}

	private void CreatePropertyDisplayListFromDict(Dictionary<Type, PropertyDisplay> propertyDictionary)
	{
		foreach (var prop in propertyDictionary)
		{
			PropertyDisplay newProp = new PropertyDisplay()
			{
				Type = prop.Key,
				SerilizedObj = new SerializedObject(prop.Value.ObjectList.ToArray()),
				ObjectList = prop.Value.ObjectList,

				PropertiesPathsHash = prop.Value.PropertiesPathsHash,
			};

			foreach (var propertiesPath in newProp.PropertiesPathsHash)
			{
				newProp.PropertiesPathsHash.Add(propertiesPath);
			}

			propertyDisplayList.Add(newProp);
		}
	}


	private PropertyDisplay ScopeToObject(PropertyDisplay parent, Object obj, string search)
	{
		SerializedObject serializedChild = new SerializedObject(obj);
		SerializedProperty iter = serializedChild.GetIterator();

		PropertyDisplay propDisplay = new PropertyDisplay();
		propDisplay.SerilizedObj = serializedChild;
		propDisplay.ObjectList.Add(obj);

		ScopeToProperties(parent, propDisplay, serializedChild, iter, search);

		return propDisplay;
	}

	private void ScopeToProperties(PropertyDisplay parent, PropertyDisplay child, SerializedObject serializedObject, SerializedProperty serializedProp, string search)
	{
		bool addingChild = false;

		serializedProp.Reset();
		while (serializedProp.NextVisible(true))
		{
			if (child.PropertiesPathsHash.Contains(serializedProp.propertyPath))
			{
				continue;
			}

			SerializedProperty property;
			if (IsPropertyMatch(serializedProp, search))
			{
				property = serializedObject.FindProperty(serializedProp.propertyPath);
				if (property == null)
				{
					continue;
				}

				addingChild = true;
				child.PropertiesPathsHash.Add(property.propertyPath);
			}
		}

		if (addingChild && parent != null)
		{
			parent.ChildPropertyDisplayList.Add(child);
		}
	}

	private void AddObjectsAndProperties(Dictionary<Type, PropertyDisplay> propertyDisplayDict, PropertyDisplay prop, Object currentObject, bool ignorePaths = false)
	{
		if (!ignorePaths && prop.PropertiesPathsHash.Count == 0 && prop.ChildPropertyDisplayList.Count == 0)
		{
			return;
		}

		Type currentType = currentObject.GetType();
		PropertyDisplay propertyDisplay = propertyDisplayDict.ContainsKey(currentType) ? propertyDisplayDict[currentType] : propertyDisplayDict[currentObject.GetType()] = (propertyDisplay = new PropertyDisplay());
		propertyDisplay.ObjectList.Add(currentObject);

		if (ignorePaths)
		{
			return;
		}

		foreach (var propertiesPath in prop.PropertiesPathsHash)
		{
			propertyDisplay.PropertiesPathsHash.Add(propertiesPath);
		}
	}

	private void CheckForPrefabChangesAll()
	{
		for (int i = 0; i < propertyDisplayList.Count; ++i)
		{
			CheckForPrefabChanges(propertyDisplayList[i]);
		}
	}

	bool CheckForPrefabChanges(PropertyDisplay property)
	{
		property.HasAppliableChanges = false;

		if (string.IsNullOrEmpty(searchQuery))
		{
			SerializedProperty iterator = property.SerilizedObj.GetIterator();
			while (iterator.Next(true))
			{
				property.HasAppliableChanges = iterator.isInstantiatedPrefab && iterator.prefabOverride;

				if (property.HasAppliableChanges)
				{
					break;
				}
			}
		}
		else
		{
			foreach (var propertiesPath in property.PropertiesPathsHash)
			{
				SerializedProperty currentProperty = property.SerilizedObj.FindProperty(propertiesPath);
				if (currentProperty == null)
				{
					continue;
				}

				property.HasAppliableChanges = currentProperty.isInstantiatedPrefab && currentProperty.prefabOverride;

				if (property.HasAppliableChanges)
				{
					break;
				}
			}
		}

		bool childrenPrefabChanges = false;
		for (int i = 0; i < property.ChildPropertyDisplayList.Count; ++i)
		{
			childrenPrefabChanges |= CheckForPrefabChanges(property.ChildPropertyDisplayList[i]);
		}

		property.HasAppliableChanges |= childrenPrefabChanges;

		return property.HasAppliableChanges;
	}

	void DoPrefabChanges(PropertyDisplay property, bool apply)
	{
		List<Object> objects = new List<Object>();

		objects.AddRange(property.ObjectList);

		for (int i = 0; i < objects.Count; ++i)
		{
			GameObject obj = objects[i] as GameObject;
			if (obj == null)
			{
				Component component = objects[i] as Component;
				if (component != null)
				{
					obj = component.gameObject;
				}

				if (obj == null)
				{
					continue;
				}
			}

			// TODO:UNITY2018:nestedprefabs:confirm//old
			// GameObject rootWithCommonParent = PrefabUtility.FindRootGameObjectWithSameParentPrefab(obj);
			// Object prefabParent = PrefabUtility.GetPrefabParent(rootWithCommonParent);
			// TODO:UNITY2018:nestedprefabs:confirm//new
			GameObject rootWithCommonParent = PrefabUtility.GetOutermostPrefabInstanceRoot(obj);
			Object prefabParent = PrefabUtility.GetCorrespondingObjectFromSource(rootWithCommonParent);

			if (prefabParent == null)
			{
				return;
			}

			if (apply)
			{
				// TODO:UNITY2018:nestedprefabs:confirm//old
				//PrefabUtility.ReplacePrefab(rootWithCommonParent, prefabParent, ReplacePrefabOptions.ConnectToPrefab);
				// TODO:UNITY2018:nestedprefabs:confirm//new
				PrefabUtility.SaveAsPrefabAssetAndConnect(rootWithCommonParent, AssetDatabase.GetAssetPath(prefabParent), InteractionMode.AutomatedAction);
			}
			else
			{
				PrefabUtility.RevertPrefabInstance(rootWithCommonParent, InteractionMode.AutomatedAction);
			}
		}
		property.HasAppliableChanges = false;
		GUIUtility.keyboardControl = 0;
	}

	void ApplyPrefabChanges(PropertyDisplay property)
	{
		DoPrefabChanges(property, true);
	}

	void RevertPrefabChanges(PropertyDisplay property)
	{
		DoPrefabChanges(property, false);
	}

	public bool IsPropertyMatch(SerializedProperty property, string search)
	{
		if (string.IsNullOrEmpty(searchQuery))
		{
			return true;
		}
		search = search.Trim(); //remove trailing and leading whitespace

		string propertyName = property.name;
		if (SearchType.FieldNameContains == searchType)
		{
			propertyName = propertyName.ToLower();
		}

		//sanitize some known property names
		if (propertyName.StartsWith("_"))
		{
			propertyName = propertyName.Remove(0, 1);
		}
		if (propertyName.StartsWith("m_"))
		{
			propertyName = propertyName.Remove(0, 2);
		}

		string[] parts = new[] { search };
		if (search.Contains(' '))
		{
			parts = search.Split(' ');
		}

		bool contains = true;
		for (int i = 0; i < parts.Length; ++i)
		{
			string part = parts[i];
			if (string.IsNullOrEmpty(part))
			{
				contains = false;
			}
			else
			{
				switch (searchType)
				{
					case SearchType.FieldValue:
						contains &= IsPropertyMatchForValue(property, part);
						break;
					case SearchType.FieldType:
						{
							string typeName = property.type;
							switch (property.propertyType)
							{
								case SerializedPropertyType.ObjectReference:
									if (property.objectReferenceValue != null)
									{
										typeName = property.objectReferenceValue.GetType().Name;
									}
									break;
							}
							contains &= typeName.Equals(part, StringComparison.OrdinalIgnoreCase);
						}
						break;
					case SearchType.FieldNameContains:
						contains &= propertyName.Contains(part);
						break;
					case SearchType.FieldNameStartsWith:
						contains &= propertyName.StartsWith(part, StringComparison.OrdinalIgnoreCase);
						break;
					case SearchType.FieldNameEndsWith:
						contains &= propertyName.EndsWith(part, StringComparison.OrdinalIgnoreCase);
						break;
					case SearchType.FieldNameIs:
						contains &= propertyName.Equals(part, StringComparison.OrdinalIgnoreCase);
						break;
				}
			}
		}
		return contains;
	}

	private bool IsPropertyMatchForValue(SerializedProperty property, string comparator)
	{
		string stringValue = string.Empty;
		switch (property.propertyType)
		{
			case SerializedPropertyType.Integer:
				int intValue;
				if (int.TryParse(comparator, out intValue))
				{
					if (property.intValue == intValue)
					{
						return true;
					}
				}
				break;
			case SerializedPropertyType.Boolean:
				bool boolValue;
				if (bool.TryParse(comparator, out boolValue))
				{
					return boolValue == property.boolValue;
				}
				break;
			case SerializedPropertyType.Float:
				float floatValue;
				if (float.TryParse(comparator, out floatValue))
				{
					return Mathf.Approximately(property.floatValue, floatValue);
				}
				break;
			case SerializedPropertyType.Generic:
				return false;
			case SerializedPropertyType.LayerMask:
				return false;
			case SerializedPropertyType.Enum:
				{
					int enumInt;
					int.TryParse(comparator, out enumInt);
					return property.enumValueIndex == enumInt;
				}
			case SerializedPropertyType.Character:
				return false;
			case SerializedPropertyType.Gradient:
				return false;
			case SerializedPropertyType.Quaternion:
				return false;
			case SerializedPropertyType.String:
				stringValue = property.stringValue;
				break;
			case SerializedPropertyType.Color:
				stringValue = property.colorValue.ToString();
				break;
			case SerializedPropertyType.ObjectReference:
				if (property.objectReferenceValue != null)
				{
					stringValue = property.objectReferenceValue.ToString();
					//return stringValue;

					//Material m = property.objectReferenceValue as UnityEngine.Material;
					//if(m != null)
					//{
					//	stringValue = m.name;
					//}
				}
				break;
			case SerializedPropertyType.Vector2:
				stringValue = property.vector2Value.ToString();
				break;
			case SerializedPropertyType.Vector3:
				stringValue = property.vector3Value.ToString();
				break;
			case SerializedPropertyType.Vector4:
				stringValue = property.vector4Value.ToString();
				break;
			case SerializedPropertyType.Rect:
				stringValue = property.rectValue.ToString();
				break;
			case SerializedPropertyType.ArraySize:
				if (property.isArray)
				{
					stringValue = property.arraySize.ToString();
				}
				break;
			case SerializedPropertyType.AnimationCurve:
				stringValue = property.animationCurveValue.ToString();
				break;
			case SerializedPropertyType.Bounds:
				stringValue = property.boundsValue.ToString();
				break;
		}

		return stringValue.ToLower().Trim().Contains(comparator);
	}

	private Action GetSelectObjectsAction(PropertyDisplay property)
	{
		Object objToSelect = (property.ObjectList != null && property.ObjectList.Count > 0) ? property.SerilizedObj.targetObjects[0] : property.SerilizedObj.targetObject;
		objToSelect = objToSelect is Component ? (objToSelect as Component).gameObject : objToSelect;

		return () =>
		{
			Selection.objects = property.SerilizedObj.targetObjects;
		};
	}

	private void UpdateProperties(PropertyDisplay displayProp)
	{
		displayProp.SerilizedObj.Update();
		for (int i = 0; i < displayProp.ChildPropertyDisplayList.Count; ++i)
		{
			UpdateProperties(displayProp.ChildPropertyDisplayList[i]);
		}
	}

	private bool StopDrawingOffscreenProperties()
	{
		bool isFinished = false;
		if (Event.current.type == EventType.Repaint)
		{
			Rect windowScreenPosition = position;
			windowScreenPosition.position = currentScrollPosition;
			isFinished = GUILayoutUtility.GetLastRect().yMax >= windowScreenPosition.yMax;
		}
		return isFinished;
	}

	private void CreateCustomEditorForProperty(PropertyDisplay property)
	{
		property.CustomEditor = Editor.CreateEditor(property.ObjectList.ToArray());
		for (int i = 0; i < property.ChildPropertyDisplayList.Count; ++i)
		{
			CreateCustomEditorForProperty(property.ChildPropertyDisplayList[i]);
		}
	}

	private bool GetHeaderIsExpanded(int id, bool expanded = false)
	{
		if (!headerExpandedDictionary.ContainsKey(id))
		{
			headerExpandedDictionary[id] = true;
		}
		return headerExpandedDictionary[id];
	}

	private void SetHeaderExpandedStateForChildren(PropertyDisplay property, bool value)
	{
		for (int i = 0; i < property.ChildPropertyDisplayList.Count; ++i)
		{
			SetHeaderExpandedState(property.ChildPropertyDisplayList[i].InstanceID, value);
		}
		headerExpandedDictionary[property.InstanceID] = value;
	}

	private void SetHeaderExpandedState(int id, bool value)
	{
		headerExpandedDictionary[id] = value;
	}

	private bool UpdateProgressBarDisplay(float progress)
	{
		if (EditorApplication.timeSinceStartup - searchStartTime > 2)
		{
			EditorUtility.DisplayProgressBar("Finding Property", "Patience...", progress);
		}

		if (EditorApplication.timeSinceStartup - searchStartTime > 10)
		{
			return true;
		}
		return false;
	}


	/// <summary>
	/// SELECTION HISTORY LIST
	/// </summary>
	static readonly int MAX_HISTORY_ITEMS = 50;
	[System.Serializable]
	class SelectedObject
	{
		public bool isLocked = false;
		public Object refObject = null;
		public bool isInScene = false;
	}

	[SerializeField] SelectedObject selectedObject = null;
	[SerializeField] List<SelectedObject> selectedObjects = new List<SelectedObject>();

	[SerializeField] Vector2 historyScrollPosition = Vector2.zero;

	[System.NonSerialized] GUIStyle historyLockButton;
	[System.NonSerialized] GUIContent historySearchButton = null;

	void DrawHistory()
	{
		GUILayout.Space(5);

		EditorGUILayout.BeginVertical("textField");
		EditorGUILayout.LabelField("Selection History");
		historyScrollPosition = EditorGUILayout.BeginScrollView(historyScrollPosition);
		GUILayout.Space(5);

		for (int i = selectedObjects.Count - 1; i >= 0; --i)
		{
			LayoutItem(i, selectedObjects[i]);
		}

		EditorGUILayout.EndScrollView();
		DrawClearHistoryButton();
		EditorGUILayout.EndVertical();
	}

	void DoHistoryOnSelectionChange()
	{
		if (Selection.activeObject == null)
		{
			selectedObject = null;
		}
		else if (selectedObject == null || selectedObject.refObject != Selection.activeObject)
		{
			SelectedObject obj = selectedObjects.Find(item => item.refObject == Selection.activeObject);
			selectedObject = obj;

			if (obj != null)
			{
				selectedObjects.Remove(obj);
				int firstNonLocked = (selectedObjects.FindIndex(item => item.isLocked == true));
				if (firstNonLocked >= 0)
				{
					selectedObjects.Insert(firstNonLocked, obj);
				}
				else
				{
					selectedObjects.Add(obj);
				}
				selectedObject = obj;
			}
			else
			{
				obj = new SelectedObject()
				{
					refObject = Selection.activeObject,
					isInScene = AssetDatabase.Contains(Selection.activeInstanceID) == false
				};

				int firstNonLocked = (selectedObjects.FindIndex(item => item.isLocked == true));
				if (firstNonLocked >= 0)
				{
					selectedObjects.Insert(firstNonLocked, obj);
				}
				else
				{
					selectedObjects.Add(obj);
				}
				selectedObject = obj;

			}

			//pop off the old items that exceed max history
			while (selectedObjects.Count > MAX_HISTORY_ITEMS)
			{
				selectedObjects.RemoveAt(0);
			}

			Repaint();
		}
	}

	bool DrawClearHistoryButton()
	{
		GUILayout.Space(5);

		bool clear = GUILayout.Button("Clear History", EditorStyles.miniButton);
		if (clear)
		{
			for (int j = selectedObjects.Count - 1; j >= 0; --j)
			{
				if (selectedObjects[j].isLocked == false)
				{
					selectedObjects.RemoveAt(j);
				}
			}
		}

		GUILayout.Space(5);
		return clear;
	}

	void LayoutItem(int i, SelectedObject obj)
	{
		if (historyLockButton == null)
		{
			GUIStyle temp = "IN LockButton";
			historyLockButton = new GUIStyle(temp);
			historyLockButton.margin.top = 3;
			historyLockButton.margin.left = 10;
			historyLockButton.margin.right = 10;
		}

		GUIStyle style = EditorStyles.miniButtonLeft;
		style.alignment = TextAnchor.MiddleLeft;

		if (obj != null && obj.refObject != null)
		{
			GUILayout.BeginHorizontal();

			bool wasLocked = obj.isLocked;
			obj.isLocked = GUILayout.Toggle(obj.isLocked, GUIContent.none, historyLockButton);
			if (wasLocked != obj.isLocked)
			{
				selectedObjects.Remove(obj);
				int firstNonLocked = (selectedObjects.FindIndex(item => item.isLocked == true));

				if (firstNonLocked >= 0)
				{
					selectedObjects.Insert(firstNonLocked, obj);
				}
				else
				{
					selectedObjects.Add(obj);
				}
			}

			if (obj == selectedObject)
			{
				GUI.enabled = false;
			}

			string objName = obj.refObject.name;
			if (obj.isInScene)
			{
				objName = ">> " + objName;// Append string to scene instances to easily tell them apart
			}

			if (GUILayout.Button(objName, style))
			{
				selectedObject = obj;
				Selection.activeObject = obj.refObject;
			}

			GUI.enabled = true;

			if (historySearchButton == null)
			{
				historySearchButton = EditorGUIUtility.IconContent("d_ViewToolZoom");
			}

			if (GUILayout.Button(historySearchButton, EditorStyles.miniButtonRight, GUILayout.MaxWidth(25), GUILayout.MaxHeight(15)))
			{
				EditorGUIUtility.PingObject(obj.refObject);
			}

			GUILayout.EndHorizontal();
		}
	}
}
