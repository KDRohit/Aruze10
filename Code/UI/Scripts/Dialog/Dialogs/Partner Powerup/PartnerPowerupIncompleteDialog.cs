using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

public class PartnerPowerupIncompleteDialog : DialogBase 
{
	public TextMeshPro rewardAmount;
	public TextMeshPro inconpleteDate;

	public GameObject prizeArea; // Doesn't appear when you or your partner dont win.

	public ButtonHandler exitButton;
	public ButtonHandler collectButton;

	public FacebookFriendInfo userInfo;
	public FacebookFriendInfo buddyInfo;

	public Renderer userFrozenHammer;
	public Renderer userRegularHammer;
	public Renderer buddyFrozenHammer;
	public Renderer buddyRegularHammer;

	private static Vector4 NORMAL_ALPHA = new Vector4(0.5f, 0.5f, 0.5f, 0.5f);
	private static Vector4 NO_ALPHA = new Vector4(0.5f, 0.5f,0.5f, 0f);
	private const string TINT_VARIABLE = "_TintColor";

	private const int USER_WON = 2;
	private const int BUDDY_WON = 1;

	// a couple conditions define what stats we send so, lets figure that out in init if we can
	private string whoCompletedStateKey = "";
	private string eventID = "";
	private static string collectOrNotStatString = "ok";

	public override void init()
	{
		long creditsAwarded = 0;
		int result = 0;
		string buddyZid = "";
		string buddyFBID = "-1";

	    int dateCompleted = (int) dialogArgs.getWithDefault(D.END_TIME, GameTimer.currentTime);
	    inconpleteDate.text = Localize.text("completed_on_{0}",
	                              Common.convertTimestampToDatetime(dateCompleted).ToShortDateString());

	    inconpleteDate.gameObject.SetActive(false);
		eventID = (string)dialogArgs.getWithDefault(D.EVENT_ID, "");
		creditsAwarded = (long)dialogArgs.getWithDefault(D.AMOUNT, 0);
		result = (int)dialogArgs.getWithDefault(D.DATA, 0);
		buddyZid = (string)dialogArgs.getWithDefault(D.PLAYER, "");

		// Normally we have to make sure this isn't 0, but we should do that everywhere we pass it.
		buddyFBID = (string)dialogArgs.getWithDefault(D.PLAYER, "-1");
		SocialMember buddy = CommonSocial.findOrCreate(buddyFBID, buddyZid);
		buddyInfo.member = buddy;

		SocialMember user = SlotsPlayer.instance.socialMember;

		userInfo.member = user;

		userRegularHammer.material.SetVector(TINT_VARIABLE, NO_ALPHA);
		userFrozenHammer.material.SetVector(TINT_VARIABLE, NO_ALPHA);

		buddyRegularHammer.material.SetVector(TINT_VARIABLE, NO_ALPHA);
		buddyFrozenHammer.material.SetVector(TINT_VARIABLE, NO_ALPHA);

		bool userWon = false;
		bool buddyWon = false;

		userWon = result == USER_WON;
		buddyWon = result == BUDDY_WON;

		prizeArea.SetActive(creditsAwarded > 0);
		collectButton.text = Localize.toUpper("ok");

		if (userWon)
		{
			whoCompletedStateKey = "buddy_incomplete";
			collectOrNotStatString = "collect";
			userRegularHammer.material.SetVector(TINT_VARIABLE, NORMAL_ALPHA);
			buddyFrozenHammer.material.SetVector(TINT_VARIABLE, NORMAL_ALPHA);
			collectButton.text = Localize.toUpper("collect");
			//show user won but not buddy
		}
		else if (buddyWon)
		{
			whoCompletedStateKey = "incomplete";
			userFrozenHammer.material.SetVector(TINT_VARIABLE, NORMAL_ALPHA);
			buddyRegularHammer.material.SetVector(TINT_VARIABLE, NORMAL_ALPHA);
		}
		else
		{
			whoCompletedStateKey = "both_incomplete";
			userFrozenHammer.material.SetVector(TINT_VARIABLE, NORMAL_ALPHA);
			buddyFrozenHammer.material.SetVector(TINT_VARIABLE, NORMAL_ALPHA);
		}

		StatsManager.Instance.LogCount(counterName: "dialog", kingdom:"co_op_challenge_over", phylum:whoCompletedStateKey, genus:"view");
		rewardAmount.text = CommonText.formatNumber(creditsAwarded);

		exitButton.registerEventDelegate(onClickClose);
		collectButton.registerEventDelegate(onClickClose);
	}

#if ZYNGA_TRAMP || UNITY_EDITOR
	public override IEnumerator automate()
	{
		Dialog.close();
		yield return null;
	}
#endif

	protected override void onFadeInComplete()
	{
		base.onFadeInComplete();
		Audio.play("ConsolationWinFanfarePP");
	}

	public void onClickClose(Dict args = null)
	{
		StatsManager.Instance.LogCount(counterName: "dialog", kingdom:"co_op_challenge_over", phylum:whoCompletedStateKey, family:collectOrNotStatString, genus:"click");
		Dialog.close();
	}

	public override void close()
	{
		Audio.play("CloseDialogPP");

		PartnerPowerupAction.completeCoOp(eventID);
	}

	// Might technically be motd
	public static void showDialog(Dict args = null)
	{
		Scheduler.addDialog("partner_power_incomplete", args);
	}
}
