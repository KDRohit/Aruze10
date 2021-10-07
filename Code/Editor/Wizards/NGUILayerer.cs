using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
*/

public class NGUILayerer : EditorWindow
{		
	[MenuItem ("Zynga/Editor Tools/NGUI Layerer")]
	public static void openNGUILayerer()
	{
		NGUILayerer layerer = (NGUILayerer)EditorWindow.GetWindow(typeof(NGUILayerer));
		layerer.Show();
	}
		
	private NGUILayererObject nguiLayererObject;

	public void OnGUI()
	{
		if (nguiLayererObject == null)
		{
			nguiLayererObject = new NGUILayererObject();
		}
		nguiLayererObject.drawGUI(position);
	}
}
	
public class NGUILayererObject : EditorWindowObject
{
	public UIPanel rootUIPanel = null;
	
	public List<RenderLayer> renderLayers = new List<RenderLayer>();
	
	// Keep an index of RenderLayers that store TextMeshPro objects by relative z position so they can be grouped.
	public Dictionary<int, RenderLayer> tmProGroups = new Dictionary<int, RenderLayer>();
	
	private Vector2 scrollPos = Vector2.zero;

	protected override string getButtonLabel()
	{
		return "NGUI Layerer";
	}

	protected override string getDescriptionLabel()
	{
		return "Standardizes and shows the Z position of UISprites and labels, showing the draw order of things. Allows you to adjust draw order of certain objects.";
	}

	private void clearData()
	{
		renderLayers.Clear();
		tmProGroups.Clear();
	}

	public override void drawGuts(Rect position)
	{
		scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
		
		rootUIPanel = EditorGUILayout.ObjectField("Root UIPanel", rootUIPanel, typeof(UIPanel), allowSceneObjects:true) as UIPanel;

		if (rootUIPanel != null)
		{
			if (GUILayout.Button("Analyze & Standardize the UI"))
			{
				standardizeUI();
			}
		}
		else
		{
			clearData();
		}
		
		EditorGUILayout.LabelField("Top of list is rendered in front, bottom in back.");
		
		EditorGUILayout.Space();
		
		foreach (RenderLayer layer in renderLayers)
		{
			if (layer.nguiPanelElements == null)
			{
				// This can happen during dev when code changes.
				break;
			}
						
			if (layer.nameType == "TextMeshPro")
			{
				if (layer.tmProElements == null)
				{
					// This can happen during dev when code changes.
					break;
				}
				// Show a special collapsible group for TextMeshPro objects at the same relative Z position.
				layer.showTMProUI();
			}
			else
			{
				EditorGUILayout.BeginHorizontal();

				if (GUILayout.Button(layer.name, GUILayout.Width(200)))
				{
					CommonEditor.pingAndSelectObject(layer.gameObject);
				}
			
				if (layer.nguiPanelElements.Count > 0)
				{
					layer.isExpanded = EditorGUILayout.Foldout(layer.isExpanded, "Widgets");
				}

				GUILayout.Space(10);
				if (layer.canEditZ)
				{
					GUILayout.FlexibleSpace();
					EditorGUILayout.LabelField("Relative Z:");
					int newRelativeZ = EditorGUILayout.IntField(layer.relativeZ, GUILayout.Width(45));

					if (newRelativeZ != layer.relativeZ)
					{
						int diff = newRelativeZ - layer.relativeZ;
						CommonTransform.setZ(layer.gameObject.transform, layer.gameObject.transform.localPosition.z + diff);
						layer.relativeZ = newRelativeZ;
					}
				}
				else
				{
					GUILayout.FlexibleSpace();
					EditorGUILayout.LabelField("Relative Z: " + layer.relativeZ, GUILayout.Width(110));
				}

				EditorGUILayout.EndHorizontal();
			}
			
			if (layer.nguiPanelElements.Count > 0 && layer.isExpanded)
			{
				foreach (WidgetRenderAtlas atlas in layer.nguiPanelElements)
				{
					atlas.showUI();
				}
			}

			EditorGUILayout.Space();
		}
		
		EditorGUILayout.EndScrollView();
	}
	
