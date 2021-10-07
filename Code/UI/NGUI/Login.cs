using UnityEngine;
using System.Collections;
using Zynga.Zdk;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;
using Zynga.Zdk.Services.Common;
using Zynga.Core.Util;
using Zynga.Core.Tasks;

/**
Controls display and functionality of the login screen.
*/

public class Login : TICoroutineMonoBehaviour 
{
	public ButtonHandler facebookButton;
	public ButtonHandler anonymousButton;
	public ButtonHandler termsOfSerivceHandler;
	public ButtonHandler privacyPolicyHandler;
	public ButtonHandler appleButton;

	public TextMeshPro coinsLabel;
	public TextMeshPro skipLabel;

	public TextMeshPro header;

	public TextMeshPro login_benefit_1;
	public TextMeshPro login_benefit_2;
	public TextMeshPro login_benefit_3;

	public static Login instance = null;

	public UIStretch stretch;

#if UNITY_WSA_10_0 && NETFX_CORE
	public static bool cancelled = false;
#endif

	private const string DEFAULT_HIR_LEGAL_TOS_TEXT = "By clicking one of the buttons below, you agree to <u><#ffde6d>Zynga's Terms of Service</color></u>";
	private const string DEFAULT_HIR_LEGAL_PRIVACY_TEXT = "and acknowledge that <u><#ffde6d>Zynga's Privacy Policy</color></u> applies.";
	
	/// Use init() instead of Awake() to initialize, since this object is inactive by default, and Awake isn't called until it becomes active.
	public void init()
	{
		instance = this;
		
		if (!Glb.appStartIncrimented)
		{
			Glb.incrementAppStartCount();
			Bugsnag.LeaveBreadcrumb(string.Format("App started '{0}' times.", Glb.appStartCount));
		}

		StatsManager.CheckCrashReporter();

		facebookButton.registerEventDelegate(clickFacebookLogin);
		anonymousButton.registerEventDelegate(clickAnonLogin);
		termsOfSerivceHandler.registerEventDelegate(clickTermsOfService);
		privacyPolicyHandler.registerEventDelegate(clickPrivacyPolicy);
		if (SlotsPlayer.getPreferences().GetInt(DebugPrefs.STARTED_FROM_COMMAND_LINE, 0) == 1)
		{
			// If we started from command line, then we are probably going to get stuck here.
			// Click the anonymous button manually now to get through to the lobby so that GameLoader.finishLoading can
			// get called and start ZAP.
			Debug.LogFormat("ZAPLOG -- Login.cs -- clicking anonymous login!");
			clickAnonLogin();
		}

		if (MobileUIUtil.isUltraWide())
		{
			stretch.enabled = true;
		}
	}
	
	void Update()
	{
		TouchInput.update();
	}

