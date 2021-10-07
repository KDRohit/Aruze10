#pragma warning disable 0618, 0168, 0414
using System;
using System.IO;
using System.Collections.Generic;
using Zynga.Zdk;
using UnityEngine;
using System.Collections;
using Zynga.Zdk.Services.Common;
using Zynga.Slots;
using Zynga.Core.UnityUtil;
using System.Threading.Tasks;
using Zynga.Core.Tasks;
using Zynga.Authentication;
using Zynga.Core.Util;
using Zynga.Authentication.Facebook;
using Zynga.Zdk.Services.Identity;
using Facebook.Unity;
using Zynga.Zdk.Services.Auth;
using Com.Scheduler;
using System.Collections.ObjectModel;
using Zynga.Core.JsonUtil;
using Zynga.Core.ZLogger;

public class AuthManager : IDependencyInitializer
{
	//response status code for game level
	public const string RESPONSE_STATUS_NOERROR = "0";
	public const string RESPONSE_STATUS_ERROR = "-1";
	public const string RESPONSE_STATUS_ERROR_TIMEOUT = "-2";
	public const string RESPONSE_DATA_EMPTY = "";
	//private ChannelBase channel;

	//general callback format and signiture of callback
	public delegate void AuthResponseHandler(string responseData, string errorMessage);

	private InitializationManager initMgr;

	//Session key that stores anon session details on the client. Needed for migration to ZIS
	private const string AnonSessionKey = "anonkey";

	public bool isInitializing { get; private set; }

	int count = 0;

