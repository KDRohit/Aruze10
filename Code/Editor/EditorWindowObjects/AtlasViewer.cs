using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

/*
	Class: AtlasViewer
	Class to collect all the Atlases uses for the selected SKU in one place so you don't have to search through the project for them.
*/


public class AtlasViewer : EditorWindow
{
	[MenuItem("Zynga/Editor Tools/Atlas Viewer")]
	public static void openAtlasViewer()
	{
		AtlasViewer atlasViewer = (AtlasViewer)EditorWindow.GetWindow(typeof(AtlasViewer));
		atlasViewer.Show();
	}

	private AtlasViewerObject atlasViewerObject;
	
	public void OnGUI()
	{
		if (atlasViewerObject == null)
		{
			atlasViewerObject = new AtlasViewerObject();
		}
		atlasViewerObject.drawGUI(position);
	}
}

public class AtlasViewerObject : EditorWindowObject
{
	protected override string getButtonLabel()
	{
		return "Atlas Viewer";
	}

	protected override string getDescriptionLabel()
	{
		return "Finds all of the atlases in our game and provides easy drag-n-drop access to them.";
	}

	private string[] skuNames = new string[]
	{
		"HIR"
	};

	private string currentSku { get { return skuNames[selectedSkuIndex]; } }
	
	private List<UIAtlas> atlasList = new List<UIAtlas>();
	private List<UIAtlas> referenceAtlasList = new List<UIAtlas>();
	
	private int selectedSkuIndex = 0;

	private Vector2 scrollPosition = Vector2.zero;
	private bool showReference = false;
	private bool showAtlas = false;
	private string search = "";

	private UIAtlas selectedAtlas;

	public override void drawGuts(Rect position)
	{
		GUILayout.BeginVertical();		
		int newSkuIndex = EditorGUILayout.Popup(
			"Sku:",
			selectedSkuIndex,
			skuNames
	    );

		if (newSkuIndex != selectedSkuIndex)
		{
			selectedSkuIndex = newSkuIndex;
			populateList();
		}
		
		if (GUILayout.Button("Populate"))
		{
			populateList();
		}
		EditorGUILayout.BeginHorizontal();
		search = GUILayout.TextField(search);
		if (GUILayout.Button("X", GUILayout.Width(40f)))
		{
			search = "";
		}
		EditorGUILayout.EndHorizontal();

		if (selectedAtlas != null)
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Set selected sprite atlases:");
			EditorGUILayout.EndHorizontal();
		
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.ObjectField(selectedAtlas, typeof(UIAtlas), false);
			if (GUILayout.Button("X", GUILayout.Width(40f)))
			{
				selectedAtlas = null;
			}
		
			if (Selection.gameObjects.Length > 0)
			{
				// If there are objects selected in the scene,
				// then show the button to set all sprites to that atlas.
				if (GUILayout.Button("Set Sprites"))
				{
					// Find all the selected UISprites.
					List<UISprite> sprites = new List<UISprite>();
					foreach (GameObject gameobject in Selection.gameObjects)
					{
						UISprite sprite = gameobject.GetComponent<UISprite>();
						if (sprite != null)
						{
							sprites.Add(sprite);
						}
					}

					int atlasSetCount = 0;
					// Then set all of the UIAtlas components.
					foreach (UISprite sprite in sprites)
					{
						// If the selected atlas has that sprite then set the atlas.
						if (sprite.atlas != selectedAtlas)
						{
							atlasSetCount++;
							sprite.atlas = selectedAtlas;
						}
					}
					if (atlasSetCount > 0)
					{
						Debug.LogFormat("Set {0} sprites to have the {1} atlas.", atlasSetCount, selectedAtlas.name);
					}
				}
			}
		
			EditorGUILayout.EndHorizontal();
		}

		
		bool didAtlasListBreak = false;
		scrollPosition = GUILayout.BeginScrollView(scrollPosition);

