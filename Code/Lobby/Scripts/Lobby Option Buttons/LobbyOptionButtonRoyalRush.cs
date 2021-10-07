﻿﻿﻿﻿﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Controls UI behavior of a menu option button related to Royal Rush
*/

public class LobbyOptionButtonRoyalRush : LobbyOptionButton
{
	// These get set when the FIRST option gets initialized for royal rush. They get cleared by MainLobbyHIR after we do
	// or don't do the FTUE. I wish I could just do a find on it, but this is fine I think.
	public static int ftuePage = -1;
	public static int pinnedLoc = -1;
	public static LobbyOptionButtonRoyalRush ftueButton;

	public Animator stateAnimator;
	public RoyalRushInfo rushInfo;

	// This class handles multiple states, so we'll want to be able to easily transition between them
	// without re-setting up the lobby
	public Renderer backgroundRenderer;
	public Renderer gameRenderer;
	public FacebookFriendInfo firstPlaceInfo;
	public FacebookFriendInfo rulerInfo;
	public TextMeshPro timerText;
	public TextMeshPro lockText;
	public Texture rankedCardBackground;
	public Texture unRankedCardBackground;
	public GameObject tooltipParent;
	
	public bool isInFTUE = false;

	// If below the minium level
	private const string LOBBY_CARD_LOCKED = "Lobby Card Locked";

	// If we're in a sprint or have info about a sprint we were in
	private const string LOBBY_CARD_RANKED = "Lobby Card Ranked";

	// If we habven't reg'd yet more or less.
	private const string LOBBY_CARD_NON_RANKED = "Lobby Card Non Ranked";

	// We transition to this state if/when we have ruler info
	private const string LOBBY_CARD_RULER = "Lobby Card Ruler";

	private bool isCylcing = false;

	private GameTimerRange timeUntilNoRegistrations;
	[SerializeField] private GameObject parentContainer;

	public override void setup(LobbyOption option, int page, float width, float height)
	{
		base.setup(option, page, width, height);
		refresh();
		setupCabinet();
	}

	// When the rush info updates, it's important to know.
	private void onRushInfoChanged(Dict args = null)
	{
		if (option == null)
		{
			Debug.LogError("Option was null when rushinfo changed for royal rush");
			return;
		}
		if (option.game == null)
		{
			Debug.LogError("LobbyOptionButtonRoyalRush::onRushInfoChanged - option.game was null when rushinfo changed for royal rush");
			return;
		}

		rushInfo = RoyalRushEvent.instance.getInfoByKey(option.game.keyName);

	
		setupCabinet();
	}
	