	/// Gets the instance of AuthManager through Globals
	static public AuthManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new AuthManager();
			}
			return _instance;
		}
	}

	static private AuthManager _instance;



	/// Authenticate - anon or FB login through MSC or ZDK
	public void Authenticate()
	{
		isInitializing = true;
		Debug.LogFormat("AuthManager.Authenticate() running serverzid {0}", ZyngaConstantsGame.auth_zid);
		logSplunk("serverzid", ZyngaConstantsGame.auth_zid);
		PreferencesBase prefs = SlotsPlayer.getPreferences();

#if UNITY_WEBGL

		if (Data.webPlatform.IsDotCom)
		{
			// Special flow for dotcom. Attempt to recover existing login. If fails,
			// we'll show a prompt.
			processDotComResult(PackageProvider.Instance.Authentication.Flow.LoginExisting());
			IdleWatch.disable();
		}
		else // Then, facebook
		{
			if (prefs.HasKey(SocialManager.fbWebglKey))
			{
				Debug.Log("Authamanger deleting key");
				prefs.DeleteKey(SocialManager.fbWebglKey);
				prefs.Save();
			}
			if (Glb.deleteWebglAccountStore)
			{
				PackageProvider.Instance.Authentication.AccountStore.DeleteAll();
			}
			if (PackageProvider.Instance.Authentication.AccountStore.GetLastUsedAccount() != null)
			{
				processResult(PackageProvider.Instance.Authentication.Flow.Login());
			}
			else
			{
				processResult(PackageProvider.Instance.Authentication.Flow.Login(AuthenticationMethod.Facebook, true));
			}
		}
#else
		if (SlotsPlayer.getPreferences().GetInt(SocialManager.kFacebookLoginSaved, 0) == 1)
		{
			Debug.Log("In Facebook login");
			processResult(PackageProvider.Instance.Authentication.Flow.Login(AuthenticationMethod.Facebook));
			prefs.DeleteKey(SocialManager.kFacebookLoginSaved);
			prefs.Save();
		}
		else if (SlotsPlayer.getPreferences().GetInt(SocialManager.kLoginPreference) == (int)SocialManager.SocialLoginPreference.Apple)
		{
			Debug.Log("In Apple login");
			processResult(PackageProvider.Instance.Authentication.Flow.Login(AuthenticationMethod.SignInWithApple));
			
			prefs.DeleteKey(SocialManager.kLoginPreference);
			prefs.Save();
		}
#if !UNITY_EDITOR//Special case where the anon user is getting a different zid after migrating. Controlled by Livedata key 
		else if (Glb.loginAnonUser && PackageProvider.Instance.Authentication.AccountStore.GetLastUsedAccount() != null &&
			PackageProvider.Instance.Authentication.AccountStore.GetLastUsedAccount().IsAnonymous &&
			!Zynga.Slots.ZyngaConstantsGame.install_credentials.IsNullOrWhiteSpace())
		{
			string clientZid = PackageProvider.Instance.Authentication.AccountStore.GetLastUsedAccount().GameAccount.PlayerId.ToZid().ToString();
			string serverZid = ZyngaConstantsGame.auth_zid;
			Debug.Log("AuthManager anon migrate issue");
			logSplunk("in-anon-migrate-issue", clientZid);
			if (!clientZid.Equals(serverZid))
			{
				anonLoginInstallCredentials(true);
			}
			else
			{
				processResult(PackageProvider.Instance.Authentication.Flow.Login());
			}
		}
		else if (!Zynga.Slots.ZyngaConstantsGame.install_credentials.IsNullOrWhiteSpace() && PackageProvider.Instance.Authentication.AccountStore.GetLastUsedAccount() == null)
		{
			//Example: {"id":"82912786575","secret":"b7ac945df323f07f"}
			anonLoginInstallCredentials(false);
		}
#endif
		else
		{
			Debug.Log("Authmanager check if migration required");
#if UNITY_EDITOR
			processResult(PackageProvider.Instance.Authentication.Flow.Login());
#else
			//check if migration is required
			if (!checkZidAndMigrate())
			{
				//If not migration then just regular login.
				Debug.Log("In regular login into last account");
				processResult(PackageProvider.Instance.Authentication.Flow.Login());

			}
#endif
		}
#endif
	}

	//Process dotcom result
	private async void processDotComResult(Task<Result<AccountDetails, LoginErrorInfo>> taskResult)
	{
		await taskResult.Callback(task =>
		{
			Debug.Log($"processDotComResult task.Result.IsSuccessful = {task.Result.IsSuccessful}");
			if (task.Result.IsSuccessful)
			{
				AccountDetails details = task.Result.SuccessValue;
				Debug.Log($"AuthManager.processDotComResult details.ZisToken.AuthenticationContexts.Contains(AuthenticationMethod.Facebook) = {details.ZisToken.AuthenticationContexts.Contains(AuthenticationMethod.Facebook)}");
				// Only resume login for EMAIL + verfied accounts or facebook accounts.
				if ((details.ZisToken.AuthenticationContexts.Contains(AuthenticationMethod.ZyngaEmailAuthCode) &&
				     details.UserAccount.Email.Verified)
				    || details.ZisToken.AuthenticationContexts.Contains(AuthenticationMethod.Facebook))
				{
					//if(details.UserAccount.Email.Verified) details.
					// If we logged in successfully, pass this forward to the normal handler.
					processResult(taskResult);
					return;
				}
			}
			else
			{
				Debug.Log($"AuthManager.processDotComResult error = {task.Result.ErrorInfo.Error.ToString()} message = {task.Result.ErrorInfo.Message}");
			}

			// If we're DotCom, we need to show a login prompt for either facebook or email login.
			ZisSaveYourProgressDialog.showDialog(true, SchedulerPriority.PriorityType.IMMEDIATE, platform =>
			{
				if (platform == "facebook")
				{
					processResult(PackageProvider.Instance.Authentication.Flow.Login(AuthenticationMethod.Facebook, true));
				}
				else if (platform == "email")
				{
					processResult(PackageProvider.Instance.Authentication.Flow.Login(AuthenticationMethod.ZyngaEmailUnverified, true));
				}
				else
				{
					Debug.LogErrorFormat("AuthManager: Invalid platform selected: {0} ", platform);
				}
			});
		});
	}

	// Process the result
	private async void processResult(Task<Result<AccountDetails, LoginErrorInfo>> taskResult)
	{
	   await taskResult.Callback(task =>
	   {
		   bool continueLogin = true;
		   PreferencesBase UnityPrefs = SlotsPlayer.getPreferences();

		   if (!task.Result.IsSuccessful)
		   {
			   Dictionary<string, string> extraFields = new Dictionary<string, string>();
			   extraFields.Add("error", task.Result.ErrorInfo.Message);
			   SplunkEventManager.createSplunkEvent("AuthManager Connection Failure", "connection-failed", extraFields);
			   Debug.LogErrorFormat("AuthManager: failed to connect to channel: {0} ", task.Result.ErrorInfo.Error + " " + task.Result.ErrorInfo.Message);
			   if (count <= 2 && Glb.retryZisLogin)
               {
				   count++;
				   processResult(PackageProvider.Instance.Authentication.Flow.Login());
			   }
			   else if (Data.webPlatform.IsDotCom)
				{
					// in case the player close the facebook login screen without doing anything, this dialog
					// show two links Reload and Support at the bottom of screen
					ZisReloadSupportDialog.showDialog();
				}
		   }
		   else
		   {
				bool loginComplete = true;
				count = 0;
			   AccountDetails accountDetails = task.Result.SuccessValue;
			   Debug.LogFormat("Logged Into: {0}", accountDetails);
			   if (SlotsPlayer.IsEmailLoggedIn && !SlotsPlayer.isFacebookUser && !SlotsPlayer.IsAppleLoggedIn)
			   {
				   ZisData.Email = accountDetails.UserAccount.Email.Id;
				   if (!accountDetails.UserAccount.Email.Verified)
				   {
					   if (Data.webPlatform.IsDotCom)
					   {
						   loginComplete = false;
					   }
					   else
					   {
						   logSplunk("email-verify-dialog", accountDetails.UserAccount.Email.Id);
						   GenericDialog.showDialog(
							Dict.create(
									D.TITLE, Localize.textOr("Verify Email", "Connect Error"),
									D.MESSAGE, "Go to " + accountDetails.UserAccount.Email.Id + " and verify your email",
									D.REASON, "social-manager-connection-error",
									D.OPTION1, Localize.textOr("Login Anonymous", "Login Anonymous"),
									D.OPTION2, Localize.textOr("Verify and Reload", "Verify and Reload"),
									D.CALLBACK, new DialogBase.AnswerDelegate((args) =>
									{
										if (args != null)
										{
											if ((string)args.getWithDefault(D.ANSWER, "") == "1")
											{
												logSplunk("email-verify-dialog-callback", "login-anonymous");
												PackageProvider.Instance.Authentication.Flow.Logout();
												Glb.resetGame(string.Format("Login anonymous"));
											}
											else
											{
												logSplunk("email-verify-dialog-callback", "verify-reload");
												Glb.resetGame(string.Format("Reload and verify"));
											}
										}
									}
									)
								),
								SchedulerPriority.PriorityType.IMMEDIATE
							);
					   }
				   }
				   else
				   {
					   Debug.Log("AuthManager in email connect");
				   }
			   }
			   else
			   {
				   Debug.Log("AuthManager in email is not connected");
			   }

			   if (SlotsPlayer.isFacebookUser)
			   { 
				   ZisData.FacebookName = accountDetails.UserAccount.Name;
				   if (accountDetails.UserAccount.Email != null)
				   {
					   ZisData.FacebookEmail = accountDetails.UserAccount.Email.Id;
				   }

				   if (PackageProvider.Instance.Settings.FacebookSettings.FacebookWrapper != null && PackageProvider.Instance.Settings.FacebookSettings.FacebookWrapper.CurrentAccessToken != null)
				   {
					   //Check the expiration time and if it is within one day of expiration ask the user to relogin to FB.
					   if (checkExpirationTime(PackageProvider.Instance.Settings.FacebookSettings.FacebookWrapper.CurrentAccessToken))
					   {
						   Debug.Log("expiration time ");
						   refreshFBToken();
					   }
					   else
					   {
						   UnityPrefs.SetString(SocialManager.fbToken, PackageProvider.Instance.Settings.FacebookSettings.FacebookWrapper.CurrentAccessToken.TokenString);
						   UnityPrefs.Save();
						   logSplunk("fb-token-notrefreshed", PackageProvider.Instance.Settings.FacebookSettings.FacebookWrapper.CurrentAccessToken.TokenString);
						   Debug.LogFormat("Logged Into Auth token {0}", PackageProvider.Instance.Settings.FacebookSettings.FacebookWrapper.CurrentAccessToken.TokenString);
					   }
				   }
				   else
				   {
					   Debug.Log("Facebook wrapper is null. Not setting the access token.");
				   }

#if UNITY_WEBGL
				// For webgl we have to relogin because of the bug where currentaccesstoken in the FB unity SDK is
				// cached in the same browser session. If you don't login again then it will login with the previous account.

				if (UnityPrefs.HasKey(SocialManager.fbToken) && UnityPrefs.HasKey(SocialManager.fbWebglToken) && !Data.webPlatform.IsDotCom)
				{
					string fbToken = UnityPrefs.GetString(SocialManager.fbToken);
					string fbWebglToken = UnityPrefs.GetString(SocialManager.fbWebglToken);
					logSplunk("fb-token-webgl", fbWebglToken);
					Debug.LogFormat("FB token {0} FB webgl token {1}", fbToken, fbWebglToken);
					//if (!fbToken.Equals(fbWebglToken) && !UnityPrefs.HasKey(SocialManager.fbWebglKey))
					if (!UnityPrefs.HasKey(SocialManager.fbWebglKey))
					{
						UnityPrefs.SetInt(SocialManager.fbWebglKey, 1);
						UnityPrefs.Save();
						continueLogin = false;

						Debug.Log("Authmanager REtrying  login");
						logSplunk("fb-webgl-relogin", fbToken);
						
						processResult(PackageProvider.Instance.Authentication.Flow.Login(AuthenticationMethod.Facebook, true));
				
					}
					else
					{
						logSplunk("fb-webgl", "continue-login");
						Debug.Log("AuthManager Continue with login");
						continueLogin = true;
					}
				}
#endif
			   }
			   else
			   {
				   Debug.Log("AuthManager fb not logged in");
			   }

			   if (SlotsPlayer.IsAppleLoggedIn)
				{
					ZisData.AppleName = accountDetails.UserAccount.Name;
					if (accountDetails.UserAccount.Email != null)
					{
						ZisData.AppleEmail = accountDetails.UserAccount.Email.Id;
					}
					PreferencesBase preferences = SlotsPlayer.getPreferences();
					if (preferences.HasKey(Prefs.HAS_APPLE_CONNECTED_SUCCESSFULLY))
					{
						ZisAccountCreatedDialog.showDialog();
						preferences.DeleteKey(Prefs.HAS_APPLE_CONNECTED_SUCCESSFULLY);
					}
				}
				else
				{
					Debug.Log("AuthManager Apple not logged in");
				}

				logSplunk("account_details", accountDetails.ToString());

				if (UnityPrefs.GetInt(SocialManager.anonmigrate) == 1)
				{
					UnityPrefs.SetInt(SocialManager.anonmigrate, 2);
					UnityPrefs.Save();
					logSplunk("account_details_migrate_token", accountDetails.ZisToken.Token);
					processResult(PackageProvider.Instance.Authentication.Flow.Login(accountDetails));
					Debug.LogFormat("Logged Into after migration: {0}", accountDetails);
				}
				else
				{
					if (loginComplete && continueLogin)
					{
					   logSplunk("account_details_token", accountDetails.ZisToken.Token);
					   Bugsnag.LeaveBreadcrumb("AuthManager - connectTaskCompleted()");
					   // Only call this if the login is considered complete. Otherwise,
					   // keep the player in a holding pattern.
					   RoutineRunner.instance.StartCoroutine(onConnection(accountDetails));
						 IdleWatch.enable();
					}
				}
			}
		});
	}

	/**** If FB token expires ******/

	private bool checkExpirationTime(FacebookAccessToken token)
	{
		DateTime now = DateTime.Now;
		DateTime refreshDate = now.AddDays(1);
		Debug.LogFormat("Expiration time {0} refresh data {1} ", token.ExpirationTime.ToString(), refreshDate.ToString());
		if (DateTime.Compare(refreshDate, token.ExpirationTime) >= 0)
		{
			logSplunk("fb-expiration-time", token.ExpirationTime.ToString());
			return true;
		}
		logSplunk("fb-expiration-time-norefresh", token.ExpirationTime.ToString());
		return false;
	}


	private void refreshFBToken()
	{
		if (!FB.IsInitialized)
		{
			Debug.Log("FB not initialized");
			// Initialize the Facebook SDK
			FB.Init(InitCallback);

		}
		else
		{
			Debug.Log("FB Initialized");
			var aToken = Facebook.Unity.AccessToken.CurrentAccessToken;
			PreferencesBase UnityPrefs = SlotsPlayer.getPreferences();
			UnityPrefs.SetString(SocialManager.fbToken, aToken.TokenString);
			UnityPrefs.Save();
			logSplunk("fb-token-refreshed", aToken.TokenString);
			// Print current access token's User ID
			Debug.LogFormat("Token is {0}", aToken.TokenString);
		}
	}

	private void InitCallback()
	{
		if (FB.IsInitialized)
		{
			logSplunk("fb-init-callback", "success");
			// Signal an app activation App Event
			FB.ActivateApp();
			FB.LogInWithReadPermissions(new string[] { "public_profile", "email", "user_friends" }, AuthCallback);
		}
		else
		{
			logSplunk("fb-init-callback", "failed");
			Debug.Log("Failed to Initialize the Facebook SDK");
		}
	}

	private void AuthCallback(ILoginResult result)
	{
		if (FB.IsLoggedIn)
		{
			// AccessToken class will have session details
			var aToken = Facebook.Unity.AccessToken.CurrentAccessToken;
			PreferencesBase UnityPrefs = SlotsPlayer.getPreferences();
			UnityPrefs.SetString(SocialManager.fbToken, aToken.TokenString);
			// Print current access token's User ID
			Debug.LogFormat("User id {0} token string {1}", aToken.UserId, aToken.TokenString);
			logSplunk("fbToken-refreshed", aToken.TokenString);
			UnityPrefs.Save();
		}
		else
		{
			logSplunk("fb-login-callback", "cancelled");
			Debug.Log("User cancelled login");
		}
	}

