using Com.Scheduler;
using UnityEngine;

public class NetworkProfileReportDialog : DialogBase
{
	public Animator animator;
	public ButtonHandler[] categoryButtons;
	public ButtonHandler[] fieldButtons;
	public ImageButtonHandler sendButton;
	public ImageButtonHandler backButton;
	public ImageButtonHandler closeButton;
	public ImageButtonHandler nextButton;

	public GameObject fieldPage;
	public GameObject categoryPage;

	public UIAnchor categorySelectMarker;
	public UIAnchor fieldSelectMarker;

	private NetworkProfileAction.ReportField field;
	private NetworkProfileAction.ReportCategory category;
	private int fieldInt = -1;
	private int categoryInt = -1;

	private string networkId = "-1";
	private SocialMember member ;

	private const string SELECTED_SPRITE = "GoldFrame";
	private const string UNSELECTED_SPRITE = "SilverFrame";

	private const string CATEGORY_TO_FIELD = "categoryToFieldPage";
	private const string CATEGORY_EXIT = "categoryExit";
	private const string FIELD_EXIT = "fieldExit";
	private const string INTRO = "intro";

	public override void init()
	{
		Audio.play("minimenuopen0");
		animator.Play(INTRO);
		member = dialogArgs.getWithDefault(D.PLAYER, null) as SocialMember;
		networkId = member.networkID;
		for (int i = 0; i < categoryButtons.Length; i++)
		{
			categoryButtons[i].registerEventDelegate(categoryButtonCallback, Dict.create(D.KEY, i));
		}

		for (int i = 0; i < fieldButtons.Length; i++)
		{
			fieldButtons[i].registerEventDelegate(fieldButtonCallback, Dict.create(D.KEY, i));
		}

		sendButton.registerEventDelegate(sendClicked);
		backButton.registerEventDelegate(backClicked);
		closeButton.registerEventDelegate(closeClicked);
		nextButton.registerEventDelegate(nextButtonClicked);
		loadCategoryPage();
	}

	public void Update()
	{
		AndroidUtil.checkBackButton(closeClicked);
	}
	
	public override void close()
	{
		if (fieldPage.activeSelf)
		{
			animator.Play(FIELD_EXIT);
		}
		else if (categoryPage.activeSelf)
		{
			animator.Play(CATEGORY_EXIT);
		}
		// Do cleanup here.
		Audio.play("XoutEscape");
		if (GameState.isMainLobby)
		{
			MainLobby.playLobbyMusic();
		}
		else if (SlotBaseGame.instance != null)
		{
			SlotBaseGame.instance.playBgMusic();
		}
	}


	private void loadFieldPage()
	{
		fieldPage.SetActive(true);
		categoryPage.SetActive(false);
		for (int i = 0; i < fieldButtons.Length; i++)
		{
			fieldButtons[i].sprite.spriteName = (fieldInt == i) ? SELECTED_SPRITE : UNSELECTED_SPRITE;
		}

		if (fieldInt < 0)
		{
			fieldSelectMarker.gameObject.SetActive(false);
			sendButton.gameObject.SetActive(false);
		}
		else
		{
			fieldSelectMarker.widgetContainer = fieldButtons[fieldInt].sprite;
			fieldSelectMarker.gameObject.SetActive(true);
			sendButton.gameObject.SetActive(true);
		}
	}

	private void loadCategoryPage()
	{
		fieldPage.SetActive(false);
		categoryPage.SetActive(true);
		for (int i = 0; i < categoryButtons.Length; i++)
		{
			categoryButtons[i].sprite.spriteName = (categoryInt == i) ? SELECTED_SPRITE : UNSELECTED_SPRITE;
			// Turn it off and on again to force sprite update.
			categoryButtons[i].SetActive(false);
			categoryButtons[i].SetActive(true);
		}

		if (categoryInt < 0)
		{
			categorySelectMarker.gameObject.SetActive(false);
			nextButton.gameObject.SetActive(false);
		}
		else
		{
			categorySelectMarker.widgetContainer = categoryButtons[categoryInt].sprite;
			categorySelectMarker.gameObject.SetActive(true);
			nextButton.gameObject.SetActive(true);
		}

		animator.Play(INTRO);
	}
	
