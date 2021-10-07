using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Zynga.Unity.Attributes;

/**
 * Base editor class applied to all Unity objects unless they define their own custom inspector.
 * This default one allows for custom tags handling, for now that includes two types of custom
 * attribute tags FoldoutHeaderGroupAttribute that lets you group serialized fields into drop downs
 * and OmitFromNonDebugInspectorAttribute which will hide serialized fields unless the Inspector
 * window is set to Debug (a bit nicer than HideInInspector which requires a recompile if you want
 * to see what was in that field).  Future custom attribute tags we want to create and use for all
 * object can be added to CommonEditor.drawDefaultInspectorUsingCustomAttributeTags() (although
 * if a class has its own Custom Inspector defined it may need special code added for that new
 * tag type in order to handle them itself).
 *
 * Original Author: Scott Lepthien
 * Creation Date: 8/16/2019
 */
namespace Zynga.Unity
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(MonoBehaviour), true)]
	public class DefaultMonoBehaviourEditor : Editor
	{
		private static Dictionary<UnityEngine.Object, Dictionary<string, bool>> foldoutGroupStatusDictionary = new Dictionary<UnityEngine.Object, Dictionary<string, bool>>();

		public override void OnInspectorGUI()
		{
			Dictionary<string, bool> foldoutStatusEntry = getFoldoutGroupStatusDictionaryEntryForObject(serializedObject.targetObject);

			CommonEditor.drawDefaultInspectorUsingCustomAttributeTags(serializedObject, foldoutStatusEntry);
			serializedObject.ApplyModifiedProperties();

			syncFoldoutGroupStatusDictionaryEntriesForMultiSelect(serializedObject.targetObjects);
		}

		// Check if we have a foldoutGroupStatusDictionary entry created for this serializedObject
		// and if not create one
		protected static Dictionary<string, bool> getFoldoutGroupStatusDictionaryEntryForObject(UnityEngine.Object obj)
		{
			if (!foldoutGroupStatusDictionary.ContainsKey(obj))
			{
				foldoutGroupStatusDictionary.Add(obj, new Dictionary<string, bool>());
				// Make STANDARD_OPTIONS_GROUP expanded by default (if there are no STANDARD_OPTIONS_GROUP entries this setting will just be ignored)
				foldoutGroupStatusDictionary[obj].Add(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP, true);
			}

			return foldoutGroupStatusDictionary[obj];
		}

		// Function to ensure that if someone is multi selecting that all of the foldout flags for the different
		// objects stay synced to each other
		protected static void syncFoldoutGroupStatusDictionaryEntriesForMultiSelect(UnityEngine.Object[] targetObjects)
		{
			if (targetObjects != null && targetObjects.Length > 1)
			{
				// Everything will be synced to the [0] entry since that is the one
				// that was modified by the editor
				Dictionary<string, bool> dictionaryToCopyFrom = foldoutGroupStatusDictionary[targetObjects[0]];
				
				for (int i = 1; i < targetObjects.Length; i++)
				{
					UnityEngine.Object currentObj = targetObjects[i];
					
					if (!foldoutGroupStatusDictionary.ContainsKey(currentObj))
					{
						foldoutGroupStatusDictionary.Add(currentObj, new Dictionary<string, bool>());
					}
					
					Dictionary<string, bool> currentDictionary = foldoutGroupStatusDictionary[targetObjects[i]];

					foreach (KeyValuePair<string, bool> kvp in dictionaryToCopyFrom)
					{
						if (currentDictionary.ContainsKey(kvp.Key))
						{
							currentDictionary[kvp.Key] = kvp.Value;
						}
					}
				}
			}
		}
	}
}
