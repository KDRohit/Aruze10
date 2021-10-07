using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PartyTime : ChallengeGame
{
	public PartyTimePickem partyTimePickem;
	public PartyTimeFreeSpins partyTimeFreeSpins;
	private string[] bonusGameLabelText = { "party_free_spins", "party_time", "free_spins" };

	private PickemOutcome pickemOutcome;
	private bool isGameEnded = false;
	
	public override void init() 
	{	
		pickemOutcome = (PickemOutcome)BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE];
		_didInit = true;
	}
	
	private IEnumerator showFreeSpins()
	{
		yield return new WaitForSeconds(1.0f);
		if (BonusGameManager.instance.wings != null)
		{
			BonusGameManager.instance.wings.hide();
		}
		BonusGamePresenter.instance.gameScreen = partyTimeFreeSpins.transform.parent.gameObject;
		SpinPanel.instance.showPanel(SpinPanel.Type.FREE_SPINS);
		Destroy(partyTimePickem.transform.parent.gameObject);
		partyTimeFreeSpins.transform.parent.gameObject.SetActive(true);
		partyTimeFreeSpins.initFreespins();

		StartCoroutine(cycleBonusGameLabelText());
	}

	private IEnumerator cycleBonusGameLabelText()
	{
		if (bonusGameLabelText.Length > 0)
		{
			int currentIndex = 0;
			while (true)
			{
				SpinPanel.instance.modifyBonusGameLabel(bonusGameLabelText[currentIndex]);
				currentIndex = (currentIndex + 1) % bonusGameLabelText.Length;

				yield return new WaitForSeconds(2f);
			}
		}
	}

	public void pickemButtonPressed(GameObject button)
	{
		PickemPick pick = pickemOutcome.getNextEntry();
		doPick (button,pick);
	}

	private void doPick(GameObject button, PickemPick pick)
	{
		partyTimePickem.onPickSelected(button, pick, continueAfterPick);
	}

	public void continueAfterPick(PickemPick pick)
	{
		if (pick.isGameOver) 
		{
			BonusGamePresenter.instance.currentPayout = BonusGamePresenter.portalPayout; // transfer the payout since we're not going to free spins
			StartCoroutine(endGame());
		}
		else if (pickemOutcome.entryCount == 0)
		{
			SlotOutcome ted01_partytime_freespins = SlotOutcome.getBonusGameOutcome(BonusGameManager.currentBonusGameOutcome, pick.bonusGame);
			if(ted01_partytime_freespins != null)
			{
				BonusGameManager.instance.outcomes[BonusGameType.GIFTING] = new FreeSpinsOutcome(ted01_partytime_freespins);
			}
			StartCoroutine(endGame());
		}
	}
	
	private IEnumerator endGame()
	{
		if(!isGameEnded)
		{
			isGameEnded = true;
			yield return StartCoroutine(partyTimePickem.revealAllPicks(pickemOutcome));
			if (partyTimePickem.microphonesPicked == PartyTimePickem.MICROPHONES_NEEDED)
			{
				Debug.Log ("go to free spins");
				StartCoroutine(showFreeSpins());
			}
			else
			{
				Debug.Log ("end game");
				BonusGamePresenter.instance.gameEnded();
			}
		}
	}
}
