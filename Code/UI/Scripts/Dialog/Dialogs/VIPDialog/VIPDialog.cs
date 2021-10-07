using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

/*
Controls display and behavior of the new VIP dialog.
*/

public class VIPDialog : DialogBase
{
	private const float BENEFITS_TRANSITION_TIME = 0.25f;
	private const float BENEFITS_START_X = -542.0f;
	private const float BENEFITS_START_Y = 96.0f;
	private const float BENEFITS_START_SCALE = 0.2f;
	
	public TextMeshPro titleLabel;		// Need to manually set this depending on the situation of viewing this dialog.
	public GameObject introducingParent;
	public VIPDialogGamePanel earlyAccessPanel;
	public GameObject gamePanelTemplate;

	public FacebookFriendInfo friendInfo;
	public TextMeshPro statusLabel;
	public TextMeshPro pointsLabel;
	public GameObject visitVIPRoomButton;
	public Transform playerInfoTextSizer;
	
	public GameObject overviewParent;
	public GameObject benefitsParent;
	public PageScroller benefitsPageScroller;
	public GameObject returnButton;

	public GameObject VIPGames4;
	public UIGrid VIPGames4Grid;
	public GameObject VIPGames5;
	public UIGrid VIPGames5Grid;
	public GameObject linkedVIPBadge;
	public VIPIconHandler vipIcon;

	/// Initialization
	public override void init()
	{
		bool isIntro = (bool)dialogArgs.getWithDefault(D.OPTION, false);
		
		if (isIntro)
		{
			titleLabel.text = Localize.textUpper("vip_info_title_intro");
		}
		else
		{
			introducingParent.SetActive(false);
			titleLabel.text = Localize.textUpper("vip_info_title");
		}
		
		VIPLevel level = VIPLevel.find(SlotsPlayer.instance.vipNewLevel, "vip_room_games");

		statusLabel.text = Localize.text("vip_member_{0}", level.name);
		pointsLabel.text = Localize.text("vip_points_{0}", CommonText.formatNumber(SlotsPlayer.instance.vipPoints));


		if (VIPLobby.instance != null)
		{
			// Already in the high limit room, so hide the button that goes there.
			visitVIPRoomButton.SetActive(false);
		}

		// Create the panels for up to the maximum allowed games.
		int panelCount = 0;
		
		// Get the early access game.
		LobbyGame eaGame = LobbyGame.vipEarlyAccessGame;

		// Make a simple list of games to show.
		List<LobbyGame> games = new List<LobbyGame>(VIPLevel.allGames);

		// The first element in that list is always the early access game, or null if there isn't one.
		// Remove it since we don't want it to count toward our game count.
		games.RemoveAt(0);

		UIGrid gameGrid;
		
		if (games.Count >= 5)
		{
			// If 5 or more games to show, then use the grid that supports 5 games.
			// This is regardless of experiment, since the VIP Level game data
			// is defined independent of experiments.
			VIPGames5.SetActive(true);
			VIPGames4.SetActive(false);
			gameGrid = VIPGames5Grid;
		} 
		else 
		{
			// If 4 or less games to show, use the grid that supports 4 games and shows the early access game.
			VIPGames5.SetActive(false);
			VIPGames4.SetActive(true);
			gameGrid = VIPGames4Grid;
			
			// Set up the early access game panel.
			earlyAccessPanel.setGame(eaGame);
		}
		int maxGameCount = gameGrid.maxPerLine; // The max number of game panels that can be created, due to space.

		foreach (LobbyGame game in games)
		{
			GameObject go = CommonGameObject.instantiate(gamePanelTemplate) as GameObject;
			go.transform.parent = gameGrid.transform;
			go.transform.localScale = Vector3.one;
			go.transform.localPosition = gamePanelTemplate.transform.localPosition;
			// Name it for sorting in the grid.
			go.name = string.Format("{0} {1}", game.vipLevel.levelNumber, game.name);
			VIPDialogGamePanel panel = go.GetComponent<VIPDialogGamePanel>();
			if(panel!=null)
				panel.setGame(game);
			panelCount++;
			if (panelCount == maxGameCount)
			{
				break;
			}
		}
		
		// Hide the panel template.
		gamePanelTemplate.SetActive(false);
		
		// Layout the grid of panels.
		gameGrid.Reposition();
		
		// Center the grid horizontally.
		CommonTransform.setX(gameGrid.transform, -0.5f * gameGrid.cellWidth * (panelCount - 1));

		overviewParent.SetActive(true);

		// Prepare the benefits page for transitioning.
		benefitsParent.transform.localPosition = new Vector3(BENEFITS_START_X, BENEFITS_START_Y, benefitsParent.transform.localPosition.z);
		benefitsParent.transform.localScale = new Vector3(BENEFITS_START_SCALE, BENEFITS_START_SCALE, 1.0f);
		benefitsParent.SetActive(false);
		
		benefitsPageScroller.init(VIPLevel.maxLevel.levelNumber + 1, createBenefitsPanel);

		friendInfo.member = SlotsPlayer.instance.socialMember;

		if ((bool)dialogArgs.getWithDefault(D.OPTION1, false))
		{
			// This option means we're going straight to the status & benefits page,
			// and we're removing the return to overview button to prevent confusion.
			returnButton.SetActive(false);
			showDetails(true);
		}
	
		SafeSet.gameObjectActive(linkedVIPBadge, LinkedVipProgram.instance.isEligible);

        vipIcon.setLevel(VIPLevel.getEventAdjustedLevel());

		// Play Menu Open Sound
		Audio.play("minimenuopen0");
		
		Audio.switchMusicKeyImmediate("idleHighLimitLobby");
		StatsManager.Instance.LogCount("dialog", "vip_overview", "", "", "", "view");
	}
	