	public void show()
	{

		// Setting the text here even though its static becuase of localization loading.
		facebookButton.text = Localize.text("connect_to_facebook");

		// MCC -- Seperating out the text and button handlers for the privacy policy and TOS links.
		termsOfSerivceHandler.text = Localize.textOr("login_screen_legal_tos", DEFAULT_HIR_LEGAL_TOS_TEXT);
		privacyPolicyHandler.text = Localize.textOr("login_screen_legal_privacy", DEFAULT_HIR_LEGAL_PRIVACY_TEXT);

		
		SafeSet.labelText(skipLabel, Localize.text("skip_for_now"));
		SafeSet.labelText(header, Localize.text("facebook_login_additional_benefits"));
		
		SafeSet.labelText(login_benefit_1, Localize.text("facebook_login_benefit_1"));
		SafeSet.labelText(login_benefit_2, Localize.text("facebook_login_benefit_2"));
		SafeSet.labelText(login_benefit_3, Localize.text("facebook_login_benefit_3"));
		
		SafeSet.labelText(coinsLabel, CreditsEconomy.convertCredits(SlotsPlayer.instance.mergeBonus));

		StatsManager.Instance.LogCount("dialog", "connect_to_facebook", "", "", "view", "view");
		
		// Reset the message visibilities for next time.
		PlayerPrefsCache.SetString(Prefs.SEEN_ANON_DIALOG, "true");
				
		//New stat tracking: When the user logs out and we have the new zid, track that network change
		if (PlayerPrefsCache.GetString(Prefs.LOGOUT_FROM_SN, "") != "")
		{
			Debug.LogWarning ("Should be about to track mapping backwards from a social network to Anonymous");
			string[] logoutDetails = PlayerPrefsCache.GetString(Prefs.LOGOUT_FROM_SN).Split(':'); // Since we set this internally, we can guarantee two elements here
			ServiceSession session1 = ZdkManager.Instance.Zsession;
			long previousSessionSnid = long.Parse(logoutDetails[0]);
			long previousSessionZid = long.Parse(logoutDetails[1]);
			string androidDeviceId = "";
			#if UNITY_ANDROID
			androidDeviceId = Zynga.Slots.ZyngaConstantsGame.androidDeviceID;
			#endif

			StatsManager.Instance.LogAssociate("snuid_device_mapping", Zynga.Slots.ZyngaConstantsGame.deviceAdvertisingID, ZyngaConstants.GameSkuVersion, SystemInfo.operatingSystem, StatsManager.DeviceModel, "", "", previousSessionSnid, previousSessionZid, androidDeviceId);
			PlayerPrefsCache.SetString(Prefs.LOGOUT_FROM_SN, "");
		}
		
		PlayerPrefsCache.SetInt(Prefs.FB_DIALOG_VIEW_COUNT,PlayerPrefsCache.GetInt(Prefs.FB_DIALOG_VIEW_COUNT,0) + 1 );
		//PlayerPrefsCache.SetInt(Prefs.USER_SELECTED_LOGOUT, 0);
		PlayerPrefsCache.Save();

		gameObject.SetActive(true);
		clickAnonLogin();
	}
	
	// Hide the login screen.
	public void hide()
	{
		gameObject.SetActive(false);
	}

	public void clickFacebookLogin(Dict args = null)
	{
		Loading.show(Loading.LoadingTransactionTarget.LOBBY_LOGIN);
		PlayerPrefsCache.SetInt(Prefs.UPGRADE_FROM_SOCIAL_SCREEN, 1);
		PlayerPrefsCache.Save();
		SocialManager.Instance.FacebookLogin(FBLoginFinished);

		StatsManager.Instance.LogCount("dialog", "game_load", "fb_auth", "click");
		StatsManager.Instance.LogCount("dialog", "connect_to_facebook", "", "", "Connect", "click");
	}

	public void clickTermsOfService(Dict args = null)
	{
		DoSomething.now("url_terms");
	}

	public void clickPrivacyPolicy(Dict args = null)
	{
		DoSomething.now("url_privacy");
	}

	private void FBLoginFinished(bool success)
    {
		Debug.Log("Login::FBLoginFinished - Status is: " + success.ToString());
		
		/*SocialManager.Instance.setPreferences(SocialManager.SocialLoginPreference.Facebook);
		if (success) 
		{
			if (Packages.SocialAuthFacebook.Channel.IsEnabled)
			{
				ZdkManager.Instance.Zsession = Packages.SocialAuthFacebook.Channel.Session;
			}
			if (ZdkManager.Instance.Zsession.Snid != Zynga.Core.Util.Snid.Facebook) // NRE OCCURS HERE
			{
				Debug.LogError("FBLoginFinished: session SNid does not match Facebook");
				loginFailedPromptRestart();
			}
			else
			{
				StatsManager.Instance.LogCount("dialog", "game_load", "fb_auth", "success");
				SocialManager.Instance.statsCallbackAfterLogin(ZdkManager.Instance.Zsession.Snid, ZdkManager.Instance.Zsession);
				SocialManager.Instance.passCompleteToManager();
				hide();
			}
		}
#if UNITY_WSA_10_0 && NETFX_CORE
		else if(SocialManager.Instance.cancelled)
		{
			PreferencesBase UnityPrefs = SlotsPlayer.getPreferences();
			UnityPrefs.SetInt(SocialManager.kUpgradeZid, 0);
			StatsManager.Instance.LogCount("dialog", "game_load", "fb_auth", "cancelled");
			//SocialManager.Instance.statsCallbackAfterLogin(ZdkManager.Instance.Zsession.Snid, ZdkManager.Instance.Zsession);
			//SocialManager.Instance.passCompleteToManager();
			SocialManager.Instance.setPreferences(SocialManager.SocialLoginPreference.FirstTime);
			SocialManager.Instance.cancelled = false;
			clickAnonLogin();
		}
#endif
		else 
		{
			Debug.LogError("FBLoginFinished: Facebook login failed..... what to do?");
			StatsManager.Instance.LogCount("dialog", "game_load", "fb_auth", "error");

			// Ask the user what to do now:
			SocialManager.Instance.SocialLoginFailed(SocialManager.Instance.FacebookLogin, FBLoginFinished);
		}*/
	}

