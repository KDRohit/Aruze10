using UnityEngine;
using System.Collections;
using Com.Scheduler;
using TMPro;
using Com.States;

public class DailyRivalsCompleteDialog : DialogBase
{
	[SerializeField] private GameObject winnerSection;
	[SerializeField] private GameObject loserSection;
	[SerializeField] private Animator animator;
	[SerializeField] private GameObject coinTrail;

	[SerializeField] private UITexture usersImage;
	[SerializeField] private TextMeshPro usersNameLabel;
	[SerializeField] private UITexture rivalsImage;
	[SerializeField] private TextMeshPro rivalsNameLabel;

	[SerializeField] private TextMeshPro usersScoreLabel;
	[SerializeField] private TextMeshPro rivalsScoreLabel;
	[SerializeField] private TextMeshPro rewardLabel;

	[SerializeField] private ButtonHandler okButtonHandler;
	[SerializeField] private TextMeshPro okButtonText;
	[SerializeField] private TextMeshPro winnerText;
	[SerializeField] private TextMeshPro loserText;
	
	[SerializeField] private ClickHandler playerButton;
	[SerializeField] private ClickHandler rivalButton;

	private string eventId = "";
	private long rewardAmount = 0L;
	private StateMachine stateMachine;
	private int division = 0;
	private long playersScore = 0;
	private long rivalsScore = 0;
	private SocialMember rivalMember = null;

	// =============================
	// CONST
	// =============================
	private const string WINNER_INTRO 	= "Winner Combined Intro";
	private const string WINNER_OUTRO 	= "Winner Combined Outro";
	private const string LOSER_INTRO 	= "Loser Intro";
	private const string LOSER_OUTRO 	= "Loser Outro";
	private const string ACTIVE_TEXT 	= "daily_rivals_active_winner";
	private const string INACTIVE_TEXT 	= "daily_rivals_inactive_winner";

	public override void init()
	{
		JSON data = (JSON)dialogArgs.getWithDefault(D.OBJECT, null);
		eventId = data.getString("event", "");

		if (WeeklyRaceDirector.currentRace != null)
		{
			division = WeeklyRaceDirector.currentRace.division;
		}

		setup(data);
	}

	public override void onCloseButtonClicked(Dict args = null)
	{
		Dialog.close(this);
	}

	public override void close()
	{
		if (rewardAmount > 0)
		{
			SlotsPlayer.addFeatureCredits(rewardAmount, "dailyRivals");
			Userflows.logStep("credits_daily_rivals", rewardAmount.ToString());
		}

		sendCompleteEvent();

		if (WeeklyRaceDirector.currentRace != null)
		{
			WeeklyRaceDirector.currentRace.clearExpiredRival();
			WeeklyRaceDirector.getUpdatedRaceData();
		}

		if (rivalsImage != null && rivalsImage.material != null)
		{
			rivalsImage.material.shader = ShaderCache.find("Unlit/GUI Texture");
		}

		StatsWeeklyRace.logDailyRivalsResults(division, playersScore, rivalsScore, rewardAmount, "close");

		rivalsImage.material.shader = ShaderCache.find("Unlit/GUI Texture");
		usersImage.material.shader = ShaderCache.find("Unlit/GUI Texture");
	}

	private void setup(JSON data)
	{
		coinTrail.SetActive(false);
		stateMachine = new StateMachine();
		stateMachine.addState("intro", new StateOptions(null, null, playIntro));
		stateMachine.addState("outro", new StateOptions(null, null, playOutro));
		stateMachine.addState(State.COMPLETE, new StateOptions(null, null, onComplete));

		rewardAmount = data.getLong("reward_amount", 0);
		rivalsScore = data.getLong("rival_score", 0);
		playersScore = data.getLong("score", 0);
		rivalsScoreLabel.text = CreditsEconomy.convertCredits(rivalsScore);
		usersScoreLabel.text = CreditsEconomy.convertCredits(playersScore);

		okButtonHandler.registerEventDelegate(onClick);
		okButtonText.text = Localize.text("ok");

		rivalMember = CommonSocial.findOrCreate("", data.getString("rival_zid", ""));

		if (rivalMember != null)
		{
			rivalsNameLabel.text = data.getString("rival_name", "");
			downloadedTextureToUITexture(rivalsImage, 0);
		}
		else
		{
			rivalsNameLabel.text = "";
		}

		if (WeeklyRaceDirector.currentRace != null)
		{
			TextMeshPro footerLabel = rewardAmount > 0 ? winnerText : loserText;
			if (WeeklyRaceDirector.currentRace.isRivalsActive)
			{
				footerLabel.text = Localize.text(ACTIVE_TEXT);
			}
			else
			{
				footerLabel.text = Localize.text(INACTIVE_TEXT);
			}
		}

		DisplayAsset.loadTextureToUITexture(usersImage, SlotsPlayer.instance.socialMember.getImageURL, "", true);
		usersNameLabel.text = "You";

		StatsWeeklyRace.logDailyRivalsResults(division, playersScore, rivalsScore, rewardAmount, "view");

		updateState("intro");

		if (!hasWon)
		{
			CustomPlayerData.setValue(CustomPlayerData.HAS_SEEN_RIVAL_LOST, true);
		}
		
		playerButton.registerEventDelegate(onPlayerButtonClick);
		rivalButton.registerEventDelegate(onRivalButtonClick);
	}

