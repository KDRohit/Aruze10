using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DevGUIMenuFeatureDirector : DevGUIMenu
{
	private Dictionary<string, bool> shouldShowMap;

	public override void drawGuts()
	{
		if (shouldShowMap == null)
		{
			setupFeatures();
		}

		GUILayout.BeginVertical();
		foreach (KeyValuePair<string, FeatureBase> pair in FeatureDirector.features)
		{
			if (string.IsNullOrEmpty(pair.Key))
			{
				continue;
			}
			
			GUILayout.BeginHorizontal();
			GUILayout.Label(pair.Key);


			bool showFeature = false;
			if (shouldShowMap.TryGetValue(pair.Key, out showFeature))
			{
				if (GUILayout.Button(showFeature ? "Hide" : "Show"))
				{
					showFeature = !showFeature;
					shouldShowMap[pair.Key] = showFeature;
				}
				if (showFeature)
				{
					pair.Value.drawGuts();
				}
			}
			else
			{
				shouldShowMap[pair.Key] = false;
			}
			
			GUILayout.EndHorizontal();
		}
		GUILayout.EndVertical();
	}

	private void setupFeatures()
	{
		shouldShowMap = new Dictionary<string, bool>();
		foreach (KeyValuePair<string, FeatureBase> pair in FeatureDirector.features)
		{
			shouldShowMap.Add(pair.Key, false);
		}
	}

	// Implements IResetGame
	new static void resetStaticClassData()
	{
		//no static data need to be reset.
	}

}
