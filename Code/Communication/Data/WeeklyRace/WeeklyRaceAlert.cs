using UnityEngine;
using System.Collections;

using TMPro;

/// <summary>
///   This is a toaster like asset that appears to tell the player about their updates/changes during a weekly race
/// </summary>
public class WeeklyRaceAlert : MonoBehaviour
{
	// =============================
	// PRIVATE
	// =============================
	// global assets
	[SerializeField] private GameObject weeklyRaceLogo;

	// negative alert
	[SerializeField] private GameObject downAlert;
	[SerializeField] private GameObject downAlertPosition;
	[SerializeField] private TextMeshPro downAlertPositionText;
	[SerializeField] private TextMeshPro downAlertPositionTextDescription;
	[SerializeField] private UISprite downAlertPositionChest;	
	[SerializeField] private GameObject downAlertZone;
	[SerializeField] private TextMeshPro downAlertZoneText;

	// positive alert
	[SerializeField] private GameObject upAlert;
	[SerializeField] private GameObject upAlertPosition;
	[SerializeField] private TextMeshPro upAlertPositionText;
	[SerializeField] private TextMeshPro upAlertPositionTextDescription;
	[SerializeField] private UISprite upAlertPositionChest;	
	[SerializeField] private GameObject upAlertZone;
	[SerializeField] private TextMeshPro upAlertZoneText;

	// rival alerts
	[SerializeField] private GameObject rivalPassed;
	[SerializeField] private Renderer rivalPassedProfile;
	[SerializeField] private TextMeshPro rivalLeadText;
	[SerializeField] private TextMeshPro playerLeadText;
	[SerializeField] private GameObject rivalLead;
	[SerializeField] private Renderer rivalLeadProfile;
	[SerializeField] private GameObject rivalEnding;

	// race ending alert
	[SerializeField] private GameObject raceEndingObject;
	[SerializeField] private TextMeshPro raceEndingText;

	private GenericDelegate onComplete; // callback for when the alert is done displaying
	private Dict args = null;

	// =============================
	// PUBLIC
	// =============================
	public bool isShowing { get; private set; }
	
	// =============================
	// CONST
	// =============================
	private const string FIRST_PLACE_MESSAGE = "You're in the lead";
	private const string FIRST_PLACE_LOST_MESSAGE = "You've lost the lead";
	
	private const string SECOND_THIRD_MESSAGE = "You've moved up";
	private const string SECOND_THIRD_LOST_MESSAGE = "You've been passed";

	private const string ZONE_MESSAGE = "You've entered the {0} Zone!";
	private const string ZONE_LOST_MESSAGE = "You've exited the {0} Zone!";

	private static string[] RIVAL_LEAD_MESSAGES = new string[]
	{
		"Your Rival has passed you! Time to bet up!",
		"Your Rival has taken the lead! Time to catch up!",
		"Your Rival is pulling ahead! Bet up to catch up!",
		"Your Rival is out in front! Time to bet up",
		"Your Rival is on a hot streak! Time to bet up!"
	};

	private static string[] PLAYER_LEAD_MESSAGES = new string[]
	{
		"You've passed your Rival! Keep it up!",
		"You've jumped ahead of your Rival! Keep it up!",
		"You're out in front of your Rival! Keep it up!",
		"Nice spins, you're leading! Keep it up!",
		"You're beating your Rival! Keep it up!"
	};

	/*=========================================================================================
	SETUP METHODS FOR DIFFERENT STATES
	=========================================================================================*/
	/// <summary>
	///   Function to setup an alert when user places into first, second, or third
	/// </summary>
	public void setupLeaderAlert()
	{
		int rank = WeeklyRaceDirector.currentRace.competitionRank;
		
		disableAllObjects();
		upAlert.SetActive(true);
		upAlertPosition.SetActive(true);
		upAlertZone.SetActive(false);
		
		upAlertPositionText.text = CommonText.formatContestPlacement(rank+1, true) + " Place!";
		upAlertPositionTextDescription.text = rank <= 0 ? FIRST_PLACE_MESSAGE : SECOND_THIRD_MESSAGE;

		if (WeeklyRaceDirector.currentRace != null)
		{
			int chestId = WeeklyRaceDirector.currentRace.getChestForRank(rank);

			if (chestId > 0)
			{
				upAlertPositionChest.gameObject.SetActive(true);
				upAlertPositionChest.spriteName = WeeklyRace.chestSpriteNames[chestId];
			}
			else
			{
				upAlertPositionChest.gameObject.SetActive(false);
			}
		}
		else
		{
			upAlertPositionChest.gameObject.SetActive(false);
		}

		Audio.play("ToasterTakeLeadWeeklyRace");
	}

