using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InboxCoinsListItemWrapper : InboxListItemWrapper
{
	/// <inheritdoc/>
	public override void setup(InboxListItem listItem, InboxItem inboxItem)
	{
		base.setup(listItem, inboxItem);

		setButton();

		listItem.toggleCloseButton(inboxItem.hasCloseCommand);
		listItem.setTimer(false);
		string name = !string.IsNullOrEmpty(inboxItem.senderSocialMember.firstName)
			? inboxItem.senderSocialMember.firstName
			: Localize.text(MISSING_FRIEND_NAME);

		if (isCreditsItem)
		{
			listItem.setMessageLabel(Localize.text("inbox_credit_message_{0}", name));
			listItem.setState("coins_gifted");
			listItem.toggleGiftRibbon(true);
		}
		else if (isHelpItem)
		{
			listItem.setMessageLabel(Localize.text("help_inbox_credits_{0}", name));
			listItem.setState("coins_requested");
			listItem.toggleGiftRibbon(false);
		}
		
		listItem.setButtonAnimation(InboxListItem.ItemAnimations.Idle);
	}

	/// <inheritdoc/>
	public override void action()
	{
		base.action();

		if (isCreditsItem)
		{
			SlotsPlayer.instance.creditsAcceptLimit.subtract(1);

			StatsInbox.logCollectOrHelp
			(
				phylum:"coins",
				klass:"collect",
				senderZid:inboxItem.senderZid,
				amount:SlotsPlayer.instance.creditsAcceptLimit.amountRemaining
			);

			listItem.toggleCoinBurst(true);
		}
		else
		{
			StatsInbox.logCollectOrHelp
			(
				phylum:"coins",
				klass:"help",
				senderZid:inboxItem.senderZid,
				amount:SlotsPlayer.instance.creditSendLimit
			);

			SlotsPlayer.instance.creditSendLimit--;

			listItem.toggleHelpCoinBurst(true);
		}

		listItem.setButtonAnimation(InboxListItem.ItemAnimations.Outro);
	}

	/// <inheritdoc/>
	public override void refresh()
	{
		setButton();

		if (isHelpItem && InboxInventory.getLimitRemaining(inboxItem) == 0 && !inboxItem.hasAcceptedItem)
		{
			listItem.hide();
		}
	}

	/// <inheritdoc/>
	protected override void setButton()
	{
		if (InboxInventory.getLimitRemaining(inboxItem) > 0)
		{
			listItem.setLimitReached(false);
			listItem.toggleButton(inboxItem.canBeClaimed);

			if (isHelpItem)
			{
				listItem.setButtonText(Localize.text("help_tab"));
			}
			else
			{
				listItem.setButtonText(Localize.text("collect"));
			}
		}
		else if (inboxItem.canBeClaimed && !inboxItem.hasAcceptedItem)
		{
			listItem.toggleButton(false);
			listItem.setLimitReached(true);
			listItem.setButtonText(Localize.text("limit_reached"));
		}
	}

	protected bool isCreditsItem
	{
		get { return inboxItem != null && inboxItem.itemType == InboxItem.InboxType.FREE_CREDITS; }
	}

	protected bool isHelpItem
	{
		get { return inboxItem != null && inboxItem.itemType == InboxItem.InboxType.SEND_CREDITS; }
	}
}