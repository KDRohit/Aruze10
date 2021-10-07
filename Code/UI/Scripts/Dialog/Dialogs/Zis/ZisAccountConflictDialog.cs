using UnityEngine;
using System.Collections;
using Com.Scheduler;
using TMPro;
using System;
using Zynga.Core.Util;
using Zynga.Authentication;
using Zynga.Zdk.Services.Identity;

public class ZisAccountConflictDialog : DialogBase
{
	public ClickHandler continueButtonClickHandler;
	public ClickHandler switchButtonClickHandler;
	public MeshRenderer playerpic1;
	public MeshRenderer playerpic2;
	public VIPNewIcon vipIcon1;
	public VIPNewIcon vipIcon2;
	[SerializeField] private TextMeshPro headerLabel;
	[SerializeField] private TextMeshPro subHeaderLabel;
	[SerializeField] private TextMeshPro newPlayerZid;
	[SerializeField] private TextMeshPro facebookNameLabel;
	[SerializeField] private TextMeshPro currentPlayerZid;
	[SerializeField] private TextMeshPro facebookLevelLabel;
	[SerializeField] private TextMeshPro continueButtonLabel;
	[SerializeField] private TextMeshPro switchButtonLabel;
	private AttachConflict currentData = null;
	private AuthenticationMethod currentAuthData = AuthenticationMethod.Facebook;

	private const string ZID_NUMBER_PRE_FIX = "ZID: ";
	//Setting up the ZIS account conflict dialog
	public override void init()
	{
		StatsZIS.logZisAssociatedAccount(genus:"view");
		currentData = dialogArgs.getWithDefault(D.CUSTOM_INPUT, null) as AttachConflict;
		currentAuthData = (AuthenticationMethod )dialogArgs.getWithDefault(D.DATA, AuthenticationMethod.Facebook);
		
		continueButtonClickHandler.registerEventDelegate(onContinueButtonClicked);
		switchButtonClickHandler.registerEventDelegate(onSwitchButtonClicked);
		
		if (newPlayerZid != null && currentData != null)
		{
			string playerName = "";
			switch (currentAuthData)
			{
				case AuthenticationMethod.Facebook:
					playerName = "Facebook Id";
					break;
				case AuthenticationMethod.SignInWithApple:
					playerName = "Apple Id";
					break;
				default:
					playerName = "Email Id";
					break;
			}

			if (playerName == "" && !String.IsNullOrEmpty(ZisSignInWithEmailDialog.inputText))
			{
				playerName =  ZisSignInWithEmailDialog.inputText;
			}
			if (headerLabel != null)
			{
				headerLabel.text = playerName;
			}
			newPlayerZid.text = ZID_NUMBER_PRE_FIX + currentData.PlayerId;
		}
		
		if (currentPlayerZid != null)
		{
			currentPlayerZid.text = ZID_NUMBER_PRE_FIX + PackageProvider.Instance?.Authentication?.Flow?.Account?.GameAccount?.PlayerId ?? "";
		}
		
	}


	private void onContinueButtonClicked(Dict args = null)
	{
		SocialManager.Instance.conflictResolveKeepAccounts(currentData,currentAuthData);
		Dialog.close(this);
	}

	private void onSwitchButtonClicked(Dict args = null)
	{ 
		SocialManager.Instance.conflictResolveSwitchAccounts(currentData,currentAuthData);
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
		StatsZIS.logZisAssociatedAccount(genus:"close");
		// Do special cleanup. Downloaded textures are automatically destroyed by Dialog class.
		SlotsPlayer.getPreferences().DeleteKey(SocialManager.fbToken);
		SlotsPlayer.getPreferences().Save();
		SlotsPlayer.IsFacebookConnected = false;
		//SocialManager.Instance.Logout(false, false, false, false); //This is done to logout from the current login process initiated by click the connect button in the manage dialog
	}

	public static void showDialog(JSON data, AttachConflict attachConflict = null, AuthenticationMethod authMethod = AuthenticationMethod.Facebook)
	{
		;
		if (attachConflict != null && attachConflict.PlayerId != null)
		{
			Debug.LogFormat("Securing conflict on {0} with player {1}", attachConflict.LoginCredentials.Type, attachConflict.PlayerId);
		}
		//D.CALLBACK, new DialogBase.AnswerDelegate(SocialManager.Instance.conflictCallback)
		Dict args = Dict.create(
			D.CUSTOM_INPUT, attachConflict,
			D.DATA, authMethod,
			D.STACK, false
		);
		Scheduler.addDialog("zis_account_conflict", args);
	}	
}
