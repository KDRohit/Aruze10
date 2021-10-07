using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;
using Com.States;

/// <summary>
///   This class is very similar in a lot of ways to WeeklyRaceLeaderboard. It displays the final standings
///   final divisions, and a promotion dialog if the user was promoted. It uses a state machine to swap through
///   the different choreography. There's little comments throughout this class, as most of it is pretty well named
/// </summary>
public class WeeklyRaceResults : DialogBase
{
	// =============================
	// PRIVATE
	// =============================
	[SerializeField] private GameObject standings;
	[SerializeField] private GameObject divisions;
	[SerializeField] private GameObject intro;
	[SerializeField] private GameObject standingsIntro;
	[SerializeField] private Animator standingsAnimator;
	[SerializeField] private Animator divisionsAnimator;
	[SerializeField] private Animator promotionAnimator;
	[SerializeField] private UISprite divisionBadge;
	[SerializeField] private UISprite divisionLabel;
	[SerializeField] private TextMeshPro descriptionText;

	// promotion assets
	[SerializeField] private GameObject promotion;
	[SerializeField] private UISprite promotionDivisionBadge;
	[SerializeField] private UISprite promotionDivisionLabel;
	[SerializeField] private TextMeshPro promotionDivisionText;

	// slide controller / list items
	[SerializeField] private GameObject playerListItem;
	[SerializeField] private GameObject playerDivisionListItem;
	[SerializeField] private GameObject playerZoneListItem;
	[SerializeField] private GameObject inflationFactorDisclaimerItem;
	[SerializeField] private GameObject itemsParent;
	[SerializeField] private TextMeshProMasker listItemMasker;
	[SerializeField] private TextMeshProMasker divItemMasker;
	[SerializeField] private SlideController slideController;

	[SerializeField] private ButtonHandler continueButton;

	// label materials
	[SerializeField] private Material beginnerMaterial;
	[SerializeField] private Material rookieMaterial;
	[SerializeField] private Material professionalMaterial;
	[SerializeField] private Material masterMaterial;
	[SerializeField] private Material grandMasterMaterial;
	[SerializeField] private Material championMaterial;
	[SerializeField] private Material grandChampionMaterial;

	// =============================
	// PUBLIC
	// =============================
	public static SchedulerPackage package;

	private WeeklyRace race;
	private StateMachine stateMachine;
	private List<GameObject> rankObjects; // list of ranks/division list items
	private WeeklyRaceDivisionListItem playersDivisionObject; // the division list item that our player belongs to
	private WeeklyRaceDivisionListItem moveToObject; // the division list item we are animating towards when user is promoted/demoted
	private GameObject usersRankObject = null;


	// =============================
	// CONST
	// =============================
	// set to true to test the promotion sequences, this is acting MORE like a constant but I made it changeable from the dev panel
	public static bool LOCAL_TESTING_PROMOTION = false;
	public static bool LOCAL_TESTING_DEMOTION = false;

	private const string INTRO_ANIM = "Race Results Intro";

	private const string PROMO_INTRO_ANIM = "Rank Promotion Intro";
	private const string PROMO_OUTRO_ANIM = "Rank Promotion Outro";

	private const float ITEM_PADDING 			= 5f; 		// padding between each leaderboard item
	private const float PROMOTION_DELAY			= 1.5f;		// minimum number of items needed before scroll bounds are calculated
	private const int MINIMUM_ITEM_COUNT		= 5;		// minimum number of items needed before scroll bounds are calculated
	private const float INTRO_DELAY				= 5f;
	private const float ITEM_STARTING_POS 		= 530f;
	
