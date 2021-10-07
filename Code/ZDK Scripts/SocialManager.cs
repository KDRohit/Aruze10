#pragma warning disable 0618, 0168, 0414
///
/// SocialManager.cs
/// authors: Nick Reynolds, Bharath Kumar (CaslteVille Legends)
/// The MiSocialManager handles logging into and out of facebook, as well as sending requests to friends and
/// potentially posting on their walls.
///
using System.Collections;
using System.Collections.Generic;
using System;
using Zynga.Zdk;
using UnityEngine;

using System.Linq;
using Com.Scheduler;
using Zynga.Zdk.Services.Common;
using Zynga.Core.Util;
using Zynga.Core.Platform;

#if !(UNITY_WSA_10_0 && NETFX_CORE)
using System.Runtime.Remoting.Messaging;
#endif

using Zynga.Core.Tasks;
using AppleAuth.IOS.Enums;
using AppleAuth.IOS.Interfaces;
using System.Text;
using AppleAuth.IOS.Extensions;
using Zynga.Zdk.Services.Identity;
using Zynga.Authentication;
using System.Threading.Tasks;
using Zynga.Core.ZLogger;

using Log = Zynga.Core.ZLogger.Log<SocialManager>;
using Zynga.Authentication.Facebook;
using Facebook.Unity;

public class SocialManager : IDependencyInitializer
{
	

	public enum SocialLoginPreference
	{
		FirstTime = 0,
		Anonymous = 1,
		Facebook = 2,
		Apple = 3
	}


	/// Gets the instance.
	public static SocialManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new SocialManager();
			}
			return _instance;
		}
	}
	private static SocialManager _instance;

	public static bool isInGoogleUpgrade = false;
	
	//private ChannelBase channel;

	/// Delegates required
	public delegate void SocialAuthResponseCallback(object data);
	public delegate void SocialAuthErrorCallback(string errorType, string errorMessage);

	public const string kLoginPreference = "PlayerPrefs.LoginPref";
	public const string kUpgradeZid = "PlayerPrefs.UpgradeZid";
	public const string kFacebookLoginSaved = "PlayerPrefs.FacebookLoginSaved";
	public const string kAppleLoginSaved = "PlayerPrefs.AppleLoginSaved";
	public const string AppleUserIdKey = "AppleUserId";
	public const string AppleAuthorizationCode = "AppleAuthorizationCode";
	public const string AppleIdentityToken = "AppleIdentityToken";
	public const string zisPlayerId = "ZisPlayerId";
	public const string zisToken = "ZisToken";
	public const string zisIssuedAt = "ZisIssuedAt";
	public const string zisExpiresAt = "ZisExpiresAt";
	public const string zisAuthContext = "ZisAuthContext";
	public const string ssoToken = "SsoToken";
	public const string ssoIssuedAt = "SsoIssuedAt";
	public const string ssoAuthContext = "SsoAuthContext";
	public const string installCredentialsId = "installCredentialsId";
	public const string installCredentialsSecret = "installCredentialsSecret";
	public const string installCredentials = "installCredentials";
	public const string installCredentialsPresent = "installCredentialsPresent";
	public const string siwaAuthCodeIssueTime = "siwaAuthCodeIssueTime";
	public const string deAuthFBToken = "deAuthFBToken";
	public const string fbToken = "fbToken";
	public const string fbWebglToken = "fbWebglToken";
	public const string fbWebglKey = "fbWebglKey";
	public const string appleUserflow = "appleUserflow";
	public const string facebookConnectUserflow = "facebookConnectUserflow";
	public const string appleName = "appleName";
	public const string fbName = "fbName";
	public const string conflictSwitch = "conflictSwitch";
	public const string welcomeBackEmail = "welcomeBackEmail";
	public const string anonmigrate = "anonmigrate";
	public const string anonmigrateCounter = "anonmigrateCounter";

    private const string GOOGLE_ZID_UPGRADE = "GOOGLE_PLUS_ZID_MIGRATION_VERSION";
	private const string GOOGLE_ZID_UPGRADE_THRESHOLD = "GOOGLE_PLUS_ZID_MIGRATION_THRESHOLD";

	public const string EMAIL_ATTACH_SUCCESS = "EMAIL_ATTACH_SUCCESS";
	public const string PLAYER_EXISTS = "PLAYER_EXISTS";
	public const string FB_ATTACH_SUCCESS = "FB_ATTACH_SUCCESS";
	public const string FB_ATTACH_FAIL = "FB_ATTACH_FAIL";
	public const string ERROR = "ERROR";

	public const int SIWA_AUTH_CODE_EXIPRATION_TIME = 60; // 1 min in seconds
	public const int ZIS_TOKEN_EXPIRATION_TIME = 1800; // 30 min in seconds
	public const int EMAIL_LOGIN_AUTO_POPUP_LEVEL = 5; // Level to pop up email login if you are anonymous
	public ServiceSession _previousSession = null;

	public static bool emailOptIn = false;
	private InitializationManager initMgr;

	// Some static variables for tracking the number of Social login failures this session:
	private static int loginSocialFailCount = 0;
	private const int LOGIN_FAIL_RESET = 3;

	// It's a bit hacky to use member variables like this, but we need someplace to store
	// this data while we're asking the user what to do:
	private Action<Action<bool>> retryCallbackLogin;
	private Action<bool> retryCallbackSuccess;

	/// Checks if ZdkManager is ready
	private bool IsReady {
		get { return ZdkManager.Instance.IsReady; }
	}

#if UNITY_WSA_10_0 && NETFX_CORE
	public bool cancelled = false;
