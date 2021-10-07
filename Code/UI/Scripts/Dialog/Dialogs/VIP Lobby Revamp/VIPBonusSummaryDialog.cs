using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using UnityEngine;

public class VIPBonusSummaryDialog : BonusSummary 
{
	[SerializeField] private LabelWrapperComponent collectButtonLabel;
	[SerializeField] private Animator buttonAnimator;

	protected override IEnumerator showResults()
	{
		if (SlotsPlayer.isAnonymous)
		{
			collectButtonLabel.text = "COLLECT";
		}
		long payoutValue = VIPTokenCollectionModule.miniJpValue + VIPTokenCollectionModule.majorJpValue + VIPTokenCollectionModule.grandJpValue + VIPTokenCollectionModule.scatterWinnings;
		finalWinAmount.text = CreditsEconomy.convertCredits(payoutValue);
		string statPhylum = "";
		bool multipleJackpots = false; //Keep track if we have multiple jackpots won to add _ into the phylum for stat tracking
		if (VIPTokenCollectionModule.grandJpValue > 0)
		{
			statPhylum += "grand";
			multipleJackpots = true;
		}
		if (VIPTokenCollectionModule.majorJpValue > 0)
		{
			if (multipleJackpots)
			{
				statPhylum += "_";
			}
			statPhylum += "major";
			multipleJackpots = true;
		}
		if (VIPTokenCollectionModule.miniJpValue > 0)
		{
			if (multipleJackpots)
			{
				statPhylum += "_";
			}
			statPhylum += "mini";
		}
		StatsManager.Instance.LogCount("dialog", "vip_room_jackpot_winner", statPhylum, "", "", "", payoutValue);
		Audio.play("VIPMinigameSummary");
		yield break;
	}

	private void collectPressed()
	{
		StatsManager.Instance.LogCount("dialog", "vip_room_jackpot_winner", "", "", "collect_share", "click");
		buttonAnimator.Play("buttonPressed");
	}

	public static void showCustomDialog(Dict args)
	{
		long totalPayout = VIPTokenCollectionModule.miniJpValue + VIPTokenCollectionModule.majorJpValue + VIPTokenCollectionModule.grandJpValue + VIPTokenCollectionModule.scatterWinnings;
		if (totalPayout > 0)
		{
			Scheduler.addDialog(
				"vip_bonus_summary_dialog", 
				args,
				SchedulerPriority.PriorityType.IMMEDIATE
			);
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
