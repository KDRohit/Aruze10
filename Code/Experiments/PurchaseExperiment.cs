using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PurchaseExperiment : EosExperiment
{
	public int startSeconds { get; private set; }
	public int endSeconds { get; private set; }
	public int charmsStartDate { get; private set; }
	public int charmsEndDate { get; private set; }
	public string imageFolderPath { get; private set; }
	public string imagePathCollections { get; private set; }
	public string collectiblesEvents { get; private set; }
	public string collectiblesEventLifts { get; private set; }
	public bool hasCardPackDropsConfigured { get; private set; }

	private Dictionary<string, string> packageData = null;

	public PurchaseExperiment(string experimentName) : base (experimentName)
	{
	}


	public string getCreditPackageName(string packageKey)
	{
		if (packageData == null)
		{
			Debug.LogError("Trying to load package: " + packageKey + " before experiment(" + experimentName + ") has loaded");
			return "";
		}

		if (!packageData.ContainsKey(packageKey))
		{
			Bugsnag.LeaveBreadcrumb("Attempting to access invalid credit package: " + packageKey);
			return "";
		}
		return packageData[packageKey];
	}

	protected virtual bool isReservedKey(string key)
	{
		switch(key)
		{
			case "start_date":
			case "end_date":
			case "charms_availability_start_date":
			case "charms_availability_end_date":
			case "image_path":
			case "image_path_collections":
			case "collectibles_events_1_to_6":
			case "collectibles_event_lifts_1_to_6":
				return true;

			default:
				return false;
		}
	}

	protected override void init(JSON data)
	{
		//contains a list of packages, so we must parse each individual key value pair

		packageData = new Dictionary<string, string>();
		foreach (string varName in data.getKeyList())
		{
			if (isReservedKey(varName))
			{
				continue;
			}
			packageData[varName] = getEosVarWithDefault(data, varName, "");
		}

		startSeconds = getEosVarWithDefault(data, "start_date", 0);
		endSeconds = getEosVarWithDefault(data, "end_date", 0);
		charmsStartDate = getEosVarWithDefault(data, "charms_availability_start_date", 0);
		charmsEndDate = getEosVarWithDefault(data, "charms_availability_end_date", 0);
		imageFolderPath = getEosVarWithDefault(data, "image_path", "");
		imagePathCollections = getEosVarWithDefault(data, "image_path_collections", "");
		collectiblesEvents = getEosVarWithDefault(data, "collectibles_events_1_to_6", "");
		collectiblesEventLifts = getEosVarWithDefault(data, "collectibles_event_lifts_1_to_6", "");

		// Go through all the packages until we find one configured to something other than "nothing"
		int packageIterator = 0;
		string compareString = null;
		while (compareString != null || packageIterator == 0)
		{
			packageIterator++;
			string packageString = string.Format("package_{0}", packageIterator);
			compareString = getPackageValue(packageString, string.Format(packageString, "_collectible_pack"), null);

			if (compareString != null && compareString != "nothing")
			{
				hasCardPackDropsConfigured = true;
			}
		}
	}

	public string getPackageValue(string package, string key, string defaultValue)
	{
		string index = package + key;
		if (null == packageData || !packageData.ContainsKey(index))
		{
			return defaultValue;
		}

		return packageData[index];
	}

	public int getPackageValue(string package, string key, int defaultValue)
	{
		string index = package + key;
		if (null == packageData || !packageData.ContainsKey(index) || string.IsNullOrEmpty(packageData[index]))
		{
			return defaultValue;
		}

		return int.Parse(packageData[index]);
	}

	public long getPackageValue(string package, string key, long defaultValue)
	{
		string index = package + key;
		if (null == packageData || !packageData.ContainsKey(index) || string.IsNullOrEmpty(packageData[index]))
		{
			return defaultValue;
		}

		return long.Parse(packageData[index]);
	}

	public bool getPackageValue(string package, string key, bool defaultValue)
	{
		string index = package + key;
		if (null == packageData || !packageData.ContainsKey(index) || string.IsNullOrEmpty(packageData[index]))
		{
			return defaultValue;
		}

		return bool.Parse(packageData[index]);
	}

}
