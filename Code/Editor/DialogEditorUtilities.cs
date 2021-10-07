using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;

/**
 * Class for making utility functions for dialogs that can be run in the Unity Editor from the Menu
 *
 * Creation Date: 11/26/2019
 * Original Author: Scott Lepthien
 */
public static class DialogEditorUtilities
{
	// Helper function for UI that will be able to determine if a dialog has a close button
	// which can be detected by ZAP.  This should help debug why a dialog isn't being auto closed.
	[MenuItem("Zynga/Assets/Dialog/Detect Automated Close Button on Selected Dialog(s)")]
	public static void detectAutomatedCloseButtonOnSelectedDialogs()
	{
		if (Selection.gameObjects != null)
		{
			StringBuilder outputLog = new StringBuilder();
			outputLog.Append("DialogEditorUtilities.detectAutomatedCloseButtonOnSelectedDialogs() - Results:\n");
			
			foreach (GameObject go in Selection.gameObjects)
			{
				try
				{
					bool isPrefabChanged = false;
					string prefabAssetPath = null;
					GameObject rootObject = go;

					bool isObjectAPrefab = false;
					bool isInPrefabStage = false;
					if (PrefabStageUtility.GetCurrentPrefabStage() != null && PrefabStageUtility.GetPrefabStage(go) != null)
					{
						isObjectAPrefab = true;
						isInPrefabStage = true;
					}
					else
					{
						isObjectAPrefab = PrefabUtility.IsPartOfAnyPrefab(go);
					}

					if (isObjectAPrefab)
					{
						bool isSceneInstance = PrefabUtility.IsPartOfNonAssetPrefabInstance(go);

						if (!isSceneInstance)
						{
							// Either part of prefab asset or nested prefab instance.  Unpack prefab to edit and save it
							// afterward.
							bool isImmutablePrefab = PrefabUtility.IsPartOfImmutablePrefab(go);
							bool isVariantPrefab = PrefabUtility.IsPartOfVariantPrefab(go);
							if (isImmutablePrefab || isVariantPrefab)
							{
								Debug.LogWarningFormat(go, "Cannot modify immutable/variant prefab {0}.", go);
								continue;
							}

							if (isInPrefabStage)
							{
								prefabAssetPath = PrefabStageUtility.GetPrefabStage(go).prefabAssetPath;
							}
							else
							{
								// Check for whether go is itself a prefab.
								PrefabInstanceStatus instanceStatus = PrefabUtility.GetPrefabInstanceStatus(go);
								PrefabAssetType assetType = PrefabUtility.GetPrefabAssetType(go);
								// This API is confusing but this seems to be the only way to figure this out now.
								bool goIsPrefab = assetType != PrefabAssetType.NotAPrefab && instanceStatus == PrefabInstanceStatus.NotAPrefab;
								GameObject prefabObject = goIsPrefab ? go : PrefabUtility.GetCorrespondingObjectFromSource(go);
								if (prefabObject == null)
								{
									Debug.LogWarningFormat(go, "Cannot find prefab for {0}.", go);
									continue;
								}
								prefabAssetPath = AssetDatabase.GetAssetPath(prefabObject);
							}
						
							rootObject = PrefabUtility.LoadPrefabContents(prefabAssetPath);
						}
					}

					DialogBase[] dialogBases = rootObject.GetComponentsInChildren<DialogBase>(true);

					foreach (DialogBase dialog in dialogBases)
					{
						GameObject closeButtonForDialog = dialog.getCloseButtonGameObjectForDialog();
						if (closeButtonForDialog != null)
						{
							outputLog.Append("dialog.gameObject.name = " + dialog.gameObject.name + "; Close button object name: " + closeButtonForDialog.name + "\n");
						}
						else
						{
							outputLog.Append("dialog.gameObject.name = " + dialog.gameObject.name + "; Close button was NULL!");
						}
					}
				}
				catch (System.ArgumentException e)
				{
					outputLog.AppendLine(e.Message);
				}
			}
			// Output would be too long to output to the console, so will output to a file
			CommonEditor.outputStringToFile("Assets/-Temporary Storage-/Tool Output/DialogUtils_Find_Close_Button.txt", outputLog.ToString(), "Searching for close button on dialogs, output to file.", LogType.Log, false, false);
		}
	}
}
