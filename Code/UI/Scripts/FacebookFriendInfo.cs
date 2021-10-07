using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Com.Scheduler;
using TMPro;
using TMProExtensions;

/**
Controls display of Facebook Friend Info (like the picture, name, credits, etc.)
*/

public class FacebookFriendInfo : TICoroutineMonoBehaviour
{
	public delegate void onImageSetDelegate(bool didSucceed);
	public event onImageSetDelegate onImageSet;

	public string zid = "";

	// name
	public TextMeshPro nameTMPro;

	public enum NameFormat
	{
		FIRST,
		LAST,
		FIRST_AND_LAST,
		FIRST_AND_LAST_INITIAL
	};
	public NameFormat nameFormat = NameFormat.FIRST;

	// misc
	public TextMeshPro creditsTMPro;
	public int creditsAbbreviationDecimals = -1;	// If > -1, the credits amount is shows as abbreviated instead of exact amount.
													// For example if set to 1 then, 10,500,000 would show up as 10.5M instead.
	public string creditsLocalizationKey = "";		// Optional localization key to plug the credits value into for display.
	public TextMeshPro rankTMPro;
	public bool addNumberSignToRank = false;	// Whether to show rank as #X or just X.
	public TextMeshPro lastNameTMPro; 
	public TextMeshPro otherTMPro;  // May be used for anything, and is manually set by whatever owns it.

	public TextMeshPro scoreTMPro;
	public SocialMember.ScoreType scoreKey = SocialMember.ScoreType.NONE;

	// image
	public UISprite borderSprite;
	public Renderer image;
	public UITexture imageUITexture;
	public Texture2D defaultMale;
	public Texture2D defaultFemale;

	public ClickHandler profileHandler;

	public GameObject rankIconAnchor;
	public AchievementRankIcon rankIcon;

    [SerializeField] private GameObject facebookLogo;
    [SerializeField] private GameObject onlineNowLogo;

	// Should be true unless you want to show facebook info over missing or generated network profile info.

	private string url = "";
	private bool hasRegisteredEvents = false;

	/// Some implementations can simply set the member property and be done.
	public SocialMember member
	{
		set
		{
			recycle();

			if (!hasRegisteredEvents && value != null)
			{
				value.onMemberUpdated += memberUpdated;
				hasRegisteredEvents = true;
			}
			RoutineRunner.instance.StartCoroutine(setMember(value));
		}

		get
		{
			return (_member);
		}
	}

	private SocialMember _member = null;

