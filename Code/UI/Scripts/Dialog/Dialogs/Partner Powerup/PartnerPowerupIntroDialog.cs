﻿﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

public class PartnerPowerupIntroDialog : DialogBase 
{
	public PPULightObject[] activatedLights;
	public TextMeshPro titleText;
	public TextMeshPro mySpins;
	public TextMeshPro friendSpins;
	public TextMeshPro rewardAmount;
	public TextMeshPro timeLeft;
	public TextMeshPro timeLeftShadow;
	public TextMeshPro amountNeeded;
	public TextMeshPro userIndividualGoal;
	public TextMeshPro buddyIndividualGoal;
	public TextMeshPro goalTypeText;
	public TextMeshPro goalTypeSubText;
	public TextMeshPro nudgeButtonTimerText;
	public TextMeshPro messageLabel;

	// Part of the user won animation sequence.
	public TextMeshPro individualWinGoalType;
	public TextMeshPro individualWinGoalAmount;
	public TextMeshPro individualWinGoalSubtype;

	public GameObject endsInParent;

	// User/Buddy images
	public FacebookFriendInfo buddyInfo;
	public FacebookFriendInfo myFacebookPic;

	public ButtonHandler exitButton;
	public ButtonHandler nudgeButton;
	public ButtonHandler collectButton;

	public UISprite userFillSprite;
	public UISprite friendFillSprite;
	public UISprite mainMeterFillSprite;

	// User area animations
	public Animator userHammer;
	public Animator buddyHammer;
	public Animator nudgeAnimation;
	public Animator buddyShuffleAnimation;
	public Animator userWinProfileAnimation;
	public Animator userWinMeterAnimation;

	// On PPU complete (both users) animation
	public Animator onWinTitleFlash;
	public Animator onWinAmountFlash;
	public Animator onWinCollectAndShareAnimation;
	public Animator onWinMeterAnimation;

	private GameTimerRange timeRemaining;
	private GameTimerRange nudgeTimer;
	private string lightOnAudio = "ProgressPip{0}PP";
	private string eventID = "";
	private string HAMMER_IDLE = "Hammer idle";
	private string statsKingdom = "";
	private string startMode = "";

	private static bool hasRecievedUpdate = false;

	// Sounds
	private const string USER_WIN_FANFARE = "BellWinFanfarePP";
	private const string HAMMER_SMASH = "HammerSmashPP";
	private const string GREEN_FILL_SOUND = "MeterFillPP";
	private const string TEXT_HIGHLIGHT_SOUND = "HighlightTextPP";
	private const string PICK_LOOP = "PickAPartnerLoopPP";
	private const string CLOSE_SOUND = "CloseDialogPP";
	private const string REWARD_FANFARE = "RewardWinFanfarePP";
	private const string PICK_END = "PickAPartnerEndPP";
	private const string NUDGE_SOUND = "NudgePlayerPP";
	private const string TITLE_HIGHLIGHT = "messaging";

	// Animation States
	private const string NUDGE_ENABLED = "pick me";
	private const string HAMMER_HIT = "Hammer hit";
	private const string NUDGE_DISABLED = "gray";
	private const string NUDGE_PLAY = "play";
	private const string ON_STATE = "on";
	private const string INTRO_STATE = "intro";
	private const string OUTRO_STATE = "outro";
	private const string TEAM_GOAL_WIN = "Team Goal Win animation";
	private const string REWARD_ANIMATION = "play reward animation";
	private const string INTRO_SHUFFLE_STATE = "shuffle ani";
	private const string DEFAULT_STATE = "defualt";

