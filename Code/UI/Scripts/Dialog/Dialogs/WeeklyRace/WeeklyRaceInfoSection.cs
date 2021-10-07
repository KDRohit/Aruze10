using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Com.States;
using Com.Scheduler;

public class WeeklyRaceInfoSection : MonoBehaviour
{
	[SerializeField] private UISprite divisionBadge;
	[SerializeField] private UISprite divisionLabel;
	[SerializeField] private TextMeshPro divisionText;
	[SerializeField] private TextMeshPro dailyBonusText;

	// daily rivals stuff
	[SerializeField] private UITexture playerImage;
	[SerializeField] private UITexture rivalImage;
	[SerializeField] private TextMeshPro timerLabel;
	[SerializeField] private ClickHandler playerButton;
	[SerializeField] private ClickHandler rivalButton;

	[SerializeField] private GameObject activeRival;
	[SerializeField] private GameObject inactiveRival;
	[SerializeField] private TextMeshPro statusText;
	[SerializeField] private UISprite rivalBg;

	[SerializeField] private Material playerWinningMaterial;
	[SerializeField] private Material playerLosingMaterial;
	[SerializeField] private Animator rivalAnimator;
	[SerializeField] private GameObject raysObject;

	private WeeklyRace race;
	private WeeklyRaceRacer rival;
	private StateMachine stateMachine;
	private bool hasLoadedImages = false;

	// =============================
	// CONST
	// =============================
	private const string RIVAL_WINNING_BG = "Daily Rivals Panel 01 Stretchy";
	private const string PLAYER_WINNING_BG = "Daily Rivals Panel 00 Stretchy";
	private const string RIVAL_LEADING = "Rival Leading";
	private const string PLAYER_LEADING = "Player Leading";

	public void setup(WeeklyRace race, int division, Material labelMaterial = null)
	{
		this.race = race;
		stateMachine = new StateMachine();
		stateMachine.addState(State.READY);
		stateMachine.addState(State.IN_PROGRESS);
		stateMachine.addState(State.COMPLETE);
		stateMachine.updateState(State.READY);

		divisionBadge.spriteName = WeeklyRace.getBadgeSprite(division);
		divisionLabel.spriteName = WeeklyRace.getDivisionTierSprite(division);

		dailyBonusText.text = string.Format("+{0}%", race.getDailyBonusForDivision(division).ToString());

		if (labelMaterial != null)
		{
			divisionText.fontMaterial = labelMaterial;
		}

		divisionText.text = WeeklyRace.getFullDivisionName(race.division);

		if (playerButton != null)
		{
			playerButton.registerEventDelegate(onPlayerButtonClick);
		}

		if (rivalButton != null)
		{
			rivalButton.registerEventDelegate(onRivalButtonClick);
		}
	}

	void Update()
	{
		if (race != null && race.rivalTimer != null && race.rivalTimer.timeRemaining <= 0 && stateMachine.can(State.IN_PROGRESS))
		{
			race.rivalTimer.removeLabel(timerLabel);
			stateMachine.updateState(State.COMPLETE);
			setupRival();
		}
		else if ((race == null || race.hasIncomingRival) && stateMachine != null && stateMachine.can(State.IN_PROGRESS))
		{
			stateMachine.updateState(State.COMPLETE);
			SafeSet.gameObjectActive(inactiveRival, true);
			SafeSet.gameObjectActive(activeRival, false);

			if (statusText != null)
			{
				statusText.text = "A new Rival contest will begin soon!";
			}
		}
	}

	public void setupRival()
	{
		if (race != null && race.isRivalsActive && race.rivalsRacerInstance != null && stateMachine.can(State.READY))
		{
			if (rival != null)
			{
				// do image loading
				hasLoadedImages = rival.id == race.rivalsRacerInstance.id;
			}

			rival = race.rivalsRacerInstance;

			stateMachine.updateState(State.IN_PROGRESS);

			SafeSet.gameObjectActive(inactiveRival, false);
			SafeSet.gameObjectActive(activeRival, true);

			if (!hasLoadedImages)
			{
				DisplayAsset.loadTextureToUITexture(playerImage, race.playersRacerInstance.member.getImageURL, "", true);
				DisplayAsset.loadTextureToUITexture(rivalImage, race.rivalsRacerInstance.member.getImageURL, "", true);
				hasLoadedImages = true;
			}

			if (race.rivalTimer != null && !race.rivalTimer.isExpired)
			{
				timerLabel.text = "Prize In: ";
				race.rivalTimer.registerLabel(timerLabel, GameTimerRange.TimeFormat.REMAINING, true);
			}

			if (race.rivalsRacerInstance.competitionRank < race.competitionRank)
			{
				timerLabel.fontMaterial = playerLosingMaterial;
				rivalBg.spriteName = RIVAL_WINNING_BG;
				rivalAnimator.Play(RIVAL_LEADING);
				raysObject.SetActive(false);
			}
			else
			{
				timerLabel.fontMaterial = playerWinningMaterial;
				rivalBg.spriteName = PLAYER_WINNING_BG;
				rivalAnimator.Play(PLAYER_LEADING);
				raysObject.SetActive(true);
			}
		}

		if (race != null && race.rivalTimer != null && race.rivalTimer.isExpired)
		{
			SafeSet.gameObjectActive(inactiveRival, true);
			SafeSet.gameObjectActive(activeRival, false);

			if (race.hasRival)
			{
				statusText.text = "Stay Tuned! We're gathering rewards";
			}
			else if (race.hasIncomingRival)
			{
				statusText.text = "A new Rival contest will begin soon!";
			}
		}
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
}