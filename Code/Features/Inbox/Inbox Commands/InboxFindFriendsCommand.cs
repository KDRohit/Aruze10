using UnityEngine;
using Com.Scheduler;

public class InboxFindFriendsCommand : InboxCommand
{
	public const string FIND_FRIENDS = "find_friends";

	public override void execute(InboxItem inboxItem)
	{
		Dialog.close();
		NetworkProfileDialog.showDialog(SlotsPlayer.instance.socialMember, SchedulerPriority.PriorityType.IMMEDIATE, null, NetworkProfileDialog.MODE_FIND_FRIENDS);
	}
	
	/// <inheritdoc/>
	public override string actionName
	{
		get { return FIND_FRIENDS; }
	}
}