#endif


	public Task<Result<LogoutError>> Logout()
	{
		PreferencesBase UnityPrefs = SlotsPlayer.getPreferences();
		if (SlotsPlayer.isFacebookUser)
		{
			UnityPrefs.SetInt(kFacebookLoginSaved, 0);
		}
		UnityPrefs.SetInt(kLoginPreference, (int)SocialLoginPreference.Anonymous);
		UnityPrefs.SetInt(Prefs.USER_SELECTED_LOGOUT, 1);
		UnityPrefs.Save();
		return PackageProvider.Instance.Authentication.Flow.Logout();
	}

	/// Logs the user out of facebook?
	public void Logout(bool doAppQuit, bool userSelectedLogout = false, bool resetFirstTime = true, bool reset = true)
	{
		PackageProvider.Instance.Authentication.Flow.Logout();
	}

	public void RevokePermissions()
	{
		/*if (SlotsPlayer.isFacebookUser)
		{
			FacebookChannel channel = Packages.SocialAuthFacebook.Channel as FacebookChannel;
			channel.RevokePermissions();
		}*/
	}

	/// Logout the user from Facebook, clear stored sessions if successful. Call callback with success or failure.
	public void Logout(Action<bool> successOrFailCallback)
	{
		//TODO: Need to update this logout function to take a snid as to not logout of all snid's at once.

	}

#region IsAuthenticated functions
	/// Sessions the is authenticated.
	static bool SessionIsAuthenticated( Snid snidType)
	{
		return false;
	}

	/// Gets a value indicating whether this instance is facebook authenticated.
	public static bool IsFacebookAuthenticated
	{
		get { return SessionIsAuthenticated(Snid.Facebook); }
	}

	/// Gets a value indicating whether this instance is facebook authenticated.
	public static bool IsGoogleAuthenticated
	{
		get { return false; }
	}

	public bool hasDeclinedPerms
	{
		get
		{
			//FacebookChannel channel = Packages.SocialAuthFacebook.Channel as FacebookChannel;
			//return channel.hasDeclinedPerms;
			return false;
		}
	}

	public bool hasDeclinedFriendsPerm
	{
		get
		{
			//FacebookChannel channel = Packages.SocialAuthFacebook.Channel as FacebookChannel;
			//return channel.hasDeclinedFriendsPerm;
			return false;
		}
	}

	public bool hasInvalidFBToken
	{
		get
		{
			//FacebookChannel channel = Packages.SocialAuthFacebook.Channel as FacebookChannel;
			//return channel.hasInvalidToken;
			return false;
		}
	}

