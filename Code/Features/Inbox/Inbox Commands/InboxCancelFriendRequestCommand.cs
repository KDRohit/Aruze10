using UnityEngine;
using System.Collections;

public class InboxCancelFriendRequestCommand : InboxCommand
{
	public const string FRIEND_REQUEST = "cancel_friend_request";

	/// <inheritdoc/>
	public override void execute(InboxItem inboxItem)
	{
		NetworkFriends.instance.rejectFriend(inboxItem.senderSocialMember);
	}

	/// <inheritdoc/>
	public override string actionName
	{
		get { return FRIEND_REQUEST; }
	}
}