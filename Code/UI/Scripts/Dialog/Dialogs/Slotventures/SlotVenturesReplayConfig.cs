using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SlotVenturesReplayConfig : MonoBehaviour 
{
	public GameObject oneButtonParent;
	public GameObject twoButtonParent;
	public GameObject trophyParent;
	public ButtonHandler oneButtonPlayAgain;
	public ButtonHandler twoButtonPlayAgain;
	public ButtonHandler twoButtonGoToLobby;
	public TextMeshPro jackpotText;
	public TextMeshPro jackpotDescription;
	public AchievementsShelfTrophy trophy;

	public void initWithOneButton(ClickHandler.onClickDelegate playAgainCallback, string newJackpot, Achievement achievement)
	{
		twoButtonParent.SetActive(false);
		oneButtonParent.SetActive(true);
		oneButtonPlayAgain.registerEventDelegate(playAgainCallback);

		setupJackpot(newJackpot);
		setupNextAchievement(achievement);
	}

	public void initWithTwoButtons(ClickHandler.onClickDelegate lobbyCallback, ClickHandler.onClickDelegate playAgainCallback, string newJackpot, Achievement achievement = null)
	{
		twoButtonParent.SetActive(true);
		oneButtonParent.SetActive(false);
		twoButtonGoToLobby.registerEventDelegate(lobbyCallback);
		twoButtonPlayAgain.registerEventDelegate(playAgainCallback);

		setupJackpot(newJackpot);
		setupNextAchievement(achievement);
	}
	
	public void setupNextAchievement(Achievement achievement)
	{
		if (achievement == null)
		{
			SafeSet.gameObjectActive(trophyParent, false);
			return;
		}
		
		SafeSet.gameObjectActive(trophyParent, true);
		
		trophy.init(achievement, SlotsPlayer.instance.socialMember);
	}

	
	private void setupJackpot(string newJackpot)
	{
		jackpotText.text = newJackpot;
		
		//If we have cards then change the description localization here
		jackpotDescription.text = Localize.text("slotventures_reward_coins_and_cards");
	}
}
