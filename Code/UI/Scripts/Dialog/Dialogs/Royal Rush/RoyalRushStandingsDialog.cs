using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

public class RoyalRushStandingsDialog : DialogBase
{
	public Animator dialogAnimator;
	public TextMeshPro timeRemaining;
	public TextMeshPro timeRemaingInfoSection;
	public TextMeshPro firstPlaceAwardText;
	public TextMeshPro secondPlaceAwardText;
	public TextMeshPro thirdPlaceAwardText;
	public Renderer gameRenderer;

	public ButtonHandler playNowButton;
	public ButtonHandler backButton;
	public GameObject backArrowSprite;
	public ButtonHandler closeButton;

	public RoyalRushLeaderboardEntry royalRuler;

	// We use this to make tabs
	public GameObject rankingTab;
	public GameObject userRankingTab;
	public GameObject comeBackSoonText;

	// What we attach the tabs to.
	public GameObject rankingAreaSlider;
	public GameObject maskingCenterPoint;

	public GameObject sprintEndedObject;
	public GameObject animationParent;
	public GameObject[] objectsToTurnOff;

	public SlideContent slideContent;
	public SlideController slideController;

	public TextMeshPro[] payoutRanks;
	public TextMeshPro[] prizeAmounts;
	public TextMeshPro howToPlayAwardText;

	public TextMeshPro eventAwardsText;
	public TextMeshPro standingsText;
	public UISprite backgroundSprite;
	public TextMeshProMasker masker;
	public RoyalRushInfo infoToUse { get; private set; }

	private const string EVENT_END_ANIMATION = "Features/Royal Rush/Prefabs/Royal Rush Results";

	private RoyalRushLeaderboardEntry userTab;

	private bool isSprintSummaryDialog = false;
	private string sprintSummaryState = null;
	private int tabLocation = 0;
	private GameTimerRange timeUntilCutoff;

	// Used so we can position things niceley in the scroll area.
	private const int INFO_INDEX_OFFSET = 1;
	private const int TAB_HEIGHT = 155;
	private const int BOUND_INCREMENT = 155;
	private const int TAB_STARTING_X = 0;
	private const int TAB_STARTING_Y = 70;
	private const float MINIMUM_VISIBLE_TABS = 3.5f;
	private const float SPRINT_SUMMARY_KILL_TIME = 0.6f; //How long to wait before turning off the sprint summary intro
	private const string BLUE_BACKGROUND_NAME = "Background Tile Crowns";
	
