using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Holds data about slot licenses.
*/

public class SlotLicense : IResetGame
{
	public string keyName = "";
	public string[] countries = null;
	public string legal_body = "";
	public string legal_title = "";
	public string legal_image = "";
	
	public static Dictionary<string, SlotLicense> all = new Dictionary<string, SlotLicense>();
	
	public static void populateAll(JSON[] array)
	{
		foreach (JSON license in array)
		{
			new SlotLicense(license);
		}
	}
	
	public SlotLicense(JSON data)
	{
		keyName = data.getString("key_name", "");
		countries = data.getStringArray("allowed_countries");
		legal_title = data.getString("paytable_title", "");
		legal_body = data.getString("paytable_description", "");

		legal_image = data.getString("paytable_image_path", "");
		// remove everything from the image path but the image name
		int lastSlashIndexInImagePath = legal_image.LastIndexOf("/");
		if (lastSlashIndexInImagePath != -1)
		{
			legal_image = legal_image.Substring(lastSlashIndexInImagePath + 1);
		}
				
		all.Add(keyName, this);
	}
	
	// Standard find method.
	public static SlotLicense find(string keyName)
	{
		if (all.ContainsKey(keyName))
		{
			return all[keyName];
		}
		return null;
	}
	
	/// Returns whether the given license is allowed in the player's country.
	public static bool isLicenseAllowed(string licenseKey)
	{
		if (string.IsNullOrEmpty(licenseKey) || string.IsNullOrEmpty(SlotsPlayer.instance.country))
		{
			// If either the license or the player's country isn't defined, we allow it (for now anyway).
			return true;
		}

		SlotLicense license = find(licenseKey);
		
		if (license != null)
		{
			if (license.countries.Length == 0)
			{
				return true;
			}
			else
			{
				return (System.Array.IndexOf(license.countries, SlotsPlayer.instance.country) > -1);
			}
		}
		
		return false;
	}

	public static void resetStaticClassData()
	{
		all = new Dictionary<string, SlotLicense>();
	}
}
