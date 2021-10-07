using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;
using Com.States;

public class WeeklyRaceLeaderboard : DialogBase
{
	// =============================
	// PRIVATE
	// =============================
	[SerializeField] private StateImageButtonHandler leaderboardHandler;
	[SerializeField] private StateImageButtonHandler friendsHandler;
	[SerializeField] private StateImageButtonHandler divisionsHandler;

	// sub dialogs
	[SerializeField] private WeeklyRaceZoneInfo weeklyRaceZoneInfo;
	[SerializeField] private WeeklyRacePrizes weeklyRacePrizes;

	// race ended prefab that appears when results are pending
	[SerializeField] private GameObject raceEndedAsset;
	[SerializeField] private TextMeshPro raceEndedText;

	// slidecontroller prefabs needed
	[SerializeField] private GameObject playerListItem;
	[SerializeField] private GameObject playerZoneListItem;
	[SerializeField] private GameObject divisionListItem;
	[SerializeField] private GameObject inflationFactorDisclaimerItem;
	[SerializeField] private GameObject itemsParent;

	// buttons
	[SerializeField] private ButtonHandler infoButton;
	[SerializeField] private ClickHandler dailyRivalsInfoHandler;

	// misc leaderboard assets
	[SerializeField] private WeeklyRaceInfoSection normalInfoSection;
	[SerializeField] private WeeklyRaceInfoSection rivalInfoSection;
	[SerializeField] private TextMeshPro timerText;
	[SerializeField] private GameObject topBarObject;
	[SerializeField] private GameObject chestSparkles;
	[SerializeField] private Animator freeBonusWheelAnim;

	// label materials
	[SerializeField] private Material beginnerMaterial;
	[SerializeField] private Material rookieMaterial;
	[SerializeField] private Material professionalMaterial;
	[SerializeField] private Material masterMaterial;
	[SerializeField] private Material grandMasterMaterial;
	[SerializeField] private Material championMaterial;
	[SerializeField] private Material grandChampionMaterial;
	[SerializeField] private Material selectedTabMaterial;
	[SerializeField] private Material unselectedTabMaterial;

	private WeeklyRace race; 						// weekly race instance to display data from
	private string currentTab = ""; 				// current tab selected in the leaderboard
	private GameObject usersRankObject = null;
	private GameObject rivalsRankObject = null;
	private WeeklyRaceInfoSection infoSection; 		// info section on the left side of the leaderboard
	private List<WeeklyRaceRacer> racers = null;
	private List<GameObject> friendObjects;
	private List<KeyValuePair<WeeklyRaceRacer, GameObject>> rankObjectPool;
	private List<KeyValuePair<WeeklyRaceRacer, GameObject>> rankObjects;
	private List<GameObject> divisionObjects;
	private GameObject promotionListItem;			// promotion zone list item (only ever 1 of these)
	private GameObject relegationListItem;			// relegation zone list item (only ever 1 of these)
	private GameObject inflationFooterItem;			// inflation factor footer item (only ever 1 of these)
	private StateMachine stateMachine;

	// =============================
	// PUBLIC
	// =============================
	public WeeklyRaceSlideController slideController;

	// =============================
	// CONST
	// =============================
	public const float PLAYER_ITEM_HEIGHT 		= 180f;		// height of the rank prefab
	public const float DIVISION_ITEM_HEIGHT 	= 336f;		// height of the division prefab
	public const float SLIDE_CONTENT_POS 		= 585f;		// starting position for slide content
	private const float ITEM_PADDING 			= 5f; 		// padding between each leaderboard item
	private const int MINIMUM_ITEM_COUNT		= 5;		// minimum number of items needed before scroll bounds are calculated
	private const string LEADERBOARD			= "Leaderboard";
	private const string DIVISIONS				= "Divisions";
	private const string FRIENDS				= "Friends";
	private const string COOLDOWN 				= "Cooldown";