	#region SETUP
	public override void init()
	{
		// Register the close button quick so we can always get out
		backButton.registerEventDelegate(onClickInfoButton);
		closeButton.registerEventDelegate(onClickClose);

		playNowButton.gameObject.SetActive(true);
		comeBackSoonText.SetActive(false);

		timeRemaingInfoSection.text = CommonText.secondsFormatted(RoyalRushEvent.initialSprintTime);

		if (dialogArgs != null)
		{
			if (dialogArgs.containsKey(D.DATA))
			{
				infoToUse = dialogArgs[D.DATA] as RoyalRushInfo;
			}

			if (dialogArgs.containsKey(D.OPTION))
			{
				sprintSummaryState = (string)dialogArgs[D.OPTION];
			}
		}

		if (infoToUse == null && GameState.game != null)
		{
			// Try to get the info. We handle if it's null later on
			infoToUse = RoyalRushEvent.instance.getInfoByKey(GameState.game.keyName);
		}

		setPlayNowButtonState();

		// If this state exists then we need to play the summary animations
		if (!sprintSummaryState.IsNullOrWhiteSpace() && infoToUse != null) 
		{
			if (sprintSummaryState == "Event Over")
			{
				backgroundSprite.spriteName = BLUE_BACKGROUND_NAME;
				backgroundSprite.gameObject.SetActive(false);
				backgroundSprite.gameObject.SetActive(true);
				eventAwardsText.text = Localize.text("contest_ended");
				standingsText.text = Localize.text("final_results");
				AssetBundleManager.load(this, EVENT_END_ANIMATION, onLoadFeatureEndAnimation, objectLoadFailure);
				System.DateTime endDate = Common.convertTimestampToDatetime((int)infoToUse.endTime);
				timeRemaining.text = endDate.Month + "/" + endDate.Day + "/" + endDate.Year;
			}
			else
			{
				isSprintSummaryDialog = true;
				StartCoroutine(playSprintEndedIntro(sprintSummaryState));
			}
		}
		else
		{
			// Only play this if we're not playing some sort of forced flow
			dialogAnimator.Play("Dialog Intro");
		}

		if (infoToUse != null)
		{
			if (sprintSummaryState != "Event Over")
			{
				if (infoToUse.rushFeatureTimer != null)
				{
					timeUntilCutoff = new GameTimerRange(GameTimer.currentTime, GameTimer.currentTime + (infoToUse.rushFeatureTimer.timeRemaining - RoyalRushEvent.minTimeRequired));
					// If we happen to run out of time in the dialog let the user know
					if (timeUntilCutoff.isExpired)
					{
						onFeatureTimeout();
					}
					else
					{
						timeUntilCutoff.registerFunction(onFeatureTimeout);
						timeUntilCutoff.registerLabel(timeRemaining);
					}
				}
			}

			// Load the game to the renderer
			if (!infoToUse.gameKey.IsNullOrWhiteSpace())
			{
				LobbyGame gameToUse = LobbyGame.find(infoToUse.gameKey);
				if (gameToUse != null)
				{
					string imagePath = SlotResourceMap.getLobbyImagePath(gameToUse.groupInfo.keyName, gameToUse.keyName);
					RoutineRunner.instance.StartCoroutine(DisplayAsset.loadTextureFromBundle(imagePath, imageTextureLoaded,skipBundleMapping:true,pathExtension:".png"));
				}
			}

			if (infoToUse.prizeList != null && infoToUse.prizeList.Count > 0)
			{
				howToPlayAwardText.text = CreditsEconomy.convertCredits(infoToUse.prizeList[0].creditsAwardAmount);

				for (int i = 0; i < infoToUse.prizeList.Count; i++)
				{
					if (infoToUse.prizeList[i].rankMin == infoToUse.prizeList[i].rankMax)
					{
						payoutRanks[i].text = CommonText.formatContestPlacement(infoToUse.prizeList[i].rankMin);
					}
					else
					{
						payoutRanks[i].text = CommonText.formatContestPlacement(infoToUse.prizeList[i].rankMin) + " - " + CommonText.formatContestPlacement(infoToUse.prizeList[i].rankMax);
					}

					prizeAmounts[i].text = CreditsEconomy.convertCredits(infoToUse.prizeList[i].creditsAwardAmount);
				}
			}
			else
			{
				Debug.LogError("RoyalRushStandingsDialog::No Prize list");
			}

			if (infoToUse.userInfos != null && infoToUse.userInfos.Count > 0)
			{
				// setup first place
				RoyalRushUser user = infoToUse.userInfos[0];
				royalRuler.user.member = user.member;
				royalRuler.score.text = CreditsEconomy.convertCredits(infoToUse.userInfos[0].score);

				// The bottom bound should be 0 so we can't scroll if we're not going to have anything hanging off screen here.
				slideController.bottomBound = 0;
				slideController.topBound = Mathf.Max((-MINIMUM_VISIBLE_TABS * TAB_HEIGHT) + (TAB_HEIGHT * infoToUse.userInfos.Count - 1), 0);//

				slideController.setBounds(slideController.topBound, slideController.bottomBound);
				slideController.scrollBar.gameObject.SetActive(slideController.topBound != 0); // If we have nowhere to scroll don't bother showing it

				RoyalRushUser userToDisplay = null;
				// Setup all the others
				for (int i = 1; i < infoToUse.userInfos.Count; i++)
				{
					GameObject newRankingTab;

					// If this user is us, use the blue tab
					if (infoToUse.userInfos[i].zid == SlotsPlayer.instance.socialMember.zId)
					{
						newRankingTab = NGUITools.AddChild(rankingAreaSlider, userRankingTab);
					}
					else
					{
						newRankingTab = NGUITools.AddChild(rankingAreaSlider, rankingTab);
					}

					CommonTransform.setX(newRankingTab.gameObject.transform, TAB_STARTING_X);
					CommonTransform.setY(newRankingTab.gameObject.transform, TAB_STARTING_Y + (i * -TAB_HEIGHT));

					// Grab the script on that object
					RoyalRushLeaderboardEntry reference = newRankingTab.GetComponent<RoyalRushLeaderboardEntry>();

					if (reference != null)
					{
						userToDisplay = infoToUse.userInfos[i];
						reference.user.member = userToDisplay.member;

						reference.score.text = CreditsEconomy.convertCredits(infoToUse.userInfos[i].score);
						reference.rank.text = CommonText.formatNumber(infoToUse.userInfos[i].position + 1);

						masker.addObjectToList(reference.score);
						masker.addObjectToList(reference.rank);
						masker.addObjectToList(reference.user.nameTMPro);

						if (infoToUse.userInfos[i].zid == SlotsPlayer.instance.socialMember.zId)
						{
							if (string.IsNullOrEmpty(sprintSummaryState))
							{
								slideController.onEndAnimation += onFinishSlidingToUser;
								slideController.scrollToVerticalPosition((i - (INFO_INDEX_OFFSET + 1)) * TAB_HEIGHT, 28);
							}
							else
							{
								// So we can scroll to it after the animation
								tabLocation = i;
							}

							userTab = reference;
							userTab.gameObject.SetActive(false);

							if (slideController.topBound <= 0)
							{
								onFinishSlidingToUser();
							}
						}
					}
				}
			}
			else
			{
				setupWithNoUsers();
			}
		}
		else
		{
			setupWithNoUsers();
			Debug.LogError("RoyalRushStandingsDialog::Init - The rush info we had was null");
		}

	}

