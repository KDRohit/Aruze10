using UnityEngine;
using Com.Scheduler;
using TMPro;
using Zynga.Zdk.Services.Identity;
using Zynga.Authentication;
using System.Collections.Generic;
using Zynga.Core.Util;
using System.Threading.Tasks;
using Zynga.Core.Tasks;

public class ZisSignInWithEmailDialog : DialogBase , IResetGame
{

	public ClickHandler collectButtonClickHandler;
	public ClickHandler continueButtonClickHandler;
	public ClickHandler newPlayerButtonClickHandler;
	public ClickHandler optInClickHandler;
	public ClickHandler llConfirmHandler;
	public ClickHandler llEditClickHandler;
	public ClickHandler dotcomInitialConfirmClickHandler;
	
	[SerializeField] private TextMeshPro collectButtonLabel;

	public GameObject signInState;
	public GameObject confirmationSentState;
	public GameObject welcomeBackState;
	public GameObject signInStateNewPlayerContent;
	public GameObject signInStateReturnPlayerContent;

	// Sign In State

	public ClickHandler confirmationButtonClickHandler;
	public UIButton confirmationButton;
	public ButtonColorExtended confirmationButtonColor;
	public UISprite signInStateInputLabel0CheckMark;
	public UISprite signInStateInputLabel0CrossMark;
	public UISprite signInStateInputLabel1CheckMark;
	public UISprite signInStateInputLabel1CrossMark;
	public UISprite optInForEmails;
	public UISprite coin;

	public UIInput inputBox0;
	public UIInput inputBox1;
	private UIInput currentlyShowingInput;

	[SerializeField] private TextMeshPro signInStateCoinAmountLabel;
	[SerializeField] private TextMeshPro signInStateSignInLabel;
	[SerializeField] private TextMeshPro signInStateEditSignInLabel;
	[SerializeField] private TextMeshPro signInStateInputLabel0;
	[SerializeField] private TextMeshPro signInStateInputLabel1;
	[SerializeField] private TextMeshPro headerLabel;
	[SerializeField] private TextMeshPro subHeaderLabel;
	[SerializeField] private TextMeshPro signInStateConfirmationButtonLabel;
	[SerializeField] private TextMeshPro confirmationButtonLabel;
	[SerializeField] private TextMeshPro messageLabel;
	
	// Confirmation Sent State

	public GameObject confirmationStateNewPlayerContent;
	public GameObject confirmationStateReturnPlayerContent;
	public ClickHandler resendButtonClickHandler;
	public ClickHandler startOverClickHandler;


	public UIInput verifyCode;

	[SerializeField] private TextMeshPro newPlayerConfirmEmailLabel;
	[SerializeField] private TextMeshPro newPlayerConfirmEmailLabelText;
	[SerializeField] private TextMeshPro confirmCoinLabel;
	[SerializeField] private TextMeshPro returnConfirmEmailLabel;
	[SerializeField] private TextMeshPro disclaimerLabel;
	[SerializeField] private TextMeshPro resendButtonLabel;
	[SerializeField] private TextMeshPro startOverButtonLabel;
	[SerializeField] private ObjectSwapper stateSwapper;
	[SerializeField] private GameObjectSwap gameStatSwapper;
	[SerializeField] private TextMeshPro retryCodeLabel;

	// Welcome Back State

	[SerializeField] private TextMeshPro welcomeBackEmailLabel;

	// confirmation sent dotcom login state
	[SerializeField] private TextMeshPro confirmationDotComEmailLabel;


	public const string SIGN_IN_STATE = "sign_in_state";
	public const string SIGN_IN_EXISTING_STATE = "sign_in_existing";
	public const string LL_MIGRATION_EDIT_STATE = "ll_migration";
	public const string CONFIRMATION_SENT_STATE = "confirmation_sent_state";
	public const string WELCOME_BACK_STATE = "welcome_back_state";


