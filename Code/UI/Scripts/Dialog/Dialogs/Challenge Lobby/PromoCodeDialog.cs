using UnityEngine;
using System.Collections;
using Com.Scheduler;
using TMPro;

/*
	Class for handling promo code input for sin city lobby/challenge lobbies
*/
public class PromoCodeDialog : DialogBase
{
	// =============================
	// PRIVATE
	// =============================	
	private enum CodeState
	{
		ACTIVE,
		INVALID,
		VALID
	}

	private CodeState codeState; // state of the promo code validation
	private string action; // action to do when the play button is clicked
	private static OnCodeAccepted callback;
	
	// =============================
	// PUBLIC
	// =============================
	public GameObject getCodeButton;
	public UIInput promoCodeInput;
	public TextMeshPro titleLabel;
	public TextMeshPro jackpotLabel;
	public TextMeshPro errorFeedbackLabel;
	public UIImageButton submitButton;
	public string campaignExperiment;
	public UISprite validMark;
	public UISprite invalidMark;
	public UISprite inputBackground;
	public GameObject codeInputSection;
	public GameObject playButton;

	public delegate void OnCodeAccepted();

	// =============================
	// CONST
	// =============================
	private const float CLOSE_DELAY = 2f;
	private const string FANPAGE_URL = "https://www.facebook.com/HitItRichSlots/";
	private const int BG_WIDTH = 1400;
	private const int BG_FULL_WIDTH = 1730;

	private static Color defaultInputColor;
	private static Vector3 codeInputPosition = new Vector3(0, -405, 0);
	private static Vector3 codeInputActiveKeyboardPosition = new Vector3(0, 675, 0);
	
	public override void init()
	{
		action = dialogArgs[D.OPTION] as string;
		
		// And they cannot have any errors.
		errorFeedbackLabel.text = Localize.text("have_a_promo_code");

		defaultInputColor = inputBackground.color;

		codeState = CodeState.ACTIVE;

		if (!string.IsNullOrEmpty(campaignExperiment))
		{
			ChallengeLobbyCampaign campaign = CampaignDirector.find(campaignExperiment) as ChallengeLobbyCampaign;
			
			if (campaign != null && jackpotLabel != null)
			{
				jackpotLabel.text = CreditsEconomy.convertCredits(campaign.currentJackpot);
			}
		}

		StatsManager.Instance.LogCount
		(
			"dialog"
			, kingdom: campaignExperiment
			, phylum: "promo_code"
			, klass: ""
			, family: ""
			, genus: "view"
		);
	}

	public override void close()
	{
		// Clean up before close here. Called by Dialog.cs do not call directly.
	}
	
	void Update()
	{
		AndroidUtil.checkBackButton(closeClicked);
	}

	/// NGUI search input box callback.
	public virtual void OnInputChanged()
	{
		if (checkInput())
		{
			showSubmitButton();

			if (isCodeValid)
			{
				onCodeValidated();
			}
		}
		else
		{
			hideSubmitButton();
		}
	}

	public void onPlayClicked()
	{
		DoSomething.now(action);
		Dialog.close();
		StatsManager.Instance.LogCount
		(
			"dialog"
			, kingdom: campaignExperiment
			, phylum: "promo_code"
			, klass: promoCodeInput.text.ToLower()
			, family: ""
			, genus: "click"
		);
	}

	/// NGUI OnSelect callback
	public virtual void OnShowKeyboard()
	{		
		if (codeState == CodeState.INVALID)
		{
			promoCodeInput.text = "";
			hideSubmitButton();
			checkInput();
		}

		moveInputPosition();
	}

	private void moveInputPosition()
	{
#if !UNITY_EDITOR && !UNITY_WEBGL
		codeInputSection.transform.localPosition = codeInputActiveKeyboardPosition;
#endif
	}

	private void resetInputPosition()
	{
#if !UNITY_EDITOR && !UNITY_WEBGL
		codeInputSection.transform.localPosition = codeInputPosition;
#endif
	}

	public virtual void OnHideKeyboard()
	{
		if (checkInput())
		{
			validateCode();
		}

		resetInputPosition();
		promoCodeInput.enabled = !checkInput() || !isCodeValid;
	}

	public void onGetCode()
	{
		Application.OpenURL(FANPAGE_URL);
	}
	
	protected bool checkInput()
	{
		string input = promoCodeInput.text.ToLower();
		if (string.IsNullOrEmpty(input))
		{
			// First we check if they have entered any confirmation input yet. If not, then we don't show an error.
			errorFeedbackLabel.text = Localize.text("have_a_promo_code");
			invalidMark.gameObject.SetActive(false);
			inputBackground.color = defaultInputColor;
			codeState = CodeState.ACTIVE;
			return false;
		}
		else if (codeState == CodeState.INVALID)
		{
			return false;
		}
		return true;
	}

	private void showSubmitButton()
	{
		Vector3 scale = inputBackground.transform.localScale;
		inputBackground.transform.localScale = new Vector3(BG_FULL_WIDTH, scale.y, scale.z);
		submitButton.gameObject.SetActive(true);
	}

	private void hideSubmitButton()
	{
		Vector3 scale = inputBackground.transform.localScale;
		inputBackground.transform.localScale = new Vector3(BG_WIDTH, scale.y, scale.z);
		submitButton.gameObject.SetActive(false);
	}

	// Callback for the close button.
	private void closeClicked()
	{
		Dialog.close();
		StatsManager.Instance.LogCount
		(
			"dialog"
			, kingdom: campaignExperiment
			, phylum: "promo_code"
			, klass: ""
			, family: "close"
			, genus: "click"
		);
	}

	private void validateCode()
	{
		if (isCodeValid)
		{
			onCodeValidated();
		}
		else
		{
			errorFeedbackLabel.text = Localize.text("incorrect_promo_code");
			invalidMark.gameObject.SetActive(true);
			inputBackground.color = Color.red;
			codeState = CodeState.INVALID;
		}
	}

	private void onCodeValidated()
	{
		PromoCodeAction.sendCodeValidated(promoCodeInput.text.ToLower(), campaignExperiment);
		closeSequence();
		codeState = CodeState.VALID;
	}

	private void submitClicked()
	{
		validateCode();
	}

	private void closeSequence()
	{
		validMark.gameObject.SetActive(true);
		errorFeedbackLabel.text = Localize.text("correct_promo_code");
		submitButton.gameObject.SetActive(false);
		inputBackground.color = Color.green;
		promoCodeInput.enabled = false;

		playButton.SetActive(true);
		getCodeButton.SetActive(false);

		if (callback != null)
		{
			callback();
		}
	}

	private bool isCodeValid
	{
		get
		{
			string[] codes = Data.liveData.getString("PROMO_CODES", "").Split(',');
			for (int i = 0; i < codes.Length; ++i)
			{
				if (codes[i] == promoCodeInput.text.ToLower())
				{
					return true;
				}
			}
			return false;
		}
	}

	public static void showDialog(string campaignName, SchedulerPriority.PriorityType priorityType = SchedulerPriority.PriorityType.LOW, OnCodeAccepted onCodeAccepted = null)
	{
		callback = onCodeAccepted;
		string doAction = campaignName.Replace("challenge_", "") + "_lobby";
		var args = Dict.create(D.PRIORITY, priorityType,  D.OPTION, doAction);
		Scheduler.addDialog("promo_code", args);
	}
}