	public override void init()
	{
		race = dialogArgs.getWithDefault(D.OBJECT, null) as WeeklyRace;

		friendObjects 		= new List<GameObject>();
		divisionObjects 	= new List<GameObject>();
		rankObjects 		= new List<KeyValuePair<WeeklyRaceRacer, GameObject>>();
		rankObjectPool 		= new List<KeyValuePair<WeeklyRaceRacer, GameObject>>();

		promotionListItem 	= NGUITools.AddChild(itemsParent, playerZoneListItem);
		relegationListItem 	= NGUITools.AddChild(itemsParent, playerZoneListItem);
		inflationFooterItem = NGUITools.AddChild(itemsParent, inflationFactorDisclaimerItem);

		promotionListItem.SetActive(false);
		relegationListItem.SetActive(false);
		inflationFooterItem.SetActive(false);

		stateMachine = new StateMachine("weekly_race_leaderboard");
		stateMachine.addState(State.READY);
		stateMachine.addState(State.IN_PROGRESS);
		stateMachine.addState(COOLDOWN, new StateOptions(null, null, setupCooldownText));
		stateMachine.addState(State.COMPLETE, new StateOptions(null, null, setupEndText));
		stateMachine.updateState(State.READY);

		string videoUrl = Data.liveData.getString("DAILY_RIVAL_FTUE_URL", "");
		dailyRivalsInfoHandler.gameObject.SetActive(!string.IsNullOrEmpty(videoUrl) && ExperimentWrapper.WeeklyRace.isDailyRivalsEnabled);

		if (race != null)
		{
			WeeklyRace.refreshLeaderBoardEvent += refreshLeaderboard;
			Audio.switchMusicKeyImmediate("FeatureBgWeeklyRace");

			setup();

			StatsWeeklyRace.handleRaceState(race);
		}
		else
		{
			Dialog.immediateClose(this);
		}
	}

	private void Update()
	{
		if (race != null && race.timeRemaining <= 0 && !raceEndedAsset.activeSelf)
		{
			setup();
		}
		else if (raceEndedAsset.activeSelf && !stateMachine.can(State.COMPLETE) && race.cooldownTimeRemaining <= 0)
		{
			stateMachine.updateState(State.COMPLETE);
		}
	}

	//Refresh leader board when the rank has changed
	public void refreshLeaderboard()
	{
		if (currentTab == LEADERBOARD && stateMachine.can(State.IN_PROGRESS))
		{
			setupLeaderboard();
		}
	}

	/*=========================================================================================
	BUTTON REGISTRATIONS
	=========================================================================================*/
	private void registerButtons()
	{
		divisionsHandler.registerEventDelegate(onClickDivisions);
		leaderboardHandler.registerEventDelegate(onClickLeaderboard);
		friendsHandler.registerEventDelegate(onClickFriends);
		infoButton.registerEventDelegate(onInfoClicked);
		dailyRivalsInfoHandler.registerEventDelegate(onClickDailyRivalInfo);
	}

	private void unregisterButtons()
	{
		divisionsHandler.unregisterEventDelegate(onClickDivisions);
		leaderboardHandler.unregisterEventDelegate(onClickLeaderboard);
		friendsHandler.unregisterEventDelegate(onClickFriends);
		infoButton.unregisterEventDelegate(onInfoClicked);
		dailyRivalsInfoHandler.unregisterEventDelegate(onClickDailyRivalInfo);
	}

	public void viewProfileClicked(Dict args = null)
	{
		NetworkProfileDialog.showDialog(SlotsPlayer.instance.socialMember);
	}

	public void viewRivalProfileClicked(Dict args = null)
	{
		if (race != null && race.rivalsRacerInstance != null)
		{
			NetworkProfileDialog.showDialog(race.rivalsRacerInstance.member);
		}
	}

	public void onClickLeaderboard(Dict args = null)
	{
		if (currentTab == LEADERBOARD)
		{
			return;
		}

		resetLeaderboardPosition();
		setupLeaderboard();
	}

	public void onClickFriends(Dict args = null)
	{
		slideController.removeAllPins();
		setupFriends();
	}

	public void onClickDivisions(Dict args = null)
	{
		slideController.removeAllPins();
		StatsWeeklyRace.logWeeklyRaceDivisions(race.division);
		weeklyRaceZoneInfo.SetActive(false);
		setupDivisions();
	}

	public void onInfoClicked(Dict args = null)
	{
		StatsWeeklyRace.logMotd(race.division, "click");
		WeeklyRaceMOTD.showDialog("", SchedulerPriority.PriorityType.IMMEDIATE);
	}

	public void onClickDailyRivalInfo(Dict args = null)
	{
		if (ExperimentWrapper.WeeklyRace.isDailyRivalsEnabled)
		{
			Dialog.close(this);
			DoSomething.now("daily_rival_ftue");
		}
	}

	public override void onCloseButtonClicked(Dict args = null)
	{
		Audio.play("Dialog1DismissWeeklyRace");
		Dialog.close(this);
	}

