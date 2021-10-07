using UnityEngine;
using System.Collections;
using TMPro;
using Com.Scheduler;
using Zynga.Zdk.Services.Identity;

public class ZisManageAccountDialog : DialogBase
{
	public ClickHandler switchButtonClickHandler;
	public ClickHandler appleButtonClickHandler;
	public ClickHandler facebookButtonClickHandler;
	public ClickHandler loyaltyLoungeClickHandler;
	public ClickHandler loyaltyLoungeSignedInClickHandler;
	public ClickHandler emailButtonClickHandler;
	public ClickHandler emailEditButtonClickHandler;

	public UISprite facebookCheckMark;
	public UISprite appleCheckMark;
	public UISprite loyaltyLoungeCheckMark;
	public UISprite emailCheckMark;

	public GameObject facebookSignedIn;
	public GameObject facebookSignedOut;
	public GameObject appleSignedIn;
	public GameObject appleSignedOut;
	public GameObject loyaltyLoungeSignedIn;
	public GameObject loyaltyLoungeSignedOut;
	public GameObject apple;
	public GameObject facebook;
	public GameObject loyaltyLounge;
	public GameObject loyaltyLoungeCoin;
	public GameObject email;
	public GameObject emailSignedIn;
	public GameObject emailSignedOut;

	[SerializeField] private TextMeshPro facebookCoinAmountLabel;
	[SerializeField] private TextMeshPro facebookSignedInLabel;
	[SerializeField] private TextMeshPro facebookSignedOutLabel;
	[SerializeField] private TextMeshPro appleCoinAmountLabel;
	[SerializeField] private TextMeshPro appleSignedInLabel;
	[SerializeField] private TextMeshPro appleSignedOutLabel;
	[SerializeField] private TextMeshPro loyaltySignedInLabel;
	[SerializeField] private TextMeshPro loyaltySignedOutLabel;
	[SerializeField] private TextMeshPro loyaltyCoinAmountLabel;
	[SerializeField] private TextMeshPro switchButtonLabel;
	[SerializeField] private TextMeshPro headerLabel;
	[SerializeField] private TextMeshPro subHeaderLabel;
	[SerializeField] private TextMeshPro emailLabel;
	[SerializeField] private TextMeshPro emailEditLabel;
	[SerializeField] private TextMeshPro emailCoinRewardLabel;

