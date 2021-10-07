using UnityEngine;
using System.Collections;
using Com.HitItRich.EUE;
using Com.Scheduler;

/*
This dialog requests notification permissions from the user.
*/

public class SoftPromptDialog : DialogBase,  IResetGame
{
	public Renderer imageRenderer;
	[SerializeField] protected ClickHandler okayHandler;
	[SerializeField] protected ClickHandler notNowHandler;
	[SerializeField] protected ClickHandler closeHandler;

	protected const string LIVE_DATA_THEME_KEY = "SOFT_PROMPT_THEME";
	private const string S3_BG_PATH = "misc_dialogs/BonusReminder_{0}.png";
	private const string DEFAULT_BG_PATH = "Features/Soft Prompt Dialog V2/Textures/BonusReminder_Default";
	protected string theme = "Default";
	
	public static bool awaitingPNState { get; private set; }
	
	protected static bool launchedFromFtue;

	protected virtual bool shouldCloseDialogOnAccept
	{
		get
		{
			return true;
		}
	}

	// Initialization
	public override void init()
	{
		okayHandler.registerEventDelegate(clickOk);
		notNowHandler.registerEventDelegate(clickClose);
		closeHandler.registerEventDelegate(clickClose);

		theme = Data.liveData.getString(LIVE_DATA_THEME_KEY, "Default", "Default");
		if (imageRenderer != null)
		{
			downloadedTextureToRenderer(imageRenderer, 0);	
		}

		if (EUEManager.isEnabled)
		{
			StatsManager.Instance.LogCount("dialog", "lobby_ftue", "step", "", "pn", "view");
		}
	}
	
	protected virtual void Update()
	{
		AndroidUtil.checkBackButton(clickClose, "dialog", "pn_soft_prompt", "ftue, coin", "non_incent", "", "back");
	}

	protected virtual void setAwaitingPushNotifState()
	{
		awaitingPNState = true;
	}

	private void clickOk(Dict args = null)
	{	
		logClickYes();
		setAwaitingPushNotifState();
		NotificationManager.PushNotifSoftPromptAccepted();
		if (EUEManager.isEnabled)
		{
			StatsManager.Instance.LogCount("dialog", "lobby_ftue", "step", "", "pn", "click");
			StatsManager.Instance.LogCount("dialog", "lobby_ftue", "pn_dialog", "", "", "yes");
		}

		if (shouldCloseDialogOnAccept)
		{
			Dialog.close();	
		}
	}

	protected virtual void logClickYes()
	{
		StatsManager.Instance.LogCount("dialog", "pn_soft_prompt", launchedFromFtue ? "lobby_ftue" : "hourly_bonus", "non_incent", "yes", "click");
		StatsManager.Instance.LogCount("dialog", "pn_system_dialog", launchedFromFtue? "lobby_ftue" : "hourly_bonus", "non_incent", "", "view");
	}

	protected virtual void logClickNo()
	{
		StatsManager.Instance.LogCount("dialog", "pn_soft_prompt", launchedFromFtue ? "lobby_ftue" : "hourly_bonus", "non_incent", "skip", "click");
	}
	
	public void clickClose(Dict args = null)
	{
		logClickNo();
		if (EUEManager.isEnabled)
		{
			StatsManager.Instance.LogCount("dialog", "lobby_ftue", "step", "", "pn", "click");
			StatsManager.Instance.LogCount("dialog", "lobby_ftue", "pn_dialog", "", "", "later");
		}
		Dialog.close();
	}
	
	/// Called by Dialog.close() - do not call directly.
	public override void close()
	{
		if (EUEManager.shouldDisplayGameIntro)
		{
			RoutineRunner.instance.StartCoroutine(showGameIntroAfterDelay(0.5f)); //wait half second for dialogs to close so we don't block (in case eue dialog is already on stack)
		}
	}

	public static void showDialog(Com.Scheduler.SchedulerPriority.PriorityType priority = SchedulerPriority.PriorityType.HIGH, bool isFromFtue = false)
	{
		string themeName = Data.liveData.getString(LIVE_DATA_THEME_KEY, "Default", "Default");

		launchedFromFtue = isFromFtue;

		//log view
		StatsManager.Instance.LogCount("dialog", "pn_soft_prompt", isFromFtue ? "lobby_ftue" : "hourly_bonus", "non_incent", "", "view");
		
		//If we're not using the default bundled BG then grab the special one from S3
		if (themeName != "Default")
		{
			//Track Soft Prompt view
			string filename = string.Format(S3_BG_PATH, themeName);
			// Show ourselves after loading the background texture.
			Dialog.instance.showDialogAfterDownloadingTextures("ios_soft_prompt", filename, null, true, priority);
		}
		else
		{
			Dialog.instance.showDialogAfterDownloadingTextures("ios_soft_prompt", DEFAULT_BG_PATH, null, true, isExplicitPath:true, priorityType: priority);
		}
	}
	
	private static IEnumerator showGameIntroAfterDelay(float delay)
	{
		yield return new TIWaitForSeconds(delay);
		EUEManager.showGameIntro();
	}
	
	public static void onEnableFromPrompt()
	{
		if (!awaitingPNState)
		{
			return;
		}
		
		awaitingPNState = false;

		// If the notifs are enabled (after not being enabled)
		if (NotificationManager.DevicePushNotifsEnabled)
		{
			NotificationManager.RegisteredForPushNotifications = true;
			StatsManager.Instance.LogCount("dialog", "pn_system_dialog", launchedFromFtue ? "lobby_ftue" : "hourly_bonus", "non_incent", "yes", "click");
		}
		else
		{
			if (ExperimentWrapper.PushNotifSoftPrompt.isIncentivizedPromptEnabled)
			{
				StatsManager.Instance.LogCount("dialog", "pn_system_dialog", launchedFromFtue ? "lobby_ftue" : "hourly_bonus", "non_incent", "no", "click");
			}
		}
	}
	
	public static void resetStaticClassData()
	{
		awaitingPNState = false;
		launchedFromFtue = false;
	}
}
