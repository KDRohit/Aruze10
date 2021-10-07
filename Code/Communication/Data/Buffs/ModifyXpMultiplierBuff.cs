using UnityEngine;

public class ModifyXpMultiplierBuff : Buff
{
	public static void xpMultiplierOnEvent(JSON xpMultiplierData)
	{
		bool buffActivated = false;
		int nowInSecs = GameTimer.currentTime;
		int endTimestamp = xpMultiplierData.getInt("end_time", 0);
		if (endTimestamp<= 0)
		{
			endTimestamp = nowInSecs + xpMultiplierData.getInt("time_remaining", 0);
		}

		if (endTimestamp <= nowInSecs)
		{
			return;
		}

		int value = xpMultiplierData.getInt("multiplier", 1);
		int duration = (endTimestamp - nowInSecs);
		string appliesTo = BuffDef.APPLIES_TO_GLOBAL;
		BuffType buffType = BuffType.find("xp_multiplier");
		string keyName = BuffDef.generateGlobalKeyName(buffType, value, duration, appliesTo);
		BuffDef buffDef = new BuffDef(
						keyName,
						buffType,
						value,
						duration,
						appliesTo);

		buffActivated = buffDef.apply(endTimestamp);
		if (!buffActivated)
		{
			/* MCC -- Commenting this out since buffs were SIR code and I dont want to convert it unless we plan on using it.
			SlotsPlayer.xpMultiplierOnEvent(xpMultiplierData);
			*/
		}
	}

	// Add a game timer to the game timers list and activate the next buff in the list
	public override bool addGameTimerAndActivateNext(BuffDef buffDef, int newStartTimestamp, int newEndTimestamp)
	{
		Buff.log("ModifyXpMultiplierBuff.addGameTimerAndActivateNext buffDef:{0}, startTs:{1}, endTs:{2}",buffDef.keyName, newStartTimestamp, newEndTimestamp);
		// Global Events are currently activated outside the Buff
		if (base.addGameTimerAndActivateNext(buffDef, newStartTimestamp, newEndTimestamp))
		{
			int nowInSecs = GameTimer.currentTime;
			if (nowInSecs < newEndTimestamp)
			{
				int remainingSecs = (endTimestamp - nowInSecs);
				bool showDialogAndCarousel = !buffDef.appliesTo.Equals(BuffDef.APPLIES_TO_GLOBAL);
				startXpMultiplierEvent(buffDef.value, remainingSecs, showDialogAndCarousel);
				return true;
			}
		}
		return false;
	}

	// Deactivates the xp multiplier buff
	public override bool deactivate(GameTimerRange gameTimer)
	{
		if (base.deactivate(gameTimer))
		{
			Buff.log("ModifyXpMultiplierBuff.deactivate deactivating level up bonus");
			stopXpMultiplierEvent();
			return true;
		}
		return false;
	}

	private void stopXpMultiplierEvent()
	{
		JSON json = new JSON("{}");
		/* MCC -- Commenting this out since buffs were SIR code and I dont want to convert it unless we plan on using it.
		SlotsPlayer.xpMultiplierOffEvent(json);
		*/
	}

	private void startXpMultiplierEvent(int multiplier, int remainingSecs, bool showDialogAndCarousel)
	{
		string jsonString = "{" +
			"\"multiplier\": " + multiplier + "," +
			"\"show_dialog\": " + (showDialogAndCarousel ? "true" : "false") + "," +
			"\"time_remaining\": " + remainingSecs +
		"}";

		Buff.log("ModifyXpMultiplierBuff.startXpMultiplierEvent jsonString:{0}",jsonString);
		// Must set this flag before calling xpMultiplierOnEvent() to make sure it actually does something even if already active.

		
		/* MCC -- Commenting this out since buffs were SIR code and I dont want to convert it unless we plan on using it.
		SlotsPlayer.instance.shouldAwardXPMultiplier = false;

		SlotsPlayer.xpMultiplierOnEvent(new JSON(jsonString));
		*/
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
		
	}
}
