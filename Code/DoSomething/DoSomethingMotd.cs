using UnityEngine;
using System.Collections;

public class DoSomethingMotd : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		string motdKey = parameter;
		if (!string.IsNullOrEmpty(motdKey))
		{
			MOTDFramework.showMOTD(motdKey);
		}
		else
		{
			Debug.LogError("DoSomething.gameDelegate: motd key is empty.");
		}
	}
	
	public override GameTimer getTimer(string parameter)
	{
		GameTimer timer = null;
		
		switch (parameter)
		{
			case "increase_mystery_gift_chance":
				if (MysteryGift.isIncreasedMysteryGiftChance)
				{
					timer = MysteryGift.increasedMysteryGiftChanceRange.endTimer;
				}
				break;

			case "increase_big_slice_chance":
				if (MysteryGift.isIncreasedBigSliceChance)
				{
					timer = MysteryGift.increasedBigSliceChanceRange.endTimer;
				}
				break;


			case "level_up_bonus_coins":
				if (LevelUpBonus.isBonusActive)
				{
					timer = LevelUpBonus.timeRange.endTimer;
				}
				break;

			case "reduced_daily_bonus_time":
				if (DailyBonusReducedTimeEvent.isActive)
				{
					timer = DailyBonusReducedTimeEvent.timerRange.endTimer;
				}
				break;

			case "starter_dialog":
				if (StarterDialog.saleTimer != null)
				{
					timer = StarterDialog.saleTimer.endTimer;
				}
				else
				{
					return null;
				}
				break;
			case "coin_sweepstakes_motd":
				timer = CreditSweepstakes.timeRange.endTimer;
				break;

			case "buy_page_perk":
				if (BuyPagePerk.isActiveAndBest || BuyPagePerk.isInUse)
				{
					timer = BuyPagePerk.timerRange.endTimer;
				}
				break;
		}
		
		return timer;
	}
	
	public override bool getIsValidToSurface(string parameter)
	{	
		bool isValid = false;
		switch (parameter)
		{
			case "increase_mystery_gift_chance":
				isValid = MysteryGift.isIncreasedMysteryGiftChance;
				break;
			case "level_up_bonus_coins":
				isValid = LevelUpBonus.isBonusActive && !LevelUpBonus.isBonusActiveFromPowerup;
				break;
			case "starter_dialog":
				isValid = StarterDialog.isActive;
				break;
			case "coin_sweepstakes_motd":
				isValid = CreditSweepstakes.isActive;
				break;
			case "reduced_daily_bonus_time":
				isValid = DailyBonusReducedTimeEvent.isActive && !DailyBonusReducedTimeEvent.isActiveFromPowerup;
				break;
			case "deluxe_games":
				isValid = ExperimentWrapper.DeluxeGames.isInExperiment;
				break;
			case "buy_page_perk":
				isValid = BuyPagePerk.isActiveAndBest || BuyPagePerk.isInUse;
				break;				
		default:
			MOTDDialogData data = MOTDDialogData.find(parameter);
			if (data == null)
			{
				// If that MOTD doesn't exists, then we obviously cannot show the MOTD.
				isValid = false;
			}
			else
			{
				// If we dont have a specific override, then just return whether that MOTD should show from its data.
				isValid = data.shouldShow;
			}
			break;
		}
		return isValid;
	}
	
	public override bool getIsValidParameter(string parameter)
	{
		return (MOTDDialogData.find(parameter) != null);
	}
}