	private void setupWithNoUsers()
	{
		royalRuler.gameObject.SetActive(false);
	}

	private void onFeatureTimeout(Dict args = null, GameTimerRange parent = null)
	{
		playNowButton.gameObject.SetActive(false);

		comeBackSoonText.SetActive(true);

		timeRemaining.text = Localize.text("prizes_soon");
	}

	private void setPlayNowButtonState()
	{
		playNowButton.clearDelegate();

		if (!string.IsNullOrEmpty(sprintSummaryState))
		{
			if (sprintSummaryState == "Event Over")
			{
				playNowButton.text = Localize.toUpper("ok");
				playNowButton.registerEventDelegate(onClickClose);
			}
			else
			{
				// Replay is play again in SCAT
				playNowButton.text = Localize.text("replay");
				playNowButton.registerEventDelegate(onClickClose);
			}
			return;
		}

		if (GameState.isMainLobby)
		{
			playNowButton.text = Localize.text("play_now");
			playNowButton.registerEventDelegate(onClickPlayNow);
		}
		else
		{
			playNowButton.text = Localize.text("continue");
			playNowButton.registerEventDelegate(onClickClose);
		}
	}
	#endregion

	#region OVERRIDES
	protected override void onFadeInComplete()
	{
		base.onFadeInComplete();

		if (string.IsNullOrEmpty(sprintSummaryState))
		{
			Audio.playWithDelay("LeaderBoardShowCrownRRush01", 1.25f);
		}
	}

