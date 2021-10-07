using UnityEngine;
using System.Collections;

/// <summary>
///   This is part of the main leaderboard dialog, it appears when the user clicks on the "View Divisions" button
/// </summary>
public class WeeklyRaceDivisions : WeeklyRaceSubDialog
{
	// =============================
	// PRIVATE
	// =============================
	private bool isSetup;
	
	[SerializeField] private GameObject itemsParent;	
	[SerializeField] private SlideController slideController;
	[SerializeField] private TextMeshProMasker masker;

	// =============================
	// PUBLIC
	// =============================
	public GameObject listItem;
	
	// =============================
	// CONST
	// =============================
	public const float ITEM_SIZE 				= 336f; 	// list item size
	public const float ITEM_PADDING 			= 5f; 		// padding between each item
	public const float START_ITEM_POS 			= 560f;		// starting y position
	
	public override void init(WeeklyRaceLeaderboard leaderboard)
	{
		setupDivisions();
		base.init(leaderboard);
	}

	protected void setupDivisions()
	{
		GameObject playersDivisionObject = null;
		
		if (!isSetup)
		{
			float yPos = START_ITEM_POS;
			float totalSize = 0f;
		
			for (int i = WeeklyRaceDirector.currentRace.currentNumberOfDivisions-1; i >= 0; --i)
			{
				GameObject divisionObject = NGUITools.AddChild(itemsParent, listItem);
				WeeklyRaceDivisionListItem divisionItem = divisionObject.GetComponent<WeeklyRaceDivisionListItem>();
				CommonTransform.setY(divisionItem.transform, yPos);
				yPos -= ITEM_SIZE + ITEM_PADDING;
				totalSize += ITEM_SIZE + ITEM_PADDING;

				divisionItem.setup(i, WeeklyRaceDirector.currentRace.getDailyBonusForDivision(i), i == WeeklyRaceDirector.currentRace.division);
				masker.addObjectToList(divisionItem.dailyBonusText);
				masker.addObjectToList(divisionItem.divisionText);
				masker.addObjectToList(divisionItem.freeBonusText);

				if (i == WeeklyRaceDirector.currentRace.division)
				{
					playersDivisionObject = divisionObject;
				}
			}

			if (playersDivisionObject != null)
			{
				slideController.safleySetYLocation(playersDivisionObject.transform.localPosition.y * -1f);
			}

			totalSize -= ITEM_SIZE * 2.5f;
			slideController.setBounds(totalSize, slideController.bottomBound);


			isSetup = true;
		}
	}
}