	private void setupCabinet(bool shouldDestroy = true)
	{
		if (shouldDestroy && gameObject != null)
		{
			DisposableObject.register(gameObject);
		}
		else
		{
			// Race condition for deleting. Avoid it here.
			return;
		}

		if (rushInfo.rushFeatureTimer != null)
		{
			// Make sure we're feeling fresh
			if (timeUntilNoRegistrations != null)
			{
				timeUntilNoRegistrations.clearEvent();
				timeUntilNoRegistrations.clearLabels();
			}

			timeUntilNoRegistrations = new GameTimerRange(GameTimer.currentTime, GameTimer.currentTime + (rushInfo.rushFeatureTimer.timeRemaining - RoyalRushEvent.minTimeRequired));

			if (!timeUntilNoRegistrations.isExpired)
			{
				// Need a colon and a space.
				timerText.text = Localize.text("closes_in") + ": ";
				timeUntilNoRegistrations.registerLabel(timerText, keepCurrentText: true);

				// This should fire when there isn't enough time to play a full sprint
				timeUntilNoRegistrations.registerFunction(onCloseRegistration);
				rushInfo.rushFeatureTimer.registerFunction(onFeatureTimeout);
			}
			else if (rushInfo.rushFeatureTimer.timeRemaining > 0)
			{
				timerText.text = Localize.text("royal_rush_no_new_entries");
				rushInfo.rushFeatureTimer.registerFunction(onFeatureTimeout);
			}
			else
			{
				timerText.text = Localize.text("event_over");
			}
		}

		string imagePath = SlotResourceMap.getLobbyImagePath(option.game.groupInfo.keyName, option.game.keyName);
		RoutineRunner.instance.StartCoroutine(DisplayAsset.loadTextureFromBundle(imagePath, imageTextureLoaded, skipBundleMapping:true, pathExtension:".png"));

		bool isLocked = SlotsPlayer.instance.socialMember.experienceLevel < RoyalRushEvent.minLevel;

		if (gameRenderer != null && gameRenderer.material != null)
		{
			gameRenderer.material.color = isLocked ? Color.gray : Color.white;
		}

		// We should check if the game is locked in some way before going further. If so just setup around that state.
		if (isLocked)
		{
			lockText.text = CommonText.formatNumber(RoyalRushEvent.minLevel);
			backgroundRenderer.material.mainTexture = unRankedCardBackground;
			stateAnimator.Play(LOBBY_CARD_LOCKED);
			return;
		}

		switch (rushInfo.currentState)
		{
			// No way we have info
		case RoyalRushInfo.STATE.AVAILABLE:
			// State without a winner, and we can register
			stateAnimator.Play(LOBBY_CARD_NON_RANKED);
			break;

			// In a state where we may or may not have info
		case RoyalRushInfo.STATE.PAUSED:
		case RoyalRushInfo.STATE.SPRINT:
		case RoyalRushInfo.STATE.STARTED:
			stateAnimator.Play(LOBBY_CARD_RANKED);
			break;

			// Have some info for sure.  Same as in progress but clicking "play now" or whatever should just show stats dialog.
		case RoyalRushInfo.STATE.COMPLETE:	
			stateAnimator.Play(LOBBY_CARD_NON_RANKED);
			break;
		
		case RoyalRushInfo.STATE.UNAVAILABLE:
			stateAnimator.Play(LOBBY_CARD_NON_RANKED);
			timeUntilNoRegistrations.removeLabel(timerText);
			timerText.text = Localize.text("royal_rush_no_new_entries");
			break;

			// Lets be safe.
		default:
			stateAnimator.Play(LOBBY_CARD_NON_RANKED);
			Debug.LogWarning("LobbyOptionButtonRoyalRush::setupCabinet - No state on the rush info. Not setup yet or just an unhandled state: " + rushInfo.currentState.ToString());
			break;
		}

		if (!isCylcing)
		{
			RoutineRunner.instance.StartCoroutine(cycleAnimations());
			isCylcing = true;
		}

		backgroundRenderer.material.shader = LobbyOptionButtonActive.getOptionShader(true);
	}

	private void onFeatureTimeout(Dict args = null, GameTimerRange parentTimer = null)
	{
		timerText.text = Localize.text("event_over");
	}

	private void onCloseRegistration(Dict args = null, GameTimerRange parentTimer = null)
	{
		timerText.text = Localize.text("royal_rush_no_new_entries");
	}

	private IEnumerator cycleAnimations()
	{
		// We check if this instance of the class is null and the rush info all the time in case 
		// something would cause it to be deleted or go otherwise go null between animation states.
		while (this != null)
		{
			// If the card is here and we have rush info and we either have user info, or if we have the user list but no user info.
			if (rushInfo != null && this != null && (rushInfo.userInfos == null || rushInfo.userInfos.Count <= 0))
			{
				stateAnimator.Play(LOBBY_CARD_NON_RANKED);
				backgroundRenderer.material.mainTexture = unRankedCardBackground;
				yield return new WaitForSeconds(6f);
			}

			// If the card is here and we have rush info and a populated user info list
			if (rushInfo != null && this != null && rushInfo.userInfos != null && rushInfo.userInfos.Count > 0)
			{
				// If we have users
				if (firstPlaceInfo.member == null || firstPlaceInfo.member.zId != rushInfo.userInfos[0].zid)
				{
					RoyalRushUser firstPlaceUser = rushInfo.userInfos[0];
					SocialMember royalUser = CommonSocial.findOrCreate(
						fbid: firstPlaceUser.fbid,
						zid: firstPlaceUser.zid,
						firstName: firstPlaceUser.name);
					firstPlaceInfo.member = royalUser as SocialMember;
				}

				stateAnimator.Play(LOBBY_CARD_RANKED);
				backgroundRenderer.material.mainTexture = rankedCardBackground;
				yield return new WaitForSeconds(3f);
			}

			// If we have a previous winner, show it
			if (rushInfo != null && this != null && rushInfo.previousWinner != null)
			{
				if (rulerInfo.member == null || rulerInfo.member.zId != rushInfo.previousWinner.zid)
				{
					SocialMember ruler = CommonSocial.findOrCreate(rushInfo.previousWinner.fbid, rushInfo.previousWinner.zid);
					rulerInfo.member = ruler as SocialMember;
					rulerInfo.gameObject.SetActive(true);
				}

				stateAnimator.Play(LOBBY_CARD_RULER);
				backgroundRenderer.material.mainTexture = rankedCardBackground;
				yield return new WaitForSeconds(3f);
			}
		}

		// Quality of life
		yield return null;
	}

