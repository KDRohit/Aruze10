using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.HitItRich.EUE;
using Com.Scheduler;
using TMPro;

public class WeeklyRaceMOTD : DialogBase
{
	[SerializeField] private UISprite divisionBadge;
	[SerializeField] private UISprite divisionLabel;
	[SerializeField] private TextMeshPro divisionText;
	[SerializeField] private TextMeshPro dailyBonusText;
	[SerializeField] private TextMeshPro playersRankText;
	[SerializeField] private ButtonHandler confirmButton;
	[SerializeField] private ButtonHandler closeButton;

	// label materials
	[SerializeField] private Material beginnerMaterial;
	[SerializeField] private Material rookieMaterial;
	[SerializeField] private Material professionalMaterial;
	[SerializeField] private Material masterMaterial;
	[SerializeField] private Material grandMasterMaterial;
	[SerializeField] private Material championMaterial;
	[SerializeField] private Material grandChampionMaterial;
	
	public override void init()
	{
		confirmButton.registerEventDelegate(closeClicked);
		closeButton.registerEventDelegate(closeClicked);
		
		WeeklyRace race = WeeklyRaceDirector.currentRace;
		if (race == null)
		{
			// !?? ? !!?!
			Dialog.close();
		}

		divisionBadge.spriteName = WeeklyRace.getBadgeSprite(race.division);
		divisionLabel.spriteName = WeeklyRace.getDivisionTierSprite(race.division);

		Material labelMaterial = getMaterialForDivision(race.division);
		divisionText.fontMaterial = labelMaterial;
		divisionText.text = WeeklyRace.getFullDivisionName(race.division);

		playersRankText.text =  CommonText.formatContestPlacement(race.competitionRank+1, true) + " <sup>place</sup>";

		dailyBonusText.text = race.getDailyBonusForDivision(WeeklyRace.NUM_DIVISIONS - 1).ToString() + "%";

		StatsWeeklyRace.logMotd(race.division, "view");
	}

	public void Update()
	{
		AndroidUtil.checkBackButton(closeClicked);
	}

	// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		if (!EUEManager.showWeeklyRaceActiveDiscovery(SlotsPlayer.instance.socialMember.experienceLevel) &&
		    WeeklyRaceDirector.currentRace != null && !Scheduler.hasTaskWith("weekly_race_leaderboard"))
		{
			WeeklyRaceLeaderboard.showDialog(Dict.create(D.OBJECT, WeeklyRaceDirector.currentRace));
		}
	}
	
	private void closeClicked(Dict args = null)
	{
		Dialog.close();
		WeeklyRace race = WeeklyRaceDirector.currentRace;
		StatsWeeklyRace.logMotd(race.division, "close");
		Audio.play("minimenuclose0");
	}
	
	public static void showDialog(string motdKey = "", SchedulerPriority.PriorityType priority = SchedulerPriority.PriorityType.LOW)
	{
		Scheduler.addDialog("weekly_race_motd", null, priority);
	}

	/*=========================================================================================
	ANCILLARY
	=========================================================================================*/
	private Material getMaterialForDivision(int division)
	{
		int divisionGroup = WeeklyRace.getDivisionGroup(division);
		
		switch(divisionGroup)
		{
			case 1:
				return rookieMaterial;

			case 2:
				return professionalMaterial;

			case 3:
				return masterMaterial;

			case 4:
				return grandMasterMaterial;

			case 5:
				return championMaterial;

			case 6:
				return grandChampionMaterial;

			default:
				return beginnerMaterial;
		}
	}
}
