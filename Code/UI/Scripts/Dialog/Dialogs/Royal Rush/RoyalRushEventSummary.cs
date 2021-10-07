﻿﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RoyalRushEventSummary : MonoBehaviour
{
	public Animator finalResults;
	public Animator rankWin;
	public Animator rulerWin;

	public TextMeshPro rankText;
	public TextMeshPro winAmount;

	public TextMeshPro royalWinAmount;

	public FacebookFriendInfo userInfo;

	public ButtonHandler continueButtonRanked;
	public ButtonHandler continueButtonRuler;

	public GameObject coinTrail;

	public const string FINAL_RESULTS_INTRO = "Final Results Intro";
	public const string RANK_WIN_INTRO = "Rank Win Intro";
	public const string REIGNING_WIN_INTRO = "Reigning Win Intro";

	private const float FINAL_RESULTS_LENGTH = 2.8f;

	private RoyalRushStandingsDialog dialogHandle;

	public IEnumerator playFinalResultsIntro(RoyalRushStandingsDialog instance, int rank = 0)
	{
		finalResults.gameObject.SetActive(true);
		finalResults.Play(FINAL_RESULTS_INTRO);
		continueButtonRuler.registerEventDelegate(onClickContinue);
		continueButtonRanked.registerEventDelegate(onClickContinue);

		dialogHandle = instance;

		rankText.text = CommonText.formatContestPlacement(dialogHandle.infoToUse.finalRank + 1);
		royalWinAmount.text = CommonText.formatNumber(dialogHandle.infoToUse.creditsAwarded * CreditsEconomy.economyMultiplier);
		winAmount.text = CommonText.formatNumber(dialogHandle.infoToUse.creditsAwarded * CreditsEconomy.economyMultiplier);

		if (SlotsPlayer.instance.socialMember != null)
		{
			userInfo.member = SlotsPlayer.instance.socialMember;
		}
		else
		{
			Debug.LogError("RoyalRushEventSummary::playFinalResultsIntro - User info for royal rush was null! This should not happen at this point.");
		}

		// unranked
		if (rank == -1)
		{
			Audio.play("ContestOverNormalRRush01");
		}
		else if (rank == 0)
		{
			Audio.play("ContestOverWinnerRRush01");
		}
		else
		{
			Audio.play("ContestOverRankedRRush01");
		}

		yield return new WaitForSeconds(FINAL_RESULTS_LENGTH);

		finalResults.gameObject.SetActive(false);

		yield return null;

		if (dialogHandle.infoToUse.creditsAwarded == 0)
		{
			onClickContinue();
			yield break;
		}
		else if (rank == 0)
		{
			rulerWin.gameObject.SetActive(true);
			rulerWin.Play(REIGNING_WIN_INTRO);
		}
		else
		{
			rankWin.gameObject.SetActive(true);
			rankWin.Play(RANK_WIN_INTRO);
		}

		yield return null;
	}

	private void onClickContinue(Dict args = null)
	{
		if (dialogHandle.infoToUse.creditsAwarded != 0)
		{
			StartCoroutine(playCoinAnimAndContinue());
		}
		else
		{
			goToDialog();
		}
	}

	private IEnumerator playCoinAnimAndContinue()
	{
		coinTrail.SetActive(true);

		iTween.MoveTo(coinTrail,
			iTween.Hash(
				"position", new Vector3(375, 890, -30),
				"time", 2.5f,
				"isLocal", true,
				"easetype", iTween.EaseType.easeInOutQuad));

		yield return new WaitForSeconds(2.75f);
		goToDialog();
	}

	private void goToDialog()
	{
		coinTrail.SetActive(false);
		Audio.play("LeaderboardEntryWipeRRush01");
		StatsManager.Instance.LogCount("royal_rush",
									   "contest_end_grant",
									   dialogHandle.infoToUse.rushKey,
									   "",
									   dialogHandle.infoToUse.finalRank.ToString(),
									   dialogHandle.infoToUse.creditsAwarded.ToString(),
									   milestone: dialogHandle.infoToUse.gameKey);

		SlotsPlayer.addFeatureCredits(dialogHandle.infoToUse.creditsAwarded, "royalRushCreditWin");
		dialogHandle.toggleAnimaionObjects(true);
		gameObject.SetActive(false);
	}
}

