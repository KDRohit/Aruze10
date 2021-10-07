using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

// This class goes through a single prefab (or all of them if you want) and
// finds any UIAnchors that are linked either to other UIAnchors or UIStretch objects
// and adds them to a list controlled by that object so that when the object they are anchored to
// repositions or resizes, the anchors that are anchored to it move them. In doing this, we 
// should never have to have an anchor that checks each frame for a reposition. 

public class AnchorDependenciesResolver : ScriptableWizard
{
	public GameObject[] objectsToCheck;
	public List<GameObject> resultList;
	List<UIAnchor> anchors;
	List<UIStretch> stretches;
	Dictionary<UIStretch, List<Transform>> stretchesAndChildren;
	[MenuItem("Zynga/Wizards/Resolve Anchor  Dependencies")]
	private static void CreateWizard()
	{
		ScriptableWizard.DisplayWizard<AnchorDependenciesResolver>("Resolve Anchor Dependencies", "Close", "Begin");
	}

	private void OnWizardUpdate()
	{

	}

	public void OnWizardCreate()
	{
		// Called when Close is clicked.
	}

	private void OnWizardOtherButton()
	{
		if (!(EditorUtility.DisplayDialog("Resolve Dependencies? This will take a little while", "Resolver", "OK")))
		{
			return;
		}

		List<GameObject> allPrefabs = new List<GameObject>();
		resultList = new List<GameObject>();
		if (objectsToCheck == null)
		{
			allPrefabs = CommonEditor.gatherPrefabs("Assets/Data/HIR/Bundles");
		}
		else
		{
			allPrefabs.AddRange(objectsToCheck);
		}

		for (int i = 0; i < allPrefabs.Count; i++)
		{
			bool cancelRequested = EditorUtility.DisplayCancelableProgressBar("Resolving dependencies...", "Processing " + allPrefabs[i].name + " asset " + i + " of " + allPrefabs.Count, i / (1.0f * allPrefabs.Count));

			if (cancelRequested)
			{
				EditorUtility.ClearProgressBar();
				return;
			}

			GameObject objectToCheck = allPrefabs[i];
			if (objectToCheck.activeSelf)
			{
				// TODO:UNITY2018:nestedprefabs:confirm//old
				// GameObject objectToManipulate = Instantiate(objectToCheck);
				// objectToManipulate = PrefabUtility.ConnectGameObjectToPrefab(objectToManipulate, objectToCheck);
				// TODO:UNITY2018:nestedprefabs:confirm//new
				string assetPath = AssetDatabase.GetAssetPath(objectToCheck);
				GameObject objectToManipulate = PrefabUtility.LoadPrefabContents(assetPath);

				if (checkForDirectStretchLinks(objectToManipulate))
				{
					resultList.Add(objectToCheck);
					// TODO:UNITY2018:nestedprefabs:confirm//old
					// PrefabUtility.ReplacePrefab(
					//  objectToManipulate,
					//  objectToCheck,
					// 	ReplacePrefabOptions.Default
					//  );
					// TODO:UNITY2018:nestedprefabs:confirm//new
					PrefabUtility.SaveAsPrefabAsset(objectToManipulate, assetPath);
					PrefabUtility.UnloadPrefabContents(objectToManipulate);
				}

				DestroyImmediate(objectToManipulate);
			}
		}

		EditorUtility.ClearProgressBar();

	}