	/*=========================================================================================
	LEADERBOARD SETUP
	=========================================================================================*/
	/**
	 * Basic setup function, this bridges off the two major states of the dialog, which is essentially
	 * when a race is in progress, or is ending/ended
	 */
	public void setup()
	{
		registerButtons();

		if (race.timeRemaining > 0)
		{
			timerText.gameObject.SetActive(true);
			raceEndedAsset.SetActive(false);
			setupLeaderboard();
			timerText.text = "Ends In ";
			race.timer.registerLabel(timerText, GameTimerRange.TimeFormat.REMAINING, true);
			stateMachine.updateState(State.IN_PROGRESS);
		}
		else
		{
			clearLeaderboardObjects();
			rivalsRankObject = null;
			setupRival();
			setInfoSection();
			topBarObject.SetActive(false);
			timerText.text = "Race has ended";
			itemsParent.SetActive(false);
			raceEndedAsset.SetActive(true);
			slideController.gameObject.SetActive(false);
			race.timer.removeLabel(timerText);

			if (race.cooldownTimeRemaining > 0)
			{
				stateMachine.updateState(COOLDOWN);
			}
			else
			{
				stateMachine.updateState(State.COMPLETE);
			}
		}
	}

	/**
	 * Handles setup for daily rivals pinning
	 */
	private void setupRival()
	{
		slideController.removeAllPins();

		if (rivalsRankObject != null)
		{
			slideController.addPinnedItem(rivalsRankObject);
		}

		// We decided not to do this, but I thought it looked cool so I'm keeping it commented out ¯\_(ツ)_/¯
		/*if (usersRankObject != null)
		{
			slideController.addPinnedItem(usersRankObject);
		}*/

		slideController.updatePinning();
	}

	/**
	 * There's a small info section in the lower left quadrant of the leaderboard, this function
	 * puts that section into the correct state
	 */
	private void setInfoSection()
	{
		if (race.isRivalsActive)
		{
			infoSection = rivalInfoSection;
			SafeSet.gameObjectActive(rivalInfoSection.gameObject, true);
			SafeSet.gameObjectActive(normalInfoSection.gameObject, false);
		}
		else
		{
			infoSection = normalInfoSection;
			SafeSet.gameObjectActive(rivalInfoSection.gameObject, false);
			SafeSet.gameObjectActive(normalInfoSection.gameObject, true);
		}

		if (infoSection != null)
		{
			infoSection.setup(race, race.division, getMaterialForDivision(race.division));

			if (race.isRivalsActive && infoSection == rivalInfoSection)
			{
				infoSection.setupRival();
			}
		}
	}

	/**
	 * When the race has ended, and rewards are being processed this function handles the cooldown timer text
	 * that is to be displayed. It is a callback function from the state machine state for "COOLDOWN"
	 */
	private void setupCooldownText()
	{
		raceEndedText.text = "Results in ";
		race.cooldownTimer.registerLabel(raceEndedText, GameTimerRange.TimeFormat.REMAINING, true);
	}

	/**
	 * Callback function the statemachine state for "Complete", which is basically a situation where clients
	 * hands are tied waiting for results still
	 */
	private void setupEndText()
	{
		raceEndedText.text = "Calculating results...";
	}

	/**
	 * Actual display of the rankings/player list items that are the most relevant
	 */
	private void setupLeaderboard(Dict args = null)
	{
		if (this == null)
		{
			Bugsnag.LeaveBreadcrumb("Trying to refresh weekly race leaderboard after dialog has been closed");
			return;
		}

		if (currentTab == FRIENDS)
		{
			Audio.play("Dialog1NavigateLRWeeklyRace");
		}

		clearLeaderboardObjects(currentTab == LEADERBOARD);
		slideController.removeAllPins();

		currentTab = LEADERBOARD;
		leaderboardHandler.label.fontMaterial = selectedTabMaterial;
		friendsHandler.label.fontMaterial = unselectedTabMaterial;
		divisionsHandler.label.fontMaterial = unselectedTabMaterial;
		leaderboardHandler.GetComponent<UIStateImageButton>().SetSelected(true);
		divisionsHandler.GetComponent<UIStateImageButton>().SetSelected();
		friendsHandler.GetComponent<UIStateImageButton>().SetSelected();

		populateLeaderboard();
		setupRival();
		setInfoSection();
	}

