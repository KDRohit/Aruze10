using UnityEngine;

public static class GameEvents
{
	public delegate void creditsWonDelegate(long amount, string gameKey, string gameGroup);
	public static event creditsWonDelegate onCreditsWon;

	public delegate void spinDelegate(string gameKey, string gameGroup);
	public static event spinDelegate onSpin;

	public delegate void levelUpDelegate(int newLevel);
	public static event levelUpDelegate onLevelUp;

	public delegate void vipLevelUpDelegate(int newLevel);
	public static event vipLevelUpDelegate onVipLevelUp;
	
	public static void trackSpin(LobbyGame game)
	{
		if (game == null)
		{
			Debug.LogErrorFormat("GameEvents.cs -- trackSpin -- somehow got here with a null game, aborting.");
			return;
		}
		string key = game.keyName;
		string group = game.groupInfo != null ?  game.groupInfo.keyName : "none";
		if (onSpin != null)
		{
			onSpin(key, group);
		}
	}

	public static void trackCreditsWonDuringSpin(long amount, LobbyGame game)
	{
		if (game == null)
		{
			Debug.LogErrorFormat("GameEvents.cs -- trackCreditsWOn -- somehow got here with a null game, aborting.");
			return;
		}
		string key = game.keyName;
		string group = game.groupInfo != null ?  game.groupInfo.keyName : "none";
		if (onCreditsWon != null)
		{
			onCreditsWon(amount, key, group);
		}
	}

	public static void trackLevelUp(int newLevel)
	{
		if (onLevelUp != null)
		{
			onLevelUp(newLevel);
		}
		PlayerPrefsCache.SetInt(Prefs.PLAYER_LEVEL, newLevel);
	}

	public static void trackVipLevelUp(int newLevel)
	{
		if (onVipLevelUp != null)
		{
			onVipLevelUp(newLevel);
		}
	}	
}