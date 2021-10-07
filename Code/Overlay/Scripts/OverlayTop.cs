using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.HitItRich.EUE;
using Com.Scheduler;
using TMPro;

/*
Abstract base class for all SKU's top Overlay classes.
*/

public abstract class OverlayTop : MonoBehaviour
{
	private const float BASE_ASPECT_RATIO = 1.33f;
	private const float SMALL_DEVICE_SCALE = 1.15f;

	public UISprite background;
	public GameObject lobbyButton;
	public TextMeshPro creditsTMPro;
	public XPUI xpUI;
	public Transform creditsMeterBG;
	public GameObject content;

	[SerializeField] public BoxCollider2D reelsBoundsLimit;

	private long creditsDisplayAmount = 0;

	protected bool didInitialize = false;
	protected float xpMeterAnchorOffset = 0.0f;		// The default UIAnchor offset when no lobby or special offer button is shown.

	private Dictionary<UIAnchor, bool> anchorEnabledMap = null;
	
	public enum SlideOutDir
	{
		Left = 0,
		Right = 1,
		Up = 2,
		Down = 3,
	}

	protected virtual void Awake()
	{
		setVIPInfo();

		if (!EUEManager.isEnabled || !EUEManager.shouldDisplayFirstLoadOverlay)
		{
			updateCredits(false);	
		}
		updateInboxCount();

		didInitialize = true;

		adjustForResolution();

		reelsBoundsLimit.enabled = false;

		registerEvents();
	}

	public void registerEvents()
	{
		Server.registerEventDelegate("inbox_items_event", onInboxUpdate, true);
		Server.registerEventDelegate("inbox_handled_event", onItemHandled, true);
	}

	public virtual void unregisterEvents()
	{
		Server.unregisterEventDelegate("inbox_items_event", onInboxUpdate, true);
		Server.unregisterEventDelegate("inbox_handled_event", onItemHandled, true);
	}

	private void onInboxUpdate(JSON data)
	{
		updateInboxCount();
	}

	private void onItemHandled(JSON data)
	{
		updateInboxCount();
	}

	// Called whenever the screen resolution changes.
	public void resolutionChangeHandler()
	{
		adjustForResolution();
	}
	
	// Adjust the width of some elements to fill the space for wider aspect ratios.
	public virtual void adjustForResolution()
	{
	}
	
	protected virtual float scaleAdjust
	{
		get { return 1.0f; }
	}
	
	// Called by the Overlay, which is why update is lower case.
	public virtual void update()
	{
		xpUI.update();
				
		if ((SpinPanel.instance != null && !SpinPanel.instance.isAutoSpinCountPanelActive) &&
			MainLobby.instance == null &&
			lobbyButton.activeInHierarchy &&
			FreeSpinGame.instance == null &&
			ChallengeGame.instance == null &&
			!Dialog.instance.isShowing
			)
		{
			AndroidUtil.checkBackButton(clickLobbyButton);
		}
	}

	// Shows or hides the top overlay from rendering.
	public void show(bool doShow)
	{
		if (this != null && gameObject != null)
		{
			gameObject.SetActive(doShow);

			if (doShow)
			{
				restorePosition();
			}
		}
	}

	public abstract void showLobbyButton();
	public abstract void hideLobbyButton();

	// Sets the VIP level text on the VIP button.
	// Called at startup and whenever the vip level changes.
	// MRCC -- Also now updates the fb connect label as this is also updated on vip level change.
	public virtual void setVIPInfo()
	{
	}

	// When disabling the sale notification, we need to reverse the credits meter shrinkage
	// that happened when the sale notification was enabled.
	// Called right after STUDActive.timerRange expires or becomes active
	public virtual void setupSaleNotification()
	{
	}
	