	/// <summary>
	///   Function to setup an alert when user loses first, second, or third place
	/// </summary>
	public void setupLeaderDownAlert()
	{
		int rank = WeeklyRaceDirector.currentRace.competitionRank;
				
		disableAllObjects();
		downAlert.SetActive(true);
		downAlertPosition.SetActive(true);
		downAlertZone.SetActive(false);
		
		downAlertPositionText.text = CommonText.formatContestPlacement(rank+1, true) + " Place!";
		downAlertPositionTextDescription.text = rank <= 0 ? FIRST_PLACE_LOST_MESSAGE : SECOND_THIRD_LOST_MESSAGE;

		if (WeeklyRaceDirector.currentRace != null)
		{
			int chestId = WeeklyRaceDirector.currentRace.getChestForRank(rank);

			if (chestId > 0)
			{
				downAlertPositionChest.gameObject.SetActive(true);
				downAlertPositionChest.spriteName = WeeklyRace.chestSpriteNames[chestId];
			}
			else
			{
				downAlertPositionChest.gameObject.SetActive(false);
			}
		}
		else
		{
			downAlertPositionChest.gameObject.SetActive(false);
		}

		Audio.play("ToasterExitPromotionWeeklyRace");
	}

	public void setupPromotionZone(bool hasExited = false)
	{
		disableAllObjects();

		if (!hasExited)
		{
			upAlert.SetActive(true);
			upAlertPosition.SetActive(false);
			upAlertZone.SetActive(true);
			upAlertZoneText.text = string.Format(ZONE_MESSAGE, "Promotion");
			Audio.play("ToasterEnterPromotionWeeklyRace");
		}
		else
		{
			downAlert.SetActive(true);
			downAlertPosition.SetActive(false);
			downAlertZone.SetActive(true);
			downAlertZoneText.text = string.Format(ZONE_LOST_MESSAGE, "Promotion");
			Audio.play("ToasterExitPromotionWeeklyRace");
		}
	}

	public void setupDropZone(bool hasExited = false)
	{
		disableAllObjects();

		// in the case of the drop zone...it's actually good if you exit, and bad if you entered it.
		if (hasExited)
		{
			upAlert.SetActive(true);
			upAlertPosition.SetActive(false);
			upAlertZone.SetActive(true);
			upAlertZoneText.text = string.Format(ZONE_LOST_MESSAGE, "Drop");
			Audio.play("ToasterEnterPromotionWeeklyRace");
		}
		else
		{
			downAlert.SetActive(true);
			downAlertPosition.SetActive(false);
			downAlertZone.SetActive(true);
			downAlertZoneText.text = string.Format(ZONE_MESSAGE, "Drop");
			Audio.play("ToasterExitPromotionWeeklyRace");
		}		
	}

	public void setupRaceEnding()
	{
		disableAllObjects();
		raceEndingObject.SetActive(true);

		if (WeeklyRaceDirector.currentRace != null && WeeklyRaceDirector.currentRace.timeRemaining > 0)
		{
			raceEndingText.text = "Ends in " + WeeklyRaceDirector.currentRace.formattedTimeleft;
		}
		else
		{
			raceEndingText.text = "Race has ended";
		}
		Audio.play("ToasterRaceEndsWeeklyRace");
	}

