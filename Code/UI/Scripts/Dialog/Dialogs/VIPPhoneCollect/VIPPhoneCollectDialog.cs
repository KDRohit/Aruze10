using UnityEngine;
using System.Collections;
using TMPro;
using TMProExtensions;
using System.Text.RegularExpressions;
using Com.Scheduler;

public class VIPPhoneCollectDialog : DialogBase {

	public static VIPPhoneCollectDialog instance = null;
	[SerializeField] private ImageButtonHandler signUpButton;
	[SerializeField] private ImageButtonHandler closeButton;
	[SerializeField] private ImageButtonHandler submitButton;
	[SerializeField] private ImageButtonHandler backButton;
	[SerializeField] private ClickHandler programTermsButton;
	[SerializeField] private ClickHandler checkTextingRatesButton;
	[SerializeField] private ClickHandler termsOfServiceButton;
	[SerializeField] private ClickHandler privacyButton;
	

	public GameObject basicPanel;
	public GameObject inputfieldPanel;
	public GameObject bottomPanel;
	public GameObject backgroundPanel;
	public GameObject signupPanel;
	public GameObject shroudPanel;

	public GameObject checkedMark;
	public UIImageButton validationUIImageButton;
	public TextMeshPro validateButtonText;

	public Renderer backgroundRenderer;
	public TextMeshPro rewardAmountMessage;
	public UIInput firstName;
	public UISprite firstNameFrame;
	public UIInput lastName;
	public UISprite lastNameFrame;
	public UIInput phoneNumber;
	public UISprite phoneNumberFrame;

	public TextMeshPro inputFieldMessage;

	private const string BACKGROUND_PATH = "vip_phone_collect/Vip_Phone_Collect_BG.png";
	private const string VIP_PHONE_REWARD = "reward_for_vip_phone_num";
	private const string VIP_PHONE_ERROR = "update_vip_data_failed";
	private const string REG_PATTERN_FOR_NAME = @"^[A-Z][a-zA-Z]*$";
	private const string REG_PATTERN_FOR_PHONENUMBER = @"([0-9]{10})";  
	private const string REG_PATTERN_FOR_PREFIXCODE = @"([0-9]{3})";  
	private const string REG_PATTERN_FOR_LASTCODE = @"([0-9]{4})";
	private const string REG_PATTERN_FOR_NUMBER = @"[^\d]";
	private const string CORRECT_SPRITE_NAME = "text_field_box_green Stretchy";
	private const string WRONG_SPRITE_NAME = "text_field_box_red Stretchy";

	private static int rewardCoin = 0;
	private string vipFirstName = "";
	private string vipLastName = "";
	private string vipNumber = "";
	private int vipSMSAgreement = 0;

	private UIInput activedInputObject = null;

	void Awake()
	{
		instance = this;	
	}

	public static bool isActive
	{
		get
		{
			return ExperimentWrapper.VIPPhoneDialogSurfacing.isInExperiment;
		}
	}

	public override void init()
	{
		downloadedTextureToRenderer(backgroundRenderer, 0);
		if (ExperimentWrapper.VIPPhoneDialogSurfacing.isInExperiment)
		{
			rewardCoin = ExperimentWrapper.VIPPhoneDialogSurfacing.coinRewardAmount;
		}
		MOTDFramework.markMotdSeen(dialogArgs);
		setInitMode();
		Audio.play("minimenuopen0");
		
		// Setup buttons
		closeButton.registerEventDelegate(closeClicked);
		signUpButton.registerEventDelegate(signUpClicked);
		submitButton.registerEventDelegate(submitClicked);
		backButton.registerEventDelegate(backClicked);
		programTermsButton.registerEventDelegate(programTermsClicked);
		checkTextingRatesButton.registerEventDelegate(checkSMS);
		termsOfServiceButton.registerEventDelegate(termsOfServiceClicked);
		privacyButton.registerEventDelegate(privacyClicked);
	}

	// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup.
	}	

	private void setInitMode()
	{
		bottomPanel.SetActive(true);
		rewardAmountMessage.text = CreditsEconomy.convertCredits(rewardCoin);
		StatsManager.Instance.LogCount("dialog", "vip_phone_number", "intro", "", "", "view"); 
	}

	void Update()
	{
		if (inputfieldPanel.activeSelf)
		{
			AndroidUtil.checkBackButton(backClicked);
		}
		else
		{
			AndroidUtil.checkBackButton(closeClicked);
		}
	}

	private void OnInputChanged(UIInput selectedObject)
	{
		activedInputObject = selectedObject;
		if (activedInputObject == firstName)
		{
			checkFirstName();
		}
		if (activedInputObject == lastName)
		{
			checkLastName();
		}
		if (activedInputObject == phoneNumber)
		{
			checkValidatePhoneNumber();
			phoneNumberFormat();
		}
		checkRemainingError();
		activeDoneButton();
	}

	private void checkRemainingError()
	{
		if (firstNameFrame.spriteName == WRONG_SPRITE_NAME ||
		    lastNameFrame.spriteName == WRONG_SPRITE_NAME ||
		    phoneNumberFrame.spriteName == WRONG_SPRITE_NAME)
		{
			inputFieldMessage.gameObject.SetActive(true);
		}
		else
		{
			inputFieldMessage.gameObject.SetActive(false);
		}

		if (firstNameFrame.spriteName == WRONG_SPRITE_NAME)
		{
			checkFirstName();
		} 
		else if (lastNameFrame.spriteName == WRONG_SPRITE_NAME)
		{
			checkLastName();
		} 
		else if (phoneNumberFrame.spriteName == WRONG_SPRITE_NAME)
		{
			checkValidatePhoneNumber();
			phoneNumberFormat();
		}
	}

	private void phoneNumberFormat()
	{
		Regex regexObj = new Regex(REG_PATTERN_FOR_NUMBER);
		string phoneNum = regexObj.Replace(phoneNumber.text, "");
		if (phoneNum.Length < 1)
		{
			phoneNumber.text = "";
		}
		else if (phoneNum.Length >= 1 && phoneNum.Length < 4)
		{
			phoneNumber.text = phoneNum;
		}
		else if (phoneNum.Length >= 4 && phoneNum.Length < 7)
		{
			phoneNumber.text = phoneNum.Substring(0, 3) + "-" + phoneNum.Substring(3);
		}
		else if (phoneNum.Length >= 7)
		{
			phoneNumber.text = phoneNum.Substring(0, 3) + "-" + phoneNum.Substring(3, 3) + "-" + phoneNum.Substring(6);
		}
	}

	public void OnSubmit(string inputData)
	{
		if (activedInputObject == null)
		{
			return;
		}

		if (activedInputObject.gameObject.name == firstName.gameObject.name)
		{
			checkFirstName();
		}

		if (activedInputObject.gameObject.name == lastName.gameObject.name)
		{
			checkLastName();
		}

		if (activedInputObject.gameObject.name == phoneNumber.gameObject.name)
		{
			checkValidatePhoneNumber();
		}
		activeDoneButton();
	}
	
	private void switchDoneButtonSatatus(bool shouldShow)
	{
		if (shouldShow)
		{
			signupPanel.SetActive(true);
			validationUIImageButton.gameObject.SetActive(false);
			firstName.GetComponent<BoxCollider>().enabled = false;
			lastName.GetComponent<BoxCollider>().enabled = false;
			phoneNumber.GetComponent<BoxCollider>().enabled = false;
		}
		else
		{
			signupPanel.SetActive(false);
			validationUIImageButton.gameObject.SetActive(true);
			firstName.GetComponent<BoxCollider>().enabled = true;
			lastName.GetComponent<BoxCollider>().enabled = true;
			phoneNumber.GetComponent<BoxCollider>().enabled = true;
		}
		activeDoneButton();
	}

	// For Validate
	private void validateInputData()
	{
		StatsManager.Instance.LogCount("dialog", "vip_phone_number", "submission_form", "", "done", "click"); 
		if (checkValidate())
		{
			switchDoneButtonSatatus(true);
		}
	}

	private bool checkValidate()
	{
		if (!checkFirstName() || !checkLastName() || !checkValidatePhoneNumber())
		{
			activeDoneButton();
			return false;
		}
		activeDoneButton();
		vipFirstName = firstName.text;
		vipLastName = lastName.text;
		return true;
	}

	private void activeDoneButton()
	{
		if (firstNameFrame.spriteName == CORRECT_SPRITE_NAME &&
		    lastNameFrame.spriteName == CORRECT_SPRITE_NAME &&
		    phoneNumberFrame.spriteName == CORRECT_SPRITE_NAME)
		{
			validationUIImageButton.isEnabled = true;
			validateButtonText.color = new Color32(195, 255, 91, 255);
		}
		else
		{
			validationUIImageButton.isEnabled = false;
			validateButtonText.color = new Color32(192, 192, 190, 255);
		}
	}

	private bool checkFirstName()
	{
		if (!Regex.Match(firstName.text, REG_PATTERN_FOR_NAME).Success)
		{
			setIncorrectField(firstNameFrame, "vip_error_msg_name");
			return false;
		}
		else if (firstName.text.Length < 2)
		{
			setIncorrectField(firstNameFrame, "vip_error_msg_phone_not_valid_name");
			return false;
		}
		else
		{
			firstNameFrame.spriteName = CORRECT_SPRITE_NAME;
		}
		return true;
	}

	private bool checkLastName()
	{
		if (!Regex.Match(lastName.text, REG_PATTERN_FOR_NAME).Success)
		{
			setIncorrectField(lastNameFrame, "vip_error_msg_name");
			return false;
		}
		else if (lastName.text.Length < 2)
		{
			setIncorrectField(lastNameFrame, "vip_error_msg_phone_not_valid_name");
			return false;
		}

		else
		{
			lastNameFrame.spriteName = CORRECT_SPRITE_NAME;
		}
		return true;
	}

	private bool checkValidatePhoneNumber()
	{
		Regex regexObj = new Regex(@"[^\d]");
		phoneNumber.text = regexObj.Replace(phoneNumber.text, "");

		if (phoneNumber.text.Length != 10)
		{
			setIncorrectField(phoneNumberFrame, "vip_error_msg_phone_not_ten");
			return false;
		}
		else if (!Regex.Match(phoneNumber.text, REG_PATTERN_FOR_PHONENUMBER).Success)
		{
			setIncorrectField(phoneNumberFrame, "vip_error_msg_phone_not_number");
			return false;
		}

		string areaCode = phoneNumber.text.Substring(0,3);
		string prefixCode = phoneNumber.text.Substring(3, 3);
		string lastCode = phoneNumber.text.Substring(6, 4);


		phoneNumber.text = string.Format("{0}-{1}-{2}", areaCode, prefixCode, lastCode);

		string notValidNumberMessage = "vip_error_msg_phone_not_valid_number";
		
		if (!Regex.Match(prefixCode, REG_PATTERN_FOR_PREFIXCODE).Success)
		{
			setIncorrectField(phoneNumberFrame, notValidNumberMessage);
			return false;
		}
		else if (!Regex.Match(lastCode, REG_PATTERN_FOR_LASTCODE).Success)
		{
			setIncorrectField(phoneNumberFrame, notValidNumberMessage);
			return false;
		}

		// Our limited phone number validation is based on information at:
		// https://en.wikipedia.org/wiki/North_American_Numbering_Plan
		// - Area codes must be 200 or greater.
		// - Area codes must not have three repeating digits.
		// - First three after area code ("Central Office"):
		// 		- First digit must be 2-9.
		// 		- second and third digits are 0-9, but can't both be 1.
		if (Regex.Matches((areaCode + prefixCode + lastCode), areaCode[0].ToString()).Count == 10)
		{
			setIncorrectField(phoneNumberFrame, notValidNumberMessage);
			return false;
		}
		else if (int.Parse(areaCode) < 200 || int.Parse(prefixCode) < 200)
		{
			setIncorrectField(phoneNumberFrame, notValidNumberMessage);
			return false;
		}
		else if (Regex.Matches(areaCode, areaCode[0].ToString()).Count == 3)
		{
			setIncorrectField(phoneNumberFrame, notValidNumberMessage);
			return false;
		}
		else if (prefixCode.Substring(1) == "11")
		{
			setIncorrectField(phoneNumberFrame, notValidNumberMessage);
			return false;
		}
		else
		{
			phoneNumberFrame.spriteName = CORRECT_SPRITE_NAME;
		}

		vipNumber = string.Format("{0}{1}{2}", areaCode, prefixCode, lastCode);
		return true;
	}

	private void setIncorrectField(UISprite targetFrame, string messageKey)
	{
		inputFieldMessage.text = @"<color=""red"">" + Localize.text(messageKey) + "</color>";;
		inputFieldMessage.gameObject.SetActive(true);
		targetFrame.spriteName = WRONG_SPRITE_NAME;
	}