	//Setting up the ZIS manage dialog
	public override void init()
	{
		StatsZIS.logZisManageAccount(genus:"view");

		if (switchButtonClickHandler != null)
		{
			switchButtonClickHandler.registerEventDelegate(switchButtonClicked);
		}
		if (appleButtonClickHandler != null)
		{
			appleButtonClickHandler.registerEventDelegate(appleButtonClicked);
		}
		if (facebookButtonClickHandler != null)
		{

#if UNITY_WEBGL && !UNITY_EDITOR
			// This may need to trigger a popup. If so, we need to have the mouse event
			// on mousedown, so it can register a mouseup handler in JS. 
			facebookButtonClickHandler.registeredEvent = ClickHandler.MouseEvent.OnMouseDown;
#endif
			facebookButtonClickHandler.registerEventDelegate(facebookClicked);
		}
		if (loyaltyLoungeClickHandler != null)
		{
			loyaltyLoungeClickHandler.registerEventDelegate(loyaltyLoungeClicked);
		}
		if (loyaltyLoungeSignedInClickHandler != null)
		{
			loyaltyLoungeSignedInClickHandler.registerEventDelegate(loyaltyLoungeClicked);
		}
		if (emailButtonClickHandler != null)
		{
			if(ExperimentWrapper.ZisPhase2.isInExperiment) 
			{
				emailButtonClickHandler.registerEventDelegate(emailClicked);	
			}
			else
			{
				emailButtonClickHandler.gameObject.SetActive(false);
			}
		}
		if (emailEditButtonClickHandler != null)
		{
			if(ExperimentWrapper.ZisPhase2.isInExperiment) 
			{
				emailEditButtonClickHandler.registerEventDelegate(emailEditClicked);
			}
			else
			{
				emailEditButtonClickHandler.gameObject.SetActive(false);
			}
				
		}

		if (!Glb.showEditButton)
        {
			if (PackageProvider.Instance.Authentication.Flow.Account.UserAccount.Email.Verified)
            {
				emailEditButtonClickHandler.gameObject.SetActive(false);
			}
		}

		if (headerLabel != null)
		{
			headerLabel.text = "Manage Account";
		}
		if (subHeaderLabel != null)
		{
			subHeaderLabel.text = "Connect to save and play on any device.";
		}
		if (facebookSignedOutLabel != null)
		{
			facebookSignedOutLabel.text = "Connect";
		}
		if (facebookCoinAmountLabel != null)
		{
			facebookCoinAmountLabel.text = Localize.text("coins_{0}", CreditsEconomy.convertCredits(SlotsPlayer.instance.mergeBonus));
		}
		if (loyaltySignedOutLabel != null)
		{
			loyaltySignedOutLabel.text = "Sign up";
		}
		if (loyaltyCoinAmountLabel != null)
		{
			loyaltyCoinAmountLabel.text = Localize.text("coins_{0}", CreditsEconomy.convertCredits(SlotsPlayer.instance.mergeBonus));
		}
		if (emailCoinRewardLabel != null)
		{
			emailCoinRewardLabel.text = Localize.text("coins_{0}", CreditsEconomy.convertCredits(SlotsPlayer.instance.mergeBonus));
		}
		if (switchButtonLabel != null)
		{
			switchButtonLabel.text = "Logout";
		}

		if (loyaltyCoinAmountLabel != null && LinkedVipProgram.instance.incentiveCredits != 0)
		{
			loyaltyCoinAmountLabel.text = CreditsEconomy.convertCredits(LinkedVipProgram.instance.incentiveCredits);
		}
		else
		{
			loyaltyLoungeCoin.SetActive(false);
		}

		apple.gameObject.SetActive(false);
		facebook.gameObject.SetActive(false);
		email.gameObject.SetActive(false);

		if (SlotsPlayer.isFacebookUser)
		{
			Debug.Log("AppleLgin: is facebook user");
			facebookSignedInState();
		}
		else
		{
			
			facebookSignedOutState();
		}

		if (SlotsPlayer.IsAppleLoggedIn)
		{
			appleSignedInState();
		}
		else
		{
			if (!SlotsPlayer.isFacebookUser && !SlotsPlayer.IsEmailLoggedIn)
			{
				appleSignedOutState();
			}
		}
		
		if (ExperimentWrapper.ZisPhase2.isInExperiment)
		{
			if (SlotsPlayer.IsEmailLoggedIn)
			{
				emailSignedInState();
			}
			else
			{
				if (Glb.showWebglEmail)
				{
					emailSignedOutState();
				}
			}
		}
		
        

		LLState();

	}

	private void facebookSignedInState()
	{
		facebook.gameObject.SetActive(true);
		facebookCheckMark.gameObject.SetActive(true);
		facebookSignedIn.SetActive(true);
		facebookSignedOut.SetActive(false);
		facebookSignedInLabel.text = ZisData.FacebookName;
	}


	private void appleSignedInState()
	{
		apple.gameObject.SetActive(true);
		appleCheckMark.gameObject.SetActive(true);
		appleSignedIn.SetActive(true);
		appleSignedOut.SetActive(false);
		appleSignedInLabel.text = ZisData.AppleName;
	}

	private void emailSignedInState()
	{
		email.gameObject.SetActive(true);
		emailCheckMark.gameObject.SetActive(true);
		emailSignedIn.SetActive(true);
		emailSignedOut.SetActive(false);
		if (emailLabel != null)
		{
			if (!ZisData.Email.IsNullOrWhiteSpace())
			{
				emailLabel.text = ZisData.Email;
			} 
			else if (!ZisData.AppleEmail.IsNullOrWhiteSpace())
			{
				emailLabel.text = ZisData.AppleEmail;
			}
			else if (!ZisData.FacebookEmail.IsNullOrWhiteSpace())
			{
				emailLabel.text = ZisData.FacebookEmail;
			}
		}

		if (!PackageProvider.Instance.Authentication.Flow.Account.UserAccount.Email.Verified)
		{
			emailEditLabel.text = "Verify";
		}
		else
		{
			emailEditLabel.text = "Edit";
		}

	}