	/**
	 * Setup for user's friend section of the leaderboard
	 */
	private void setupFriends(Dict args = null)
	{
		if (currentTab == FRIENDS)
		{
			return;
		}

		clearLeaderboardObjects();

		currentTab = FRIENDS;
		leaderboardHandler.label.fontMaterial = unselectedTabMaterial;
		friendsHandler.label.fontMaterial = selectedTabMaterial;
		divisionsHandler.label.fontMaterial = unselectedTabMaterial;
		leaderboardHandler.GetComponent<UIStateImageButton>().SetSelected();
		divisionsHandler.GetComponent<UIStateImageButton>().SetSelected();
		friendsHandler.GetComponent<UIStateImageButton>().SetSelected(true);

		populateFriendboard();

		Audio.play("Dialog1NavigateLRWeeklyRace");
	}

	/**
	 * Setup for the list of divisions
	 */
	private void setupDivisions(Dict args = null)
	{
		if (currentTab == DIVISIONS)
		{
			return;
		}

		clearLeaderboardObjects();

		currentTab = DIVISIONS;
		leaderboardHandler.label.fontMaterial = unselectedTabMaterial;
		friendsHandler.label.fontMaterial = unselectedTabMaterial;
		divisionsHandler.label.fontMaterial = selectedTabMaterial;
		leaderboardHandler.GetComponent<UIStateImageButton>().SetSelected();
		divisionsHandler.GetComponent<UIStateImageButton>().SetSelected(true);
		friendsHandler.GetComponent<UIStateImageButton>().SetSelected();

		populateDivisions();
	}

	/**
	 * Creates all the elements in the rankings section of the leaderboard, as well as positioning,
	 * promotion/relegation zone, slidecontroller bounds, etc.
	 */
	private void populateLeaderboard()
	{
		// make sure we have some racers
		racers = race.getRacersByRank;

		bool hasAddedPromo = false;
		bool hasAddedRelegation = false;

		float yPos = 0.0f;
		float totalSize = 0f;
		
		for (int i = 0; i < racers.Count; ++i)
		{			
			WeeklyRaceRacer racer = racers[i];

			// check if we need to display the drop zone item (appears above the racers)
			if (!hasAddedRelegation && race.isRankWithinRelegation(racer.competitionRank))
			{
				hasAddedRelegation = true;
				addZoneListItem(yPos, false);
				yPos -= WeeklyRaceZoneInfo.ZONE_ITEM_HEIGHT + ITEM_PADDING;
				totalSize += WeeklyRaceZoneInfo.ZONE_ITEM_HEIGHT + ITEM_PADDING;
			}

			// add the racer
			addPlayerListItem(racer, yPos);
			yPos -= PLAYER_ITEM_HEIGHT + ITEM_PADDING;
			totalSize += PLAYER_ITEM_HEIGHT + ITEM_PADDING;

			// check if we need to display the promotion zone item (appears below the racer)
			if (!hasAddedPromo && race.isRankWithinPromotion(racer.competitionRank))
			{
				// if it's the last racer in the list, or the last racer eligible for promotion, then add the promotion zone asset
				if (i == racers.Count - 1 || !race.isRankWithinPromotion(racers[i+1].competitionRank))
				{
					hasAddedPromo = true;
					addZoneListItem(yPos);
					yPos -= WeeklyRaceZoneInfo.ZONE_ITEM_HEIGHT + ITEM_PADDING;
					totalSize += WeeklyRaceZoneInfo.ZONE_ITEM_HEIGHT + ITEM_PADDING;
				}
			}
		}

		int itemCount = racers.Count;
		if (!race.isScoreInflated)
		{
			addInflationFactorDisclaimerListItem(yPos);
			totalSize += PLAYER_ITEM_HEIGHT + ITEM_PADDING;
			itemCount++;  // inflation factor footer should be counted
		}

		if (itemCount <= MINIMUM_ITEM_COUNT)
		{
			slideController.setBounds(0, 0);
			slideController.enabled = false;
			slideController.toggleScrollBar(false);
		}
		else
		{
			totalSize -= PLAYER_ITEM_HEIGHT * MINIMUM_ITEM_COUNT/2;
			slideController.setBounds(totalSize, SLIDE_CONTENT_POS);
			slideController.enabled = true;
			slideController.toggleScrollBar(true);
		}

		if (usersRankObject != null)
		{
			// navigate to the user's position when the dialog is first opened
			if (racers.Count > MINIMUM_ITEM_COUNT && stateMachine.can(State.READY))
			{
				slideController.safleySetYLocation(usersRankObject.transform.localPosition.y * -1f);
			}
			else if (racers.Count <= MINIMUM_ITEM_COUNT)
			{
				resetLeaderboardPosition();
			}
		}
	}

