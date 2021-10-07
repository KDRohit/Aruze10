using UnityEngine;
using System.Collections;
using Com.Scheduler;

public class NetworkProfileMOTD : DialogBase
{
	public MeshRenderer profileRenderer;
	public ImageButtonHandler closeButton;
	public ImageButtonHandler openProfileButton;
	public ButtonHandler topProfileButton;
	public MeshRenderer smallProfileRenderer;
	public Animator animator;

	private const string INTRO = "networkProfileMOTD_intro";
	private const string IDLE = "networkProfileMOTD_idle";

	private static string motdKey = "";

	private string statKingdom = "";
	public override void init()
	{
		if (motdKey == "network_profiles")
		{
			statKingdom = "ll_profile_motd";
		}
		else
		{
			statKingdom = "profile_updates";
		}
		animator.Play(INTRO);
		Audio.play("minimenuopen0");
		DisplayAsset.loadTextureToRenderer(profileRenderer, SlotsPlayer.instance.socialMember.getImageURL, "", true);
		DisplayAsset.loadTextureToRenderer(smallProfileRenderer, SlotsPlayer.instance.socialMember.getImageURL, "", true);
		closeButton.registerEventDelegate(closeClicked);
		openProfileButton.registerEventDelegate(openProfileClicked);
		MOTDFramework.markMotdSeen(dialogArgs);

		// Sync the position of this with the one in the overlay.
		if (Overlay.instance != null &&
			Overlay.instance.topHIR != null &&
			Overlay.instance.topHIR.profileButton != null)
		{
			Vector3 newPosition = Vector3.one;
			newPosition.x = Overlay.instance.topHIR.profileButton.transform.localPosition.x;
			newPosition.y = Overlay.instance.topHIR.profileButton.transform.parent.localPosition.y;
			topProfileButton.transform.localPosition = newPosition;
		}

		topProfileButton.registerEventDelegate(openProfileClicked);
		StatsManager.Instance.LogCount("dialog", statKingdom, "", "view");
	}

	public override void close()
	{
		Audio.play("XoutEscape");
		// Do cleanup
	}

	public void Update()
	{
		AndroidUtil.checkBackButton(closeClicked);
	}	

    private void closeClicked(Dict args = null)
	{
		topProfileButton.gameObject.SetActive(false);
		if (motdKey == "network_profile")
		{
			StatsManager.Instance.LogCount("dialog", statKingdom, "", "close");
		}

		Dialog.close();

	}

    private void openProfileClicked(Dict args = null)
	{
		Dialog.close();
		NetworkProfileDialog.showDialog(SlotsPlayer.instance.socialMember, SchedulerPriority.PriorityType.IMMEDIATE);
		if (!string.IsNullOrEmpty(SlotsPlayer.instance.socialMember.networkID))
		{
			StatsManager.Instance.LogCount("dialog", statKingdom, "", "setup");
		}
		else
		{
			StatsManager.Instance.LogCount("dialog", statKingdom, "", "join");
		}
	}

	public static bool showDialog(string motdKey)
	{
		if (SlotsPlayer.instance.socialMember.networkProfile == null)
		{
			// If the profile is null, ask the server for the profile.
			if (motdKey == "network_profile")
			{
				NetworkProfileAction.getProfile(SlotsPlayer.instance.socialMember, updateAndShowDialog);
			}
			else
			{
				NetworkProfileAction.getProfile(SlotsPlayer.instance.socialMember, updateAndShowNewDialog);
			}
		}
		else
		{
			string dialogName = (motdKey == "network_profile") ? "network_profile_motd" : "network_profile_motd_1_5";
			Dict args = Dict.create(D.PLAYER, SlotsPlayer.instance.socialMember, D.MOTD_KEY, motdKey);
			Dialog.instance.showDialogAfterDownloadingTextures(dialogName,
				SlotsPlayer.instance.socialMember.getImageURL,
				args,
				shouldAbortOnFail:false,
				priorityType: SchedulerPriority.PriorityType.LOW,
				isExplicitPath:true,
				isPersistent:true); // The profile image is persistent since it is used all over the app.
		}
		return true;
	}

	private static void updateAndShowDialog(JSON data)
	{
		NetworkProfileFeature.instance.parsePlayerProfile(data);
		SocialMember member = SlotsPlayer.instance.socialMember;
		Dict args = Dict.create(D.PLAYER, member, D.MOTD_KEY, motdKey);
		// Now that we have updated the network profile object, show the MOTD.
		Dialog.instance.showDialogAfterDownloadingTextures("network_profile_motd",
			member.getImageURL,
			args,
			shouldAbortOnFail:false,
			priorityType: SchedulerPriority.PriorityType.LOW,
			isExplicitPath:true,
			isPersistent:true);  // The profile image is persistent since it is used all over the app.
	}

	private static void updateAndShowNewDialog(JSON data)
	{
		NetworkProfileFeature.instance.parsePlayerProfile(data);
		SocialMember member = SlotsPlayer.instance.socialMember;
		Dict args = Dict.create(D.PLAYER, member, D.MOTD_KEY, motdKey);
		// Now that we have updated the network profile object, show the MOTD.
		Dialog.instance.showDialogAfterDownloadingTextures("network_profile_motd_1_5",
			member.getImageURL,
			args,
			shouldAbortOnFail:false,
			priorityType: SchedulerPriority.PriorityType.LOW,
			isExplicitPath:true,
			isPersistent:true);  // The profile image is persistent since it is used all over the app.
	}	
}
