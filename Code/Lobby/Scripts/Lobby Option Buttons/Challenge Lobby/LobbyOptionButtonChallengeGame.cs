using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/**
Controls UI behavior of a Land of Oz menu game option.
*/

public class LobbyOptionButtonChallengeGame : LobbyOptionButtonActive
{
	// =============================
	// PRIVATE
	// =============================
	private float iconFadeLevel = 1f;
	
	// =============================
	// PUBLIC
	// =============================
	public GameObject lockElements;
	public GameObject finishedBox;
	public ObjectivesGrid objectiveGrid;

	// =============================
	// CONST
	// =============================
	private const float LOCKED_FADE_LEVEL = 0.5f;

	// Each subclass must override this to do its own visual setup.
	// Each override must call base.setup() first.
	public override void setup(LobbyOption option, int page, float width, float height)
	{
		base.setup(option, page, width, height);
		
		// Create the new game effect if necessary.
		createNewGameEffect();

		if (gameNameLabel != null)
		{
			SafeSet.gameObjectActive(gameNameLabel.gameObject, false);
		}

		if (image != null)
		{
			// try to get it as a renderer. If we don't have one, try as a UI texture.
			imageRendererUI = image.GetComponent<UITexture>();
			imageRenderer = image.GetComponent<Renderer>();

			if (imageRenderer != null)
			{
				imageRenderer.material = new Material(ShaderCache.find("Unlit/GUI Texture"));
				imageRenderer.material.color = Color.black;
			}
			else if (imageRendererUI != null)
			{
				imageRendererUI.material = new Material(ShaderCache.find("Unlit/GUI Texture"));
				imageRendererUI.material.color = Color.black;
			}
		}

		if (screenMaterial != null)
		{
			screenMaterial.color = Color.black;
		}

		if (cabinetGlowRenderer != null && option != null && option.game != null)
		{
			// Set the glow to a color that matches the game's color scheme.
			cabinetGlowRenderer.material.color = option.game.lobbyColor;
		}

		// Disable all mousedown tinting until the image is finished being loaded.
		foreach (UIButtonColor bc in gameObject.GetComponentsInChildren<UIButtonColor>())
		{
			bc.enabled = false;
		}

		if (objectiveGrid != null)
		{
			objectiveGrid.init(option.game);
		}

		refresh();
	}
	
	public override void refresh()
	{
		// Set the lock status. There is no level number to set since unlocking is done by completing challenges.
		if (!option.game.isUnlocked)
		{
			lockElements.SetActive(true);
			iconFadeLevel = LOCKED_FADE_LEVEL;
		}
		else
		{
			iconFadeLevel = 1f;
		}

		if (objectiveGrid != null)
		{
			objectiveGrid.fadeObjectiveAssets(iconFadeLevel);
		}

		if (finishedBox != null)
		{
			LobbyMission mission = null;

			ChallengeCampaign campaign = CampaignDirector.findWithGame(option.game.keyName);

			// use the current mission first, this is in case we are on tier 2 or tier 3 of land of oz
			if ( campaign.currentMission != null && campaign.currentMission.containsGame( option.game.keyName ) )
			{
				mission = campaign.currentMission as LobbyMission;
			}
			else
			{
				mission = campaign.findWithGame(option.game.keyName) as LobbyMission;
			}

			if (mission != null)
			{
				if (mission.isCompleteByGame(option.game.keyName))
				{
					finishedBox.SetActive(true);
					objectiveGrid.gameObject.SetActive(false);
				}
				else
				{
					finishedBox.SetActive(false);
				}
			}
		}

		if (objectiveGrid != null)
		{
			objectiveGrid.refresh();
		}
	}
	
	protected override void OnPress()
	{
		base.OnPress();
	}

	protected override void OnRelease()
	{
		base.OnRelease();
	}

	protected override void OnClick()
	{
		base.OnClick();
	}
}
