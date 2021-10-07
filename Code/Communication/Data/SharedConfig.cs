using UnityEngine;
using UnityEngine.SceneManagement;
using Facebook.Unity;
using System;
using System.Reflection;
using Zynga.Core.Util;

public class SharedConfig
{
	public static string currentConfigName = "";
	private static JSON _configJSON;
	public static void reload()
	{
		_configJSON = null;
		PreferencesBase preferences = SlotsPlayer.getPreferences();
		if (SocialManager.Instance != null && SlotsPlayer.isFacebookUser)
		{
			// Attempt a proper logout of MiSocialManager
			SocialManager.Instance.Logout(false);
		}
		else
		{
			// Fallback
			preferences.SetInt(SocialManager.kLoginPreference, (int)SocialManager.SocialLoginPreference.FirstTime);
			preferences.Save();
		}
#if !UNITY_WEBGL
		Glb.resetGame("Loading new stage");		
		Data.loadConfig();
#endif
	}

	/// <summary>
	///   grabs the config file by name, configNamePrefix should be something like: hir_env3
	/// </summary>
	public static void create(string configNamePrefix)
	{		
		string configPath = string.Format("Config/{0}", configNamePrefix);

		TextAsset configFile = Resources.Load(configPath) as TextAsset;

		if (configFile != null && configFile.text != null)
		{
			PreferencesBase preferences = SlotsPlayer.getPreferences();
			// wipe out data, using access tokens from other stages causes issues
			preferences.DeleteAll();
			preferences.SetString(DebugPrefs.CONFIG_OVERRIDE_PATH, configPath);
			currentConfigName = configNamePrefix + ".txt";
			preferences.Save();
			reload();
		}
		else
		{
			Debug.Log("Incorrect config: " + configPath);
		}
	}
	
#if !ZYNGA_PRODUCTION
	public static JSON configJSON
	{
		get
		{	
			// Attempt to read the config file to acquire the basic data URL and other stuff.
			// This should only happen once per session.
			if (_configJSON == null)
			{
				PreferencesBase preferences = SlotsPlayer.getPreferences();
				string overrideConfigPath = preferences.GetString(DebugPrefs.CONFIG_OVERRIDE_PATH, "");

				if (!string.IsNullOrEmpty(overrideConfigPath))
				{
					TextAsset configFile = Resources.Load(overrideConfigPath) as TextAsset;
					if (configFile != null && configFile.text != null)
					{
						_configJSON = new JSON(configFile.text);
					}
				}
			}

			return _configJSON;
		}
	}
#endif			

}