/******************/

	private IEnumerator onConnection(AccountDetails accountDetails)
	{
		// this drops the huge task stack that appears otherwise
		yield return null;

		ServiceClientBase serviceClient = PackageProvider.Instance.ServicesCommon.Client;
		ServiceSession session = serviceClient.Session;

		if (session != null)
		{
			if (session.HasToken)
			{
				SetSessions(session, serviceClient);
			}
			else
			{
				Server.connectionCriticalFailure("ZA", "Null zauth session.");
			}
		}

		isInitializing = false;

	}

	// Method to login with install credentials for anonymous accounts
	private void anonLoginInstallCredentials(bool anonLoginInstallCredForced = false)
	{
		JSON installCreds = new JSON(Zynga.Slots.ZyngaConstantsGame.install_credentials);
		string id = installCreds.getString("id", "");
		string secret = installCreds.getString("secret", "");

		if (!id.IsNullOrWhiteSpace() && !secret.IsNullOrWhiteSpace())
		{
			Debug.LogFormat("id {0} secret {1}", id, secret);
			InstallCredentials installCredentials = new InstallCredentials(long.Parse(id), secret);
			if (anonLoginInstallCredForced)
			{
				logSplunk("recover-with-installcreds-forced", installCreds.ToString());
			}
			else
            {
				logSplunk("recover-with-installcreds", installCreds.ToString());
			}
			processResult(PackageProvider.Instance.Authentication.Flow.RecoverWithCredentials(installCredentials));
		}
		else
		{
			Debug.Log("Install creds are empty. Bailing on logging in anonymously");
		}
	}


	// This method checks to see if the anon zid that is returned from basic data is the same as is got back from auth ZDK
	// if it is not then migrate the one that is got from the server else the one that is stored on the device else just create a new anon
	// zid
	private bool checkZidAndMigrate()
	{
		PreferencesBase UnityPrefs = SlotsPlayer.getPreferences();
		int migrationcounter = UnityPrefs.GetInt(SocialManager.anonmigrateCounter, 0);
		//Zid that is returned from basic game data provided ad id was passed to the server and a zid exists
		string serverZid = ZyngaConstantsGame.auth_zid;
		string clientZid = "";

		logAnonAuth(serverZid, clientZid, "anon-login-data");
		if (SlotsPlayer.getPreferences().GetInt(SocialManager.anonmigrate) != 2 || (PackageProvider.Instance.Authentication.AccountStore.GetLastUsedAccount() != null &&
			PackageProvider.Instance.Authentication.AccountStore.GetLastUsedAccount().IsAnonymous &&
			Zynga.Slots.ZyngaConstantsGame.install_credentials.IsNullOrWhiteSpace()) && migrationcounter < Glb.migrationCounter)
		{
			//Only migrate if the last used account is null
			AccountDetails accountDetails = PackageProvider.Instance.Authentication.AccountStore.GetLastUsedAccount();
			if (accountDetails == null)
			{
				if (!serverZid.IsNullOrWhiteSpace() && !ZyngaConstantsGame.auth_user_hash.IsNullOrWhiteSpace() && !ZyngaConstantsGame.auth_user_id.IsNullOrWhiteSpace())
				{
					string password = Common.Base64Decode(ZyngaConstantsGame.auth_user_hash);
					var snuid = new Snuid(ZyngaConstantsGame.auth_user_id);
					var snid = Zynga.Core.Util.Snid.Anonymous;
					var zid = new Zid(serverZid);

					migrateAnonZids(serverZid, clientZid, zid, snuid, snid, password, migrationcounter);
					return true;
				}
				else if (serverZid.IsNullOrWhiteSpace()) // If the server doesn't have a zid then use the zid that is stored on the client and migrate that to ZIS and is anonymous login
				{
					if (UnityPrefs.HasKey(AnonSessionKey))
					{
						var str = UnityPrefs.GetString(AnonSessionKey);
						Debug.LogFormat("AuthManager RetrieveStoredSession - getting Pref-saved session: {0}", str);
						var dict = Json.Deserialize(str) as Dictionary<string, object>;
						var clientStoredPassword = dict.GetString("userkey", null);
						var clientStoredSnuid = new Snuid(dict.GetString("userId", null));
						var clientStoredZid = new Zid(dict.GetString("zid", null));
						var clientStoredSnid = (Snid)dict.GetLong("snid", 0);

						if (clientStoredZid != null && clientStoredPassword != null && clientStoredSnuid != null)
						{
							string message = "zid: " + clientStoredZid.ToString() + " pw: " + clientStoredPassword + " snuid: " + clientStoredSnuid.ToString() + " snid: " + clientStoredSnid.ToString();
							logSplunk("client-stored-session", message);
							migrateAnonZids(serverZid, clientStoredZid.ToString(), clientStoredZid, clientStoredSnuid, clientStoredSnid, clientStoredPassword, migrationcounter);
							return true;
						}
						else
						{
							logSplunk("client-stored-session", "client stored zid, password or snuid is null");
							return false;
						}

					}
					else
					{
						logSplunk("client-stored-session", "null");
						return false;

					}
				}
				else
				{
					logAnonAuth(serverZid, clientZid, "anon-has-null");
					return false;

				}
			}
			else
			{
				string message = "";

				if (SlotsPlayer.IsEmailLoggedIn)
				{
					message = "email-user";
				}
				if (SlotsPlayer.isFacebookUser)
				{
					message = "fb-user";
				}
				if (SlotsPlayer.IsAppleLoggedIn)
				{
					message = "siwa-user";
				}
				if (SlotsPlayer.isAnonymous)
				{
					message = "anon-user-already-migrated";
				}
				logAnonAuth(serverZid, clientZid, message);
				return false;
			}
		}
		return false;
	}

	// Method that does the migration call
	private void migrateAnonZids(string serverZid, string clientZid, Zid zid, Snuid snuid, Snid snid, string password, int migrationcounter, ServiceSession session = null, ServiceClientBase serviceClient = null)
	{
		Packages.Auth = new AuthPackage(PackageProvider.Instance.ServicesCommon);
		Packages.Auth.Auth.IssueToken(zid, snuid, password).CallbackOrForwardErrors(issueTokenTask =>
		{
			if (!issueTokenTask.Result.IsSuccessful) // If not successful login then just regular login.
			{
				Debug.LogWarningFormat("Could not issue anonymous token {0}", issueTokenTask.Result.ErrorInfo);
				logAnonAuth(serverZid, clientZid, "anon-token-notsuccessful", issueTokenTask.Result.ErrorInfo.ToString());
				processResult(PackageProvider.Instance.Authentication.Flow.Login());
			}
			else
			{
				PreferencesBase UnityPrefs = SlotsPlayer.getPreferences();
				UnityPrefs.SetInt(SocialManager.anonmigrate, 1);
				logAnonAuth(serverZid, clientZid, "anon-token-successful", issueTokenTask.Result.Token.ToString());
				LoginCredentials loginCredentials = new LoginCredentials(AuthenticationMethod.LegacyAnon, snuid.ToString(), issueTokenTask.Result.Token);
				logAnonAuth(serverZid, clientZid, "anon-token-logincredentials", loginCredentials.ToString());
				logAccountStore();
				logAnonAuth(serverZid, clientZid, "migration-counter", migrationcounter.ToString());
				migrationcounter = migrationcounter + 1;
				UnityPrefs.SetInt(SocialManager.anonmigrateCounter, migrationcounter);
				UnityPrefs.Save();
				ServiceSession newsession = new ServiceSession(password, zid, snid, snuid);
				newsession.SetToken(issueTokenTask.Result.Token, issueTokenTask.Result.Expires);
				PackageProvider.Instance.ServicesCommon.Client.SetSession(newsession);
				processMigrationResult(loginCredentials, 0, newsession, PackageProvider.Instance.Authentication.MigrationFlow.Migrate(loginCredentials));	
			}

		});
	}

	//Method that check the result of the migration and then retries if the result is false
	private async void processMigrationResult(LoginCredentials loginCreds, int migrationcounter, ServiceSession session, Task<Result<AccountDetails, LoginErrorInfo>> migrateTask)
	{
		await migrateTask.Callback(async task =>
		{
			if (!task.Result.IsSuccessful)
			{
				Dictionary<string, string> extraFields = new Dictionary<string, string>();
				extraFields.Add("error", task.Result.ErrorInfo.Message);
				extraFields.Add("migration-retry", migrationcounter.ToString());
				SplunkEventManager.createSplunkEvent("AuthManager Migration Connection Failure", "migration-connection-failed", extraFields);
				Debug.LogErrorFormat("AuthManager: failed to connect to channel: {0} ", task.Result.ErrorInfo.Message);
				int delay = Glb.zisMigrateRetryMs + (migrationcounter * Glb.zisMigrateRetryMsMultiplier);
				if (migrationcounter < Glb.zisMigrateRetry)
				{
					await Task.Delay(delay);
					processMigrationResult(loginCreds, migrationcounter+1, session, PackageProvider.Instance.Authentication.MigrationFlow.Migrate(loginCreds));

				}
			}
            else
            {
				AccountDetails accountDetails = task.Result.SuccessValue;
				PreferencesBase UnityPrefs = SlotsPlayer.getPreferences();
				logSplunk("account_details_migrate_token", accountDetails.ZisToken.Token);
				UnityPrefs.SetInt(SocialManager.anonmigrate, 2);
				UnityPrefs.Save();
				processResult(PackageProvider.Instance.Authentication.Flow.Login(accountDetails));
				
			}
		});
	}

	// Method for logging channel is being logged into
	private void logSplunk(string name, string value)
	{
		if (Glb.logAnonAuth)
		{
			Dictionary<string, string> extraFields = new Dictionary<string, string>();
			extraFields.Add("value", value);
			SplunkEventManager.createSplunkEvent("AuthManager login", name, extraFields);
		}
	}

	// Method for logging anon auth
	private void logAnonAuth(string serverZid, string clientZid, string name, string error = "")
	{
		if (Glb.logAnonAuth)
		{
			Dictionary<string, string> extraFields = new Dictionary<string, string>();
			extraFields.Add("serverzid", serverZid);
			extraFields.Add("clientzid", clientZid);
			extraFields.Add("password", Common.Base64Decode(ZyngaConstantsGame.auth_user_hash));
			extraFields.Add("uniqueIdentifier", Zynga.Slots.ZyngaConstantsGame.deviceAdvertisingID);
			extraFields.Add("error", error);
			SplunkEventManager.createSplunkEvent("AuthManager anon login", name, extraFields);
		}
	}

	//Sets all the sessions for the game
	private void SetSessions(ServiceSession session, ServiceClientBase serviceClient)
	{
		ZdkManager.Instance.Zsession = session;
		Debug.Log("loginAnonymously = ZDK session is set!!");
		logAccountStore();
		logSession(session);
		InitCompleted();
	}

	// Logging the start session separately
	private void logSession(ServiceSession session)
	{
		if (Glb.logAnonAuth)
		{
			Dictionary<string, string> extraFields = new Dictionary<string, string>();
			extraFields.Add("session-token", session.Token);
			extraFields.Add("session-zid", session.Zid.ToString());
			extraFields.Add("session-snid", session.Snid.ToString());
			extraFields.Add("session-snuid", session.Snuid.ToString());
			SplunkEventManager.createSplunkEvent("AuthManager session", "session-details", extraFields);
		}
	}

	//Logging account store details
	private void logAccountStore()
    {
		if (Glb.logAccountStore)
		{
			ReadOnlyCollection<AccountDetails> accounts = PackageProvider.Instance.Authentication.AccountStore.GetAccounts();
			int count = 0;
			if (accounts != null && accounts.Count > 0)
			{
				foreach (AccountDetails account in accounts)
				{
					Dictionary<string, string> extraFields = new Dictionary<string, string>();
					extraFields.Add("token", account.ZisToken.Token);
					if (account.GameAccount != null)
					{
						extraFields.Add("playerid-zid", account.GameAccount.PlayerId.ToZid().ToString());
						extraFields.Add("playerid", account.GameAccount.PlayerId.ToString());
					}
					if (account.UserAccount != null)
                    {
						extraFields.Add("useraccount-zid", account.UserAccount.Id.ToString());
					}
					extraFields.Add("install-cred", account.InstallCredentials.ToString());
					extraFields.Add("zis-token-issue", account.ZisToken.IssuedAt.ToString());
					extraFields.Add("zis-token-expire", account.ZisToken.ExpiresAt.ToString());
					if (SlotsPlayer.isAnonymous)
					{
						extraFields.Add("anonymous", "yes");
					}
					if (SlotsPlayer.isFacebookUser)
					{
						extraFields.Add("facebook", "yes");
					}
					if (SlotsPlayer.IsAppleLoggedIn)
					{
						extraFields.Add("siwa", "yes");
					}
					if (SlotsPlayer.IsEmailLoggedIn)
					{
						extraFields.Add("email", "yes");
					}
					string name = "account-store-" + count.ToString();
					SplunkEventManager.createSplunkEvent("AuthManager accountstore", name, extraFields);
					count++;
				}
			}
			else
            {
				Debug.Log("Authmanager Accountstore is null");
            }
		}
	}

	private void InitCompleted()
	{
		//First time we have a session to send track calls
		StatsManager.Instance.LogStartUpStep("Init");

		initMgr.InitializationComplete(this);
	}

#region ISVDependencyInitializer implementation
	/// The AuthManager is dependent on GameSession
	public Type[] GetDependencies()
	{
		return new Type[] { typeof(ZdkManager) };
	}

	/// Initializes the AuthManager
	public void Initialize(InitializationManager mgr)
	{
		if (ZdkManager.Instance == null)
		{
			throw new System.Exception("Cannot initialize AuthManager because ZdkManager is not initialized. Dependency error.");
		}

		ZdkManager.Instance.Zdk = Packages.Initialize();
		initMgr = mgr;
		Authenticate();
	}

	// short description of this dependency for debugging purposes
	public string description()
	{
		return "AuthManager";
	}

#endregion


}
#pragma warning restore 0618, 0168, 0414
