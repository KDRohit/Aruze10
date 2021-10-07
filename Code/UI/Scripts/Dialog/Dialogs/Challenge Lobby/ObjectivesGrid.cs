using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Handles creating and displaying UI for a set of achievements for Land of Oz.
*/
public class ObjectivesGrid : MonoBehaviour
{
	private const float DELAY_BETWEEN_FADES = 0.25f;
	
	// =============================
	// PRIVATE
	// =============================
	private LobbyGame game = null;
	private List<ObjectiveAsset> objectiveAssets = new List<ObjectiveAsset>();
	[SerializeField] private ClickHandler showDialogHandler;
	
	// =============================
	// PUBLIC
	// =============================
	public UIGrid grid;
	public bool animateChecksImmedaitely = true;
	public ObjectiveAsset firstObjective;	// Also used as the template for creating an array of achievements.
	public UIButton button;					// Only used by the in-game achievements panel, to launch the dialog.
	
	public virtual void init(LobbyGame game, Mission mission = null)
	{
		this.game = game;
	
		// Since this could be called multiple times when reused for a lobby option,
		// only create options if they don't already exist.
		if (objectiveAssets.Count < LobbyMission.OBJECTIVES_PER_GAME)
		{
			objectiveAssets.Add(firstObjective);
		
			// Create the other 4 achievements.
			for (int i = 1; i < LobbyMission.OBJECTIVES_PER_GAME; i++)
			{
				GameObject go = NGUITools.AddChild(grid.gameObject, firstObjective.gameObject);
				go.name = string.Format("{0} Achievement", i);
			
				ObjectiveAsset asset = go.GetComponent<ObjectiveAsset>();
			
				objectiveAssets.Add(asset);
			}
		
			grid.repositionNow = true;
		}
		
		refresh(mission);
		
		if (animateChecksImmedaitely)
		{
			animateChecks();
		}

		if (showDialogHandler != null)
		{
			showDialogHandler.registerEventDelegate(showAchievementsDialog);
		}
	}
	
	// This is called externally too.
	public virtual void refresh(Mission mission = null)
	{
		for (int i = 0; i < LobbyMission.OBJECTIVES_PER_GAME; i++)
		{
			objectiveAssets[i].refresh(game, i, mission);
		}
	}

	public virtual void playSpinAnimations()
	{
		// In case something needs to happen on spin
	}

	public void animateChecks()
	{
		// Animate the checkmark on any achievements that are "achieved".
		for (int i = 0; i < LobbyMission.OBJECTIVES_PER_GAME; i++)
		{
			// No need to check for achieved status, because calling this on
			// achievements that aren't showing the achived mode won't do anything anyway.
			objectiveAssets[i].animateCheck();
		}
	}

	public void fadeObjectiveAssets(float alpha = 1f)
	{
		for (int i = 0; i < LobbyMission.OBJECTIVES_PER_GAME; ++i)
		{
			objectiveAssets[i].fadeTo(alpha);
		}
	}
	
	public IEnumerator fadeOutAchievements()
	{
		LobbyAssetData lobbyAssetData = ChallengeLobby.findAssetDataForCampaign(CampaignDirector.findWithGame(GameState.game.keyName).campaignID);
		Audio.play(lobbyAssetData.getAudioByKey(LobbyAssetData.OBJECTIVE_FADE));
				
		for (int i = 0; i < LobbyMission.OBJECTIVES_PER_GAME; i++)
		{
			StartCoroutine(objectiveAssets[i].fadeOut(i * DELAY_BETWEEN_FADES));
		}
		
		// Wait for all of them to finish fading.
		while (ObjectiveAsset.isFading)
		{
			yield return null;
		}
	}

	// NGUI button callback, mainly used by the in-game side UI.
	private void showAchievementsDialog(Dict args = null)
	{
		ChallengeLobbyObjectivesDialog.showDialog();
	}

	public virtual void onSelectAutoSpin()
	{
		// In case a custom grid needs to do something with auto spins
	}
}
