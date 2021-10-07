using UnityEngine;
using System.Collections.Generic;
using Com.Scheduler;
using Zynga.Zdk;

public class GDPRDialog : DialogBase
{

	private class GDPRRequestData
	{
		public GDPRRequestData(DialogBase.AnswerDelegate callback, System.Action onDataComplete, SchedulerPriority.PriorityType p)
		{
			priorityType = p;
			dataCompleteCallback = onDataComplete;
			dialogCallback = callback;
			ComplianceUrlAction.GetGDPRUrl(onGetUrl);
		}

		private void onGetUrl(string zid, string pin, string url)
		{
			Scheduler.addDialog("gdpr",
				Dict.create(
					D.OPTION, GDPRDialog.DISPLAY_REQUEST_INFO,
					D.URL, url,
					D.PLAYER, zid,
					D.DATA, pin,
					D.CALLBACK, dialogCallback
				), 
				priorityType
			);

			if (null != dataCompleteCallback)
			{
				dataCompleteCallback();
			}
		}

		
		private SchedulerPriority.PriorityType priorityType;
		private DialogBase.AnswerDelegate dialogCallback;
		private System.Action dataCompleteCallback;
	}

	public const string DISPLAY_USER_DELETE = "delete";
	public const string DISPLAY_USER_SUSPEND = "suspend";
	public const string DISPLAY_REQUEST_INFO = "request";
	public const string DISPLAY_COPPA_DELETE = "coppa_delete";

	private const string DELETE_TITLE_KEY = "account_blocked";
	private const string DELETE_BUTTON_KEY = "check_status";
	private const string GDPR_DELETE_KEY = "gdpr_delete";
	private const string GDPR_DELETE_STATUS_KEY = "gdpr_delete_status";
	private const string SUSPEND_TITLE_KEY = "account_suspend";
	private const string SUSPEND_BUTTON_KEY = "visit_customer_service";
	private const string GDPR_SUSPEND_KEY = "gdpr_suspend";
	private const string GDPR_SUSPEND_STATUS_KEY = "gdpr_suspend_status";
	private const string COPPA_DELETE_KEY = "coppa_delete";


	public const string ZID_LOCALIZE_KEY = "gdpr_zid";
    public const string PIN_LOCALIZE_KEY = "gdpr_pin";
	

	public GDPRBlockedPanel accountBlocked;
	public GDPRInfoPanel dataRequest;

	public List<ButtonHandler> closeButtons;

	private bool quitOnClose = false;

	public enum DisplayMode
	{
		DELETE,
		SUSPEND,
		REQUEST,
		COPPA_DELETE
	}

	private DisplayMode _mode;
	public DisplayMode mode
	{
		private set
		{
			_mode = value;
			bool displayBlockPanel = false;
			bool displayAccountPanel = false;
			switch(_mode)
			{
				case DisplayMode.COPPA_DELETE:
				case DisplayMode.DELETE:
					displayBlockPanel = true;
					break;

				case DisplayMode.SUSPEND:
					displayBlockPanel = true;
					break;

				case DisplayMode.REQUEST:
				if (null != dataRequest)
					{
						string zid = dialogArgs.getWithDefault(D.PLAYER, "") as string;
						string pin = dialogArgs.getWithDefault(D.DATA, "") as string;
						string url = dialogArgs.getWithDefault(D.URL, "") as string;
						
						dataRequest.SetZID(zid);
						dataRequest.SetPin(pin);
						dataRequest.SetUrl(url);
					}
					displayAccountPanel = true;
					break;
			}

			if (null != accountBlocked && null != accountBlocked.gameObject)
			{
				SafeSet.gameObjectActive(accountBlocked.gameObject, displayBlockPanel);
			}

			if (null != dataRequest && null != dataRequest.gameObject)
			{
				SafeSet.gameObjectActive(dataRequest.gameObject, displayAccountPanel);
			}
					
		}
		get
		{
			return _mode;
		}
	}
	
