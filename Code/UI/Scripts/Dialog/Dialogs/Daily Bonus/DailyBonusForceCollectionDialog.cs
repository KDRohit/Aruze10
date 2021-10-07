using UnityEngine;
using Com.HitItRich.EUE;
using Com.Scheduler;
using TMPro;

public class DailyBonusForceCollectionDialog : DialogBase
{
	[SerializeField] private Transform bonusButtonParent;
	[SerializeField] private GameObject dailyBonusButton;
	[SerializeField] private ClickHandler buttonHandler;
	[SerializeField] private Animator dailyBonusButtonAnimator;
	[SerializeField] private TextMeshPro displayText;

	public const string ON_ANIMATION = "on";
	public const string FORCE_COLLECT_AUDIO_KEY = "ForceCollectDialogOpenNewDailyBonus";
	
	public override void init()
	{
		Audio.play(FORCE_COLLECT_AUDIO_KEY);
	    if (DailyBonusButton.instance != null)
		{
			if (EUEManager.isEnabled)
			{
				displayText.text = Localize.text("forced_daily_bonus_collect_ftue");
			}
			else
			{
				displayText.text = Localize.text("forced_daily_bonus_text");
			}
			// Scale the button by the lobby button scaling factor.
			if (MainLobby.hirV3 != null)
			{
				dailyBonusButton.transform.localScale = Vector3.one * MainLobby.hirV3.getScaleFactor();
			}

			// Position the daily bonus button over the lobby one behind it.
			CommonTransform.matchScreenPosition(bonusButtonParent.transform, DailyBonusButton.instance.transform);
		}
		else
		{
			Debug.LogWarningFormat("DailyBonusForceCollectionDialog.cs -- init() -- could not find a daily bonus button in the lobby to match the position to. This dialog may look weird to the user.");
		}

		// Register the click handler
		buttonHandler.registerEventDelegate(bonusButtonClicked);
		// Probably start the animation when its here.
	    dailyBonusButtonAnimator.Play(ON_ANIMATION);

		MOTDFramework.markMotdSeen(dialogArgs);
	}
	
	private void bonusButtonClicked(Dict args = null)
	{
		string bonusString = ExperimentWrapper.NewDailyBonus.isInExperiment ?
			ExperimentWrapper.NewDailyBonus.bonusKeyName : "bonus";
		
		// Tell the Scheduler to wait until we get the claim action back.
		Scheduler.addTask(new DailyBonusForceCollectionTask(), SchedulerPriority.PriorityType.BLOCKING);
		CreditAction.claimTimerCredits(-1, bonusString); // Payout number isnt read on the server.
		Dialog.close();
	}

	public override void close()
	{
		// Turn off our button so it doesnt move with the animation and look weird.
		dailyBonusButton.SetActive(false);
	}

	public void Update()
	{
		AndroidUtil.checkBackButton(onCloseButtonClicked);
	}

	public override void onCloseButtonClicked(Dict args = null)
	{
		bonusButtonClicked();
	}

	public static bool showDialog(string motdKey = "", AnswerDelegate callback = null)
	{
		// Show the dialog.
		Scheduler.addDialog("daily_bonus_force_collect", Dict.create(D.MOTD_KEY, motdKey, D.CALLBACK, callback));
		return true;
	}
}
