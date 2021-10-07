using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
HIR subclass for the daily bonus button.
*/

public class DailyBonusButtonHIR : DailyBonusButton
{
	public GameObject hyperCollectNowParent;
	public GameObject hyperReadyInParent;
	public TextMeshPro buttonLabel;

	/// Resets the button to the non-ready state.
	public override void setActive(bool isActive)
	{
		base.setActive(isActive);
		hyperCollectNowParent.SetActive(false);
		hyperReadyInParent.SetActive(false);
		buttonLabel.text = Localize.text("daily_bonus_with_return");
	}

	protected override void Update ()
	{
		base.Update();
		// CRC - possible fix for exception, we should null-check the timer before using anywhere in this function.
		// https://app.crittercism.com/developers/exception-details/68dd1066abe020f69abb62d1df98180755c10d027a9a215112ccac6f
		if (SlotsPlayer.instance.dailyBonusTimer == null)
		{
			return;
		}

		if (SlotsPlayer.instance.dailyBonusTimer.isExpired) // Expired timer means daily bonus can be collected now.
		{
			if (DailyBonusReducedTimeEvent.isActive)
			{
				if (!hyperCollectNowParent.activeSelf)
				{
					readyInParent.SetActive(false);
					collectNowParent.SetActive(false);
					hyperReadyInParent.SetActive(false);
					hyperCollectNowParent.SetActive(true);
					timerLabel.text = Localize.textUpper("collect_now");
					buttonLabel.text = Localize.textUpper("hyperspeed_bonus");
				}
			}
			else
			{
				if (!collectNowParent.activeSelf)
				{
					readyInParent.SetActive(false);
					collectNowParent.SetActive(true);
					hyperReadyInParent.SetActive(false);
					hyperCollectNowParent.SetActive(false);
					timerLabel.text = Localize.textUpper("collect_now");
					buttonLabel.text = Localize.text("daily_bonus_with_return"); // daily_bonus_re contains "return \n", so don't set it to upper case.
				}
			}
		}
		else // Daily bonus in cool down.
		{
			if (DailyBonusReducedTimeEvent.isActive)
			{
				if (!hyperReadyInParent.activeSelf)
				{
					readyInParent.SetActive(false);
					collectNowParent.SetActive(false);
					hyperReadyInParent.SetActive(true);
					hyperCollectNowParent.SetActive(false);
					buttonLabel.text = Localize.textUpper("hyperspeed_bonus");
				}
			}
			else
			{
				if (!readyInParent.activeSelf)
				{
					readyInParent.SetActive(true);
					collectNowParent.SetActive(false);
					hyperReadyInParent.SetActive(false);
					hyperCollectNowParent.SetActive(false);
					buttonLabel.text = Localize.text("daily_bonus_with_return");
				}
			}
			timerLabel.text = SlotsPlayer.instance.dailyBonusTimer.timeRemainingFormatted;
		}
	}
}