	public override void close()
	{
		if (Overlay.instance.jackpotMystery != null && 
			Overlay.instance.jackpotMystery.tokenBar != null && 
			Overlay.instance.jackpotMystery.tokenBar as RoyalRushCollectionModule != null)
		{
			RoyalRushCollectionModule currentRoyalRushMeter = Overlay.instance.jackpotMystery.tokenBar as RoyalRushCollectionModule;
			if (currentRoyalRushMeter != null && SlotBaseGame.instance != null && SlotBaseGame.instance.isRoyalRushGame)
			{
				if (isSprintSummaryDialog)
				{
					if (infoToUse.inWithinRegistrationTime())
					{
						RoutineRunner.instance.StartCoroutine(currentRoyalRushMeter.playSprintStartAnimations());
					}
					currentRoyalRushMeter.resetOverlayUI();
				}
				else if (!sprintSummaryState.IsNullOrWhiteSpace() && sprintSummaryState == "Event Over") 
				{
					currentRoyalRushMeter.updateInfoAfterCompetetionEnd(infoToUse.gameKey);
				}
			}
		}

		if (timeUntilCutoff != null)
		{
			timeUntilCutoff.clearEvent();
			timeUntilCutoff.clearLabels();
		}
	}
	#endregion

	#region ANIMATION
	// Turns off objects so we don't block the animation
	public void toggleAnimaionObjects(bool newSetting)
	{
		for (int i = 0; i < objectsToTurnOff.Length; i++)
		{
			objectsToTurnOff[i].SetActive(newSetting);
		}

		// This means we were turned on again and should play the crown sound.
		if (newSetting == true)
		{
			slideController.onEndAnimation += onFinishSlidingToUser;
			slideController.scrollToVerticalPosition((tabLocation - INFO_INDEX_OFFSET) * TAB_HEIGHT, 28);
			Audio.playWithDelay("LeaderBoardShowCrownRRush01", 1.25f);
		}
	}

	private IEnumerator playSprintEndedIntro(string state)
	{
		int tabIndex = 0; //Leaderboard shows up to 5 entries so this will be something 0-4
		int currentRank = infoToUse.competitionRank;
		int numberOfparticipants = infoToUse.userInfos.Count;
		if (currentRank == 0)
		{
			tabIndex = 0;
		}
		else if(numberOfparticipants <= 5)
		{
			tabIndex = currentRank;
		}
		else //Have a list that is actually scrollable
		{
			if (currentRank + 1 == numberOfparticipants) //Last place
			{
				tabIndex = 4;
			}
			else if (currentRank + 2 == numberOfparticipants) //2nd to last
			{
				tabIndex = 3;
			}
			else if (currentRank + 3 == numberOfparticipants) //3rd to last
			{
				tabIndex = 2;
			}
			else
			{
				tabIndex = 1; //If we're not 2 places within last place then we're always going to center the user's tab onto the 2nd position in the leaderboard
			}
		}
		toggleAnimaionObjects(false);
		GameObject sprintEndedObjectInstance = NGUITools.AddChild(animationParent, sprintEndedObject);
		RoyalRushSprintSummary sprintSummary = sprintEndedObjectInstance.GetComponent<RoyalRushSprintSummary>();
		yield return StartCoroutine(sprintSummary.playIntroAnimations(state, type.getAnimInTime(), infoToUse, tabIndex));
		toggleAnimaionObjects(true);
		yield return new WaitForSeconds(SPRINT_SUMMARY_KILL_TIME);
		sprintEndedObjectInstance.SetActive(false);
	}

	private IEnumerator playFeatureEnded(GameObject animationObject)
	{
		toggleAnimaionObjects(false);

		RoyalRushEventSummary sprintSummary = animationObject.GetComponent<RoyalRushEventSummary>();

		yield return StartCoroutine(sprintSummary.playFinalResultsIntro(this, infoToUse.finalRank));
	}
	#endregion