	private string initialInputText = "";
	private string initialConfirmationText = "";

	public static string inputText = "";
	public static bool optIn = false;
	public static bool llWasDisplayed = false;
	public static string confirmationText = "";
	private static ZyngaAccountLoginFlowBase loginFlow = null;
	private static ZyngaAccountAuthCodeFlowBase authCodeFlow = null;
	private static string state = "";
	private static bool securityCheck = false; // Security challenge enabled check during login

	//Setting up the ZIS sgin in with email dialog
	
	private const string SIGN_IN_NEW_STATE = "enter_email_new_player";
	private const string SIGN_IN_EXISTING_PLAYER_STATE = "enter_email_returning_player";
	private const string LL_MIGRATION_STATE = "enter_email_LL_migration";
	private const string EMAIL_ERROR_STATE = "enter_email_error";
	
	public const string CONF_SENT_NEW_PLAYER_STATE = "confirmation_sent_new_player";
	public const string CONF_SENT_DOTCOM_LOGIN_STATE = "confirmation_sent_dotcom_login";
	private const string CONF_SENT_RETURNING_PLAYER_STATE = "confirmation_sent_returning_player";
	private const string CONF_SENT_RETURNING_PLAYER_ERROR_STATE = "confirmation_sent_returning_player_error";
	private const string DIALOG_WELCOME_BACK_STATE = "welcome_back_returning_player";
	

	// is dialog triggered during initial login flow for dotcom.
	// This dialog is triggered during initial login for dotcom only 
	private bool isDotcomInitialLoginFlow => Data.webPlatform.IsDotCom && AuthManager.Instance.isInitializing;

	private void initCommonSignInState()
	{
		optIn = false;
		inputText = "";
		signInStateInputLabel0CheckMark.gameObject.SetActive(false);
		signInStateInputLabel0CrossMark.gameObject.SetActive(false);
		
		if (stateSwapper.getCurrentState() == SIGN_IN_EXISTING_PLAYER_STATE)
		{
			if (isDotcomInitialLoginFlow)
			{
				// initial login flow
				SafeSet.labelText(signInStateEditSignInLabel, "Enter your email, then check your inbox to confirm");	
			}
			else
			{
				SafeSet.labelText(headerLabel, "Edit Email");
				string currentEmail = "";
				if (PackageProvider.Instance.Authentication.Flow.Account.UserAccount.Email != null)
				{
					currentEmail = PackageProvider.Instance.Authentication.Flow.Account.UserAccount.Email.Id;
				}
				else
				{
					currentEmail = "an email";
				}

				SafeSet.labelText(subHeaderLabel, "You are connected with " + currentEmail);
				SafeSet.labelText(signInStateEditSignInLabel, "Enter new email, then check your inbox to confirm");
			}
		}

		if (confirmationButtonLabel != null)
		{
			confirmationButtonLabel.text = "Confirm";
		}
		if (confirmationButtonClickHandler != null)
		{
			confirmationButtonClickHandler.isEnabled = false;
			confirmationButtonColor.isEnabled = false;
			confirmationButtonClickHandler.registerEventDelegate(confirmationButtonClicked);
		}
		loginFlow = dialogArgs.getWithDefault(D.CUSTOM_INPUT, null) as ZyngaAccountLoginFlowBase;
		initialInputText = inputBox0.text.ToLower();
	}

	private void onEmailInputBoxSubmit(string input) {
		// This triggers when enter is hit. Trigger
		// the button click.
		confirmationButtonClicked();
	}

	private void onConfInputBoxSubmit(string input) {
		// This triggers when enter is hit. Trigger
		// the button click.
		enterCodeClicked();
	}

