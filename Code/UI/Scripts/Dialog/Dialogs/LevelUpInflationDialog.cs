using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

/*
 Dialog that appears when a player levels up and their buy page inflation rate increases
 or is one level away from their infaltion rate increasing
*/

public class LevelUpInflationDialog : DialogBase
{
	[SerializeField] private GameObject thisLevelParent;
	[SerializeField] private GameObject nextLevelParent;

	[SerializeField] private TextMeshPro thisLevelLabel;
	[SerializeField] private TextMeshPro nextLevelLabel;

	[SerializeField] private TextMeshPro thisLevelPercentLabel;
	[SerializeField] private TextMeshPro nextLevelPercentLabel;


	[SerializeField] private ButtonHandler closeButton;
	[SerializeField] private ButtonHandler buyPageButton;
	[SerializeField] private TextMeshPro ctaButtonLabel;
	private const string levelLabelPrefix = "<size={0}>LEVEL</size> {1}";
	private const int thisLevelTextSize = 70;
	private const int nextLevelTextSize = 80;

	private bool isIncreasing = false;
	private int newLevel = 0;
	
	public override void init()
	{
		closeButton.registerEventDelegate(onCloseClicked);
		buyPageButton.registerEventDelegate(onBuyButtonClicked);
		isIncreasing = (bool)dialogArgs.getWithDefault(D.MODE, false);
		newLevel = (int)dialogArgs.getWithDefault(D.NEW_LEVEL, 0);
		if (isIncreasing)
		{
			thisLevelParent.SetActive(true);
			thisLevelLabel.text = string.Format(levelLabelPrefix, thisLevelTextSize, newLevel);
			thisLevelPercentLabel.text = string.Format("{0}%", SlotsPlayer.instance.currentBuyPageInflationPercentIncrease);
		}
		else
		{
			nextLevelParent.SetActive(true);
			nextLevelLabel.text = string.Format(levelLabelPrefix, nextLevelTextSize, newLevel + 1);
			nextLevelPercentLabel.text = string.Format("{0}%", SlotsPlayer.instance.nextBuyPageInflationPercentIncrease);
			ctaButtonLabel.text = Localize.text("okay");
		}
	}

	private void onBuyButtonClicked(Dict args = null)
	{
		if (isIncreasing)
		{
			BuyCreditsDialog.showDialog("", SchedulerPriority.PriorityType.IMMEDIATE);
		}
		Dialog.close();
	}

	private void onCloseClicked(Dict args = null)
	{
		Dialog.close();
	}

	public override void close ()
	{
		if (LevelUpUserExperienceFeature.instance.isEnabled)
		{
			checkUnlockDataAndShowToaster();
		}

	}

	private void checkUnlockDataAndShowToaster()
	{
		List<string> unlocked = GameUnlockData.findUnlockedGamesForLevel(newLevel);

		if (unlocked == null)
		{
			// No games unlocked at this level.
			return;
		}

		int i = 0;
		LobbyGame gameToUse = null;
		while (gameToUse == null && i < unlocked.Count)
		{
			gameToUse = LobbyGame.find(unlocked[i]);
			i++;
		}

		if (gameToUse != null)
		{
			Dict args = Dict.create(D.KEY, LevelUpUserExperienceToaster.PRESENTATION_TYPE.GAME_UNLOCK, D.OPTION1,
				gameToUse, D.OPTION2, LobbyGame.getNextUnlocked(newLevel));
			ToasterManager.addToaster(ToasterType.LEVEL_UP, args, null, 4f);
		}
	}


	public static void showDialog(int newLevel, bool isIncreasing)
	{
		Dict args = Dict.create(D.MODE, isIncreasing, D.NEW_LEVEL, newLevel);
		Scheduler.addDialog("level_up_inflation", args);
	}
}
