using UnityEngine;
using System.Collections;
using TMPro;
using Com.Scheduler;

//Dialog when the apple account is created 
public class ZisAccountCreatedDialog : DialogBase
{
	public ClickHandler collectClickHandler;
	public ClickHandler optInClickHandler;
	public UISprite logo;
	public UISprite emailCheckMark;
	public UISprite optInForEmails;

	[SerializeField] private TextMeshPro headerLabel;
	[SerializeField] private TextMeshPro subHeaderLabel;
	[SerializeField] private TextMeshPro userNameLabel;
	[SerializeField] private TextMeshPro userEmailLabel;
	[SerializeField] private TextMeshPro coinLabel;
	[SerializeField] private TextMeshPro collectButtonLabel;
	[SerializeField] private GameObject coin;
	private string statPhylum;

	//Setting up the ZIS apple account created dialog
	public override void init()
	{
		JSON data = dialogArgs.getWithDefault(D.CUSTOM_INPUT, null) as JSON;
		string mergeBonus = "";
		if (data != null)
		{
			Debug.LogFormat("AppleLogin: in email data {0}", data);
			mergeBonus = data.getString("merge_bonus", "");
			if (!mergeBonus.IsNullOrWhiteSpace())
			{
				SlotsPlayer.addCredits(long.Parse(mergeBonus), "mergebonus for connecting to Email");
			}
		}
		if (collectClickHandler != null)
		{
			collectClickHandler.registerEventDelegate(collectButtonClicked);
		}
		if (optInClickHandler != null)
		{
			optInClickHandler.registerEventDelegate(onOptInClick);
		}
		if (headerLabel != null)
		{
			headerLabel.text = "Account Created!";
		}
		if (subHeaderLabel != null)
		{
			subHeaderLabel.text = "You can sign in on any device.";
		}
		if (userNameLabel != null)
		{
			
			userNameLabel.text = "Connected";

			if (SlotsPlayer.isFacebookUser)
			{
				logo.spriteName = "Logo Facebook 00";
				userNameLabel.text = ZisData.FacebookName;
			} 
			else if (SlotsPlayer.IsAppleLoggedIn)
			{
				logo.spriteName = "Logo Apple 00";
				userNameLabel.text = ZisData.AppleName;
			}
			else if (SlotsPlayer.IsEmailLoggedIn)
			{
				userNameLabel.gameObject.SetActive(false);
			}
			else
			{
				userNameLabel.text = "test test";
			}

		}
		if (userEmailLabel != null && emailCheckMark != null)
		{
		    //emails in Apple can look like user@privaterelay.appleid.com
		    //This can confuse players
		    //while the format is set right now, it may change in future
            //so, we are disabling email for apple users as well
		    //more info on this on following
		    //https://support.apple.com/en-us/HT210425
			userEmailLabel.gameObject.SetActive(false);
			emailCheckMark.gameObject.SetActive(false);
		
			if (SlotsPlayer.IsEmailLoggedIn && PackageProvider.Instance.Authentication.Flow.Account.UserAccount.Email != null && !PackageProvider.Instance.Authentication.Flow.Account.UserAccount.Email.Id.IsNullOrWhiteSpace())
			{
				logo.gameObject.SetActive(false);
				userEmailLabel.gameObject.SetActive(true);
				emailCheckMark.gameObject.SetActive(true);
				userEmailLabel.text = PackageProvider.Instance.Authentication.Flow.Account.UserAccount.Email.Id;
			}
		}
		if (coinLabel != null)
		{
			if (SlotsPlayer.isFacebookUser || SlotsPlayer.IsEmailLoggedIn)
			{
				coinLabel.text = Localize.text("coins_{0}", CreditsEconomy.convertCredits(SlotsPlayer.instance.mergeBonus));
			}
			if (SlotsPlayer.IsAppleLoggedIn)
			{
				coin.SetActive(false);
			}
		}
		if (collectButtonLabel != null)
		{
			if (SlotsPlayer.isFacebookUser)
			{
				collectButtonLabel.text = "Collect & Play";
			}
			else if (SlotsPlayer.IsAppleLoggedIn)
			{
                //apple gives no merge bonus
				collectButtonLabel.text = "Continue";
			}
		}

		bool isNewUser = GameExperience.totalSpinCount == 0;
		statPhylum = isNewUser ? "new_user" : "existing_user";

		if (isNewUser && SlotsPlayer.instance.isGDPRSuspend)
		{
			statPhylum += "_gdpr";
		}

		if (SlotsPlayer.isFacebookUser)
		{
			StatsZIS.logZisSigninIncentive(statPhylum, "", "view", "facebook_account");
		}
		else if (SlotsPlayer.IsAppleLoggedIn)
		{
			StatsZIS.logZisSigninIncentive(statPhylum, "", "view", "sign_in_with_apple_account");
		}
		else if (SlotsPlayer.IsEmailLoggedIn)
		{
			StatsZIS.logZisSigninIncentive(statPhylum, "", "view", "sign_in_with_email_account");

		}

		if (!SocialManager.emailOptIn)
		{
			optInClickHandler.gameObject.SetActive(false);
		}
	}

	private void onOptInClick(Dict args = null)
	{
		Debug.Log("Opt in Click");
		if (optInForEmails != null)
		{
			Debug.Log("Pot in for emails");
			optInForEmails.gameObject.SetActive(!optInForEmails.gameObject.activeSelf);
		}
	}

	// click handler when collect button is clicked
	private void collectButtonClicked(Dict args = null)
	{
		Dialog.close(this);

		if (SlotsPlayer.isFacebookUser)
		{
			StatsZIS.logZisSigninIncentive(statPhylum, "CTA", "click", "facebook_account");
		}
		else if (SlotsPlayer.IsAppleLoggedIn)
		{
			StatsZIS.logZisSigninIncentive(statPhylum, "CTA", "click", "sign_in_with_apple_account");
		}
		else if (SlotsPlayer.IsEmailLoggedIn)
		{
			StatsZIS.logZisSigninIncentive(statPhylum, "", "view", "sign_in_with_email_account");

		}
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
		if (optInForEmails.gameObject.activeSelf)
		{
			// Send the emailoptin action
		}
	}

	public static void showDialog(JSON data = null)
	{
		Dict args = Dict.create(
			D.CUSTOM_INPUT, data,
			D.STACK, false
		);
		Scheduler.addDialog("zis_account_created", args);
	}
}
