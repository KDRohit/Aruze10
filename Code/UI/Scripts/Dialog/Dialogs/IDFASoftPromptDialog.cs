using Com.HitItRich.IDFA;
using Com.Scheduler;

public class IDFASoftPromptDialog : DialogBase, IResetGame
{
	public ButtonHandler allowButton;
	public ButtonHandler noThanksButton;

	private AnswerDelegate allowCallback;
	private AnswerDelegate noThanksCallback;
	private IDFASoftPromptManager.SurfacePoint surfacePoint;

	private const string STATS_KINGDOM = "idfa_soft_prompt";
	private const string STATS_YES = "yes";
	private const string STATS_SKIP = "skip";
	private string statsPhylum = "";
	
	public override void init()
	{
		// Register button callbacks
		if (allowButton != null)
		{
			allowButton.registerEventDelegate(allowButtonClicked);
		}

		if (noThanksButton != null)
		{
			noThanksButton.registerEventDelegate(noThanksButtonClicked);
		}

		// Get parameters
		if (dialogArgs != null)
		{
			allowCallback = dialogArgs.getWithDefault(D.OPTION1, null) as AnswerDelegate;
			noThanksCallback = dialogArgs.getWithDefault(D.OPTION2, null) as AnswerDelegate;
			surfacePoint =
				(IDFASoftPromptManager.SurfacePoint) dialogArgs.getWithDefault(D.OPTION,
					(int) IDFASoftPromptManager.SurfacePoint.GameEntry);
		}

		// Log stat
		statsPhylum = (surfacePoint == IDFASoftPromptManager.SurfacePoint.W2E) ? "pre_w2e" : "app_entry";
		StatsManager.Instance.LogCount(
			counterName: TRACK_COUNTER,
			kingdom: STATS_KINGDOM,
			phylum: statsPhylum,
			genus: TRACK_CLASS_VIEW);
	}

	public override void close()
	{
		// Do special cleanup.
	}

	public void OnDestroy()
	{
		// Deregister button callbacks
		if (allowButton != null)
		{
			allowButton.unregisterEventDelegate(allowButtonClicked);
		}

		if (noThanksButton != null)
		{
			noThanksButton.unregisterEventDelegate(noThanksButtonClicked);
		}
	}

	private void allowButtonClicked(Dict args)
	{
		// Log stat
		StatsManager.Instance.LogCount(
			counterName: TRACK_COUNTER,
			kingdom: STATS_KINGDOM,
			phylum: statsPhylum,
			family: STATS_YES,
			genus: TRACK_CLASS_CLICK);

		Dialog.close(this);
		if (allowCallback != null)
		{
			allowCallback(null);
		}
	}

	private void noThanksButtonClicked(Dict args)
	{
		// Log stat
		StatsManager.Instance.LogCount(
			counterName: TRACK_COUNTER,
			kingdom: STATS_KINGDOM,
			phylum: statsPhylum,
			family: STATS_SKIP,
			genus: TRACK_CLASS_CLICK);

		Dialog.close(this);
		if (noThanksCallback != null)
		{
			noThanksCallback(null);
		}
	}

	public static void showDialog(IDFASoftPromptManager.SurfacePoint sp, AnswerDelegate allowCallback, AnswerDelegate noThanksCallback)
	{
		Scheduler.addDialog("idfa_soft_prompt", 
			Dict.create(
				D.OPTION1, allowCallback,
				D.OPTION2, noThanksCallback,
				D.OPTION, sp),
			SchedulerPriority.PriorityType.BLOCKING);
	}
}