	public override void init()
	{
		// Used to see if we can nudge or not.
		int lastNudgedTimestamp = PlayerPrefsCache.GetInt(Prefs.LAST_NUDGED_TIME, 0);
		int timeSinceLastNudge = GameTimer.currentTime - lastNudgedTimestamp;

		// Close delegate first, so users can always leave.
		exitButton.registerEventDelegate(onClickClose);

		friendFillSprite.fillAmount = 0;
		userFillSprite.fillAmount = 0;
		mainMeterFillSprite.fillAmount = 0;
		PartnerPowerupCampaign campaign = CampaignDirector.partner;
		Audio.switchMusicKeyImmediate("FeatureDialogBgPP");

		userHammer.Play(HAMMER_IDLE);
		buddyHammer.Play(HAMMER_IDLE);
		
		if (dialogArgs.containsKey(D.EVENT_ID))
		{
			eventID = (string)dialogArgs.getWithDefault(D.EVENT_ID, "");
		}

		if (dialogArgs != null && dialogArgs.containsKey(D.DATA))
		{
			startMode = dialogArgs [D.DATA] as string;
		}

		if (dialogArgs != null && dialogArgs.containsKey(D.TYPE))
		{
			statsKingdom = dialogArgs[D.TYPE] as string;
			StatsManager.Instance.LogCount(counterName: "dialog", kingdom: statsKingdom, genus: "view");
		}

		setupUserImages(startMode);

		titleText.text = Localize.text("team_up_and_complete_challenge");

		if (campaign != null)
		{
			switch (CampaignDirector.partner.challengeType)
			{
			case "spins":
				individualWinGoalType.text = Localize.toUpper("spin");
				goalTypeText.text = Localize.toUpper("spin");
				goalTypeSubText.text = Localize.toUpper("times");
				individualWinGoalSubtype.text = Localize.toUpper("times");
				break;

			case "coins_won":
				individualWinGoalType.text = Localize.toUpper("win");
				goalTypeText.text = Localize.toUpper("win");
				goalTypeSubText.text = Localize.toUpper("coins");
				individualWinGoalSubtype.text = Localize.toUpper("coins");
				break;
			}

			if (!string.IsNullOrEmpty(startMode))
			{
				switch (startMode)
				{
				case "START":
					PlayerPrefsCache.SetInt(Prefs.HAS_SHOWN_PPU_START, 1);
					// Play the on start animation of users shuffling
					StartCoroutine(playIntroAnimation());
					break;

				case "PAST COMPLETE":
					safeSetup(campaign);
					StartCoroutine(playChallengeCompleteState());
					return;

				case "COMPLETE":
					// The state is complete, i dont care what we're at right now, we should be at 100% done.
					titleText.text = Localize.text ("challenge_completed");
					campaign.userProgress = campaign.individualProgressRequired;
					campaign.buddyProgress = campaign.individualProgressRequired;

					// Play the on start animation of users shuffling
					StartCoroutine(playChallengeCompleteState());
					break;

				case "USERCOMPLETE":
						// Just to make sure, set the progress to where we wanna
					campaign.userProgress = campaign.individualProgressRequired;
					PlayerPrefsCache.SetInt(Prefs.HAS_SHOWN_PPU_COMPLETE, 1);

						// Play user complete sequence
					StartCoroutine(playUserWonAnimation());
					checkAndPlayFillAnim(campaign);
					break;
				}

				mySpins.text = CommonText.formatNumber(campaign.userProgress);
				friendSpins.text = CommonText.formatNumber(campaign.buddyProgress);
			}
			else if (campaign.state == ChallengeCampaign.COMPLETE)
			{
				titleText.text = Localize.text ("challenge_completed");
				mainMeterFillSprite.fillAmount = 1f;
				friendFillSprite.fillAmount = 1f;
				userFillSprite.fillAmount = 1f;
				safeSetup(campaign);
				return;
			}
			else
			{
				// Set to Loading while we wait on data.
				mySpins.text = Localize.text("loading");
				friendSpins.text = Localize.text ("loading");
				CampaignDirector.partner.addFunctionToOnGetProgress(onGetProgressUpdate);
				// Only update if we aren't forcing an animation.
				PartnerPowerupAction.getProgress();
				// on response, do the rest...
			}

			timeRemaining = campaign.timerRange;
			timeRemaining.registerLabel(timeLeft);
			timeRemaining.registerLabel(timeLeftShadow);

			string formattedIndividualProgress = CommonText.formatNumber(campaign.individualProgressRequired);

			userIndividualGoal.text = Localize.text("out_of_{0}", CommonText.formatNumber(campaign.individualProgressRequired));
			buddyIndividualGoal.text = Localize.text("out_of_{0}", CommonText.formatNumber(campaign.individualProgressRequired));
			individualWinGoalAmount.text = CommonText.formatNumber(campaign.individualProgressRequired);

			rewardAmount.text = CommonText.formatNumber(campaign.reward);
			amountNeeded.text = CommonText.formatNumber(campaign.challengeGoal);

			// If the campaign is running show the button. Otherwise...don't.
			nudgeButton.SetActive(campaign.timerRange.isActive);

			if (timeSinceLastNudge > campaign.pokeLimitTimeFrame || lastNudgedTimestamp == 0)
			{
				nudgeButton.registerEventDelegate(onNudgeBuddy);
				nudgeAnimation.Play(NUDGE_ENABLED);
				campaign.pokeTimeframe = new GameTimerRange(GameTimer.currentTime, GameTimer.currentTime + 1);
			}
			else
			{
				// Give it as many seconds as it wants.
				campaign.pokeTimeframe = new GameTimerRange(GameTimer.currentTime, GameTimer.currentTime + (campaign.pokeLimitTimeFrame - timeSinceLastNudge));
				nudgeAnimation.Play(NUDGE_DISABLED);
				campaign.pokeTimeframe.registerLabel(nudgeButtonTimerText);
			}

			campaign.addFunctionToOnStateChange(onCampaignStateChange);

		}
		else
		{
			safeSetup();
		}
	}

