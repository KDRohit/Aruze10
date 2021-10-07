using UnityEngine;

public class ModifyDailyBonusReducedTimerBuff : Buff
{
	public static void init()
	{
		bool buffActivated = false;
		GameTimerRange timerRange = new GameTimerRange(
			Data.liveData.getInt("REDUCED_DAILY_BONUS_TIME_START_DATE", 0),
			Data.liveData.getInt("REDUCED_DAILY_BONUS_TIME_END_DATE", 0),
			Data.liveData.getBool("REDUCED_DAILY_BONUS_TIME_ENABLED", false)
		);
		int value = Data.liveData.getInt("REDUCED_DAILY_BONUS_TIME_LENGTH", 120);
		if (timerRange.isActive)
		{
			int duration = (timerRange.endTimestamp - timerRange.startTimestamp);
			BuffType buffType = BuffType.find("daily_bonus_reduced_timer");
			string appliesTo = BuffDef.APPLIES_TO_GLOBAL;
			string keyName = BuffDef.generateGlobalKeyName(buffType, value, duration, appliesTo);
			BuffDef buffDef = new BuffDef(
				keyName,
				buffType,
				value,
				duration,
				appliesTo);
			buffActivated = buffDef.apply(timerRange.startTimestamp, timerRange.endTimestamp);
		}
		if (!buffActivated)
		{
			DailyBonusReducedTimeEvent.init();
		}
	}

	protected override bool isLowerValueBetter
	{
		get {
			return true;
		}
	}

	// Add a game timer to the game timers list and activate the next buff in the list
	public override bool addGameTimerAndActivateNext(BuffDef buffDef, int newStartTimestamp, int newEndTimestamp)
	{
		Buff.log("ModifyDailyBonusReducedTimerBuff.addGameTimerAndActivateNext buffDef:{0}, startTs:{1}, endTs:{2}",buffDef.keyName, newStartTimestamp, newEndTimestamp);
		if (base.addGameTimerAndActivateNext(buffDef, newStartTimestamp, newEndTimestamp))
		{
			int nowInSecs = GameTimer.currentTime;
			if (nowInSecs < newEndTimestamp)
			{
				// value = reduced timer
				if (SlotsPlayer.instance.dailyBonusTimer.timeRemaining > value)
				{
					// don't actually set the timer because the server hasn't either, it will be correct after the first collect
					//SlotsPlayer.instance.dailyBonusTimer.startTimer(value);
				}
				GameTimerRange timerRange = new GameTimerRange(
					nowInSecs,
					endTimestamp,
					true
				);
				DailyBonusReducedTimeEvent.init(timerRange);
				bool shouldShowDialogAndCarousel = !buffDef.appliesTo.Equals(BuffDef.APPLIES_TO_GLOBAL);
				if (shouldShowDialogAndCarousel)
				{
					Buff.log("ModifyDailyBonusReducedTimerBuff.addGameTimerAndActivateNext shouldShowDialog:{0}", shouldShowDialogAndCarousel);
					DailyBonusReducedTimeMOTD.showDialog();
					showCarousel();
				}
				return true;
			}
		}
		return false;
	}

	// Deactivates the Reduced Bonus Timer Buff
	public override bool deactivate(GameTimerRange gameTimer)
	{
		// Nothing special for deactivating (similar to ModifyClaimFreeCoinsTimerCharm)
		return base.deactivate(gameTimer);
	}

	private void showCarousel()
	{
		CarouselData carouselData = CarouselData.findInactiveByAction("daily_bonus_reduced_time");
		if (carouselData != null)
		{
			carouselData.activate();
		}
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
		
	}
}
