using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RoyalRushCollectionModule : TokenCollectionModule
{
	[SerializeField] private GameObject mover; //Mover for the UI bar
	[SerializeField] private LabelWrapperComponent sprintTimeRemainingLabel; //Time remaining in the current sprint
	[SerializeField] private LabelWrapperComponent currentSprintScoreLabel; //User's current sprint score
	[SerializeField] private LabelWrapperComponent currentRankLabel; //User's current sprint score
	[SerializeField] private GameObject timeProgressMeter; //Time progress sprite. Since it is not a filled sprite it needs to be scaled up/down for the filling up effect
	[SerializeField] private GameObject notEligibleForMoreTimeText; //Time progress sprite. Since it is not a filled sprite it needs to be scaled up/down for the filling up effect
	[SerializeField] private GameObject timeProgressSparkle; //Sparkle trail that moves across the time meter when progress is made
	[SerializeField] private GameObject greenTimePanel; //Green panel background
	[SerializeField] private GameObject redTimePanel; //Red panel background
	[SerializeField] private GameObject yellowTimePanel; //Yellow panel background
	[SerializeField] private Animator spinFTUEAnimator; //FTUE animation that plays over the spin button
	[SerializeField] private FacebookFriendInfo userInfo; //Used to display the current players photo
	[SerializeField] private Animator timerPausedOverlayAnimator; //Animation that plays when the timers are paused
	[SerializeField] private LabelWrapperComponent uiOverlayStateLabel; //Animation that plays when the timers are paused
	[SerializeField] private Animator currentPlayerRankAnimator; //Animator for the current player's rank tab
	[SerializeField] private Animator playerToBeatRankAnimator; //Animator for the tab of the player to beat
	[SerializeField] private Animator updateTimeAnimator; //Animator for updating the text itslef
	[SerializeField] private Animator clockAnimator; //Animator for the actual clock object
	[SerializeField] private GameObject rankUpOverlay; //Overlay that flies in when we rank up
	[SerializeField] private GameObject newHighScoreOverlay; //Overlay that flies in when we get a new high score
	[SerializeField] private GameObject additionalTimeOverlay; //Overlay that flies in when gaining time
	[SerializeField] private LabelWrapperComponent additionalTimeLabel; //Overlay that flies in when gaining time
	[SerializeField] private GameObject newSprintIntroOverlay; //Overlay that plays when a new sprint is started
	[SerializeField] private LabelWrapperComponent sprintStartOverlayLabel;
	[SerializeField] private ClickHandler topBarButton;
	[SerializeField] private LabelWrapperComponent currentScoreToBeatLabel;
	[SerializeField] private LabelWrapperComponent currentRankToBeatLabel;
	[SerializeField] private FacebookFriendInfo currentPlayerToBeatInfo; //Used to display the current players photo
	[SerializeField] private LabelWrapperComponent nextScoreToBeatLabel;
	[SerializeField] private LabelWrapperComponent nextRankToBeatLabel;
	[SerializeField] private FacebookFriendInfo nextPlayerToBeatInfo; //Used to display the current players photo
	[SerializeField] private GameObject refreshingTooltip; //Object shown while refreshing leaderboard data before showing the dialog
	[SerializeField] private AnimationListController.AnimationInformationList spinFTUEIntroAnimList;
	[SerializeField] private AnimationListController.AnimationInformationList spinFTUEOutroAnimList;

	[System.NonSerialized] public bool needsToSubmitScoreAfterSpinOutcome = false;

	//Game Timer for the current sprint
	private GameTimerRange sprintTimeRemaining
	{
		get { return _sprintRemainingTimer; }
		set
		{
			// Clear all registered labels for the old _sprintRemainingTimer, since we do not need to update th
			if (_sprintRemainingTimer != null)
			{
				_sprintRemainingTimer.clearLabels();
			}

			// Assign the new timer to _sprintRemainingTimer
			_sprintRemainingTimer = value;
		
			// If new timer is not paused, make sure we get the "Paused" Banner for previous timer removed.
			// We had a edge case problem that old _sprintRemainingTimer paused with "Paused" banner on and never get resumed,
			// and then new _sprintRemainingTimer gets created and starts, we check the new one is NOT paused so we will
			// never take an action to remove "Paused" banner.
			if (timerPausedOverlayAnimator != null && (_sprintRemainingTimer != null && !_sprintRemainingTimer.endTimer.isPaused))
			{
				timerPausedOverlayAnimator.Play(TIMER_UNPAUSE_ANIMATION);
			}
		}
	}

	private GameTimerRange _sprintRemainingTimer;

	public RoyalRushInfo currentRushInfo; //Most current info on the rush of the current game we're in

	private long currentSprintScore = 0; //Current score
	private int currentTimeMeterThreshold = 0; //Current threshold to pass to gain time
	private int meterProgress = 0; //current progress on the time meter
	private long currentHighScoreToBeat = 0; //Current high score in the current rush event
	private int currentRank = -1;

	private RoyalRushUser currentPlayerToBeat = null;

	private float timeProgressMeterMaxLength = 0; //X scale value for a completely filled time progress meter
	private float progressMeterTotalDistance = 0; //Total distance from one end of the time meter to the other
	private float idleTimer = 0;
	private float updateInfoTimer = 0;
	private int infoUpdateCooldown = 0; //The minimum amount of time that needs to happen before we grab fresh leaderboard info

	private bool addAdditionalTime = false; //True if we meet the current time meter threshold
	private bool isRankingUp = false; //True if current sprint score is higher than the score to beat
	private bool isNewHighScore = false; // true if current sprint score is higher than the current high score
	private bool tooltipIsAnimating = false; //Prevent the tooltip animation from spamming/overlapping
	private bool needsToPlayFTUEAnimations = false; //True if we're playing the event for the first time
	private bool sprintAlreadyStarted = false; //True if we're entering the game with a sprint already in progress
	private bool isHighScoreSprint = false;
	private bool isNewRulerSprint = false;
	private bool shouldShowStandingsOnUpdate = false; //Should we show the standings immediately or after an update
	private bool currentGameIsUnavailable = false; //True once the game has entered the unavailable state
	private bool hasReceivedResultsForPreviousCompetetion = false; //True if we've received the competetion ended results for this same game
	private bool waitingForSprintEndedResults = false; //True if we've submitted our score and are waiting for the ended results to come back

	private const int OVERLAY_Y_TWEEN_INTRO_TARGET = 700; //Where we move the overlay bar to on the inital load of the game
	private const int OVERLAY_Y_TWEEN_OUTRO_TARGET = 850; //Where we move the overlay bar to on the inital load of the game

	//Time Triggers
	private const int YELLOW_TIME_TRIGGER = 60; //Time when the background turns yellow
	private const int RED_TIME_TRIGGER = 30; //Time when the background turns red

	//Animation States
	private const string INTRO_ANIMS_NAME = "intro";
	private const string OUTRO_ANIMS_NAME = "outro";
	private const string TIMER_PAUSE_ANIMATION = "pause";
	private const string TIMER_UNPAUSE_ANIMATION = "off";
	private const string UPDATE_TIME_LEFT_TEXT_ANIMATION = "activate";
	private const string ADD_TIME_CLOCK_ANIMATION = "increaseTime";
	private const string USER_RANK_UP_ANIMATION = "rankUp";
	private const string RANK_UP_TO_FIRST_ANIMATION = "firstPlace";
	private const string ALREADY_IN_FIRST_ANIMATION = "loop";
	private const string FIRST_PLACE_IDLE_ANIMATION = "firstPlaceIdle";
	private const string NEW_HIGH_SCORE_ANIMATION = "newBest";
	private const string PLAYER_BEATEN_ANIMATION = "beatenByPlayer";
	private const string CURRENT_USER_RANK_IDLE_ANIMATION = "rankedIdle";
	private const string PLAYER_TO_BEAT_RANK_IDLE_ANIMATION = "idle";
	private const string HIGH_SCORE_IDLE_ANIMATION = "rankedIdle";
	private const string CURRENT_USER_DEFAULT_ANIMATION = "default";

	private const float IDLE_TIME = 5.0f;

	//Values for tweening the time progress meter
	private const float TIME_PROGRESS_METER_MIN_LENGTH = 78.0f; //Minimum value for the time meter progress X scale
	private const float TIME_PROGRESS_UPDATE_TWEEN_TIME = 0.5f; //Time it take for the meter to update
	private const float TIME_PROGRESS_SPARKLE_TRAIL_START = -719f; //Start of the progress meter
	private const float TIME_PROGRESS_SPARKLE_TRAIL_END = -490f; //End point

	//Animation Delay Times
	private const float RESET_TIME_BAR_DELAY = 0.5f;
	private const float ADDITIONAL_TIME_OVERLAY_ANIM_LENGTH = 2.0f;
	private const float NEW_SPRINT_START_DELAY = 2.75f;
	private const float NEW_HIGH_SCORE_ANIMATION_LENGTH = 2.0f;
	private const float PLAY_PLAYER_BEATEN_ANIM_DELAY = 1.0f;
	private const float PLAYER_BEATEN_ANIMATION_LENGTH = 0.5f;
	private const float UPDATE_PLAYER_TO_BEAT_LABELS = 0.5f;
	private const float POST_SPIN_ANIMATION_DELAY = 0.6f;
	private const float INTRO_UI_TWEEN_IN_TIME = 0.5f;

	//Sounds
	private const string NEW_SPRINT_START_SOUND = "StartNewRoundRRush01";
	private const string NEW_BEST_SCORE_SOUND = "NewBestRRush01";
	private const string RANKUP_SOUND = "RankUpRRush01";
	private const string RANKUP_TO_FIRST_SOUND = "RankUpToFirstRRush01";
	private const string ADD_TIME_SOUND = "AddTimeRRush01";
	private const string TIMER_ALERT_SOUND = "TimerAlertRRush01";

	//Localization Strings
	private const string CONTEST_ENDING_SOON_OVERLAY_TEXT = "royal_rush_contest_ending_soon";
	private const string EVENT_OVER_OVERLAY_TEXT = "royal_rush_event_over";
	private const string NEW_CONTEST_STARTED_OVERLAY_TEXT = "royal_rush_join_new_contest";

	void Update()
	{
		//Auto close when we've gone idle
		if (toolTipShowing && (Time.time - idleTimer > IDLE_TIME))
		{
			tooltipAnimator.Play(OUTRO_ANIMS_NAME);
			toolTipShowing = false;
		}

		if (currentRushInfo != null && !currentGameIsUnavailable && currentRushInfo.rushFeatureTimer.timeRemaining < RoyalRushEvent.minTimeRequired && (Time.time - updateInfoTimer > infoUpdateCooldown)) //Once we're unavailable lets auto-update once in a while
		{
			updateInfoTimer = Time.time;
			RoyalRushAction.getUpdate(GameState.game.keyName);
		}
	}

	private void onGetInfo(Dict args = null)
	{
		currentRushInfo.onGetInfo -= onGetInfo;
		setupBar();
	}

	private void onCallbackTimeout(Dict args = null, GameTimerRange sender = null)
	{
		if (currentRushInfo == null || currentRushInfo.currentState == RoyalRushInfo.STATE.AVAILABLE)
		{
			Destroy(this);
			string gameKey = GameState.game != null ? GameState.game.keyName : "Missing Game Key";
			Debug.LogError("RoyalRushCollectionModule::onCallbackTimeout - looks like our request to join royal rush timed out in " + gameKey);
		}
	}
	
	public override void setupBar()
	{
		Overlay.instance.hideShroud();
		currentRushInfo = RoyalRushEvent.instance.getInfoByKey(GameState.game.keyName);
		if (currentRushInfo == null)
		{
			string gameKey = GameState.game != null ? GameState.game.keyName : "Missing Game Key";
			
			Debug.LogError("SETTING UP THE ROYAL RUSH UI BUT NO RUSH INFO WAS FOUND FOR THIS GAME: " + gameKey);
			Destroy(this);
		}
		
		if (currentRushInfo.currentState == RoyalRushInfo.STATE.AVAILABLE)
		{
			GameTimerRange fiveSecondTimer = GameTimerRange.createWithTimeRemaining(5);
			fiveSecondTimer.registerFunction(onCallbackTimeout);
			currentRushInfo.onGetInfo -= onGetInfo;
			currentRushInfo.onGetInfo += onGetInfo;
			gameObject.SetActive(false);
			Debug.LogError("ENTERING THE ROYAL RUSH GAME WITHOUT BEING REGISTERED FIRST");
			currentRushInfo.registerForRush();
			return;
		}
		
		gameObject.SetActive(true);
		
		updateInfoTimer = Time.time;
		DisposableObject.register(gameObject); //Make sure this gets destroyed when loading into something else
		
		if (SpinPanel.instance.autoSpinButton != null)
		{
			SpinPanel.instance.autoSpinButton.gameObject.SetActive(ExperimentWrapper.RoyalRush.isAutoSpinEnabled);
		}
		else if (SpinPanel.instance.autoSpinHandler != null)
		{
			SpinPanel.instance.autoSpinHandler.enabled = ExperimentWrapper.RoyalRush.isAutoSpinEnabled;
			if (SpinPanel.instance.autoSpinTextCycler != null && !ExperimentWrapper.RoyalRush.isAutoSpinEnabled)
			{
				Destroy(SpinPanel.instance.autoSpinTextCycler);
			}
		}

		infoUpdateCooldown = RoyalRushEvent.rushInfoUpdateTime;
		topBarButton.registerEventDelegate(onBarClick);
		userInfo.member = SlotsPlayer.instance.socialMember; //Setting the player's profile picture in the UI
		
		// Attach to spin panel and set put in the middle
		spinFTUEAnimator.gameObject.transform.parent = (SpinPanelHIR.instance as SpinPanelHIR).royalRushFTUEAnchor.transform;

		// The animator is part of the NGUI overlay, so we refresh it here to avoid it having issues showing up.
		spinFTUEAnimator.gameObject.transform.localPosition = new Vector3(0, 0, -10);
		spinFTUEAnimator.gameObject.SetActive(false);
		spinFTUEAnimator.gameObject.SetActive(true);

		//Adding our delegates to the event list
		currentRushInfo.onGetInfo -= updateSprintInfo;
		currentRushInfo.onEndRush -= sprintEndedEvent;
		currentRushInfo.onEndCompetetion -= contestEndedEvent;

		currentRushInfo.onGetInfo += updateSprintInfo;
		currentRushInfo.onEndRush += sprintEndedEvent;
		currentRushInfo.onEndCompetetion += contestEndedEvent;

		sprintTimeRemaining = currentRushInfo.rushSprintTimer;
		currentHighScoreToBeat = currentRushInfo.highScore;
		//This sprite should be fully filled in the prefab. Storing our the X scale for a complete meter
		timeProgressMeterMaxLength = timeProgressMeter.transform.localScale.x;
		progressMeterTotalDistance = timeProgressMeterMaxLength - TIME_PROGRESS_METER_MIN_LENGTH;
		currentTimeMeterThreshold = currentRushInfo.timeMeterThreshold;
		currentRank = currentRushInfo.competitionRank;
		if (isSprintActive() && sprintTimeRemaining != null) //If we're entering a game with a sprint already in progress then make sure all of this is setup
		{
			sprintAlreadyStarted = true;
			sprintTimeRemaining.registerLabel(sprintTimeRemainingLabel.tmProLabel);
			currentSprintScore = currentRushInfo.userScore;
			needsToPlayFTUEAnimations = false;
			updateTimerOverlayPanel();
			currentSprintScoreLabel.text = CreditsEconomy.convertCredits(currentSprintScore);
			meterProgress = currentRushInfo.timeMeterProgress;
			float percentFilled = (float)meterProgress / (float)currentTimeMeterThreshold; //Calulate the percentage we should be filled at
			timeProgressMeter.transform.localScale = new Vector3(getMeterSpriteTweenScale(percentFilled), timeProgressMeter.transform.localScale.y, timeProgressMeter.transform.localScale.z); //If we haven't started yet then start this at the minumum value.
			timeProgressSparkle.transform.localPosition = new Vector3(getSparkleTweenTarget(percentFilled), timeProgressSparkle.transform.localPosition.y, timeProgressSparkle.transform.localPosition.z);

			if (currentSprintScore >= currentHighScoreToBeat && currentHighScoreToBeat != 0)
			{
				isHighScoreSprint = true;
				currentPlayerRankAnimator.Play(HIGH_SCORE_IDLE_ANIMATION);
			}
		}
		else
		{
			if (currentRushInfo.currentState != RoyalRushInfo.STATE.UNAVAILABLE) //If we're allowing new sprints then go ahead and set this all up
			{
				needsToPlayFTUEAnimations = true;
				currentSprintScoreLabel.text = "0";
				if (currentRushInfo.rushFeatureTimer.timeRemaining > RoyalRushEvent.initialSprintTime)
				{
					sprintTimeRemainingLabel.text = CommonText.secondsFormatted(RoyalRushEvent.initialSprintTime);
				}
				else //If theres not enough time for a full sprint, initialize the timer to be the competetion timer
				{
					sprintTimeRemaining = currentRushInfo.rushFeatureTimer;
					sprintTimeRemaining.registerLabel(sprintTimeRemainingLabel.tmProLabel);
				}
				updateTimerOverlayPanel();
				timeProgressMeter.transform.localScale = new Vector3(TIME_PROGRESS_METER_MIN_LENGTH, timeProgressMeter.transform.localScale.y, timeProgressMeter.transform.localScale.z); //If we haven't started yet then start this at the minumum value.
				timeProgressSparkle.transform.localPosition = new Vector3(TIME_PROGRESS_SPARKLE_TRAIL_START, timeProgressSparkle.transform.localPosition.y, timeProgressSparkle.transform.localPosition.z);
			}
			else
			{
				setUIToUnavailable();
			}
		}

		setPlayerToBeat();

		if (currentPlayerToBeat != null)
		{
			currentScoreToBeatLabel.text = CreditsEconomy.convertCredits(currentPlayerToBeat.score);
			currentRankToBeatLabel.text = (currentPlayerToBeat.position + 1).ToString();
			currentPlayerToBeatInfo.member = currentPlayerToBeat.member;
			currentRank = currentPlayerToBeat.position + 1;
		}
		else
		{
			playerToBeatRankAnimator.gameObject.SetActive(true); // Make sure this is on and active at this point, otherwise we'll run into issues with the animator not turning on
			//If there is no player to beat now, then it means our list is empty and theres no scores submitted to play against
			//Treat this like we're in first
			playerToBeatRankAnimator.Play(ALREADY_IN_FIRST_ANIMATION);
			currentRank = 0;
			isNewRulerSprint = true;
		}

		currentRankLabel.text = (currentRank + 1).ToString();

		SpinPanel.instance.setButtons(true);
		additionalTimeLabel.text = string.Format("+{0} Seconds!", RoyalRushEvent.additionalTimeAmount);
		//Set up current ranking 
		//Set up the new score to beat
	}

	//Event that gets called every spin
	public void updateSprintInfo(Dict data = null)
	{
		if (currentGameIsUnavailable)
		{
			if (currentRushInfo.currentState != RoyalRushInfo.STATE.UNAVAILABLE) //If the game was previously unavailable but isn't anymore then let the user know to retunr to the lobby to be rebucketed
			{
				topBarButton.clearAllDelegates();
				uiOverlayStateLabel.text = Localize.text(NEW_CONTEST_STARTED_OVERLAY_TEXT);
				if (hasReceivedResultsForPreviousCompetetion) //Only need to slide this in if it had been slid off
				{
					slideInTopMeter();
				}
			}
		}

		if (currentRushInfo.currentState == RoyalRushInfo.STATE.UNAVAILABLE)
		{
			setUIToUnavailable();
		}

		if (data.containsKey(D.DATA) && (data[D.DATA] as JSON).hasKey("rankings")) //If we're getting info with the rankings then lets see if player to beat needs to be updated
		{
			//Only stuff we care about updating are the current rank and player to beat info
			int lastRank = currentRank;
			currentRank = currentRushInfo.competitionRank;
			currentRankLabel.text = (currentRank + 1).ToString();

			setPlayerToBeat();
			if (currentPlayerToBeat != null)
			{
				currentScoreToBeatLabel.text = CreditsEconomy.convertCredits(currentPlayerToBeat.score);
				currentRankToBeatLabel.text = (currentPlayerToBeat.position + 1).ToString();
				currentPlayerToBeatInfo.member = currentPlayerToBeat.member;
			}
			if (currentRank > lastRank) //If we've ranked down then make sure we don't show the rank up animation
			{
				isRankingUp = false;
			}

			if (currentPlayerToBeat != null && currentRank != 0)
			{
				playerToBeatRankAnimator.Play(PLAYER_TO_BEAT_RANK_IDLE_ANIMATION);
			}

			if (shouldShowStandingsOnUpdate)
			{
				shouldShowStandingsOnUpdate = false;
				refreshingTooltip.SetActive(false);
				RoyalRushStandingsDialog.showDialog();
			}
		}
		else
		{
			if (!isEligibleForMoreTime())
			{
				timeProgressMeter.SetActive(false);
				notEligibleForMoreTimeText.SetActive(true);
			}
			if (!sprintAlreadyStarted || sprintTimeRemaining != currentRushInfo.rushSprintTimer) //If this is the first spin of the sprint then start up our timers
			{
				sprintTimeRemaining = currentRushInfo.rushSprintTimer;
				if (sprintTimeRemaining != null)
				{
					sprintTimeRemaining.registerLabel(sprintTimeRemainingLabel.tmProLabel);
					sprintAlreadyStarted = true;
				}
				else
				{
					currentSprintScoreLabel.text = "0";
					if (currentRushInfo.rushFeatureTimer.timeRemaining > RoyalRushEvent.initialSprintTime)
					{
						sprintTimeRemainingLabel.text = CommonText.secondsFormatted(RoyalRushEvent.initialSprintTime);
					}
					else //If theres not enough time for a full sprint, initialize the timer to be the competetion timer
					{
						sprintTimeRemaining = currentRushInfo.rushFeatureTimer;
						sprintTimeRemaining.registerLabel(sprintTimeRemainingLabel.tmProLabel);
					}
					timeProgressMeter.transform.localScale = new Vector3(TIME_PROGRESS_METER_MIN_LENGTH, timeProgressMeter.transform.localScale.y, timeProgressMeter.transform.localScale.z); //If we haven't started yet then start this at the minumum value.
				}
				updateTimerOverlayPanel();
			}

			long updatedScore = currentRushInfo.userScore; //Grab our new score
			if (currentTimeMeterThreshold != currentRushInfo.timeMeterThreshold && isSprintActive() && isEligibleForMoreTime()) //If we have a new threshold then we know we're gaining time
			{
				tokenWon = true; //If we have a new threshold then additional time has been awarded
				currentTimeMeterThreshold = currentRushInfo.timeMeterThreshold; //Store the new threshold
				addAdditionalTime = true;
			}

			meterProgress = currentRushInfo.timeMeterProgress;
			if (isSprintActive() && isEligibleForMoreTime())
			{
				StartCoroutine(updateTimeMeter());
			}
			if (currentRushInfo.currentState != RoyalRushInfo.STATE.SPRINT && currentRushInfo.currentState != RoyalRushInfo.STATE.PAUSED) //Only need to grab the submitted rank on the orignal setup
			{
				currentRank = currentRushInfo.competitionRank;
			}
			//Check through the rankings to see if we ranked up
			//Grab the score to beat
			if (currentRank > 0 && currentPlayerToBeat != null && updatedScore > (currentPlayerToBeat.score) && currentRushInfo.userInfos != null && currentRushInfo.userInfos.Count > 0)
			{
				tokenWon = true; //Lets treat ranking up as collecting something as well
				isRankingUp = true;
				currentRank--;
				if (currentRank > 0)
				{
					setPlayerToBeat();
					if (currentPlayerToBeat == null)
					{
						isNewRulerSprint = true;
						currentRank = 0;
						playerToBeatRankAnimator.Play(ALREADY_IN_FIRST_ANIMATION);
					}
					else
					{
						nextScoreToBeatLabel.text = CreditsEconomy.convertCredits(currentPlayerToBeat.score);
						nextRankToBeatLabel.text = (currentPlayerToBeat.position+1).ToString();
						nextPlayerToBeatInfo.member = currentPlayerToBeat.member;
						currentRank = currentPlayerToBeat.position + 1;
					}
				}
				else
				{
					currentPlayerToBeat = null;
					isNewRulerSprint = true;
				}

				if (Time.time - updateInfoTimer > infoUpdateCooldown)
				{
					updateInfoTimer = Time.time;
					RoyalRushAction.getUpdate(GameState.game.keyName);
				}
			}
			//If our current score is higher than our previous highest score then do the new high score animations
			if (isSprintActive() && updatedScore > currentHighScoreToBeat && updatedScore == currentRushInfo.highScore && currentHighScoreToBeat != 0)
			{
				isNewHighScore = true;
				tokenWon = true;
			}

			if (needsToSubmitScoreAfterSpinOutcome && !addAdditionalTime)
			{
				needsToSubmitScoreAfterSpinOutcome = false;
				StartCoroutine(submitScoreAfterADelay());
			}
		}
		
		if (currentRushInfo.currentState != RoyalRushInfo.STATE.PAUSED && sprintTimeRemaining != null && sprintTimeRemaining.endTimer.isPaused)
		{
			//Play unpause timer animations if timer is paused but we're not in the PAUSED state
			handleUnpausing();
		}
	}

	private float getSparkleTweenTarget(float percentFilled)
	{
		float moveTweenTarget = 0;
		moveTweenTarget = (TIME_PROGRESS_SPARKLE_TRAIL_END - TIME_PROGRESS_SPARKLE_TRAIL_START) * percentFilled + TIME_PROGRESS_SPARKLE_TRAIL_START; //The target x value for our sparkle trail to move to
		return moveTweenTarget;
	}

	private float getMeterSpriteTweenScale(float percentFilled)
	{
		float scaleTweenTarget = 0;
		scaleTweenTarget = TIME_PROGRESS_METER_MIN_LENGTH + progressMeterTotalDistance * percentFilled; //The target x value for our scale to stretch to
		return scaleTweenTarget;
	}

	private IEnumerator updateTimeMeter()
	{
		//tween the scale of our meter to the correct X scale
		//if we're granting additional time then go to 100%, add time, reset to new progress
		float scaleTweenTarget = 0;
		float moveTweenTarget = 0;
		//If we're adding additional time then hold everything at the max length/distance
		if (addAdditionalTime)
		{
			scaleTweenTarget = timeProgressMeterMaxLength;
			moveTweenTarget = TIME_PROGRESS_SPARKLE_TRAIL_END;
		}
		else
		{
			float percentFilled = (float)meterProgress / (float)currentTimeMeterThreshold; //Calulate the percentage we should be filled at
			scaleTweenTarget = getMeterSpriteTweenScale(percentFilled);
			moveTweenTarget = getSparkleTweenTarget(percentFilled);
		}

		Vector3 meterScale = new Vector3(scaleTweenTarget, timeProgressMeter.transform.localScale.y, timeProgressMeter.transform.localScale.z);
		Vector3 sparkleTarget = new Vector3(moveTweenTarget, timeProgressSparkle.transform.localPosition.y, timeProgressSparkle.transform.localPosition.z);

		timeProgressSparkle.SetActive(true); //Turn on the sparkle effect
		iTween.MoveTo(timeProgressSparkle, iTween.Hash("position", sparkleTarget, "time", TIME_PROGRESS_UPDATE_TWEEN_TIME, "islocal", true, "easetype", iTween.EaseType.linear)); //start tweening the sparkle
		iTween.ScaleTo(timeProgressMeter, iTween.Hash("scale", meterScale, "time", TIME_PROGRESS_UPDATE_TWEEN_TIME, "easetype", iTween.EaseType.linear)); //start stretching the meter sprite
		yield return new WaitForSeconds(TIME_PROGRESS_UPDATE_TWEEN_TIME);
		timeProgressSparkle.SetActive(false); //deactivate the sparkle once this is finished

		//If we're adding time then do the extra choreography
		if (addAdditionalTime)
		{
			Audio.play(ADD_TIME_SOUND);
			updateTimeAnimator.Play(UPDATE_TIME_LEFT_TEXT_ANIMATION); //Animation on the text changing
			yield return new WaitForSeconds(RESET_TIME_BAR_DELAY);
			//Reset our sparkle position and sprite scale once the animations are done
			timeProgressMeter.transform.localScale = new Vector3(TIME_PROGRESS_METER_MIN_LENGTH, timeProgressMeter.transform.localScale.y, timeProgressMeter.transform.localScale.z);
			timeProgressSparkle.transform.localPosition = new Vector3(TIME_PROGRESS_SPARKLE_TRAIL_START, timeProgressSparkle.transform.localPosition.y, timeProgressSparkle.transform.localPosition.z);
			clockAnimator.Play(ADD_TIME_CLOCK_ANIMATION); //Clock animation of it winding
			additionalTimeOverlay.SetActive(true); //Extra time up overlay that flies in and out
			updateTimerOverlayPanel(); //Check if the colored background needs to change
			addAdditionalTime = false;
			yield return new WaitForSeconds(ADDITIONAL_TIME_OVERLAY_ANIM_LENGTH);
			additionalTimeOverlay.SetActive(false); //Turn this back off once its off screen again
		}
	}

	//Occurs after the spin is complete if we're ranking up or getting a new high score
	public override IEnumerator addTokenAfterCelebration()
	{
		if (isNewHighScore && !isRankingUp && !isHighScoreSprint) //Only play the new high score animations if we're not also ranking up
		{
			isNewHighScore = false;
			isHighScoreSprint = true;
			StartCoroutine(playNewHighScoreAnimations());
		}

		if (isRankingUp)
		{
			//Play our rank up animations
			isRankingUp = false;
			isHighScoreSprint = true;
			isNewHighScore = false;
			StartCoroutine(playRankUpAnimations());
		}

		tokenWon = false;
		yield break;
	}

	private IEnumerator playRankUpAnimations()
	{
		yield return new WaitForSeconds(POST_SPIN_ANIMATION_DELAY);
		string rankUpSound = currentRank == 0 ? RANKUP_TO_FIRST_SOUND : RANKUP_SOUND;
		string rankUpAnimation = currentRank == 0 ? RANK_UP_TO_FIRST_ANIMATION : PLAYER_BEATEN_ANIMATION;
		Audio.play(rankUpSound);
		rankUpOverlay.SetActive(true);
		currentPlayerRankAnimator.Play(USER_RANK_UP_ANIMATION);
		currentRankLabel.text = (currentRank + 1).ToString();
		yield return new WaitForSeconds(PLAY_PLAYER_BEATEN_ANIM_DELAY);
		playerToBeatRankAnimator.Play(rankUpAnimation);
		yield return new WaitForSeconds(UPDATE_PLAYER_TO_BEAT_LABELS);
		if (currentPlayerToBeat != null)
		{
			currentScoreToBeatLabel.text = CreditsEconomy.convertCredits(currentPlayerToBeat.score);
			currentRankToBeatLabel.text = (currentPlayerToBeat.position+1).ToString();
			currentPlayerToBeatInfo.member = currentPlayerToBeat.member;
		}
		yield return new WaitForSeconds(PLAYER_BEATEN_ANIMATION_LENGTH);
		rankUpOverlay.SetActive(false);
	}

	private IEnumerator playNewHighScoreAnimations()
	{
		yield return new WaitForSeconds(POST_SPIN_ANIMATION_DELAY);
		Audio.play(NEW_BEST_SCORE_SOUND);
		newHighScoreOverlay.SetActive(true);
		currentRankLabel.text = (currentRank + 1).ToString();
		currentPlayerRankAnimator.Play(USER_RANK_UP_ANIMATION);
		yield return new WaitForSeconds(NEW_HIGH_SCORE_ANIMATION_LENGTH);
		newHighScoreOverlay.SetActive(false);
	}

	public override void betChanged (bool isIncreasingBet)
	{
		idleTimer = Time.time;
		//Open up the tooltip
		if (!toolTipShowing && !tooltipIsAnimating && currentRushInfo != null && currentRushInfo.currentState != RoyalRushInfo.STATE.UNAVAILABLE)
		{
			showToolTip();
		}

		//If already showing then just reset the close timer
	}

	public override void spinHeld()
	{
		//Turn off the extra FTUE animations if those just played
		if (needsToPlayFTUEAnimations)
		{
			if (spinFTUEAnimator.GetCurrentAnimatorStateInfo(0).IsName(INTRO_ANIMS_NAME))
			{
				StartCoroutine(playSpinFTUEAnimationOutro());
			}
			newSprintIntroOverlay.SetActive(false);
			needsToPlayFTUEAnimations = false;
		}
	}

	public override void spinPressed()
	{
		//Close tooltips/overlays
		if (toolTipShowing)
		{
			tooltipAnimator.Play(OUTRO_ANIMS_NAME);
			toolTipShowing = false;
		}

		//Turn off the extra FTUE animations if those just played
		if (needsToPlayFTUEAnimations)
		{
			if (spinFTUEAnimator.GetCurrentAnimatorStateInfo(0).IsName(INTRO_ANIMS_NAME))
			{
				StartCoroutine(playSpinFTUEAnimationOutro());
			}
			newSprintIntroOverlay.SetActive(false);
			needsToPlayFTUEAnimations = false;
		}

		if (sprintTimeRemaining != null && sprintTimeRemaining.startTimer.isPaused && !currentGameIsUnavailable)
		{
			if (currentRank == 0 || currentPlayerToBeat == null)
			{
				playerToBeatRankAnimator.Play(ALREADY_IN_FIRST_ANIMATION);
			}
		}
	}

	private IEnumerator playSpinFTUEAnimationIntro()
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(spinFTUEIntroAnimList));
		// Hide in-game feature UI that might overlap with this FTUE. e.g. board game dices
		InGameFeatureContainer.showFeatureUI(false);
	}
	
	
	private IEnumerator playSpinFTUEAnimationOutro()
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(spinFTUEOutroAnimList));
		// Wait until the FTUE completely hides, then display in-game feature UI that hid in playSpinFTUEAnimationIntro
		InGameFeatureContainer.showFeatureUI(true);
	}
	
	private void handleUnpausing()
	{
		if (!currentGameIsUnavailable)
		{
			if (timerPausedOverlayAnimator != null)
			{
				timerPausedOverlayAnimator.Play(TIMER_UNPAUSE_ANIMATION);
			}

			if (sprintTimeRemaining != null)
			{
				sprintTimeRemaining.unPauseTimers();
			}
		}
	}

	public void sprintEndedEvent(Dict data = null)
	{
		//Make sure the sprint is actually over
		//Disable buttons to make sure no spins happen while we're doing this stuffs
		//Submit the final score
		//Pop sprint ended dialog
		waitingForSprintEndedResults = false;
		string summaryState = RoyalRushSprintSummary.NO_NEW_BEST_STATE;
		string statPhylum = "not_best_score";
		if (currentRushInfo.userScore >= currentRushInfo.highScore) //Check to make sure the sprint ended score is our highest score
		{
			statPhylum = "best_score";

			if (currentRushInfo.competitionRank == 0)
			{
				summaryState = RoyalRushSprintSummary.NEW_RULER_STATE;
			}
			else
			{
				summaryState = RoyalRushSprintSummary.NEW_BEST_STATE;
			}
		}

		sprintTimeRemaining = null;
		needsToSubmitScoreAfterSpinOutcome = false;
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "royal_rush_round_over",
			phylum: statPhylum,
			genus: "view"
		);

		StatsManager.Instance.LogCount
		(
			counterName: "royal_rush", 
			kingdom: "round_submit", 
			phylum: currentRushInfo.rushKey,
			klass: "",
			family: statPhylum,
			genus: CreditsEconomy.convertCredits(currentRushInfo.userScore)
		);

		Dict args = Dict.create(D.DATA, currentRushInfo, D.OPTION, summaryState);
		RoyalRushStandingsDialog.showDialog(args);
		if (!Glb.spinTransactionInProgress)
		{
			SpinPanel.instance.setButtons(true);
		}
	}

	public void resetOverlayUI()
	{
		if (currentRushInfo.rushFeatureTimer.timeRemaining < RoyalRushEvent.minTimeRequired)
		{
			setUIToUnavailable();
		}
		else
		{
			if (!currentGameIsUnavailable)
			{
				timerPausedOverlayAnimator.Play(TIMER_UNPAUSE_ANIMATION);
			}
			//Reset all our stuff for the next sprint
			if (currentRushInfo.currentState == RoyalRushInfo.STATE.COMPLETE || currentRushInfo.currentState == RoyalRushInfo.STATE.STARTED)
			{
				sprintTimeRemaining = null;
				sprintAlreadyStarted = false;
				isHighScoreSprint = false;
				isNewRulerSprint = false;
				isNewHighScore = false;
				timeProgressMeter.transform.localScale = new Vector3(TIME_PROGRESS_METER_MIN_LENGTH, timeProgressMeter.transform.localScale.y, timeProgressMeter.transform.localScale.z); //If we haven't started yet then start this at the minumum value.
				timeProgressSparkle.transform.localPosition = new Vector3(TIME_PROGRESS_SPARKLE_TRAIL_START, timeProgressSparkle.transform.localPosition.y, timeProgressSparkle.transform.localPosition.z);
				currentSprintScore = 0;
				meterProgress = 0;
				currentTimeMeterThreshold = currentRushInfo.timeMeterThreshold;
				currentRank = currentRushInfo.competitionRank;
				currentSprintScoreLabel.text = CreditsEconomy.convertCredits(currentSprintScore);
				currentRankLabel.text = (currentRank + 1).ToString();
				if (currentRushInfo.rushFeatureTimer.timeRemaining > RoyalRushEvent.initialSprintTime)
				{
					sprintTimeRemainingLabel.text = CommonText.secondsFormatted(RoyalRushEvent.initialSprintTime);
				}
				else //If theres not enough time for a full sprint, initialize the timer to be the competetion timer
				{
					sprintTimeRemaining = currentRushInfo.rushFeatureTimer;
					sprintTimeRemaining.registerLabel(sprintTimeRemainingLabel.tmProLabel);
				}
			}
			updateTimerOverlayPanel();
		}
		//When the sprint ends, we get updated leaderboards, so refresh the player to beat
		if (currentRushInfo.currentState != RoyalRushInfo.STATE.SPRINT || !isHighScoreSprint) //If we haven't started the next sprint or don't have a high score in the new sprint, go ahead and put this back into the orange state
		{
			currentPlayerRankAnimator.Play(CURRENT_USER_DEFAULT_ANIMATION);
		}
		currentHighScoreToBeat = currentRushInfo.highScore;
		isNewHighScore = false;
		setPlayerToBeat();
		if (currentPlayerToBeat != null)
		{
			playerToBeatRankAnimator.Play(PLAYER_TO_BEAT_RANK_IDLE_ANIMATION);
			currentScoreToBeatLabel.text = CreditsEconomy.convertCredits(currentPlayerToBeat.score);
			currentRankToBeatLabel.text = (currentPlayerToBeat.position + 1).ToString();
			SocialMember rankedUser = CommonSocial.findOrCreate("-1", currentPlayerToBeat.zid);
			currentPlayerToBeatInfo.member = rankedUser;
		}
		else
		{
			//If there is no player to beat now, then it means our list is empty and theres no scores submitted to play against
			//Treat this like we're in first
			playerToBeatRankAnimator.Play(ALREADY_IN_FIRST_ANIMATION);
		}
	}

	//Updates the colored background of the overlay. Triggered by timer events and when time is added
	private void updateTimerOverlayPanel(Dict data = null, GameTimerRange sender = null)
	{
		if (currentRushInfo.currentState == RoyalRushInfo.STATE.UNAVAILABLE || currentGameIsUnavailable)
		{
			return;
		}

		int timeRemaining = (sprintTimeRemaining != null && !sprintTimeRemaining.isExpired) ? sprintTimeRemaining.timeRemaining : RoyalRushEvent.initialSprintTime;
		if (timeRemaining > YELLOW_TIME_TRIGGER || currentRushInfo.currentState == RoyalRushInfo.STATE.STARTED) //Automatically green if we're over the yellow trigger
		{
			redTimePanel.SetActive(false);
			greenTimePanel.SetActive(true);
			yellowTimePanel.SetActive(false);
			if (sprintTimeRemaining != null && isSprintActive())
			{
				if (!sprintTimeRemaining.isEventRegisteredOnActiveTimer(updateTimerOverlayPanelTimerEvent))
				{
					sprintTimeRemaining.registerFunction(updateTimerOverlayPanelTimerEvent, null, YELLOW_TIME_TRIGGER); //If the bar is currently in the green then register for the yellow event
				}
			}
		}
		else if (timeRemaining > RED_TIME_TRIGGER) //Yellow if we're less than the yellow trigger but greater than red
		{
			Audio.play(TIMER_ALERT_SOUND);
			if (sprintTimeRemaining != null && !sprintTimeRemaining.isEventRegisteredOnActiveTimer(updateTimerOverlayPanelTimerEvent))
			{
				sprintTimeRemaining.registerFunction(updateTimerOverlayPanelTimerEvent, null, RED_TIME_TRIGGER); //If the bar is currently in the green then register for the yellow event
			}

			redTimePanel.SetActive(false);
			greenTimePanel.SetActive(false);
			yellowTimePanel.SetActive(true);
		}
		else //Red if we're under the yellow & red trigger times
		{
			Audio.play(TIMER_ALERT_SOUND);
			redTimePanel.SetActive(true);
			greenTimePanel.SetActive(false);
			yellowTimePanel.SetActive(false);
		}
	}

	public static void updateTimerOverlayPanelTimerEvent(Dict data = null, GameTimerRange sender = null)
	{
		RoyalRushCollectionModule topMeter = getTopMeter();

		if (topMeter != null)
		{
			topMeter.updateTimerOverlayPanel(data, sender);
		}
	}

	//Called after returning to the base game from a bonus game
	public override IEnumerator setTokenState()
	{
		yield return null;
		//Resume the sprint timer/Play animations
		if (currentRushInfo != null)
		{
			if (playerToBeatRankAnimator != null && (currentRank == 0 || currentRushInfo.userInfos == null || currentRushInfo.userInfos.Count <= 0))
			{
				playerToBeatRankAnimator.Play(FIRST_PLACE_IDLE_ANIMATION);
			}

			if (currentPlayerRankAnimator != null && isHighScoreSprint)
			{
				currentPlayerRankAnimator.Play(HIGH_SCORE_IDLE_ANIMATION);
			}

			if (currentRushInfo.rushSprintTimer != null)
			{
				currentRushInfo.rushSprintTimer.updateEndTime(currentRushInfo.sprintTimeLeft);
			}
			else
			{
				Bugsnag.LeaveBreadcrumb("Royal Rush's sprint timer was null when we went to update it. Remaking it and updating it");
				currentRushInfo.rushSprintTimer = GameTimerRange.createWithTimeRemaining(currentRushInfo.sprintTimeLeft);
			}

			if (timerPausedOverlayAnimator != null)
			{
				timerPausedOverlayAnimator.Play(TIMER_PAUSE_ANIMATION);

				if (sprintTimeRemaining != null)
				{
					sprintTimeRemaining.pauseTimers();
				}
			}
			updateTimerOverlayPanel();
		}
	}

	public void pauseTimers()
	{
		//Play any pause animations that we need to play
		if (sprintTimeRemaining != null && !sprintTimeRemaining.endTimer.isPaused)
		{
			sprintTimeRemaining.pauseTimers();
		}

		timerPausedOverlayAnimator.Play(TIMER_PAUSE_ANIMATION);
		if (currentRank == 0)
		{
			playerToBeatRankAnimator.Play(FIRST_PLACE_IDLE_ANIMATION);
		}
	}

	//Called when we rollup winnings so we can update the sprint score simultaneously
	public void updateSprintScore(long amountToAdd = 0)
	{
		if (isSprintActive())
		{
			long updatedScore = currentRushInfo.userScore; //Grab our new score
			if (amountToAdd != 0)
			{
				updatedScore = currentSprintScore + amountToAdd;
			}
			StartCoroutine(SlotUtils.rollup(currentSprintScore, updatedScore, currentSprintScoreLabel));
			currentSprintScore = updatedScore;
		}
	}

	public override IEnumerator slotStarted()
	{
		while (Loading.isLoading)
		{
			yield return null; //Want to wait till the loading bar is gone so we actually see the animations
		}
			
		//If this is the beginning of a new sprint then do the extra animations
		//Don't bother dropping in the UI if we're in the unavailable state
		if (currentRushInfo.currentState != RoyalRushInfo.STATE.UNAVAILABLE)
		{
			if (needsToPlayFTUEAnimations)
			{
				yield return RoutineRunner.instance.StartCoroutine(playSprintStartAnimations());
			}
			slideInTopMeter();
		}
	}

	public IEnumerator playSprintStartAnimations()
	{
		needsToPlayFTUEAnimations = true; //Making sure we turn these off if they got turned on
		if ((currentRushInfo.rushFeatureTimer.timeRemaining/60) < RoyalRushEvent.contestEndingSoonTime)
		{
			sprintStartOverlayLabel.text = Localize.text(CONTEST_ENDING_SOON_OVERLAY_TEXT);
		}
		if (currentRushInfo.currentState == RoyalRushInfo.STATE.STARTED || currentRushInfo.currentState == RoyalRushInfo.STATE.COMPLETE) //Make sure to only play this when a sprint is starting
		{
			Audio.play(NEW_SPRINT_START_SOUND);
			newSprintIntroOverlay.SetActive(true);
			yield return new WaitForSeconds(NEW_SPRINT_START_DELAY);
			if (needsToPlayFTUEAnimations) //If a spin happened while the sprint start overlay was up then let's not show this stuff
			{
				if (spinFTUEAnimator != null)
				{
					StartCoroutine(playSpinFTUEAnimationIntro());
				}

				if (this != null && mover.transform.localPosition.y != OVERLAY_Y_TWEEN_INTRO_TARGET)
				{
					StartCoroutine(showToolTipAfterWaitingForIntroTween());
				}
				else
				{
					showToolTip();
				}
			}
		}
	}

	private IEnumerator showToolTipAfterWaitingForIntroTween()
	{
		yield return new WaitForSeconds(INTRO_UI_TWEEN_IN_TIME);
		showToolTip();
	}

	private void onBarClick(Dict args = null)
	{
		if (!SlotBaseGame.instance.isGameBusy)
		{
			StatsManager.Instance.LogCount
			(
				counterName: "dialog",
				kingdom: "royal_rush_standings",
				klass: "in_game",
				genus: "view"
			);

			if (Time.time - updateInfoTimer > infoUpdateCooldown && currentRushInfo.inWithinRegistrationTime() && !currentGameIsUnavailable)
			{
				updateInfoTimer = Time.time;
				refreshingTooltip.SetActive(true);
				RoyalRushAction.getUpdate(GameState.game.keyName);
				shouldShowStandingsOnUpdate = true;
			}
			else
			{
				refreshingTooltip.SetActive(false);
				shouldShowStandingsOnUpdate = false;
				RoyalRushStandingsDialog.showDialog();
			}
		}
	}

	private void setPlayerToBeat()
	{
		if (currentRushInfo.userInfos != null && currentRushInfo.userInfos.Count > 0) //This can be empty/null if there are no submitted scores yet
		{
			if (currentRank < 0)
			{
				currentPlayerToBeat = currentRushInfo.userInfos[currentRushInfo.userInfos.Count - 1];
			}
			else if (currentRank == 0) //First Place
			{
				currentPlayerToBeat = null;
			}
			else
			{
				if (isSprintActive())
				{
					//If we're mid sprint then we need to iterate through the list of rankings since now our saved competetion rank will be out of date
					for(int i = currentRank-1; i >= 0; i--)
					{
						RoyalRushUser playerToBeat = currentRushInfo.userInfos[i]; 
						if ((playerToBeat.score) <= currentRushInfo.userScore)
						{
							continue;
						}
						else
						{
							currentPlayerToBeat = playerToBeat;
							break;
						}
					}
				}
				else
				{
					currentPlayerToBeat = currentRushInfo.userInfos[currentRank-1];
				}
			}
		}
	}

	public override void showToolTip ()
	{
		if (!toolTipShowing && !tooltipIsAnimating)
		{
			toolTipShowing = true;
			if (this != null && tooltipAnimator != null)
			{
				tooltipAnimator.Play(INTRO_ANIMS_NAME);
			}
			idleTimer = Time.time;
		}
	}

	public override void hideBar ()
	{
		if (GameState.game == null && currentRushInfo != null)
		{
			currentRushInfo.onGetInfo -= updateSprintInfo;
			currentRushInfo.onEndRush -= sprintEndedEvent;
			currentRushInfo.onEndCompetetion -= contestEndedEvent;
		}
	}

	private bool isSprintActive()
	{
		return (currentRushInfo.currentState == RoyalRushInfo.STATE.SPRINT || currentRushInfo.currentState == RoyalRushInfo.STATE.PAUSED);
	}

	private bool isEligibleForMoreTime()
	{
		return currentRushInfo.sprintTimeLeft + RoyalRushEvent.additionalTimeAmount < currentRushInfo.rushFeatureTimer.timeRemaining - RoyalRushEvent.scoreSubmitEnd;	
	}

	private void hideTopMeter()
	{
		Vector3 barTarget = new Vector3(mover.transform.localPosition.x, OVERLAY_Y_TWEEN_OUTRO_TARGET, mover.transform.localPosition.z);
		iTween.MoveTo(mover, iTween.Hash("position", barTarget, "time", INTRO_UI_TWEEN_IN_TIME, "islocal", true, "easetype", iTween.EaseType.linear));
		if (needsToPlayFTUEAnimations)
		{
			newSprintIntroOverlay.SetActive(false);
			StartCoroutine(playSpinFTUEAnimationOutro());
		}

		if (toolTipShowing)
		{
			StartCoroutine(playSpinFTUEAnimationOutro());
		}
	}

	private void setUIToUnavailable()
	{
		uiOverlayStateLabel.text = Localize.text(EVENT_OVER_OVERLAY_TEXT);
		timerPausedOverlayAnimator.Play(TIMER_PAUSE_ANIMATION);
		if (currentRank == 0)
		{
			playerToBeatRankAnimator.Play(FIRST_PLACE_IDLE_ANIMATION);
		}

		if (sprintTimeRemaining != null)
		{
			sprintTimeRemaining.pauseTimers();
		}

		greenTimePanel.SetActive(false);
		yellowTimePanel.SetActive(false);
		redTimePanel.SetActive(false);
		currentGameIsUnavailable = true;
	}

	private IEnumerator submitScoreAfterADelay()
	{
		yield return new WaitForSeconds(1.0f);
		currentRushInfo.sendScore();
	}

	public void contestEndedEvent(Dict data = null)
	{
		setUIToUnavailable();
	}

	public void disableSpinsOnTimeOut()
	{
		SpinPanel.instance.setButtons(false); //Don't let the player press the spin button
		SpinPanel.instance.resetAutoSpinUI();
		RoyalRushEvent.waitingForSprintSummary = true; //Blocks spins from happening in case the player already had the Spin button pressed down before we disabled it
		StartCoroutine(waitForAPossibleLastSpinThenSendScore());
	}


	private IEnumerator waitForAPossibleLastSpinThenSendScore()
	{
		yield return null; //Give the client a little breathing room incase spins happen at 0 seconds left
		//If after disabling spins and waiting a frame we're not in a spin then we can go ahead and submit the score
		waitingForSprintEndedResults = true;
		if (!Glb.spinTransactionInProgress || SlotBaseGame.instance.outcome != null)
		{
			currentRushInfo.sendScore();
		}
		else
		{
			//If we're in the middle of a spin then lets remember to submit the score after we get an outcome
			needsToSubmitScoreAfterSpinOutcome = true;
		}
		StartCoroutine(activateButtonsAfterADelay());
	}

	private IEnumerator activateButtonsAfterADelay()
	{
		yield return new TIWaitForSeconds(RoyalRushEvent.scoreSubmittedResponseTimeout);
		if (waitingForSprintEndedResults)
		{
			Debug.LogError("We submitted our score but didn't get a response back. Resuming the game now");
			RoyalRushEvent.waitingForSprintSummary = false;
			SpinPanel.instance.setButtons(true);
			SpinPanel.instance.resetAutoSpinUI();
		}
	}
		
	public void updateInfoAfterCompetetionEnd(string gameKey)
	{
		//Go ahead and hide the UI bar if we received a score for this rush and game key and the current rush info we're using is in the UNAVAILABLE state
		if (currentRushInfo != null && gameKey == currentRushInfo.gameKey && currentRushInfo.currentState == RoyalRushInfo.STATE.UNAVAILABLE)
		{
			hasReceivedResultsForPreviousCompetetion = true;
			hideTopMeter();
		}
	}

	private void slideInTopMeter()
	{
		//Do the drop-in of the overlay
		Vector3 barTarget = new Vector3(mover.transform.localPosition.x, OVERLAY_Y_TWEEN_INTRO_TARGET, mover.transform.localPosition.z);
		iTween.MoveTo(mover, iTween.Hash("position", barTarget, "time", INTRO_UI_TWEEN_IN_TIME, "islocal", true, "easetype", iTween.EaseType.linear));
	}
		
	private static RoyalRushCollectionModule getTopMeter()
	{
		if (SlotBaseGame.instance != null && 
			SlotBaseGame.instance.isRoyalRushGame && 
			Overlay.instance.jackpotMysteryHIR.tokenBar != null &&
			Overlay.instance.jackpotMysteryHIR.tokenBar as RoyalRushCollectionModule != null)
		{
			return Overlay.instance.jackpotMysteryHIR.tokenBar as RoyalRushCollectionModule;
		}

		return null;
	}

	private void showTopMeterMover(bool show)
	{
		if (mover != null)
		{
			mover.SetActive(show);
		}
	}

	public static void showTopMeter(bool show)
	{
		RoyalRushCollectionModule topMeter = getTopMeter();

		if (topMeter != null)
		{
			topMeter.showTopMeterMover(show);
		}
	}

	private void OnDestroy()
	{
		if (currentRushInfo != null)
		{
			currentRushInfo.onGetInfo -= updateSprintInfo;
			currentRushInfo.onEndRush -= sprintEndedEvent;
			currentRushInfo.onEndCompetetion -= contestEndedEvent;
		}
	}

	// Implements IResetGame
	public static void resetStaticClassData()
	{

	}
}