	private void safeSetup(PartnerPowerupCampaign campaign = null)
	{
		// Keep them lights on
		for (int i = 0; i < activatedLights.Length; i++)
		{
			activatedLights[i].setLights(true);
		}

		if (dialogArgs.containsKey(D.END_TIME))
		{
			int dateCompleted = (int)dialogArgs.getWithDefault(D.END_TIME, GameTimer.currentTime);
			messageLabel.text = Localize.text("completed_on_{0}", Common.convertTimestampToDatetime(dateCompleted).ToShortDateString());
		}
		else
		{
			messageLabel.text = Localize.text("congratulations_ppu_complete");
		}

		mySpins.text = Localize.textUpper("complete");
		friendSpins.text = Localize.textUpper("complete");

		endsInParent.SetActive(false);

		goalTypeText.gameObject.SetActive(false);

		long creditsReward = 0;
		if (dialogArgs.containsKey(D.DATA))
		{
			string statePassed = (string)dialogArgs.getWithDefault(D.DATA, "");

			if (statePassed == "PAST COMPLETE")
			{
				StartCoroutine(playChallengeCompleteState());
			}
		}
		if (dialogArgs.containsKey(D.AMOUNT))
		{
			creditsReward = (long)dialogArgs.getWithDefault(D.AMOUNT, 0);
		}
		else if (campaign != null)
		{
			creditsReward = campaign.reward;
		}

		if (dialogArgs.containsKey(D.SCORE))
		{
			mySpins.text = CommonText.formatNumber((long)dialogArgs.getWithDefault(D.SCORE, 0));
		}
		if (dialogArgs.containsKey(D.SCORE2))
		{
			friendSpins.text = CommonText.formatNumber((long)dialogArgs.getWithDefault(D.SCORE2, 0));
		}
		if (dialogArgs.containsKey(D.TOTAL_CREDITS))
		{
			userIndividualGoal.text = Localize.text("out_of_{0}", CommonText.formatNumber((long)dialogArgs.getWithDefault(D.TOTAL_CREDITS, 0)));
			buddyIndividualGoal.text = Localize.text("out_of_{0}", CommonText.formatNumber((long)dialogArgs.getWithDefault(D.TOTAL_CREDITS, 0)));
		}
		else
		{
			goalTypeSubText.gameObject.SetActive(false);
			userIndividualGoal.gameObject.SetActive(false);
			buddyIndividualGoal.gameObject.SetActive(false);
		}

		rewardAmount.text = CommonText.formatNumber(creditsReward);

		amountNeeded.text = Localize.textUpper("complete");
		nudgeButton.gameObject.SetActive(false);
	}

