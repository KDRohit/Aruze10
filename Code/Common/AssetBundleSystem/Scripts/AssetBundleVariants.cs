using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// The currently defined variant enums...
public enum Variant
{
	HD,             // The original, high-res, non-mipped, ETC1/ETC2 textures as we've always had
	SD,             // For lower-end android: 1/2 sized, mipmapped textures, same ETC1/ETC2 compression as HD
	SD4444,         // For lower-end android: 1/2 sized, mipmapped textures, alpha textures are dithered RGBA4444 (no ETC2)
	TintedMipTest   // Dev-only test to colorize a handful of mipmaps
}


// Runtime variant Utility/Support code
public static class AssetBundleVariants
{

	// Returns the best variant mapping for this device that exists in this manifest
	// Can return <null> for older, pre-variant manifests
	static public Dictionary<string,string> getIdealVariantMapping(AssetBundleManifestWrapper manifest)
	{
		Dictionary<string,string> variantMapping;
			
		// Try to lookup/use a ZID based whitelisted override (for QA purposes)
		string variantKey = getWhitelistedVariantKeyOverride();
		if (variantKey != null)
		{
			if (manifest.bundleVariants.TryGetValue(variantKey, out variantMapping))
			{
				Debug.Log("Using whitelisted ZID-based override: Variant." + variantKey);
				setActiveVariantName(variantKey);
				return variantMapping;
			}
			else
			{
				Debug.Log("Tried ZID-based override: Variant." + variantKey + ", but not in manifest!" );
			}
		}
		else
		{
			Debug.Log("Could not find whitelisted variant override for this users ZID");
		}


		// Handle WebGL query string in the form of "variant=HD", etc
		Dictionary<string, string> fieldValuePairs = URLStartupManager.Instance.urlParams;
		if (fieldValuePairs != null && fieldValuePairs.TryGetValue("variant", out variantKey))
		{
			if (manifest.bundleVariants.TryGetValue(variantKey, out variantMapping))
			{
				Debug.Log("Using query-string based override: Variant." + variantKey);
				setActiveVariantName(variantKey);
				return variantMapping;
			}
			else
			{
				Debug.Log("Tried query-string based override: Variant." + variantKey + ", but not in manifest!");
			}
		}


		// Get ideal variant (may or may not exist in our manifest)
		Variant variant = getIdealVariantForThisDevice();

		// Try the 'ideal' variant for this device
		if (manifest.bundleVariants.TryGetValue(variant.ToString(), out variantMapping))
		{
			Debug.Log("Using ideal Variant." + variant);
			setActiveVariantName(variant.ToString());
			return variantMapping;
		}
		else
		{
			Debug.Log("Ideal Variant." + variant + " does not exist in manifest.");
		}

		// If ideal variant doesn't exist, try default HD variant
		variant = Variant.HD;
		if (manifest.bundleVariants.TryGetValue(variant.ToString(), out variantMapping))
		{
			Debug.Log("Using alternate Variant." + variant);
			setActiveVariantName(variant.ToString());
			return variantMapping;
		}
		else
		{
			Debug.Log("Alternate Variant." + variant + " does not exist in manifest.");
		}

		// If that doesn't exist, use whatever variant we find in the manifest (can happen for single-variant dev bundle builds)
		variantKey = getFirstVariantKey(manifest);
		if (variantKey != null)
		{
			Debug.Log("Using last-resort Variant." + variantKey);
			setActiveVariantName(variantKey);
			return manifest.bundleVariants[variantKey];
		}
		else
		{
			Debug.Log("No variant definitions found in manifest (probably old pre-variant manifest)");
		}

		// nothing...
		setActiveVariantName("none");
		return null;
	}


	// Gets/Sets the variant name we're using that was previously chosen by getIdealVariantMapping(...)
	static public string getActiveVariantName()
	{
		return _activeVariantName;
	}

	static public void setActiveVariantName(string value)
	{
		_activeVariantName = value + (ZdkManager.Instance.IsReady ? "" : "(pre)");
	}

	static private string _activeVariantName = "undefined";


