using UnityEngine;
using System.Collections;
using Com.Scheduler;
using TMPro;

/*
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
*/

public class AntisocialDialog : DialogBase
{
	public TextMeshPro coinsLabel;
	[SerializeField] private ButtonHandler loginButton;
	[SerializeField] private ButtonHandler closeButton;
	
	// Initialization
	public override void init()
	{
		Audio.play("minimenuopen0");
		if (coinsLabel != null)
		{
			coinsLabel.text = CreditsEconomy.convertCredits(SlotsPlayer.instance.mergeBonus);
		}
		PlayerPrefsCache.SetInt(Prefs.SHOWN_LOGIN_DIALOG, 1); // Mark that we have spawned it before so we don't do it again.
		PlayerPrefsCache.Save();
		StatsManager.Instance.LogCount("dialog", "auth", "view", "view");

		loginButton.registerEventDelegate(loginClicked);
		closeButton.registerEventDelegate(skipClicked);
		MOTDFramework.markMotdSeen(dialogArgs);
	}
			
	void Update()
	{
		AndroidUtil.checkBackButton(skipClicked);
	}

	public void loginClicked(Dict args = null)
	{
		dialogArgs.merge(D.ANSWER, "login");
		StatsManager.Instance.LogCount("dialog", "auth", "facebook", "click");
		Dialog.close();
		if (SlotsPlayer.IsAppleLoggedIn)
		{
			SocialManager.Instance.FBConnect();
		}
		else
		{
			SlotsPlayer.facebookLogin();
		}
	}
	
	public void skipClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount("dialog", "auth", "skip", "click");
		Dialog.close();
	}
			
	// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup. Downloaded textures are automatically destroyed by Dialog class.
	}

	public static bool showDialog(string motdKey)
	{
		Scheduler.addDialog("antisocial",
			Dict.create(D.MOTD_KEY, motdKey));
		return true;
	}

	// Static method to show the dialog.
	public static bool showDialog(Dict args = null, SchedulerPriority.PriorityType priority = SchedulerPriority.PriorityType.LOW)
	{
		if (args != null)
		{
			args.Add(D.CALLBACK, new DialogBase.AnswerDelegate(SocialManager.antisocialCallback));
		}
		else
		{
			args = Dict.create(D.CALLBACK, new DialogBase.AnswerDelegate(SocialManager.antisocialCallback));
		}

		Scheduler.addDialog("antisocial", args, priority);
		return true;
	}
}
