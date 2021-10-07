using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

public class NetworkProfileTab : NetworkProfileTabBase
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
	public FacebookFriendInfo fbInfo;

	public ClickHandler reportButton;
	
	public ImageButtonHandler editButton;

	protected bool useDefaults = false;
	protected bool isProfileSetup = false;
	protected NetworkProfileDialog dialog;
	
	protected const string DEFAULT_NAME = "Slots Player";
	protected const string DEFAULT_STATUS = "I am feeling lucky today!";

	public override IEnumerator onIntro(NetworkProfileDialog.ProfileDialogState fromState, string extraData = "")
	{
		if (NetworkFriends.instance.isEnabled && member.isUser)
		{
			StatsManager.Instance.LogCount(
				counterName: "dialog",
				kingdom: "ll_profile",
				phylum: "profile", 
				klass: "view",
				family: SocialMember.allFriends.Count.ToString(),
				genus: member.networkID.ToString());
		}
		else
		{
			StatsManager.Instance.LogCount(
				counterName: "dialog",
				kingdom: "ll_profile",
				phylum: "profile", 
				klass: "view",
				family: statFamily,
				genus: member.networkID.ToString());
		}

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

	protected GameObject attachPrefab(GameObject prefab, Transform parent)
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
		fbInfo.member = member;

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
		if (member.networkProfile.gender.ToLower() == "male")
		{
			genderText = "Male";
		}
		else if (member.networkProfile.gender.ToLower() == "female")
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

		isProfileSetup = true;
	}
	
	protected void editClicked(Dict args = null)
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

	protected void reportClicked(Dict args = null)
	{
		dialog.isOpeningReportDialog = true;
		Dialog.close();
		NetworkProfileReportDialog.showDialog(member, SchedulerPriority.PriorityType.IMMEDIATE);
	}
}