	// Determines the ideal asset bundle variant to TRY to use for this platform/device/capabilities
	// It's all hardcoded logic for now...
	static Variant getIdealVariantForThisDevice()
	{
#if UNITY_EDITOR
        if (PlayerPrefsCache.GetInt(DebugPrefs.USE_SD_BUNDLES, 0) > 0)
        {
            return Variant.SD;
        }
#endif
		var platform = Application.platform;
		int sysMem = SystemInfo.systemMemorySize;
		int gfxMem = SystemInfo.graphicsMemorySize;
		int width = Screen.width;
		int height = Screen.height;

		// landscape orientation please
		if (width < height)
		{
			width = Screen.height;
			height = Screen.width;
		}

		// Default variant is "HD" ... unless we find something better
		Variant variant = Variant.HD;

		// Only Android has variants so far...
		if (platform == RuntimePlatform.Android)
		{
			if (!SystemInfo.SupportsTextureFormat(TextureFormat.ETC2_RGBA8))
			{
				Dictionary<string, string> extraFields = new Dictionary<string, string>();
				extraFields.Add("reason", "Device does not support ETC2");
				
				SplunkEventManager.createSplunkEvent("getIdealBundleVariant", "sd", extraFields);
				Debug.Log("Choosing Variant.SD because device does not support ETC2");
				return Variant.SD;
			}

			// Testing shows 2nd-miplevel at ~800p, & top HD miplevel at >1012p (and elements vary between)
			// Based on metrics, our two closest popular vertical resolutions are 800 and 1080, NOTHING between 855 and 1079
			// So... 1000 is a good decision making threshold between HD and SD content
			if (height < 1000)
			{
				Debug.Log("Choosing Variant.SD because device res is < 1080P");
				return Variant.SD;
			}

			// Maybe also constrain by memory? 
			// No need, Splunk stats show everything >= 1080P has at least 512MB gpu mem, great!

			// Whitelist/blacklist/...? by device? by ZID?
		}

		if (platform == RuntimePlatform.IPhonePlayer)
		{
			if (sysMem <= 512)
			{
				Debug.Log("Choosing Variant.SD because device memory is < 512");
				return Variant.SD;
			}
		}

#if UNITY_WEBGL
		{
			Debug.Log("Choosing Variant.SD because we're running on WebGL");
			return Variant.SD;
		}
#endif

		return variant;
	}


	// returns a whitelisted variantname for this user if it exists in livedata, null if none found
	static string getWhitelistedVariantKeyOverride()
	{
		try // Dealing with external live data; anything can blow up...
		{
			if (!ZdkManager.Instance.IsReady)
			{
				Debug.Log("Trying to lookup ZID based variant override before ZdkManager is ready!");
				return null;
			}

			string myZid = ZdkManager.Instance.Zid;
			if (string.IsNullOrEmpty(myZid))
			{
				Debug.LogError("ZID is nullOrEmpty; can't lookup variant overrides");
				return null;
			}

			string[] whitelist = Data.liveData.getArray("ASSETBUNDLE_VARIANT_WHITELIST", null);
			if (whitelist == null)
			{
				Debug.LogError("Could not find LiveData ASSETBUNDLE_VARIANT_WHITELIST");
				return null;
			}

			// Livedata whitelist is an array of strings in this format:
			//   VariantName1: zid1,zid2,zid3...  (deliminted by commas and/or spaces)
			foreach (string line in whitelist)
			{
				string[] split = line.Split(':');

				if (string.IsNullOrEmpty(line.Trim()))
				{
					continue;
				}
				else if (split.Length != 2)
				{
					Debug.LogError("could not split whitelist line in 2: " + line);
					continue;
				}

				string variantName = split[0].Trim();
				string zidString = split[1].Trim();
				string[] zids = zidString.Split(',', ' ', '(', ')' ); //commas, spaces, and/or parenthesis (for zid-comments)
				foreach (string zid in zids)
				{
					if (myZid == zid.Trim())
					{
						Debug.Log("Found variant." + variantName + " override for Zid " + myZid);
						return variantName;
					}
				}
			}
		}
		catch(System.Exception e)
		{
			Debug.LogError("Error in getWhitelistedVariantKeyOverride: " + e);
			return null;
		}

		// no overrides found
		Debug.Log("Did not find any zid-based variant overrides for ZID: " + ZdkManager.Instance.Zid);
		return null;
	}


	// Returns the first variant key we find in the manifest; null if none
	// (variants are kept in a dictionary, so they aren't technically ordered)
	static string getFirstVariantKey(AssetBundleManifestWrapper manifest)
	{
		// Don't be confused by the foreach; We're just returning the first key we find
		foreach(string key in manifest.bundleVariants.Keys)
		{
			return key;
		}

		// Else no keys, return null
		return null;
	}

	// TODO: Add variant choice to editor/login settings?
}