	/// Some implementations require a coroutine to wait until the image has finished loading before returning.
	public virtual IEnumerator setMember(SocialMember member)
	{
		zid = member.zId;
		if (!SlotsPlayer.isLoggedIn)
		{
			// Check this at first, and after each yield.
			yield break;
		}

		_member = member;

		if (_member != null)
		{
			long credits = 0L;
			string creditsText = "0";

			// Set the credits label before requesting anything remotely, so it's immediate.
			if (_member.isUser)
			{
				credits = SlotsPlayer.creditAmount;
			}
			else if (_member.credits > 0L)
			{
				credits = _member.credits;
			}

			if (!_member.isUser && credits == 0L)
			{
				// HIR-13830: If non-player credits is 0, then show blank instead of 0.
				// This is mainly for things like Daily Race when a new race starts and nobody has score yet.
				creditsText = "";
			}
			else if (creditsAbbreviationDecimals > -1)
			{
				creditsText = CreditsEconomy.multiplyAndFormatNumberAbbreviated(credits, creditsAbbreviationDecimals);
			}
			else
			{
				creditsText = CreditsEconomy.convertCredits(credits);
			}

			if (creditsTMPro != null)
			{
				if (creditsLocalizationKey != "")
				{
					creditsTMPro.text = Localize.textUpper(creditsLocalizationKey, creditsText);
				}
				else
				{
					creditsTMPro.text = creditsText;
				}
			}

			if (scoreTMPro != null && scoreKey != SocialMember.ScoreType.NONE)
			{
				scoreTMPro.text = CommonText.formatNumber(_member.getScore(scoreKey));
			}

			if (nameTMPro != null)
			{
				nameTMPro.text = "";	// Clear it by default, so it doesn't use the prefab default value while waiting for name to download.
			}

			if (lastNameTMPro != null)
			{
				lastNameTMPro.text = "";  // Clear it by default, so it doesn't use the prefab default value while waiting for name to download.
			}
			
			yield return null;      // Make sure the initial label values take effect.

			if (_member == null)
			{
				Debug.LogErrorFormat("FacebookFriendInfo.cs -- setMember -- SocialMember became null after set the labels to empty.");
				yield break;				
			}
			if (!SlotsPlayer.isLoggedIn)
			{
				// Check this at first, and after each yield.
				yield break;
			}

			string names = "";

			if (nameTMPro != null)
			{
				// If we have the network ID use it.
				if (_member.networkProfile != null && !string.IsNullOrEmpty(_member.networkProfile.name))
				{
					names = _member.networkProfile.name;
				}
				else if (
					(string.IsNullOrEmpty(_member.id) || (_member.id != "-1" && _member.id != "notyet")) &&
						  _member.firstName == "")	// -1 is anonymous players, such as for daily race.
				{
					yield return StartCoroutine(_member.requestName());
				}

				if (_member == null)
				{
					Debug.LogErrorFormat("FacebookFriendInfo.cs -- setMember -- SocialMember became null after we requested their name from facebook, aborting.");
					yield break;
				}

				if (!SlotsPlayer.isLoggedIn)
				{
					// Check this at first, and after each yield.
					yield break;
				}

				if (names == "")
				{
					switch (nameFormat)
					{
						case NameFormat.FIRST:
							names = _member.firstName;
							break;

						case NameFormat.LAST:
							names = _member.lastName;
							break;

						case NameFormat.FIRST_AND_LAST:
							names = _member.fullName;
							break;

						case NameFormat.FIRST_AND_LAST_INITIAL:
							names = _member.firstNameLastInitial;
							break;
						default:
							names = _member.fullName;
						break;
					}
				}

				if (nameTMPro != null && nameTMPro.font != null && !nameTMPro.font.HasCharacters(names))
				{
					// If using TextMeshPro with a font that doesn't have one or more characters in the name,
					// then set this blank again to force using the anonymous name below.
					names = "";
				}
				
				if (string.IsNullOrEmpty(names) || names.IsNullOrWhiteSpace())
				{
					// If the name is STILL empty, then this must be a non-friend that we've utterly failed
					// to retrieve the name of (sometimes the facebook request fails).
					// Use the last resort fallback instead of showing blank.
					_member.firstName = _member.anonymousNonFriendName;
					names = _member.firstName;
				}
			}

			string lastName = "";
			if (null != lastNameTMPro)
			{
				lastName = removeLastFromNames(names);
				if (null != lastNameTMPro)
				{
					lastNameTMPro.text = lastName;
				}
			}

			if (nameTMPro != null)
			{
				nameTMPro.text = names;
			}

			if (image != null || imageUITexture != null)
			{
				// Set the default image before trying to get the real one from Facebook.
				Texture2D newTexture = _member.isFemale ? defaultFemale : defaultMale;

				if (newTexture != null)
				{
					if (image != null)
					{
						_member.applyTextureToRenderer(newTexture, image);
					}
					else if (imageUITexture != null)
					{
						_member.applyTextureToRenderer(newTexture, imageUITexture);
					}
				}

				string memberUrl = _member.getImageURL;
				if (!string.IsNullOrEmpty(memberUrl))
				{
					// Now try setting the real image.
					url = memberUrl;
					if (image != null || imageUITexture != null)
					{
						RoutineRunner.instance.StartCoroutine(
							DisplayAsset.loadTexture(
								primaryPath: url,
								callback: onTextureLoaded,
								data: null,
								secondaryPath: "",
								isExplicitPath: true,
								loadingPanel: false));
					}
				}
				else
				{
					Debug.LogWarningFormat("FacebookFriendInfo.cs -- setMember -- imageURL is empty, not trying to download it fullname {0} firstname {1} fbfirstname {2}", _member.fullName, _member.firstName, _member.fbFirstName);
				}
			}

			if (profileHandler != null)
			{
				profileHandler.clearAllDelegates();
				profileHandler.registerEventDelegate(onProfileButtonClick, Dict.create(D.PLAYER, _member));
			}

			if (NetworkAchievements.isEnabled && rankIconAnchor != null)
			{
				// If network achievements is on, then let's load in the rank icon if we have an anchor linked.
				AchievementRankIcon.loadRankIconToAnchor(rankIconAnchor, _member);
			}

			if (NetworkAchievements.isEnabled && rankIcon != null)
			{
				// If network achievements is on, the populate the rank icon if its loaded.
				rankIcon.setRank(_member);
			}
			bool shouldShowFacebook = _member == null ? false : _member.isFacebookFriend;
			SafeSet.gameObjectActive(facebookLogo, shouldShowFacebook);
			SafeSet.gameObjectActive(onlineNowLogo, false); // This isn't supported yet.

		}
		else
		{
			Debug.LogErrorFormat("FacebookFriendInfo.cs -- setMember -- member is null...wtf man.");
		}
	}