	/**
	 * Very similar to the populateLeaderboard() but just for the user's friends section
	 */
	private void populateFriendboard()
	{
		List<SocialMember> friends = race.getFriendsByDivision;
		float yPos = 0.0f;
		float totalSize = 0f;
		
		for (int i = 0; i < friends.Count; ++i)
		{
			addFriendListItem(friends[i], yPos, i);
			yPos -= PLAYER_ITEM_HEIGHT + ITEM_PADDING;
			totalSize += PLAYER_ITEM_HEIGHT + ITEM_PADDING;
		}

		if (friends.Count <= MINIMUM_ITEM_COUNT)
		{
			slideController.setBounds(0f, 0f);
			slideController.enabled = false;
		}
		else
		{
			totalSize -= PLAYER_ITEM_HEIGHT * MINIMUM_ITEM_COUNT/2;
			slideController.setBounds(totalSize, SLIDE_CONTENT_POS);
			slideController.enabled = true;
			slideController.toggleScrollBar(true);
		}

		resetLeaderboardPosition();

		StatsWeeklyRace.logViewFriends(race.division, friends.Count);
	}

	/**
	 * This functionality used to exist in a sub dialog, but not exists in the main dialog. It is the
	 * display of all the current divisions
	 */
	private void populateDivisions()
	{
		GameObject playersDivisionObject = null;

		float yPos = 0.0f;
		float totalSize = 0f;

		for (int i = race.currentNumberOfDivisions-1; i >= 0; --i)
		{
			addDivisionListItem(yPos, i);
			yPos -= DIVISION_ITEM_HEIGHT + ITEM_PADDING;
			totalSize += DIVISION_ITEM_HEIGHT + ITEM_PADDING;


			if (i == WeeklyRaceDirector.currentRace.division)
			{
				playersDivisionObject = divisionObjects[divisionObjects.Count - 1];
			}
		}

		totalSize -= DIVISION_ITEM_HEIGHT * 1.5f;
		slideController.setBounds(totalSize, SLIDE_CONTENT_POS);
		slideController.enabled = true;
		slideController.toggleScrollBar(true);

		if (playersDivisionObject != null)
		{
			slideController.safleySetYLocation(playersDivisionObject.transform.localPosition.y * -1f);
		}
	}

	private void resetLeaderboardPosition()
	{
		CommonTransform.setY(slideController.content.transform, SLIDE_CONTENT_POS);
	}

	/*=========================================================================================
	ADDING/REMOVING LEADERBOARD OBJECTS
	=========================================================================================*/
	private void addPlayerListItem(WeeklyRaceRacer racer, float position)
	{
		GameObject racerObject = createRankObject(racer);
		WeeklyRacePlayerListItem playerItem = racerObject.GetComponent<WeeklyRacePlayerListItem>();
		CommonTransform.setY(playerItem.transform, position);

		if (racer == race.playersRacerInstance)
		{
			usersRankObject = racerObject;
		}

		if (racer == race.rivalsRacerInstance)
		{
			rivalsRankObject = racerObject;
		}
		
		rankObjects.Add(new KeyValuePair<WeeklyRaceRacer, GameObject>(racer, racerObject));
	}

	private void addInflationFactorDisclaimerListItem(float position)
	{
		inflationFooterItem.SetActive(true);
		CommonTransform.setY(inflationFooterItem.transform, position);
	}

	private void addFriendListItem(SocialMember member, float position, int rank)
	{
		GameObject racerObject = NGUITools.AddChild(itemsParent, playerListItem);
		WeeklyRacePlayerListItem playerItem = racerObject.GetComponent<WeeklyRacePlayerListItem>();
		CommonTransform.setY(playerItem.transform, position);
		playerItem.setupFriend(member, rank);

		friendObjects.Add(racerObject);
	}

	private void addZoneListItem(float position, bool isPromotion = true)
	{
		GameObject item = isPromotion ? promotionListItem : relegationListItem;
		WeeklyRaceZoneListItem zoneItem = item.GetComponent<WeeklyRaceZoneListItem>();
		item.SetActive(true);
		
		CommonTransform.setY(item.transform, position);
		zoneItem.setup(this, isPromotion);
	}