	private void setupUserImages(string startMode = "")
	{
		// Grab ZIDS out of dialog args
		// Setup profile pics
		string buddyZid = PartnerPowerupCampaign.buddyString;
		string buddyFBID = PartnerPowerupCampaign.buddyFBID;
		SocialMember buddy;

		// If we want to use a different buddy
		if (dialogArgs.containsKey(D.PLAYER))
		{
			buddyZid = (string)dialogArgs.getWithDefault(D.PLAYER, "");
		}

		if (dialogArgs.containsKey(D.FACEBOOK_ID))
		{
			// Normally we have to make sure this isn't 0, but we should do that everywhere we pass it.
			buddyFBID = (string)dialogArgs.getWithDefault(D.PLAYER, "-1");
		}

		Userflows.addExtraFieldToFlow(userflowKey, "partner_zid", buddyZid);
		
		buddy = CommonSocial.findOrCreate(buddyFBID, buddyZid);

		//If we see that we're using the anonomous name then try to use the first name provided by the server if its available
		PartnerPowerupCampaign campaign = CampaignDirector.partner;
		if ((buddy.firstName == "" || buddy.firstName == buddy.anonymousNonFriendName) && 
			campaign != null && 
			campaign.buddyFirstName != "" && 
			campaign.buddyFirstName != Localize.text("partner"))
		{
			buddy.firstName = CampaignDirector.partner.buddyFirstName;
		}

		if (buddyInfo != null)
		{
			buddyInfo.member = buddy as SocialMember;
		}

		SocialMember user = SlotsPlayer.instance.socialMember;
		 
		if (user == null)
		{
			Debug.LogError("ParnterPowerupIntroDialog::setupUserImages - User was nulll. This shouldn't happen. Pic will look wrong");
		}

		if (myFacebookPic != null)
		{
			myFacebookPic.member = user;
		}

	}

	// Sets up all our variables we're going to need to do some animation and then plays whatever we need.
	public void checkAndPlayFillAnim(PartnerPowerupCampaign campaign)
	{
		// Set meter stuff
		float totalFillPercent = 0;
		float meterFillPercent = 0;
		float friendMeterFillPercent = 0;
		int maxLightToTurnOn = activatedLights.Length;

		// Use this to set what the meters for players are at since we animate them up
		long oldUserProgress;
		long oldBuddyProgress;

		// Save what state we want
		string userAnimationStateToUse = "";
		string buddyAnimationStateToUse = "";

		// Only do the animatons if we have to
		bool shouldContinueAnimation = false;

		if (campaign.userProgressSinceLastCheck > 0)
		{
			oldUserProgress = campaign.userProgress - campaign.userProgressSinceLastCheck;

			campaign.userProgressSinceLastCheck = 0;
			userAnimationStateToUse = HAMMER_HIT;
			shouldContinueAnimation = true;
		}
		else
		{
			oldUserProgress = campaign.userProgress;
			userAnimationStateToUse = HAMMER_IDLE;
		}

		if (campaign.buddyProgressSinceLastCheck > 0)
		{
			oldBuddyProgress = campaign.buddyProgress - campaign.buddyProgressSinceLastCheck;
			campaign.buddyProgressSinceLastCheck = 0;
			buddyAnimationStateToUse = HAMMER_HIT;
			shouldContinueAnimation = true;
		}
		else
		{
			oldBuddyProgress = campaign.buddyProgress;
			buddyAnimationStateToUse = HAMMER_IDLE;
		}

		// Avoid divide by 0 errors.
		if (campaign.userProgress + campaign.buddyProgress != 0)
		{
			meterFillPercent = (float)(oldUserProgress) / (float)campaign.individualProgressRequired;
			friendMeterFillPercent = (float)oldBuddyProgress / (float)campaign.individualProgressRequired;
			totalFillPercent = (float)(oldUserProgress + oldBuddyProgress) / (float)campaign.challengeGoal;
		}

		if (mainMeterFillSprite != null)
		{
			mainMeterFillSprite.fillAmount = totalFillPercent;
		}
		if (userFillSprite != null)
		{
			userFillSprite.fillAmount = meterFillPercent;
		}
		if (friendFillSprite != null)
		{
			friendFillSprite.fillAmount = friendMeterFillPercent;
		}

		for (int i = 0; i < maxLightToTurnOn; i++)
		{
			// how many lights out of the max lights as a percent for ez conversion.
			float lightFillPercent = (float)i/(float)maxLightToTurnOn;

			// If we're matching the green meter fill as best we can,
			if (lightFillPercent <= totalFillPercent)
			{
				activatedLights[i].setLights(true);
			}
			else
			{
				activatedLights[i].setLights(false);
			}
		}

		if (shouldContinueAnimation)
		{
			StartCoroutine(playHammerAnimationAfterDelay(campaign, userAnimationStateToUse, buddyAnimationStateToUse));
		}

	}

