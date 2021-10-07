using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.HitItRich.Feature.VirtualPets;
using Com.Scheduler;
using TMPro;
using Zynga.Core.Util;


/**
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
**/

public abstract class HelpDialog : DialogBase
{
	public UISprite backgroundSprite;
	public GameObject facebookBonus;
	public GameObject facebookConnected;
	public GameObject facebookDisconnected;
	public GameObject playerPic;
	public MeshRenderer pic;
	public FacebookFriendInfo friendInfo;

	public TextMeshPro facebookActionLabel;
	public TextMeshPro playerNameLabel;

	public GameObject facebookButtonBlue;
	public GameObject facebookButtonPink;

	public GameObject zadeMoreGamesButton;
	public GameObject payTableButton;
	public GameObject rightSideButtons;

	public GameObject musicToggleRoot;
	public GameObject musicToggleOn;
	public GameObject musicToggleOff;
	public GameObject soundToggleRoot;
	public GameObject soundToggleOn;
	public GameObject soundToggleOff;
	public GameObject alertsToggleRoot;
	public GameObject alertsToggleOn;
	public GameObject alertsToggleOff;
	public GameObject perfToggleRoot;
	public GameObject perfToggleOn;
	public GameObject perfToggleOff;
	public GameObject collectableToggleRoot;
	public GameObject collectableToggleOn;
	public GameObject collectableToggleOff;
	public GameObject collectablesDivider;
	public GameObject petsAlertToggleRoot;
	public GameObject petsAlertToggleOn;
	public GameObject petsAlertToggleOff;

	public TextMeshPro copyrightLabel;
	public TextMeshPro versionLabel;
	public TextMeshPro ZIDConnectedLabel;
	public TextMeshPro ZIDDisconnectedLabel;
	public TextMeshPro ZIDLabel;

	public GameObject linkedVIPButton;          // The linked VIP button that appears in the same location as the ZADE MORE GAMES Button.
	public TextMeshPro linkedVIPButtonLabel;    // The label text for the main Linked VIP Button.
	public TextMeshPro seeStatusLabel;          // The label text for the main Linked VIP Button in see Status mode
	public GameObject linkedConnectObject;
	public GameObject seeStatusObject;

	public GameObject rateUsButton;             // The button to rate the app
	public GameObject notifPromptButton;        // The button to popup the notifications soft prompt.

	public ClickHandler onConnectButton;
	
	public UIGrid grid;
	public SlideController settingsScrollView;

	public int numberVisibleSettings = 4;

	// TODO MCC -- Only used in Helpv2, we need to update this whole dialog though to deprecate v1/remove SIR
	// and swap to ButtonHandlers.
	public TextMeshPro facebookLogoutLabel;

	private bool isBusy = false;    // If busy, don't process other button clicks.
	protected string ccpaURL = "";

	private const string ZADE_MORE_GAMES_SLOT_NAME = "MOB_HIR_I12_MGL"; // AdSlot name for the More Games Zade Ad