#region BUTTON_DELEGATES
	// Button Delegates

		private void signUpClicked(Dict args = null)
	{
		Audio.play("minimenuopen0");
		StatsManager.Instance.LogCount("dialog", "vip_phone_number", "intro", "", "sign_up", "click"); 
		StatsManager.Instance.LogCount("dialog", "vip_phone_number", "submission_form", "", "", "view"); 
		inputfieldPanel.SetActive(true);
		validationUIImageButton.isEnabled = false;
		validateButtonText.color = new Color32(192, 192, 190, 255);
		iTween.ScaleTo(backgroundPanel, new Vector3(2.5f, 2.5f, 1f), 0.3f);
		basicPanel.SetActive(false);
		switchDoneButtonSatatus(false);
	}

	private void backClicked(Dict args = null)
	{
		Audio.play("minimenuopen0");
		inputfieldPanel.SetActive(false);
		iTween.ScaleTo(backgroundPanel, new Vector3(1f, 1f, 1f), 0.3f);
		basicPanel.SetActive(true);
		setInitMode();
	}

	private void closeClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount("dialog", "vip_phone_number", "intro", "", "close", "click"); 
		Dialog.close();
	}

	private void submitClicked(Dict args = null)
	{
		if (!checkValidate())
		{
			return;
		}

		shroudPanel.SetActive(true);

		StatsManager.Instance.LogCount("dialog", "vip_phone_number", "submission_form", "", "submit", "click"); 
		VIPPhoneCollectAction.vipSubmitInformation(vipNumber, vipFirstName, vipLastName, vipSMSAgreement.ToString());
	}

	private void programTermsClicked(Dict args = null)
	{
		Application.OpenURL(Glb.HELP_LINK_SMS);
	}

	private void termsOfServiceClicked(Dict args = null)
	{
		Application.OpenURL(Glb.HELP_LINK_TERMS);
	}

	private void privacyClicked(Dict args = null)
	{
		Application.OpenURL(Glb.HELP_LINK_PRIVACY);
	}	

	private void checkSMS(Dict args = null)
	{
		checkedMark.SetActive((checkedMark.activeSelf) ? false : true);
		vipSMSAgreement = (checkedMark.activeSelf) ? 1 : 0;
	}	
