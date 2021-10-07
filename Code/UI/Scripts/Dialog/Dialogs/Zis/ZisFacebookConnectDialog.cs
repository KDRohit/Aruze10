using UnityEngine;
using System.Collections;
using TMPro;
using Com.Scheduler;
using Zynga.Core.Util;

public class ZisFacebookConnectDialog : DialogBase
{
	public FacebookFriendInfo friendInfo;

	public ClickHandler facebookClickHandler;
	public ClickHandler continueClickHandler;
	public GameObject ConnectedState;
	public GameObject UnConnectedState;
	public GameObject RestoreState;
	public GameObject PlayWithFriendsState;
	public GameObject playerPic;

	[SerializeField] private TextMeshPro playerNameLabel;
	[SerializeField] private TextMeshPro coinAmountLabel;
	[SerializeField] private TextMeshPro connectLabel;
	[SerializeField] private TextMeshPro headerLabel;
	[SerializeField] private TextMeshPro subHeaderLabel;
	[SerializeField] private TextMeshPro facebookButtonLabel;
	[SerializeField] private TextMeshPro continueButtonLabel;
	[SerializeField] private TextMeshPro connectedNameLabel;
	[SerializeField] private TextMeshPro connectedEmailLabel;
	[SerializeField] private TextMeshPro connectedCoinAmountLabel;


	//Setting up the ZIS save your progress dialog
	public override void init()
	{
		JSON data = dialogArgs.getWithDefault(D.CUSTOM_INPUT, null) as JSON;
		string facebookName = "test";
		string facebookEmail = "test@apple.com";
		string mergeBonus = "";
		if (data != null)
		{
			Debug.LogFormat("AppleLogin: in zisfacebook data {0}", data);
			mergeBonus = data.getString("merge_bonus", "");

			facebookName = ZisData.FacebookName;
			facebookEmail = ZisData.FacebookEmail;
		}
		else
		{
			facebookName = ZisData.AppleName;
			facebookEmail = ZisData.AppleEmail;
		}
		PreferencesBase preferences = SlotsPlayer.getPreferences();

		if (!mergeBonus.IsNullOrWhiteSpace())
		{
			SlotsPlayer.addCredits(long.Parse(mergeBonus), "mergebonus for connecting to FB");
		}
		if (SlotsPlayer.isFacebookUser && preferences.GetInt(Prefs.FACEBOOK_CONNECT_FAILED) != 1 && preferences.GetInt(Prefs.HAS_FB_CONNECTED_SUCCESSFULLY) != 1)
		{
			ConnectedState.SetActive(true);
			UnConnectedState.SetActive(false);

			if (continueClickHandler != null)
			{
				continueClickHandler.registerEventDelegate(continueClicked);
			}
			if (headerLabel != null)
			{
				headerLabel.text = "Account Connected!";
			}
			if (subHeaderLabel != null)
			{
				subHeaderLabel.text = "You can now play on any device.";
			}
			if (connectedNameLabel != null)
			{
				//Name that we get from facebook connection
				connectedNameLabel.text = facebookName;
			}
			if (connectedEmailLabel != null)
			{
				// Email if we get one after the facebook connection
				connectedEmailLabel.text = facebookEmail;
			}
			if (connectedCoinAmountLabel != null && !mergeBonus.IsNullOrWhiteSpace())
			{
				connectedCoinAmountLabel.text = Localize.text("coins_{0}", CreditsEconomy.convertCredits(SlotsPlayer.instance.mergeBonus));
			}
			else
			{
				connectedCoinAmountLabel.gameObject.SetActive(false);
			}
		}
		// show the unconnect state if the user isn't attempting facebook login
		else if (preferences.GetInt(Prefs.FACEBOOK_CONNECT_FAILED) != 1 && preferences.GetInt(Prefs.HAS_FB_CONNECTED_SUCCESSFULLY) != 1)
		{
			StatsZIS.logZisSigninRestoreAccount("play_with_friend", genus:"view");
			ConnectedState.SetActive(false);
			UnConnectedState.SetActive(true);
			RestoreState.SetActive(false);

			if (facebookClickHandler != null)
			{
				facebookClickHandler.registerEventDelegate(facebookClicked);
			}
			if (headerLabel != null)
			{
				headerLabel.text = "Play wth friends!";
			}
			if (subHeaderLabel != null)
			{
				subHeaderLabel.text = "Play with friends to collect free coins and other special gifts.";
			}
			if (connectLabel != null)
			{
				connectLabel.text = "Connect now and get";
			}
			if (coinAmountLabel != null)
			{
				coinAmountLabel.text = Localize.text("coins_{0}", CreditsEconomy.convertCredits(SlotsPlayer.instance.mergeBonus));
			}
			if (facebookButtonLabel != null)
			{
				facebookButtonLabel.text = "Connect";
			}
		}
		else
		{
			if (SocialManager.Instance.hasDeclinedFriendsPerm)
			{
				StatsZIS.logZisSigninRestoreAccount("restore_friend_list", genus:"view");
			}
			else
			{
				StatsZIS.logZisSigninRestoreAccount("restore_account", genus:"view");
			}

			if (facebookClickHandler != null)
			{
				facebookClickHandler.registerEventDelegate(facebookClicked);
			}

			ConnectedState.SetActive(false);
			UnConnectedState.SetActive(true);
			RestoreState.SetActive(true);
			PlayWithFriendsState.SetActive(false);
			playerNameLabel.gameObject.SetActive(false);
			SafeSet.labelText(facebookButtonLabel, "Connect");
			SafeSet.labelText(subHeaderLabel, "Please reconnect to restore your progress and play with friends.");
			SafeSet.labelText(headerLabel, "Restore your Account");

			preferences.SetInt(Prefs.FACEBOOK_CONNECT_FAILED, 0); //Resetting this since we've shown them the dialog
			preferences.SetInt(Prefs.HAS_FB_CONNECTED_SUCCESSFULLY, 0); //Resetting this since we've shown them the dialog
			preferences.Save();
		}
	}

	// click handler when facebook button is clicked
	private void facebookClicked(Dict args = null)
	{
		//If connecting for the first time then send action to the server and get the zid 
		Dialog.close(this);

		if (SocialManager.Instance.hasDeclinedFriendsPerm)
		{
			StatsZIS.logZisSigninRestoreAccount("restore_friend_list", family:"connect", genus:"click");
		}
		else
		{
			StatsZIS.logZisSigninRestoreAccount("restore_account", family:"connect", genus:"click");
		}

		if (SlotsPlayer.IsAppleLoggedIn)
		{
			SocialManager.Instance.FBConnect();
		}
		else
		{
			SlotsPlayer.facebookLogin();
		}
	}

	// click handler when continue button is clicked
	private void continueClicked(Dict args = null)
	{

		Dialog.close(this);

	}

	// Update is called once per frame
	void Update()
	{
		AndroidUtil.checkBackButton(onCloseButtonClicked);
	}

	// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup. Downloaded textures are automatically destroyed by Dialog class.
	}

	public static void showDialog(JSON data)
	{
		Dict args = Dict.create(
			D.CUSTOM_INPUT, data,
			D.STACK, false
		);
		Scheduler.addDialog("zis_facebook_connect", args);
	}	
}