	public override void init()
	{
		state = dialogArgs.getWithDefault(D.STATE, null) as string;
		string email = dialogArgs.getWithDefault(D.EMAIL, null) as string;
		securityCheck = (bool)dialogArgs.getWithDefault(D.ACTIVE, false);

		confirmationButtonClickHandler.gameObject.SetActive(true);

		if (optInClickHandler != null)
		{
			optInClickHandler.registerEventDelegate(onOptInClick);
		}
		if (!SocialManager.emailOptIn)
		{
			optInClickHandler.gameObject.SetActive(false);
			optInForEmails.gameObject.SetActive(false);
		}
		
		switch (state)
		{
			case SIGN_IN_STATE:
				if (isDotcomInitialLoginFlow)
				{
					// for the initial login flow on dotcom
					// we do not want to show coin rewards
					// so we set state and set appropriate text in initCommonSignInState()
					stateSwapper.setState(SIGN_IN_EXISTING_PLAYER_STATE);
					inputBox0.onSubmit = onEmailInputBoxSubmit;
				}
				else
				{
					stateSwapper.setState(SIGN_IN_NEW_STATE);
					SafeSet.labelText(signInStateCoinAmountLabel, CreditsEconomy.convertCredits(SlotsPlayer.instance.mergeBonus));
				}
				initCommonSignInState();
			    break;
			case SIGN_IN_EXISTING_STATE:
		        stateSwapper.setState(SIGN_IN_EXISTING_PLAYER_STATE);
		        initCommonSignInState();
				break;
			case CONFIRMATION_SENT_STATE:
			{
				//TODO add returnig player from dialog 
				newPlayerButtonClickHandler.gameObject.SetActive(false);
				stateSwapper.setState(CONF_SENT_RETURNING_PLAYER_STATE);
				verifyCode.onSubmit = onConfInputBoxSubmit;
				if (collectButtonClickHandler != null)
				{
					collectButtonClickHandler.isEnabled = false;
					ButtonColorExtended buttonColor = collectButtonClickHandler.GetComponent<ButtonColorExtended>();
					if (buttonColor != null)
					{
						buttonColor.isEnabled = false;
					}
					
					collectButtonClickHandler.registerEventDelegate(enterCodeClicked);
				}

				if (resendButtonClickHandler != null)
				{
					resendButtonClickHandler.registerEventDelegate(resendClicked);
				}

				if (startOverClickHandler != null)
				{
					startOverClickHandler.registerEventDelegate(startOverClicked);

				}

				authCodeFlow = dialogArgs.getWithDefault(D.CUSTOM_INPUT, null) as ZyngaAccountAuthCodeFlowBase;
				// If the inputText is empty then get the email from the authcodeflow. This happens when security challenge is turned on.
				if (inputText.IsNullOrWhiteSpace())
                {
					inputText = authCodeFlow.Identifier;
                }
				SafeSet.labelText(headerLabel, "Confirmation Sent!");
				SafeSet.labelText(subHeaderLabel, "Enter the code sent to "+inputText+" to continue.");
				string errorMsg = dialogArgs.getWithDefault(D.MESSAGE, "") as string;
				if (!errorMsg.IsNullOrWhiteSpace())
				{
					retryCodeLabel.gameObject.SetActive(true);
					SafeSet.labelText(retryCodeLabel, errorMsg);
				}
				closeButtonHandler.gameObject.SetActive(!isDotcomInitialLoginFlow);
			}
				break;
			case CONF_SENT_NEW_PLAYER_STATE:
				if (isDotcomInitialLoginFlow)
				{
					stateSwapper.setState(CONF_SENT_DOTCOM_LOGIN_STATE);
					SafeSet.labelText(headerLabel, "Email Sent!");
					if (subHeaderLabel != null)
					{
						subHeaderLabel.gameObject.SetActive(false);
					}

					if (confirmationDotComEmailLabel != null)
					{
						if (PackageProvider.Instance.Authentication.Flow.Account.UserAccount.Email != null)
						{
							SafeSet.labelText(confirmationDotComEmailLabel,
								PackageProvider.Instance.Authentication.Flow.Account.UserAccount.Email.Id);
						}
						else
						{
							confirmationDotComEmailLabel.gameObject.SetActive(false);
						}
					}
					if (dotcomInitialConfirmClickHandler != null)
					{
						dotcomInitialConfirmClickHandler.registerEventDelegate(verifyLinkClicked);
					}
					closeButtonHandler.gameObject.SetActive(false);
				}
				else
				{
					stateSwapper.setState(CONF_SENT_NEW_PLAYER_STATE);
					SafeSet.labelText(headerLabel, "Confirmation Sent!");
					SafeSet.labelText(subHeaderLabel, "Check your email to confirm.");
					confirmationStateNewPlayerContent.SetActive(true);
					confirmationStateReturnPlayerContent.SetActive(false);
					SafeSet.labelText(newPlayerConfirmEmailLabel, email);
					SafeSet.labelText(confirmCoinLabel, CreditsEconomy.convertCredits(SlotsPlayer.instance.mergeBonus));
					resendButtonClickHandler.gameObject.SetActive(false);
					startOverClickHandler.gameObject.SetActive(false);
					collectButtonClickHandler.gameObject.SetActive(false);
					if (newPlayerButtonClickHandler != null)
					{
						newPlayerButtonClickHandler.gameObject.SetActive(true);
						newPlayerButtonClickHandler.registerEventDelegate(verifyLinkClicked);
					}
					closeButtonHandler.gameObject.SetActive(!isDotcomInitialLoginFlow);
				}
				break;
			case WELCOME_BACK_STATE:
			{
				signInState.SetActive(false);
				confirmationSentState.SetActive(false);
				welcomeBackState.SetActive(true);
				stateSwapper.setState(DIALOG_WELCOME_BACK_STATE);
				SafeSet.labelText(headerLabel, "Welcome Back!");
				SafeSet.labelText(subHeaderLabel, "Thanks for signing in and confirming");
				SafeSet.labelText(returnConfirmEmailLabel, email);
				if (continueButtonClickHandler != null)
				{
					continueButtonClickHandler.registerEventDelegate(welcomeContinue);
				}
			}
				break;
			case LL_MIGRATION_EDIT_STATE:
			{
				loginFlow = dialogArgs.getWithDefault(D.CUSTOM_INPUT, null) as ZyngaAccountLoginFlowBase;
				stateSwapper.setState(LL_MIGRATION_STATE);
				if (email != null)
				{
					inputBox0.text = email;
				}
				
				if (confirmationButtonClickHandler != null)
				{
					//Turning of elements not need that are on the prfab
					confirmationButtonClickHandler.gameObject.SetActive(false);
					confirmationButtonClickHandler.enabled = false;
				}

					if (llEditClickHandler != null)
					{
						llEditClickHandler.gameObject.SetActive(true);
						llEditClickHandler.enabled = true;
						if (email != null)
						{
							setConfirmationButtonEnabled(llEditClickHandler);
						}
						else
						{
							llEditClickHandler.isEnabled = false;

						}

						llEditClickHandler.registerEventDelegate(llEditButtonClicked);
					}

				if (llConfirmHandler != null)
				{
					if (email != null)
					{
						setConfirmationButtonEnabled(llConfirmHandler);
					}
					else
					{
						llConfirmHandler.isEnabled = false;

					}

					llConfirmHandler.registerEventDelegate(confirmationButtonClicked);
				}
				int dateBy =  Data.liveData.getInt("ZIS_SAVE_PROGRESS_DATE", 0);
				SafeSet.labelText(confirmationButtonLabel, "Edit");
				string dateString = dateBy != 0 ? CommonText.formatDate(Common.convertFromUnixTimestampSeconds(dateBy)) : "";
				SafeSet.labelText(messageLabel, "Confirm by " + dateString +" to ensure progress is saved!");

				SafeSet.labelText(headerLabel, "Save Your Progress!");
				SafeSet.labelText(subHeaderLabel, "Confirm " + email + " to play on any device!");
				llWasDisplayed = true;

				break;
			}
			default:
				stateSwapper.setState(SIGN_IN_NEW_STATE);
				break;
		}
	}

