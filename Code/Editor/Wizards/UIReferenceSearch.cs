using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.Reflection;
using System.Linq;


/**
Finds UI references in key paths in the project.
*/
public class UIReferenceSearch : ScriptableWizard
{
	[System.Serializable] public class HitReferencer
	{
		public string path;
		public GameObject pointer;
		public GameObject root;
	}
	
	[System.Serializable] public class HitResult
	{
		public string name;
		public HitReferencer[] referencers;
	}
	
	/// A list of paths we want to search.
	/// Be careful not to add a path that is within another path also in the list.
	public string[] pathsToSearch = new string[]
	{
		"Assets/Data/Common/",
		"Assets/Data/HIR/"
	};
	
	public UIAtlas atlasToBeReplaced;
	public UIAtlas atlasReplacement;
	
	public HitResult[] atlasHits;			///< A list of the stored search results
	public HitResult[] fontHits;			///< A list of the stored search results
	
	[MenuItem ("Zynga/Wizards/Find Stuff/Find UI References")]static void CreateWizard()
	{
		ScriptableWizard.DisplayWizard<UIReferenceSearch>("Find UI References", "Close", "Search");
	}
	
	public void OnWizardUpdate()
	{
		string help = "Click 'Search' to scan the following paths for UI references:\n";
		foreach (string path in pathsToSearch)
		{ 
			help += "    " + path + "\n";
		}
		help += "\nLeave 'Atlas To Be Replaced' and 'Atlas Replacement' fields blank to leave prefabs unmodified.";
		
		helpString = help;
	}
	
	/// Clicked 'Close'
	public void OnWizardCreate()
	{
	
	}
	
	/// Clicked 'Search'
	public void OnWizardOtherButton()
	{
		bool doAtlasReplacement = atlasToBeReplaced != null && atlasReplacement != null;
	
		// It's our standard to not use SortedDictionary, but since this is an editor wizard, it's ok.
		SortedDictionary<string, List<GameObject>> atlasResults = new SortedDictionary<string, List<GameObject>>();
		SortedDictionary<string, List<GameObject>> fontResults = new SortedDictionary<string, List<GameObject>>();

		Dictionary<GameObject, string> searchSpace = new Dictionary<GameObject, string>();
		List<GameObject> allPrefabs = new List<GameObject>();
		
		foreach (string path in pathsToSearch)
		{
			allPrefabs.AddRange(CommonEditor.gatherPrefabs(path));
		}
		
		// Build the search space by including all the children of each prefab
		foreach (GameObject prefab in allPrefabs)
		{
			List<GameObject> allChildren = CommonGameObject.findAllChildren(prefab, true);
			string prefabPath = AssetDatabase.GetAssetPath(prefab);
			foreach (GameObject child in allChildren)
			{
				expandSearchSpace(searchSpace, child, prefabPath);
			}
		}
		
		// Scan the search space for references
		foreach (KeyValuePair<GameObject, string> p in searchSpace)
		{
			GameObject testObject = p.Key;
			string testObjectPath = p.Value;
			
			MonoBehaviour[] scripts = testObject.GetComponents<MonoBehaviour>() as MonoBehaviour[];
		
			// Check every attached behaviour to see if it has something of interest.
			// We use the base classes of UISprite and UILabel to catch all the others.
			foreach (MonoBehaviour script in scripts)
			{
				if (script is UISprite)
				{
					UISprite sprite = (UISprite)script;
					if (sprite.atlas == null)
					{
						tally(atlasResults, "<null>", testObject);
					}
					else
					{
						if (doAtlasReplacement && sprite.atlas == atlasToBeReplaced)
						{
							sprite.atlas = atlasReplacement;
							EditorUtility.SetDirty(sprite);
						}
					
						string atlasPath = AssetDatabase.GetAssetPath(sprite.atlas);
						if (string.IsNullOrEmpty(atlasPath))
						{
							atlasPath = sprite.atlas.name;
						}
					
						tally(atlasResults, atlasPath, testObject);
					}
				}
				else if (script is UILabel)
				{
					UILabel label = (UILabel)script;
					if (label.font == null)
					{
						tally(fontResults, "<null>", testObject);
					}
					else
					{
						string fontPath = AssetDatabase.GetAssetPath(label.font);
						if (string.IsNullOrEmpty(fontPath))
						{
							fontPath = label.font.name;
						}
				
						tally(fontResults, fontPath, testObject);
					}
				}
			}
		}
		
		// Populate atlas search hits
		int totalHitCount = 0;
		
		atlasHits = new HitResult[atlasResults.Count];
		totalHitCount += populateHits(atlasHits, atlasResults);
		
		fontHits = new HitResult[fontResults.Count];
		totalHitCount += populateHits(fontHits, fontResults);
		
		Debug.Log("Done searching " + allPrefabs.Count + " prefabs, got " + totalHitCount + " total hits.");
	}
	