	// Updates the credits display amount whenever it changes.
	public void updateCredits(bool playCreditsRollupSound, bool shouldSkipOnTouch = true, float time = 0f, string rollupSoundOverride = "", string rollupSoundTermOverride = "")
	{
		if (SlotsPlayer.instance == null)
		{
			return;
		}

		// Roll to the new amount.
		long oldAmount = creditsDisplayAmount;
		long newAmount = SlotsPlayer.creditAmount;

		if (newAmount < 0)
		{
			Debug.LogError("Credits are going negative, which shouldn't happen.");
			newAmount = 0; //Prevent the credits from going into the negatives
		}

		if (newAmount < oldAmount || oldAmount == 0)
		{
			// If losing credits or setting it from 0, the change immediate instead of rolling.
			creditsTMPro.text = CreditsEconomy.convertCredits(newAmount);
			creditsDisplayAmount = newAmount;
			return;
		}
		// Do the roll up.
		if (SlotBaseGame.instance != null && SlotBaseGame.instance is TumbleSlotBaseGame)
		{
			float rollupTime = ((TumbleSlotBaseGame)SlotBaseGame.instance).getBaseRollUpTime(oldAmount, newAmount);
			StartCoroutine(SlotUtils.rollup(oldAmount, newAmount, updateCreditsRoll, playCreditsRollupSound, rollupTime, rollupOverrideSound:rollupSoundOverride, rollupTermOverrideSound:rollupSoundTermOverride));
		}
		else
		{
			if (this.gameObject.activeSelf)
			{
				StartCoroutine(SlotUtils.rollup(oldAmount, newAmount, updateCreditsRoll, playCreditsRollupSound, time, shouldSkipOnTouch, rollupOverrideSound: rollupSoundOverride, rollupTermOverrideSound: rollupSoundTermOverride));
			}
			else
			{
				// for LikelyToLapse Lobby Selection Mode, overlay is disabled, so cant run a coroutine off it.
				// just set the text instantly
				updateCreditsRoll(newAmount);
			}
		}

	}

	// Rollup callback function. Called in each frame update during the rollup.
	private void updateCreditsRoll(long value)
	{
		creditsDisplayAmount = value;
		creditsTMPro.text = CreditsEconomy.convertCredits(value);
	}

	private void clickVIPButton()
	{
		if (!VIPLevelUpDialog.showWelcomeIfNecessary())
		{
			VIPDialog.showDialog();
		}
		StatsManager.Instance.LogCount("top_nav", "vip_program", "", "", "", "click");
	}
	
	public void clickBuyCredits(Dict args = null)
	{
		if (SlotBaseGame.instance != null && SlotBaseGame.instance.hasAutoSpinsRemaining)
		{
			return;
		}

		if (ExperimentWrapper.FirstPurchaseOffer.isInExperiment)
		{
			StatsManager.Instance.LogCount(counterName: "dialog", 
				kingdom: "buy_page_v3", 
				phylum:"auto_surface", 
				klass: "first_purchase_offer",
				genus: "view");
		}

		BuyCreditsDialog.showDialog(priority:SchedulerPriority.PriorityType.IMMEDIATE);

		StatsManager.Instance.LogCount("top_nav", "buy_coins_button", "", "", "", "click");
	}

	// NGUI button callback.
	protected void onClickLobbyButton(Dict args = null)
	{
		clickLobbyButton();
		string kingdom = "back";
		StatsManager.Instance.LogCount("top_nav", kingdom, "", "", "", "click");
	}

	public void clickLobbyButton()
	{
		if (VIPLobby.instance != null)
		{
			// We use the lobby button to return to the main lobby when in the high limit lobby.
			VIPLobby.instance.backClicked();
			return;
		}

		if (MaxVoltageLobbyHIR.instance != null)
		{
			MaxVoltageLobbyHIR.instance.backClicked();
			return;
		}

		if (ChallengeLobby.instance != null)
		{
			ChallengeLobby.instance.backClicked();
			return;
		}
		
		Overlay.instance.hideShroud();

		bool wasInGame = GameState.game != null;
	
		// Since this can be called from the canvas, we need to do some validation that it's ok to do something.
		if (!Glb.isNothingHappening || GameState.isMainLobby)
		{
			return;
		}

		GameState.pop();

		if (wasInGame)
		{
			NGUIExt.disableAllMouseInput();
			Loading.show(Loading.LoadingTransactionTarget.LOBBY);
			Glb.loadLobby();
		}

		// HIR-6846.  If the slot music is faded out, then stop it
		// to make sure it doesn't play for a second on the way back to the lobby.
		if (Audio.currentMusicPlayer != null && Audio.currentMusicPlayer.isPlaying)
		{
			if (Audio.currentMusicPlayer.relativeVolume < 0.01f)
			{
				Audio.switchMusicKeyImmediate("");
			}
		}

		if (EUEManager.isEnabled && !EUEManager.shouldDisplayChallengeIntro)
		{
			StatsManager.Instance.LogCount("game_actions", "machine_ftue", "return_to_lobby","", "", "click");
		}
		
		Audio.stopAll();
		Audio.removeDelays();
		Audio.listenerVolume = Audio.maxGlobalVolume;
		Audio.play("return_to_lobby");
		Overlay.instance.setBackingSpriteVisible(false);
		
	}

