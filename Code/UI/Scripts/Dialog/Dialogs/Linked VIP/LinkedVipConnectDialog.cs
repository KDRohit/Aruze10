using UnityEngine;
using System.Collections;
using Com.Scheduler;
using TMPro;
/*
  Class for handling the Network dialog where you enter your email address.
*/
public class LinkedVipConnectDialog : DialogBase
{	
	[SerializeField] private UIInput emailInput;
	[SerializeField] private UIInput confirmEmailInput;

	[SerializeField] private ClickHandler helpButton;
	[SerializeField] private ImageButtonHandler signUpButton;
	[SerializeField] private TextMeshPro signUpButtonLabel;
	[SerializeField] private ImageButtonHandler closeButton;

	[SerializeField] private TextMeshPro coinIncentiveLabel;
	[SerializeField] private GameObject coinIncentiveParent;

	[SerializeField] private TextMeshPro errorFeedbackLabel;
	[SerializeField] private GameObject errorFeedbackParent;

	[SerializeField] private UISprite emailInputSprite;
	[SerializeField] private UISprite emailConfirmationSprite;

	[SerializeField] private GameObject editingInputPosition;

	[SerializeField] private Color signUpButtonOnColor;
	[SerializeField] private Color signUpButtonOffColor;

	private string initialInputText = "";
	private string initialConfirmationText = "";
	private bool hasIncentive = false;
	/*
		There is a bug on iOS where the OnShowKeyboard gets called before OnHideKeyboard() if you click from
		one input directly to another input. Thus we need to store the last shown input, and only move the dialog
		if the hiding input matches the last shown input.
	*/
	private UIInput currentlyShowingInput;

	private const string GOLD_BORDER_INPUT = "Email Textbox Active Stretchy";
	private const string GRAY_BORDER_INPUT = "Email Textbox Inactive Stretchy";

	public override void init()
	{
		helpButton.registerEventDelegate(networkHelpClicked);
		signUpButton.registerEventDelegate(sendClicked);
		closeButton.registerEventDelegate(closeClicked);

		helpButton.show(LinkedVipProgram.instance.isHelpShiftActive);
		setSignUpButtonEnabled(false); // Disabled when first opened because the emails are empty.

		hasIncentive = LinkedVipProgram.instance.incentiveCredits > 0;
		if (hasIncentive)
		{
			coinIncentiveLabel.text = CreditsEconomy.convertCredits(LinkedVipProgram.instance.incentiveCredits);
			coinIncentiveParent.SetActive(true);
		}
		else
		{
			coinIncentiveParent.SetActive(false);
		}

		// And they cannot have any errors.
		errorFeedbackParent.SetActive(false);

		// Store the initial values for validation later.
		initialInputText = emailInput.text.ToLower();
		initialConfirmationText = confirmEmailInput.text.ToLower();

		// Turn both these input these both 
		emailInputSprite.spriteName = GRAY_BORDER_INPUT;
		emailConfirmationSprite.spriteName = GRAY_BORDER_INPUT;
		StatsManager.Instance.LogCount ("dialog", "linked_vip", "network_sign_up", "", "sign_up_now", "view");

		Audio.playMusic("FeatureBgLL");
		Audio.switchMusicKey("FeatureBgLL");

		Audio.play("EnterEmailFlourishLL");
	}

	public override void close()
	{
		// Clean up before close here. Called by Dialog.cs do not call directly.
	}

	void Update()
	{
		AndroidUtil.checkBackButton(closeClicked);
	}

	private void setSignUpButtonEnabled(bool isEnabled)
	{
		signUpButton.enabled = isEnabled;
		signUpButtonLabel.color = isEnabled ? signUpButtonOnColor : signUpButtonOffColor;
	}

	/// NGUI search input box callback.
	public virtual void OnInputChanged()
	{
		// Check the input for a value and sanitize it.
		setSignUpButtonEnabled(checkInput(null));
	}

