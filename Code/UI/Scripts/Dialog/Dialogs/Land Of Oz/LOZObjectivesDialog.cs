using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class LOZObjectivesDialog : ChallengeLobbyObjectivesDialog
{	
	public override void init()
	{		
		base.init();
		
		if (didCompleteAll)
		{
			// Let the lobby know to show the special presentation the next time the player is in the LOZ lobby.
			CustomPlayerData.setValue(CustomPlayerData.LOZ_LOBBY_COMPLETE_SEEN, false);
			jackpotLabel.text = CommonText.formatNumber(campaign.currentJackpot);
		}

		if (didCompleteThis)
		{
			Audio.play("ObjectiveClearSingleLOOZ");
		}
	}
	
	// When all events are complete, show the final presentation.
	protected override IEnumerator showJackpot()
	{		
		// Wait for the checkmark animations to finish before fading out.
		yield return new WaitForSeconds(2.0f);
		
		yield return StartCoroutine(objectiveGrid.fadeOutAchievements());
		
		achievementsParent.SetActive(false);
		jackpotParent.SetActive(true);
		
		iTween.ScaleTo(jackpotParent,
			iTween.Hash(
				"scale", Vector3.one,
				"time", 1.0f,
				"easetype", iTween.EaseType.easeOutQuad
			)
		);

		long jackpotAmount = 100000;
		if (rewards.Count > 0)
		{
			jackpotAmount = rewards[0].amount;
		}

		isShowingJackpot = true;

		// Roll up the jackpot amount.
		yield return StartCoroutine(SlotUtils.rollup(
			start: 0L,
			end: jackpotAmount,
			tmPro: jackpotLabel,
			specificRollupTime: 5.0f,
			rollupOverrideSound: "RollupCollectJockpotLOOZ",
			rollupTermOverrideSound: "RollupTermCollectJackpotLOOZ"
		));
		
		StartCoroutine(skipJackpotAnim());
	}
	
	protected override IEnumerator skipJackpotAnim()
	{
		if (didSkipJackpot)
		{
			yield break;
		}
			
		didSkipJackpot = true;

		StatsManager.Instance.LogCount("dialog", "loz_jackpot_intro", "", "tier" + (campaign as LOZCampaign).tier.ToString());

		// user finished last tier? don't do this new jackpot rollup
		if (campaign.isActive)
		{
			jackpotAnim.SetTrigger("Finished");
					
			// Give the transition a little time to happen before starting the next rollup.
			yield return new WaitForSeconds(1.0f);
		
			// Roll up the new jackpot amount.
			long newJackpotAmount = campaign.currentJackpot;

			yield return StartCoroutine(SlotUtils.rollup
			(
				start: 0L,
				end: newJackpotAmount,
				tmPro: newJackpotLabel,
				specificRollupTime: 3.0f,
				rollupOverrideSound: "NextTierIncrementLoopLOOZ",
				rollupTermOverrideSound: "NextTierIncrementTermLOOZ"
			));
		}
		else
		{
			closeButton.SetActive(true); // doesn't go to the final jackpot rollup, show the close button
		}
	}	
	
	// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		Audio.play("DialogueCloseLOOZ");
		
		// Do special cleanup. Downloaded textures are automatically destroyed by Dialog class.
	}
	
	protected override void playOpenSound()
	{
		Audio.play("DialogueOpenLOOZ");
	}

	public override void playCloseSound()
	{
		Audio.play("DialogueCloseLOOZ");
	}
}
