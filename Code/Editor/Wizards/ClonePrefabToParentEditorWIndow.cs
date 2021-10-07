using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// This wizard lets you select a prefab and instantiate it under any number of selected objects, instead of having to drag it into the scene one at a time.
/// Makes positioning things during setup easier.
/// </summary>
public class ClonePrefabToParentEditorWIndow : EditorWindow
{
	private const int MIN_WINDOW_WIDTH = 300;
	private const int MIN_WINDOW_HEIGHT = 135;
	private const int MAX_TEXTFIELD_HEIGHT = 20;

	[SerializeField] private GameObject prefabToClone;
	private GameObject[] selectedGameObjects;
	private string buttonSuffix;
	[SerializeField] private List<GameObject> prefabsWithOverrides;

	#region Set up the wizard dialog
	//Draws the Editor Window//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[MenuItem("Zynga/Wizards/Clone Object To Parents")]
	static void CreateWizard()
	{
		ClonePrefabToParentEditorWIndow window = (ClonePrefabToParentEditorWIndow)EditorWindow.GetWindow(typeof(ClonePrefabToParentEditorWIndow));
		window.setDefaultWindowSize();
	}

	// Sets the window to the given size and centers it on the screen.
	private void setWindowSize(int windowWidth, int windowHeight)
	{
		this.position = new Rect((Screen.currentResolution.width - windowWidth) / 2, (Screen.currentResolution.height - windowHeight) / 2, windowWidth, windowHeight);
	}

	private void setDefaultWindowSize()
	{
		setWindowSize(MIN_WINDOW_WIDTH, MIN_WINDOW_HEIGHT);
	}
	#endregion

	private void OnEnable()
	{
		selectedGameObjects = Selection.gameObjects;
	}

	private void OnSelectionChange()
	{
		selectedGameObjects = Selection.gameObjects;
		buttonSuffix = selectedGameObjects.Length > 1 ? "Gameobjects" : "Gameobject";
	}

	private void OnGUI()
	{
		EditorGUILayout.LabelField("Prefab To Clone");
		prefabToClone = (GameObject)EditorGUILayout.ObjectField("", prefabToClone, typeof(GameObject), false);

		if(prefabToClone != null && selectedGameObjects.Length > 0)
		{
			GUI.enabled = true;
			GUI.color = Color.green;
		}
		else
		{
			GUI.enabled = false;
			GUI.color = Color.white;
		}

		if (GUILayout.Button(("Clone to " + selectedGameObjects.Length + " " + buttonSuffix), GUILayout.Height(45)))
		{
			foreach (GameObject obj in selectedGameObjects)
			{
				GameObject instance = PrefabUtility.InstantiatePrefab(prefabToClone) as GameObject;
				instance.transform.parent = obj.transform;
				instance.name = prefabToClone.name;

				//Resets the transforms to default
				instance.transform.localPosition = Vector3.zero;
				instance.transform.localScale = Vector3.one;
				instance.transform.rotation = Quaternion.identity;
			}
		}

		if (prefabToClone != null && selectedGameObjects.Length > 0)
		{
			GUI.enabled = true;
			GUI.color = Color.red;
		}
		else
		{
			GUI.enabled = false;
			GUI.color = Color.white;
		}

		if (GUILayout.Button(("Remove instance from " + selectedGameObjects.Length + " " + buttonSuffix), GUILayout.Height(45)))
		{
			foreach (GameObject obj in selectedGameObjects)
			{
				foreach(Transform child in obj.transform)
				{

					GameObject instance = PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject);
					if (instance == prefabToClone)
					{
						Debug.Log("Match");

						if (PrefabUtility.GetPropertyModifications(child.gameObject).Length > 0)
						{
							Debug.Log("Prefab has overrides");

							for (int i = 0; i < PrefabUtility.GetPropertyModifications(child.gameObject).Length; i++)
							{
								Debug.Log(PrefabUtility.GetPropertyModifications(child.gameObject)[i].propertyPath);
							}

							prefabsWithOverrides.Add(child.gameObject);
						}
						else
						{
							Debug.Log("No modifications");
							DestroyImmediate(child.gameObject);
						}



						if (prefabsWithOverrides.Count > 0)
						{
							bool removeOverrides = EditorUtility.DisplayDialog("Remove Instances with Overrides?", "There are instances of this prefab found that have some properties overridden. Would you like to remove these from the scene as well?", "Yes", "No");

							if (removeOverrides)
							{
								for (int i = prefabsWithOverrides.Count - 1; i >= 0; i--)
								{
									DestroyImmediate(prefabsWithOverrides[i]);
								}
							}
						}
					}
				}
			}
		}
	}
}