	/// Initialization
	public override void init()
	{
		if (SlotsPlayer.instance == null)
		{
			return;
		}

		if (LinkedVipProgram.instance.shouldPromptForConnect)
		{
			linkedVIPButton.SetActive(true);
			linkedVIPButtonLabel.text = Localize.textUpper("connect");

			SafeSet.gameObjectActive(seeStatusObject, false);
			SafeSet.gameObjectActive(linkedConnectObject, true);
		}
		else if (LinkedVipProgram.instance.isEligible)
		{
			linkedVIPButton.SetActive(true);
			linkedVIPButtonLabel.text = Localize.textUpper("status");

			if (seeStatusObject != null)
			{
				SafeSet.gameObjectActive(linkedConnectObject, false);
				SafeSet.gameObjectActive(seeStatusObject, true);
				SafeSet.labelText(seeStatusLabel, linkedVIPButtonLabel.text);
			}
		}
		else
		{
			linkedVIPButton.SetActive(false);
		}

		if (payTableButton != null)
		{
			payTableButton.SetActive(GameState.game != null);
		}

		// Turn on the More Games button only if we are not anti-targeting this user/device
		if (zadeMoreGamesButton != null)
		{
			zadeMoreGamesButton.SetActive(false); // Make sure that it is off by default.
		}

		if (notifPromptButton != null)
		{
#if UNITY_IPHONE
			notifPromptButton.SetActive(!NotificationManager.PushNotifsAllowed);
#else
			notifPromptButton.SetActive(false);
#endif
		}

		// Show/Hide the 'rate us' button based on if we have a URL to link to (WebGL doesn't)
		if (rateUsButton != null)
		{
			rateUsButton.SetActive(!string.IsNullOrEmpty(Glb.clientAppstoreURL));
		}

		// Set up volume and music settings?
		setMusic(!Audio.muteMusic);
		setSound(!Audio.muteSound);

		// Set up player alerts control
		setAlerts(PlayerPrefsCache.GetInt(Prefs.PLAYER_ALERTS, 1) != 0);

#if UNITY_WEBGL
		if (perfToggleRoot != null)
		{
			perfToggleRoot.SetActive(false);
		}
#else
		if (perfToggleOn != null && perfToggleOff != null)
		{
			// Set up performance mode
			setPerf(PlayerPrefsCache.GetInt(Prefs.PLAYER_PERF, 0) != 0);
		}
#endif

		if (Collectables.isActive())
		{
			collectablesDivider.SetActive(true);
			collectableToggleRoot.SetActive(true);
			// set right side buttons scaling to .9
			// set right side buttons pos up 50. (+50)
			if (collectableToggleOn != null && collectableToggleOff != null)
			{
				setCollectables(CustomPlayerData.getBool(CustomPlayerData.SHOW_COLLECT_ALERTS, true));
			}
		}

		if (petsAlertToggleRoot != null)
		{
			if (VirtualPetsFeature.instance != null && VirtualPetsFeature.instance.isEnabled)
			{
				petsAlertToggleRoot.SetActive(true);
				setPetsAlerts(!VirtualPetsFeature.instance.silentFeedPet);
			}
			else
			{
				petsAlertToggleRoot.SetActive(false);
			}
		}
		
		if (grid != null && settingsScrollView != null)
		{
			settingsScrollView.setBounds((grid.transform.childCount - numberVisibleSettings) * grid.cellHeight,0) ;
		}
		versionLabel.text = " v" + Glb.clientVersion;
		if (ZIDLabel != null)
		{
			ZIDLabel.text = "ZID " + SlotsPlayer.instance.socialMember.zId;
		}
	}