	private IEnumerator playHammerAnimationAfterDelay(PartnerPowerupCampaign campaign, string userAnimationState, string buddyAnimationState)
	{
		// How much fill we need and how fast we want to get it
		float fillIncrement = 0.002f;

		float mainMeterTargetFillAmount = (float)(campaign.userProgress + campaign.buddyProgress) / (float)campaign.challengeGoal;
		float userTargetFillAmount = (float)(campaign.userProgress) / (float)campaign.individualProgressRequired;
		float buddyTargetFillAmount = (float)(campaign.buddyProgress) / (float)campaign.individualProgressRequired;

		// This is all happening right when we open the dialog so... lets make sure the user sees it
		yield return new WaitForSeconds(2);

		// Play correct hammer state
		userHammer.Play(userAnimationState);
		buddyHammer.Play(buddyAnimationState);

		if (userAnimationState == HAMMER_HIT || buddyAnimationState == HAMMER_HIT)
		{
			Audio.play(HAMMER_SMASH);
		}

		yield return new WaitForSeconds(1.25f);

		StartCoroutine(fillToAmount(userTargetFillAmount, buddyTargetFillAmount, mainMeterTargetFillAmount));
	}

	public IEnumerator playChallengeCompleteState()
	{
		collectButton.registerEventDelegate(onClickClose);
		for (int i = 0; i < activatedLights.Length; i++)
		{
			activatedLights[i].setLights(false);
		}

		// Set to Filled.
		yield return StartCoroutine(fillToAmount(1f, 1f, 1f));

		Audio.play(USER_WIN_FANFARE);

		// Play hammers as needed?
		// Do we want to play all the fills first too?
		onWinTitleFlash.Play(ON_STATE);
		Audio.play(TEXT_HIGHLIGHT_SOUND);

		onWinMeterAnimation.Play(TEAM_GOAL_WIN);

		yield return new WaitForSeconds(2.0f);

		Audio.play(REWARD_FANFARE);
		onWinAmountFlash.Play(REWARD_ANIMATION);

		yield return new WaitForSeconds(4.25f);

		onWinCollectAndShareAnimation.Play(INTRO_STATE);

		yield return null;

	}

