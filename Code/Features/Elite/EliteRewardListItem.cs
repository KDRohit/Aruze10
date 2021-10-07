using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum EliteRewardDisplayType
{
	DailyCoins = 0,
	FreeGift,
	SurpriseGift
}

public class EliteRewardListItem : MonoBehaviour
{
	[SerializeField] private ObjectSwapper stateSwapper;
	[SerializeField] private ObjectSwapper giftboxSwapper;
	[SerializeField] private TextMeshPro descriptionLabel;
	[SerializeField] private TextMeshPro buttonLabel;
	[SerializeField] private TextMeshPro timerLabel;
	[SerializeField] private AnimationListController.AnimationInformationList buttonAnimInfo;
	[SerializeField] private ButtonHandler claimButton ;

	private const string LOCKED_STATE = "locked";
	private const string UNLOCKED_UNCLAIMEDSTATE = "unlocked_unclaimed";
	private const string UNLOCKED_CLAIMED_STATE = "unlocked_claimed";
	private EliteRewardDisplayType displayType = EliteRewardDisplayType.DailyCoins;
	private EliteDialog.onActionButtonClickDelegate callback;
	private bool tabSwitched = false;
	private InboxItem inboxItem;

	public void OnEnable()
	{
		if (EliteManager.hasActivePass && tabSwitched)
		{
			StartCoroutine(AnimationListController.playListOfAnimationInformation(buttonAnimInfo));
			tabSwitched = false;
		}
	}

	public void OnDisable()
	{
		tabSwitched = true;
	}

	public void setup(int index, EliteDialog.onActionButtonClickDelegate callback)
	{
		this.callback = callback;
		displayType = (EliteRewardDisplayType) index; 
		setupDisplay(displayType);
		if (!EliteManager.hasActivePass)
		{
			stateSwapper.setState(LOCKED_STATE);
		}
		else
		{
			if (doesInboxContainMessageOfType(displayType))
			{
				stateSwapper.setState(UNLOCKED_UNCLAIMEDSTATE);
			}
			else
			{
				EliteManager.rolloverTimer.registerLabel(timerLabel);
				stateSwapper.setState(UNLOCKED_CLAIMED_STATE);
			}

			StartCoroutine(AnimationListController.playListOfAnimationInformation(buttonAnimInfo));
			claimButton.registerEventDelegate(performAction);
		}
	}

	private void setupDisplay(EliteRewardDisplayType type)
	{
		switch (type)
		{
			case EliteRewardDisplayType.DailyCoins:
				giftboxSwapper.setState("giftbox_small_00");
				descriptionLabel.text = Localize.text("elite_reward_lost_bet");
				buttonLabel.text = "Go Claim!";
				break;
			case EliteRewardDisplayType.FreeGift:
				giftboxSwapper.setState("giftbox_large_00");
				descriptionLabel.text = Localize.text("elite_reward_free_gift");
				buttonLabel.text = "Shop Now";
				break;
			case EliteRewardDisplayType.SurpriseGift:
				giftboxSwapper.setState("giftbox_small_01");
				descriptionLabel.text = Localize.text("elite_reward_surprise"); 
				buttonLabel.text = "Go Claim!";
				break;
		}
	}

	private bool doesInboxContainMessageOfType(EliteRewardDisplayType type)
	{
		switch (type)
		{
			case EliteRewardDisplayType.DailyCoins:
				InboxItem cashBackItem = InboxInventory.findItemByCommand<InboxEliteCashBackCommand>();
				return cashBackItem != null;
			case EliteRewardDisplayType.FreeGift:
				return true;
			case EliteRewardDisplayType.SurpriseGift:
				InboxItem mysteryGiftItem = InboxInventory.findItemByCommand<InboxEliteCommand>();
				return mysteryGiftItem != null;
			default:
				return false;
		}
	}
	
	private void performAction(Dict args = null)
	{
		Audio.play("ButtonConfirm");
		switch (displayType)
		{
			case EliteRewardDisplayType.FreeGift:
				StatsElite.logRewardListItemClicked("free_gift");
				BuyCreditsDialog.showDialog();
				break;
			case EliteRewardDisplayType.DailyCoins:
				StatsElite.logRewardListItemClicked("daily_cb");
				InboxDialog.showDialog(InboxDialog.MESSAGES_STATE);
				break;
			case EliteRewardDisplayType.SurpriseGift:
				StatsElite.logRewardListItemClicked("daily_surprise");
				InboxDialog.showDialog(InboxDialog.MESSAGES_STATE);
				break;
		}

		if (callback != null)
		{
			callback();
		}
	}
}