	protected override void Start()
	{
		base.Start();

		friendInfo.member = SlotsPlayer.instance.socialMember;
		// Facebook authenticated:
		if (SlotsPlayer.isFacebookUser || SlotsPlayer.IsAppleLoggedIn || SlotsPlayer.IsEmailLoggedIn)
		{
			if (ExperimentWrapper.Zis.isInExperiment)
			{
				Debug.LogFormat("AppleLogin: zid zdkmanager {0} zid socialmember {1}", ZdkManager.Instance.Zid, friendInfo.member.zId);
				// This means that the user has already associated his FB zid to ZIS player id
				// so logged into SIWA first and then connected to FB and then logged out and then 
				// connected to FB.

				ZIDConnectedLabel.text = "ZID " + PackageProvider.Instance.Authentication.Flow.Account.GameAccount.PlayerId.ToString();

				facebookDisconnected.SetActive(false);
				facebookConnected.SetActive(true);
				DisplayAsset.loadTextureToRenderer(pic, friendInfo.member.getImageURL, shouldShowBrokenImage:false);
			}
			else
			{
				if (facebookBonus != null)
				{
					facebookBonus.SetActive(false);
				}
			}
			SafeSet.gameObjectActive(facebookButtonBlue, false);
			SafeSet.gameObjectActive(facebookButtonPink, true);

			// We have the full player info, so fill it in here:
			if (playerPic != null)
			{
				playerPic.SetActive(true);
			}
#if UNITY_WEBGL
			if (playerNameLabel != null)
			{
				playerNameLabel.SetText(friendInfo.member.fullName);
			}
			// Complicated way to make the FB login/logout button disappear.
			if (facebookActionLabel != null)
			{ 
				facebookActionLabel.gameObject.transform.parent.gameObject.SetActive(false);
			}
			if (facebookLogoutLabel != null)
			{
				// Only exists in v2.
				facebookLogoutLabel.gameObject.SetActive(false);
			}
#else
			// Set the Facebook "log out" button text:
			if (facebookActionLabel != null)
			{
				facebookActionLabel.text = Localize.text("facebook_logout");
			}
			if (ExperimentWrapper.Zis.isInExperiment)
			{
				if (SlotsPlayer.IsAppleLoggedIn)
				{
					Debug.LogFormat("Player name label in apple label help dialog {0}", ZisData.AppleName);
					playerNameLabel.SetText(ZisData.FacebookName);
				}
				else if (SlotsPlayer.isFacebookUser)
				{
					Debug.LogFormat("Player name label in help dialog {0}", friendInfo.member.firstName + " " + friendInfo.member.lastName);
					//playerNameLabel.text = friendInfo.member.firstName + " " + friendInfo.member.lastName;
					playerNameLabel.SetText(ZisData.FacebookName);
				}
				else if (SlotsPlayer.IsEmailLoggedIn)
				{
					playerNameLabel.SetText(ZisData.Email);
				}

			}
			else
			{
				if (playerNameLabel != null)
				{
					playerNameLabel.text = friendInfo.member.firstName + " " + friendInfo.member.lastName;
				}
			}
#endif
		}
		else
		{
			if (ExperimentWrapper.Zis.isInExperiment)
			{
				ZIDDisconnectedLabel.text = "ZID " + friendInfo.member.zId;
				if (onConnectButton != null)
				{
					onConnectButton.registerEventDelegate(onConnectButtonClicked);
				}
				facebookDisconnected.SetActive(true);
				facebookConnected.SetActive(false);
			}
			SafeSet.gameObjectActive(facebookButtonBlue, true);
			SafeSet.gameObjectActive(facebookButtonPink, false);

			playerPic.SetActive(false);
			
			// We're not a real player, fill in a blank name:
			//playerNameLabel.text = Localize.text("player_anonymous_name");
			
			string coins = SlotsPlayer.instance.mergeBonus > 0 ? CreditsEconomy.multiplyAndFormatNumberAbbreviated(SlotsPlayer.instance.mergeBonus) : "";
			facebookActionLabel.text = Localize.text("settings_progress_{0}", coins);
			
		}
	}


	//Function called when the connect button is clicked
	public virtual void onConnectButtonClicked(Dict args = null)
	{
		Dialog.close();
		ZisSaveYourProgressDialog.showDialog();
		StatsZIS.logSettingsZis();
	}

	public void clickOk()
	{
		if (isBusy)
		{
			return;
		}
		isBusy = true;

//		Glb.playButtonSelected();
		dialogArgs.merge(D.ANSWER, "yes");	// Just in case something needs to know if ok or close was clicked.
		Dialog.close();
	}

	public void clickClose()
	{
		if (isBusy)
		{
			return;
		}
		isBusy = true;

		if (Collectables.isActive())
		{
			StatsManager.Instance.LogCount(counterName:"dialog",
				kingdom: "settings",
				phylum: "card_alerts",
				klass: CustomPlayerData.getBool(CustomPlayerData.SHOW_COLLECT_ALERTS, true) ? "enable" : "disable",
				family: "close",
				genus: "click");
		}

		dialogArgs.merge(D.ANSWER, "no");	// Just in case something needs to know if ok or close was clicked.
		Dialog.close();
	}

