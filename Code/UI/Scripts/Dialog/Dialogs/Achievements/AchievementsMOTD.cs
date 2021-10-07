using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

public class AchievementsMOTD : DialogBase
{
	[SerializeField] private TextMeshPro badgeLabel;
	[SerializeField] private GameObject badgeParent;
	[SerializeField] private MeshRenderer profileRenderer;
	[SerializeField] private ImageButtonHandler closeButton;
	[SerializeField] private ImageButtonHandler viewButton;
	[SerializeField] private ClickHandler profileButton;

	private const float AUDIO_DELAY_INTRO = 0.77f;
	public const string OPEN_MOTD_AUDIO = "MOTDNetworkAchievements";
	
	public override void init()
	{
		if (!downloadedTextureToRenderer(profileRenderer, 0))
		{
			// As a bakcup lets call this if it failed earlier.
			SlotsPlayer.instance.socialMember.setPicOnRenderer(profileRenderer);
		}
		closeButton.registerEventDelegate(closeClicked);
		viewButton.registerEventDelegate(viewClicked);
		profileButton.registerEventDelegate(profileClicked);
		badgeLabel.text = NetworkAchievements.numNew.ToString();
		badgeParent.SetActive(NetworkAchievements.numNew > 0);
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "ll_trophy_motd",
			phylum: "",
			klass: "view",
			family: "",
			genus: "");
		
		MOTDFramework.markMotdSeen(dialogArgs);
		Audio.playSoundMapOrSoundKeyWithDelay(OPEN_MOTD_AUDIO, AUDIO_DELAY_INTRO);
	}

	public override void close()
	{
		// Has to be implemented since its abstract
	}

	void Update()
	{
		AndroidUtil.checkBackButton(closeClicked);
	}

	private void closeClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "ll_trophy_motd",
			phylum: "",
			klass: "close",
			family: "",
			genus: "");		
		Dialog.close();
		if (ExperimentWrapper.NetworkAchievement.activeDiscoveryEnabled)
		{
			NetworkProfileDialog.showDialog(SlotsPlayer.instance.socialMember,
				SchedulerPriority.PriorityType.IMMEDIATE,
				null,
				NetworkProfileDialog.MODE_TROPHIES);	
		}
		
	}

	private void viewClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "ll_trophy_motd",
			phylum: "",
			klass: "click",
			family: "view",
			genus: "");
		Dialog.close();

		// Otherwise move them to the trophies tab directly.
		NetworkProfileDialog.showDialog(SlotsPlayer.instance.socialMember,
			SchedulerPriority.PriorityType.IMMEDIATE,
			null,
			NetworkProfileDialog.MODE_TROPHIES);
	}

	private void profileClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "ll_trophy_motd",
			phylum: "",
			klass: "click",
			family: "profile",
			genus: "");		
		Dialog.close();
		NetworkProfileDialog.showDialog(SlotsPlayer.instance.socialMember, SchedulerPriority.PriorityType.IMMEDIATE);
	}

	public static bool showDialog(string motdKey)
	{
		string dialog = "achievements_motd";
		if (!NetworkAchievements.rewardsEnabled)
		{
			dialog = "achievements_no_rewards_motd";
		}
		else if (NetworkAchievements.isBackfillAwardAvailable())
		{
			dialog = "achievements_update_motd";
		}

		Dialog.instance.showDialogAfterDownloadingTextures(dialog,
			SlotsPlayer.instance.socialMember.getImageURL,
			Dict.create(D.MOTD_KEY, motdKey),
			priorityType: SchedulerPriority.PriorityType.IMMEDIATE,
			isPersistent: true);
		return true;
	}
}