#endregion
	
#region STATIC_METHODS
	// Static methods
	public static bool showDialog(string motdKey = "")
	{
		Dialog.instance.showDialogAfterDownloadingTextures(
			"vip_phone_collect_dialog",
			BACKGROUND_PATH,
			Dict.create(D.MOTD_KEY, motdKey),
			false,
			SchedulerPriority.PriorityType.IMMEDIATE);
		return true;
	}

	public static void registerEventDelegates()
	{
		Server.registerEventDelegate(VIP_PHONE_REWARD, executeUpdateSuccess, true);
		Server.registerEventDelegate(VIP_PHONE_ERROR, executeUpdateFail, true);
	}
	private static void executeUpdateSuccess(JSON data)
	{
		Server.unregisterEventDelegate(VIP_PHONE_REWARD, executeUpdateSuccess);
		Server.unregisterEventDelegate(VIP_PHONE_ERROR, executeUpdateFail);
		if (Dialog.instance.isShowing && instance != null)
		{
			instance.backClicked();
			instance.shroudPanel.SetActive(false);
			Dialog.close(instance);
		}
		VIPPhoneCollectRewardDialog.showDialog(data);
	}

	private static void executeUpdateFail(JSON data)
	{
		instance.shroudPanel.SetActive(false);
		string errorMsg = data.getString("error_msg", "");
		instance.inputFieldMessage.gameObject.SetActive(true);
		instance.inputFieldMessage.text = @"<color=red>* " + errorMsg + "</color>";
		instance.phoneNumberFrame.spriteName = WRONG_SPRITE_NAME;
		UICamera.selectedObject = instance.phoneNumber.gameObject;
		instance.switchDoneButtonSatatus(false);
	}
#endregion
}