	private void createBenefitsPanel(GameObject panel, int index)
	{
		VIPBenefitsPanel ben = panel.GetComponent<VIPBenefitsPanel>();
		ben.setVIPLevel(index);
	}
	
	// protected override void onFadeInComplete()
	// {
	// 	base.onFadeInComplete();
	// 	
	// 	if ((bool)dialogArgs.getWithDefault(D.OPTION1, false))
	// 	{
	// 		showDetails();
	// 	}
	// }

	void Update()
	{
		if (benefitsParent.activeSelf)
		{
			// Due to the weird nature of this particular page scroller,
			// use our own custom code to swipe for paging.
			if (TouchInput.didSwipeLeft && benefitsPageScroller.scrollPos < benefitsPageScroller.totalPanels)
			{
				benefitsPageScroller.scrollPos++;
			}
			else if (TouchInput.didSwipeRight && benefitsPageScroller.scrollPos > 0)
			{
				benefitsPageScroller.scrollPos--;				
			}
		}
		
		AndroidUtil.checkBackButton(closeClicked, "dialog", "vip", "", "", "", "back"); 
	}

	// NGUI button callback.
	private void closeClicked()
	{
		Dialog.close();
	}
	
	// NGUI button callback.
	private void visitClicked()
	{
		Dialog.close();
		LobbyLoader.returnToNewLobbyFromDialog(false, LobbyInfo.Type.VIP);
	}
	
	// NGUI button callback.
	private void detailsClicked()
	{
		showDetails(false);
	}
	
	// Show the "STATUS & BENEFITS" part of the dialog.
	private void showDetails(bool isInstant)
	{
		benefitsParent.SetActive(true);

		benefitsPageScroller.scrollPosQuietly = VIPLevel.getEventAdjustedLevel();

		StartCoroutine(tweenBenefits(true, isInstant));
	}

	// NGUI button callback.
	private void returnClicked()
	{
		StartCoroutine(tweenBenefits(false));
	}
	
	// iTween ValueTo callback.
	private void returnFinished()
	{
		benefitsParent.SetActive(false);
	}

	private void goToHelpLink()
	{
		LinkedVipProgram.instance.openHelpUrl();
	}
	
	private IEnumerator tweenBenefits(bool isShowing, bool isInstant = false)
	{
		if (isInstant)
		{
			benefitsParent.transform.localPosition = new Vector3(
				(isShowing ? 0 : BENEFITS_START_X),
				(isShowing ? 0 : BENEFITS_START_Y),
				benefitsParent.transform.localPosition.z
			);

			benefitsParent.transform.localScale = (isShowing ? Vector3.one : new Vector3(BENEFITS_START_SCALE, BENEFITS_START_SCALE, 1.0f));
		}
		else
		{
			iTween.MoveTo(benefitsParent, iTween.Hash(
				"x", (isShowing ? 0 : BENEFITS_START_X),
				"y", (isShowing ? 0 : BENEFITS_START_Y),
				"time", BENEFITS_TRANSITION_TIME,
				"islocal", true,
				"easetype", iTween.EaseType.linear
			));

			iTween.ScaleTo(benefitsParent, iTween.Hash(
				"scale", (isShowing ? Vector3.one : new Vector3(BENEFITS_START_SCALE, BENEFITS_START_SCALE, 1.0f)),
				"time", BENEFITS_TRANSITION_TIME,
				"islocal", true,
				"easetype", iTween.EaseType.linear
			));
				
			yield return new WaitForSeconds(BENEFITS_TRANSITION_TIME);
		}
		
		if (!isShowing)
		{
			benefitsParent.SetActive(false);
		}
		
		StatsManager.Instance.LogCount(
			"dialog",
			"vip_benefit",
			"",
			"",
			"",
			isShowing ? "view" : "close");
	}
		
	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		StatsManager.Instance.LogCount("dialog", "vip_overview", "", "", "", "close");
		// Do special cleanup.
		if (GameState.isMainLobby)
		{
			MainLobby.playLobbyMusic();
		}
		else
		{
			Audio.switchMusicKeyImmediate(Audio.soundMap("reelspin_base"));
		}
	}
	
	public static void showDialog(bool shouldGoDirectToStatusMode = false, bool isIntro = false)
	{
		Scheduler.addDialog("vip",
			Dict.create(
				D.OPTION, isIntro,
				D.OPTION1, shouldGoDirectToStatusMode
			)
		);
	}
}
