using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Handles stuff for the "coin sweepstakes" feature.
*/

public class CreditSweepstakes : IResetGame
{
	public const string ACTION = "motd:coin_sweepstakes_motd";
	
	public static GameTimerRange timeRange = null;	// Stores the start & end time range.
	public static int winnerCount = 0;			// The number of players that will win the sweepstakes.
	public static long payout = 0L;				// The prize amount for winning.
	public static long wonCredits = 0L;			// Actual credits amount won if the player won.
	public static int wonVersion = 0;			// The version of the sweepstakes, to differentiate different sweepstakes events.

	private static bool lastIsActive = false;
	private static string legalText = "";

	private const string LINK_TAG = "<link=\"credits_sweepstakes\">";
	private const string LINK_END_TAG = "</link>";

	// Initialize the sweepstakes data.
	public static void init()
	{
		// Need to define this even if the sweepstakes isn't enabled,
		// since the lose event relies on it. It's possible to get a lose event
		// after the sweepstakes has ended and has been disabled.
		payout = Data.liveData.getLong("COIN_SWEEPSTAKES_PAYOUT_AMOUNT", 0L) * 1000000L;
		winnerCount = Data.liveData.getInt ("COIN_SWEEPSTAKES_NUMBER_WINNERS", 0);

		// Always create the timeRange object, even if the time frame is inactive.
		timeRange = new GameTimerRange(
			Data.liveData.getInt("COIN_SWEEPSTAKES_START_TIMESTAMP", 0),
			Data.liveData.getInt("COIN_SWEEPSTAKES_END_TIMESTAMP", 0),
			Data.liveData.getBool("COIN_SWEEPSTAKES_ENABLED", false)
		);
		
		// Register for server events, even if the event is currently disabled.
		Server.registerEventDelegate("coin_sweepstakes_win", winEvent, true);
		Server.registerEventDelegate("coin_sweepstakes_lose", loseEvent, true);
	}
		
	private static void formatLink()
	{
		legalText = Localize.text("coinsw_legal_{0}_{1}",
			timeRange.startDateFormattedLocalTime,
			timeRange.endDateFormattedLocalTime
		);
	}

	public static string getLegalText()
	{
		if (legalText == "")
		{
			formatLink();
		}

		return legalText;
	}
	
	public static void winEvent(JSON data)
	{
		CreditSweepstakesWinner.showDialog(
			data.getLong("coin_sweepstakes_winnings", 0L),
			data.getInt("coin_sweepstakes_version", 0)
		);
	}

	public static void loseEvent(JSON data)
	{
		List<string> winners = new List<string>();
		if (data.hasKey("coin_sweepstakes_winners"))
		{
			winners.AddRange(data.getStringStringDict("coin_sweepstakes_winners").Values);
		}
		CreditSweepstakesLoser.showDialog(winners);
	}
	
	public static bool isActive
	{
		get
		{
			return timeRange != null && timeRange.isActive;
		}
	}
	
	// We need to continually check for an activating sweepstakes if the start date is in the future.
	// Called by Overlay.cs in every frame.
	public static void update()
	{
		if (timeRange == null || !timeRange.isActive)
		{
			return;
		}
		
		// Deactivation of expired carousel slides is automatically handled by the carousel system.
		// However, we must still check for the feature activating in the middle of the session.
		if (lastIsActive && !isActive)
		{
			// The sweepstakes has started in the middle of the session!
			// Activate the carousel slide.
			CarouselData slide = CarouselData.findInactiveByAction(ACTION);
			if (slide != null)
			{
				slide.activate();
			}
		}
		
		lastIsActive = isActive;
	}
	
	public static void resetStaticClassData()
	{
		timeRange = null;
		payout = 0L;
		lastIsActive = false;
		wonCredits = 0L;
		wonVersion = 0;
	}
}
