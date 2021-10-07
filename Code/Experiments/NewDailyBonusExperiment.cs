using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewDailyBonusExperiment : EosExperiment
{
	public string bonusKeyName { get; private set; }
	public int dailyStreakEndingReminder { get; private set; }
	public string notifLocKeyDay1To6 { get; private set; }
	public string notifLocKeyDay7{ get; private set; }

	//How many seconds before the streak reset timer should we show the local notification 
	public const int DAILY_STREAK_REMINDER = 3600; //Using -1 will set the daily streak notif to OFF

	public NewDailyBonusExperiment(string name) : base(name)
	{

	}
	protected override void init(JSON data)
	{
		bonusKeyName = getEosVarWithDefault(data, "bonus_key_name", "none");
		dailyStreakEndingReminder = getEosVarWithDefault(data, "daily_streak_ending", DAILY_STREAK_REMINDER);
		notifLocKeyDay1To6 = getEosVarWithDefault(data, "notif_mobile_d1_6", "");
		notifLocKeyDay7 = getEosVarWithDefault(data, "notif_mobile_d7", "");
	}

	public override void reset()
	{
		base.reset();
		bonusKeyName = "none";
	}
}