	private void addDivisionListItem(float position, int d)
	{
		GameObject divisionObject = NGUITools.AddChild(itemsParent, divisionListItem);
		WeeklyRaceDivisionListItem divisionItem = divisionObject.GetComponent<WeeklyRaceDivisionListItem>();
		CommonTransform.setY(divisionItem.transform, position);

		divisionItem.setup(d, WeeklyRaceDirector.currentRace.getDailyBonusForDivision(d), d == WeeklyRaceDirector.currentRace.division);

		divisionObjects.Add(divisionObject);
	}

	private GameObject createRankObject(WeeklyRaceRacer racer)
	{
		GameObject obj = null;
		if (rankObjectPool.Count > 0)
		{
			obj = getRacerObjectFromPool(racer);

			if (obj != null)
			{
				obj.SetActive(true);
			}
		}

		if (obj == null)
		{
			obj = NGUITools.AddChild(itemsParent, playerListItem);
		}

		WeeklyRacePlayerListItem playerItem = obj.GetComponent<WeeklyRacePlayerListItem>();
		playerItem.setup(this, racer, race, slideController);

		return obj;
	}

	private void removeRankObjects(bool setObjectEnabled = false)
	{
		for (int i = 0; i < rankObjects.Count; ++i)
		{
			rankObjectPool.Add(rankObjects[i]);
			rankObjects[i].Value.SetActive(setObjectEnabled);
		}

		rankObjects.Clear();
	}

	private void removeDivisionObjects()
	{
		for (int i = 0; i < divisionObjects.Count; ++i)
		{
			Destroy(divisionObjects[i]);
		}

		divisionObjects.Clear();
	}

	private void removeFriendObjects()
	{
		for (int i = 0; i < friendObjects.Count; ++i)
		{
			Destroy(friendObjects[i]);
		}

		friendObjects.Clear();
	}

	private void removeZoneObjects(bool setRankObjectsEnabled = false)
	{
		// only turn these off if we specifically wanted them removed
		// there are instances where we keep them enabled, and the populateLeaderboard() should
		// handle that scenario
		if (!setRankObjectsEnabled)
		{
			promotionListItem.SetActive(false);
			relegationListItem.SetActive(false);
			inflationFooterItem.SetActive(false);
		}
	}

	private void clearLeaderboardObjects(bool setRankObjectsEnabled = false)
	{
		removeRankObjects(setRankObjectsEnabled);
		removeZoneObjects(setRankObjectsEnabled);
		removeDivisionObjects();
		removeFriendObjects();
	}

	/*=========================================================================================
	BUTTON HANDLING
	=========================================================================================*/
	/**
	 * Displays the zone info sub dialog
	 */
	public void showZoneInfo(bool showPromotionInfo = false)
	{
		slideController.enabled = false;
		weeklyRaceZoneInfo.SetActive(true);
		weeklyRaceZoneInfo.init(this);

		// setup call has to be made with either the promotion or relegation info to be shown
		weeklyRaceZoneInfo.setup(showPromotionInfo);
	}

	/**
	 * Displays when a user clicks a chest icon in the rankings section of the leaderboard
	 */
	public void showPrizes()
	{
		slideController.enabled = false;
		weeklyRacePrizes.SetActive(true);
		weeklyRacePrizes.init(this);
	}

	/**
	 * This is sort of pointless now, but it's a callback function for any sub dialogs we open (e.g. the zone info dialog)
	 */
	public void onSubDialogClosed()
	{
		slideController.enabled = true;

		//Enable chest sparkles
		if (raceEndedAsset.activeSelf) 
		{
			chestSparkles.SetActive(true);	
		}
		freeBonusWheelAnim.enabled = true;
		Audio.play("Dialog1NavigateOutWeeklyRace");
	}

	// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		WeeklyRace.refreshLeaderBoardEvent -= refreshLeaderboard;
		unregisterButtons();
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

	private GameObject getRacerObjectFromPool(WeeklyRaceRacer racer)
	{
		for (int i = 0; i < rankObjectPool.Count; ++i)
		{
			if (rankObjectPool[i].Key.id == racer.id)
			{
				GameObject obj = rankObjectPool[i].Value;
				rankObjectPool.RemoveAt(i);
				return obj;
			}
		}

		return null;
	}

	/*=========================================================================================
	STATIC
	=========================================================================================*/
	public static void showDialog(Dict args)
	{
		WeeklyRace race = args.getWithDefault(D.OBJECT, null) as WeeklyRace;

		WeeklyRaceDirector.getUpdatedRaceData();

		if (race != null && !Scheduler.hasTaskWith("weekly_race_leaderboard"))
		{
			Scheduler.addDialog("weekly_race_leaderboard", args);
		}
	}
}