	private void standardizeUI()
	{
		clearData();
		
		rootUIPanel.sortByDepth = false;	// This should never be enabled when using our standard layering rules.

		setChildZ(rootUIPanel.gameObject, null);
		
		renderLayers.Sort(RenderLayer.sortByZ);
		
		foreach (RenderLayer layer in renderLayers)
		{
			foreach (WidgetRenderAtlas elements in layer.nguiPanelElements)
			{
				// Calculate the average Z position of widgets of each atlas,
				// to be used as the sorting method for each widget, for setting
				// the relative z position of each widget.
				elements.calculateAverageZ();
				elements.widgets.Sort(RenderLayer.sortWidgetsByDepth);
			}
			
			layer.nguiPanelElements.Sort(WidgetRenderAtlas.sortByAverageZ);
			
			int atlasNo = 0;
			foreach (WidgetRenderAtlas elements in layer.nguiPanelElements)
			{
				foreach (UIWidget widget in elements.widgets)
				{
					if (widget is UISprite)
					{
						setZIfDifferent(widget.gameObject, atlasNo * -1);
					}
				}
				atlasNo++;
			}
		}
	}
	
	private void setChildZ(GameObject go, RenderLayer parentRenderLayer)
	{
		var widget = go.GetComponent<UIWidget>();
	
		if (widget != null)
		{
			var panel = go.GetComponent<UIPanel>();
	
			if (panel != null)
			{
				// A widget object should not also have a UIPanel directly on it.
				Object.DestroyImmediate(panel);
				EditorUtility.DisplayDialog("UIPanel Violation", "Found a UIPanel on object with a NGUI widget, which is bad practice. Use a parent object with a UIPanel if necessary. The offending panel has been removed.", "See the object involved");
				CommonEditor.pingAndSelectObject(go);
			}

			if (widget is UITexture)
			{
				renderLayers.Add(new RenderLayer("UITexture", go, rootUIPanel.gameObject));
			}
			else
			{
				if (widget is UILabel)
				{
					// This is a UILabel, which is always at -10 local z, same as TextMeshPro objects.
					// Sprites have their z position set after sorting, since there may be more than one atlas rendered on the panel,
					// and we want each sprite within an atlas to have the same z, but different than other sprites with different atlases.
					setZIfDifferent(go, -10);
				}
		
				if (parentRenderLayer == null)
				{
					// This shouldn't happen.
					Debug.LogError("Found UISprite or UILabel without a parent UIPanel: " + go.name, go);
				}
				else
				{
					parentRenderLayer.addWidget(widget);
				}
			}
		}
		else
		{
			var panel = go.GetComponent<UIPanel>();
	
			if (panel != null)
			{
				// Found a UIPanel. Leave the Z alone, since this is used for layering.
				// If a child UIPanel is found under another UIPanel,
				// then the child UIPanel is the new parent for anything under it.
				parentRenderLayer = new RenderLayer("UIPanel", go, rootUIPanel.gameObject);
				renderLayers.Add(parentRenderLayer);
				panel.sortByDepth = false;	// This should never be enabled when using our standard layering rules.
			}
			else
			{
				var tmPro = go.GetComponent<TextMeshPro>();
		
				if (tmPro != null)
				{
					// We need to create the new layer first, even if not used, to get the relative Z of it.
					RenderLayer newLayer = new RenderLayer("TextMeshPro", go, rootUIPanel.gameObject);
					
					// All labels should be at -10 z. Must do this after making the renderLayer.
					setZIfDifferent(go, -10);
					newLayer.setRelativeZ();
					
					RenderLayer existingLayer = null;
					if (tmProGroups.TryGetValue(newLayer.relativeZ, out existingLayer))
					{
						// Use an existing layer instead of the new layer.
						existingLayer.addTMPro(tmPro);
					}
					else
					{
						tmProGroups.Add(newLayer.relativeZ, newLayer);
						renderLayers.Add(newLayer);
						newLayer.addTMPro(tmPro);
					}
				}
				else
				{
					var rnd = go.GetComponent<MeshRenderer>();
			
					if (rnd != null)
					{
						// Leave these as-is for Z.
						renderLayers.Add(new RenderLayer("MeshRenderer", go, rootUIPanel.gameObject));
					}
					else
					{
						var particleSystem = go.GetComponent<ParticleSystem>();
			
						if (particleSystem != null)
						{
							// Leave these as-is for Z.
							renderLayers.Add(new RenderLayer("ParticleSystem", go, rootUIPanel.gameObject));
						}
						else
						{
							if (!areChildrenAllFreeZRenderLayers(go))
							{
								// No special components, and children are not exclusively non-NGUI things that get rendered,
								// so set this to 0 z and don't add to the renderLayers list.
								setZIfDifferent(go, 0);
							}
						}
					}
				}
			}
		}
		
		for (int i = 0; i < go.transform.childCount; i++)
		{
			var child = go.transform.GetChild(i);
			setChildZ(child.gameObject, parentRenderLayer);
		}
	}
	
