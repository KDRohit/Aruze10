using UnityEngine;
using System.Collections;
using TMPro;

public class PartnerPowerupStatusButton : MonoBehaviour, IResetGame
{
	public ImageButtonHandler button;

	// On win and on progress
	public Animator eventAnimations;

	// Meter is verticle fill, so 0-1 for fullness
	public UISprite meter;

	public bool shouldPlayAnimation = false;

	public static PartnerPowerupStatusButton instance;
	void Awake()
	{
		button.registerEventDelegate(showIntoDialog);
		instance = this;
		if (CampaignDirector.partner == null)
		{
			StartCoroutine(waitForCampaign());
			return;
		}
		else
		{
			CampaignDirector.partner.addFunctionToOnGetProgress(onEventProgress);
		
			setMeterToCorrectScale();
		}
	}

	public void playAnimation()
	{
		eventAnimations.Play("rollup ani");

		setMeterToCorrectScale();

		if (CampaignDirector.partner.userProgress >= CampaignDirector.partner.individualProgressRequired && PlayerPrefsCache.GetInt(Prefs.HAS_SHOWN_PPU_COMPLETE) == 0)
		{
			eventAnimations.Play("bell indiviul win");
		}
		shouldPlayAnimation = false;
	}

	public void onEventProgress(Dict args = null)
	{
		shouldPlayAnimation = true;
	}

	public void setMeterToCorrectScale()
	{		
		long totalProgress = CampaignDirector.partner.buddyProgress + CampaignDirector.partner.userProgress;
		float scaleValue = 0;
		if (totalProgress != 0)
		{
			scaleValue = ((float)CampaignDirector.partner.userProgress + (float)CampaignDirector.partner.buddyProgress) / (float)CampaignDirector.partner.challengeGoal;
		}

		meter.fillAmount = scaleValue;
	}

	// Sometimes the data comes in late. So wait for it.
	private IEnumerator waitForCampaign()
	{
		while (CampaignDirector.partner == null)
		{
			yield return null;
		}

		CampaignDirector.partner.addFunctionToOnGetProgress(onEventProgress);
		setMeterToCorrectScale();
	}

	private void showIntoDialog(Dict args = null)
	{
		if (SlotBaseGame.instance != null && SlotBaseGame.instance.isGameBusy)
		{
			// Don't let people spam click while we're in here.
			return;
		}

		StatsManager.Instance.LogCount(counterName:"in_game", kingdom:"co_op_challenge", phylum:"game_meter", genus:"click");
		PartnerPowerupIntroDialog.showDialog();
	}

	public static void resetStaticClassData()
	{
		instance = null;
	}
}