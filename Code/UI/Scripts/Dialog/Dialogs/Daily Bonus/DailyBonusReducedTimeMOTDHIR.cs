using UnityEngine;
using System.Collections;
using TMPro;

public class DailyBonusReducedTimeMOTDHIR : DailyBonusReducedTimeMOTD {

	public const string BACKGROUND_PATH = "reduced_daily_bonus/Reduced_Daily_Bonus_MOTD_BG_Image.png";
	public const string BACKGROUND_ANIM_PATH = "reduced_daily_bonus/Reduced_Daily_Bonus_MOTD_BG_Anim.png";
	public Renderer backgroundAnimRenderer;
	public Transform buttonParent;
	public GameObject hyperCollectNowParent;
	public GameObject hyperReadyInParent;
	public TextMeshPro buttonLabel;
	public TextMeshPro buttonTimerLabel;

	public override void init()
	{
		base.init();
		downloadedTextureToRenderer(backgroundAnimRenderer, 1); // Download the animation image
		iTween.RotateBy(backgroundAnimRenderer.gameObject,iTween.Hash("z", 10, "time", 100, "looptype", iTween.LoopType.loop, "easetype", iTween.EaseType.linear));

		// Sync positions of lobby button and dialog button.
		// MOTD has old lobby look for button, leaving this in if it ever gets updated for lobby v3
		/*
		if (MainLobby.hir != null && DailyBonusButtonHIR.instance != null)
		{
			buttonParent.position = new Vector3(DailyBonusButtonHIR.instance.transform.position.x, DailyBonusButtonHIR.instance.transform.position.y, buttonParent.position.z);
			// Add the difference between camera positions. This 10000 is Dialog Camera's local position x value.
			CommonTransform.setX(buttonParent, buttonParent.localPosition.x + 10000);
			// Adjust the localScale in case the lobby button gets stretched.
			buttonParent.localScale = MainLobby.hir.featureButtonsSizer.localScale;
		}
		else
		*/
		{
			buttonParent.gameObject.SetActive(false);
		}
	}

	protected override void Update() 
	{
		base.Update();

		if (SlotsPlayer.instance.dailyBonusTimer.isExpired)
		{
			if (!hyperCollectNowParent.activeSelf)
			{
				hyperReadyInParent.SetActive(false);
				hyperCollectNowParent.SetActive(true);
				buttonTimerLabel.text = Localize.textUpper("collect_now");
				buttonLabel.text = Localize.textUpper("hyperspeed_bonus");
			}
		}
		else
		{
			if (!hyperReadyInParent.activeSelf)
			{
				hyperReadyInParent.SetActive(true);
				hyperCollectNowParent.SetActive(false);
				buttonLabel.text = Localize.textUpper("hyperspeed_bonus");
			}
			buttonTimerLabel.text = SlotsPlayer.instance.dailyBonusTimer.timeRemainingFormatted;
		}

		timerLabel.text = DailyBonusReducedTimeEvent.timerRange.timeRemainingFormatted;
		AndroidUtil.checkBackButton(closeClicked);
	}
}
