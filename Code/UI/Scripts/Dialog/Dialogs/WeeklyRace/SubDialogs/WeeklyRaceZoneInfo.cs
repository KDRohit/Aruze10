using UnityEngine;
using System.Collections;

/// <summary>
///   This is part of the main leaderboard dialog, it appears when users select the zone info button
/// </summary>
public class WeeklyRaceZoneInfo : WeeklyRaceSubDialog
{
	[SerializeField] private GameObject dropZoneAsset;
	[SerializeField] private GameObject promotionZoneAsset;
	[SerializeField] private GameObject dropZoneListItem;
	[SerializeField] private GameObject promoZoneListItem;
	[SerializeField] private ButtonHandler divisionsButton;
	
	[SerializeField] private WeeklyRaceDivisionListItem divisionTopItem;
	[SerializeField] private WeeklyRaceDivisionListItem divisionBottomItem;

	// =============================
	// CONST
	// =============================
	public const float ZONE_ITEM_HEIGHT = 100f;		// height of the zone (promotion/relegation) prefab

	public override void init(WeeklyRaceLeaderboard leaderboard)
	{
		base.init(leaderboard);

		if (divisionsButton != null)
		{
			divisionsButton.registerEventDelegate(onClickDivisions);
		}
	}

	public void setup(bool showPromotionInfo)
	{
		int division = WeeklyRaceDirector.currentRace.division;
		int nextDivision = Mathf.Min(WeeklyRace.NUM_DIVISIONS-1, showPromotionInfo ? division + 1 : division - 1);
		
		dropZoneAsset.SetActive(!showPromotionInfo);
		dropZoneListItem.SetActive(!showPromotionInfo);
		promotionZoneAsset.SetActive(showPromotionInfo);
		promoZoneListItem.SetActive(showPromotionInfo);

		if (showPromotionInfo)
		{
			divisionTopItem.setup(nextDivision, WeeklyRaceDirector.currentRace.getDailyBonusForDivision(nextDivision), false);
			divisionBottomItem.setup(division, WeeklyRaceDirector.currentRace.getDailyBonusForDivision(division), true);
		}
		else
		{
			divisionTopItem.setup(division, WeeklyRaceDirector.currentRace.getDailyBonusForDivision(division), true);
			divisionBottomItem.setup(nextDivision, WeeklyRaceDirector.currentRace.getDailyBonusForDivision(nextDivision), false);
		}
	}

	private void onClickDivisions(Dict args = null)
	{
		leaderboard.onSubDialogClosed();
		leaderboard.onClickDivisions();
	}
}

