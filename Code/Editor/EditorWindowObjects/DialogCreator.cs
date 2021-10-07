using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using TMPro;

public class DialogCreator : EditorWindow 
{
	public const int STANDARD_SPACE = 10;

	public CreatorData creatorData;
	public bool addApplicable = true;
	public string currentType;
	public Color defaultColor;
	public int panelCount = 0;

	[MenuItem("Zynga/Editor Tools/Dialog Creator")]

	public static void openDialogCreator()
	{
		DialogCreator dialogCreator = (DialogCreator)EditorWindow.GetWindow(typeof(DialogCreator));
		dialogCreator.Show();
	}

	public void Awake()
	{
		init();
	}

	public void init()
	{
		defaultColor = GUI.color;
		creatorData = new CreatorData();
	}


	protected void OnGUI()
	{
		if (creatorData == null)
		{
			init();
		}


		creatorData.root = Selection.activeTransform;

		if (creatorData.root == null)
		{
			creatorData.root = GameObject.Find("Dialog Panel").transform;
		}

		addApplicable = !(creatorData.root.gameObject.GetComponent<UISprite>() || creatorData.root.gameObject.GetComponent<TextMeshPro>() || creatorData.root.gameObject.GetComponent<MeshFilter>());


		GUILayout.BeginHorizontal();
		GUILayout.Label("Parent Name: ");
		creatorData.root.gameObject.name = EditorGUILayout.TextField(creatorData.root.gameObject.name, GUILayout.Height(20));
		GUILayout.EndHorizontal();

		GUILayout.Space(STANDARD_SPACE);

		GUI.backgroundColor = new Color(0.7f, 0, 0.3f);
		GUILayout.BeginHorizontal();
		GUI.enabled = creatorData.root.GetComponent<Animator>() == null;
		if(GUI.enabled)
		{
			if (GUILayout.Button("Attach an animator", GUILayout.Height(30)))
			{
				creatorData.root.gameObject.AddComponent<Animator>();
			}
		}
		else
		{
			GUI.backgroundColor = Color.green;
			GUILayout.Button("Animator Script Applied", GUILayout.Height(30));
			GUI.backgroundColor = new Color(0.7f, 0, 0.3f);
		}

		GUI.enabled = creatorData.root.GetComponent<TabManager>() == null;
		if (GUI.enabled)
		{
			if (GUILayout.Button("Attach a tab manager", GUILayout.Height(30)))
			{
				creatorData.root.gameObject.AddComponent<TabManager>();
			}
		}
		else
		{
			GUI.backgroundColor = Color.green;
			GUILayout.Button("Tab Manager Script Applied", GUILayout.Height(30));
			GUI.backgroundColor = new Color(0.7f, 0, 0.3f);
		}



		GUI.enabled = creatorData.root.GetComponent<SlideController>() == null;
		if (GUI.enabled)
		{
			if (GUILayout.Button("Make this parent a Slider", GUILayout.Height(30)))
			{
				addSlider();
			}
		}
		else
		{
			GUI.backgroundColor = Color.green;
			GUILayout.Button("Slider Script Applied", GUILayout.Height(30));
			GUI.backgroundColor = new Color(0.7f, 0, 0.3f);
		}
		GUILayout.EndHorizontal();


		GUILayout.BeginHorizontal();
		GUI.enabled = creatorData.root.GetComponent<UIPanel>() == null;
		if (GUI.enabled)
		{
			if (GUILayout.Button("Attach a UI Panel to current parent", GUILayout.Height(30)))
			{
				addPanel();
			}
		}
		else
		{
			GUI.backgroundColor = Color.green;
			GUILayout.Button("UI Panel Script Applied", GUILayout.Height(30));
			GUI.backgroundColor = new Color(0.7f, 0, 0.3f);
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUI.enabled = creatorData.root.GetComponent<UISprite>() == null;
		if (GUILayout.Button("Add a Sizer", GUILayout.Height(30)))
		{
			addSizer();
		}
		if (GUILayout.Button("Load Default Dialog", GUILayout.Height(30)))
		{
			addDefaultObjects();
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUI.enabled = creatorData.root.GetComponent<UISprite>() == null;
		if (GUILayout.Button("Add a Mesh", GUILayout.Height(30)))
		{
			addMesh();
		}
		GUI.enabled = creatorData.root.GetComponent<UIAnchor>() == null;
		if (GUI.enabled)
		{
			if (GUILayout.Button("Add a Anchor", GUILayout.Height(30)))
			{
				addAnchor();
			}
		}
		else
		{
			GUI.backgroundColor = Color.green;
			GUILayout.Button("Anchor Script Applied", GUILayout.Height(30));
			GUI.backgroundColor = new Color(0.7f, 0, 0.3f);
		}

		GUI.enabled = true;
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		if(creatorData.root.GetComponent<UIButton>())
		{
			if (GUILayout.Button("Change to a Image Button", GUILayout.Height(30)))
			{
				DestroyImmediate(creatorData.root.gameObject.GetComponent<ButtonHandler>());
				DestroyImmediate(creatorData.root.gameObject.GetComponent<UIButton>());
				UIImageButton imageButton = creatorData.root.gameObject.AddComponent<UIImageButton>();
				ImageButtonHandler imageHandler = creatorData.root.gameObject.AddComponent<ImageButtonHandler>();
				imageButton.target = creatorData.root.GetComponentInChildren<UISprite>();
				imageHandler.imageButton = imageButton;
			}
		}
		if(creatorData.root.GetComponent<UIImageButton>())
		{
			if (GUILayout.Button("Change to a regular Button", GUILayout.Height(30)))
			{
				DestroyImmediate(creatorData.root.gameObject.GetComponent<ImageButtonHandler>());
				DestroyImmediate(creatorData.root.gameObject.GetComponent<UIImageButton>());
				UIButton button = creatorData.root.gameObject.AddComponent<UIButton>();
				ButtonHandler handler = creatorData.root.gameObject.AddComponent<ButtonHandler>();
				button.tweenTarget = creatorData.root.GetComponentInChildren<UISprite>().gameObject;
				handler.button = button;
			}
		}

		GUILayout.EndHorizontal();
		GUI.backgroundColor = defaultColor;

		GUILayout.Space(STANDARD_SPACE);

		typeIndex();

		GUILayout.Space(STANDARD_SPACE);

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Generate Prefab", GUILayout.Height(45)))
		{
			generatePrefab();
		}
		GUILayout.EndHorizontal();

		GUILayout.Space(STANDARD_SPACE);

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Display Panel List", GUILayout.Height(45)))
		{
			diaplayPanelList();
		}
		if (GUILayout.Button("Refresh", GUILayout.Height(45)))
		{
			creatorData.listInit();
			creatorData.populateList();
		}
		GUILayout.EndHorizontal();
	}

	private void addAnchor()
	{
		UIAnchor anchor = creatorData.root.gameObject.AddComponent<UIAnchor>();
	}

	private void addMesh()
	{
		GameObject mesh = Instantiate(creatorData.baseMesh, creatorData.root);
		mesh.name = "Base Mesh";
		mesh.transform.localScale = new Vector3(1, 1, 1);
		mesh.transform.localPosition = new Vector3(0, 0, 0);
	}

	private void diaplayPanelList()
	{
		Node root = new Node();
		panelCount = 0;
		recurFindPanel(creatorData.root, root);
		PanelListDisplay listDisplay = new PanelListDisplay(root, panelCount);
		listDisplay.Show();
	}

	private void recurFindPanel(Transform currentTransform, Node parentNode)
	{
		Node newNode = new Node(parentNode);
		newNode.name = currentTransform.gameObject.name;

		if (currentTransform.GetComponent<UIPanel>())
		{
			newNode.hasPanel = true;
			panelCount++;
		}
		if(!currentTransform.GetComponentInChildren<UIPanel>())
		{
			return;
		}
		for (int i = 0; i < currentTransform.childCount; i++)
		{
			recurFindPanel(currentTransform.GetChild(i), newNode);
		}
	}

	private void addSizer()
	{
		GameObject sizer = new GameObject();
		sizer.transform.SetParent(creatorData.root);
		sizer.name = "Sizer";
		sizer.transform.localScale = new Vector3(1, 1, 1);
		sizer.transform.localPosition = new Vector3(0, 0, 0);
	}

	public void addDefaultObjects()
	{
		GameObject dialogparent = GameObject.Find("Dialog Panel");
		if(dialogparent == null)
		{
			Debug.LogError("Could not find dialog parent in scene!");
			Close();
		}
		creatorData.root = Instantiate(creatorData.baseTemplate, creatorData.root.transform).transform;
		creatorData.root.gameObject.name = "Dialog";
		creatorData.root.localPosition = new Vector3(0, 0, 0);
		Selection.activeTransform = creatorData.root;
	}

	private void generatePrefab()
	{
		GameObject prefabRoot = recurFindPrefab(creatorData.root.gameObject);
		if(prefabRoot == null)
		{
			creatorData.root.gameObject.AddComponent<DialogBase>();
			prefabRoot = creatorData.root.gameObject;
		}
		// TODO:UNITY2018:nestedprefabs:confirm//old
		// UnityEngine.Object prefab = PrefabUtility.CreateEmptyPrefab("Assets/-Temporary Storage-/" + prefabRoot.name + ".prefab");
		// PrefabUtility.ReplacePrefab(prefabRoot, prefab, ReplacePrefabOptions.ConnectToPrefab);
		// TODO:UNITY2018:nestedprefabs:confirm//new
		PrefabUtility.SaveAsPrefabAsset(prefabRoot, "Assets/-Temporary Storage-/" + prefabRoot.name + ".prefab");
	}

	private GameObject recurFindPrefab(GameObject current)
	{
		if(current.GetComponent<DialogBase>())
		{
			return current;
		}
		if(current.transform.parent != null)
		{
			return recurFindPrefab(current.transform.parent.gameObject);
		}
		else
		{
			return null;
		}
	}

	private void addSlider()
	{
		SlideController slideController = creatorData.root.gameObject.AddComponent<SlideController>();
		SwipeArea swipeArea = creatorData.root.gameObject.AddComponent<SwipeArea>();
		creatorData.root.gameObject.AddComponent<TextMeshProMasker>();
		Instantiate(creatorData.scrollBar, creatorData.root.transform);
		GameObject sliderContent = new GameObject();
		slideController.swipeArea = swipeArea;
		slideController.content = sliderContent.AddComponent<SlideContent>();
		sliderContent.name = "Slider Content";
		sliderContent.transform.SetParent(creatorData.root);
	}


	private void addItem(CreatorData.PrefabEntry prefabEntry)
	{
		GameObject loadedPrefab = creatorData.tryLoadPrefab(prefabEntry);
		GameObject newInstance = Instantiate(loadedPrefab, creatorData.root);
		newInstance.name = string.Format(prefabEntry.displayName);
		if(newInstance.GetComponent<TextMeshPro>())
		{
			newInstance.transform.localPosition = new Vector3(0, 0, -10);
		}
		else
		{
			newInstance.transform.localPosition = new Vector3(0, 0, 0);
		}
		UISprite sprite = newInstance.GetComponent<UISprite>();
		if(sprite)
		{
			configureSprite(sprite);
		}
	}

	private void configureSprite(UISprite sprite)
	{
		sprite.name = "Sprite";
		sprite.atlas = NGUISettings.atlas;

		if (sprite.atlas != null)
		{
			string sn = EditorPrefs.GetString("NGUI Sprite", "");
			UIAtlas.Sprite sp = sprite.atlas.GetSprite(sn);

			if (sp != null)
			{
				sprite.spriteName = sn;
				if (sp.inner != sp.outer) sprite.type = UISprite.Type.Sliced;
			}
		}
		sprite.pivot = NGUISettings.pivot;
		sprite.cachedTransform.localScale = new Vector3(100f, 100f, 1f);
		sprite.MakePixelPerfect();
		Selection.activeGameObject = sprite.gameObject;
	}


	private void addPanel()
	{
		UIPanel newPanel = creatorData.root.gameObject.AddComponent<UIPanel>();
	}

	private void typeIndex()
	{
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Label("Please select a Type");
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		string[] options = new string[creatorData.prefabIndex.Count];
		int index = 0;
		foreach (KeyValuePair<string, List<CreatorData.PrefabEntry>> type in creatorData.prefabIndex)
		{
			if (type.Key != null)
			{
				options[index] = type.Key;
				index++;
			}
		}
		List<string> typeList = new List<string>(options);
		int selectedIndex = EditorGUILayout.Popup(
			typeList.IndexOf(currentType),
			options);
		List<CreatorData.PrefabEntry> itemList = null;
		if (selectedIndex > -1)
		{
			currentType = options[selectedIndex];
			itemList = creatorData.prefabIndex[currentType];
		}
		GUILayout.Space(STANDARD_SPACE);
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Label("Please select a Template");
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();

		itemIndex(itemList);
	}


	private void itemIndex(List<CreatorData.PrefabEntry> itemList)
	{
		List<CreatorData.PrefabEntry> indexedItems = new List<CreatorData.PrefabEntry>();
		if(itemList != null)
		{
			indexedItems = itemList;
		}
		string[] options = new string[indexedItems.Count];
		for (int i = 0; i < indexedItems.Count; i++)
		{
			if (indexedItems[i].displayName != null)
			{
				options[i] = indexedItems[i].displayName;
			}
		}
		int selectedIndex = EditorGUILayout.Popup(
			indexedItems.IndexOf(creatorData.currentSelection),
			options);
		GUILayout.Space(STANDARD_SPACE);
		GUILayout.BeginHorizontal();
		GUI.enabled = addApplicable;
		if(GUILayout.Button("Add to current parent", GUILayout.Height(45)))
		{
			if(selectedIndex > -1)
			{
				addItem(indexedItems[selectedIndex]);
			}
		}
		GUI.enabled = true;
		GUILayout.EndHorizontal();
		if (selectedIndex > -1)
		{
			creatorData.currentSelection = indexedItems[selectedIndex];
		}
	}

	private void OnDestroy()
	{
		creatorData.cleanUp();
		creatorData = null;
	}

	public class Node
	{
		public Node parent;
		public string name;
		public bool hasPanel;
		public int depth;
		public List<Node> children = new List<Node>();
		private Node parentNode;

		public Node()
		{
			this.parent = null;
			this.depth = 0;
		}

		public Node(Node parentNode)
		{
			parentNode.children.Add(this);
			this.parent = parentNode;
			this.depth = parentNode.depth + 1;
		}
	}

}
