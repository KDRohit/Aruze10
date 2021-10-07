using UnityEngine;
using System.Collections;
using Com.Scheduler;
using TMPro;

public class ReactivateFriendSenderRewardDialog : DialogBase 
{
	public TextMeshPro bodyMessageLabel;
	public TextMeshPro coinAmountLabel;
	public FacebookFriendInfo receiverInfo;

	private long coinAmount;
	private string eventID = string.Empty;

	public override void init()
	{
		JSON data = dialogArgs.getWithDefault(D.DATA, null) as JSON;
		eventID = data.getString("event", "");
		coinAmount = data.getLong("amount", 0);
		coinAmountLabel.text = CreditsEconomy.convertCredits(coinAmount);

		string receiverZID = data.getString("recipient", "");
		SocialMember member = SocialMember.findByZId(receiverZID);
		string receiverNameText = string.Empty;
		receiverNameText = string.Format("{0}{1}</color>", ReactivateFriendSenderOfferDialog.HIGHLIGHT_COLOR, member.fullName);
		receiverInfo.member = member;

		bodyMessageLabel.text = Localize.text("reactivate_friend_sender_reward_desc_{0}", receiverNameText);

		Audio.playWithDelay("LogoInRAF", ReactivateFriendSenderOfferDialog.LOGO_ANIM_DELAY);
		Audio.playWithDelay("SummaryFanfareRAF", ReactivateFriendSenderOfferDialog.BG_MUSIC_DELAY);
		StatsManager.Instance.LogCount("dialog", "reactivate_friend", "sender_reward", "", "", "view");
	}

	protected virtual void Update()
	{
		AndroidUtil.checkBackButton(collectClicked);
	}

	public void collectClicked()
	{
		Audio.play("CollectSubmitRAF");
		SlotsPlayer.addCredits(coinAmount, "Reactivate Friend: sender's final reward.", playCreditsRollupSound:false, reportToGameCenterManager:false);
		Server.adjustKnownCredits(coinAmount);
		ReactivateFriendAction.confirmFinish(eventID);
		//StatsManager.Instance.LogCount("dialog", kingdom, phylum, "", "collect", "click");
		Dialog.close();
	}

	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
	}

	public static void showDialog(JSON response)
	{
		if (response == null)
		{
			Debug.LogError("ReactivateFriendSenderRewardDialog: reponse is null.");
			return;
		}

		Dict args = Dict.create(D.DATA, response);

		Scheduler.addDialog("reactivate_friend_sender_reward", args);
	}
}
