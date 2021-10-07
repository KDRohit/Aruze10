using UnityEngine;
using System.Collections;
using Com.HitItRich.EUE;
using Com.Scheduler;

public class NetworkLevelAchievementsMOTD : DialogBase
{
	[SerializeField] private ClickHandler closeButton;
	[SerializeField] private ClickHandler viewNowButton;
	[SerializeField] private ClickHandler profileButton;
	[SerializeField] private MeshRenderer profileRenderer;
	
	private const string SHOWN_ACHIEVEMENT_KEY = "";
	
	public override void init()
	{
		if (!downloadedTextureToRenderer(profileRenderer, 0))
		{
			// As a bakcup lets call this if it failed earlier.
			SlotsPlayer.instance.socialMember.setPicOnRenderer(profileRenderer);
		}
		
		closeButton.registerEventDelegate(closeClicked);
		viewNowButton.registerEventDelegate(viewNowClicked);
		profileButton.registerEventDelegate(profileClicked);
		SlotsPlayer.instance.socialMember.setPicOnRenderer(profileRenderer);

		MOTDFramework.markMotdSeen(dialogArgs);
	}

	public override void close()
	{

	}

	protected override void onFadeInComplete()
	{
		base.onFadeInComplete();
	}

	private void profileClicked(Dict args = null)
	{
		NetworkProfileDialog.showDialog(member:SlotsPlayer.instance.socialMember, priorityType: SchedulerPriority.PriorityType.IMMEDIATE);
		Dialog.close();
	}
	
	private void closeClicked(Dict args = null)
	{
		Dialog.close();
		NetworkProfileDialog.showDialog(
			member:SlotsPlayer.instance.socialMember,
			priorityType: SchedulerPriority.PriorityType.HIGH,
			earnedAchievement:null,
			dialogEntryMode:NetworkProfileDialog.MODE_TROPHIES);
	}

	private void viewNowClicked(Dict args = null)
	{
		Dialog.close();
		NetworkProfileDialog.showDialog(
			member:SlotsPlayer.instance.socialMember,
			priorityType: SchedulerPriority.PriorityType.HIGH,
			earnedAchievement:null,
			dialogEntryMode:NetworkProfileDialog.MODE_TROPHIES);
	}

	public static bool showDialog(string motdKey = "")
	{
		Dict args = Dict.create(D.MOTD_KEY, motdKey);
		Dialog.instance.showDialogAfterDownloadingTextures(
			"achievements_loyalty_lounge_motd", SlotsPlayer.instance.socialMember.getLargeImageURL, args);
		return true;
	}
}