	private void LLState()
	{
		if (LinkedVipProgram.instance != null)
		{
			if (LinkedVipProgram.instance.isPending)
			{
				loyaltyLounge.SetActive(true);
				loyaltyLoungeCheckMark.gameObject.SetActive(false);
				loyaltyLoungeSignedOut.SetActive(true);
				loyaltyLoungeSignedIn.SetActive(false);
				if (loyaltySignedOutLabel != null)
				{
					loyaltySignedOutLabel.text = "Status";
					//loyaltyCoinAmountLabel.gameObject.SetActive(false);
					loyaltyLoungeCoin.SetActive(false);
				}
			}
			else if (LinkedVipProgram.instance.shouldPromptForConnect)
			{
				loyaltyLounge.SetActive(true);
				loyaltyLoungeCheckMark.gameObject.SetActive(false);
				loyaltyLoungeSignedOut.SetActive(true);
				loyaltyLoungeSignedIn.SetActive(false);
			}
			else if (LinkedVipProgram.instance.isConnected)
			{
				loyaltyLounge.SetActive(true);
				loyaltyLoungeCheckMark.gameObject.SetActive(true);
				loyaltyLoungeSignedIn.SetActive(true);
				loyaltyLoungeSignedOut.SetActive(false);
			}
			else
			{
				loyaltyLounge.SetActive(false);
			}
		}
		else
		{
			loyaltyLounge.SetActive(false);
		}
	}

	private void appleSignedOutState()
	{
		apple.gameObject.SetActive(true);
		appleCheckMark.gameObject.SetActive(false);
		appleSignedIn.SetActive(false);
		appleSignedOut.SetActive(true);
	}


	private void facebookSignedOutState()
	{
		facebook.gameObject.SetActive(true);
		facebookCheckMark.gameObject.SetActive(false);
		facebookSignedIn.SetActive(false);
		facebookSignedOut.SetActive(true);
	}

	private void emailSignedOutState()
	{
		email.gameObject.SetActive(true);
		emailCheckMark.gameObject.SetActive(false);
		emailSignedIn.SetActive(false);
		emailSignedOut.SetActive(true);
	}

	private void emailEditClicked(Dict args = null)
	{
		Dialog.close(this);
		if (!PackageProvider.Instance.Authentication.Flow.Account.UserAccount.Email.Verified)
		{
			SocialManager.Instance.onVerifyPressed();
		}
		else 
		{
			SocialManager.Instance.emailChangePressed();
		}
	}

	// when loyalty lounge button is clicked
	private void loyaltyLoungeClicked(Dict args = null)
	{
		StatsZIS.logZisManageAccount(family:"loyalty_lounge",genus:"click");
		Dialog.close();
		if (LinkedVipProgram.instance.shouldPromptForConnect)
		{
			LinkedVipConnectDialog.showDialog();
		}
		else
		{
			LinkedVipStatusDialog.checkNetworkStateAndShowDialog();
		}
	}

	private void emailClicked(Dict args = null)
	{
		Dialog.close(this);
		SocialManager.Instance.CreateAttach(AuthenticationMethod.ZyngaEmailUnverified);
	}

	//click handler when switch button is clicked
	private void switchButtonClicked(Dict args = null)
	{
		Dialog.close(this);


		ZisSignOutDialog.showDialog(Dict.create(
			D.TITLE, ZisSignOutDialog.DISCONNECTED_HEADER_LOCALIZATION
		));

		StatsZIS.logZisManageAccount(family:"log_out",genus:"click");
	}

	// click handler when facebook button is clicked
	private void facebookClicked(Dict args = null)
	{
		//If connecting for the first time then send action to the server and get the zid 
		Dialog.close(this);
		if (SlotsPlayer.IsAppleLoggedIn || SlotsPlayer.IsEmailLoggedIn)
		{
			Userflows.flowStart(SocialManager.facebookConnectUserflow);
			SocialManager.Instance.CreateAttach(AuthenticationMethod.Facebook);
		}
		StatsZIS.logZisManageAccount(family:"facebook",genus:"click");
	}

	// click handler when apple button is clicked
	private void appleButtonClicked(Dict args = null)
	{
		Dialog.close(this);
		StatsZIS.logZisManageAccount(family:"apple",genus:"click");
	}

	// Update is called once per frame
	void Update()
	{
		AndroidUtil.checkBackButton(onCloseButtonClicked);
	}

	// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		StatsZIS.logZisManageAccount(genus: "close");
		// Do special cleanup. Downloaded textures are automatically destroyed by Dialog class.
	}

	public static void showDialog()
	{
		Scheduler.addDialog("zis_manage_account");
	}
}