	/*=========================================================================================
	DAILY RIVAL ALERTS
	=========================================================================================*/
	public void setupRivalAlert(bool rivalHasLead)
	{
		SafeSet.gameObjectActive(weeklyRaceLogo, false);

		if (WeeklyRaceDirector.currentRace != null)
		{
			if (rivalHasLead && WeeklyRaceDirector.currentRace.rivalsRacerInstance != null)
			{
				WeeklyRaceRacer rival = WeeklyRaceDirector.currentRace.rivalsRacerInstance;
				SafeSet.gameObjectActive(rivalLead, true);
				DisplayAsset.loadTextureToRenderer(rivalLeadProfile, rival.member.getImageURL, "", true, shouldShowBrokenImage:false);
				rivalLeadText.text = generateStatusText(RIVAL_LEAD_MESSAGES);
				Audio.play("ToasterTakeLeadWeeklyRace");
			}
			else
			{
				playerLeadText.text = generateStatusText(PLAYER_LEAD_MESSAGES);
				SafeSet.gameObjectActive(rivalPassed, true);
				DisplayAsset.loadTextureToRenderer(rivalPassedProfile, SlotsPlayer.instance.socialMember.getImageURL, "", true, shouldShowBrokenImage:false);
				Audio.play("ToasterExitPromotionWeeklyRace");
			}
		}
	}

	public void setupRivalPaired()
	{
		SafeSet.gameObjectActive(weeklyRaceLogo, false);

		if (WeeklyRaceDirector.currentRace != null && WeeklyRaceDirector.currentRace.rivalsRacerInstance != null)
		{
			WeeklyRaceRacer rival = WeeklyRaceDirector.currentRace.rivalsRacerInstance;
			playerLeadText.text = Localize.text("new_rival_{0}", rival.name);
			SafeSet.gameObjectActive(rivalPassed, true);
			DisplayAsset.loadTextureToRenderer(rivalPassedProfile, rival.member.getImageURL, "", true, shouldShowBrokenImage:false);
			Audio.play("ToasterTakeLeadWeeklyRace");
		}
	}

	public void setupRivalWins(SocialMember rival, string rivalName)
	{
		SafeSet.gameObjectActive(weeklyRaceLogo, false);

		SafeSet.gameObjectActive(rivalLead, true);
		DisplayAsset.loadTextureToRenderer(rivalLeadProfile, rival.getImageURL, "", true, shouldShowBrokenImage:false);
		rivalLeadText.text = Localize.text("rival_won_{0}", rivalName);
		Audio.play("ToasterTakeLeadWeeklyRace");
	}

	public void setupRivalEnding()
	{
		SafeSet.gameObjectActive(weeklyRaceLogo, false);
		SafeSet.gameObjectActive(rivalEnding, true);
		Audio.play("ToasterRaceEndsWeeklyRace");
	}

	void Update()
	{
		if (raceEndingObject.activeSelf)
		{
			if (WeeklyRaceDirector.currentRace != null && WeeklyRaceDirector.currentRace.timeRemaining > 0)
			{
				raceEndingText.text = "Ends in " + WeeklyRaceDirector.currentRace.formattedTimeleft;
			}
		}
	}

	/*=========================================================================================
	PLAYING ALERT
	=========================================================================================*/
	public void show(GenericDelegate callback = null)
	{
		GenericToaster toaster = gameObject.GetComponent<GenericToaster>();
		toaster.init(null);
		StartCoroutine(waitSequence());
		isShowing = true;
		onComplete = callback;
	}

	private IEnumerator waitSequence(float time = 3.0f)
	{
		yield return new WaitForSeconds(time);

		Audio.play("ToasterDismissWeeklyRace");

		isShowing = false;

		if (onComplete != null)
		{
			onComplete();
		}
	}

	/*=========================================================================================
	ANCILLARY
	=========================================================================================*/
	private void disableAllObjects()
	{
		downAlert.SetActive(false);
		downAlertPosition.SetActive(false);
		downAlertZone.SetActive(false);
		upAlert.SetActive(false);
		upAlertPosition.SetActive(false);
		upAlertZone.SetActive(false	);
		raceEndingObject.SetActive(false);
	}

	private string generateStatusText(string[] texts)
	{
		int r = Random.Range(0, texts.Length - 1);
		return texts.Length > 0 ? texts[r] : "";
	}
}