#endregion

	// Method for logging channel is being logged into
	private void logSplunk(string name, string key, string value)
	{
		Dictionary<string, string> extraFields = new Dictionary<string, string>();
		extraFields.Add(key, value);
		SplunkEventManager.createSplunkEvent("SocialManager", name, extraFields);
	}

	public async void CreateAttach(AuthenticationMethod authMethod)
	{
			await PackageProvider.Instance.Authentication.Flow.Attach(authMethod).Callback(task =>
			{
				PreferencesBase UnityPrefs = SlotsPlayer.getPreferences();
				if (task.Result.IsSuccessful)
				{
					Log.Info("attach of {0} successful, refreshing dialog", authMethod);
				    logSplunk("create-attach", "login-channel", authMethod.ToString());
					AccountDetails accountDetails = task.Result.SuccessValue;
					Log.Info("account details {0} successful", accountDetails);
					logSplunk("create-attach", "login-successful", accountDetails.ToString());

					if (authMethod == AuthenticationMethod.Facebook)
					{
						UnityPrefs.SetInt(Prefs.HAS_FB_CONNECTED_SUCCESSFULLY, 1);
						UnityPrefs.Save();
						Glb.resetGame("Successful facebook account");
					}
					else if (authMethod == AuthenticationMethod.ZyngaEmailUnverified)
					{
						
						//Server.registerEventDelegate("email_connect", emailConnectCompleted);
						//EmailConnectAction.emailConnect(accountDetails.ZisToken.Token, accountDetails.UserAccount.Email.Id);
					}
					else
					{
						ZisData.AppleName = accountDetails.UserAccount.Name;
						if (accountDetails.UserAccount.Email != null && !accountDetails.UserAccount.Email.Id.IsNullOrWhiteSpace())
						{
							ZisData.AppleEmail = accountDetails.UserAccount.Email.Id;
						}
						Glb.resetGame("Successful apple account");
						
						UnityPrefs.SetInt(Prefs.HAS_APPLE_CONNECTED_SUCCESSFULLY, 1);
						UnityPrefs.Save();
						//ZisAccountCreatedDialog.showDialog();
					}
				}
				else
				{
					Log.Info("attach of {0} unsuccessful, refreshing dialog", authMethod);
					task.Result.ErrorValue.Match(
					 	(attachConflict) =>
					{
						var authTracking = PackageProvider.Instance.Authentication.Tracking;
						var authFlow = PackageProvider.Instance.Authentication.Flow;
						authTracking.RecoverPrompt(attachConflict, TrackPromptAction.View);
						logSplunk("create-attach", "login-unsuccessful", authMethod.ToString());
						ZisAccountConflictDialog.showDialog(null, attachConflict, authMethod);
					},
					(error) =>
					{
						if (error.LoginError != LoginError.SecuringCanceledLogin)
						{
							Log.Error("Authentication action was not successful: {0}", error.LoginError);
							logSplunk("create-attach", "login-unsuccessful", error.LoginError.ToString());
						}
						string channelName;
						switch (authMethod)
						{
							case AuthenticationMethod.SignInWithApple:
								channelName = "Sign In With Apple";
								break;
							case AuthenticationMethod.Facebook:
								channelName = "Facebook";
								break;
							case AuthenticationMethod.AppleGameCenter:
								channelName = "Apple GameCenter";
								break;
							case AuthenticationMethod.GooglePlayGames:
								channelName = "Google Play Games";
								break;
							case AuthenticationMethod.ZyngaPhone:
								channelName = "Phone Number";
								break;
							case AuthenticationMethod.ZyngaSso:
								channelName = "Single Sign-On";
								break;
							case AuthenticationMethod.ZyngaEmailPassword:
							case AuthenticationMethod.ZyngaEmailUnverified:
							case AuthenticationMethod.ZyngaEmailAuthCode:
								channelName = "Email Address";
								break;
							default:
								channelName = "Unknown";
								break;
						}
						string errorMessage = "";
						bool canceled = false;
						switch (error.LoginError)
						{
							case LoginError.ChannelFailedLogin:
								Log.Error("Unable to connect using {0}", channelName);
								errorMessage = channelName + " failed to connect.";
								break;
							case LoginError.InvalidEmailAddress:
								errorMessage = "The email address provided was invalid.";
								break;
							case LoginError.AuthMethodAlreadyAttached:
								errorMessage = "You've already attached " + channelName + " to this user account.";
								break;
							case LoginError.SecuringCanceledLogin:
								canceled = true;
								errorMessage = "Canceled securing login";
								break;
							default:
								Log.Error("Unknown error occurred {0}", error.LoginError);
								errorMessage = "Unknown error occured " + error.LoginError.ToString();
								break;
						}
						Debug.LogError(string.Format("Login error occurred {0}", error.LoginError));
						logSplunk("create-attach", "login-unsuccessful", errorMessage);
						if (canceled)
						{
							canceled = false;
							ZisSignOutDialog.showDialog(Dict.create(
								D.TITLE, ZisSignOutDialog.CONNECT_FAILED_HEADER_LOCALIZATION
							));	
						}
						else
						{
							GenericDialog.showDialog(
							Dict.create(
									D.TITLE, Localize.textOr("connect_error", "Connect Error"),
									D.MESSAGE, errorMessage,
									D.REASON, "social-manager-connection-error",
									D.CALLBACK, new DialogBase.AnswerDelegate((args) =>
									{
										if (args != null)
										{
											Glb.resetGame(string.Format("Login error occurred {0}", error.LoginError));
										}
									}
									)
								),
								SchedulerPriority.PriorityType.IMMEDIATE
							);
						}
					}
					);
				}
			});
	}

	private Task<bool> PromptRecoverFlow(Dict args = null)
	{
		
		var tcs = new TaskCompletionSource<bool>();
		string answer = (string)args.getWithDefault(D.ANSWER, "");

		if (answer != "recover")
		{
			tcs.SetResult(false);
		}
		else
		{
			tcs.SetResult(true);
		}
		return tcs.Task;
	}

	public async void conflictResolveSwitchAccounts(AttachConflict attachConflict, AuthenticationMethod authMethod)
	{
		PreferencesBase UnityPrefs = SlotsPlayer.getPreferences();
		var authTracking = PackageProvider.Instance.Authentication.Tracking;
		var authFlow = PackageProvider.Instance.Authentication.Flow;
		Log.Info("recovering {0}", authMethod);
		authTracking.RecoverPrompt(attachConflict, TrackPromptAction.Submit);
		if (!authFlow.Account.IsAnonymous || !ZyngaConstants.AuthenticationFlow.RecoverAbandonedAnonymousAccounts)
		{
			await authFlow.Logout();
		}
		Result<AccountDetails,LoginErrorInfo> loginResult = await authFlow.Login(attachConflict.LoginCredentials, true);

		if (loginResult.IsSuccessful)
		{
			authTracking.RecoverPrompt(attachConflict, TrackPromptAction.Success);
			Log.Info("recovering successful");
			logSplunk("SocialManager", "Conflict-Recovering-Succcessful", attachConflict.ToString());
			
		}
		else
		{
			authTracking.RecoverPrompt(attachConflict, TrackPromptAction.Fail, loginResult.ErrorValue.LoginError.ToString());
			Log.Info("recovering unsuccessful message {0}", loginResult.ErrorInfo.Message);
			logSplunk("SocialManager", "Conflict-Recovering-Unsucccessful", loginResult.ErrorInfo.Message.ToString());
		}
		if (authMethod == AuthenticationMethod.Facebook)
        {
			UnityPrefs.SetInt(Prefs.HAS_FB_CONNECTED_SUCCESSFULLY, 1);
			UnityPrefs.Save();
		}
		SlotsPlayer.getPreferences().SetBool(SocialManager.welcomeBackEmail, false);
		Glb.resetGame("Recovering account");
		
	}

	public async void conflictResolveKeepAccounts(AttachConflict attachConflict, AuthenticationMethod authMethod)
	{
		var authTracking = PackageProvider.Instance.Authentication.Tracking;
		var authFlow = PackageProvider.Instance.Authentication.Flow;
		Log.Info("not recovering {0}", authMethod);
		authTracking.RecoverPrompt(attachConflict, TrackPromptAction.Cancel);
		logSplunk("SocialManager", "Conflict-keeping-account", attachConflict.ToString());

		// So I don't like the fact that we have to logout of Facebook explicitly here.  This
		// is normally being done by the AuthenticationFlow.  The problem is, we changed the
		// flow to simply immediately fail if there's a conflict, and the game has to deal with
		// it.  But since we don't get control back if they choose not to recover, we can't
		// automatically logout of FB for them.
		await LogoutAuthMethod(authMethod);
	}
	

	private async Task LogoutAuthMethod(AuthenticationMethod authMethod)
	{
		foreach (var channel in PackageProvider.Instance.Authentication.Flow.LoginChannels)
		{
			if (channel.AuthenticationMethod == authMethod)
			{
				Log.Trace("Logging out {0}", authMethod);
				await channel.Logout();
				break;
			}
		}
	}

	// Callback after you enter the email in the dialog
	public async void emailLogin(ZyngaAccountLoginFlowBase loginFlow, string email)
	{
		Result<AccountDetails, Either<AttachConflict, LoginErrorInfo>> result = await loginFlow.SubmitIdentifier(email);

		Debug.Log("email login");

		if (!result.IsSuccessful)
		{
			result.ErrorValue.Match(
				conflict =>
				{
					// Exit out of this dialog, and let the AccountController handle it.
					loginFlow.Fail(result.ErrorInfo);
					Debug.LogErrorFormat("email login error {0}", result.ErrorInfo.Message);
				},
				errorInfo =>
				{
					string errorMessage = "";
					switch (errorInfo.LoginError)
					{
						case LoginError.SecuringCanceledLogin:
							// Canceled -- nothing needed.  Allow the user to input an email address to try again.
							Debug.Log("nothing needed.  Allow the user to input an email address to try again.");

							break;
						case LoginError.InvalidEmailAddress:
							Debug.Log("The email address is invalid.");
							errorMessage = "The email address is invalid.";
							break;
						case LoginError.ChannelFailedLogin:
							Debug.Log("Unable to connect using this email address.");
							errorMessage = "Unable to connect using this email address.";
							break;
						case LoginError.AuthMethodAlreadyAttached:
							Debug.Log("You've already attached an email address to this user account.");
							errorMessage = "You've already attached an email address to this user account.";
							break;
						case LoginError.CaptchaVerificationFailed:
							Debug.Log("Failed to validate user with captcha.");
							errorMessage = "Failed to validate user with captcha.";
							break;
						default:
							Log.Error("Error reported by the identity login: {0}", result.ErrorInfo);
							errorMessage = "Something went wrong. Please enter another email.";
							break;
					}
					//SocialManager.Instance.Logout();
					logSplunk("SocialManager", "Email-Login", errorMessage);
					if (!errorMessage.IsNullOrWhiteSpace())
					{
						GenericDialog.showDialog(
							Dict.create(
								D.TITLE, Localize.textOr("connect_error", "Connect Error"),
								D.MESSAGE, errorMessage,
								D.REASON, "social-manager-connection-error",
								D.CALLBACK, new DialogBase.AnswerDelegate((args) =>
									{
										if (Data.webPlatform.IsDotCom && AuthManager.Instance.isInitializing)
										{
											AuthManager.Instance.Authenticate();
										}
									}
								)),
							SchedulerPriority.PriorityType.IMMEDIATE
						);
					}
				}
				
			);
		}
		else
		{
			// If successful, exit out of this dialog and let it finish in AccountController.
			Debug.LogFormat("Successful unverified email login {0}", result.SuccessValue);
			logSplunk("SocialManager", "Email-Login", result.SuccessValue.ToString());
			AccountDetails accountDetails = result.SuccessValue;
			ZisData.Email = accountDetails.UserAccount.Email.Id;
			// DotCom Doesn't need to show this during initialization.
			if (!(Data.webPlatform.IsDotCom && AuthManager.Instance.isInitializing))
			{
				ZisSignInWithEmailDialog.showDialog(ZisSignInWithEmailDialog.WELCOME_BACK_STATE, null, ZisData.Email);
			}
			if (emailOptIn)
			{
				// Send the action for emailOptin
				EmailOptOutAction.emailOptOut("false", accountDetails.UserAccount.Email.Id);
			}
		}
	}

	public void onResendPressed(ZyngaAccountAuthCodeFlowBase authCode)
	{
		authCode.ResendCode();
		ZisSignInWithEmailDialog.showDialog(ZisSignInWithEmailDialog.CONFIRMATION_SENT_STATE, null, null, authCode);
	}

	public Task onVerifyLink(string email)
	{
		return null;
	}

	public async void verifyCode(ZyngaAccountAuthCodeFlowBase authCode, string code)
	{
		Debug.LogFormat("In verify code {0}", code);
		Result<AuthSecretError> result = await authCode.SubmitCode(code);

		if (!result.IsSuccessful)
		{
			string errorMessage = "";
			switch (result.ErrorValue)
			{
				case AuthSecretError.Cancel:
					Debug.Log("Cancel is pressed");
					break;
				case AuthSecretError.Inactive:
					Debug.Log("The account is inactive.");
					errorMessage = "The account is inactive.";
					break;
				case AuthSecretError.Invalid:
					Debug.Log("Invalid auth code.  Please make sure it is correct and not expired.");
					errorMessage = "Code is invalid or expired.";
					break;
				case AuthSecretError.NotFound:
					Debug.Log("The account was not found.");
					errorMessage = "The account was not found";
					break;
				case AuthSecretError.ServerInternal:
					Debug.Log("An internal server error occurred.  Please try again.");
					errorMessage = "An internal server error occurred.  Please try again.";
					break;
				case AuthSecretError.Expired:
					Debug.Log("The provided verification code has expired.  A new one has been sent; please try again with the new code.");
					errorMessage = "The provided verification code has expired.  A new one has been sent; please try again with the new code.";
					break;
				case AuthSecretError.Unknown:
					Debug.Log("An unknown error occurred.");
					errorMessage = "An unknown error occurred.";
					break;
				default:
					Debug.Log("Default");
					break;
			}
			logSplunk("SocialManager", "verify-Code", errorMessage);
			ZisSignInWithEmailDialog.showDialog(ZisSignInWithEmailDialog.CONFIRMATION_SENT_STATE, null, null, authCode,errorMessage);

		}
		else
		{
			Debug.Log("Successful code verification");
			if (PackageProvider.Instance.Authentication.Flow.Account.GameAccount != null)
			{
				logSplunk("SocialManager", "verify-Code", PackageProvider.Instance.Authentication.Flow.Account.GameAccount.PlayerId.ToString());
			}
		}
	}

	public async void onVerifyPressed()
	{
		var authTracking = PackageProvider.Instance.Authentication.Tracking;
		authTracking.SettingsAction(TrackSettingsAction.EmailVerify, TrackPromptAction.Submit);
		var verifyResult = await PackageProvider.Instance.Authentication.Flow.VerifyAccount();
		if (verifyResult.IsSuccessful)
		{
			authTracking.SettingsAction(TrackSettingsAction.EmailVerify, TrackPromptAction.Success);
		}
		else
		{
			authTracking.SettingsAction(TrackSettingsAction.EmailVerify, TrackPromptAction.Fail);
		}
	}



	public void FBLogin()
	{
		
		FacebookWrapperBase fbWrapper = PackageProvider.Instance.Settings.FacebookSettings.FacebookWrapper;
		fbWrapper.Init(Data.fbAppId, null).Callback(task =>
		{
			fbWrapper.Login(new string[] { "email" }).Callback(task2 =>
			{
				var loginCredentials = new LoginCredentials(AuthenticationMethod.Facebook, null, null);
				var token = task2.Result.AccessToken.TokenString;
				loginCredentials = new LoginCredentials(AuthenticationMethod.Facebook, null, token);
				SlotsPlayer.getPreferences().SetString(SocialManager.fbToken, loginCredentials.Secret);
				PackageProvider.Instance.Authentication.Flow.Login(loginCredentials, true).Callback( resulttask =>
				{
					if(resulttask.Result.IsSuccessful)
					{
						Log.Info("Login of FB successful, refreshing dialog" );
						AccountDetails accountDetails = resulttask.Result.SuccessValue;
						Log.Info("account details {0} successful", accountDetails);
						Glb.resetGame("Logging into FB");
					}
					else
					{
						Log.Info("attach of FB unsuccessful, refreshing dialog {0}", resulttask.Result.ErrorValue);
						//Glb.resetGame("Logging into FB");
					}
				});
			});
		});
	}


	public async void emailChangePressed()
	{
		var authFlow = PackageProvider.Instance.Authentication.Flow;
		var result = await authFlow.UpdateEmail();
		if (result.IsSuccessful)
		{
			Log.Info("change email successful, refreshing dialog");
		}
		else
		{
			result.ErrorValue.Match(
				conflict => { Log.Error("Email successfully changed"); },
				errorInfo =>
				{
					string errorMessage = "";
					switch (errorInfo.LoginError)
					{
						case LoginError.SecuringCanceledLogin:
								// Canceled -- nothing needed.  Allow the user to input an email address to try again.
								break;
						case LoginError.InvalidEmailAddress:
							Log.Error("The email address is invalid.");
							errorMessage = "The email address is invalid.";
							break;
						case LoginError.EmailUpdateRequestExpired:
							Log.Error("The provided verification code has expired.  A new one has been sent; please try again with the new code.");
							errorMessage = "The provided verification code has expired.  A new one has been sent; please try again with the new code.";
							break;
						default:
							Log.Error("Error reported by the identity login: {0}", result.ErrorInfo);
							errorMessage = "Error reported by the identity login: Please retry with different email";
							break;
					}
					
					if (!errorMessage.IsNullOrWhiteSpace())
					{
						GenericDialog.showDialog(
						Dict.create(
								D.TITLE, Localize.textOr("connect_error", "Connect Error"),
								D.MESSAGE, errorMessage,
								D.REASON, "social-manager-connection-error"
							),
							SchedulerPriority.PriorityType.IMMEDIATE
						);
					}
					
				});
		}
	}

	/// logs in through facebook
	public void FacebookLogin(Action<bool> successOrFailCallback)
	{
		
	}

	private void socialLogin (Action<bool> successOrFailCallback)
	{
		
	}

	// This function is called when facebook is connected and apple is logged in
	public void FBConnect() 
	{
		FacebookLogin(FBConnectFinished);
	}

	//Callback from when FB connect finishes
	private void FBConnectFinished(bool success)
	{

	}

	// Check to see if the email is verified if not show account created dialog else welcome back dialog
	public void CheckEmailVerifiedOnGameLoad()
    {
		if (SlotsPlayer.IsEmailLoggedIn && !SlotsPlayer.isFacebookUser && !SlotsPlayer.IsAppleLoggedIn)
		{
			if (PackageProvider.Instance.Authentication.Flow.Account.UserAccount.Email.Verified)
			{
				Server.registerEventDelegate("email_connect", emailConnectCompleted);
				EmailConnectAction.emailConnect(PackageProvider.Instance.Authentication.Flow.Account.ZisToken.Token, PackageProvider.Instance.Authentication.Flow.Account.UserAccount.Email.Id);
			}
		}
	}


	// Sets the email opt in for email login
	public void setEmailOptIn()
    {
		if (Glb.showEmailOptIn != 0)
		{
			SocialManager.emailOptIn = true;
		}
		else
		{
			SocialManager.emailOptIn = false;
		}
	}

	//Callback for when Email connect returned from server
	private void emailConnectCompleted(JSON data)
	{
		Debug.LogFormat("AppleLogin: email connect completed data from server {0}", data.ToString());
		string status = data.getString("status", "");
		JSON playerData = data.getJSON("player_data");


		// These dialogs should not surface
		// for dotcom.
		if (!Data.webPlatform.IsDotCom) 
		{
			if (status == SocialManager.EMAIL_ATTACH_SUCCESS)
			{
				Debug.LogFormat("AppleLogin: email attach success {0}", playerData.ToString());
				logSplunk("SocialManager", "email-attach-success", data.ToString());
				ZisAccountCreatedDialog.showDialog(playerData);
			}
			else
			{
				logSplunk("SocialManager", "email-attach-failed", data.ToString());
				showWelcomeBackDialog(ZisData.Email);
			}
		}
	}

	private void showWelcomeBackDialog(string email)
	{
		PreferencesBase preferences = SlotsPlayer.getPreferences();
		if (!preferences.HasKey(SocialManager.welcomeBackEmail))
		{
			preferences.SetBool(SocialManager.welcomeBackEmail, false);
		}
		if (!preferences.GetBool(SocialManager.welcomeBackEmail))
		{
			ZisSignInWithEmailDialog.showDialog(ZisSignInWithEmailDialog.WELCOME_BACK_STATE, null, email);
			preferences.SetBool(SocialManager.welcomeBackEmail, true);
		}
		preferences.Save();
	}