	// Sometimes we have a parent object that only contains a child for scaling reasons.
	// If all of the children of the parent object are free to be at any Z,
	// then we know that the parent is free to be at any Z.
	private bool areChildrenAllFreeZRenderLayers(GameObject parent)
	{
		string[] types = new string[]
		{
			"UIPanel",
			"UITexture",
			"MeshRenderer",
			"ParticleSystem",
			"ParticleEmitter"
		};
		
		// Check the children.
		for (int i = 0; i < parent.transform.childCount; i++)
		{
			var child = parent.transform.GetChild(i);
			bool didFindFreeZRenderLayer = false;
			foreach (string type in types)
			{
				if (child.gameObject.GetComponent(type) != null ||	// Check this child.
					!areChildrenAllFreeZRenderLayers(child.gameObject)	// Recurse into the child's grandchildren.
				)
				{
					didFindFreeZRenderLayer = true;
				}
			}
			if (!didFindFreeZRenderLayer)
			{
				return false;
			}
		}		

		return true;
	}
	
	private void setZIfDifferent(GameObject go, int z)
	{
		if (Mathf.RoundToInt(go.transform.localPosition.z) != z)
		{
//			Debug.LogWarning(string.Format("Changing Z from {0} to {1} on {2}", CommonMath.round(go.transform.localPosition.z, 2), CommonMath.round(z, 2), go.name), go);
			CommonTransform.setZ(go.transform, z);
		}
	}
		
	// Represents a single layer of renderering.
	[System.Serializable]
	public class RenderLayer
	{
		public string nameType = "";
		public bool isExpanded = false;
		public bool canEditZ = true;
		public int relativeZ = 0;	// The new relative z.
		public string name;
		public GameObject gameObject;	// If nameType is "TextMeshPro", then the tmProElements list is used instead of this.
				
		// Create a list of TextMeshPro elements for collapsing in the view.
		// All TextMeshPro objects in this list will have the same relative Z value.
		public List<TextMeshPro> tmProElements = new List<TextMeshPro>();

		// Create a list of NGUI elements for sorting.
		public List<WidgetRenderAtlas> nguiPanelElements = new List<WidgetRenderAtlas>();
		// Also index by UIAtlas for quick lookup.
		public Dictionary<UIAtlas, WidgetRenderAtlas> nguiPanelElementsDict = new Dictionary<UIAtlas, WidgetRenderAtlas>();
		
		private GameObject rootGameObject;	// Keep a local reference.
		
		public RenderLayer(string nameType, GameObject go, GameObject rootGameObject)
		{
			this.rootGameObject = rootGameObject;
			this.nameType = nameType;
			gameObject = go;
			
			setRelativeZ();
			
			if (nameType == "TextMeshPro" || go == rootGameObject)
			{
				// Only the root game object, TextMeshPro and UISprite and UILabel can't be manually adjusted.
				canEditZ = false;
			}
			
			name = string.Format("{0}: \"{1}\"", nameType, go.name);
		}
		
		// This should get called whenever the z position is changed.
		public void setRelativeZ()
		{
			Vector3 diff = rootGameObject.transform.InverseTransformPoint(gameObject.transform.position);
			relativeZ = Mathf.RoundToInt(diff.z);
		}
		
		public void addWidget(UIWidget widget)
		{
			// Put the widget into the list of the atlas used to render it.
			UIAtlas atlas = null;
			UISprite sprite = widget as UISprite;
			UILabel label = widget as UILabel;
			
			if (sprite != null)
			{
				atlas = sprite.atlas;
			}
			else if (label != null)
			{
				atlas = label.font.atlas;
			}
			
			atlas = SpriteFinder.getFinalAtlas(atlas);
			
			if (atlas == null)
			{
				Debug.LogWarning("UIWidget " + widget.gameObject.name + " has no atlas assigned.");
				return;
			}
			
			WidgetRenderAtlas renderAtlas = null;
			
			if (nguiPanelElementsDict.ContainsKey(atlas))
			{
				renderAtlas = nguiPanelElementsDict[atlas];
			}
			else
			{
				renderAtlas = new WidgetRenderAtlas(atlas);
				nguiPanelElements.Add(renderAtlas);
				nguiPanelElementsDict.Add(atlas, renderAtlas);
			}
						
			renderAtlas.widgets.Add(widget);
		}
		
