using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Achievement
{
	public string id = "";
	public string name = "";
	public string description = "";
	public long goal = 0L;
	public int score = 0;
	public List<KeyValuePair<string, string>> dataMetricPairs;
	public string trophyURL = "";
	public string localURL = "";
	public int version = 0;
	public int rarityId;
	public int reward;
	public float unlockPercentage;
	public NetworkAchievements.Sku sku;

    public long trackedProgress = 0L; // This is the tracked progress of the current playing user.

	private const string LOCAL_URL_FORMAT = "trophies/{0}/{1}";
	
	public Achievement(JSON data, NetworkAchievements.Sku sku)
	{
		this.sku = sku;
		id = data.getString("id", "");
		name = data.getString("name", "");
		description = data.getString("description", "");
		goal = data.getLong("goal", 0L);
		score = data.getInt("score", 0);
		rarityId = data.getInt("rarityId", 0); //defaulting to 0 as actual rarities start at 1
		reward = data.getInt("reward", 0);
		unlockPercentage = data.getFloat("unlockPercentage", 0);

		dataMetricPairs = new List<KeyValuePair<string, string>>();

		string dataMetricString = data.getString("dataMetric", "");
		string[] pairs = dataMetricString.Split(',');
		for (int i = 0; i < pairs.Length; i++)
		{
			int index = pairs[i].IndexOf(':');
			if (index >= 0)
			{
				string key = pairs[i].Substring(0, index);
				string value = pairs[i].Substring(index + 1);
				dataMetricPairs.Add(new KeyValuePair<string, string>(key, value));
			}
			else
			{
				dataMetricPairs.Add(new KeyValuePair<string, string>(pairs[i], ""));
			}
		}

		registerTracking();
		
		version = data.getInt("version", 0);
		trophyURL = data.getString("trophy", "");
		trophyURL = Glb.fixupStaticAssetHostUrl(trophyURL);
		localURL = getLocalURLFromRemoteURL(trophyURL);

	}


	private string getLocalURLFromRemoteURL(string remoteURL)
	{
		int lastToken = remoteURL.LastIndexOf("/");
		if (lastToken > 0)
		{
			// Grab the final token and then process it.
			string result = remoteURL.Substring(lastToken + 1);

			int index = result.IndexOf(".png");
			if (index >= 0)
			{
				// If the url contains .png, but does not end with it,
				// then in this context is has the weird versioning after the extenstion
				// (e.g. SWF/images/etc..../BestPartner_00.png-1
				// Lets strip that off
				result = result.Substring(0, index);
			}

			return string.Format(LOCAL_URL_FORMAT, NetworkAchievements.skuToString(sku), result);
		}
		else
		{
			Debug.LogWarningFormat("Achievement.cs -- getLocalURLFromRemoteURL -- didnt find any / in the url: {0}, so we couldn't parse out the local url. Trophy name is: {1}", remoteURL, name);
			return "";
		}		
	}

	
	private void registerTracking()
	{
		List<string> usedKeys = new List<string>();
		if (dataMetricPairs != null)
		{
			for (int i = 0; i < dataMetricPairs.Count; i++)
			{
				KeyValuePair<string, string> pair = dataMetricPairs[i];
			    if (!usedKeys.Contains(pair.Key))
				{
					// If we aren't already registered for this event, register now.
					switch(pair.Key)
					{
						case "credits":
							GameEvents.onCreditsWon += trackCreditsWon;
							break;
						case "spin":
							GameEvents.onSpin += trackSpin;
							break;
					}
					usedKeys.Add(pair.Key);
				}
			}
		}
	}

	private void trackCreditsWon(long amount, string gameKey, string gameGroup)
	{
		long adjustedAmount = CreditsEconomy.multipliedCredits(amount);
		if (dataMetricPairs != null)
		{
			for (int i = 0; i < dataMetricPairs.Count; i++)
			{
				KeyValuePair<string, string> pair = dataMetricPairs[i];
				if (pair.Key == "credits" &&
					(pair.Value == gameKey || pair.Value == "any" || pair.Value == gameGroup))
				{
					// If we care about this event, update the progress then return so we don't update twice.
					if (trackedProgress < goal)
					{
						// Only do this tracking if we havent achieved the goal already and sent a request.
						trackedProgress += adjustedAmount;
						if (trackedProgress >= goal)
						{
							NetworkAchievements.getAchievementsForUser(SlotsPlayer.instance.socialMember);
						}
						return;						
					}
				}
			}
		}
	}

	private void trackSpin(string gameKey, string gameGroup)
	{
		if (dataMetricPairs != null)
		{
			for (int i = 0; i < dataMetricPairs.Count; i++)
			{
				KeyValuePair<string, string> pair = dataMetricPairs[i];
				if (pair.Key == "spins" &&
					(pair.Value == gameKey || pair.Value == "any" || pair.Value == gameGroup))
				{
					// If we care about this event, update the progress then return so we don't update twice.
					if (trackedProgress < goal)
					{
						// Only do this tracking if we havent achieved the goal already and sent a request.
						trackedProgress++;
						if (trackedProgress >= goal)
						{
							NetworkAchievements.getAchievementsForUser(SlotsPlayer.instance.socialMember);
						}
						return;						
					}
				}
			}
		}
	}

	public bool isUnlockedNotSeen
	{
		get
		{
			return NetworkAchievements.isUnlockedNotSeen(this);
		}
	}

	public bool isUnlockedNotClicked
	{
		get
		{
			return NetworkAchievements.isUnlockedNotClicked(this);
		}
	}	
	
	public bool isNew
	{
		get
		{
			return NetworkAchievements.newAchievements.Contains(this);
		}
		set
		{
			if (value)
			{
				// We don't care if you try to set this to true.
				return;
			}
			// If we are marking this as new then we should remove this from the newAchievements.
			NetworkAchievements.markAchievementSeen(this);
		}
	}

	public System.DateTime getUnlockedTime(SocialMember member = null)
	{
		if (member == null)
		{
			member = SlotsPlayer.instance.socialMember;
		}

		if (member == null || member.achievementProgress == null)
		{
			return System.DateTime.Now;
		}
		return member.achievementProgress.getUnlockedTime(id);
	}

	// Shortcut to get whether an achievment is unlocked for a specific user (defaults to player).
	public bool isUnlocked(SocialMember member = null)
	{
		if (member == null)
		{
			member = SlotsPlayer.instance.socialMember;
		}
		
		if (member == null)
		{
			return false;
		}
		
		if (member.networkProfile != null && member.networkProfile.displayAchievement != null && member.networkProfile.displayAchievement.id == id)
		{
			return true;
		}

		if (member.achievementProgress == null)
		{
			return false;
		}
		return member.achievementProgress.isUnlocked(id);
	}

	public bool hasCollectedReward(SocialMember member = null)
	{
		if (member == null)
		{
			member = SlotsPlayer.instance.socialMember;
		}
		
		if (member == null)
		{
			return false;
		}

		if (member.achievementProgress == null)
		{
			return false;
		}
		return member.achievementProgress.isRewardCollected(id);
	}
	
	// Shortcut to get the progress of an achievment for a specific user (defaults to player).
	public long getProgress(SocialMember member = null)
	{
		if (member == null)
		{
			member = SlotsPlayer.instance.socialMember;
		}

		if (member == null || member.achievementProgress == null)
		{
			return 0L;
		}
		return member.achievementProgress.getProgress(id);
	}

	public int getPercentage(SocialMember member = null)
	{
		if (member == null)
		{
			member = SlotsPlayer.instance.socialMember;
		}

		if (member == null || member.achievementProgress == null)
		{
			return 0;
		}
		return member.achievementProgress.getPercentage(id);
	}

	public bool isUnlockedForSku(SocialMember member, NetworkAchievements.Sku skuType)
	{
		if (skuType == NetworkAchievements.Sku.NETWORK && linkedAchievements != null)
		{
			// This is only a valid check for Network-level Achievements.
			for (int i = 0; i < linkedAchievements.Count; i++)
			{
				if (linkedAchievements[i].sku == skuType && linkedAchievements[i].isUnlocked(member))
				{
					return true;
				}
			}
		}
		return false;
	}

	private List<Achievement> _linkedAchievements;
	public List<Achievement> linkedAchievements
	{
		get
		{
			if (this.sku == NetworkAchievements.Sku.NETWORK && _linkedAchievements == null && this.dataMetricPairs != null)
			{
				// Set this up lazily to avoid race conditions.
				_linkedAchievements = new List<Achievement>();
				Achievement linkedAchievement;
				for (int i = 0; i < dataMetricPairs.Count; i++)
				{
					linkedAchievement = NetworkAchievements.getAchievement(dataMetricPairs[i].Key);
					if (linkedAchievement != null)
					{
						// If this is a valid achievement, then add it to the list.
						_linkedAchievements.Add(linkedAchievement);
					}
				}
				_linkedAchievements.Sort(linkedAchievementsSortFunc);
			}
			return _linkedAchievements;
		}
	}

	private int linkedAchievementsSortFunc(Achievement one, Achievement two)
	{
		return ((int)one.sku).CompareTo((int)two.sku);
	}
	
	public void loadTextureToRenderer(MeshRenderer renderer)
	{
		DisplayAsset.loadTextureToRenderer(
			imageRenderer:renderer,
			url:localURL,
			fallbackUrl:trophyURL,
			isExplicitPath:false,
			shouldShowBrokenImage:true,
			skipBundleMapping:true,
			pathExtension:".png"
		);
	}

	public void loadTextureToUITexture(UITexture texture)
	{
		DisplayAsset.loadTextureToUITexture(
			uiTexture:texture,
			url:localURL,
			fallbackUrl:trophyURL,
			isExplicitPath:false,
			shouldShowBrokenImage:true,
			skipBundleMapping:true,
			pathExtension:".png"
		);
	}

	// Used for if we want a custom callback.
	public void loadTextureToUITexture(UITexture texture, TextureDelegate callback)
	{
		Dict args = Dict.create(
			D.IMAGE_TRANSFORM, (texture != null ? texture.transform : null),
			D.TEXTURE, texture,
			D.SHOULD_HIDE_BROKEN, true
		);		
		
		RoutineRunner.instance.StartCoroutine(
			DisplayAsset.loadTextureFromBundle(
				primaryPath:localURL,
				callback:callback,
				data:args,
				secondaryPath:trophyURL,
				isExplicitPath:false,
				loadingPanel:false,
				onDownloadFailed:null,
				skipBundleMapping:true,
				pathExtension:".png"
		));
	}

	// Used for if we want a custom callback.
	public void loadTextureToRenderer(MeshRenderer renderer, TextureDelegate callback)
	{
		Dict args = Dict.create(
			D.IMAGE_TRANSFORM, (renderer != null ? renderer.transform : null),
			D.TEXTURE, renderer,
			D.SHOULD_HIDE_BROKEN, true
		);
		
		RoutineRunner.instance.StartCoroutine(
			DisplayAsset.loadTextureFromBundle(
				primaryPath:localURL,
				callback:callback,
				data:args,
				secondaryPath:trophyURL,
				isExplicitPath:false,
				loadingPanel:false,
				onDownloadFailed:null,
				skipBundleMapping:true,
				pathExtension:".png"
		));
	}	

	public override string ToString()
	{
		string result = "";
		result += "id" + ":" + id + ",\n";
		result += "name" + ":" + name + ",\n";
		result += "description" + ":" + description + ",\n";
		result += "goal" + ":" + goal + ",\n";
		result += "score" + ":" + score + ",\n";
		if (dataMetricPairs != null)
		{
			string pairString = "metricPairs:";
			for (int i = 0; i < dataMetricPairs.Count; i++)
			{
				pairString += string.Format("[{0}:{1}],", dataMetricPairs[i].Key, dataMetricPairs[i].Value);
			}
			
			result += pairString + ",\n";
		}
		result += "trophyURL" + ":" + trophyURL + ",\n";
		result += "localURL" + ":" + localURL + ",\n";
		result += "version" + ":" + version;
		return result;
	}
}