	/// Called by Dialog.close() - do not call directly.
	public override void close()
	{
		// Do special cleanup.
	}

#if !UNITY_WEBGL
	private void OnLogoutFacebookClicked(GameObject ignored)
	{
		if (isBusy)
		{
			return;
		}
		isBusy = true;

		if (!SlotsPlayer.isFacebookUser)
		{
			// This is actually a *log in*:
			isBusy = false;    // set to false otherwise if players cancel Antisocial and this dialog comes back none of the buttons will work.
			Dialog.close(this);
			SocialManager.Instance.CreateAttach(Zynga.Zdk.Services.Identity.AuthenticationMethod.Facebook);
			return;
		}

		StatsFacebookAuth.logDisconnectClick();
		
		// Otherwise logout.
		if (NetworkFriends.instance.isEnabled)
		{
			// Use the friends logout dialog.
			NetworkFriendsFacebookDisconnectDialog.showDialog(onLogoutConfirm, onLogoutCancel);
		}
		else
		{
			// Otherwise default to the generic dialog.
			// Ask for confirmation about quitting.
			GenericDialog.showDialog(
				Dict.create(
					D.TITLE, Localize.text("facebook_logout"),
					D.MESSAGE,  Localize.text("facebook_to_anonymous_logout"),
					D.OPTION1,  Localize.textUpper("yes"),
					D.OPTION2,  Localize.textUpper("no"),
					D.REASON, "help-dialog-facebbok-to-anon-logout",
					D.CALLBACK, new DialogBase.AnswerDelegate(LogoutFacebookCallback),
					D.STACK, true
				)
			);
		}
	}

	// Callback for logging in.
	private void loginFacebookCallback(Dict answerArgs)
	{
		if ((answerArgs[D.ANSWER] as string) == "1")
		{
			Dialog.close();
		}
		isBusy = false;
	}

	private void LogoutFacebookCallback(Dict answerArgs)
	{
		if ((answerArgs[D.ANSWER] as string) == "1")
		{
			Dialog.close();
			SocialManager.Instance.Logout();
			//SlotsPlayer.facebookLogout(true);
			StatsFacebookAuth.logDisconnectConfirmed();
			Glb.resetGame("Logout out from FB old flow");
		}
		else
		{
			StatsFacebookAuth.logDisconnectCancelled();
			isBusy = false;
		}
	}

	// MCC new callbacks for the friends disconnect dialog, while its gross having double callbacks here,
	// the generic callbacks will get removed when friends is at 100%.
    private void onLogoutConfirm(Dict args = null)
	{
		Dialog.close();
		SocialManager.Instance.Logout();
		//SlotsPlayer.facebookLogout(true);
		isBusy = false;
		Glb.resetGame("Logging out old flow");
	}

	private void onLogoutCancel(Dict args = null)
	{
		isBusy = false;
	}
#endif

	// Callback for the ZADE RequestAd call that turns the button on.
	/*protected virtual void onZadeAdLoaded(Zap.Ad ad, Texture2D tex)
	{
		if (ad != null)
		{
			zadeMoreGamesButton.SetActive(true);
		}
	}*/

	// NGUI callback for the zade more games button.
	private void ZadeMoreGamesClicked()
	{
		//TODO: Girish
		//ZADEAdManager.Instance.RequestAd(ZADEAdManager.ZADE_MORE_GAMES_SLOT_NAME, null, null);
		StatsManager.Instance.LogCount("setting", "more_games", "", "", "", "click");
	}

	private void networkSettingClicked()
	{
		Dialog.close();
		StatsManager.Instance.LogCount ("dialog", "settings", "linked_vip", "", "", "click");
		if (LinkedVipProgram.instance.shouldPromptForConnect)
		{
			if (ExperimentWrapper.ZisPhase2.isInExperiment)
            {
				SocialManager.Instance.CreateAttach(Zynga.Zdk.Services.Identity.AuthenticationMethod.ZyngaEmailUnverified);
			}
            else
			{
				LinkedVipConnectDialog.showDialog();
			}
		}
		else
		{
			LinkedVipStatusDialog.checkNetworkStateAndShowDialog();
		}
	}
	
	// Callback for when the notification button is clicked.
	private void notifSettingClicked()
	{
		Dialog.close();
		StatsManager.Instance.LogCount ("dialog", "settings", "notif_prompt", "", "", "click");
		SoftPromptDialog.showDialog();
	}
		
	// NGUI callback for the zade more games button.
	private void OnPaytableClicked()
	{
		Dialog.close();
		PaytablesDialog.showDialog(SchedulerPriority.PriorityType.IMMEDIATE);
		Audio.play("minimenuclose0");
		StatsManager.Instance.LogCount(
			counterName : "dialog",
			kingdom : "settings",
			phylum: "pay_table",
			genus : "click"
		);
	}
	
