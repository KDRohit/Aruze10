using UnityEngine;
using System.Collections.Generic;
[System.Obsolete]
public class FeatureDirector : IResetGame
{
	private static FeatureDirector _instance;
	private static FeatureDirector instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new FeatureDirector();
			}
			return _instance;
		}	
	}

	private Dictionary<string, FeatureBase> allFeatures;
	private static bool hasReceivedLoginData;

	// Read only public getter.
	public static Dictionary<string, FeatureBase> features { get { return instance.allFeatures; } }
	// Constructor
	private FeatureDirector()
	{
		allFeatures = new Dictionary<string, FeatureBase>();
		hasReceivedLoginData = false;
	}

	public static TFeature createOrGetFeature<TFeature>(string key) where TFeature: FeatureBase, new()
	{
		TFeature result;
		if (!instance.allFeatures.ContainsKey(key))
		{
			result = new TFeature();
			instance.allFeatures.Add(key, result);
			if (hasReceivedLoginData && Data.login != null)
			{
				result.initFeature(Data.login);
			}
			else if (hasReceivedLoginData && Data.login == null)
			{
				Bugsnag.LeaveBreadcrumb("Feature director out of sync! We think we have login data but it's null. Changing hasRecievedLoginData to false");
			}
		}
		else
		{
			result = instance.allFeatures[key] as TFeature;
		}
		return result as TFeature;
	}

	private static void checkFeaturesRequiredAtLogin(JSON loginData)
	{
		//ensure features needed at login are created
		TicketTumblerFeature.checkInstance();
		NetworkFriends.instance.createFeatureInstance();

		//need to init regardless in case user has pending reward from past event
		QuestForTheChest.QuestForTheChestFeature.checkInstance();
		LootBoxFeature.checkInstance();
	}

	public static void recieveLoginData(JSON loginData)
	{
		//some features must be instantiated at this point, do a check to verify they exist
		checkFeaturesRequiredAtLogin(loginData);

		if (instance.allFeatures != null)
		{
			// Initialize all features that were created before login data was received.
			// Pull keys and iterate through the list of them to prevent out of sync exceptions
			List<string> strings = new List<string>();
			strings.AddRange(instance.allFeatures.Keys);
			int keyCount = strings.Count;

			for (int i = 0; i < strings.Count; i++)
			{
				if (instance.allFeatures.ContainsKey(strings[i]))
				{
					instance.allFeatures[strings[i]].initFeature(loginData);
				}
			}

			if (keyCount != instance.allFeatures.Count)
			{
				Debug.LogError("FeatureDirector::recieveLoginData - A feature was added to the feature dictionary as we iterated through it. This may cause issues");
			}
		}
		hasReceivedLoginData = true;
	}

	public static bool hasFeature(string name)
	{
		return instance.allFeatures.ContainsKey(name);
	}

	public static void resetStaticClassData()
	{
		_instance = null;
		hasReceivedLoginData = false;
	}
}
