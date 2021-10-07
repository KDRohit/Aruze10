using UnityEngine;
using System.Collections;
using Com.Scheduler;
using TMPro;

public class WeeklyRaceBoost : DialogBase
{
	// =============================
	// PRIVATE
	// =============================
	[SerializeField] private TextMeshPro timerText;
	[SerializeField] private TextMeshPro headerText;
	[SerializeField] private ButtonHandler continueButton;
	[SerializeField] private ButtonHandler standingsButton;
	[SerializeField] private ButtonHandler closeButton;

	private int startTime = 0;
	private int endTime = 0;
	private int frequency = 0;
	private GameTimerRange timer;

	// =============================
	// CONST
	// =============================
	private const string HEADER_FORMAT = "Collect your Free Bonus every {0} minutes!";

	public override void init()
	{
		Audio.play("StartRaceFanfareWeeklyRace");
		continueButton.registerEventDelegate(onContinueClicked);
		closeButton.registerEventDelegate(onContinueClicked);
		standingsButton.registerEventDelegate(onViewStandings);
		startTime = (int)dialogArgs.getWithDefault(D.START_TIME, 0);
		endTime = (int)dialogArgs.getWithDefault(D.END_TIME, 0);
		frequency = (int)dialogArgs.getWithDefault(D.TIME, 0);

		headerText.text = string.Format(HEADER_FORMAT, frequency.ToString());
		timer = new GameTimerRange(startTime, endTime);

		if (WeeklyRaceDirector.currentRace  != null)
		{
			StatsWeeklyRace.logBoostDialog(WeeklyRaceDirector.currentRace.division, "view");
		}

		// Use the lesser of the normal time remaining and the charm's time limit amount.
		// In this case, the modifyBy value is in minutes, so we need to convert it to seconds for the timer.
		int secondsRemaining = Mathf.Min(SlotsPlayer.instance.dailyBonusTimer.timeRemaining, frequency * 60);
		
		SlotsPlayer.instance.dailyBonusTimer.startTimer(secondsRemaining);
	}

	private void onContinueClicked(Dict args = null)
	{
		Dialog.close(this);
		if (WeeklyRaceDirector.currentRace  != null)
		{
			StatsWeeklyRace.logBoostDialog(WeeklyRaceDirector.currentRace.division, "close");
		}
	}

	private void onViewStandings(Dict args = null)
	{
		Dialog.close(this);
		WeeklyRaceLeaderboard.showDialog(Dict.create(D.OBJECT, WeeklyRaceDirector.currentRace));
	}

	void Update()
	{
		timerText.text = timer.timeRemainingFormatted;
	}

	// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup. Downloaded textures are automatically destroyed by Dialog class.
	}

	// This shouldn't be called directly - only by the Scheduler as a replacement dialog.
	public static void showDialog(Dict args = null, SchedulerPriority.PriorityType priority = SchedulerPriority.PriorityType.LOW)
	{
		Scheduler.addDialog
		(
			"weekly_race_boost",
			args,
			SchedulerPriority.PriorityType.LOW,
			WeeklyRaceResults.package
		);
	}
}