	void Update()
	{
		// Change pages with swiping gesture.
		if (TouchInput.didSwipeLeft)
		{
			//Debug.Log("swiped left: " + TouchInput.swipeObject.name, TouchInput.swipeObject);
			if (TouchInput.swipeObject == this.musicToggleRoot)
			{
				setMusic(false);
			}
			else if (TouchInput.swipeObject == this.soundToggleRoot)
			{
				setSound(false);
			}
			else if (TouchInput.swipeObject == this.alertsToggleRoot)
			{
				setAlerts(false);
			}
			else if (TouchInput.swipeObject == this.perfToggleRoot)
			{
				setPerf(false);
			}
			else if (TouchInput.swipeObject == this.collectableToggleRoot)
			{
				setCollectables(false);
			}
			else if (petsAlertToggleRoot != null && TouchInput.swipeObject == this.petsAlertToggleRoot)
			{
				setPetsAlerts(false);
			}
		}
		else if (TouchInput.didSwipeRight)
		{
			//Debug.Log("swiped right: " + TouchInput.swipeObject.name, TouchInput.swipeObject);
			if (TouchInput.swipeObject == this.musicToggleRoot)
			{
				setMusic(true);
			}
			else if (TouchInput.swipeObject == this.soundToggleRoot)
			{
				setSound(true);
			}
			else if (TouchInput.swipeObject == this.alertsToggleRoot)
			{
				setAlerts(true);
			}
			else if (TouchInput.swipeObject == this.perfToggleRoot)
			{
				setPerf(true);
			}
			else if (TouchInput.swipeObject == this.collectableToggleRoot)
			{
				setCollectables(true);
			}
			else if (TouchInput.swipeObject == this.petsAlertToggleRoot)
			{
				setPetsAlerts(true);
			}
		}
		AndroidUtil.checkBackButton(clickClose, "dialog", "top_nav", "", "", "", "back");
	}

	/// This is the callback for when the music is currently ON.
	private void clickMusicOn()
	{
		setMusic(false);
	}

	/// This is the callback for when the music is currently OFF.
	private void clickMusicOff()
	{
		if (SlotBaseGame.instance != null)
		{
			SlotBaseGame.instance.idleTimer = Time.time;
		}
		setMusic(true);
	}

	/// This is the callback for when the sound is currently ON.
	private void clickSoundOn()
	{
		setSound(false);
	}

	/// This is the callback for when the sound is currently OFF.
	private void clickSoundOff()
	{
		if (SlotBaseGame.instance != null)
		{
			SlotBaseGame.instance.idleTimer = Time.time;
		}
		setSound(true);
	}

	/// This is the callback for when the players alerts is currently ON.
	private void clickAlertsOn()
	{
		setAlerts(false);
	}

	/// This is the callback for when the player alerts is currently OFF.
	private void clickAlertsOff()
	{
		setAlerts(true);
	}

	/// This is the callback for when performance mode is currently ON.
	private void clickPerfOn()
	{
		setPerf(false);
	}

	/// This is the callback for when the performance mode is currently OFF.
	private void clickPerfOff()
	{
		setPerf(true);
	}

	/// This is the callback for when performance mode is currently ON.
	private void clickCollectablesOn()
	{
		setCollectables(false);
	}

	/// This is the callback for when the performance mode is currently OFF.
	private void clickCollectablesOff()
	{
		setCollectables(true);
	}
	/// This is the callback for when pets alert is currently ON.
	private void clickPetsAlertOn()
	{
		setPetsAlerts(false);
	}

	/// This is the callback for when the pets alert mode is currently OFF.
	private void clickPetsAlertOff()
	{
		setPetsAlerts(true);
	}


	private void setMusic(bool musicOn)
	{
		Audio.muteMusic = !musicOn;

		musicToggleOn.SetActive(musicOn);
		musicToggleOff.SetActive(!musicOn);

		Audio.play(musicOn ? "minimenuopen0" : "minimenuclose0");
	}