	public override void init()
	{
		continueButton.registerEventDelegate(onContinueClick);
		race = dialogArgs.getWithDefault(D.OBJECT, null) as WeeklyRace;
		rankObjects = new List<GameObject>();

		stateMachine = new StateMachine("statemachine_weekly_race_results_" + race.raceName);
		stateMachine.addState( "intro", 			 new StateOptions(null, null, playIntro));
		stateMachine.addState( "outro", 			 new StateOptions(null, null, playOutro));
		stateMachine.addState( "ranks", 			 new StateOptions(null, null, setupRankings));
		stateMachine.addState( "promotion", 		 new StateOptions(null, null, setupPromotion));
		stateMachine.addState( "promotion_outro", 	 new StateOptions(null, null, playPromotionOutro));
		stateMachine.addState( "divisions", 		 new StateOptions(null, null, playDivisions));
		stateMachine.addState( "divisions_populate", new StateOptions(null, null, setupDivisions));
		stateMachine.addState( "divisions_display",  new StateOptions(null, null, displayDivisions));
		stateMachine.addState( "divisions_promotion", new StateOptions(null, null, playDivisionPromotion));
		stateMachine.addState( State.COMPLETE );
		updateState("intro");

		if (!ExperimentWrapper.WeeklyRace.isTextMaskEnabled)
		{
			listItemMasker = null;
			divItemMasker = null;
		}

		StatsWeeklyRace.logFinalStandings(race.newDivision, race.division, race.competitionRank, (int)race.playersScore, "view");
	}

	private void onContinueClick(Dict args = null)
	{
		// some audio to play when you click on this sucker at different states
		switch(stateMachine.currentState)
		{
			case "promotion_outro":
				Audio.play("Dialog2Overlay1DismissWeeklyRace");
				break;

			case "divisions_promotion":
				Audio.play("Dialog3DismissWeeklyRace");
				break;
		}
		
		updateState(nextState);
	}

	private void updateState(string state)
	{
		stateMachine.updateState(state);
				
		if ( stateMachine.currentState == State.COMPLETE )
		{
			Dialog.close(this);
			StatsWeeklyRace.logFinalStandings(race.newDivision, race.division, race.competitionRank, (int)race.playersScore, "click");
		}
	}

	/*=========================================================================================
	SETUP METHODS
	=========================================================================================*/
	private void setupRankings()
	{
		standingsIntro.SetActive(false);
		standings.SetActive(true);
		divisions.SetActive(false);
		promotion.SetActive(false);
		continueButton.gameObject.SetActive(true);
		intro.SetActive(true);
		
		List<WeeklyRaceRacer> racers = race.getRacersByRank;
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
				addRelegationListItem(yPos);
				yPos -= WeeklyRaceZoneInfo.ZONE_ITEM_HEIGHT + ITEM_PADDING;
				totalSize += WeeklyRaceZoneInfo.ZONE_ITEM_HEIGHT + ITEM_PADDING;
			}

			// add the racer
			addPlayerListItem(racer, yPos);
			yPos -= WeeklyRaceLeaderboard.PLAYER_ITEM_HEIGHT + ITEM_PADDING;
			totalSize += WeeklyRaceLeaderboard.PLAYER_ITEM_HEIGHT + ITEM_PADDING;