	// We'll want the amounts for all the fill amounts up front. The end amount. Always set start amount first. 
	private IEnumerator fillToAmount(float playerMeterFill, float buddyMeterFill, float mainMeterFill)
	{
		// Get the current fill amounts. They should have been setup already.
		float userTargetFillIncrementor = userFillSprite.fillAmount;
		float buddyTargetFillIncrementor = friendFillSprite.fillAmount;
		float mainMeterTargerFillIncrementor = mainMeterFillSprite.fillAmount;

		// make const
		float fillIncrement = 0.005f;
		int lightTarget = Mathf.RoundToInt(activatedLights.Length * mainMeterFill);
		int newIncrement = 0;
		int lightTargetIncrementor = 0;

		PlayingAudio meterSound = Audio.play(GREEN_FILL_SOUND);

		// Fill em up.
		while (userTargetFillIncrementor < playerMeterFill || 
			buddyTargetFillIncrementor < buddyMeterFill ||
			mainMeterTargerFillIncrementor < mainMeterFill)
		{
			userTargetFillIncrementor += fillIncrement;
			buddyTargetFillIncrementor += fillIncrement;
			mainMeterTargerFillIncrementor += fillIncrement;

			newIncrement = Mathf.RoundToInt((mainMeterTargerFillIncrementor / mainMeterFill) * lightTarget);

			// Dont over fill, but wait for the other
			if (userTargetFillIncrementor > playerMeterFill)
			{
				userTargetFillIncrementor = playerMeterFill;
			}

			if (buddyTargetFillIncrementor > buddyMeterFill)
			{
				buddyTargetFillIncrementor = buddyMeterFill;
			}

			if (mainMeterTargerFillIncrementor > mainMeterFill)
			{
				//MetersFIllPP stop
				if (meterSound != null)
				{
					meterSound.stop(0);
				}
				mainMeterTargerFillIncrementor = mainMeterFill;
			}

			if (lightTargetIncrementor > lightTarget)
			{
				newIncrement = lightTarget;
				lightTargetIncrementor = lightTarget;
			}

			mainMeterFillSprite.fillAmount = mainMeterTargerFillIncrementor;
			friendFillSprite.fillAmount = buddyTargetFillIncrementor;
			userFillSprite.fillAmount = userTargetFillIncrementor;

			if (lightTargetIncrementor < activatedLights.Length)
			{
				activatedLights[lightTargetIncrementor].setLights(true);
			}

			if (newIncrement != lightTargetIncrementor)
			{
				// The audio naming is kind of screwy, so I gotta do this for now.
				string formattedNumber = newIncrement < 10 ? "0" + newIncrement : newIncrement.ToString();

				string audioToPlay = string.Format(lightOnAudio, formattedNumber);
				Audio.play(audioToPlay);
				lightTargetIncrementor = newIncrement;
			}

			// next frame plz
			yield return null;
		}
	}

	public IEnumerator playUserWonAnimation()
	{
		// Wait to start so we know we loaded
		yield return new WaitForSeconds(1.0f);

		Audio.play(USER_WIN_FANFARE);
		userWinProfileAnimation.Play(ON_STATE);

		userWinMeterAnimation.Play(INTRO_STATE);

		yield return new WaitForSeconds(2.0f);

		userWinMeterAnimation.Play(OUTRO_STATE);

		yield return new WaitForSeconds(1.0f);

		userWinMeterAnimation.Play(TITLE_HIGHLIGHT);
		Audio.play(TEXT_HIGHLIGHT_SOUND);

		yield return new WaitForSeconds(2.0f);

		userWinMeterAnimation.gameObject.SetActive(false);

		yield return null;
	}

	// play shuffle animation for a moment.
	public IEnumerator playIntroAnimation()
	{
		buddyShuffleAnimation.Play(INTRO_SHUFFLE_STATE);

		Audio.play(PICK_LOOP);

		yield return new WaitForSeconds(3.5f);

		Audio.play(PICK_END);

		buddyShuffleAnimation.Play(DEFAULT_STATE);
	}

	public void onNudgeBuddy(Dict args = null)
	{
		PlayerPrefsCache.SetInt(Prefs.LAST_NUDGED_TIME, GameTimer.currentTime);

		Audio.play(NUDGE_SOUND);

		StatsManager.Instance.LogCount(counterName: "dialog", kingdom: statsKingdom, family: "nudge", genus: "click");

		StartCoroutine(playNudgeThenGreyOut());

		// Check to see if we can send notif, and if so, send it. At this time we'll also want to send both the A2U And the PN
		NotificationAction.sendPartnerPowerupNotif();
		PartnerPowerupAction.pokeUser();

		// Not sure when or how this gets reset..
		CampaignDirector.partner.pokeTimeframe = new GameTimerRange(GameTimer.currentTime, GameTimer.currentTime + CampaignDirector.partner.pokeLimitTimeFrame);
		CampaignDirector.partner.pokeTimeframe.registerFunction(onCanPokeAgain);
		CampaignDirector.partner.pokeTimeframe.registerLabel(nudgeButtonTimerText);

		nudgeButton.enabled = false;
	}