	private void onTextureLoaded(Texture2D tex, Dict data)
	{
		if (this == null)
		{
			return;
		}

		bool didSucceed = false;
		if (tex != null)
		{
			if (image != null && image.gameObject != null)
			{
				FriendListItem friendListItem = this.GetComponent<FriendListItem>();
				if (friendListItem != null && friendListItem.useShader == true)
				{
					image.sharedMaterial = DisplayAsset.getNewRendererMaterial(image, true);
				}
				else
				{
					image.sharedMaterial = DisplayAsset.getNewRendererMaterial(image);
				}
				if (null != image.sharedMaterial)
				{
					image.sharedMaterial.mainTexture = tex;
					image.sharedMaterial.color = Color.white;
					image.gameObject.SetActive(true);	// Just in case we deactivated the texture object while loading.
					didSucceed = true;
				}
			}
			else if (imageUITexture != null && imageUITexture.gameObject != null)
			{
				imageUITexture.gameObject.SetActive(false);
				Material mat = new Material(imageUITexture.material);
				if (mat != null) //if material is null .mainTexture call will NRE
				{
					imageUITexture.material = mat;
					imageUITexture.mainTexture = tex;
					imageUITexture.gameObject.SetActive(true);
					didSucceed = true;
				}
			}
		}

		if (!didSucceed)
		{
			if (member != null)
			{
				// reduced this logging while debugging only since privacy, and deleted profiles can cause failed profile image downloads
				if (Data.debugMode)
				{
					Debug.LogErrorFormat("FacebookFriendInfo.cs -- onTextureLoaded -- failed to load image texture for user zid: {0}", member.zId);
				}
			}
			else
			{
				Debug.LogError("FacebookFriendInfo.cs -- onTextureLoaded -- failed to load image texture for user. Even worse member is null, ZID not known.");
			}
		}

		if (onImageSet != null)
		{
			onImageSet(didSucceed);
		}
	}

	// If something happens that requires us to setup something outside of profiles or this class,
	// but we still want to be able to LINK to the profile, we can do that and use this.
	public void registerMemberToProfileHandler(SocialMember memberToTarget)
	{
		if (profileHandler != null)
		{
			profileHandler.registerEventDelegate(onProfileButtonClick, Dict.create(D.PLAYER, memberToTarget));
		}
	}

	public void memberUpdated(SocialMember updatedMember)
	{
		if (rankIcon == null && rankIconAnchor != null)
		{
			rankIcon = rankIconAnchor.GetComponentInChildren<AchievementRankIcon>();
		}

		if (rankIcon != null)
		{
			rankIcon.setRank(updatedMember);
		}

		if (url != updatedMember.getImageURL)
		{
			// Don't update the image if it is already pointing to the same URL.
			if (image != null)
			{
				url = updatedMember.getImageURL; // If we load a texture, set the url.
				DisplayAsset.loadTextureToRenderer(image, url, "", true, false);
			}
			else if (imageUITexture != null)
			{
				url = updatedMember.getImageURL; // If we load a texture set the url.
				DisplayAsset.loadTextureToUITexture(imageUITexture, url, "", true);
			}
		}

		if (nameTMPro != null &&
			updatedMember.networkProfile != null &&
			!string.IsNullOrEmpty(updatedMember.networkProfile.name))
		{
			bool shouldUseGuest = !string.IsNullOrEmpty(updatedMember.firstNameLastInitial) && updatedMember.networkProfile.name.Contains("Guest");
			string newName = shouldUseGuest ? updatedMember.firstNameLastInitial : updatedMember.networkProfile.name;
			if (nameTMPro.text != newName)
			{
				nameTMPro.text = newName;
			}
		}
	}

	private void onProfileButtonClick(Dict args)
	{
		if (NetworkProfileFeature.instance.isEnabled)
		{
			// Only do this if the network profiles feature is on.
			SocialMember argMember = args.getWithDefault(D.PLAYER, null) as SocialMember;
			if (argMember != null)
			{
				NetworkProfileDialog.showDialog(argMember, SchedulerPriority.PriorityType.IMMEDIATE);
			}
			else
			{
				Debug.LogErrorFormat("FacebookFriendInfo.cs -- onProfileButtonClick -- member is null cannot open profile.");
			}
		}
		else
		{
			Debug.LogErrorFormat("FacebookFriendInfo.cs -- onProfileButtonClick -- network profile is not enabled, not opening profile.");
		}
	}

	private string removeLastFromNames(string names)
	{
		string[] n = names.Split(' ');
		names = n[0];
		int i = 0;
		int end = n.Length;
		string lastName = "";
		//usually only once
		while (++i < end)
		{
			lastName += n[i] + ' ';
		}

		return lastName;
	}

	public int rank
	{
		set
		{
			_rank = value;
			if (rankTMPro != null)
			{
				string text = CommonText.formatNumber(rank);
				if (addNumberSignToRank)
				{
					text = "#" + text;
				}
				rankTMPro.text = text;
			}
		}

		get
		{
			return _rank;
		}
	}
	private int _rank;

	public void recycle()
	{
		if (member != null)
		{
			// When this object is recycled remove this object from that members friendInfos.
			member.onMemberUpdated -= memberUpdated;
			hasRegisteredEvents = false;
		}
		_member = null;
	}

	void OnDestroy()
	{
		if (member != null)
		{
			// When this object is destroyed remove this object from that members friendInfos.
			member.onMemberUpdated -= memberUpdated;
		}
	}
}