	private void verifyLinkClicked(Dict args = null)
	{
		Glb.resetGame("verify link clicked");
	}

	private void enterCodeClicked(Dict args = null)
	{
		Debug.Log("Enter code clicked");
		if (optInForEmails.gameObject.activeSelf)
		{
			SocialManager.emailOptIn = true;
		}
		Dialog.close(this);
		SocialManager.Instance.verifyCode(authCodeFlow, verifyCode.text.ToLower());
	}

	private void confirmationButtonClicked(Dict args = null)
	{
		if (optInForEmails.gameObject.activeSelf) 
		{
			SocialManager.emailOptIn = true;
		}
		inputText = inputBox0.text.ToLower();
		Dialog.close(this);
		Debug.Log("Confirmation button clicked");
		SocialManager.Instance.emailLogin(loginFlow, inputText);
	}

	private void llEditButtonClicked (Dict args = null)
	{
		Debug.Log("Edit LL button clicked");
		Dialog.close();
		SocialManager.Instance.CreateAttach(AuthenticationMethod.ZyngaEmailUnverified);
		//SocialManager.Instance.emailChangePressed();
	}

	private void resendClicked(Dict args = null)
	{
		Dialog.close(this);
		SocialManager.Instance.onResendPressed(authCodeFlow);
	}