	/// Initialization
	public override void init()
	{
		string dialogMode = dialogArgs.getWithDefault(D.OPTION, DISPLAY_USER_SUSPEND) as string;
		string description = "";
		string instructions = "";
		string title = "";
		string button = "";
		bool showUserInfo = false;
		ClickHandler.onClickDelegate onUrlClickedDelegate = urlButtonClicked;

		switch (dialogMode)
		{
			case DISPLAY_USER_DELETE:
				button = DELETE_BUTTON_KEY;
				title = DELETE_TITLE_KEY;
				description = GDPR_DELETE_KEY;
				instructions = GDPR_DELETE_STATUS_KEY;
				showUserInfo = false;
				mode = DisplayMode.DELETE;
				break;

			case DISPLAY_USER_SUSPEND:
				button = SUSPEND_BUTTON_KEY;
				title = SUSPEND_TITLE_KEY;
				description = GDPR_SUSPEND_KEY;
				instructions = GDPR_SUSPEND_STATUS_KEY;
				showUserInfo = true;
				mode = DisplayMode.SUSPEND;
				break;
			
			case DISPLAY_COPPA_DELETE:
				title = DELETE_TITLE_KEY;
				description = COPPA_DELETE_KEY;
				showUserInfo = false;
				onUrlClickedDelegate = null;
				quitOnClose = true;
				mode = DisplayMode.COPPA_DELETE;
				break;

			default:
				mode = DisplayMode.REQUEST;
				break;
		}


		if (null != dataRequest)
		{
			dataRequest.init();
		}
		

		if (null != accountBlocked)
		{
			accountBlocked.init(title, description, instructions, button, showUserInfo, onUrlClickedDelegate);
		}

		//register the close button;
		for(int i=0; i<closeButtons.Count; ++i)
		{
			if (null != closeButtons[i].gameObject && closeButtons[i].gameObject.activeSelf)
			{
				closeButtons[i].registerEventDelegate(closeButtonClicked);
			}	
		}
	}

	public void OnDestroy()
	{
		for(int i=0; i<closeButtons.Count; ++i)
		{
			closeButtons[i].unregisterEventDelegate(closeButtonClicked);
		}
	}

	void Update()
	{
		AndroidUtil.checkBackButton(closeButtonClicked);
	}

	public void closeButtonClicked(Dict args)
	{
		Dialog.close();
	}

	public void urlButtonClicked(Dict args)
	{
		ComplianceUrlAction.GetGDPRUrl(onGetUrlCallback);
	}

	private void onGetUrlCallback(string zid, string pin, string url)
	{
		Common.openUrlWebGLCompatible(url);
	}

	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup.
		if (quitOnClose)
		{
			Common.QuitApp();
		}
	}

	public static void showUserSuspendDialog(DialogBase.AnswerDelegate callback, SchedulerPriority.PriorityType priorityType = SchedulerPriority.PriorityType.LOW)
	{
		Scheduler.addDialog("gdpr",
			Dict.create(
				D.OPTION, GDPRDialog.DISPLAY_USER_SUSPEND,
				D.CALLBACK, callback
			), 
			priorityType
		);
	}

	public static void showUserDeleteDialog(DialogBase.AnswerDelegate callback, SchedulerPriority.PriorityType priorityType = SchedulerPriority.PriorityType.LOW)
	{
		Scheduler.addDialog("gdpr",
			Dict.create(
				D.OPTION, GDPRDialog.DISPLAY_USER_DELETE,
				D.CALLBACK, callback
			), 
			priorityType
		);
	}

	public static void showCOPPADeleteDialog(DialogBase.AnswerDelegate callback)
	{
		Scheduler.addDialog("gdpr",
			Dict.create(
				D.OPTION, GDPRDialog.DISPLAY_COPPA_DELETE,
				D.CALLBACK, callback
			), 
			SchedulerPriority.PriorityType.IMMEDIATE
		);
	}

	public static void showRequestInfoDialog(System.Action onDialogReady, SchedulerPriority.PriorityType priorityType = SchedulerPriority.PriorityType.LOW)
	{
		
		new GDPRRequestData(null, onDialogReady, priorityType);
		
	}

	
}