		public void addTMPro(TextMeshPro label)
		{
			tmProElements.Add(label);
		}
				
		public static int sortByZ(RenderLayer a, RenderLayer b)
		{
			if (a.gameObject.transform.position.z == b.gameObject.transform.position.z)
			{
				// If the z is the same, then sort by nameType and then name, just for grouping purposes.
				if (a.nameType == b.nameType)
				{
					return a.name.CompareTo(b.name);
				}
				return a.nameType.CompareTo(b.nameType);
			}
			
			return a.gameObject.transform.position.z.CompareTo(b.gameObject.transform.position.z);
		}
		
		public static int sortWidgetsByDepth(UIWidget a, UIWidget b)
		{
			return b.depth.CompareTo(a.depth);
		}

		public void showTMProUI()
		{
			EditorGUILayout.BeginHorizontal();
			isExpanded = EditorGUILayout.Foldout(isExpanded, "TextMeshPro Labels");
			GUILayout.FlexibleSpace();
			EditorGUILayout.LabelField("Relative Z: " + relativeZ, GUILayout.Width(110));
			EditorGUILayout.EndHorizontal();
			
			if (isExpanded)
			{
				foreach (TextMeshPro label in tmProElements)
				{
					EditorGUILayout.BeginHorizontal();
					GUILayout.Space(30);
					
					if (GUILayout.Button(label.gameObject.name + ", \"" + label.text.Substring(0, Mathf.Min(15, label.text.Length)) + "\""))
					{
						CommonEditor.pingAndSelectObject(label.gameObject);
					}

					EditorGUILayout.EndHorizontal();
				}
			}
		}
	}
	
	// Contains info about UIWidgets being rendered by a UIPanel, with possible various atlases.
	public class WidgetRenderAtlas
	{
		public UIAtlas atlas;
		public float averageZPosition;
		public List<UIWidget> widgets = new List<UIWidget>();
		
		private bool isExpanded = false;
		
		public WidgetRenderAtlas(UIAtlas atlas)
		{
			this.atlas = atlas;
		}
		
		public void calculateAverageZ()
		{
			float total = 0.0f;
			
			foreach (UIWidget widget in widgets)
			{
				total += widget.transform.localPosition.z;
			}
			
			averageZPosition = total / widgets.Count;
		}

		public void showUI()
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(10);
			isExpanded = EditorGUILayout.Foldout(isExpanded, atlas.gameObject.name + " Atlas Widgets, Local Z: " + widgets[0].transform.localPosition.z);
			EditorGUILayout.EndHorizontal();
			
			if (isExpanded)
			{
				foreach (UIWidget widget in widgets)
				{
					EditorGUILayout.BeginHorizontal();
					GUILayout.Space(30);
			
					if (GUILayout.Button(widget.gameObject.name, GUILayout.Width(150)))
					{
						CommonEditor.pingAndSelectObject(widget.gameObject);
					}

					GUILayout.FlexibleSpace();
					EditorGUILayout.LabelField("Depth:");
					int newDepth = EditorGUILayout.IntField(widget.depth, GUILayout.Width(30));
					if (newDepth != widget.depth)
					{
						widget.depth = newDepth;
						if (widget.panel.gameObject.activeSelf)
						{
							// Force the UIPanel to refresh the display by deactivating and reactivating it.
							GameObject panelObj = widget.panel.gameObject;	// Store this because the reference to widget.panel is lost when deactivating it.
							panelObj.SetActive(false);
							panelObj.SetActive(true);
						}
					}

					EditorGUILayout.EndHorizontal();
				}
			}
		}

		public static int sortByAverageZ(WidgetRenderAtlas a, WidgetRenderAtlas b)
		{
			return b.averageZPosition.CompareTo(a.averageZPosition);
		}
	}
}