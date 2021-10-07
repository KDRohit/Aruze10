using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Handles creating and displaying UI for a set of achievements for Land of Oz.
*/
public class LandOfOzAchievementListScript : MonoBehaviour
{
	private const float DELAY_BETWEEN_FADES = 0.25f;
	
	// =============================
	// PRIVATE
	// =============================
	private LobbyGame game = null;
	private List<LandOfOzAchievementScript> achievements = new List<LandOfOzAchievementScript>();
	
	// =============================
	// PUBLIC
	// =============================
	public UIGrid grid;
	public bool animateChecksImmedaitely = true;
	public LandOfOzAchievementScript firstAchievement;	// Also used as the template for creating an array of achievements.
	public UIButton button;								// Only used by the in-game achievements panel, to launch the dialog.
	
	public void init(LobbyGame game, Mission mission = null)
	{
		this.game = game;
	
		// Since this could be called multiple times when reused for a lobby option,
		// only create options if they don't already exist.
		if (achievements.Count < LobbyMission.OBJECTIVES_PER_GAME)
		{
			achievements.Add(firstAchievement);
		
			// Create the other 4 achievements.
			for (int i = 1; i < LobbyMission.OBJECTIVES_PER_GAME; i++)
			{
				GameObject go = NGUITools.AddChild(grid.gameObject, firstAchievement.gameObject);
				go.name = string.Format("{0} Achievement", i);
			
				LandOfOzAchievementScript achievement = go.GetComponent<LandOfOzAchievementScript>();
			
				achievements.Add(achievement);
			}
		
			grid.repositionNow = true;
		}
		
		refresh(mission);
		
		if (animateChecksImmedaitely)
		{
			animateChecks();
		}
	}
	
	// This is called externally too.
	public void refresh(Mission mission = null)
	{
		for (int i = 0; i < LobbyMission.OBJECTIVES_PER_GAME; i++)
		{
			achievements[i].refresh(game, i, mission);
		}
	}

	public void animateChecks()
	{
		// Animate the checkmark on any achievements that are "achieved".
		for (int i = 0; i < LobbyMission.OBJECTIVES_PER_GAME; i++)
		{
			// No need to check for achieved status, because calling this on
			// achievements that aren't showing the achived mode won't do anything anyway.
			achievements[i].animateCheck();
		}
	}
	
	public IEnumerator fadeOutAchievements()
	{
		Audio.play( "ObjectivesFadeLOOZ" );
				
		for (int i = 0; i < LobbyMission.OBJECTIVES_PER_GAME; i++)
		{
			StartCoroutine(achievements[i].fadeOut(i * DELAY_BETWEEN_FADES));
		}
		
		// Wait for all of them to finish fading.
		while (LandOfOzAchievementScript.isFading)
		{
			yield return null;
		}
	}

	// NGUI button callback, mainly used by the in-game side UI.
	private void showAchievementsDialog()
	{
		LOZObjectivesDialog.showDialog();
	}	
}
