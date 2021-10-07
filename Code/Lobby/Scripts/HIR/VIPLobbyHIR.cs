using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Controls the VIP lobby for HIR 1.
*/

public class VIPLobbyHIR : VIPLobby
{
	private const float IPAD_ASPECT = 1.33f;
	
	public GameObject vipBackButton;
	public FacebookFriendInfo vipFriendInfo;
	public GameObject vipEarlyAccessParent;
	public GameObject vipPlayerNameLabel;
	public TextMeshPro vipPlayerStatusLabel;
	public TextMeshPro vipPlayerPointsLabel;
	public GameObject vipPlayerInfo;
	public PageScroller vipPageScroller;
	public GameObject transitionPrefab;
	public GameObject progressParent;
	public GameObject nextElementsParent; 
	public GameObject microVIPEventAnchor;

	// Used for the VIP status boost event
	public TextMeshPro nextText;
	public TextMeshPro currentText;

	protected override void Awake()
	{
		if (LinkedVipProgram.instance.isEligible)
		{
			vipIconIndexOffset = 2;
		}

		base.Awake();
	
		// Create the standalone early access game option, which isn't in the page scroller.
		GameObject go = NGUITools.AddChild(vipEarlyAccessParent, optionButtonPrefab);
		LobbyOptionButtonVIP button = go.GetComponent<LobbyOptionButton>() as LobbyOptionButtonVIP;
		
		if (earlyAccessOption == null)
		{
			// If for some reason we got to this case without an early access option setup properly, make a coming soon one.
			Debug.LogErrorFormat("VIPLobbyHIR.cs -- Awake -- earlyAccessOption was null, creating a early access coming soon option.");
			earlyAccessOption = new LobbyOption();
			earlyAccessOption.type = LobbyOption.Type.VIP_EARLY_ACCESS_COMING_SOON;
		}
		button.setup(earlyAccessOption);
		StartCoroutine(earlyAccessOption.loadImages());

		if (NGUIExt.aspectRatio > IPAD_ASPECT)
		{
			// If wider than iPad, then hide the bottom back button because there's no room for it.
			vipBackButton.SetActive(false);
		}

		vipFriendInfo.member = SlotsPlayer.instance.socialMember;
		if (SlotsPlayer.isAnonymous || NetworkProfileFeature.instance.isEnabled)
		{
			// MCC -- added a check for network profile enabled because we have a name and
			// picture that can be used in this case even for anonymous players.
			vipFriendInfo.member = SlotsPlayer.instance.socialMember;
		}
		else
		{
			// Hide the player's name and vertically center the other info if not connected to facebook.
			vipPlayerNameLabel.SetActive(false);
			CommonTransform.setY(vipPlayerInfo.transform, 0);
		}
		
		vipPageScroller.init(options.Count, onCreateVIPPagePanel);
		vipPageScroller.onStartDrag = showOffscreenVIPPanels;
		vipPageScroller.onBeforeScroll = showOffscreenVIPPanels;
		vipPageScroller.onAfterScroll = hideOffscreenVIPPanels;

		if (pageBeforeGame > -1)
		{
			// Return to the scroll position we were at before going into a game.
			vipPageScroller.scrollPosImmediate = pageBeforeGame;
		}
		else
		{
			// Dynamically determine the default scroll position.
			int seekLevel = SlotsPlayer.instance.vipNewLevel;
		
			if (highlightGameLevel > -1)
			{
				// Make sure a particular VIP level's game is being shown.
				seekLevel = highlightGameLevel;
				highlightGameLevel = -1;
			}
		
			// Figure out the highest unlocked VIP game, and scroll so that it's in view on the right.
			int highest = -1;
			for (int i = 0; i < options.Count; i++)
			{
				if (options[i].game != null && seekLevel >= options[i].game.vipLevel.levelNumber)
				{
					highest = i;
				}
			}
			
			vipPageScroller.scrollPosImmediate = Mathf.Max(0, highest - vipPageScroller.panelsPerRow + 1);
		}

		pageBeforeGame = vipPageScroller.scrollPos;

		hideOffscreenVIPPanels();	// Force the offscreen ones to be hidden immediately.

	    VIPLevel currentLevel = VIPLevel.find(SlotsPlayer.instance.vipNewLevel);
		progressParent.SetActive(true);
		nextElementsParent.SetActive(currentLevel != VIPLevel.maxLevel);

		vipNewIcons[0].gameObject.SetActive(!LinkedVipProgram.instance.isEligible);
		vipNewIcons[1].gameObject.SetActive(!LinkedVipProgram.instance.isEligible);
	
		vipNewIcons[2].gameObject.SetActive(LinkedVipProgram.instance.isEligible);
		vipNewIcons[3].gameObject.SetActive(LinkedVipProgram.instance.isEligible && currentLevel != VIPLevel.maxLevel);

		if (VIPStatusBoostEvent.isEnabled())
		{
			VIPStatusBoostEvent.loadVIPRoomAssets();
			SafeSet.componentGameObjectActive(nextText, false);
			SafeSet.componentGameObjectActive(currentText, false);
		}
	}
	
