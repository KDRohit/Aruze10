using UnityEngine;
using System.Collections;
using Com.Scheduler;
using TMPro;

// credit reward for flash to webgl upgrade, this event only happens on webgl version
public class FlashWebGLThankYouDialog : DialogBase
{
	public TextMeshPro creditsMeterLabel;
	public ButtonHandler collectButtonHandler;
	private long creditsToAward = 0;
	private string eventID;
	private bool hasCollected;
	private const float ROLLUP_DURATION = 1.0f;

	public override void init()
	{
		collectButtonHandler.registerEventDelegate(onCloseButtonClicked);

		eventID = (string)dialogArgs[D.EVENT_ID];
		creditsToAward = (long)dialogArgs.getWithDefault(D.BONUS_CREDITS, 0);
		creditsMeterLabel.text = CreditsEconomy.convertCredits(0, true);

		StatsManager.Instance.LogCount(counterName: "dialog", 
									kingdom: "webgl_coin_grant", 
									phylum:"view", 
									klass: "",
									genus: "view");		
	}

    protected override void onFadeInComplete()
	{
		base.onFadeInComplete();

		// Start the rollup.
		StartCoroutine(SlotUtils.rollup(start:0,
			end:creditsToAward,
			tmPro:creditsMeterLabel,
			playSound:true,
			specificRollupTime:ROLLUP_DURATION)
		);
	}

	public override void onCloseButtonClicked(Dict args = null)
	{
		if (hasCollected)
		{
			return;
		}

		StatsManager.Instance.LogCount(counterName: "dialog", 
									kingdom: "webgl_coin_grant", 
									phylum:"click", 
									klass: "",
									genus: "click");				

		hasCollected = true;

		CreditAction.acceptFlashToWebglCredits(eventID);
		SlotsPlayer.addCredits(creditsToAward, "webgl incentive accepted");			

		Dialog.close();
	}

	public static void registerEventDelegates()
	{
#if UNITY_WEBGL
		Server.registerEventDelegate("flash_to_webgl_credits", webGlCreditsAwardedServerEvent, false);
#endif
	}

	public static void webGlCreditsAwardedServerEvent(JSON data)
	{
#if UNITY_WEBGL
		FlashWebGLThankYouDialog.showDialog(
			data.getString("event", ""),
			data.getLong("credits", 0)
		);
#endif
	}

	public override void close()
	{
		// Do cleanup here.
	}

	public static void showDialog(string eventID, long creditsRewarded)
	{
		Debug.LogFormat("flash_webgl_thank_you_dialog added to Scheduler with {0} credits.", creditsRewarded);
		Scheduler.addDialog("flash_webgl_thank_you_dialog", Dict.create(D.EVENT_ID, eventID, D.BONUS_CREDITS, creditsRewarded));
	}
}