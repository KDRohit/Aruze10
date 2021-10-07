using UnityEngine;
using System.Collections;
using Com.HitItRich.EUE;
using Com.Scheduler;
using TMPro;

public class NetworkFriendsMOTDDialog : DialogBase
{
	private const string OPEN_MOTD_AUDIO = "MOTDNetworkAchievements";
	[SerializeField] private ImageButtonHandler closeButton;
	[SerializeField] private ImageButtonHandler friendsButton;
	[SerializeField] private TextMeshPro friendCodeLabel;
	
	public override void init()
	{
		Audio.play("minimenuopen0");
		SafeSet.registerEventDelegate(closeButton, closeClicked);
		SafeSet.registerEventDelegate(friendsButton, friendsClicked);
		MOTDFramework.markMotdSeen(dialogArgs); // Mark it as seen.

		if (SlotsPlayer.instance != null &&
			SlotsPlayer.instance.socialMember != null &
			SlotsPlayer.instance.socialMember.networkProfile != null)
		{
			SafeSet.labelText(friendCodeLabel, SlotsPlayer.instance.socialMember.networkProfile.friendCode);
		}
		else
		{
			// if we failed to get the friend code, leave it as the default value.
		}

		Audio.play(OPEN_MOTD_AUDIO);

		string motdKey = dialogArgs == null ? "" : (string)dialogArgs.getWithDefault(D.MOTD_KEY, "");
		string location = string.IsNullOrEmpty(motdKey) ? "carousel" : "app_entry";
		
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "friends",
			phylum: "intro",
			klass: location,
			family: "",
			genus: "view");
	}

	public override void close()
	{
		// Cleanup here.
	}

	private void closeClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "friends",
			phylum: "intro",
			klass: "close",
			family: "",
			genus: "click");
		Dialog.close();
		
		NetworkProfileDialog.showDialog(SlotsPlayer.instance.socialMember, 
			SchedulerPriority.PriorityType.HIGH,
			null,
			NetworkProfileDialog.MODE_FIND_FRIENDS);
		
	}

	private void friendsClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "friends",
			phylum: "intro",
			klass: "CTA",
			family: "",
			genus: "click");
		
		Dialog.close();
		NetworkProfileDialog.showDialog(SlotsPlayer.instance.socialMember,
			SchedulerPriority.PriorityType.HIGH,
		    null,
		    NetworkProfileDialog.MODE_FIND_FRIENDS);
	}

	
	public static bool showDialog(string motdKey = "")
	{
		Scheduler.addDialog("network_friends_motd", Dict.create(D.MOTD_KEY, motdKey));
		return true;
	}
}
