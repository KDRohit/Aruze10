using UnityEngine;
using System.Collections;

public class InboxCancelSpecialOfferCommand : InboxCommand
{
	public const string CANCEL_SPECIAL_OFFER = "cancel_special_offer";

	public override void execute(InboxItem inboxItem)
	{
		GiftChestOffer.instance.views = GiftChestOffer.instance.maxViews;
	}

	/// <inheritdoc/>
	public override string actionName
	{
		get { return CANCEL_SPECIAL_OFFER; }
	}
}