using UnityEngine;
using System.Collections;

public class InboxMessagesListItemWrapper : InboxListItemWrapper
{
	/// <inheritdoc/>
	public override void setup(InboxListItem listItem, InboxItem inboxItem)
	{
		base.setup(listItem, inboxItem);

		setButton();

		listItem.toggleExclusiveOffer(false);
		listItem.setMessageLabel(inboxItem.message);
		listItem.toggleButton(inboxItem.hasPrimaryCommand);
		listItem.toggleCloseButton(inboxItem.hasCloseCommand);

		if (inboxItem.feature == "Special OOC")
		{
			InboxListItemOOC oocItem = listItem as InboxListItemOOC;
			if (oocItem != null)
			{
				oocItem.enableFreeCoinsTag(true);
			}
		}
		
		
		
		if (inboxItem.primaryCommand is InboxCollectCreditsCommand)
		{
			listItem.swapper.setState("messages_gifted");
		}
		else if (inboxItem.primaryCommand is InboxSpecialOfferCommand)
		{
			listItem.swapper.setState("offer");
			listItem.toggleExclusiveOffer(true);

			string nameToUse = SlotsPlayer.instance.socialMember.firstName.ToLower() != "blank" ? SlotsPlayer.instance.socialMember.firstName : "Spinner";
			GiftChestOffer.instance.refreshOfferData();
			listItem.setMessageLabel
			(
				Localize.text
				(
					"inbox_offer_description",
					nameToUse,
					CommonText.formatNumber(GiftChestOffer.instance.grandTotal),
					GiftChestOffer.instance.purchasePackage.priceLocalized
				)
			);
		}
		else
		{
			listItem.swapper.setState("messages");
		}

		if (inboxItem.primaryCommand is InboxUnlockGameCommand || inboxItem.primaryCommand is InboxCollectSpinsCommand || 
		    (inboxItem.primaryCommand != null && inboxItem.primaryCommand.action.Equals("select_rating")))
		{
			listItem.swapper.setState("messages_unlock_game");

			LobbyOption option = LobbyOption.activeGameOption(inboxItem.gameKey);
			if (option != null && option.game != null && option.game.groupInfo != null)
			{
				string path = SlotResourceMap.getLobbyImagePath(option.game.groupInfo.keyName, option.game.keyName);
				listItem.setGameImage(path);
			}
		}

		if (!string.IsNullOrEmpty(inboxItem.background))
		{
			listItem.loadTextureToBackground(inboxItem.background);
		}
		StatsInbox.logMessage(inboxItem, "view");
	}


	/// <inheritdoc/>
	public override void action()
	{
		StatsInbox.logMessage(inboxItem);
		base.action();

		if (inboxItem.primaryCommand is InboxCollectCreditsCommand || 
			(inboxItem.primaryCommand is InboxEliteCommand && !(inboxItem.primaryCommand is InboxEliteFreeSpinsCommand) )
			)
		{
			listItem.toggleCoinBurst(true);
		}
	}

	/// <inheritdoc/>
	public override void action(string actionKey)
	{
		StatsInbox.logMessage(inboxItem);
		base.action(actionKey);
	}

	/// <inheritdoc/>
	public override void dismiss()
	{
		StatsInbox.logMessage(inboxItem, "close");
		base.dismiss();
	}

	/// <inheritdoc/>
	protected override void setButton()
	{
		listItem.toggleButton(inboxItem.canBeClaimed);

		if (inboxItem.canBeClaimed)
		{
			listItem.setButtonAnimation(InboxListItem.ItemAnimations.Intro);
		}

		listItem.setLimitReached(false);
		listItem.setButtonText(inboxItem.ctaText);
	}
}