	private void startOverClicked(Dict args = null)
	{
		Dialog.close(this);
		if(isDotcomInitialLoginFlow)
		{
			AuthManager.Instance.Authenticate();
		}
		else if (securityCheck) // If security challenge check is on then start over will login as new anon user
        {
			processLogin(PackageProvider.Instance.Authentication.Flow.LoginNew());
        }
		else
		{
			SocialManager.Instance.CreateAttach(AuthenticationMethod.ZyngaEmailUnverified);
		}
	}

	private async void processLogin(Task<Result<AccountDetails, LoginErrorInfo>> taskResult)
	{
		await taskResult.Callback(task =>
		{
			if (!task.Result.IsSuccessful)
			{
				logSplunk("show-dialog-login-flow", state, "process-new-login-failed");
			}
			else
			{
				logSplunk("show-dialog-login-flow", state, "process-new-login-success");
				Glb.resetGame("Logging in fresh anon account");
			}
		});
	}

	private void welcomeContinue(Dict args = null)
	{
		Dialog.close();
	}
	
	private void onOptInClick(Dict args = null)
	{
		if (optInForEmails != null)
		{
			optInForEmails.gameObject.SetActive(!optInForEmails.gameObject.activeSelf);
		}
	}

	// Setup the confirmation button 
	private void setConfirmationButtonEnabled(ClickHandler buttonToEnable)
	{
		string input = inputBox0.text.ToLower();
		bool hasEnteredInput = input != initialInputText;
		bool isValidInput = (!string.IsNullOrEmpty(input) && input.Contains('@') && input.Contains('.'));
		ButtonColorExtended buttonColor = buttonToEnable.GetComponent<ButtonColorExtended>();
		if (isValidInput && hasEnteredInput)
		{
			signInStateInputLabel0CheckMark.gameObject.SetActive(true);
			signInStateInputLabel0CrossMark.gameObject.SetActive(false);
			buttonToEnable.enabled = true;
			buttonToEnable.isEnabled = true;

			if (buttonColor != null)
			{
				buttonColor.isEnabled = true;
			}
			signInStateCoinAmountLabel.gameObject.SetActive(true);
			coin.gameObject.SetActive(true);
			if (state == SIGN_IN_EXISTING_STATE)
			{
				showErrorText(signInStateEditSignInLabel, "Enter new email, then check your inbox to confirm");
			}
			else
			{
				showErrorText(signInStateSignInLabel, "Sign in and confirm to get");
			}
			
		}
		else
		{
			buttonToEnable.enabled = false;
			buttonToEnable.isEnabled = false;

			if (buttonColor != null)
			{
				buttonColor.isEnabled = false;
			}
			signInStateInputLabel0CrossMark.gameObject.SetActive(true);
			signInStateInputLabel0CheckMark.gameObject.SetActive(false);
			signInStateCoinAmountLabel.gameObject.SetActive(false);
			coin.gameObject.SetActive(false);
			if (state == SIGN_IN_EXISTING_STATE)
			{
				showErrorText(signInStateEditSignInLabel, Localize.text("invalid_signin_email"), true);
			}
			else
			{
				showErrorText(signInStateSignInLabel, Localize.text("invalid_signin_email"), true);
			}
		}

	}

