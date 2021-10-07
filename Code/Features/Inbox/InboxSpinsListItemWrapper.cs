using UnityEngine;
using System.Collections;

public class InboxSpinsListItemWrapper : InboxListItemWrapper
{
	/// <inheritdoc/>
	public override void setup(InboxListItem listItem, InboxItem inboxItem)
	{
		base.setup(listItem, inboxItem);

		LobbyOption option = LobbyOption.activeGameOption(inboxItem.gameKey);
		
		if (option != null && option.game != null && option.game.groupInfo != null)
		{
			string name = !string.IsNullOrEmpty(inboxItem.senderSocialMember.firstName)
				? inboxItem.senderSocialMember.firstName
				: Localize.text(MISSING_FRIEND_NAME);

			listItem.setMessageLabel(Localize.text("inbox_spin_message_{0}_{1}", name, option.game.name));
			
			string path = SlotResourceMap.getLobbyImagePath(option.game.groupInfo.keyName, option.game.keyName);
			listItem.setGameImage(path);

			if (listItem is InboxListItemRichPass)
			{
				((InboxListItemRichPass)listItem).setGoldPassRequirement(option.isGoldPass);
			}

		}
		else if (listItem is InboxListItemRichPass)
		{
			((InboxListItemRichPass)listItem).setGoldPassRequirement(false);
		}

		listItem.setButtonAnimation(InboxListItem.ItemAnimations.Intro);
		listItem.toggleCloseButton(inboxItem.hasCloseCommand);
		listItem.toggleGiftRibbon(true);
		
		setButton();
	}

	/// <inheritdoc/>
	public override void refresh()
	{
		setButton();
	}

	/// <inheritdoc/>
	public override void action()
	{
		StatsInbox.logCollectOrHelp
		(
			phylum:"spins",
			klass:"collect",
			senderZid:inboxItem.senderZid,
			gameName:inboxItem.gameKey,
			SlotsPlayer.instance.giftBonusAcceptLimit.amountRemaining
		);
		
		SlotsPlayer.instance.giftBonusAcceptLimit.subtract(1);
		base.action();
	}

	/// <inheritdoc/>
	protected override void setButton()
	{
		if (InboxInventory.getLimitRemaining(inboxItem) > 0)
		{
			listItem.setLimitReached(false);
			listItem.toggleButton(inboxItem.canBeClaimed);
			listItem.setButtonText(Localize.text("play_now"), Localize.text("vip_level_title_gold"));
		}
		else if (inboxItem.canBeClaimed && !inboxItem.hasAcceptedItem)
		{
			listItem.toggleButton(false);
			listItem.setLimitReached(true);
			listItem.setButtonText(Localize.text("limit_reached"));
		}
	}
}