	protected override void benefitsClicked()
	{
		// Launch the VIP dialog and go straight to the benefits page of it.
		VIPDialog.showDialog();
	}

	// A VIP page panel has been created.
	private void onCreateVIPPagePanel(GameObject pagePanel, int index)
	{
		LobbyOption option = options[index];
		option.panel = pagePanel;
		LobbyOptionButtonVIP button = pagePanel.GetComponent<LobbyOptionButton>() as LobbyOptionButtonVIP;
		button.setup(option);
		StartCoroutine(option.loadImages());
	}
	
	// Called after scrolling VIP lobby ends each time, but before the panels are cleaned up.
	private void hideOffscreenVIPPanels()
	{
		// Hide any options that aren't visible, so we don't see the edge of the options out of view.
		// We have to do this due to the design of the VIP panels, which have those poles between options,
		// so there is no clean-cut way to hide the out of view panels without explicitly hiding them here.
		// This means we also need to re-show them when dragging or scrolling starts.
		vipPageScroller.forEachShownPanel(hideVIPPanel);

		pageBeforeGame = vipPageScroller.scrollPos;
	}
	
	// Hide the panel if it is out of view.
	private void hideVIPPanel(GameObject panel, int index)
	{
		if (index < vipPageScroller.scrollPos || index >= vipPageScroller.scrollPos + vipPageScroller.panelsPerRow)
		{
			panel.SetActive(false);
		}
	}
	
	// Make sure all panels are visible before scrolling.
	private void showOffscreenVIPPanels()
	{
		vipPageScroller.forEachShownPanel(showVIPPanel);
	}

	// Show the panel.
	private void showVIPPanel(GameObject panel, int index)
	{
		panel.SetActive(true);
	}

	public override VIPLevel refreshUI()
	{
		base.refreshUI();

		// refresh UI will take care of a lot of stuff on its own. 
		VIPLevel currentLevel = VIPLevel.find(SlotsPlayer.instance.vipNewLevel, "vip_room_games");
		
		vipPlayerStatusLabel.text = Localize.text("vip_member_{0}", currentLevel.name);
		vipPlayerPointsLabel.text = Localize.text("vip_points_{0}", CommonText.formatNumber(SlotsPlayer.instance.vipPoints));
		
		return currentLevel;
	}

	public override IEnumerator transitionToMainLobby()
	{
		yield return StartCoroutine(base.transitionToMainLobby());
		
		// Create the curtains transition.
		GameObject go = CommonGameObject.instantiate(transitionPrefab) as GameObject;
		LobbyCurtainsTransition curtains = go.GetComponent<LobbyCurtainsTransition>();
		
		yield return StartCoroutine(curtains.closeCurtains());

		Overlay.instance.top.hideLobbyButton();
		yield return StartCoroutine(LobbyLoader.instance.createMainLobby());

		// The curtains auto-destroy itself at the end of this function call.
		// Use RoutineRunner as the host instead of this object, since this object is being destroyed.
		RoutineRunner.instance.StartCoroutine(curtains.openCurtains());
		
		Destroy(gameObject);

		NGUIExt.enableAllMouseInput();

	}

	public override int getTrackedScrollPosition()
	{
		return vipPageScroller.page + 1;
	}
	
	new public static void resetStaticClassData()
	{
	}
}
