using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AchievementProgress
{
	public long score = 0L;
	public AchievementLevel rank
	{
		get
		{
			AchievementLevel result = AchievementLevel.getLevelFromScore(score);
			if (result == null)
			{
				Debug.LogErrorFormat("AchievementProgress.cs -- rank -- could not find an achievement level from score: {0}. Returning null.", score);
				return null;
			}
			return result;
		}
	}

	private Dictionary<string, long> progressMap = new Dictionary<string, long>();
	private Dictionary<string, long> unlockedMap = new Dictionary<string, long>();

	private HashSet<string> rewardHash = new HashSet<string>();
	public bool isUnlocked(string achievementKey)
	{
		if (unlockedMap.ContainsKey(achievementKey))
		{
			return (unlockedMap[achievementKey] > 0);
		}
		else
		{
			return false;
		}
	}

	public bool isRewardCollected(string achievementKey)
	{
		return rewardHash.Contains(achievementKey);
	}


	// Get all the achievements that user has unlocked but not claimed the reward for
	public List<string> getAchievementsWithRewardsAvailable()
	{
		List<string> achievements = new List<string>();
		foreach(string id in unlockedMap.Keys)
		{
			if (unlockedMap[id] > 0 && !rewardHash.Contains(id))
			{
				achievements.Add(id);
			}
		}
		return achievements;
	}

	public System.DateTime getUnlockedTime(string achievementKey)
	{
		if (unlockedMap.ContainsKey(achievementKey))
		{
			long unlockedTime = unlockedMap[achievementKey];
			double seconds = System.Convert.ToDouble(unlockedTime);
			System.DateTime unlockedDateTime = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
			unlockedDateTime = unlockedDateTime.AddSeconds(seconds);
			return unlockedDateTime;
		}
		else
		{
			return System.DateTime.Now;
		}
	}

	public long getProgress(string achievementKey)
	{
		if (progressMap.ContainsKey(achievementKey))
		{
			return progressMap[achievementKey];
		}
		else
		{
			return 0;
		}
	}

	public int getPercentage(string achievementKey)
	{
		if (progressMap.ContainsKey(achievementKey))
		{
			Achievement achievement = NetworkAchievements.getAchievement(achievementKey);
			if (achievement == null)
			{
				return 0;
			}
		    long progress = progressMap[achievementKey];
			float progressFloat = System.Convert.ToSingle(progress);			
			long goal = achievement.goal;
			float percentage = (progressFloat / goal) * 100f;
			int finalPercentage = Mathf.FloorToInt(percentage);
			if (finalPercentage == 100 && (progress < goal))
			{
				// if the progress is less than the goal, lets make sure that we cap the percentage at 99.
				finalPercentage = 99;
			}
			return finalPercentage;
		}
		else
		{
			return 0;
		}
	}

	public void setRewardCollected(string achievementKey, bool isCollected = true)
	{
		if (isCollected)
		{
			if (!rewardHash.Contains(achievementKey))
			{
				rewardHash.Add(achievementKey);
			}
			else
			{
				Debug.LogWarning("Duplicate reward collected");
			}
		}
		else
		{
			if (rewardHash.Contains(achievementKey))
			{
				rewardHash.Remove(achievementKey);
			}
			else
			{
				Debug.LogWarning("Reward hasn't been collected");
			}
		}
		
		
	}

	public void setProgress(string achievementKey, long progress)
	{
		// Just assign here so we override any existing values.
		progressMap[achievementKey] = progress;
	}

	public void setUnlock(string achievementKey, bool isUnlocked)
	{
		// Just assign here so we override any existing values.
		long unlockedTime = isUnlocked ? GameTimer.currentTime : -1;
		setUnlock(achievementKey, unlockedTime);

	}

	public void setUnlock(string achievementKey, long unlockedTime)
	{
		// Just assign here so we override any existing values.
		unlockedMap[achievementKey] = unlockedTime;
	}

	public void setScore(long newScore)
	{
		score = newScore;
	}

	public void incrementScore(long delta)
	{
		score += delta;
	}

	public AchievementProgress(JSON data, long score, SocialMember member)
	{
		this.score = score;
		readData(data, member);
	}

	public void update(JSON data, long newScore, SocialMember member)
	{
		readData(data, member);
		score = newScore;
	}
	
	public void readData(JSON data, SocialMember member)
	{
		foreach (NetworkAchievements.Sku skuType in System.Enum.GetValues(typeof(NetworkAchievements.Sku)))
		{
			// We only care about the SKU for accessing 
			string sku = NetworkAchievements.skuToString(skuType);
			JSON[] skuAchievements = data.getJsonArray(sku);
			for (int i = 0; i < skuAchievements.Length; i++)
			{
				string id = skuAchievements[i].getString("id", "");
				if (!string.IsNullOrEmpty(id))
				{
					long progress = skuAchievements[i].getLong("progress", 0L);
					long unlockedTime = skuAchievements[i].getLong("unlocked", -1);
					Achievement achievement = NetworkAchievements.getAchievement(id);
					
					if (skuType == NetworkAchievements.Sku.NETWORK && member.isUser)
					{
						if (achievement != null)
						{
							// If this is a network achievement, we need to grab the progress from other achievements.
							progress = 0L; // Reset the progress to 0.
							Achievement linkedAchievement;
							for (int j = 0; j < achievement.dataMetricPairs.Count; j++)
							{
								KeyValuePair<string, string> pair = achievement.dataMetricPairs[j];
								linkedAchievement = NetworkAchievements.getAchievement(pair.Key);
								if (linkedAchievement != null && linkedAchievement.isUnlocked(member))
								{
									// If this is unlocked for that user, then increment the progress.
									progress++;
								}
							}
						}
						else
						{
							Debug.LogErrorFormat("AchievementProgress.cs -- readData() -- could not find the achievement from this id: {0}", id);
						}
					}

					setProgress(id, progress);
					setUnlock(id, unlockedTime);

					if (member.isUser && achievement != null)
					{
						// If the achievement was found and this is the current user's data,
						// let's update the private tracked progress here.
						achievement.trackedProgress = progress;
					}
				}
				else
				{
					Debug.LogErrorFormat("AchievementProgress.cs -- readData -- found null id when parsing progress data.");
				}
			}
		}
	}
}

