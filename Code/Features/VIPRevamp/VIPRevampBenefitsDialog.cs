using System;
using UnityEngine;
using System.Collections;
using Com.Scheduler;
using TMPro;

public class VIPRevampBenefitsDialog : DialogBase
{
	[SerializeField] private VIPRevampBenefits benefits;
	[SerializeField] private TextMeshPro vipPoints;
	[SerializeField] private TextMeshPro vipStatus;
	[SerializeField] private TextMeshPro giftsResetText;

	private GameTimerRange timer;

	public override void init()
	{
		StatsInbox.logDailyLimits("view",InboxDialog.currentTabName);
		benefits.onStatus();

		VIPLevel modifiedLevel = VIPLevel.find(SlotsPlayer.instance.adjustedVipLevel);
		vipStatus.text = Localize.text("vip_revamp_member_{0}", modifiedLevel.name);
		vipPoints.text = Localize.text("vip_revamp_points_{0}", CommonText.formatNumber(SlotsPlayer.instance.vipPoints));

		TimeSpan timeSpan = DateTime.Today.AddDays(1).ToUniversalTime() - DateTime.Now.ToUniversalTime();
		timer = GameTimerRange.createWithTimeRemaining((int)timeSpan.TotalSeconds);
		timer.registerLabel(giftsResetText, keepCurrentText:true);
	}

	public override void close()
	{
		StatsInbox.logDailyLimits("close",InboxDialog.currentTabName);
	}
}