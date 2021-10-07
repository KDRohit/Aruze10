using System.Collections;
using Com.Scheduler;
using UnityEngine;
using TMPro;

public class DailyRivalsDialog : DialogBase
{
	[SerializeField] private Animator animator;
	[SerializeField] private UITexture usersImage;
	[SerializeField] private TextMeshPro usersNameLabel;
	[SerializeField] private UITexture rivalsImage;
	[SerializeField] private TextMeshPro rivalsNameLabel;
	[SerializeField] private TextMeshPro timerLabel;
	[SerializeField] private ClickHandler clickHandler;
	[SerializeField] private ClickHandler playerButton;
	[SerializeField] private ClickHandler rivalButton;

	private SmartTimer closeTimer;
	private WeeklyRace race;

	// =============================
	// CONST
	// =============================
	private const string OUTRO = "Outro";
	private const float CLOSE_DELAY = 5f;

	public override void init()
	{
		CustomPlayerData.setValue(CustomPlayerData.HAS_SEEN_RIVAL_PAIRING, true);

		string rivalsPhoto = (string)dialogArgs.getWithDefault(D.DATA, PhotoSource.BACKUP_URL);
		string rivalsName = (string)dialogArgs.getWithDefault(D.VALUE, "Anonymous Racer");

		DisplayAsset.loadTextureToUITexture(usersImage, WeeklyRaceDirector.currentRace.playersRacerInstance.member.getImageURL, "", true);
		DisplayAsset.loadTextureToUITexture(rivalsImage, rivalsPhoto, "", true);
		usersNameLabel.text = "You";
		rivalsNameLabel.text = rivalsName;

		clickHandler.registerEventDelegate(onClick);
		playerButton.registerEventDelegate(onPlayerButtonClick);
		rivalButton.registerEventDelegate(onRivalButtonClick);

		race = WeeklyRaceDirector.currentRace;
		if (race.rivalTimer != null)
		{
			CustomPlayerData.setValue(CustomPlayerData.DAILY_RIVALS_LAST_SEEN, race.rivalTimer.startTimestamp);

			timerLabel.text = "Prize In: ";
			race.rivalTimer.registerLabel(timerLabel, GameTimerRange.TimeFormat.REMAINING, true);
		}

		if (race.rivalsRacerInstance != null)
		{
			StatsWeeklyRace.logDailyRivalsPairing(race.division, race.rivalsRacerInstance.id, "view");
		}

		closeTimer = new SmartTimer(CLOSE_DELAY, false, close, "daily_rivals_dialog_close");
		closeTimer.start();

		Audio.play("FeatureDRDialogPairing");
	}

	private void onClick(Dict args = null)
	{
		if (race.rivalsRacerInstance != null)
		{
			StatsWeeklyRace.logDailyRivalsPairing(race.division, race.rivalsRacerInstance.id, "click");
		}

		Dialog.close(this);
	}

	private void onPlayerButtonClick(Dict args = null)
	{
		NetworkProfileDialog.showDialog(SlotsPlayer.instance.socialMember, SchedulerPriority.PriorityType.IMMEDIATE);
	}
	
	private void onRivalButtonClick(Dict args = null)
	{
		if (race.rivalsRacerInstance != null)
		{
			NetworkProfileDialog.showDialog(race.rivalsRacerInstance.member, SchedulerPriority.PriorityType.IMMEDIATE);
		}
	}

	public override void close()
	{
		if (race.rivalsRacerInstance != null)
		{
			StatsWeeklyRace.logDailyRivalsPairing(race.division, race.rivalsRacerInstance.id, "close");
		}

		closeTimer.stop();
		closeTimer.destroy();
		StartCoroutine(playOutro());
	}

	private IEnumerator playOutro()
	{
		if (animator != null)
		{
			animator.Play(OUTRO);
		}

		yield return new WaitForSeconds(1.5f);

		Dialog.close(this);
	}

	public static void showDialog(WeeklyRace race, bool forceShow = false)
	{
		int dailyRivalSeen = CustomPlayerData.getInt(CustomPlayerData.DAILY_RIVALS_LAST_SEEN, 0);

		if
		(	!Scheduler.hasTaskWith("daily_rivals_dialog") &&
			(forceShow || race.rivalTimer.startTimestamp != dailyRivalSeen || dailyRivalSeen == 0)

		)
		{
			onShow(race);
		}
	}

	private static void onShow(WeeklyRace race)
	{
		if (race == null || race.rivalsRacerInstance == null)
		{
			Debug.LogError("Daily Rivals Dialog: no rival to display");
			return;
		}

		if (race.rivalsRacerInstance != null && race.rivalsRacerInstance.member != null)
		{
			bool addLeaderboard = WeeklyRace.clearLeaderboardFromDialogs();

			Dict args = Dict.create
			(
				D.DATA, race.rivalsRacerInstance.member.getImageURL,
				D.VALUE, race.rivalsRacerInstance.name
			);

			CustomPlayerData.setValue(CustomPlayerData.DAILY_RIVALS_LAST_SEEN, race.rivalTimer.startTimestamp);

			if (Dialog.isSpecifiedDialogShowing("weekly_race_leaderboard"))
			{
				Scheduler.addDialog("daily_rivals_dialog", args, SchedulerPriority.PriorityType.IMMEDIATE);
			}
			else
			{
				Scheduler.addDialog("daily_rivals_dialog", args);
			}


			if (addLeaderboard)
			{
				WeeklyRaceLeaderboard.showDialog(Dict.create(D.OBJECT, WeeklyRaceDirector.currentRace));
			}
		}
	}
}