	private bool checkForDirectStretchLinks(GameObject objectToManipulate)
	{
		bool requiresSave = false;
		// This is all the anchors and stretches on this object
		anchors = new List<UIAnchor>();
		stretches = new List<UIStretch>();

		// These are what the anchors and or stretches know about. So if a stretch is linked indirectly to a background or something,
		// we can see if our anchor is ALSO linked to said background.
		stretchesAndChildren = new Dictionary<UIStretch, List<Transform>>();

		// First grab all of our anchors and stretches
		anchors.AddRange(objectToManipulate.GetComponentsInChildren<UIAnchor>(true));

		stretches.AddRange(objectToManipulate.GetComponentsInChildren<UIStretch>(true));

		UIStretch stretchToManipulate;

		// Grab all the stuff that's a child of the stretch. If one of our anchors are linked to one, it's dependent on that stretch. Also clear known anchors for now.
		for (int i = 0; i < stretches.Count; i++)
		{
			stretchToManipulate = stretches[i];
			stretchToManipulate.dependentAnchors.Clear();
			stretchToManipulate.dependentStretches.Clear();

			stretchesAndChildren.Add(stretchToManipulate, new List<Transform>());
			stretchesAndChildren[stretchToManipulate].AddRange(stretchToManipulate.GetComponentsInChildren<Transform>(true));
		}
		for (int i = 0; i < anchors.Count; i++)
		{
			anchors[i].dependentAnchors.Clear();
		}


		// So I don't have to type the index out over and over
		UIAnchor workingAnchor;
		GameObject linkedObject = null;

		// Go through all anchors
		for (int i = 0; i < anchors.Count; i++)
		{
			workingAnchor = anchors[i];

			// For each stretch we found on the object, go through their list of game objects and check if we're linked to one. if we are, it's a dependent anchor
			foreach (KeyValuePair<UIStretch, List<Transform>> stretchPair in stretchesAndChildren)
			{
				if (workingAnchor.widgetContainer != null)
				{
					linkedObject = workingAnchor.widgetContainer.gameObject;
				}
				else if (workingAnchor.panelContainer != null)
				{
					linkedObject = workingAnchor.panelContainer.gameObject;
				}
				else
				{
					continue;
				}

				// If the anchor is linked to either the stretch gameobject directly or one of it's children...
				if ((linkedObject == stretchPair.Key.gameObject || stretchPair.Value.Contains(linkedObject.transform)) && !stretchPair.Key.dependentAnchors.Contains(workingAnchor))
				{
					stretchPair.Key.dependentAnchors.Add(workingAnchor);
					requiresSave = true;
				}
			}

			// Now see if we have any anchors on anchors!
			for (int j = 0; j < anchors.Count; j++)
			{
				// If it's not the same index and it is actually anchored to something
				if (j != i)
				{
					if (linkedObject == anchors[j].gameObject && !anchors[j].dependentAnchors.Contains(workingAnchor) && !workingAnchor.dependentAnchors.Contains(anchors[j]))
					{
						anchors[j].dependentAnchors.Add(workingAnchor);
						requiresSave = true;
					}

					else if (anchors[j].dependentAnchors.Contains(workingAnchor) && workingAnchor.dependentAnchors.Contains(anchors[j]))
					{
						Debug.LogError("CIRCULAR ANCHOR DEPENDENCY DETECTED! THIS WILL CAUSE CHAOS!");
					}
				}
			}
		}


		UIStretch workingStretch;
		// While unlikely we should make sure that any stretches that are dependent are also linked.
		foreach (KeyValuePair<UIStretch, List<Transform>> stretchPair in stretchesAndChildren)
		{
			workingStretch = stretchPair.Key;
			// Nested foreach seems bad but it's an editor script so whatever
			foreach (KeyValuePair<UIStretch, List<Transform>> otherStretchPair in stretchesAndChildren)
			{
				// Make sure we're not looking at ourself
				if (workingStretch != otherStretchPair.Key)
				{
					if (workingStretch.widgetContainer != null)
					{
						linkedObject = workingStretch.widgetContainer.gameObject;
					}
					else if (workingStretch.panelContainer != null)
					{
						linkedObject = workingStretch.panelContainer.gameObject;
					}
					else
					{
						continue;
					}

					// If the stretch we're checking against(otherStretchPair) has the object 
					// the stretch we're checking with (stretchPair) is linked to,
					// then we must be dependent on them. 
					if (otherStretchPair.Value.Contains(linkedObject.transform) && !otherStretchPair.Key.dependentStretches.Contains(workingStretch))
					{
						otherStretchPair.Key.dependentStretches.Add(workingStretch);
						requiresSave = true;
					}
				}
			}
		}
		return requiresSave;
	}
}

