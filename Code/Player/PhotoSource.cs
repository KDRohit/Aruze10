using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PhotoSource
{
	private static List<PhotoSource> blankGeneratedImages = new List<PhotoSource>();

	private static string _profileBackupImage = "";
	public  static string profileBackupImage
	{
		get
		{
			if (string.IsNullOrEmpty(_profileBackupImage) && Data.liveData != null)
			{
				_profileBackupImage = Data.liveData.getString("PROFILES_BACKUP_IMAGE", "");
			}
			return _profileBackupImage;
		}
	}	
	
	public enum Source
	{
		FB,
		FB_LARGE,
		PROFILE,
		GENERATED
	}

	private SocialMember member;
	private bool hasBeenSet = false;
	private string facebookUrl = "";
	private string facebookLargeUrl = "";
	private string profileUrl = "";
	// Make the backup URL lazy since we shouldn't need it most of the time.
	private string _generatedUrl = "";
	private string generatedUrl
	{
		get
		{
			if (string.IsNullOrEmpty(_generatedUrl))
			{
				// Lazy generation upon request.
				if (NetworkProfileFeature.instance.hasPopulatedAvatarList)
				{
					// If we have the avatar list, then generate one.
					_generatedUrl = generatePhotoUrl();
				}
				else
				{
					// Otherwise we can not do anything and mark this a needed to be generated when we get the URLs.
					blankGeneratedImages.Add(this);
				}
			}
			return _generatedUrl;
		}
		set
		{
			_generatedUrl = value;
		}
	}

	public const string BACKUP_URL = "https://slotsshared0-a.akamaihd.net/network_profiles/avatars/Avatars_01.png";

	public PhotoSource(SocialMember member)
	{
		this.member = member;
	}

	public string getUrl(Source source)
	{
		switch (source)
		{
 			case Source.FB:
				return facebookUrl;
			case Source.FB_LARGE:
				return facebookLargeUrl;
			case Source.PROFILE:
				return profileUrl;
			case Source.GENERATED:
				return generatedUrl;
		}
		return "";
	}

	public void setUrl(string url, Source source)
	{
		if (string.IsNullOrEmpty(url))
		{
			// If we are trying to set an empty value, then log a warning (or breadcrumb on prod)
			// and return so we dont think its setup yet.
#if ZYNGA_PRODUCTION
			Bugsnag.LeaveBreadcrumb(string.Format("PhotoSource.cs -- setUrl() -- trying to set and empty url for source: {0}", source));
#else
			Debug.LogWarningFormat("PhotoSource.cs -- setUrl() -- trying to set an empty url for source: {0}", source);
#endif
			return;
		}
		string primaryUrl = "";
		if (hasBeenSet)
		{
			// If we try to get the primary url on a photo source that hasn't been used at all yet
			// then we are always going to generate a url, which should not happen.
			primaryUrl = getPrimaryUrl();
		}

		switch (source)
		{
 			case Source.FB:
				facebookUrl = url;
				break;
			case Source.FB_LARGE:
				facebookLargeUrl = url;
				break;
			case Source.PROFILE:
				profileUrl = url;
				break;
			case Source.GENERATED:
				generatedUrl = url;
				break;
		}

		string newPrimary = getPrimaryUrl();
		if (primaryUrl != newPrimary)
		{
			// If in adding this url, we changed the primary url that should be used, then send out the event.
			member.setUpdated();
		}
		hasBeenSet = true;
	}

	public string getPrimaryUrl(bool allowLarge = false)
	{
		if (NetworkProfileFeature.instance.isEnabled && !string.IsNullOrEmpty(profileUrl))
		{
			return profileUrl;
		}
		else if (allowLarge && member.isFacebookConnected && !string.IsNullOrEmpty(facebookLargeUrl))
		{
			return facebookLargeUrl;
		}
		else if (member.isFacebookConnected && !string.IsNullOrEmpty(facebookUrl))
		{
			return facebookUrl;
		}
		else if (!string.IsNullOrEmpty(generatedUrl))
		{
			return generatedUrl;
		}
		else
		{
			// The backup url is an avatar only if network profiles is enabled, otherwise return empty.
			return NetworkProfileFeature.instance.isEnabled ? BACKUP_URL : "";
		}
	}

    private string generatePhotoUrl()
	{
		if (member == null)
		{
#if UNITY_EDITOR
			Debug.LogErrorFormat("PhotoSource.cs -- generatePhotoUrl() -- member is null on this PhotoSource, this should never be the case.");
#else
			Bugsnag.LeaveBreadcrumb("PhotoSource.cs -- generatePhotoUrl() -- member is null on this PhotoSource, this should never be the case");
#endif
		}
		string refId = member.networkID;
		string zidString = member.zId;
		if (string.IsNullOrEmpty(refId) || refId == "-1")
		{
			refId = "0";
		}
		
		if (string.IsNullOrEmpty(zidString) || zidString == "-1")
		{
			zidString = "0";
		}
		
		string result = "";
		try
		{
			long referenceId = long.Parse(refId); // Converting to a long so that we can do math on it.
			long zid = long.Parse(zidString);
			if (NetworkProfileFeature.instance.avatarList != null)
			{
				if (NetworkProfileFeature.instance.avatarList.Count > 10)
				{
					// If we have enough urls to do a generation based on network or zid then do that.
					// If the photo url is empty after generation and we have the avatar list,
					// then use the last digit of the networkID to pick one.
					long randomNumber = 0;
					if (referenceId > 0)
					{
						randomNumber = referenceId % 10L;
					}
					else if (zid > 0)
					{
						// If the network ID doesn't exist,then lets use the zid.
						randomNumber = zid % 10L;
					}
					else
					{
						randomNumber = 0;
					}
					int index = System.Convert.ToInt32(randomNumber);
					if (index > NetworkProfileFeature.instance.avatarList.Count -1 || index < 0)
					{
						Debug.LogWarningFormat("NetworkProfile.cs -- generatePhotoUrl -- tried to use index {0} in an url list of length: {1}, defaulting to 0", index, NetworkProfileFeature.instance.avatarList.Count);
					}
					else
					{
						result = NetworkProfileFeature.instance.avatarList[index];
					}

				}
				else if (NetworkProfileFeature.instance.avatarList.Count > 0)
				{
					// If we have some, but not enough to to a 0-9 generation then use the first one.
					result = NetworkProfileFeature.instance.avatarList[0];
				}
				else
				{
					// Otherwise use the backup hardcoded value.
					Debug.LogWarningFormat("NetworkProfile -- imageURL -- using the hardcoded backup for profile image because avatar list was empty: {0}", profileBackupImage);
					result = profileBackupImage;
				}
			}

			if (string.IsNullOrEmpty(result))
			{
				// If we dont have the avatar list, or if using the zid or network ID to generate
				// a url didnt work, then use the backup.
				Debug.LogWarningFormat("NetworkProfile -- imageURL -- using the hardcoded backup for profile image because all else failed: {0}", profileBackupImage);
				result = profileBackupImage;
			}
		}
		catch (System.FormatException e)
		{
			Debug.LogErrorFormat("NetworkProfile.cs -- generatePhotoUrl -- could not parse as long: {0}, {1} exception text was {2}", refId, zidString, e.Message);
		}
		return result;
	}

	public string ToString()
	{
		if (string.IsNullOrEmpty(_generatedUrl))
		{
			// Dont generate the generatedUrl if its not set already, it shoudl alwyas be blank unless needed.
			return string.Format("Primary: {0}\nFB:{1}\nFB_LARGE:{2}\nPROFILE:{3}\n",
				getPrimaryUrl(),
				facebookUrl,
				facebookLargeUrl,
				profileUrl);	
		}
		else
		{
			return string.Format("Primary: {0}\nFB:{1}\nFB_LARGE:{2}\nPROFILE:{3}\nGENERATED:{4}\n",
				getPrimaryUrl(),
				facebookUrl,
				facebookLargeUrl,
				profileUrl,
				generatedUrl);			
		}
	}
	
	public static void updateBlankSources()
	{
		// Now that we have the avatar urls from the server, lets populate any sources that needed a generated one.
		if (blankGeneratedImages != null)
		{
			PhotoSource source;
			for (int i = 0; i < blankGeneratedImages.Count; i++)
			{
				source = blankGeneratedImages[i];
				source.setUrl(source.generatePhotoUrl(), Source.GENERATED);
			}
		}
		// Now clear it.
		blankGeneratedImages.Clear();
	}

	public static void resetStaticClassData()
	{
		blankGeneratedImages = new List<PhotoSource>();
		_profileBackupImage = "";
	}
}
