using UnityEngine;
using System.Collections;
using TMPro;
using Com.Scheduler;
using System.Threading.Tasks;
using Zynga.Core.Util;
using Zynga.Core.Tasks;
using Zynga.Authentication;

public class ZisSignOutDialog : DialogBase
{
	public ClickHandler signOutButtonClickHandler;
	public ClickHandler keepPlayingButtonClickHandler;
	public ClickHandler customerServiceClickHandler;

	[SerializeField] private TextMeshPro headerLabel;
	[SerializeField] private TextMeshPro subHeaderLabel;
	[SerializeField] private TextMeshPro alertLabel;
	[SerializeField] private TextMeshPro signOutLabel;
	[SerializeField] private TextMeshPro signOutButtonLabel;
	[SerializeField] private TextMeshPro keepPlayingButtonLabel;
	[SerializeField] private TextMeshPro disclaimerLabel;

	private bool isDisconnectedVersion = false;
	private bool keepPlaying = true;

	public const string DISCONNECTED_HEADER_LOCALIZATION = "logged_out";
	public const string CONNECT_FAILED_HEADER_LOCALIZATION = "login_problems";

	public override void init()
	{
		string headerLocalization = (string)dialogArgs.getWithDefault(D.TITLE, CONNECT_FAILED_HEADER_LOCALIZATION);
		keepPlaying = (bool) dialogArgs.getWithDefault(D.OPTION, false);
		signOutButtonClickHandler.registerEventDelegate(onSignOutButtonClicked);
		keepPlayingButtonClickHandler.registerEventDelegate(onKeepPlayingButtonClicked);
		customerServiceClickHandler.registerEventDelegate(onCustomerServiceClicked);

		isDisconnectedVersion = (headerLocalization == DISCONNECTED_HEADER_LOCALIZATION);
		if (isDisconnectedVersion)
		{
			if (headerLabel != null)
			{
				headerLabel.text = "Sign Out";
			}
			if (subHeaderLabel != null)
			{
				subHeaderLabel.gameObject.SetActive(false);
			}

			if (alertLabel != null)
			{
				alertLabel.text = "Signing out will remove your account progress from this device.";
			}

			if (signOutLabel != null)
			{
				signOutLabel.text = "Your account is safe. You can always sign in to restore it.";
			}
			StatsZIS.logZisSignOut(phylum: "sign_out", genus: "view");
		}
		else
		{
			if (headerLabel != null)
			{
				headerLabel.text = "Please Reconnect";
			}

			if (subHeaderLabel != null)
			{
				subHeaderLabel.text = "We're having trouble connecting your accounts";
			}

			if (alertLabel != null)
			{
				alertLabel.text = "Please sign out and try connecting your accounts again.";
			}

			if (signOutLabel != null)
			{
				signOutLabel.text = "Your account is safe. You can always sign in to restore it.";
			}
			StatsZIS.logZisSignOut(phylum: "please_reconnect", genus: "view");
		}

		if (signOutButtonLabel != null)
		{
			signOutButtonLabel.text = "Sign Out";
		}

		if (keepPlayingButtonLabel != null)
		{
			keepPlayingButtonLabel.text = "Keep Playing";
		}

		if (disclaimerLabel != null)
		{
			disclaimerLabel.text = "<#9f9f9f>Having issues?</color> <#cdcdcd><u>Contact Customer Service</u></color>";
		}

		//keeping this commented as working out if isAnonymous needs to be updated
        //its being used as !fb context at certain places
		//signOutButtonClickHandler.gameObject.SetActive(SlotsPlayer.isAnonymous);
		signOutButtonClickHandler.gameObject.SetActive(SlotsPlayer.IsAppleLoggedIn || SlotsPlayer.isFacebookUser || SlotsPlayer.IsEmailLoggedIn);

	}

	//click handler call back for signout button
	private async void onSignOutButtonClicked(Dict args = null)
	{
		StatsZIS.logZisSignOut(family:"sign_out", genus:"click");

		if (LinkedVipProgram.instance != null)
		{
			if (LinkedVipProgram.instance.isConnected)
			{
				if (Glb.llapidisconnect)
				{
					StatsManager.Instance.LogCount("dialog", "ZisManageAccountDialog", "log_out_network", "", "theme", "click");
					Server.registerEventDelegate("network_disconnect", networkCallback);
					NetworkAction.disconnectNetwork();
				}
			}
		}

		await SocialManager.Instance.Logout().Callback(task => {
			// Callback to make sure we don't reset the game before fully being logged out.
			Glb.resetGame("zis logout");
		});
	}

	//Callback when LL is disconnected
	private void networkCallback(JSON data)
	{
		string status = (string)data.getString("network_state.status", "none");
		LinkedVipProgram.instance.updateNetworkStatus(status);
	}

	//click handler callback for keep playing button
	private void onKeepPlayingButtonClicked(Dict args = null)
	{
		StatsZIS.logZisSignOut(family:"keep_playing", genus:"click");
		if (!isDisconnectedVersion && !keepPlaying)
		{
			SocialManager.Instance.retryPrompt(Dict.create(D.ANSWER, "2"));
		}
		Dialog.close(this);
	}

	// When customer service is clicked
	private void onCustomerServiceClicked(Dict args = null)
	{
		if (isDisconnectedVersion) // signing out
		{
			Dialog.close(this);
		}
		Common.openSupportUrl(Glb.HELP_LINK_SUPPORT);
		StatsZIS.logZisSignOut(family:"contact_customer_service", genus:"click");
	}

	// Update is called once per frame
	void Update()
	{
		AndroidUtil.checkBackButton(onCloseButtonClicked);
	}

	// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		StatsZIS.logZisSignOut(genus: "close");
		// Do special cleanup. Downloaded textures are automatically destroyed by Dialog class.
	}

	public static void showDialog(Dict args = null)
	{
		Scheduler.addDialog("zis_sign_out", args);
	}
}
