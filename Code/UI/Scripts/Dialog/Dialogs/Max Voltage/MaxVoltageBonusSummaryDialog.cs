using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using UnityEngine;

public class MaxVoltageBonusSummaryDialog : BonusSummary
{

	protected override IEnumerator showResults()
	{
		StatsManager.Instance.LogCount(
			"dialog",
			"max_voltage",
			"summary",
			"",
			"",
			"view");
		Audio.play("MVBonusSummary");
		long payoutValue = BonusGameManager.instance.currentGameFinalPayout;
		MaxVoltageTokenCollectionModule.minigameWinnings = payoutValue;
		finalWinAmount.text = CreditsEconomy.convertCredits(payoutValue);
		yield break;
	}

	/// NGUI button callback.
	private void closeClicked()
	{
		if (Dialog.instance.isClosing)
		{
			return;
		}

		StatsManager.Instance.LogCount(
			"dialog",
			"max_voltage",
			"summary",
			"",
			"close",
			"click");

		cancelAutoClose();
		Dialog.close();
	}

	/// NGUI button callback.
	private void collectClicked(Dict args = null)
	{
		if (Dialog.instance.isClosing)
		{
			return;
		}
		StatsManager.Instance.LogCount(
			"dialog",
			"max_voltage",
			"summary",
			"",
			"collect",
			"click");

		cancelAutoClose();
		Dialog.close();
	}

	public static void showCustomDialog(Dict args)
	{
		long payoutValue = BonusGameManager.instance.currentGameFinalPayout;
		if (payoutValue > 0)
		{
			Scheduler.addDialog(
				"max_voltage_bonus_summary",
				args,
				SchedulerPriority.PriorityType.IMMEDIATE);
		}
		else
		{
			processBonusSummary();
			if (args != null)
			{
				AnswerDelegate callback = args.getWithDefault(D.CALLBACK, null) as AnswerDelegate;
				if (callback != null)
				{
					callback.Invoke(null);
				}
			}
		}
		
	}

	public override void init ()
	{
		StartCoroutine(showResults());
		collectButtonHandler.registerEventDelegate(collectClicked);
	}
}