	// NGUI button callback.
	private void HIRLogoClicked()
	{
		Bugsnag.LeaveBreadcrumb("Clicked the HIRLogo button");
		StatsManager.Instance.LogCount("top_nav", "hir_icon", "", "", "", "click");

		if (VIPLobby.instance != null)
		{
			VIPLobby.instance.backClicked();
		}
		else if (GameState.isMainLobby)
		{
			Debug.LogWarning("clickHIRLogo main lobby got into a spot that wasn't planned");
		}
		else
		{
			Debug.LogWarning("clickHIRLogo got into a spot that wasn't planned");
		}
	}

	// NGUI button callback.
	protected void inboxTopClicked(Dict args = null)
	{
		inboxButtonClicked("top_nav");
	}
	
	// Either the top or the lobby inbox button was clicked. They both do the same thing.
	public void inboxButtonClicked(string tracking)
	{
		if (!Scheduler.hasTaskOfType<InboxTask>())
		{
			string kingdom = "gifts";
			StatsManager.Instance.LogCount(tracking, kingdom, "", "", "", "click");
			Scheduler.addTask(new InboxTask());
		}
	}

	// NGUI button callback.
	protected void settingsClicked()
	{
		HelpDialog.showDialog();
		StatsManager.Instance.LogCount("top_nav", "settings", "", "", "", "click");

		Audio.play("minimenuopen0");
	}
		
	// Sets top buttons enabled or disabled.
	public virtual void setButtons(bool isEnabled)
	{
		lobbyButton.SetActive(isEnabled);
	}

	// Whenever the inbox count changes this should be called to update the number label.
	public virtual int updateInboxCount()
	{
		int inboxCount = InboxInventory.totalActionItems(false, CampaignDirector.richPass != null && CampaignDirector.richPass.isActive);

		if (MainLobby.instance != null && MainLobby.instance.inboxCountLabel != null)
		{
			bool isInboxActive = inboxCount > 0;

			if (MainLobby.instance.inboxCountLabel.transform.parent != null)
			{
				MainLobby.instance.inboxCountLabel.transform.parent.gameObject.SetActive(isInboxActive);
			}
			if(isInboxActive)
			{
				MainLobby.instance.inboxCountLabel.text = CommonText.formatNumber(inboxCount);
			}
		}
		return inboxCount;
	}

	// Slides the top bar off the screen
	// isWingsDistance -	Pass true to when sliding left or right will slide the entire wing width distance,
	//						else it only slides the screen width distance.
	public IEnumerator slideOut(SlideOutDir direction, float duration, bool isWingsDistance)
	{
		if (anchorEnabledMap == null)
		{
			anchorEnabledMap = CommonGameObject.getUIAnchorEnabledMapForGameObject(gameObject);
		}

		CommonGameObject.disableUIAnchorsForGameObject(gameObject);

		float leftRightDistance = NGUIExt.effectiveScreenWidth;
		Vector3 targetPosition = Vector3.zero;

		switch (direction)
		{
			case SlideOutDir.Left:
				targetPosition.x = -leftRightDistance;
				break;
			case SlideOutDir.Right:
				targetPosition.x = leftRightDistance;
				break;
			case SlideOutDir.Up:
				targetPosition.y = NGUIExt.effectiveScreenHeight;
				break;
			case SlideOutDir.Down:
				targetPosition.y = -NGUIExt.effectiveScreenHeight;
				break;
		}

		if (duration > 0.0f)
		{
			yield return new TITweenYieldInstruction(iTween.MoveTo(gameObject, iTween.Hash("position", targetPosition, "time", duration, "islocal", true, "easetype", iTween.EaseType.linear)));
		}
		else
		{
			// something seems to go wrong if we use a duration of 0 now where the tween doesn't finish, so let's just set the position directly
			gameObject.transform.localPosition = targetPosition;
		}
	}