	#region CALLBACKS
	// Specific to the game renderer
	private void imageTextureLoaded(Texture2D tex, Dict data = null)
	{
		if (gameRenderer != null && tex != null)
		{
			// Making sure we have the right shader
			gameRenderer.material.shader = LobbyOptionButtonActive.getOptionShader();
			gameRenderer.material.mainTexture = tex;
			gameRenderer.material.color = Color.white;
		}
		else if (gameRenderer == null)
		{
			if (!dialogBaseHasBeenClosed)
			{
				// if dialog has not been closed it's an error we should look into
				Debug.LogError("RoyalRushStandingsDialog: gameRenderer was null. dialogBaseHasBeenClosed = " + dialogBaseHasBeenClosed);
			}
		}
		else
		{
			Debug.LogError("RoyalRushStandingsDialog::imageTextureLoaded - downloaded texture was null!");
		}
	}

	public void onClickClose(Dict args = null)
	{
		Audio.play("XoutEscape");
		Dialog.close();
		StatsManager.Instance.LogCount("dialog", "royal_rush_standings", family: "close", genus: "click");
	}

	public void onClickPlayNow(Dict args = null)
	{
		Audio.play("menuselect0");
		StatsManager.Instance.LogCount("dialog", "royal_rush_standings", family: "play_now", genus: "click");
		
		if (infoToUse != null && !string.IsNullOrWhiteSpace(infoToUse.gameKey))
		{
			//Load the royal rush game if we're not already in it and we sucessfully find the game to load
			if (GameState.game == null || GameState.game.keyName != infoToUse.gameKey)
			{
				LobbyGame rrGame = LobbyGame.find(infoToUse.gameKey);
				if (rrGame != null && rrGame.isRoyalRush)
				{
					rrGame.askInitialBetOrTryLaunch(false, true);
				}
			}
		}
		Dialog.close();
	}

	// When the user clicks the top left buton
	public void onClickInfoButton(Dict args = null)
	{
		Audio.play("menuselect0");
		dialogAnimator.Play("Dialog To Info");
		backButton.unregisterEventDelegate(onClickInfoButton);
		backButton.registerEventDelegate(onClickBackButton);
		backButton.text = Localize.text("back");
		backArrowSprite.SetActive(true);
	}

	public void onClickBackButton(Dict args = null)
	{
		Audio.play("menuselect0");
		dialogAnimator.Play("Info To Dialog");
		backButton.unregisterEventDelegate(onClickBackButton);
		backButton.registerEventDelegate(onClickInfoButton);
		backButton.text = Localize.text("how_to_play");
		backArrowSprite.SetActive(false);
	}

	private void onLoadFeatureEndAnimation(string assetPath, Object obj, Dict data = null)
	{
		GameObject sprintEndedObjectInstance = NGUITools.AddChild(animationParent, obj as GameObject);

		StartCoroutine(playFeatureEnded(sprintEndedObjectInstance));
	}

	private void objectLoadFailure(string assetPath, Dict data = null)
	{
		Debug.LogError("PartnerPowerupCampaign::partnerPowerIconLoadFailure - Failed to load asset at: " + assetPath);
	}

	// After we do the forced slide to our position.
	private void onFinishSlidingToUser(Dict args = null)
	{
		// scroll to user tab. Just add momentum till we are there?
		if (userTab != null)
		{
			userTab.gameObject.SetActive(true);
			userTab.popInAnimator.Play(RoyalRushLeaderboardEntry.PLAYER_RANK_ANIMATION);

			// For the animation to when we place a user on the board
			Audio.play("LeaderBoardEntryWipeRRush01");
			Audio.playWithDelay("LeaderBoardEntryWipeRRush01", .80f);
		}
	}
	#endregion

	#region STANDARD_DIALOG_FUNCTIONS
	void Update()
	{
		AndroidUtil.checkBackButton(checkBackButton);
	}

	// Might technically be motd
	public static void showDialog(Dict args = null)
	{
		Scheduler.addDialog("royal_rush_standings_dialog", args);
	}

	private void checkBackButton()
	{
		Audio.play("XoutEscape");
		Dialog.close();
	}
	#endregion
}