	/// NGUI search input box callback.
	public virtual void OnInputChanged()
	{

		switch (state)
		{
			case SIGN_IN_STATE:
				setConfirmationButtonEnabled(confirmationButtonClickHandler);
				break;
			case SIGN_IN_EXISTING_STATE:
				setConfirmationButtonEnabled(confirmationButtonClickHandler);
				break;
			case CONFIRMATION_SENT_STATE:
			{   
				uint code = 0;
				ButtonColorExtended buttonColor = collectButtonClickHandler.GetComponent<ButtonColorExtended>();
				if (verifyCode.text.Length == 6 && uint.TryParse(verifyCode.text.ToLower(),out code)) //Codes are always 6 digits long and a number
				{
					collectButtonClickHandler.isEnabled = true;

					if (buttonColor != null)
					{
						buttonColor.isEnabled = true;
					}
				}
				else
				{
					collectButtonClickHandler.isEnabled= false;
					if (buttonColor != null)
					{
						buttonColor.isEnabled = false;
					}
				}
			}
				break;
			case LL_MIGRATION_EDIT_STATE:
				setConfirmationButtonEnabled(llConfirmHandler);
				setConfirmationButtonEnabled(llEditClickHandler);
				break;

		}
	}

	public virtual void OnShowKeyboard(UIInput input)
	{
		currentlyShowingInput = input;

#if !UNITY_EDITOR && !UNITY_WEBGL
		/*transform.localPosition = Vector3.zero;
		float yDistance = editingInputPosition.transform.position.y - input.transform.position.y;
		Vector3 dialogPosition = transform.position;
		dialogPosition.y += yDistance;
		transform.position = dialogPosition;*/
#endif

		// Only check this when the user is leaving the second input as per new UX instructions.
		//emailInputSprite.spriteName = (input == inputBox0) ? GOLD_BORDER_INPUT : GRAY_BORDER_INPUT;
		//emailConfirmationSprite.spriteName = (input == inputBox1) ? GOLD_BORDER_INPUT : GRAY_BORDER_INPUT;
		//setConfirmationButtonEnabled(checkInput(input));
	}

	public virtual void OnHideKeyboard(UIInput input)
	{
#if !UNITY_EDITOR && !UNITY_WEBGL
		if (currentlyShowingInput == input)
		{
			// Reset the dialog to normal position.
			transform.localPosition = Vector3.zero;
		}
#endif

		// Only check this when the user is leaving the second input as per new UX instructions.
		setConfirmationButtonEnabled(confirmationButtonClickHandler);
	}


