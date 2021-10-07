using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using TMPro;

// Finder wizard that searches NGUI UI labels and TMP text fields for numerical values above or below thresholds.
// Author: Jake Smith
// Date: 7/5/2018

public class BigSmallNumberFinder : FinderBase 
{
	public int minValueThreshold = 0; 		//UI Labels with # less than this value will be picked up
	public int maxValueThreshold = 1000; 	//UI Labels with # greater than this value will be picked up
	public DefaultTextResult[] results;

	private string bigReplacement = "replacement", smallReplacement = "replacement";

	public override void Awake() 
	{
		searchScene = false;
		assetsPath = SearchPath.CURRENT_SELECTION;
		minSize = new Vector2(this.minSize.x, 400); // So that the optional Replacement feature is more likely to be seen
		displayCurrentSelection();
	}

	protected override bool isValidInput
	{
		get { return minValueThreshold <= maxValueThreshold; }
	}

	new public void OnWizardUpdate() 
	{
		helpString = "Searches paths for NGUI Labels/TMP components with text field values below min (exclusive) or above max (inclusive) thresholds.";
	}

	[MenuItem ("Zynga/Wizards/Find Stuff/Find Big and Small Numbers in Text Labels")] 
	static void CreateWizard()
	{
		ScriptableWizard.DisplayWizard<BigSmallNumberFinder>("Find Big and Small Numbers in Text Labels", "Close", "Find");
	}

	protected override void filterResults(List<GameObject> searchSpace) 
	{
		List<DefaultTextResult> matches = new List<DefaultTextResult>();
		foreach (GameObject searchItem in searchSpace)
		{
			Object component = searchItem.GetComponent<UILabel>();
			if (component == null)
			{
				component = searchItem.GetComponent<TextMeshPro>();
			}

			//If textField is neither a UI Label or TextMeshPro, skip this
			if (component == null) 
			{ 
				continue;
			}

			//Guarenteed to be UI Label or TMP. Check text field.

			DefaultTextResult offensive = getBadText(searchItem);
			if (offensive != null)
			{
				matches.Add(offensive);
			}
		}

		results = prepareDefaultTextResults(matches);
	}

	protected DefaultTextResult[] prepareDefaultTextResults(List<DefaultTextResult> dtMatches) 
	{
		//This is a slightly modified version of FinderBase::prepareSearchResultsDisplay to handle DefaultTextResult[]
		DefaultTextResult[] dtResults = dtMatches.ToArray();
		
		for (int i = 0; i < dtResults.Length; i++)
		{
			GameObject go = dtResults[i].gameObject as GameObject;
			if (go != null)
			{
				dtResults[i].gameObject = go;
				// TODO:UNITY2018:nestedprefab:confirm
				if (PrefabUtility.GetCorrespondingObjectFromSource(go) == null && PrefabUtility.GetPrefabObject(go) != null)
				{
					// This is a prefab (not an object in the scene). Find the root object.
					while (go.transform.parent != null)
					{
						go = go.transform.parent.gameObject;
					}
				
					dtResults[i].gameObjectRoot = go;
				} 
			}
		}
		return dtResults;
	}

	protected DefaultTextResult getBadText(GameObject match) 
	{
		DefaultTextResult result;
		UILabel textField = match.GetComponent<UILabel>();
		
		if (textField != null)
		{
			result = new DefaultTextResult(
				match,
				determineOffenseType(textField.text),
				DefaultTextResult.TextLabelType.NGUI,
				textField.text
			);
		}
		else
		{
			TextMeshPro tmpField = match.GetComponent<TextMeshPro>();
			result = new DefaultTextResult(
				match, 
				determineOffenseType(tmpField.text),
				DefaultTextResult.TextLabelType.TMP,
				tmpField.text
			);
		}

		return result.offenseType != DefaultTextResult.OffenseType.NONE ? result : null;
	}

	protected override bool DrawWizardGUI()	
	{
		base.DrawWizardGUI();
		if (results != null && results.Length > 0)
		{
			drawReplacementGUI();
		}
		return false;
	}

	private void drawReplacementGUI()
	{
		GUILayout.Label("Replace Violations", EditorStyles.boldLabel);
		GUILayout.Label("Note: Saves all dirty files in the asset database");

		bigReplacement = EditorGUILayout.TextField("Replace BIG labels with:", bigReplacement);
		if (GUILayout.Button("Replace Big Values", GUILayout.Width(130), GUILayout.Height(20)))
		{
			replaceLabelContent(DefaultTextResult.OffenseType.BIG, bigReplacement);
		}

		smallReplacement = EditorGUILayout.TextField("Replace SMALL labels with:", smallReplacement);		
		if (GUILayout.Button("Replace Small Values", GUILayout.Width(130), GUILayout.Height(20)))
		{
			replaceLabelContent(DefaultTextResult.OffenseType.SMALL, smallReplacement);
		}
	}

    private void replaceLabelContent(DefaultTextResult.OffenseType targetOffenseType, string replacement)
	{
		int replacementCount = 0;
		for (int i = 0; i < results.Length; i++)
		{
			if (results[i].offenseType == targetOffenseType)
			{
				if (results[i].labelType == DefaultTextResult.TextLabelType.NGUI)
				{
					UILabel label = results[i].gameObject.GetComponent<UILabel>();
					label.text = replacement;
				}
				else if (results[i].labelType == DefaultTextResult.TextLabelType.TMP)
				{
					TextMeshPro tmp = results[i].gameObject.GetComponent<TextMeshPro>();
					tmp.text = replacement;
				}
				replacementCount++;
				EditorUtility.SetDirty(results[i].gameObject);
			}
		}

		if (replacementCount > 0)
		{
			AssetDatabase.SaveAssets();
		}

		Debug.Log("Replaced " + replacementCount + " labels with \"" + replacement + "\"");
	}

	private DefaultTextResult.OffenseType determineOffenseType(string text) 
	{
		double labelValue;
		if (double.TryParse(text.Replace(",", ""), out labelValue))
		{
			if (labelValue < minValueThreshold)
			{
				return DefaultTextResult.OffenseType.SMALL;
			} 
			else if (labelValue >= maxValueThreshold)
			{
				return DefaultTextResult.OffenseType.BIG;
			}
		}
		return DefaultTextResult.OffenseType.NONE;
	}

	[System.Serializable]
	public class DefaultTextResult 
	{
		public enum TextLabelType {NGUI, TMP};
		public enum OffenseType {NONE, BIG, SMALL};

		public GameObject gameObject;
		public GameObject gameObjectRoot;
		public OffenseType offenseType;
		public TextLabelType labelType;
		[SerializeField] private string offensiveString;

		public DefaultTextResult(GameObject result, OffenseType offense, TextLabelType label, string offensiveString)
		{
			this.gameObject = result;
			this.offenseType = offense;
			this.labelType = label;
			this.offensiveString = offensiveString;
		}
	}
}