	protected override void OnClick()
	{
		if (SlotsPlayer.instance.socialMember.experienceLevel < RoyalRushEvent.minLevel || rushInfo == null || rushInfo.rushFeatureTimer == null || option == null || option.game == null)
		{
			if (rushInfo == null)
			{
				Debug.LogError("LobbyOptionButtonRoyalRush::OnClick() - Missing rush info for royal rush");
			}
			if (option == null)
			{
				Debug.LogError("LobbyOptionButtonRoyalRush::OnClick() - option for royal rush");
			}
			if (option.game == null)
			{
				Debug.LogError("LobbyOptionButtonRoyalRush::OnClick() - Missing option.game for royal rush");
			}

			return;
		}

		if (rushInfo.registrationIsLocked)
		{
			return;
		}
		
		if (rushInfo.currentState == RoyalRushInfo.STATE.AVAILABLE && timeUntilNoRegistrations != null && !timeUntilNoRegistrations.isExpired)
		{
			loadTooltip();
		}

		option.game.askInitialBetOrTryLaunch();

		SlotAction.setLaunchDetails("royal_rush", option.lobbyPosition);
		
		if (StatsManager.Instance != null)
		{
			StatsManager.Instance.LogCount("lobby", "", "royal_rush_banner", option.game.keyName, "royal_rush", "click");
		}

		if (Overlay.instance != null && Overlay.instance.jackpotMysteryHIR != null)
		{
			if (Overlay.instance.jackpotMysteryHIR.tokenBar != null)
			{
				Overlay.instance.jackpotMysteryHIR.setUpTokenBar(option.game.keyName);
			}
		}
	}

	// Specific to the game renderer
	private void imageTextureLoaded(Texture2D tex, Dict data = null)
	{
		if (gameRenderer != null && tex != null)
		{
			gameRenderer.material.shader = LobbyOptionButtonActive.getOptionShader(true);
			gameRenderer.material.mainTexture = tex;

			if (gameRenderer != null && gameRenderer.material != null)
			{
				gameRenderer.material.color = SlotsPlayer.instance.socialMember.experienceLevel < RoyalRushEvent.minLevel ? Color.gray : Color.white;
			}

		}
		else if (gameRenderer == null && this != null)
		{
			Debug.LogError("LobbyOptionButtonRoyalRush: image was null");
		}
		else if (this != null)
		{
			Debug.LogError("LobbyOptionButtonRoyalRush::imageTextureLoaded - downloaded texture was null!");
		}
	}

	private void onLoadImageFail(string assetPath, Dict data = null)
	{
		Debug.LogError("LobbyOptionButtonRoyalRush::onLoadImageFail - Failed to load background at " + assetPath);
	}

	public void loadTooltip(bool isFTUE = false)
	{
		if (this != null && gameObject != null && RoyalRushEvent.instance != null)
		{
			isInFTUE = isFTUE;
			RoyalRushEvent.instance.loadTooltip(tooltipParent, isFTUE);
		}
	}

	public void designateGame(string keyName)
	{
		rushInfo = RoyalRushEvent.instance.getInfoByKey(keyName);

		// Always remove first. If the info got hot swapped for some reason, the old one
		// wont be getting listened to now.
		rushInfo.onEndRush -= onRushInfoChanged;
		rushInfo.onGetInfo -= onRushInfoChanged;

		rushInfo.onEndRush += onRushInfoChanged;
		rushInfo.onGetInfo += onRushInfoChanged;
	}

	public override void refresh()
	{
		setupCabinet();
		base.refresh();
	}

	private void OnDestroy()
	{
		if (timeUntilNoRegistrations != null)
		{
			timeUntilNoRegistrations.clearEvent();
			timeUntilNoRegistrations.clearLabels();
		}

		rankedCardBackground = null;
		unRankedCardBackground = null;
		if (rushInfo != null)
		{
			rushInfo.onEndRush -= onRushInfoChanged;
			rushInfo.onGetInfo -= onRushInfoChanged;
		}
	}
}
