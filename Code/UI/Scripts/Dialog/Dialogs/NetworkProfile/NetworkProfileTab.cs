using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

public class NetworkProfileDisplay : NetworkProfileTabBase
{
	public TextMeshPro nameLabel;
	public TextMeshPro statusLabel;
	public TextMeshPro genderLabel;
	public TextMeshPro locationLabel;
	public TextMeshPro networkIdLabel;
	public TextMeshPro memberSinceLabel;
	public VIPIconHandler vipIcon;
	public UISprite loyaltyLougneBadge;

	public NetworkProfileStatPanel hirStats;

	public MeshRenderer profileImage;

	public ClickHandler reportButton;
	
	public ImageButtonHandler editButton;

	private bool useDefaults = false;
	private bool isProfileSetup = false;
	private NetworkProfileDialog dialog;

	// Achievements
	[SerializeField] private MeshRenderer trophyImage;
	[SerializeField] private GameObject defaultTrophy;
    [SerializeField] private TextMeshPro trophyName;
	[SerializeField] private AchievementRankIcon rankIcon;
	[SerializeField] private GameObject trophyTooltip;
	[SerializeField] private GameObject rankTooltipPrefab;
	[SerializeField] private GameObject rankTooltipAnchor;	
	[SerializeField] private TextMeshPro trophyTooltipLabel;

	[SerializeField] private ClickHandler trophyClickHandler;
	[SerializeField] private ClickHandler trophyShroudHandler;
	[SerializeField] private ClickHandler rankClickHandler;

	private NetworkProfileRankTooltip rankTooltip;
	
	private const string DEFAULT_NAME = "Slots Player";
	private const string DEFAULT_STATUS = "I am feeling lucky today!";

	public override IEnumerator onIntro(NetworkProfileDialog.ProfileDialogState fromState, string extraData = "")
	{
		StatsManager.Instance.LogCount("dialog", "ll_profile", "profile", "view", statFamily, member.networkID.ToString());
		yield return null;
	}
	
	public virtual void init(SocialMember member, NetworkProfileDialog dialog)
	{
		this.member = member;
		this.dialog = dialog;
		setupProfile();
		reportButton.registerEventDelegate(reportClicked);
		editButton.registerEventDelegate(editClicked);
	}

	private GameObject attachPrefab(GameObject prefab, Transform parent)
	{
		if (prefab != null && parent != null)
		{
			GameObject newObject = GameObject.Instantiate(prefab, parent);
		}
		else
		{
			Debug.LogErrorFormat("NetworkProfileDisplay.cs -- attachPrefab -- either prefab or parent were null, bailing.");
		}
		return null;

	}
	
