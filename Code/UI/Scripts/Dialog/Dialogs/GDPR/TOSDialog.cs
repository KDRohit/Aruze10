using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using UnityEngine;
using TMPro;
using Zynga.Core.Util;

public class TOSDialog : DialogBase, IResetGame
{
	private static bool viewed = false;
	public GameObject existingUserGraphics;
	public GameObject newUserGraphics;
	public ButtonHandler acceptButton;
	public TextMeshPro policyLabel;
	public TextMeshPro infoLabel;
	public TextMeshPro newUserPolicyLabel;
	private PreferencesBase preferences;

	private const string tosPolicyKey = "gdpr_tos";
	private const string tosInfoKey = "gdpr_tos_instruction";

	public static bool setCustomData { get; private set; }


	public override void init()
	{
		preferences = SlotsPlayer.getPreferences();

		acceptButton.registerEventDelegate(acceptButtonClicked);

		bool isNewUser = (bool)dialogArgs.getWithDefault(D.OPTION, false);
		SafeSet.gameObjectActive(newUserGraphics, isNewUser);
		SafeSet.gameObjectActive(existingUserGraphics, !isNewUser);


		string policyText = Localize.text(tosPolicyKey, "url:" + Glb.HELP_LINK_TERMS, "url:" + Glb.HELP_LINK_PRIVACY);

		if (isNewUser)
		{
			newUserPolicyLabel.text = policyText;
		}
		else
		{
			infoLabel.text = Localize.text(tosInfoKey);
			policyLabel.text = policyText;
		}

	}

	void Update()
	{
		TouchInput.update();
	}

	public override void close()
	{
		// Do special cleanup.
	}

	public void OnDestroy()
	{
		acceptButton.unregisterEventDelegate(acceptButtonClicked);
		StatsManager.Instance.LogCount("dialog", "terms_of_service", "", "", "close", "click");
	}

	private void acceptButtonClicked(Dict args)
	{
		setCustomData = true;
		preferences.SetInt(Prefs.GDPR_TOS_VIEWED, Data.liveData.getInt("TOS_UPDATE_RUNTIME_VERSION", 0));

		PlayerAction.acceptTermsOfService();

		AnalyticsManager.Instance.LogTOSAccept();

		// this dialog shows while lobby is loading and hides the loading screen
		// show it again on closing so other dialogs can't show and players don't see exposed top overlay with blank lobby for a few seconds
		Loading.show(Loading.LoadingTransactionTarget.LOBBY);
		Dialog.close();
	}

	

	/// <summary>
	/// This is called when a user that hasn't viewed the tos first reaches gdpr code.
	/// </summary>
	public static void GDPRUpgrade()
	{
		setCustomData = true;
		SlotsPlayer.getPreferences().SetInt(Prefs.GDPR_TOS_VIEWED, Data.liveData.getInt("TOS_UPDATE_RUNTIME_VERSION", 0));
	}


	public static void showDialog(DialogBase.AnswerDelegate callback, bool isNewUser)
	{
		if (isNewUser && viewed)
		{
			if (null != callback)
			{
				callback(null);
			}
		}
		else
		{
			Dict args = Dict.create(D.OPTION, isNewUser, D.CALLBACK, callback);
			Scheduler.addDialog("terms_of_service", args, SchedulerPriority.PriorityType.BLOCKING);
			viewed = true;
			AnalyticsManager.Instance.LogTOSView();
		}
		
	}

	public static void resetStaticClassData()
	{
		//Do not reset viewed;
		setCustomData = false;
	}

}