	// Use this to put the top bar back in the right place after it moves
	public void restorePosition()
	{
		iTween.Stop(gameObject);
		transform.localPosition = Vector3.zero;
		CommonGameObject.restoreUIAnchorActiveMapToGameObject(gameObject, anchorEnabledMap);
		anchorEnabledMap = null;
	}

	void OnDestroy()
	{
		unregisterEvents();
	}

	public Bounds getBounds(OverlayJackpotMystery jackpotMystery, GameObject jackpotMysteryAnchor, bool isFreeSpins)
	{
		bool isFeatureWithExtraUIEnabled = false;
		Bounds extraUIBounds = new Bounds(Vector3.zero, Vector3.zero);

		// First we need to figure out if a token bar or feature is turned on which will extend the bounds
		if (jackpotMystery != null)
		{
			// check if we have a non-token feature
			GameOverlayFeatureDisplay enabledFeature = jackpotMystery.getActiveFeatureDisplay();
			if (enabledFeature != null)
			{
				isFeatureWithExtraUIEnabled = true;
				extraUIBounds = enabledFeature.getBounds();
			}
			else
			{
				// check if there is a token bar feature on
				if (jackpotMystery.tokenBar != null && jackpotMystery.tokenAnchor.activeSelf)
				{
					isFeatureWithExtraUIEnabled = true;
					extraUIBounds = jackpotMystery.tokenBar.getBounds();
				}
			}
		}

		if (!isFreeSpins && isFeatureWithExtraUIEnabled)
		{
			// If we didn't detect a size for the feature, but we have a feature then
			// check if jackpotMystery has a default size for features we can use.
			if (extraUIBounds.size == Vector3.zero && jackpotMystery.defaultFeatureReelsBoundsLimit != null)
			{
				extraUIBounds = jackpotMystery.defaultFeatureReelsBoundsLimit.bounds;
			}

			// If we didn't have a custom collider or default to fall back to we can use the code
			// below which will just use two times the overlays bounds, which could be a little big,
			// but should prevent the feature from covering the game.
			if (extraUIBounds.size == Vector3.zero)
			{
				reelsBoundsLimit.enabled = true;
				Bounds finalBounds = reelsBoundsLimit.bounds;
				reelsBoundsLimit.enabled = false;

				finalBounds.center = new Vector3(finalBounds.center.x, finalBounds.center.y - finalBounds.extents.y, finalBounds.center.z);
				finalBounds.extents = new Vector3(finalBounds.extents.x, finalBounds.extents.y * 2, finalBounds.extents.z);

				return finalBounds;
			}
			else
			{
				// We need to combine the feature bounds and the standard bounds for OverlayTop together to get a final bounds
				reelsBoundsLimit.enabled = true;
				Bounds finalBounds = reelsBoundsLimit.bounds;
				reelsBoundsLimit.enabled = false;
				finalBounds.Encapsulate(extraUIBounds);
				return finalBounds;

			}
		}
		else
		{
			// No feature is enabled so we can just use the standard bounds where they normally are
			reelsBoundsLimit.enabled = true;
			Bounds finalBounds = reelsBoundsLimit.bounds;
			reelsBoundsLimit.enabled = false;
			return finalBounds;
		}
	}

	public Vector2 getOverlayReelSize()
	{
		return new Vector2(reelsBoundsLimit.gameObject.transform.localPosition.y, reelsBoundsLimit.gameObject.transform.localScale.y);
	}
}
