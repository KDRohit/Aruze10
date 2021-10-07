﻿﻿﻿﻿﻿﻿﻿﻿﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
         using Com.Scheduler;
         using TMPro;

public class RoyalRushTooltipController : MonoBehaviour
{
	public Animator tooltipAnimation;
	public ButtonHandler joinButton;
	public ButtonHandler closeButton;

	public TextMeshPro titleText;
	public TextMeshPro messageText;

	public GameObject messageTextTop; // We just need to toggle this off, no need to grab it as TMPro

	private LobbyOptionButtonRoyalRush optionReference;
	private Transform lobbyOptionTransform;
	private Vector3 originalLocation = Vector3.zero;
	private GameObject originalParent;

	private const int LEFT_SIDE_X = -275;
	private const int RIGHT_SIDE_X = 750;
	private const int LOADING_TAG_X = 235;
	private const int LOADING_TAG_Y = -520;

	// These might get modified.
	private float cameraSpacingX = 1000;
	private int cameraSpacingY = 550;
	private int zOverlayPosition = -1150;
	private int tooltipYLocation = -180;

	private const string INTRO = "RR Lobby Tool Tip Intro";
	private const string INTRO_FLIPPED = "RR Lobby Tool Tip Intro Flipped";
	private static GameTimerRange timeUntilNoRegistrations;

	public static RoyalRushTooltipController instance = null;

	void Start()
	{
		if (tooltipAnimation == null)
		{
			// We are just a loading tag
			CommonTransform.setX(gameObject.transform, LOADING_TAG_X);
			CommonTransform.setY(gameObject.transform, LOADING_TAG_Y);

			// If there's an instance we're on the overlay in the FTUE, so put this tooltip there.
			if (instance != null)
			{
				CommonGameObject.setLayerRecursively(gameObject, Layers.ID_NGUI_OVERLAY);
			}
		}
		else
		{
			instance = this;

			StatsManager.Instance.LogCount("dialog", "royal_rush_join", genus: "view");
			
			optionReference = gameObject.GetComponentInParent<LobbyOptionButtonRoyalRush>();
			GameObject parentObject = optionReference.gameObject;
			lobbyOptionTransform = parentObject.transform;
			timeUntilNoRegistrations = new GameTimerRange(GameTimer.currentTime, GameTimer.currentTime + (optionReference.rushInfo.rushFeatureTimer.timeRemaining - RoyalRushEvent.minTimeRequired));
			if (timeUntilNoRegistrations.isExpired)
			{
				onClickClose();
			}
			else
			{
				timeUntilNoRegistrations.registerFunction(onTimeout);
			}

			// So we can put it back
			originalLocation = lobbyOptionTransform.localPosition;
			originalParent = lobbyOptionTransform.parent.gameObject;

			// Move it to the overlay camera.
			lobbyOptionTransform.parent = Overlay.instance.gameObject.transform;

			// Set the lobby card this is on and all of its children to the overlay
			CommonGameObject.setLayerRecursively(parentObject, Layers.ID_NGUI_OVERLAY);

			float shroudZ = MainLobby.hirV3 == null ? -1100 : -1800;

			if (MainLobby.hirV3 == null)
			{
				CommonTransform.setX(lobbyOptionTransform, originalLocation.x - cameraSpacingX);
				CommonTransform.setY(lobbyOptionTransform, cameraSpacingY);
			}

			// adjust for lobby v3
			zOverlayPosition = (int)shroudZ - 50;

			Overlay.instance.showShroud(clickDelegate: onClickShroudAway, z: shroudZ, isFTUE: true);

			CommonTransform.setZ(lobbyOptionTransform, zOverlayPosition);

			// If the option is on the right side of the screen, put this on the left
			if(LobbyOptionButtonRoyalRush.pinnedLoc > 1)
			{
				CommonTransform.setX(gameObject.transform, LEFT_SIDE_X);
				tooltipAnimation.Play(INTRO_FLIPPED);
			}
			else
			{
				CommonTransform.setX(gameObject.transform, RIGHT_SIDE_X);
				tooltipAnimation.Play(INTRO);
			}

			CommonTransform.setY(gameObject.transform, tooltipYLocation);
		
			joinButton.registerEventDelegate(tryJoin);
	        closeButton.registerEventDelegate(onClickClose);
			// we're going deep!
		}
	}

	private void onTimeout(Dict args = null, GameTimerRange parent = null)
	{
	
		Debug.LogError("User timed out while looking at the royal rush FTUE");
		onClickClose();
	}
	private void onClickClose(Dict args = null)
	{
		StatsManager.Instance.LogCount("dialog", "royal_rush_join", family: "close", genus: "click");
		if (Overlay.instance != null)
		{
			Overlay.instance.hideShroud();
		}
	}

	private void onClickShroudAway()
	{
		resetUIPositions();
	}

	// On clicking the join button, lets get in there
	public void tryJoin(Dict args = null)
	{
		StatsManager.Instance.LogCount("dialog", "royal_rush_join", family: "join_now", genus: "click");

		LobbyGame gameToLaunch = LobbyGame.find(optionReference.rushInfo.gameKey);

		if (gameToLaunch != null)
		{
			optionReference.loadTooltip();
			// We want to show the tooltip on the option. Just not the FTUE.
			gameToLaunch.askInitialBetOrTryLaunch();

			SlotAction.setLaunchDetails("royal_rush_tooltip");
		}
		else
		{
			Bugsnag.LeaveBreadcrumb("RoyalRushTooltipController::onGetRegisterInfo - Missing the game we want to launch. Game was " + optionReference.rushInfo.gameKey);
			onClickClose();
		}
		Overlay.instance.hideShroud();
		// Set to false so we dont confuse users.
		gameObject.SetActive(false);
	}

	private void resetUIPositions()
	{
		// The safest reset ever coded.
		if (this != null && gameObject != null)
		{
			if (gameObject.transform.parent != null && gameObject.transform.parent.gameObject != null)
			{
				GameObject parent = gameObject.transform.parent.gameObject;

				if (originalParent != null)
				{
					lobbyOptionTransform.parent = originalParent.transform;

					//put it back
					lobbyOptionTransform.localPosition = originalLocation;

					// Set the lobby card this is on and all of its children to what it was
					CommonGameObject.setLayerRecursively(lobbyOptionTransform.gameObject, Layers.ID_NGUI);
				}
			}
			Destroy(gameObject);
		}
		// Else : We're already gone. Maybe this got called after we entered a game?
	}

	void OnDestroy()
	{
		if (LobbyCarousel.instance != null && LobbyCarousel.instance.pageScroller != null && !LobbyCarousel.instance.pageScroller.canSwipe)
		{
			LobbyCarousel.instance.pageScroller.canSwipe = true;
		}

		if (optionReference != null)
		{
			optionReference.isInFTUE = false;
		}
		Scheduler.removeFunction(RoyalRushEvent.instance.playRoyalRushFTUE);
		instance = null;
	}

	private void objectLoadFailure(string assetPath, Dict data = null)
	{
		Debug.LogError("PartnerPowerupCampaign::partnerPowerIconLoadFailure - Failed to load asset at: " + assetPath);
	}

	public void onUnpause()
	{
		resetUIPositions();
		Overlay.instance.hideShroud();
	}
}

