using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Contains functions related to TextMeshPro, but are not extension methods (See TMProExtensions.cs for those).
*/

public static class TMProFunctions
{
	// Sets alpha to all child TextMeshPro objects.
	public static void fadeGameObject(GameObject obj, float alpha, bool includeInActive = false)
	{
		if (obj == null)
		{
			return;
		}
		
		List<TextMeshPro> list = new List<TextMeshPro>();
		list.AddRange(obj.GetComponentsInChildren<TextMeshPro>(includeInActive));

		if (list == null)
		{
			return;
		}

		foreach(TextMeshPro label in list)
		{
			label.alpha = Mathf.Clamp01(alpha);
		}
	}

	/// Returns a mapping of the alpha values to TMPro labels on a GameObject, allowing them to be restored back to default if they are changed
	public static Dictionary<TextMeshPro, float> getAlphaValueMapForGameObject(GameObject gameObject, bool includeInActive = false)
	{
		Dictionary<TextMeshPro, float> alphaMap = new Dictionary<TextMeshPro, float>();

		foreach (TextMeshPro label in gameObject.GetComponentsInChildren<TextMeshPro>(includeInActive))
		{
			alphaMap.Add(label, label.alpha);
		}

		return alphaMap;
	}

	/// Restores alpha values from a map created by calling getAlphaValueMapForUIGameObject()
	public static void restoreAlphaValuesToGameObjectFromMap(GameObject gameObject, Dictionary<TextMeshPro, float> alphaMap, float multiplier = 1.0f)
	{
		foreach (TextMeshPro label in gameObject.GetComponentsInChildren<TextMeshPro>(true))
		{
			if (alphaMap.ContainsKey(label))
			{
				label.alpha = alphaMap[label] * multiplier;
			}
		}
	}

	/// Restores alpha values from a map created by calling getAlphaValueMapForGameObject(), performed over a set duration
	public static IEnumerator restoreAlphaValuesToGameObjectFromMapOverTime(GameObject gameObject, Dictionary<TextMeshPro, float> alphaMap, float duration)
	{
		// gather a list of labels so we don't have to grab stuff every update loop
		List<TextMeshPro> textMeshProList = new List<TextMeshPro>();
		foreach (TextMeshPro label in gameObject.GetComponentsInChildren<TextMeshPro>(true))
		{	
			textMeshProList.Add(label);		
		}
		
		float elapsedTime = 0;
		
		while (elapsedTime < duration)
		{
			elapsedTime += Time.deltaTime;
			
			foreach (TextMeshPro label in textMeshProList)
			{
				if (alphaMap.ContainsKey(label))
				{
					label.alpha = alphaMap[label] * (elapsedTime / duration);
				}
			}
			yield return null;
		}
		
		// ensure all values are set to final amounts
		foreach (TextMeshPro label in textMeshProList)
		{
			if (alphaMap.ContainsKey(label))
			{
				label.alpha = alphaMap[label];
			}
		}
	}
}
