using UnityEngine;
using System.Collections;
using Com.Scheduler;
using TMPro;

public class CreditRewardDialog : DialogBase
{
	public TextMeshPro creditsMeterLabel;
	[SerializeField] private ButtonHandler collectButton;

	private long giftedCredits = 0;

	private const float ROLLUP_DURATION = 1.0f;

	public override void init()
	{
		giftedCredits = (long)dialogArgs.getWithDefault(D.BONUS_CREDITS, 0);
		creditsMeterLabel.text = CreditsEconomy.convertCredits(0, true);
		collectButton.registerEventDelegate(collectClicked);
	}

    protected override void onFadeInComplete()
	{
		base.onFadeInComplete();

		// Start the rollup.
		StartCoroutine(SlotUtils.rollup(start:0,
			end:giftedCredits,
			tmPro:creditsMeterLabel,
			playSound:true,
			specificRollupTime:ROLLUP_DURATION)
		);
	}

	public void collectClicked(Dict args = null)
	{
		collectButton.clearAllDelegates();
		if (giftedCredits > 0)
		{
			SlotsPlayer.addCredits(giftedCredits, "url reward");
		}
		Dialog.close();
	}

	public override void close()
	{
		// Do cleanup here.
	}

	public static void showDialog(long creditsRewarded)
	{
		Debug.LogFormat("PN> reward_credit_dialog added to Scheduler with {0} credits.", creditsRewarded);
		Scheduler.addDialog("reward_credit_dialog", Dict.create(D.BONUS_CREDITS, creditsRewarded));
	}
}