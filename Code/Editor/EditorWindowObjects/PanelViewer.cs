using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;

/*
	Class: AtlasViewer
	Class to collect all the Atlases uses for the selected SKU in one place so you don't have to search through the project for them.
*/


public class PanelViewer : EditorWindow
{
	[MenuItem("Zynga/Editor Tools/Panel Viewer")]
	public static void openPanelViewer()
	{
		PanelViewer panelViewer = (PanelViewer)EditorWindow.GetWindow(typeof(PanelViewer));
		panelViewer.Show();
	}

	private PanelViewerObject panelViewerObject;
	
	public void OnGUI()
	{
		if (panelViewerObject == null)
		{
			panelViewerObject = new PanelViewerObject();
		}
		panelViewerObject.drawGUI(position);
	}
}

public class PanelViewerObject : EditorWindowObject
{
	protected override string getButtonLabel()
	{
		return "Panel Viewer";
	}

	protected override string getDescriptionLabel()
	{
		return "Shows all the items being used in the current selected object in the scene.";
	}

	private bool hasReadObject = false;

	private List<MeshRenderer> renderers;
	private List<TextMeshPro> tmPros;
	private List<UISprite> sprites;
	private List<UIPanel> panels;
	private List<UIImageButton> imageButtons;
	private Dictionary<UIAtlas, List<UISprite>> atlases;

	private bool showMeshRenderers = false;
	private bool showTextMeshPros = false;
	private bool showSprites = false;
	private bool showPanels = false;
	private bool showImageButtons = false;
	private bool showAtlases = false;

	private Dictionary<UIAtlas, bool> shouldShowAtlas;
	private GameObject activeGo = null;

	private void reset()
	{
		renderers = new List<MeshRenderer>();
		tmPros = new List<TextMeshPro>();
		sprites = new List<UISprite>();
		panels = new List<UIPanel>();
		imageButtons = new List<UIImageButton>();
	    atlases = new Dictionary<UIAtlas, List<UISprite>>();
		
		showMeshRenderers = false;
		showTextMeshPros = false;
		showSprites = false;
		showPanels = false;
		showImageButtons = false;
		showAtlases = false;

		shouldShowAtlas = new Dictionary<UIAtlas, bool>();
	}


	private void populateObjectReferences()
	{
		// Parse object data
		activeGo.GetComponentsInChildren(renderers);
		activeGo.GetComponentsInChildren(sprites);
		activeGo.GetComponentsInChildren(panels);
		activeGo.GetComponentsInChildren(imageButtons);

		// Find UIAtlas references
		foreach (UISprite sprite in sprites)
		{
			if (!atlases.ContainsKey(sprite.atlas))
			{
				atlases[sprite.atlas] =  new List<UISprite>();
			}
			atlases[sprite.atlas].Add(sprite);
		}
		
		// Seperate TextMeshPros from texture renderers.
		for (int i = renderers.Count -1 ; i >= 0; i--)
		{
			TextMeshPro tmPro = renderers[i].gameObject.GetComponent<TextMeshPro>();
			if (tmPro != null)
			{
				tmPros.Add(tmPro);
				renderers.RemoveAt(i);
			}
		}
		hasReadObject = true;		
	}

	private void displayObject(Object o, System.Type t)
	{
		if (o != null)
		{
			EditorGUILayout.ObjectField(o, t, false);
		}
	}

	private void displayList<T>(List<T> list, string labelPrefix, ref bool shouldShow) where T : Object
	{
	    shouldShow = EditorGUILayout.Foldout(shouldShow, labelPrefix + list.Count.ToString());
		if (shouldShow)
		{
			EditorGUI.indentLevel++;
			foreach (T obj in list)
			{
				EditorGUILayout.ObjectField(obj, typeof(T), false);
			}
			EditorGUI.indentLevel--;
		}

	}
	
	public override void drawGuts(Rect position)
	{
		GUILayout.BeginVertical();
		GameObject selectedGo = Selection.activeGameObject;
		if (selectedGo == null)
		{
			hasReadObject = false;
			GUILayout.Label("Please select an object in the scene.");
		}
		else
		{
		    if (activeGo != selectedGo)
			{
				activeGo = selectedGo;
				hasReadObject = false;
			}
			else if (GUILayout.Button("Refresh Data"))
			{
				hasReadObject = false;
			}

			if (!hasReadObject)
			{
				reset();
			    populateObjectReferences();
			}

			GUILayout.Label("Showing Info for: " + activeGo.name);
			// Display Object Data

			displayList<MeshRenderer>(renderers, "Mesh Renderers: ", ref showMeshRenderers);
			displayList<TextMeshPro>(tmPros, "TMPros: ", ref showTextMeshPros);
			displayList<UISprite>(sprites, "UISprites: ", ref showSprites);
			displayList<UIPanel>(panels, "UIPanels: ", ref showPanels);
			displayList<UIImageButton>(imageButtons, "Image Buttons: ", ref showImageButtons);
			
			/*
			showMeshRenderers = EditorGUILayout.Foldout(showMeshRenderers, "Mesh Renderers: " + renderers.Count.ToString());
			if (showMeshRenderers)
			{
				displayList<MeshRenderer>(renderers);
			}

		    showTextMeshPros = EditorGUILayout.Foldout(showTextMeshPros, "TMPros: " + tmPros.Count.ToString());
			if (showTextMeshPros)
			{
				displayList<TextMeshPro>(tmPros);
			}
			
		    showSprites = EditorGUILayout.Foldout(showSprites, "UISprites: " + sprites.Count.ToString());
			if (showSprites)
			{
				displayList<UISprite>(sprites);
			}
		    showPanels = EditorGUILayout.Foldout(showPanels, "UIPanels: " + panels.Count.ToString());
			if (showPanels)
			{
				displayList<UIPanel>(panels);			
			}
			
		    showImageButtons = EditorGUILayout.Foldout(showImageButtons, "Image Buttons: " + imageButtons.Count.ToString());
			if (showImageButtons)
			{
				displayList<UIImageButton>(imageButtons);
			}
			*/
		    showAtlases = EditorGUILayout.Foldout(showAtlases, "Atlas Refs: " + atlases.Count.ToString());			
			if (showAtlases)
			{
				foreach (KeyValuePair<UIAtlas,List<UISprite>> pair in atlases)
				{
					if (pair.Key == null || pair.Key.gameObject == null)
					{
						continue;
					}					
					UIAtlas atlas = pair.Key;
					if (!shouldShowAtlas.ContainsKey(atlas))
					{
						shouldShowAtlas[atlas] = false;
					}
					shouldShowAtlas[atlas] = EditorGUILayout.Foldout(shouldShowAtlas[atlas], atlas.name + ": " + pair.Value.Count.ToString());
					if (shouldShowAtlas[atlas])
					{
						EditorGUI.indentLevel++;						
						foreach (UISprite atlasSprite in pair.Value)
						{
							EditorGUILayout.ObjectField(atlasSprite, typeof(UISprite), false);
						}
						EditorGUI.indentLevel--;						
					}
				}
			}
		}
		GUILayout.EndVertical();
	}
}
