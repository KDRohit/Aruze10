using UnityEngine;
using System.Collections;

public class InboxSendCreditsCommand : InboxCommand
{
	public const string SEND_CREDITS = "send_credits";

	/// <inheritdoc/>
	public override void execute(InboxItem inboxItem)
	{
		SocialMember member = SocialMember.findByZId(inboxItem.senderZid);

		if (member != null)
		{
			member.canSendCredits = false;
		}
	}

	/// <inheritdoc/>
	public override string actionName
	{
		get { return SEND_CREDITS; }
	}
}