#if !(UNITY_WSA_10_0 && NETFX_CORE)
	/// Publish feed on SN.
	public void PublishFeedOnSN(string feedname, string caption, string desc, Dictionary<string,string> mapOfTrackData, Dictionary<string,string> mapOfArgs, MethodCall reqCb){


	}
#endif


#if !(UNITY_WSA_10_0 && NETFX_CORE)
    /// Publishs the feed to friend on the current SN.
    public void PublishFeedToFriendOnSN(string testzid,string feedname, string caption, string desc, Dictionary<string,string> mapOfTrackData, Dictionary<string,string> mapOfArgs, MethodCall reqCb){


	}
#endif

	/// successfully grabbed friends, now parse
	public void OnSuccessCB(List<Dictionary<string,string>> arg) {
		
	}

	/// did not grab friends successfully
	public void OnErrorCB(int code, string msg)
	{
		Debug.Log("code=" + code + " msg="+msg);
	}

	/// Send a request to a social network, with arguments.
	public void SendRequestToFriends(Snuid[] friendSnuidList, Dictionary<string, string> args, string msg, System.Action<List<string>> reqCb, System.Action<string> errCb)
	{
		/*// Before sending reqest to friends check and see if the channel is enabled or not.
		if (Packages.SocialAuthFacebook.Channel != null && !Packages.SocialAuthFacebook.Channel.IsEnabled)
		{
			return;
		}

		var requestStream = Packages.RequestStream;
		// Convert data from Gift struct to format requestStream wants.
		var data = new Dictionary<string, object>();
		foreach(KeyValuePair<string,string> pair in args)
		{
			data[pair.Key] = pair.Value;
		}
		requestStream.SendRequest(msg, data, friendSnuidList).Callback(task => {
				Debug.Log("SendRequest returned " + task.Result.ToString());
				if (task.Result.IsSuccessful)
				{
					var successIds = new List<string>();
					Zynga.SocialAuth.Facebook.SendRequestResult result = task.Result;
					foreach(Snuid key in result.SentRequestIds.Keys)
					{
						successIds.Add(key.ToString());
					}

					if (successIds.Count > 0)
					{
						reqCb(successIds);
					}
					else
					{
						// We should call the error callback here with some messaging saying that the list was empty for reasons.
						Bugsnag.LeaveBreadcrumb("Gift Request error:  Could not SendRequestToFriends, successIds was empty");
						errCb("Could not SendRequestToFriends, successIds was empty");
					}	
				}
				else
				{
					errCb("Could not SendRequestToFriends");
					
					if (Glb.logRequestError)
					{
						Dictionary<string, string> extraFields = new Dictionary<string, string>();
						extraFields.Add("fb_request_error", task.Result.ErrorInfo.ToString());
						SplunkEventManager.createSplunkEvent("Social Manager Send Request", "gift-request-error", extraFields);
					}

					reqCb(null);
				}
			});
			*/
	}

