using UnityEngine;
using System.Collections;
using TMPro;
using Com.Scheduler;
using System;
#if UNITY_IOS
using UnityEngine.iOS;
#endif

public class ZisEmailOptInDialog : DialogBase
{
	public ClickHandler optInClickHandler;

	[SerializeField] private TextMeshPro coinAmountLabel;
	[SerializeField] private TextMeshPro headerLabel;
	[SerializeField] private TextMeshPro subHeaderLabel;
	[SerializeField] private TextMeshPro optInButtonLabel;
	[SerializeField] private TextMeshPro nameLabel;
	[SerializeField] private TextMeshPro messageLabel;

	private const string HEADER = "zis_email_opt_in_header";
	private const string SUBHEADER = "zis_email_opt_in_subheader";
	private const string COIN_AMOUNT = "zis_email_opt_in_coinamount";
	private const string OPT_IN_BUTTON = "zis_email_opt_in_buttonlabel";
	private const string MESSAGE = "zis_email_opt_in_message";


	private string email = "";
	private string rewardAmount = "";

	public static string statsLocation = "";
	//Setting up the ZIS save your progress dialog
	public override void init()
	{

		optInClickHandler.registerEventDelegate(onOptInButtonClicked);
		JSON data = dialogArgs.getWithDefault(D.DATA, null) as JSON;

		if(data != null)
		{
			email = data.getString("email", "");
			rewardAmount = data.getString("rewardAmount", "");
			statsLocation = "loyalty_lounge";
		}
		else
		{
			email = dialogArgs.getWithDefault(D.EMAIL, "test@test.com").ToString();
			rewardAmount = dialogArgs.getWithDefault(D.AMOUNT, 0L).ToString();
			if (SlotsPlayer.isFacebookUser)
			{
				statsLocation = "facebook";
			}
			if (SlotsPlayer.IsAppleLoggedIn)
			{
				statsLocation = "siwa";
			}
		}

		if (headerLabel != null)
		{
			headerLabel.text = Localize.text(HEADER); //"Get coin gifts and news!"	
		}

		if (subHeaderLabel != null)
		{
			subHeaderLabel.text = Localize.text(SUBHEADER); //"Opt-in for coin gifts and announcements by email!"
		}

		if (coinAmountLabel != null)
		{
			coinAmountLabel.text = Localize.text("coins_{0}", CreditsEconomy.convertCredits(long.Parse(rewardAmount)));
		}

		if (optInButtonLabel != null)
		{
			optInButtonLabel.text = Localize.text(OPT_IN_BUTTON); //"Opt-in and Collect"
		}

		if (messageLabel != null)
		{
			messageLabel.text = Localize.text(MESSAGE); //"Opt-in to emails from Zynga and get"
		}

		if (nameLabel != null)
		{
			nameLabel.text = email;
		}
	}

	private void onOptInButtonClicked(Dict args = null)
	{
		EmailOptOutAction.emailOptOut("false", email);
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

	public override void onCloseButtonClicked(Dict args = null)
	{
		EmailOptOutAction.emailOptOut("true", email); 
		Dialog.close(this);
	}

	public static void showDialog(Dict args = null)
	{
		Scheduler.addDialog("zis_email_opt_in", args);
	}
}
