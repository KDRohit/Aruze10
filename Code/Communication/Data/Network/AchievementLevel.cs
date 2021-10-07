using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
  Class: AchievementLevel
  Author: Michael Christensen-Calvin <mchristensencalvin@zynga.com>
  Description: This class holds all the pertinent information for the different ranks players can earn from 
  the Achievements feature. It also provides static methods to access relevant information.
*/
public class AchievementLevel : IResetGame
{
	public string name = "";
	public string url = "";
	public string spriteName = "";
	public long requiredScore = 0L;
	public int rank = 0;

	public static Dictionary<int, AchievementLevel> allLevels = new Dictionary<int, AchievementLevel>();
	private static List<AchievementLevel> sortedLevels = new List<AchievementLevel>();
	private static bool areLevelsPopulated = false;

	public AchievementLevel(JSON data, int index)
	{
		rank = index;
		name = data.getString("name", string.Format("Rank {0}", index));
		url = data.getString("icon", ""); // For displaying a rank we don't have on the client.
		url = Glb.fixupStaticAssetHostUrl(url);
		spriteName = string.Format("Badge Rank {0}", index + 1); // For reading from our atlases (1 - indexed)
		requiredScore = data.getLong("requiredScore", 0L);
	}


	public static AchievementLevel getLevel(int rank)
	{
		if (allLevels.ContainsKey(rank))
		{
			return allLevels[rank];
		}
		else
		{
			Debug.LogErrorFormat("AchievementLevel.cs -- getLevel -- tried to get an AchievementLevel {0}, which doesn't exist", rank);
			return null;
		}
	}

	public static AchievementLevel getLevelFromScore(long score)
	{
		for (int i = sortedLevels.Count - 1; i >= 0; i--)
		{
			if (score >= sortedLevels[i].requiredScore)
			{
				// We are going backwards through the sorted list, so if we are greater than the required score
				// then we have found our level.
				return sortedLevels[i];
			}
		}
		
		if (areLevelsPopulated)
		{
			Debug.LogErrorFormat("AchievementLevel.cs -- getLevelFromScore -- couldn't find the rank from score: {0}. Returning null.", score);
		}
		else
		{
			Debug.LogWarningFormat("AchievementLevel.cs -- getLevelFromScore -- accessing achievement levels before initializaiton. Returning null.");
		}

		return null;
	}

	public static void populateAll(JSON[] levelData)
	{
		for (int i = 0; i < levelData.Length; i++)
		{
			AchievementLevel newLevel = new AchievementLevel(levelData[i], i);
			allLevels.Add(i, newLevel);
			sortedLevels.Add(newLevel);
		}
		sortedLevels.Sort(sortByRequiredScore);
		areLevelsPopulated = true;
	}

	private static int sortByRequiredScore(AchievementLevel one, AchievementLevel two)
	{
		return one.requiredScore.CompareTo(two.requiredScore);
	}

	public static void resetStaticClassData()
	{
		allLevels = new Dictionary<int, AchievementLevel>();
		sortedLevels = new List<AchievementLevel>();
		areLevelsPopulated = false;
	}
}