	private void setSound(bool soundOn)
	{
		Audio.muteSound = !soundOn;

		soundToggleOn.SetActive(soundOn);
		soundToggleOff.SetActive(!soundOn);

		Audio.play(soundOn ? "minimenuopen0" : "minimenuclose0");
	}

	private void setAlerts(bool alertsOn)
	{
		ToasterManager.isPlayerAlertsOn = alertsOn;

		alertsToggleOn.SetActive(alertsOn);
		alertsToggleOff.SetActive(!alertsOn);

		Audio.play(alertsOn ? "minimenuopen0" : "minimenuclose0");
	}

	private void setPerf(bool perfOn)
	{
		PlayerPrefsCache.SetInt(Prefs.PLAYER_PERF, perfOn ? 1 : 0);
		PlayerPrefsCache.Save();

		NGUILoader.setVisualQuality();

		perfToggleOn.SetActive(perfOn);
		perfToggleOff.SetActive(!perfOn);

		Audio.play(perfOn ? "minimenuopen0" : "minimenuclose0");
	}

	private void setCollectables(bool collectablesOn)
	{
		CustomPlayerData.setValue(CustomPlayerData.SHOW_COLLECT_ALERTS, collectablesOn);

		collectableToggleOn.SetActive(collectablesOn);
		collectableToggleOff.SetActive(!collectablesOn);

		Audio.play(collectablesOn ? "minimenuopen0" : "minimenuclose0");
	}

	private void setPetsAlerts(bool alertOn)
	{
		if (VirtualPetsFeature.instance == null ||
		    petsAlertToggleOff == null ||
		    petsAlertToggleOn == null)
		{
			return;
		}
		VirtualPetsFeature.instance.silentFeedPet = !alertOn;
		petsAlertToggleOn.SetActive(alertOn);
		petsAlertToggleOff.SetActive(!alertOn);
	}
	
	//// Main buttons ////
	private void OnSupportClicked(GameObject ignored)
	{
		Common.openSupportUrl(Glb.HELP_LINK_SUPPORT);
	}

	private void OnInboxClicked(GameObject ignored)
	{
		Common.openSupportUrl(Glb.HELP_LINK_SUPPORT);
	}

	private void OnTermsOfServiceClicked(GameObject ignored)
	{
		//NetworkAction.getNetworkState (networkState);
		//NetworkAction.connectNetwork ("gnair@zynga.com", networkState);
		//NetworkAction.disconnectNetwork (networkState);
		Common.openUrlWebGLCompatible(Glb.HELP_LINK_TERMS);
	}

	private void OnPersonalDataClicked(GameObject buttonObj)
	{
		//turn off button so the user can't keep clicking while the data request is in progress
		UIImageButton btn = buttonObj.GetComponent<UIImageButton>();
		btn.isEnabled = false;

		GDPRDialog.showRequestInfoDialog(()=> 
			{
				btn.isEnabled = true;
			}, 
			SchedulerPriority.PriorityType.IMMEDIATE);
	}

	private void OnCCPAClicked(GameObject button)
	{
		if (!string.IsNullOrEmpty(ccpaURL))
		{
			Common.openUrlWebGLCompatible(ccpaURL);	
		}
		
	}

	private void OnPrivacyPolicyClicked(GameObject ignored)
	{
		Common.openUrlWebGLCompatible(Glb.HELP_LINK_PRIVACY);
	}	

	private void OnRateClicked(GameObject ignored)
	{
		if (!string.IsNullOrEmpty(Glb.clientAppstoreURL))
		{
			Common.openUrlWebGLCompatible(Glb.clientAppstoreURL);
		}
	}

	private void networkState(JSON data)
	{
		if (data.getString ("type", "blah") == "network_disconnect") 
		{
			Dictionary <string, string> jsonData = data.getStringStringDict("network_state");
			Debug.LogFormat("HelpDialog: networkState = {0}", jsonData);
		}
	}

	public static void showDialog()
	{
		if (ExperimentWrapper.Zis.isInExperiment)
		{
			Scheduler.addDialog("helpV3");
		}
		else
		{
			Scheduler.addDialog("helpV2");
		}
	}

}
