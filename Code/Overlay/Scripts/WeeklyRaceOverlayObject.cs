using UnityEngine;
using System.Collections;
using Com.HitItRich.EUE;
using Com.Scheduler;
using TMPro;

public class WeeklyRaceOverlayObject : OverlayObject
{
	[HideInInspector] public UISprite divisionBadge;
	[HideInInspector] public UISprite divisionLabel;
	[HideInInspector] public TextMeshPro playerRank;
	[HideInInspector] public UISprite rankArrow;

	public GameObject divisionBadgeParent;
	public GameObject rankParent;
	public GameObject notifBubbleParent;
	public bool isSetup { get; private set; }

	//Used to check if the new badge should be shown followed by the intro dialog when clicked on the icon
	private bool showEueActiveDiscovery = false;
	private int badgeMaxLevel = 0;

	void Awake()
	{
		init();
	}

	protected void init()
	{
		refresh();
		setEueActiveDiscoveryBadge();
		Server.registerEventDelegate("leveled_up", levelUpEvent, true);
		badgeMaxLevel = Data.liveData.getInt(EUEManager.ACTIVE_DISCOVERY_MAX_LEVEL_LIVEDATA, EUEManager.ACTIVE_DISCOVERY_DEFAULT_MAX_LEVEL);
	}

	public void refresh(Dict args = null)
	{
		Scheduler.removeFunction(refresh);
		WeeklyRace race = WeeklyRaceDirector.currentRace;
		
		if (race != null && divisionBadge != null && divisionLabel != null && rankArrow != null && playerRank != null)
		{
			updateRank(race.division, race.competitionRank);
		}
		else
		{
			updateRank(-1, -1);
		}
	}

	private void updateRank(int division, int playerRank)
	{
		//default to first tier
		if (division < 0)
		{
			division = 0;
		}
		
		if (divisionBadge != null)
		{
			divisionBadge.spriteName = WeeklyRace.getBadgeSprite(division);	
		}

		if (divisionLabel != null)
		{
			divisionLabel.spriteName = WeeklyRace.getDivisionTierSprite(division);
			if (divisionLabel.gameObject != null)
			{
				divisionLabel.gameObject.SetActive(!string.IsNullOrEmpty(WeeklyRace.getTierNumeral(division)));		
			}
		}

		WeeklyRace race = WeeklyRaceDirector.currentRace;
		if (rankArrow != null)
		{
			if (race != null && race.isInPromotion)
			{
				SafeSet.gameObjectActive(rankArrow.gameObject, race.timeRemaining > 0);
				rankArrow.spriteName = "Rank Promotion Zone Indicator";
			}
			else if (race != null && race.isInRelegation)
			{
				SafeSet.gameObjectActive(rankArrow.gameObject, race.timeRemaining > 0);
				rankArrow.spriteName = "Rank Drop Zone Indicator";
			}
			else
			{
				SafeSet.gameObjectActive(rankArrow.gameObject, false);
			}	
		}
		
		if (this.playerRank != null)
		{
			if (playerRank > 0 && race != null)
			{
				SafeSet.gameObjectActive(this.playerRank.gameObject, race.timeRemaining > 0);
				this.playerRank.text = CommonText.formatContestPlacement(playerRank+1, true);
			}
			else
			{
				SafeSet.gameObjectActive(this.playerRank.gameObject, false);
			}	
		}

		isSetup = true;
	}

	public void onClick()
	{
		if (WeeklyRaceDirector.currentRace == null)
		{
			return;
		}

		if (showEueActiveDiscovery)
		{
			WeeklyRaceMOTD.showDialog("", SchedulerPriority.PriorityType.HIGH);
			notifBubbleParent.SetActive(false);
			showEueActiveDiscovery = false;
			//Set the actively discovered custom player data here
			CustomPlayerData.setValue(CustomPlayerData.EUE_ACTIVE_DISCOVERY_WEEKLY_RACE, true);
			EUEManager.logActiveDiscovery("weekly_race");
		}
		else if (WeeklyRaceDirector.currentRace != null && !Scheduler.hasTaskWith("weekly_race_leaderboard"))
		{
			showEueActiveDiscovery = false;
			WeeklyRaceLeaderboard.showDialog(Dict.create(D.OBJECT, WeeklyRaceDirector.currentRace));
		}
	}

	private void setEueActiveDiscoveryBadge()
	{
		showEueActiveDiscovery = EUEManager.showWeeklyRaceActiveDiscovery(SlotsPlayer.instance.socialMember.experienceLevel);
		notifBubbleParent.SetActive(!EUEManager.reachedActiveDiscoveryMaxLevel && showEueActiveDiscovery);
	}

	private void levelUpEvent(JSON data)
	{
		if (data != null)
		{
			int newLevel = data.getInt("level", SlotsPlayer.instance.socialMember.experienceLevel);
			showEueActiveDiscovery = EUEManager.showWeeklyRaceActiveDiscovery(newLevel);
			notifBubbleParent.SetActive(newLevel < badgeMaxLevel && showEueActiveDiscovery);
		}
	}

	private void OnDestroy()
	{
		Server.unregisterEventDelegate("leveled_up", levelUpEvent, true);
	}
}
