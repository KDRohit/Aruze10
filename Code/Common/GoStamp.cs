using UnityEngine;

// Game Object Stamps.
// Please see the documentation in GoStamper.cs.

public class GoStamp : TICoroutineMonoBehaviour
{
	[SerializeField] private GameObject prefab; // This is the stamp prefab.
	[SerializeField] private GameObject go;     // Stamp the game object.

	void Awake()
	{
		if (Application.isPlaying)
		{
			enabled = false;
		}
	}

	public void assignPrefab(GameObject prefab)
	{
		this.prefab = prefab;
	}
	
	public void applyChanges()
	{
#if UNITY_EDITOR
		if (prefab != null && go != null)
		{
			// Apply your changes to the prefab.

			// TODO:UNITY2018:nestedprefab:confirm//old
			// UnityEditor.PrefabUtility.ReplacePrefab(
			// 	go, prefab, UnityEditor.ReplacePrefabOptions.ConnectToPrefab);
			// TODO:UNITY2018:nestedprefab:confirm//old
			UnityEditor.PrefabUtility.SaveAsPrefabAssetAndConnect(go,
				UnityEditor.AssetDatabase.GetAssetPath(prefab),
				UnityEditor.InteractionMode.AutomatedAction);
			
			// Replacing the prefab replaces it, but it also puts it in a weird kind of broken state.
			// You have to save it to fix it, but the only way to save one thing is to save everything.
			
			UnityEditor.AssetDatabase.SaveAssets();
			
			// It forgot the prefab!  Get it from the game object.
			// TODO:UNITY2018:nestedprefab:fix
			prefab = UnityEditor.PrefabUtility.GetPrefabParent(go) as GameObject;

			// See if there's a go stamper on the parent folder.
			// (It might have multiple go stampers, for example,
			// a picking round like lis01 might have a few different kinds of pickems).
			// Figure out which go stamper contains this go stamp.
			
			GoStamper[] goStampers = GetComponentsInParent<GoStamper>();
			GoStamper goStamper = null;
			
			if (goStampers != null)
			{
				// Loop backwards through the go stampers.
				// That way, if none of them explicitly contain the go stamp,
				// then the loop stops on the first (and possibly only) go stamper.
				
				for (int iGoStamper = goStampers.Length - 1; iGoStamper >= 0; iGoStamper--)
				{
					goStamper = goStampers[iGoStamper];
					
					if (goStamper.goStamps != null)
					{
						if (System.Array.IndexOf(goStamper.goStamps, this) != -1)
						{
							break;
						}
					}
				}
			}
			
			if (goStamper != null)
			{
				// It forgot all the links to the prefabs!
				// Assign the prefab to fix the links and to stamp the changes to all the objects.
				
				goStamper.prefab = prefab;
				goStamper.assignPrefab();
			}
		}
#endif
	}
	
	// Stamping is virtual in case you want to create different kinds of stamps.
	// For example, the buttons in a menu may all look the same except for the text.
	// A menu item stamp could have a text tunable, and stamping could stamp the text, too.

	public virtual void stampObject()
	{
		if (prefab != null)
		{
			if (go != null)
			{
				deleteObject();
			}

			go = CommonGameObject.instantiate(prefab) as GameObject;

			go.name = go.name.Replace("Stamp", "Object");
			go.name = go.name.Replace("(Clone)","");
			
			go.transform.parent = transform;
			go.transform.localPosition = Vector3.zero;
			go.transform.localScale = Vector3.one;
		}
	}

	public void deleteObject()
	{
		if (go != null)
		{
			DestroyImmediate(go);
		}
	}
}
