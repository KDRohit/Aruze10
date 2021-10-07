using UnityEngine;

public class ModifyLevelupBonusMultiplierBuff : Buff
{
	// min value for this to be activated
	public static void init()
	{
		GameTimerRange timerRange = new GameTimerRange(
				Data.liveData.getInt("LEVEL_UP_BONUS_COINS_START_DATE", 0),
				Data.liveData.getInt("LEVEL_UP_BONUS_COINS_END_DATE", 0),
				Data.liveData.getBool("LEVEL_UP_BONUS_COINS_EVENT", false)
			);
		int value = Data.liveData.getInt("LEVEL_UP_BONUS_COINS_MULTIPLIER", 1);
		string appliesTo = Data.liveData.getString("LEVEL_UP_BONUS_COINS_LEVEL_REQUIRED", "");
		bool buffActivated = false;
		if (timerRange.isActive)
		{
			int duration = (timerRange.endTimestamp - timerRange.startTimestamp);
			BuffType buffType = BuffType.find("levelup_bonus_multiplier");
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
			LevelUpBonus.init();
		}
	}

	// Add a game timer to the game timers list and activate the next buff in the list
	public override bool addGameTimerAndActivateNext(BuffDef buffDef, int newStartTimestamp, int newEndTimestamp)
	{
		Buff.log("ModifyLevelupBonusMultiplierBuff.addGameTimerAndActivateNext buffDef:{0}, startTs:{1}, endTs:{2}",buffDef.keyName, newStartTimestamp, newEndTimestamp);
		if (base.addGameTimerAndActivateNext(buffDef, newStartTimestamp, newEndTimestamp))
		{
			startLevelUpBonusEvent(buffDef, newStartTimestamp, newEndTimestamp);
			bool shouldShowDialogAndCarousel = LevelUpBonus.isBonusActive;
			if (shouldShowDialogAndCarousel)
			{
				LevelUpBonusMotd.showDialog();
				showCarousel();
				return true;
			}
		}
		return false;
	}

	// Deactivates the Level Up Bonus multiplier Buff
	public override bool deactivate(GameTimerRange gameTimer)
	{
		if (base.deactivate(gameTimer))
		{
			Buff.log("ModifyLevelupBonusMultiplierBuff.deactivate deactivating level up bonus");
			LevelUpBonus.pattern = LevelUpBonus.LevelPattern.NONE;
			LevelUpBonus.multiplier = 1;
			LevelUpBonus.timeRange.startTimers(0, 0);
			if (Overlay.instance != null)
			{
				Overlay.instance.top.xpUI.checkEventStates();
			}			
			return true;
		}
		return false;
	}

	private void startLevelUpBonusEvent(BuffDef buffDef, int startTimestamp, int endTimestamp)
	{
		Buff.log("ModifyLevelupBonusMultiplierBuff.startLevelUpBonusEvent buffDef:{0}, startTs:{1}, endTs:{2}", buffDef.keyName, startTimestamp, endTimestamp);
		LevelUpBonus.init();
		LevelUpBonus.setLevelPattern(buffDef.appliesTo.ToUpper());
		LevelUpBonus.multiplier = buffDef.value;
		// stored in live data key
		LevelUpBonus.timeRange.startTimers(startTimestamp, endTimestamp);
		if (Overlay.instance != null)
		{
			Overlay.instance.top.xpUI.checkEventStates();
		}		
	}

	private void showCarousel()
	{
		CarouselData carouselData = CarouselData.findInactiveByAction("motd:level_up_bonus_coins");
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