    public void setupProfile()
	{
		// Download the profile image and put it on the renderer.
		DisplayAsset.loadTextureToRenderer(profileImage, member.getLargeImageURL);

		if (!string.IsNullOrEmpty(member.networkProfile.name))
		{
			nameLabel.text = member.networkProfile.name;
		}
		else if (!string.IsNullOrEmpty(member.fullName))
		{
			nameLabel.text = member.firstNameLastInitial;
		}
		else
		{
			nameLabel.text = DEFAULT_NAME;
		}

		if (string.IsNullOrEmpty(member.networkProfile.status))
		{
			statusLabel.text = DEFAULT_STATUS;
		}
		else
		{
			statusLabel.text = member.networkProfile.status;
		}

		locationLabel.text = member.networkProfile.location;
		
		string genderText = "";
		if (member.networkProfile.gender == "male")
		{
			genderText = "Male";
		}
		else if (member.networkProfile.gender == "female")
		{
			genderText = "Female";
		}
		genderLabel.text = genderText;

        if (!string.IsNullOrEmpty(member.networkID) && member.networkProfile.joinTime >= 0)
        {
            System.DateTime joinDate = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
            joinDate = joinDate.AddSeconds(member.networkProfile.joinTime);
            memberSinceLabel.text = string.Format("LL Member Since: {0}", joinDate.ToString("MM/yy"));
        }
        else
        {
            memberSinceLabel.text = "";
        }

		if (member.isUser)
		{
			vipIcon.setLevel(VIPLevel.getEventAdjustedLevel());
		}
		else
		{
			vipIcon.setLevel(member.vipLevel);
		}

		if (member.isUser && !string.IsNullOrEmpty(member.networkProfile.networkID))
		{
			networkIdLabel.text = string.Format("LL ID: {0}", member.networkProfile.networkID);
		}
		else
		{
			networkIdLabel.text = "";
		}

		bool isNetworkConnected = !string.IsNullOrEmpty(member.networkProfile.networkID);
		// If they are a connected user, show the badge, otherwise hide it.
		loyaltyLougneBadge.gameObject.SetActive(isNetworkConnected);

		if (hirStats != null)
		{
			// This isn't present in the achievements version of the dialog.
			if (member.networkProfile.gameStats != null && member.networkProfile.gameStats.ContainsKey("hir"))
			{
				hirStats.setLabels(member.networkProfile.gameStats["hir"]);
			}
			else
			{
				hirStats.setLabels(null);
			}
		}

		if (member.networkProfile != null && member.networkProfile.displayAchievement != null)
		{
			setFavoriteTrophy(member.networkProfile.displayAchievement);
		}
		else
		{
			// Use the default settings for this.
			if (trophyImage != null)
			{
				SafeSet.gameObjectActive(trophyImage.gameObject, false);
			}
			string tooltipKey = member.isUser ? "choose_completed_trophy" : "no_favorite_trophy_selected_yet";
			SafeSet.labelText(trophyTooltipLabel, Localize.text(tooltipKey, ""));
			SafeSet.gameObjectActive(defaultTrophy, true);
			SafeSet.labelText(trophyName, Localize.text("no_selected_trophy", ""));
		}
		
		if (rankIcon != null)
		{
			// Need to nullcheck while we support the old dialog.
			rankIcon.setRank(member);
		}

		if (trophyClickHandler != null)
		{
			// Need to nullcheck while we support the old dialog.
			trophyClickHandler.registerEventDelegate(trophyClicked);
		}

		if (trophyShroudHandler != null)
		{
			trophyShroudHandler.registerEventDelegate(trophyClicked);
		}
		
		if (rankClickHandler != null)
		{
			// Need to nullcheck while we support the old dialog.
			rankClickHandler.registerEventDelegate(rankClicked);
		}

		if (member == SlotsPlayer.instance.socialMember)
		{
			reportButton.gameObject.SetActive(false);
			editButton.gameObject.SetActive(true);
		}
		else
		{
			// We dont wan't players to be able to report their own profiles
			reportButton.gameObject.SetActive(true);
			editButton.gameObject.SetActive(false);
		}
		
		if (trophyTooltip != null)
		{
			trophyTooltip.SetActive(false); // Default this to off.
		}

		if (rankTooltipAnchor != null && rankTooltipPrefab != null)
		{
			GameObject rankTooltipObject = GameObject.Instantiate(rankTooltipPrefab, rankTooltipAnchor.transform);
			if (rankTooltipObject == null)
			{
			    Debug.LogErrorFormat("NetworkProfileDisplay.cs -- setupProfile -- could not create the rank tooltip object...");
			}
			else
			{
				rankTooltipObject.SetActive(false);
				rankTooltip = rankTooltipObject.GetComponent<NetworkProfileRankTooltip>();
			}
		}
		isProfileSetup = true;
	}

	public override void setFavoriteTrophy(Achievement achievement)
	{
		SafeSet.gameObjectActive(trophyImage.gameObject, true);
		SafeSet.gameObjectActive(defaultTrophy, false);
		if (trophyImage != null)
		{
			member.networkProfile.displayAchievement.loadTextureToRenderer(trophyImage);
		}
		SafeSet.labelText(trophyName, member.networkProfile.displayAchievement.name);
	}
	
	private void editClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "ll_profile",
			phylum: "profile",
			klass: "edit",
			family: "statFamily",
			genus: member.networkID.ToString());
		dialog.switchState(NetworkProfileDialog.ProfileDialogState.PROFILE_EDITOR);
	}

	private void reportClicked(Dict args = null)
	{
		dialog.isOpeningReportDialog = true;
		Dialog.close();
		NetworkProfileReportDialog.showDialog(member, SchedulerPriority.PriorityType.IMMEDIATE);
	}

	private void trophyClicked(Dict args = null)
	{
		if (member.networkProfile.displayAchievement != null)
		{
			StatsManager.Instance.LogCount(
				counterName: "dialog",
				kingdom: "ll_profile",
				phylum: "favorite_trophy",
				klass: "click",
				family: member.networkProfile.displayAchievement.id,
				genus: member.networkID);
			dialog.showSpecificTrophy(Dict.create(D.ACHIEVEMENT, member.networkProfile.displayAchievement));
		}
		else
		{
			trophyTooltip.SetActive(!trophyTooltip.activeSelf);
		}
	}

	public override void rankClicked(Dict args = null)
	{
		if (rankTooltip != null)
		{
			rankTooltip.show(member);
		}
	}

	public override void hideRankTooltip() 
	{
		if (rankTooltip != null)
		{
			rankTooltip.hide ();
		}		

	}

}
