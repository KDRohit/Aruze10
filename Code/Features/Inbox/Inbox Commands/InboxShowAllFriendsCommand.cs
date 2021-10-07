using UnityEngine;
using Com.Scheduler;

public class InboxShowAllFriendsCommand : InboxCommand
{
	public const string SHOW_ALL_FRIENDS = "all_friends";
	
	public override void execute(InboxItem inboxItem)
	{
		Dialog.close();
		NetworkProfileDialog.showDialog(SlotsPlayer.instance.socialMember, SchedulerPriority.PriorityType.IMMEDIATE, null, NetworkProfileDialog.MODE_ALL_FRIENDS);
	}
	
	/// <inheritdoc/>
	public override string actionName
	{
		get { return SHOW_ALL_FRIENDS; }
	}
}
