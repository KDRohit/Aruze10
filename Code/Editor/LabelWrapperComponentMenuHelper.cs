using UnityEditor;
using UnityEngine;
using System.Collections;
using TMPro;

/**
Provides support for attaching GenericLabelComponents to objects that have labels
*/
public static class LabelWrapperComponentMenuHelper 
{
	// Goes through all selected object and child objects and searches for NGUI or TextMeshPro objects to attach a LabelWrapperComponent to
	[MenuItem("Zynga/Assets/Add LabelWrapperComponent To All Text Labels")]
	public static void addLabelWrapperComponentToAllTextLabels()
	{
		if (Selection.gameObjects != null)
		{
			foreach (GameObject selectedObject in Selection.gameObjects)
			{
				bool isChanged = false;

				TextMeshPro[] textMeshPros = selectedObject.GetComponentsInChildren<TextMeshPro>(true);

				foreach (TextMeshPro textMeshPro in textMeshPros)
				{
					LabelWrapperComponent labelWrapperComp = textMeshPro.gameObject.GetComponent<LabelWrapperComponent>();
					if (labelWrapperComp == null)
					{
						labelWrapperComp = textMeshPro.gameObject.AddComponent<LabelWrapperComponent>();
						labelWrapperComp.tmProLabel = textMeshPro;
					}
					else
					{
						// if label wrapper is missing a label ref hook this to the already connected LabelWrapperComponent
						labelWrapperComp.tmProLabel = textMeshPro;
					}
					isChanged = true;
				}

				UILabel[] nguiLabels = selectedObject.GetComponentsInChildren<UILabel>(true);

				foreach (UILabel nguiLabel in nguiLabels)
				{
					LabelWrapperComponent labelWrapperComp = nguiLabel.gameObject.GetComponent<LabelWrapperComponent>();
					if (labelWrapperComp == null)
					{
						labelWrapperComp = nguiLabel.gameObject.AddComponent<LabelWrapperComponent>();
						labelWrapperComp.nguiLabel = nguiLabel;
					}
					else
					{
						// hook this to the already connected LabelWrapperComponent
						labelWrapperComp.nguiLabel = nguiLabel;
					}
					isChanged = true;
				}

				if(isChanged)
				{
					EditorUtility.SetDirty (selectedObject);
				}
			}
		}
	}
}