			// check if we need to display the promotion zone item (appears below the racer)
			if (!hasAddedPromo && race.isRankWithinPromotion(racer.competitionRank))
			{
				// if it's the last racer in the list, or the last racer eligible for promotion, then add the promotion zone asset
				if (i == racers.Count - 1 || !race.isRankWithinPromotion(racers[i+1].competitionRank))
				{
					hasAddedPromo = true;
					addPromotionListItem(yPos);
					yPos -= WeeklyRaceZoneInfo.ZONE_ITEM_HEIGHT + ITEM_PADDING;
					totalSize += WeeklyRaceZoneInfo.ZONE_ITEM_HEIGHT + ITEM_PADDING;
				}
			}
		}

		if (!race.isScoreInflated)
		{
			addInflationFactorDisclaimerListItem(yPos);
			totalSize += WeeklyRaceLeaderboard.PLAYER_ITEM_HEIGHT + ITEM_PADDING;
		}

		if (racers.Count <= MINIMUM_ITEM_COUNT)
		{
			slideController.setBounds(0f, 0f);
			slideController.enabled = false;
			CommonTransform.setY(slideController.content.transform, ITEM_STARTING_POS);
			slideController.toggleScrollBar();
		}
		else
		{
			totalSize -= WeeklyRaceLeaderboard.PLAYER_ITEM_HEIGHT * MINIMUM_ITEM_COUNT/2;
			slideController.setBounds(totalSize, ITEM_STARTING_POS);
			slideController.enabled = true;
			slideController.toggleScrollBar(true);

			if (usersRankObject != null)
			{
				slideController.safleySetYLocation(usersRankObject.transform.localPosition.y * -1f);
			}
		}

		divisionBadge.spriteName = WeeklyRace.getBadgeSprite(race.division);
		divisionLabel.spriteName = WeeklyRace.getDivisionTierSprite(race.division);
		descriptionText.text = WeeklyRace.getFullDivisionName(race.division);
	}

	private void setupPromotion()
	{
		standingsIntro.SetActive(false);
		promotion.SetActive(true);
		standings.SetActive(false);
		divisions.SetActive(false);
		intro.SetActive(false);
		
		if (LOCAL_TESTING_PROMOTION || race.newDivision > race.division)
		{
			int division = LOCAL_TESTING_PROMOTION ? race.division + 1 : race.newDivision;
			promotionDivisionBadge.spriteName = WeeklyRace.getBadgeSprite(division);
			promotionDivisionLabel.spriteName = WeeklyRace.getDivisionTierSprite(division);

			Material labelMaterial = getMaterialForDivision(division);
			promotionDivisionText.fontMaterial = labelMaterial;
			promotionDivisionText.text = WeeklyRace.getFullDivisionName(division);
			promotionAnimator.Play(PROMO_INTRO_ANIM);
			continueButton.gameObject.SetActive(true);
			Audio.play("PromotionFanfareWeeklyRace");
			StatsWeeklyRace.logPromotion(race.newDivision, race.division, "view");
		}
		else
		{
			updateState(nextState);
		}
	}

	protected void setupDivisions()
	{		
		intro.SetActive(true);
		standingsIntro.SetActive(false);
		promotion.SetActive(false);
		standings.SetActive(false);
		divisions.SetActive(true);
		
		float yPos = 0.0f;
		float totalSize = 0f;
		float playerPos = 0f;
		int playerDivision = 0;

		GameObject divisionObject = null;
		WeeklyRaceDivisionListItem divisionItem = null;

		for (int i = race.currentNumberOfDivisions-1; i >= 0; --i)
		{
			// we are going to setup the player at the very end so it can resolve most layering issues
			if (i == race.division)
			{
				// we are going to add the promotion bar to above the player object
				if (LOCAL_TESTING_PROMOTION || race.newDivision > race.division)
				{
					addPromotionListItem(yPos, "Promotion");
					
					yPos -= WeeklyRaceZoneInfo.ZONE_ITEM_HEIGHT + ITEM_PADDING;
					totalSize += WeeklyRaceZoneInfo.ZONE_ITEM_HEIGHT;
				}
				else if (race.newDivision == race.division)
				{
					addRelegationListItem(yPos, "No Division Change");
					
					yPos -= WeeklyRaceZoneInfo.ZONE_ITEM_HEIGHT + ITEM_PADDING;
					totalSize += WeeklyRaceZoneInfo.ZONE_ITEM_HEIGHT;
				}
				
				playerPos = yPos;
				playerDivision = i;
				yPos -= WeeklyRaceDivisions.ITEM_SIZE + ITEM_PADDING;
				totalSize += WeeklyRaceDivisions.ITEM_SIZE + ITEM_PADDING;

				if (LOCAL_TESTING_DEMOTION || race.newDivision < race.division)
				{
					addRelegationListItem(yPos, "Drop Zone");
					
					yPos -= WeeklyRaceZoneInfo.ZONE_ITEM_HEIGHT + ITEM_PADDING;
					totalSize += WeeklyRaceZoneInfo.ZONE_ITEM_HEIGHT;
				}
				
				continue;
			}

			divisionObject = NGUITools.AddChild(itemsParent, playerDivisionListItem);
			divisionItem = divisionObject.GetComponent<WeeklyRaceDivisionListItem>();
			CommonTransform.setY(divisionItem.transform, yPos);
			
			yPos -= WeeklyRaceDivisions.ITEM_SIZE + ITEM_PADDING;
			totalSize += WeeklyRaceDivisions.ITEM_SIZE + ITEM_PADDING;

			divisionItem.setup(i, race.getDailyBonusForDivision(i), false);
			if (divItemMasker != null)
			{
				divItemMasker.addObjectToList(divisionItem.dailyBonusText);
				divItemMasker.addObjectToList(divisionItem.divisionText);
				divItemMasker.addObjectToList(divisionItem.freeBonusText);
			}

			rankObjects.Add(divisionObject);
		}

		// setup the player division list item
		divisionObject = NGUITools.AddChild(itemsParent, playerDivisionListItem);
		divisionItem = divisionObject.GetComponent<WeeklyRaceDivisionListItem>();
		CommonTransform.setY(divisionItem.transform, playerPos);

		divisionItem.setup(playerDivision, race.getDailyBonusForDivision(playerDivision), true);
		if (divItemMasker != null)
		{
			divItemMasker.addObjectToList(divisionItem.dailyBonusText);
			divItemMasker.addObjectToList(divisionItem.divisionText);
			divItemMasker.addObjectToList(divisionItem.freeBonusText);
		}

		rankObjects.Add(divisionObject);
		playersDivisionObject = divisionItem;

		totalSize -= WeeklyRaceDivisions.ITEM_SIZE * 1.5f;
		slideController.setBounds(totalSize, ITEM_STARTING_POS);
		slideController.toggleScrollBar(true);
		
		if (LOCAL_TESTING_PROMOTION || LOCAL_TESTING_DEMOTION || race.hasPromotion || race.hasRelegation)
		{
			continueButton.gameObject.SetActive(false);

			if (race.hasPromotion || LOCAL_TESTING_PROMOTION)
			{
				descriptionText.text = Localize.text("weekly_race_promoted");
			}
			else
			{
				descriptionText.text = Localize.text("weekly_race_oh_no");
			}
		}
		else if (playerDivision == race.currentNumberOfDivisions - 1)
		{
			descriptionText.text = Localize.text("weekly_race_division_cap");
		}
		else
		{
			descriptionText.text = Localize.text("weekly_race_oh_no");
		}

		StartCoroutine(waitSequence(0.5f));
	}

	private void addPlayerListItem(WeeklyRaceRacer racer, float position)
	{
		GameObject racerObject = NGUITools.AddChild(itemsParent, playerListItem);
		WeeklyRacePlayerListItem playerItem = racerObject.GetComponent<WeeklyRacePlayerListItem>();
		CommonTransform.setY(playerItem.transform, position);
		playerItem.setup(null, racer, race, null, false);
		if (listItemMasker != null)
		{
			listItemMasker.addObjectToList(playerItem.playerName);
			listItemMasker.addObjectToList(playerItem.playerRank);
			listItemMasker.addObjectToList(playerItem.playerScore);
			listItemMasker.addObjectToList(playerItem.friendScore);
			listItemMasker.addObjectToList(playerItem.divisionName);
		}

		if (racer == race.playersRacerInstance)
		{
			usersRankObject = racerObject;
		}

		rankObjects.Add(racerObject);
	}

	private void addPromotionListItem(float position, string text = "Promotion Zone")
	{
		GameObject promoObject = NGUITools.AddChild(itemsParent, playerZoneListItem);
		WeeklyRaceZoneListItem promoItem = promoObject.GetComponent<WeeklyRaceZoneListItem>();
		
		CommonTransform.setY(promoObject.transform, position);
		promoItem.setup(null, true);
		promoItem.setText(text);
		rankObjects.Add(promoObject);
		if (listItemMasker != null)
		{
			listItemMasker.addObjectToList(promoItem.promotionText);
		}
	}

	private void addInflationFactorDisclaimerListItem(float position)
	{
		GameObject inflationFactorDisclaimer = NGUITools.AddChild(itemsParent, inflationFactorDisclaimerItem);
		rankObjects.Add(inflationFactorDisclaimer);
		CommonTransform.setY(inflationFactorDisclaimer.transform, position);
	}

	private void addRelegationListItem(float position, string text = "Drop Zone")
	{
		GameObject relegationObject = NGUITools.AddChild(itemsParent, playerZoneListItem);
		WeeklyRaceZoneListItem relegationItem = relegationObject.GetComponent<WeeklyRaceZoneListItem>();
		
		CommonTransform.setY(relegationObject.transform, position);
		relegationItem.setup(null, false);
		relegationItem.setText(text);
		rankObjects.Add(relegationObject);
		if (listItemMasker != null)
		{
			listItemMasker.addObjectToList(relegationItem.relegationText);
		}
		
	}

	private void removeRankObjects()
	{
		for (int i = 0; i < rankObjects.Count; ++i)
		{
			GameObject.Destroy(rankObjects[i]);
		}

		rankObjects = new List<GameObject>();
	}

	/*=========================================================================================
	ANIMATION/SEQUENCING
	=========================================================================================*/
	private void playIntro()
	{
		standingsIntro.SetActive(true);
		intro.SetActive(false);
		promotion.SetActive(false);
		standings.SetActive(false);
		divisions.SetActive(false);
		continueButton.gameObject.SetActive(false);
		StartCoroutine(waitSequence(INTRO_DELAY * 0.8f));
		Audio.play("FeatureEndsAlarmWeeklyRace");
	}

	private void playOutro()
	{
		StartCoroutine(waitSequence(INTRO_DELAY * 0.2f));
		Audio.play("FeatureEndFoley1WeeklyRace");
	}

	private void playPromotionOutro()
	{
		if (LOCAL_TESTING_PROMOTION || race.newDivision > race.division)
		{
			promotionAnimator.Play(PROMO_OUTRO_ANIM);
			StartCoroutine(waitSequence(PROMOTION_DELAY));
		}
		else
		{
			updateState(nextState);
		}
	}

	protected void playDivisions()
	{
		removeRankObjects();
		promotion.SetActive(false);
		standings.SetActive(false);
		divisions.SetActive(true);
		intro.SetActive(true);
		slideController.gameObject.SetActive(false);
		divisionsAnimator.Play(INTRO_ANIM);
		Audio.play("Dialog3OpenWeeklyRace");
		updateState(nextState);

		slideController.safleySetYLocation(playersDivisionObject.transform.localPosition.y * -1f);
	}

	protected void displayDivisions()
	{
		slideController.gameObject.SetActive(true);
		
		if (LOCAL_TESTING_PROMOTION || LOCAL_TESTING_DEMOTION || race.hasPromotion || race.hasRelegation)
		{
			continueButton.gameObject.SetActive(false);
			StartCoroutine(waitSequence(2f));
		}
		else
		{
			updateState(nextState);
		}
	}

	private void playDivisionPromotion()
	{
		if (LOCAL_TESTING_PROMOTION || LOCAL_TESTING_DEMOTION || race.hasPromotion || race.hasRelegation)
		{
			int newDivision = race.newDivision;

			if (LOCAL_TESTING_PROMOTION)
			{
				newDivision = race.division + 1;
			}
			else if (LOCAL_TESTING_DEMOTION)
			{
				newDivision = race.division - 1;
			}
			
			moveToObject = rankObjects[race.currentNumberOfDivisions - newDivision - 1].GetComponent<WeeklyRaceDivisionListItem>();
			playersDivisionObject.profileObject.transform.parent = moveToObject.profileObject.transform.parent;

			Audio.play("Dialog3PromoteTravelsWeeklyRace");
			
			iTween.MoveTo
			(
				playersDivisionObject.profileObject,
				iTween.Hash
				(
					"y",
					moveToObject.profileObject.transform.localPosition.y,
					"time",
					0.8f,
					"islocal",
					true,
					"easetype",
					iTween.EaseType.easeInOutQuart,
					"oncompletetarget",
					this.gameObject,
					"oncomplete",
					"onDivisionPromotionComplete"
				)
			);


			// only animate if we aren't promoting to the last division
			if (newDivision < race.currentNumberOfDivisions - 2)
			{
				iTween.MoveTo
				(
					slideController.content.gameObject,
					iTween.Hash
					(
						"y",
						moveToObject.transform.localPosition.y * -1,
						"time",
						0.8f,
						"islocal",
						true,
						"easetype",
						iTween.EaseType.easeInOutQuart
					)
				);
			}

			playersDivisionObject.playProfileAnimation();
		}
		else
		{
			slideController.enabled = true;
			continueButton.gameObject.SetActive(true);
		}
	}

	public void onDivisionPromotionComplete()
	{
		int newDivision = race.newDivision;

		if (LOCAL_TESTING_PROMOTION)
		{
			newDivision = race.division + 1;
		}
		else if (LOCAL_TESTING_DEMOTION)
		{
			newDivision = race.division - 1;
		}
		
		WeeklyRaceDivisionListItem playerListItem = playersDivisionObject.GetComponent<WeeklyRaceDivisionListItem>();
		WeeklyRaceDivisionListItem moveToObject = rankObjects[race.currentNumberOfDivisions - newDivision - 1].GetComponent<WeeklyRaceDivisionListItem>();

		playerListItem.showStandardBg();
		moveToObject.showPlayerBg();

		continueButton.gameObject.SetActive(true);
		slideController.enabled = true;

		if (LOCAL_TESTING_PROMOTION || race.hasPromotion)
		{
			Audio.play("Dialog3PromoteLandsWeeklyRace");
			moveToObject.playProfileAnimation();
		}
		else if (LOCAL_TESTING_DEMOTION || race.hasRelegation)
		{
			moveToObject.playDemotionAnimation();
		}
	}

	private IEnumerator waitSequence(float delay = 3.0f)
	{
		yield return new WaitForSeconds(delay);

		updateState(nextState);
	}


	// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup. Downloaded textures are automatically destroyed by Dialog class.
		WeeklyRacePlayerListItem.clearProfilePictures();
		
		// Get all friends again since their division status might be changed.
		PlayerAction.getFriendsListAgain
		(
			// Reset all player data before getting new set of them
			SocialMember.resetStaticClassData
		);
	}

	/*=========================================================================================
	GETTERS
	=========================================================================================*/
	private string nextState
	{
		get
		{
			switch(stateMachine.currentState)
			{
				case "intro":
					return "outro";

				case "outro":
					return "ranks";

				case "ranks":
					return "promotion";

				case "promotion":
					return "promotion_outro";

				case "promotion_outro":
					return "divisions";

				case "divisions":
					return "divisions_populate";

				case "divisions_populate":
					return "divisions_display";

				case "divisions_display":
					return "divisions_promotion";
					
				default:
					return State.COMPLETE;
			}
		}		
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

	/*=========================================================================================
	STATIC
	=========================================================================================*/
	public static void showDialog(bool hasChest, Dict args = null, SchedulerPriority.PriorityType priority = SchedulerPriority.PriorityType.HIGH)
	{
		WeeklyRace.clearLeaderboardFromDialogs();

		package = new DialogPackage();
		package.addTask(new DialogTask("weekly_race_results"));

		if (hasChest)
		{
			package.addTask(new DialogTask("weekly_race_rewards"));
			package.addTask(new DialogTask("weekly_race_boost"));
		}

		if (Dialog.isSpecifiedDialogShowing("weekly_race_leaderboard"))
		{
			priority = SchedulerPriority.PriorityType.IMMEDIATE;
		}
		
		Scheduler.addPackage(package, priority);
		Scheduler.addDialog("weekly_race_results", args, priority, package);
	}
}