#region ISVDependencyInitializer implementation
	/// The AuthManager is dependent on GameSession
	public System.Type[] GetDependencies()
	{
		return new System.Type[] { typeof (AuthManager), typeof (ExperimentManager) } ;	
	}

	/// Initializes the SocailManager
	public void Initialize(InitializationManager mgr)
	{
		if (mgr == null)
		{
			throw new System.Exception("null parameter passed to SocialAuth::Initialize. Cannot initialize.");
		}

		
		initMgr = mgr;
		StartLogin();

	}

	// short description of this dependency for debugging purposes
	public string description()
	{
		return "SocialManager";
	}

#endregion

	/// Go through steps to find which flow we should go through (already logged in, need to show login screen, need to login, continue as anonymous)
	private void StartLogin()
	{
		PreferencesBase UnityPrefs = SlotsPlayer.getPreferences();
		
#if UNITY_WEBGL && !UNITY_EDITOR
		if (Data.canvasBasedConfig != null)
		{

			string installDate = PlayerPrefsCache.GetString(Prefs.FIRST_APP_START_TIME, null);
			if (string.IsNullOrEmpty(installDate) && WebGLFunctions.isLocalStorageAvailable())
			{
				installDate = WebGLFunctions.getLocalStorageItem(Prefs.FIRST_APP_START_TIME);
			}

			//must check if local storage is available to verify install date
			bool isFirstTime = ((string.IsNullOrEmpty(installDate) || NotificationManager.DayZero) && WebGLFunctions.isLocalStorageAvailable());
			if (isFirstTime)
			{
				TOSDialog.showDialog((args) =>
				{
					passCompleteToManager();
				}, true);
			}
			else
			{
				passCompleteToManager();

			}
		}
		else
#endif
		{
			//if a player deletes their cache this will trigger again
			bool isFirstTime = UnityPrefs.GetInt(kLoginPreference) == (int)SocialManager.SocialLoginPreference.FirstTime;
			if (isFirstTime)
			{
				// Perform first time login.
				firstTimeLogin();
			}
			else
			{
				passCompleteToManager();
			}
		}
	}

	private static void FacebookAuthed(int success)
	{
		
	}

	public static void antisocialCallback(Dict args)
	{
		string answer = (string)args.getWithDefault(D.ANSWER, "");
		if (answer != "login")
		{
			return;
		}

		// Do the login.
		if (!SlotsPlayer.IsAppleLoggedIn)
		{
			SlotsPlayer.facebookLogin();
		}
	}

	// Method for handling the different first time auth flows.
	private void firstTimeLogin()
	{
		Loading.hide(Loading.LoadingTransactionResult.SUCCESS);
		TOSDialog.showDialog((args)=> 
		{
			TOSDialog.GDPRUpgrade();
			PreferencesBase UnityPrefs = SlotsPlayer.getPreferences();
			UnityPrefs.SetInt(kLoginPreference, (int)SocialLoginPreference.Anonymous);
			UnityPrefs.Save();
			passCompleteToManager();
		}, true);
	}

	private void acceptTOSCallback(Dict args)
	{
		PreferencesBase UnityPrefs = SlotsPlayer.getPreferences();
		StatsManager.Instance.LogMileStone("standard_auth_flow", 1);
		// Splash screen asking the user to sign in.
		UnityPrefs.SetInt(kLoginPreference, (int)SocialManager.SocialLoginPreference.FirstTime);
#if UNITY_WEBGL && !UNITY_EDITOR
		FacebookLogin(ForceFBLoginFinished);	
#else
		checkFBAuthDialog();
#endif
		checkForSufficentDiskSpace();
	}

	private void checkForSufficentDiskSpace()
	{
		long minAmountOfBytesRequired = Data.liveData.getLong("MIN_NUMBER_OF_FREE_BYTES_LOGIN", -1);
		if (minAmountOfBytesRequired > 0)
		{
			#if UNITY_IPHONE && !UNITY_EDITOR
			long freeBytesAvailable = MemoryHelper.UnityGetAvailableDiskSpace();
			if (freeBytesAvailable < minAmountOfBytesRequired)
			{
				Debug.LogError("Not enough hard disk space available for inital lobby asset bundles. Bytes available: " + freeBytesAvailable.ToString());
				GenericDialog.showDialog(Dict.create(D.TITLE, "Low Memory", D.MESSAGE, "Your device might encounter errors while playing. Please clear up some memory.", D.REASON, "social-manager-out-of-space"), SchedulerPriority.PriorityType.IMMEDIATE);
			}
			#endif
		}
	}

	public void facebookAccountAdded(JSON data)
	{
		ZisData.setApplePreferences(data);

		PreferencesBase preferences = SlotsPlayer.getPreferences();

		string zisTokenValue = preferences.GetString(SocialManager.zisToken);
		string zisPlayerIdentity = preferences.GetString(SocialManager.zisPlayerId);
		string zisTokenExpire = preferences.GetString(SocialManager.zisExpiresAt);

		Debug.LogFormat("AppleLogin: facebookAccountAdded zisToken {0} zisplayeridentity {1} zisTokenExpire {2}", zisTokenValue, zisPlayerIdentity, zisTokenExpire);

		if (ZdkManager.Instance.Zsession != null)
		{
			Debug.Log("AppleLogin: facebookAccountAdded setting new token");
			System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
			dtDateTime = dtDateTime.AddSeconds(double.Parse(zisTokenExpire));
			ZdkManager.Instance.Zsession.SetToken(zisTokenValue, dtDateTime);
		} 
		else 
		{
			Debug.Log("AppleLogin: facebookAccountAdded session is null");
		}
	}


	/// Callback for when facebook login finishes, could either be successful or a failure. Continue login process if successful (and set player prefs), reset game if failure.
	private void ForceFBLoginFinished(bool success)
	{
		
	}

	public void SocialLoginFailed(Action<Action<bool>> retryLoginCallback, Action<bool> successOrFailCallback)
	{
		PreferencesBase UnityPrefs = SlotsPlayer.getPreferences();
		
		Debug.LogError("SocialLoginFailed: Social Network login failed. Asking the user how to continue.");
		loginSocialFailCount += 1;
		Loading.hide(Loading.LoadingTransactionResult.FAIL);

		if (loginSocialFailCount < LOGIN_FAIL_RESET)
		{
			retryCallbackLogin = retryLoginCallback;
			retryCallbackSuccess = successOrFailCallback;

			if (!hasInvalidFBToken) {
				ZisSignOutDialog.showDialog(Dict.create(
				D.TITLE, ZisSignOutDialog.CONNECT_FAILED_HEADER_LOCALIZATION
			));	
			}
			else
			{
				retryPrompt(Dict.create(D.ANSWER, "2"));
			}

		}
		else
		{
			//If login has failed too many times then we'll login anonomously and make sure to show the dialog to the player
			//telling them to connect to FB through the settings menu
			retryPrompt(Dict.create(D.ANSWER, "2"));	
			UnityPrefs.SetInt(Prefs.FACEBOOK_CONNECT_FAILED, 1);
			UnityPrefs.Save();
		}
	}

	// Callback when retry confirmation question is clicked.
	public void retryPrompt(Dict args)
	{

		// Copy values before clearing the member variables - to avoid stepping on our own toes:
		Action<Action<bool>> localRetryCallbackLogin = retryCallbackLogin;
		Action<bool> localRetryCallbackSuccess = retryCallbackSuccess;

		retryCallbackLogin = null;
		retryCallbackSuccess = null;

		PreferencesBase UnityPrefs = SlotsPlayer.getPreferences();

		if ((string)args.getWithDefault(D.ANSWER, "") == "1")
		{
			// 1. Retry:
			Loading.show(Loading.LoadingTransactionTarget.LOBBY_LOGIN);
			if (Glb.NEW_RETRY_LOGIC == true) {
				UnityPrefs.SetInt (kLoginPreference, (int)SocialManager.SocialLoginPreference.Facebook);
				UnityPrefs.SetInt (SocialManager.kUpgradeZid, 1);
				UnityPrefs.Save ();
				Glb.resetGame ("SocialManager.retryPrompt(facebook)");
			} else {
				localRetryCallbackLogin(localRetryCallbackSuccess);
			}
		}
		else if (((string)args.getWithDefault(D.ANSWER, "") == "2") ||
			((string)args.getWithDefault(D.ANSWER, "") == "no"))
		{
			// 2. Cancel, go around again:
			Loading.show(Loading.LoadingTransactionTarget.LOBBY_LOGIN);
			UnityPrefs.SetInt(kLoginPreference, (int)SocialManager.SocialLoginPreference.Anonymous); // Just login anonymous for now.
			UnityPrefs.SetInt(kFacebookLoginSaved, 0); // Need to clear this also in order to login anonymous.
			UnityPrefs.SetInt(SocialManager.kUpgradeZid, 0);
			UnityPrefs.Save();
			loginSocialFailCount = 0;
			Glb.resetGame("SocialManager.retryPrompt(anonymous)");
		}

	}

	// Callback when reset confirmation question is clicked.
	public void resetPrompt(Dict args)
	{
		PreferencesBase UnityPrefs = SlotsPlayer.getPreferences();
		if ((string)args.getWithDefault(D.ANSWER, "") == "1")
		{
			// 1. Reset:
			UnityPrefs.SetInt(kLoginPreference, (int)SocialManager.SocialLoginPreference.FirstTime);
			UnityPrefs.SetInt(SocialManager.kUpgradeZid, 0);
			UnityPrefs.Save();
			loginSocialFailCount = 0;
			Login.loginFailedPromptRestart();
		}
		else if (((string)args.getWithDefault(D.ANSWER, "") == "2") ||
			((string)args.getWithDefault(D.ANSWER, "") == "no"))
		{
			// 2. Cancel, go around again:
			Loading.show(Loading.LoadingTransactionTarget.LOBBY_LOGIN);
			UnityPrefs.SetInt(kLoginPreference, (int)SocialManager.SocialLoginPreference.FirstTime);
			UnityPrefs.SetInt(SocialManager.kUpgradeZid, 0);
			UnityPrefs.Save();
			loginSocialFailCount = 0;
			Glb.resetGame("SocialManager.resetPrompt()");
		}
	}

	public void statsCallbackAfterLogin(Snid snid, ServiceSession session1)
	{
		PreferencesBase UnityPrefs = SlotsPlayer.getPreferences();
# if !UNITY_WEBGL
		// Do not associate when using FB canvas on WebGL!
		if (snid != Snid.Anonymous && UnityPrefs.GetInt(Prefs.UPGRADE_FROM_SOCIAL_SCREEN, 0) == 1)
		{
			//If we logged in from the social screen, at this time we need to fire a snuid_device_mappings call.
			//This is separate from if the user logs into either social network from other flows, since in this flow, the process can be
			//lost due to the app restarting. Additionally, we aren't upgrading the user from one account to another, and we want to track
			//*all* social network transitions. This is largely to fill holes.
			long previousSessionSnid = (long)_previousSession.Snid;
			long previousSessionZid = long.Parse(_previousSession.Zid.ToString());
			string androidDeviceId = "";
	#if UNITY_ANDROID
			androidDeviceId = Zynga.Slots.ZyngaConstantsGame.androidDeviceID;
	#endif
			StatsManager.Instance.LogAssociate("snuid_device_mapping", Zynga.Slots.ZyngaConstantsGame.deviceAdvertisingID, ZyngaConstants.GameSkuVersion, SystemInfo.operatingSystem, StatsManager.DeviceModel, "", "", previousSessionSnid, previousSessionZid, androidDeviceId);
			UnityPrefs.SetInt(Prefs.UPGRADE_FROM_SOCIAL_SCREEN, 0);
		}

#endif

		// While we're here, check for 'install' of the new SNs. We are adding it here because the timing of LogVisit may prevent SN's from having the auth set.
		AnalyticsManager.Instance.CheckLogInstall();

		UAWrapper.Instance.OnCompleteAnyLoginMethod();
	}

	/// Set the player preferences
	public void setPreferences(SocialManager.SocialLoginPreference platform)
	{
		PreferencesBase UnityPrefs = SlotsPlayer.getPreferences();
#if UNITY_WSA_10_0 && NETFX_CORE
		if (!UnityEngine.WSA.Application.RunningOnAppThread())
		{
			UnityEngine.WSA.Application.InvokeOnAppThread(() =>
			{
				UnityPrefs.SetInt(kLoginPreference, (int)platform);
				UnityPrefs.SetInt(kFacebookLoginSaved, platform == SocialLoginPreference.Facebook ? 1 : 0);
				UnityPrefs.Save();
			}, false);
		}
		else
#endif
		if (platform == SocialLoginPreference.Facebook)
		{
			UnityPrefs.SetInt(kLoginPreference, (int)platform);
			UnityPrefs.SetInt(kFacebookLoginSaved, platform == SocialLoginPreference.Facebook ? 1 : 0);
			UnityPrefs.SetInt(Prefs.HAS_FB_CONNECTED_SUCCESSFULLY, 1);
			UnityPrefs.Save();
		}
		else if (platform == SocialLoginPreference.Apple)
		{
			UnityPrefs.SetInt(kLoginPreference, (int)platform);
			UnityPrefs.SetInt(kAppleLoginSaved, platform == SocialLoginPreference.Apple ? 1 : 0);
			UnityPrefs.SetInt(Prefs.HAS_APPLE_CONNECTED_SUCCESSFULLY, 1);
			UnityPrefs.Save();
		}
	}

	private void checkFBAuthDialog()
	{
		int loginCount = PlayerPrefsCache.GetInt(Prefs.LOGIN_COUNT, 1);
		int fbDialogViewCount = PlayerPrefsCache.GetInt(Prefs.FB_DIALOG_VIEW_COUNT, 0);
		
		//We sill want to force show the dialog on first load
		//Without the FB login page, we don't have an opportunity for new users to see our privacy policy and TOS pages before they start playing. We need this for legal reasons.
		if ((loginCount >= PlayerPrefsCache.GetInt(Prefs.FB_AUTH_COUNT) && fbDialogViewCount < PlayerPrefsCache.GetInt(Prefs.FB_AUTH_SESSION_NUM)))
		{
			Loading.hide(Loading.LoadingTransactionResult.SUCCESS);
			Login.instance.show();
		}
		else
		{
			passCompleteToManager();
		}
	}

	/// Inform the social manager that the initialization is complete that was reliant upon some kind of login from another class
	public void passCompleteToManager()
	{
		if (initMgr == null)
		{
			throw new System.Exception("Cannot complete initialization because initMgr is not initialized. Dependency error.");
		}
		else
		{
			Bugsnag.LeaveBreadcrumb("SocialManager: user logged in");

			if ( Loading.isLoading )
			{
				Loading.instance.clearResetTimer();
			}
			
			int loginCount = PlayerPrefsCache.GetInt(Prefs.LOGIN_COUNT, 1);
			PlayerPrefsCache.SetInt(Prefs.LOGIN_COUNT, ++loginCount);
			initMgr.InitializationComplete(this);
		}
	}
}
#pragma warning restore 0618, 0168, 0414
