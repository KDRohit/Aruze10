using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Zynga.Zdk;

#if UNITY_EDITOR
using UnityEditor;
#endif

/*
Game Network Achievements dev panel.
*/

public class DevGUIMenuNetworkAchievements : DevGUIMenu
{

	private Dictionary<string, bool> _shouldShowSkuAchievementsMap;
	public Dictionary<string, bool> shouldShowSkuAchievementsMap
	{
		get
		{
			if (_shouldShowSkuAchievementsMap == null)
			{
				_shouldShowSkuAchievementsMap = new Dictionary<string, bool>();
				
				foreach (NetworkAchievements.Sku skuType in System.Enum.GetValues(typeof(NetworkAchievements.Sku)))
				{
					_shouldShowSkuAchievementsMap[NetworkAchievements.skuToString(skuType)] = false;
				}
			}
			return _shouldShowSkuAchievementsMap;
		}
	}
	private string newScore = "";
	
	public override void drawGuts()
	{
		GUILayout.BeginVertical();
		if (SlotsPlayer.instance != null &&
			SlotsPlayer.instance.socialMember != null &&
			SlotsPlayer.instance.socialMember.achievementProgress != null)
		{
			GUILayout.Label(string.Format("Player Score: {0}", SlotsPlayer.instance.socialMember.achievementProgress.score));
		}


#if UNITY_EDITOR
		if (NetworkAchievements.dataJSON == null)
		{
			GUILayout.Label("No Achievement JSON data....");
		}
		else if (GUILayout.Button("Copy Achievements JSON"))
		{
			EditorGUIUtility.systemCopyBuffer = NetworkAchievements.dataJSON.ToString();
		}
#endif

		GUILayout.BeginHorizontal();
		newScore = GUILayout.TextField(newScore.ToString());
		if (GUILayout.Button("Set Score"))
		{
			try
			{
				long scoreNum = 0L;
				long.TryParse(newScore, out scoreNum);
				NetworkAchievementAction.setAchievementScore(scoreNum);
			}
			catch(System.Exception e)
			{
				Debug.LogErrorFormat("DevGUIMenuGameNetwork.cs -- Set Score -- failed to parse {0} as a long with Exception: {1}", newScore, e.ToString());
			}
		}
		GUILayout.EndHorizontal();

		
		if (GUILayout.Button("Unlock LL Trophy"))
		{
			Achievement networkAchievement = NetworkAchievements.getAchievement("network_jackpot_1");
			if (networkAchievement != null)
			{
				DevGUI.isActive = false;
				AchievementsRewardDialog.showDialog(networkAchievement, true);
			}
			else
			{
				Debug.LogErrorFormat("DevGUIMenuNetworkAchievements.cs -- drawGuts() -- could not find achievement network_login_1");
			}
		}
		GUILayout.Label("Achievement Ranks:");
		foreach (KeyValuePair<int, AchievementLevel> pair in AchievementLevel.allLevels)
		{
			GUILayout.Label(string.Format("Rank: {0} -- {1} -- requiredPoints: {2}", pair.Key, pair.Value.name, pair.Value.requiredScore));
		}

		if (GUILayout.Button("Get Achievements"))
		{
			NetworkAchievements.getAchievementsForUser(SlotsPlayer.instance.socialMember);
		}
		GUILayout.Label("Achievements:");
		foreach (NetworkAchievements.Sku skuType in System.Enum.GetValues(typeof(NetworkAchievements.Sku)))
		{
			string sku = NetworkAchievements.skuToString(skuType);
			GUILayout.BeginHorizontal();
			GUILayout.Label(string.Format("{0} Achievements: ", sku.ToUpper()));

			string buttonLabel = string.Format(shouldShowSkuAchievementsMap[sku] ? "Hide {0}" : "show {0}", sku.ToUpper());
			if (GUILayout.Button(buttonLabel))
			{
				bool current = shouldShowSkuAchievementsMap[sku];
				shouldShowSkuAchievementsMap[sku] = !current;
			}
			GUILayout.EndHorizontal();

			if (shouldShowSkuAchievementsMap[sku])
			{
				if (NetworkAchievements.allAchievements != null && NetworkAchievements.allAchievements.ContainsKey(sku))
				{
					Dictionary<string, Achievement> map = NetworkAchievements.allAchievements[sku];
					if (map != null)
					{
						foreach (KeyValuePair<string, Achievement> pair in map)
						{
							string key = pair.Key;
							Achievement achievement = pair.Value;
							
							GUILayout.BeginHorizontal();

							// Left Side
							if (SlotsPlayer.instance.socialMember.achievementProgress != null)
							{
								GUILayout.BeginVertical();
								GUILayout.Label(key);
								long currentProgress = SlotsPlayer.instance.socialMember.achievementProgress.getProgress(key);
								float progressFloat = GUILayout.HorizontalSlider(System.Convert.ToSingle(currentProgress), 0f, System.Convert.ToSingle(achievement.goal));

								long progress = System.Convert.ToInt64(progressFloat);
							
								SlotsPlayer.instance.socialMember.achievementProgress.setProgress(key, progress);
								if (GUILayout.Button("Set Progress"))
								{
									NetworkAchievementAction.setAchievementProgress(key, progress);
								}
								bool isUnlocked = SlotsPlayer.instance.socialMember.achievementProgress.isUnlocked(key);
								string unlockButtonText = isUnlocked ? "Lock" : "Unlock";
								if (GUILayout.Button(unlockButtonText))
								{
									SlotsPlayer.instance.socialMember.achievementProgress.setUnlock(key, !isUnlocked);
									NetworkAchievementAction.setAchievementUnlocked(key, !isUnlocked);
								}

							
								if (isUnlocked)
								{
									if (!SlotsPlayer.instance.socialMember.achievementProgress.isRewardCollected(achievement.id))
									{
										if (GUILayout.Button("Show Reward Dialog"))
										{
											AchievementsRewardDialog.showDialog(achievement, true);
										}
									}
									else
									{
										if (GUILayout.Button("Reset reward (client only)"))
										{
											SlotsPlayer.instance.socialMember.achievementProgress.setRewardCollected(achievement.id, false);
										}
									}
								}
							
								GUILayout.EndVertical();	
							}

							//Right side
							GUILayout.TextArea(achievement.ToString());
							GUILayout.EndHorizontal();

							List<string> allRarities = NetworkAchievements.getAllRarities();
							if (null != allRarities && allRarities.Count > 1)
							{
								//set rarity
								GUILayout.BeginHorizontal();
								for(int i=0; i<allRarities.Count; ++i)
								{
									string rare = allRarities[i]; 
									if (GUILayout.Button("Make " + rare))
									{
										int percentage = 100 - (allRarities.Count) + i;
										achievement.unlockPercentage = percentage;
										achievement.rarityId = i+1;
									}
								}
								GUILayout.EndHorizontal();
							}
						}
					}
				}
			}
		}
		GUILayout.EndVertical();
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}