	public static void loginFailedPromptRestart()
	{
		Debug.LogError("Login::loginFailedPromptRestart - Prompting the user to log in again.");
		// Prompt the user and then compeletely reset the game:
		Loading.hide(Loading.LoadingTransactionResult.FAIL);
		GenericDialog.showDialog(
			Dict.create(
				D.TITLE, Localize.textOr("error", "Error"),
				D.MESSAGE, Localize.textOr("error_auth_token_expired", "Authentication token expired.\n\nThe game will now quit. Please log in again."),
				D.REASON, "login-auth-token-expired",
				D.CALLBACK, new DialogBase.AnswerDelegate( (args) => { doLogoutQuit(); })
			),
			SchedulerPriority.PriorityType.BLOCKING
		);
	}

	private static void doLogoutQuit()
	{
		PreferencesBase UnityPrefs = SlotsPlayer.getPreferences();
		if (SocialManager.Instance != null)
		{
			// Attempt a proper logout of MiSocialManager
			SocialManager.Instance.Logout(true);
		}
		else
		{
			// Fallback
			UnityPrefs.SetInt(SocialManager.kLoginPreference, (int)SocialManager.SocialLoginPreference.FirstTime);
			UnityPrefs.Save();
		}
		
		// Regardless at this point, we know that two PlayerPref keys got to go.
		Debug.LogWarning("Attempting to clear all  PlayerPrefsCache");
		UnityPrefs.DeleteAll();
		UnityPrefs.Save();

		PlayerPrefsCache.DeleteAll();
		PlayerPrefsCache.Save();

		// When we get here, we know that some serious stuff messed up.
		// We *must* force the application to quit, so preferences don't get re-created from memory.	
		Common.QuitApp();	
		
#if UNITY_EDITOR
		Debug.LogError("The application would have terminated here, please stop execution in the editor.");
		Debug.Break();
#endif
	}
	
	public void clickAnonLogin(Dict args = null)
	{
		Loading.show(Loading.LoadingTransactionTarget.LOBBY_LOGIN);
		StatsManager.Instance.LogCount("dialog", "game_load", "anon", "success");
		StatsManager.Instance.LogCount("dialog", "connect_to_facebook", "", "", "Skip For Now", "click");
		SocialManager.Instance.passCompleteToManager();
		hide();
		//StartCoroutine(loginAnonymously());
	}
	
	IEnumerator loginAnonymously()
	{
		PreferencesBase UnityPrefs = SlotsPlayer.getPreferences();
		yield return new WaitForSeconds(.1f);
		if (UnityPrefs.HasKey(SocialManager.kLoginPreference))
		{
			if (UnityPrefs.GetInt(SocialManager.kLoginPreference) != (int)SocialManager.SocialLoginPreference.Apple)
			{

				Debug.Log("AppleLogin: Setting kLoginPreference to Anonymous");
				UnityPrefs.SetInt(SocialManager.kLoginPreference, (int)SocialManager.SocialLoginPreference.Anonymous);
				UnityPrefs.Save();
			}
		}
		SocialManager.Instance.passCompleteToManager();
		hide();
	}
}