	// Automatically called by Dialog when a dialog is closed, to clean up downloaded textures.
	public override void destroyDownloadedTextures()
	{
		// do nothing, we don't want to clear weekly race leaderboard, or active toasters of a user's image.
		// that texture is used in multiple places
	}

	private void onComplete()
	{
		Dialog.close(this);
	}

	private void onClick(Dict args = null)
	{
		if (stateMachine.currentState != "outro")
		{
			updateState(nextState);
		}
	}
	
	private void onPlayerButtonClick(Dict args = null)
	{
		NetworkProfileDialog.showDialog(SlotsPlayer.instance.socialMember, SchedulerPriority.PriorityType.IMMEDIATE);
	}
	
	private void onRivalButtonClick(Dict args = null)
	{
		if (rivalMember != null)
		{
			NetworkProfileDialog.showDialog(rivalMember, SchedulerPriority.PriorityType.IMMEDIATE);
		}
	}

	private void playIntro()
	{
		SafeSet.gameObjectActive(winnerSection, hasWon);
		SafeSet.gameObjectActive(loserSection, !hasWon);

		if (hasWon)
		{
			animator.Play(WINNER_INTRO);
			okButtonText.text = "Collect";
			rewardLabel.text = CreditsEconomy.convertCredits(rewardAmount);
			rivalsImage.material.shader = ShaderCache.find("Unlit/GUI Texture Monochrome");
			usersImage.material.shader = ShaderCache.find("Unlit/GUI Texture");
			Audio.play("FeatureDRDialogWinner");
			Audio.play("Dialog4Fanfare4WeeklyRace");
		}
		else
		{
			rivalsImage.material.shader = ShaderCache.find("Unlit/GUI Texture");
			usersImage.material.shader = ShaderCache.find("Unlit/GUI Texture Monochrome");
			Audio.play("Dialog3PromoteTravelsWeeklyRace");
			Audio.play("FeatureDRDialogLoser");
			animator.Play(LOSER_INTRO);
		}
	}

	public void playRewardAudio()
	{
		Audio.play("Dialog4Anim1WeeklyRace");
	}

	private void playOutro()
	{
		if (hasWon)
		{
			coinTrail.SetActive(true);
			animator.Play(WINNER_OUTRO);
			StartCoroutine(waitSequence(2.0f));
			Audio.play("Dialog1NavigateInWeeklyRace");
			Audio.play("CountCoinsWeeklyRace");
			StatsWeeklyRace.logDailyRivalsResults(division, playersScore, rivalsScore, rewardAmount, "click");
		}
		else
		{
			animator.Play(LOSER_OUTRO);
			StartCoroutine(waitSequence(1.0f));
			Audio.play("FeatureDRDialogLoserButton");
		}
	}

	private void updateState(string state)
	{
		stateMachine.updateState(state);
	}

	private IEnumerator waitSequence(float delay = 3.0f)
	{
		yield return new WaitForSeconds(delay);

		updateState(nextState);
	}

	private void sendCompleteEvent()
	{
		if (!string.IsNullOrEmpty(eventId))
		{
			WeeklyRaceAction.onDailyRivalsComplete(eventId);
		}
	}

	private bool hasWon
	{
		get { return rewardAmount > 0; }
	}

	private string nextState
	{
		get
		{
			switch (stateMachine.currentState)
			{
				case "intro":
					return "outro";

				case "outro":
					return "complete";

				default:
					return "complete";
			}
		}
	}
	public static void showDialog(JSON data)
	{
		bool addLeaderboard = WeeklyRace.clearLeaderboardFromDialogs();

		SocialMember rivalMember = CommonSocial.findOrCreate("", data.getString("rival_zid", ""));
		string rivalsImageUrl = rivalMember.getImageURL;

		Dict args = Dict.create(D.OBJECT, data);

		SchedulerPriority.PriorityType p = SchedulerPriority.PriorityType.LOW;
		if (Dialog.instance.currentDialog != null && Dialog.instance.currentDialog.type.keyName == "weekly_race_leaderboard")
		{
			p = SchedulerPriority.PriorityType.IMMEDIATE;
		}
		Dialog.instance.showDialogAfterDownloadingTextures("daily_rivals_complete", rivalsImageUrl, args, false, p);

		if (WeeklyRaceDirector.currentRace != null)
		{
			if (addLeaderboard)
			{
				WeeklyRaceLeaderboard.showDialog(Dict.create(D.OBJECT, WeeklyRaceDirector.currentRace));
			}
		}
	}
}