using UnityEngine;
using System.Collections;

public class InboxFriendRequestCommand : InboxCommand
{
	public const string FRIEND_REQUEST = "friend_request";

	/// <inheritdoc/>
	public override void execute(InboxItem inboxItem)
	{
		NetworkFriends.instance.acceptFriend(inboxItem.senderSocialMember);
	}

	/// <inheritdoc/>
	public override string actionName
	{
		get { return FRIEND_REQUEST; }
	}
}