		showAtlas = EditorGUILayout.Foldout(showAtlas, "Atlases");
		if (showAtlas)
		{
			foreach (UIAtlas atlas in atlasList)
			{
				if (atlas == null)
				{
					didAtlasListBreak = true;
					break;
				}				

				if (search != "" && atlas.name != null)
				{
					if (!atlas.name.ToLower().Contains(search.ToLower()))
					{
						continue;
					}
				}
				GUILayout.BeginHorizontal();
				EditorGUILayout.ObjectField(
					atlas,
					typeof(UIAtlas),
					false);
				if (GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDrag)
				{
					// Enabled Dragging.
					DragAndDrop.PrepareStartDrag ();
					DragAndDrop.objectReferences = new Object[] {atlas};
					DragAndDrop.StartDrag (atlas.name + "<UIAtlas>");
					Event.current.Use();
				}

				if (GUILayout.Button("<", GUILayout.Width(40f)))
				{
					selectedAtlas = atlas;
				}
				GUILayout.EndHorizontal();
			}
		}

		/* MRCC -- Commenting this out because we dont use Reference Atlases anymore.
		showReference = EditorGUILayout.Foldout(showReference, "Reference Atlases");
		if (showReference)
		{
			foreach (UIAtlas atlas in referenceAtlasList)
			{
				if (atlas == null)
				{
					didAtlasListBreak = true;
				    break;
				}
				if (search != "" && atlas.name != null)
				{
					if (!atlas.name.ToLower().Contains(search.ToLower()))
					{
						continue;
					}
				}
				GUILayout.BeginHorizontal();
				EditorGUILayout.ObjectField(
				    atlas,
					typeof(UIAtlas),
					false);
				if (GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDrag)
				{
					// Enabled Dragging.
					DragAndDrop.PrepareStartDrag ();
					DragAndDrop.objectReferences = new Object[] {atlas};
					DragAndDrop.StartDrag (atlas.name + "<UIAtlas>");
					Event.current.Use();
				}

				if (atlas == selectedAtlas)
				{
					if (GUILayout.Button("^", GUILayout.Width(40f)))
					{
						// Un-select this atlas.
						selectedAtlas = null;
					}
				}
				else
				{
					if (GUILayout.Button("<", GUILayout.Width(40f)))
					{
						// Select this atlas.
						selectedAtlas = atlas;
					}
				}

				GUILayout.EndHorizontal();
			}
		}
		*/
		
		GUILayout.EndScrollView();
		GUILayout.EndVertical();

		if (didAtlasListBreak)
		{
			// If we somehow we have null atlases, then invalidate the list and repopulate.
			populateList();
		}
	}

	public void populateList()
	{
		// Find all the UIAtlases in this folder:
		atlasList.Clear();
		referenceAtlasList.Clear();

		List<string> atlasPaths = new List<string>();

		// Add the reference Atlas Paths to the total list.
		string referenceAtlasPath = string.Format("Data/{0}/NGUI/Reference Atlases", currentSku);
		atlasPaths.AddRange(Directory.GetFiles(Application.dataPath + "/" + referenceAtlasPath));

		referenceAtlasList.AddRange(getAtlases(atlasPaths, referenceAtlasPath));
		
		// Go through Subfolders to find the Atlas Paths and add them to the total list.
		string atlasFoldersPath = string.Format("Data/{0}/NGUI/Atlases", currentSku);
		string[] atlasFolders = Directory.GetFiles(Application.dataPath + "/" + atlasFoldersPath);
		foreach (string path in atlasFolders)
		{
			string folderPath = path.Replace(".meta", "");
			List<string> files = new List<string>(Directory.GetFiles(folderPath));
			folderPath = folderPath.Substring(folderPath.IndexOf("Unity/Assets/") + "Unity/Assets/".Length);
			
			atlasList.AddRange(getAtlases(files, folderPath));
		}
		
	}
	
	public List<UIAtlas> getAtlases(List<string> filePaths, string basePath)
	{
		List<UIAtlas> result = new List<UIAtlas>();
		foreach (string filename in filePaths)
		{
			int index = filename.LastIndexOf("/");

			string localPath = "Assets/" + basePath;

			if (index > 0)
			{
				localPath += filename.Substring(index);
			}

			UIAtlas atlas = (UIAtlas)AssetDatabase.LoadAssetAtPath(localPath, typeof(UIAtlas)); 
			
			if (atlas != null)
			{
				result.Add(atlas);
			}
		}
		return result;
	}

	
}