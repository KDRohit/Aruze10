using UnityEngine;
using System.Collections;
using Com.Scheduler;
using TMPro;

public class NetworkProfileMOTDTooltip : DialogBase
{
	[SerializeField] private Animator animator;
	[SerializeField] private TextMeshPro label;
	[SerializeField] private ClickHandler editProfileButton;
	[SerializeField] private ClickHandler closeButton;
	[SerializeField] private ClickHandler profileButton;
	[SerializeField] private MeshRenderer profileRenderer;
	[SerializeField] private GameObject arrowParent;
	[SerializeField] private GameObject rankIconParent;

	private string statFamily = "";
	private const string CONNECTED_LOC_KEY = "dont_forget_to_custom_your_profile";
	private const string UNCONNECTED_LOC_KEY = "you_can_now_customize_your_profile";
	
	public override void init()
	{
		statFamily = LinkedVipProgram.instance.isConnected ? "loyalty_lounge" : "non_loyalty_lounge";
		animator.Play("intro"); // Start the animation.
		string localizationKey = LinkedVipProgram.instance.isConnected ? CONNECTED_LOC_KEY : UNCONNECTED_LOC_KEY;
		label.text = Localize.text(localizationKey, "");
		editProfileButton.registerEventDelegate(editClicked);
		profileButton.registerEventDelegate(editClicked);
		closeButton.registerEventDelegate(closeClicked);
		string url = SlotsPlayer.instance.socialMember.getImageURL;
		DisplayAsset.loadTextureToRenderer(profileRenderer, url, PhotoSource.profileBackupImage, true);

		if (NetworkAchievements.isEnabled)
		{
		    AchievementRankIcon.loadRankIconToAnchor(rankIconParent, SlotsPlayer.instance.socialMember);
		}
		
		// Sync the position of this with the one in the overlay.
		if (Overlay.instance != null &&
			Overlay.instance.topHIR != null &&
			Overlay.instance.topHIR.profileButton != null)
		{
			Vector3 delta = profileButton.transform.localPosition;

			CommonTransform.matchScreenPosition(profileButton.transform, Overlay.instance.topHIR.profileButton.transform);

			delta = profileButton.transform.localPosition - delta;

			arrowParent.transform.localPosition = arrowParent.transform.localPosition + delta;
		}
		
		MOTDFramework.markMotdSeen(dialogArgs);
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "ll_profile_motd",
			phylum: "edit_profile_tooltip",
			klass: "view",
			family: statFamily);
	}

	public void Update()
	{
		AndroidUtil.checkBackButton(closeClicked);
	}
	
	public override void close()
	{
		// Cleanup here.
	}

	private void closeClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "ll_profile_motd",
			phylum: "edit_profile_tooltip",
			klass: "close",
			family: statFamily);		
		Dialog.close();		
	}
	
	private void editClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "ll_profile_motd",
			phylum: "edit_profile_tooltip",
			klass: "click",
			family: statFamily);		
		Dialog.close();
		NetworkProfileDialog.showDialog(SlotsPlayer.instance.socialMember, SchedulerPriority.PriorityType.IMMEDIATE);
	}

	public static bool showDialog(string motdKey)
	{
		Dict args = Dict.create(D.MOTD_KEY, motdKey);
		Scheduler.addDialog("network_profile_motd_tooltip", args);
		return true;
	}
}