	private void categoryButtonCallback(Dict args = null)
	{
		categorySelectMarker.gameObject.SetActive(true);
		int key = (int)args.getWithDefault(D.KEY, -1);
		category = intToCategory(key);
		for (int i = 0; i < categoryButtons.Length; i++)
		{
			categoryButtons[i].sprite.spriteName = (key == i) ? SELECTED_SPRITE : UNSELECTED_SPRITE;
		}
		categorySelectMarker.widgetContainer = categoryButtons[key].sprite;
		categorySelectMarker.reposition();
		nextButton.gameObject.SetActive(true);
	}

	private void fieldButtonCallback(Dict args = null)
	{
		fieldSelectMarker.gameObject.SetActive(true);
		int key = (int)args.getWithDefault(D.KEY, -1);
		field = intToField(key);
		for (int i = 0; i < fieldButtons.Length; i++)
		{
			fieldButtons[i].sprite.spriteName = (key == i) ? SELECTED_SPRITE : UNSELECTED_SPRITE;
		}
		fieldSelectMarker.widgetContainer = fieldButtons[key].sprite;
		fieldSelectMarker.reposition();
		sendButton.gameObject.SetActive(true);
	}

    private void nextButtonClicked(Dict args = null)
	{
		animator.Play(CATEGORY_TO_FIELD);
		fieldSelectMarker.gameObject.SetActive(false); // Turn off the checkmark when we move to the next page.
		categorySelectMarker.gameObject.SetActive(false);
		loadFieldPage();
	}

	private void sendClicked(Dict args = null)
	{
		NetworkProfileAction.reportProfile(networkId, "", field, category, reportCallbackFromServer);
		Dialog.close();
		NetworkProfileReportSentDialog.showDialog(SchedulerPriority.PriorityType.IMMEDIATE);
	}

	private void backClicked(Dict args = null)
	{
		if (categoryPage.activeSelf)
		{
			Dialog.close();
			NetworkProfileDialog.showDialog(member);
		}
		else
		{
			loadCategoryPage();
		}
	}

	private void closeClicked(Dict args = null)
	{
		Dialog.close();
	}

	private static void reportCallbackFromServer(JSON data)
	{
		Debug.LogErrorFormat("NetworkProfileReporter.cs -- reportCallback -- reporting ");
	}


	private NetworkProfileAction.ReportCategory intToCategory(int val)
	{
		switch (val)
		{
			case 0:
				return NetworkProfileAction.ReportCategory.SEXUALLY_EXPLICIT;
			case 1:
				return NetworkProfileAction.ReportCategory.HATE_SPEECH;
			case 2:
				return NetworkProfileAction.ReportCategory.IMPERSONATION;				
			case 3:
				return NetworkProfileAction.ReportCategory.ILLEGAL;
			case 4:
				return NetworkProfileAction.ReportCategory.SPAM;
			case 5:
				return NetworkProfileAction.ReportCategory.NUDITY;
			case 6:
				return NetworkProfileAction.ReportCategory.OTHER;
			default:
				return NetworkProfileAction.ReportCategory.OTHER;
		}
	}

	private NetworkProfileAction.ReportField intToField(int val)
	{
		switch (val)
		{
		case 0:
			return NetworkProfileAction.ReportField.NAME;
		case 1:
			return NetworkProfileAction.ReportField.STATUS;
		case 2:
			return NetworkProfileAction.ReportField.LOCATION;
		case 3:
			return NetworkProfileAction.ReportField.PHOTO;
		default:
			return NetworkProfileAction.ReportField.NAME;
		}
	}

	public static void showDialog(SocialMember member, SchedulerPriority.PriorityType priority = SchedulerPriority.PriorityType.LOW)
	{
		if (member != null)
		{
			Scheduler.addDialog("network_profile_report", Dict.create(D.PLAYER, member, D.PRIORITY, priority), priority);
		}
		else
		{
			Debug.LogErrorFormat("NetworkProfileReportDialog.cs -- showDialog -- unable to show report dialog for a null social member.");
		}

	}
}
 