
using UnityEngine;
using System.Collections;
using TMPro;
using Com.Scheduler;
using System;
using System.Collections.Generic;
using Zynga.Zdk.Services.Identity;
#if UNITY_IOS
using UnityEngine.iOS;
#endif

public class ZisSaveYourProgressDialog : DialogBase
{
	public enum Mode
	{
		STANDARD,
		INITIAL_LOGIN
	}

	public ClickHandler facebookClickHandler;
	public ClickHandler appleClickHandler;
	public ClickHandler emailClickHandler;
	public GameObject apple;
	public GameObject closeButton;
	private Mode mode;
	[SerializeField] private TextMeshPro coinAmountLabel;
	[SerializeField] private TextMeshPro emailCoinAmountLabel;
	[SerializeField] private TextMeshPro signInLabel;
	[SerializeField] private TextMeshPro headerLabel;
	[SerializeField] private TextMeshPro headerLabelShadow;
	[SerializeField] private TextMeshPro subHeaderLabel;
	[SerializeField] private TextMeshPro facebookButtonLabel;
	[SerializeField] private TextMeshPro appleButtonLabel;
	[SerializeField] private TextMeshPro emailButtonLabel;
	[SerializeField] private List<GameObject> allCoinAwards;

	//Setting up the ZIS save your progress dialog
	public override void init()
	{
		if (facebookClickHandler != null)
		{
			if(Data.webPlatform.IsDotCom)
			{
				// This may need to trigger a popup. If so, we need to have the mouse event
				// on mousedown, so it can register a mouseup handler in JS. 
				facebookClickHandler.registeredEvent = ClickHandler.MouseEvent.OnMouseDown;
			}
			facebookClickHandler.registerEventDelegate(facebookClicked);
		}
		if (appleClickHandler != null)
		{
			appleClickHandler.registerEventDelegate(appleLoginClicked);
		}
		if (Glb.showWebglEmail)
		{
			if (emailClickHandler != null)
			{
				if (ExperimentWrapper.ZisPhase2.isInExperiment || Data.webPlatform.IsDotCom )
				{
					emailClickHandler.registerEventDelegate(emailLoginClicked);
				}
				else
				{
					emailClickHandler.gameObject.SetActive(false);
				}
			}
		}
		else
        {
			emailClickHandler.gameObject.SetActive(false);
		}
		SafeSet.labelText(headerLabel, "Save your progress!");
		SafeSet.labelText(subHeaderLabel, "Play your games and share VIP status on all your devices!");
		SafeSet.labelText(signInLabel, "Sign in now and get");
		SafeSet.labelText(facebookButtonLabel, "Sign in with Facebook");
		SafeSet.labelText(emailButtonLabel, "Sign in with Email");
		SafeSet.labelText(coinAmountLabel, CreditsEconomy.multiplyAndFormatNumberAbbreviated(SlotsPlayer.instance.mergeBonus));
		SafeSet.labelText(emailCoinAmountLabel, CreditsEconomy.multiplyAndFormatNumberAbbreviated(SlotsPlayer.instance.mergeBonus));

#if UNITY_IOS
		if (isIOSVersionValid())
		{
			SafeSet.labelText(appleButtonLabel, "Sign in with Apple");
			apple.SetActive(true);
		}
		else
		{
			apple.SetActive(false);
		}
#else
		apple.SetActive(false);
#endif

		mode = (Mode)dialogArgs.getWithDefault(D.MODE, Mode.STANDARD);
		string titleText = (mode == Mode.INITIAL_LOGIN) ? "Sign in to play!" : "Save your progress!";

		// Update title
		if (headerLabel != null)
		{
			headerLabel.text = titleText;
		}
		if (headerLabelShadow != null)
		{
			headerLabelShadow.text = titleText;
		}
		
		// show / hide close button
		if (closeButton != null)
		{
			closeButton.SetActive(mode != Mode.INITIAL_LOGIN);
		}

		// Show / hide coin rewards
		foreach (var coinAward in allCoinAwards)
		{
			if (coinAward != null)
			{
				coinAward.SetActive(mode != Mode.INITIAL_LOGIN);
			}
		}
	}