	// Show error text
	private void showErrorText(TextMeshPro textLabel, string text = "", bool errorText = false)
	{
		
		textLabel.text = text;
		if (errorText)
		{
			textLabel.color = Color.red;
		}
		else
		{
			textLabel.color = Color.white;
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
		switch (state)
		{
			case SIGN_IN_STATE:
			case SIGN_IN_EXISTING_STATE:
				if (confirmationButtonClickHandler != null)
				{
					confirmationButtonClickHandler.unregisterEventDelegate(confirmationButtonClicked);
				}
				break;
			case CONFIRMATION_SENT_STATE:
				if (collectButtonClickHandler != null)
				{
					collectButtonClickHandler.unregisterEventDelegate(enterCodeClicked);
				}

				if (resendButtonClickHandler != null)
				{
					resendButtonClickHandler.unregisterEventDelegate(resendClicked);
				}

				if (startOverClickHandler != null)
				{
					startOverClickHandler.unregisterEventDelegate(startOverClicked);
				}

				break;
			case CONF_SENT_NEW_PLAYER_STATE:
				if (newPlayerButtonClickHandler != null)
				{
					newPlayerButtonClickHandler.unregisterEventDelegate(verifyLinkClicked);
				}
				if (dotcomInitialConfirmClickHandler != null)
				{
					dotcomInitialConfirmClickHandler.unregisterEventDelegate(verifyLinkClicked);
				}
				//Reset the game
				Glb.resetGame("verify link clicked");
				break;
			case WELCOME_BACK_STATE:
				if (continueButtonClickHandler != null)
				{
					continueButtonClickHandler.unregisterEventDelegate(welcomeContinue);
				}
				break;
			case LL_MIGRATION_EDIT_STATE:
				if (llConfirmHandler != null)
				{
					llConfirmHandler.unregisterEventDelegate(confirmationButtonClicked);
				}
				if (llEditClickHandler != null)
				{
					llEditClickHandler.unregisterEventDelegate(llEditButtonClicked);
				}
				break;
		}

		optIn = optInForEmails.gameObject.activeSelf;
		StatsZIS.logZisSignIn("", "close");
		// Do special cleanup. Downloaded textures are automatically destroyed by Dialog class.

		confirmationText = "";
	}

	//Close/back/esc button handler
	public override void onCloseButtonClicked(Dict args = null)
	{
		if ((state == CONFIRMATION_SENT_STATE || state == CONF_SENT_NEW_PLAYER_STATE) &&
		    (isDotcomInitialLoginFlow))
		{
			// dont let them close during initial flow for dotcom
			return;
		}

		if (CONFIRMATION_SENT_STATE == state)
		{
			//authCodeFlow.Cancel();
		}
		Dialog.close(this);
		AuthManager.Instance.Authenticate();
	}

	// Method for logging channel is being logged into
	private static void logSplunk(string name, string key, string value)
	{
		Dictionary<string, string> extraFields = new Dictionary<string, string>();
		extraFields.Add("key", key);
		extraFields.Add("value", value);
		SplunkEventManager.createSplunkEvent("ZisSignInWithEmailDialog", name, extraFields);
	}

	public static void showDialog(string state, ZyngaAccountLoginFlowBase loginFlow = null, string email = null, ZyngaAccountAuthCodeFlowBase authCode = null, string errorMsg = null, bool securitycheck = false, SchedulerPriority.PriorityType schedulerPriority = SchedulerPriority.PriorityType.LOW)
	{
		
		Dict args = null;
		args = Dict.create(D.STATE, state,D.STACK, false);
		if (loginFlow != null)
		{
			args.Add(D.CUSTOM_INPUT, loginFlow);
			logSplunk("show-dialog-login-flow", state, loginFlow.ToString());
		}

		if (email != null)
		{
			args.Add(D.EMAIL, email);
			logSplunk("show-dialog-email", state, email);
		}

		if (authCode != null)
		{
			args.Add(D.CUSTOM_INPUT, authCode);
			logSplunk("show-dialog-authcode", state, authCode.ToString());
		}

		if (securitycheck)
        {
			args.Add(D.ACTIVE, true);
        }

		if(errorMsg != null)
		{
			args.Add(D.MESSAGE, errorMsg);
			logSplunk("show-dialog-error", state, errorMsg);
		}
		Scheduler.addDialog("zis_sign_in_with_email", args, schedulerPriority);
	}

	public static void resetStaticClassData()
	{
		 inputText = "";
		 optIn = false;
		 llWasDisplayed = false;
	}
}
