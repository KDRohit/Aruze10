using UnityEngine;
using System.Collections;
using Com.Scheduler;
using TMPro;

public class ReactivateFriendReceiverInviteDialog : DialogBase
{
	public TextMeshPro bodyMessageLabel;
	public TextMeshPro coinAmountLabel;
	public FacebookFriendInfo senderInfo;

	private long coinAmount;
	private int moreSenders;
	private string eventID = string.Empty;

	public override void init()
	{
		JSON data = dialogArgs.getWithDefault(D.DATA, null) as JSON;
		eventID = data.getString("event", "");
		coinAmount = data.getLong("amount", 0);
		moreSenders = data.getInt("more_senders", 0);
		coinAmountLabel.text = CreditsEconomy.convertCredits(coinAmount);

		string senderZID = data.getString("sender", "");
		SocialMember sender = SocialMember.findByZId(senderZID);
		string senderNameText = string.Empty;
		senderNameText = string.Format("{0}{1}</color>", ReactivateFriendSenderOfferDialog.HIGHLIGHT_COLOR, sender.fullName);
		senderInfo.member = sender;

		if (moreSenders == 0)
		{
			bodyMessageLabel.text = Localize.text("reactivate_friend_receiver_invite_desc_{0}", senderNameText);
		}
		else
		{
			string moreSendersText = string.Format("{0}{1}</color>", ReactivateFriendSenderOfferDialog.HIGHLIGHT_COLOR, moreSenders);
			bodyMessageLabel.text = Localize.text("reactivate_friend_receiver_invite_desc_{0}_{1}", senderNameText, moreSendersText);
		}

		Audio.playWithDelay("LogoInRAF", ReactivateFriendSenderOfferDialog.LOGO_ANIM_DELAY);
		Audio.playWithDelay("SummaryFanfareRAF", ReactivateFriendSenderOfferDialog.BG_MUSIC_DELAY);
		StatsManager.Instance.LogCount("dialog", "reactivate_friend", "receiver_invite", "", "", "view");
	}

	protected virtual void Update()
	{
		AndroidUtil.checkBackButton(collectClicked);
	}

	public void collectClicked()
	{
		Audio.play("CollectSubmitRAF");
		SlotsPlayer.addCredits(coinAmount, "Reactivate Friend: recipient accepted.", playCreditsRollupSound:false, reportToGameCenterManager:false);
		Server.adjustKnownCredits(coinAmount);
		ReactivateFriendAction.confirmAccept(eventID);
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
			Debug.LogError("ReactivateFriendReceiverInviteDialog: reponse is null.");
			return;
		}

		Dict args = Dict.create(D.DATA, response);

		Scheduler.addDialog("reactivate_friend_receiver_invite", args);
	}
}