	// click handler when facebook button is clicked
	private void facebookClicked(Dict args = null)
	{
		StatsZIS.logZisSignIn("sign_in_with_facebook", "click");
		SlotsPlayer.IsFacebookConnected = false;
		Dialog.close(this);
		// Log into facebook
		if(mode == Mode.INITIAL_LOGIN)
		{
			Action<string> callback = (Action<string>)dialogArgs.getWithDefault(D.CALLBACK, null);
			if(callback != null)
			{
				callback("facebook");
			}
			else
			{
				Debug.LogError("Save Your Progress is missing facebook callback.");
			}
		}
		else
		{
			SocialManager.Instance.CreateAttach(AuthenticationMethod.Facebook);
		}
	}

	// click handler when apple button is clicked
	private void appleLoginClicked(Dict args = null)
	{
		StatsZIS.logZisSignIn("sign_in_with_apple", "click");
		Userflows.flowStart(SocialManager.appleUserflow);
		Dialog.close(this);
		SocialManager.Instance.CreateAttach(AuthenticationMethod.SignInWithApple);
	}

	// click handler when email is clicked
	private void emailLoginClicked(Dict args = null)
	{
		StatsZIS.logZisSignIn("sign_in_with_email", "click");
		Dialog.close(this);

		if(mode == Mode.INITIAL_LOGIN)
		{
			Action<string> callback = (Action<string>)dialogArgs.getWithDefault(D.CALLBACK, null);
			if(callback != null)
			{
				callback("email");
			}
			else
			{
				Debug.LogError("Save Your Progress is missing email callback.");
			}
		}
		else
		{
			if (!SlotsPlayer.IsEmailLoggedIn)
			{
				SocialManager.Instance.CreateAttach(AuthenticationMethod.ZyngaEmailUnverified);

			}
			else
			{
				if (PackageProvider.Instance.Authentication.Flow.Account.UserAccount.Email.Verified)
				{
					ZisSignInWithEmailDialog.showDialog(ZisSignInWithEmailDialog.WELCOME_BACK_STATE);
				}
				else
				{
					ZisSignInWithEmailDialog.showDialog(ZisSignInWithEmailDialog.CONFIRMATION_SENT_STATE);
				}
			}
		}
	}

	private void appleLoginFinished(bool success)
	{
		if (success)
		{

			Debug.Log("AppleLogin: appleLoginFinished success");

			SlotsPlayer.finishAppleLogin();
			SlotsPlayer.IsAppleLoggedIn = true;
		}
		else
		{
			Debug.Log("AppleLogin: appleLoginFinished failure");
		}
	}

	//Function to check whether the ios version is valid for ZADE
	private bool isIOSVersionValid()
	{
#if UNITY_IOS && !UNITY_EDITOR
		string[] versionPrefix = Device.systemVersion.Split('.');

		if (versionPrefix[0] != null)
		{
			int prefix = Convert.ToInt32(versionPrefix[0]);
			if (prefix < 13)
			{
				return false;
			}
		}
		return true;
#endif
		return false;

	}

	// Update is called once per frame
	void Update()
	{
		AndroidUtil.checkBackButton(onCloseButtonClicked);
	}

	public override void onCloseButtonClicked(Dict args = null)
	{
		if (mode != Mode.INITIAL_LOGIN) // do not allow back/close when this is the initial login prompt
		{
			base.onCloseButtonClicked(args);
		}
	}

	// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		StatsZIS.logZisSignIn("", "close");
		// Do special cleanup. Downloaded textures are automatically destroyed by Dialog class.
	}

	public static void showDialog(bool fromLogin = false, SchedulerPriority.PriorityType priority = SchedulerPriority.PriorityType.LOW, Action<String> callback = null)
	{
		Dict args = Dict.create(D.MODE, fromLogin ? Mode.INITIAL_LOGIN : Mode.STANDARD, D.CALLBACK, callback);
		Scheduler.addDialog("zis_save_your_progress", args, priority);
	}
}