	public virtual void OnShowKeyboard(UIInput input)
	{
		currentlyShowingInput = input;
		
#if !UNITY_EDITOR && !UNITY_WEBGL
		transform.localPosition = Vector3.zero;
		float yDistance = editingInputPosition.transform.position.y - input.transform.position.y;
		Vector3 dialogPosition = transform.position;
		dialogPosition.y += yDistance;
		transform.position = dialogPosition;
#endif

		// Only check this when the user is leaving the second input as per new UX instructions.
		emailInputSprite.spriteName = (input == emailInput) ? GOLD_BORDER_INPUT : GRAY_BORDER_INPUT;
		emailConfirmationSprite.spriteName = (input == confirmEmailInput) ? GOLD_BORDER_INPUT : GRAY_BORDER_INPUT;
		setSignUpButtonEnabled(checkInput(input));
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
		setSignUpButtonEnabled(checkInput(input, true));

		// When we hide the keyboard turn the inputs gold since they are no longer active.
		emailInputSprite.spriteName = GRAY_BORDER_INPUT;
		emailConfirmationSprite.spriteName = GRAY_BORDER_INPUT;
	}

	protected bool checkInput(UIInput uiInput, bool isHiding = false)
	{
		string input = emailInput.text.ToLower();
		string confirmationInput = confirmEmailInput.text.ToLower();

		bool hasEnteredInput = input != initialInputText;
		bool hasEnteredConfirmation = confirmationInput != initialConfirmationText;
		bool hasEnteredText = hasEnteredInput  && hasEnteredConfirmation;
		bool isValidInput = (!string.IsNullOrEmpty(input) && input.Contains('@') && input.Contains('.'));
		bool doInputsMatch = (input == confirmationInput);

		if (!hasEnteredInput)
		{
			// First we check if they have entered any input yet. If not, then we don't show an error.
			showErrorText(false);
			return false;
		}
		else if (!isValidInput)
		{
			// If they have not entered a valid input email, and they are clicking away from the input field
			// Set label to tell the user their email address is invalid.
			// Also adding an OR for if they have edited the confirmation item. This means they have edited the
			// the input field already so we want to tell them if its invalid no matter where they are clicking.
			if (uiInput == emailInput)
			{
				showErrorText(true, Localize.text("invalid_email"));
			}
			return false;
		}
		else if (!doInputsMatch)
		{
			// If they have edited both fields, and the first one is valid, but the second one
			// doesn't match it, then tell them the addresses don't match.
			// Only do this when thye are clicking away from the confirmation input.
			if (uiInput == confirmEmailInput && isHiding)
			{
				showErrorText(true, Localize.text("emails_do_not_match"));
			}
			return false;
		}

		// Lets still hide the error text when they have corrected
		// the error no matter which input they just editted.
		if (doInputsMatch && hasEnteredText && isValidInput)
		{
			showErrorText(false);
			return true;
		}
		else
		{
			showErrorText(false);
			return false;
		}
	}

	private void showErrorText(bool shouldShow, string text = "")
	{
		if (hasIncentive)
		{
			coinIncentiveParent.SetActive(!shouldShow);
		}

		if (!shouldShow)
		{
			errorFeedbackParent.SetActive(false);
			return;
		}

		errorFeedbackLabel.text = text;
		errorFeedbackParent.SetActive(true);
	}

	// Callback for the close button.
	private void closeClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount ("dialog", "linked_vip", "network_sign_up", "", "close", "click");
		Dialog.close();
		Audio.play("QuitFeatureLL");
		RoutineRunner.instance.StartCoroutine(Glb.restoreMusic(1.0f));
	}

	private void sendClicked(Dict args = null)
	{
		LinkedVipProgram.instance.connectWithEmail(emailInput.text.ToLower());
		Dialog.close();
		Audio.play("SubmittedEmailFanfareLL");
		RoutineRunner.instance.StartCoroutine(Glb.restoreMusic());
		StatsManager.Instance.LogCount ("dialog", "linked_vip", "network_sign_up", "", "sign_up_now", "click");
	}

	protected void networkHelpClicked(Dict args = null)
	{
		LinkedVipProgram.instance.openHelpUrl();
	}

	private void connectCallback(JSON response)
	{
	}

	public static void showDialog(SchedulerPriority.PriorityType p = SchedulerPriority.PriorityType.LOW)
	{
		if (ExperimentWrapper.ZisPhase2.isInExperiment)
		{
			SocialManager.Instance.CreateAttach(Zynga.Zdk.Services.Identity.AuthenticationMethod.ZyngaEmailUnverified);
		}
		else
		{
			Scheduler.addDialog("linked_vip_connect", null, p);
		}
	}
}