	/// Tally up some results in a SortedDictionary, adding keys as needed
	private static void tally(SortedDictionary<string, List<GameObject>> results, string key, GameObject obj)
	{
		if (!results.ContainsKey(key))
		{
			results.Add(key, new List<GameObject>());
		}
		results[key].Add(obj);
	}
	
	/// Populate up the results structure for easy inspector-ish viewing
	private static int populateHits(HitResult[] hits, SortedDictionary<string, List<GameObject>> results)
	{
		// Loop variables at this scope for speed optimization
		string name;
		List<GameObject> referencers;
		HitResult result;
		HitReferencer referencer;
		Transform transform;
		string path;
	
		int hitCount = 0;
		int i = 0;
		
		foreach (KeyValuePair<string, List<GameObject>> p in results)
		{
			name = p.Key;
			referencers = p.Value;
			
			result = new HitResult();
			result.name = name;
			result.referencers = new HitReferencer[referencers.Count];
			
			// Check all referencers
			for (int j = 0; j < referencers.Count; j++)
			{
				referencer = new HitReferencer();
				referencer.pointer = referencers[j];
				
				// Follow the hierachy upward
				transform = referencer.pointer.transform;
				path = transform.name;
				while (transform.parent != null)
				{
					transform = transform.parent;
					path = transform.name + "/" + path;
				}
				
				referencer.path = path;
				referencer.root = transform.gameObject;
				
				result.referencers[j] = referencer;
			}
			
			// Count up results
			hitCount += result.referencers.Length;
			hits[i++] = result;
		}
		
		return hitCount;
	}
	
	private static void expandSearchSpace(Dictionary<GameObject, string> searchSpace, GameObject child, string prefabPath)
	{
		if (!searchSpace.ContainsKey(child))
		{
			
			MonoBehaviour[] scripts = null;
			
			try
			{
				scripts = child.GetComponents<MonoBehaviour>() as MonoBehaviour[];
			}
			catch (System.Exception)
			{
				// The below happens a lot, citing uninitialized variables, commented out the error for now.
				//Debug.LogErrorFormat("ERROR in '{0}'\n{1}", prefabPath, ex.ToString());
			}
			
			if (scripts != null)
			{
				searchSpace.Add(child, prefabPath);
				
				foreach (MonoBehaviour script in scripts)
				{
					if (script != null)
					{
						// Look for prefabs referenced in this MonoBehaviour
						var bindingFlags = BindingFlags.Instance |
							BindingFlags.NonPublic |
							BindingFlags.Public;
				
						var fieldValues = script.GetType()
							.GetFields(bindingFlags)
							.Select(field => field.GetValue(script))
							.ToList();
				
						foreach (object nestedObject in fieldValues)
						{
							if (nestedObject is GameObject && nestedObject != null)
							{
								expandSearchSpace(searchSpace, nestedObject as GameObject, prefabPath);
							}
						}
					}
				}
			}
		}
	}
}