	private void onCanPokeAgain(Dict args = null, GameTimerRange sender = null)
	{
		nudgeButton.enabled = true;
		nudgeAnimation.Play(NUDGE_ENABLED);
	}

	public IEnumerator playNudgeThenGreyOut()
	{
		nudgeAnimation.Play(NUDGE_PLAY);

		yield return new WaitForSeconds(2.5f);

		nudgeAnimation.Play(NUDGE_DISABLED);

	}

	public void onCampaignStateChange(Dict data = null)
	{
		PartnerPowerupCampaign campaign = CampaignDirector.partner;

		switch (campaign.state)
		{
		// Complete! Play that animation
		case PartnerPowerupCampaign.COMPLETE:
			statsKingdom = "co_op_challenge_complete";
			campaign.userProgress = campaign.individualProgressRequired;
			campaign.buddyProgress = campaign.individualProgressRequired;

			mySpins.text = CommonText.formatNumber(campaign.userProgress);
			friendSpins.text = CommonText.formatNumber(campaign.buddyProgress);

			StartCoroutine(playChallengeCompleteState());
			break;

			// Make sure we close this, since we'll be showing the incomplete dialog.
		case PartnerPowerupCampaign.INCOMPLETE:
			Dialog.close();
			break;

			// How the hell did we get here
		case PartnerPowerupCampaign.IN_PROGRESS:
			Debug.LogError("PartnerPowerupIntroDialog::onCampaignStateChange - Campaign is in progress. That shouldn't happen here.");
			break;

		default:
			Dialog.close();
			Debug.LogError("PartnerPowerupIntroDialog::onCampaignStateChange - No idea what to do with state " + campaign.state + " upon completion");
			break;

		}
	}

	private void onGetProgressUpdate(Dict args = null)
	{
		PartnerPowerupCampaign campaign = CampaignDirector.partner;
		mySpins.text = CommonText.formatNumber(campaign.userProgress);
		friendSpins.text = CommonText.formatNumber(campaign.buddyProgress);
		checkAndPlayFillAnim(campaign);

		CampaignDirector.partner.removeFunctionOnGetProgress(onGetProgressUpdate);
	}

	public override void close()
	{
		if (statsKingdom == "co_op_challenge_motd")
		{
			StatsManager.Instance.LogCount(counterName: "dialog", kingdom: statsKingdom, family: "play_now",
				genus: "click");
		}

		if (statsKingdom == "co_op_challenge_complete")
		{
			StatsManager.Instance.LogCount(counterName: "dialog", kingdom: statsKingdom, family: "collect",
				genus: "click");
		}

		// While this should never happen,
		if (CampaignDirector.partner != null)
		{
			CampaignDirector.partner.removeFunctionOnGetProgress(onGetProgressUpdate);

			if (CampaignDirector.partner.pokeTimeframe != null)
			{
				CampaignDirector.partner.pokeTimeframe.removeFunction(onCanPokeAgain);
			}
		}

		Audio.play(CLOSE_SOUND);

		// Always switch the music back to whatever/wherever we were in case we played some of those sweet subscriber tunes
		if (GameState.isMainLobby)
		{
			MainLobby.playLobbyMusic();
		}
		else if (SlotBaseGame.instance != null)
		{
			SlotBaseGame.instance.playBgMusic();
		}

		if (!string.IsNullOrEmpty(eventID))
		{
			// Send server the recepit for coins and apply coin value or whatever the heck we won
			PartnerPowerupAction.completeCoOp(eventID);
		}

		timeRemaining = null;
	}

	public void onClickClose(Dict args = null)
	{
		if (CampaignDirector.partner != null)
		{
			CampaignDirector.partner.removeFunctionOnStateChange(onCampaignStateChange);
		}

		Dialog.close();
	}

	void Update()
	{
		AndroidUtil.checkBackButton(onClickClose);
	}

	// Might technically be motd
	public static void showDialog(Dict args = null)
	{
		Scheduler.addDialog("partner_power_intro", args);
	}

}
