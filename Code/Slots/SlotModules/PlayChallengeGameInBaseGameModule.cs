//
// This module is used to activate a challenge game that is inside the basegame by overriding
// executeOnPreBonusGameCreated and blocking until it is completed.
//
// The bonus game to play in the basegame should be the first bonus game found in the slotOutcome.
//
// This is used in orig002 to trigger a wheel game that is played in the base game to award additional freespins before
// proceeding to the freespin game.
//
// Author : Nick Saito <nsaito@zynga.com>
// Date : Sept 9th, 2020
// Games : orig002, orig012
//

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

public class PlayChallengeGameInBaseGameModule : SlotModule
{
	[System.Serializable]
	private class BonusGameCriteria
	{
		public string bonusGamePaytableName;
		public bool searchInSubSubOutcomesForBonusGame = false;
		public string currentGameKeyOverride;
		public ChallengeGameCreationCheckExecutionPoint createBonusGameCheckpoint = ChallengeGameCreationCheckExecutionPoint.OnPreBonusGameCreated;
	}

	private enum ChallengeGameCreationCheckExecutionPoint
	{
		OnReelsStopped,
		OnPreBonusGameCreated,
		OnBoth
	}

	[Tooltip("When should this check for needing to create a bonus game?")]
	private ChallengeGameCreationCheckExecutionPoint createBonusGameCheckpoint;

	[Tooltip("Bonus Game Criteria, used to determine if bonus game should be launched, how it is found in outcomes, and when to check to create")] [SerializeField]
	private List<BonusGameCriteria> bonusGameCriteria;
	private BonusGameCriteria selectedBonusGameCriteria;
	
	[Tooltip("List of bonus game names that can be playing in the base game.")]
	[SerializeField] private List<string> bonusGameNames;

	[Tooltip("The BonusGamePresenter that should be present.")]
	[SerializeField] private BonusGamePresenter challengePresenter;

	[Tooltip("The ChallengeGame that will be played before the bonus game.")]
	[SerializeField] private ModularChallengeGame challengeGame;

	[Tooltip("Keep the existing value of BonusGameManager.currentBaseGame ")] 
	[SerializeField] private bool keepCurrentBasegame;
	
	[Tooltip("Pass through Reelgame relativeMultiplier to BonusGameManager currentMultiplier")] 
	[SerializeField] private bool passThroughRelativeMultiplier;

	private bool hasShownBonusOnce = false; // Only used if bonusGameCriteria aren't defined (the older way of using this module).  In the older version we only want the bonus stuff to trigger once (so this flag will check once it has been shown, and not show it again).
	
	// executeOnPreSpin() section
	// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) before the reels spin
	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnPreSpin()
	{
		// Reset this every spin so that another bonus could be triggered on the next spin.
		// This flag is unused if bonusGameCriteria are setup to define when to trigger this module.
		hasShownBonusOnce = false;
		yield break;
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return checkCreateCriteria(ChallengeGameCreationCheckExecutionPoint.OnReelsStopped);
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		yield return StartCoroutine(createBonusGame());
	}

	public override bool needsToExecuteOnPreBonusGameCreated()
	{
		return checkCreateCriteria(ChallengeGameCreationCheckExecutionPoint.OnPreBonusGameCreated);
	}

	public override IEnumerator executeOnPreBonusGameCreated()
	{
		yield return StartCoroutine(createBonusGame());
	}

	private bool checkCreateCriteria(ChallengeGameCreationCheckExecutionPoint checkpoint)
	{
		if (challengeGame == null || challengePresenter == null)
		{
			return false;
		}

		if (bonusGameCriteria != null && bonusGameCriteria.Count > 0)
		{
			foreach (BonusGameCriteria bonusGameCriteria in bonusGameCriteria)
			{
				SlotOutcome slotOutcome = null;
				if ((reelGame.outcome.hasBonusGame() && (slotOutcome = reelGame.outcome.getBonusGameInOutcomeDepthFirst()) != null) || 
				    (bonusGameCriteria.searchInSubSubOutcomesForBonusGame && hasBonusGameInSubSubOutcomes(out slotOutcome)))
				{
					string bonusGameTableName = slotOutcome.getBonusGamePayTableName();
					if (bonusGameTableName == bonusGameCriteria.bonusGamePaytableName && (checkpoint == bonusGameCriteria.createBonusGameCheckpoint || 
						bonusGameCriteria.createBonusGameCheckpoint == ChallengeGameCreationCheckExecutionPoint.OnBoth))
					{
						selectedBonusGameCriteria = bonusGameCriteria;
						return true;
					}
				}
			}

			return false;
		}
		//used by pre-existing games
		else
		{
			if (reelGame.outcome.hasBonusGame() && !hasShownBonusOnce)
			{
				hasShownBonusOnce = true;
				SlotOutcome slotOutcome = reelGame.outcome.getBonusGameInOutcomeDepthFirst();
				string bonusGameTableName = slotOutcome.getBonusGamePayTableName();
				return bonusGameNames.Contains(bonusGameTableName);
			}
		}

		return false;
	}

	private bool hasBonusGameInSubSubOutcomes(out SlotOutcome returnOutcome)
	{
		ReadOnlyCollection<SlotOutcome> subOutcomeList = reelGame.outcome.getSubOutcomesReadOnly();
		foreach (SlotOutcome subOutcome in subOutcomeList)
		{
			if (subOutcome.getOutcomeType() == SlotOutcome.OutcomeTypeEnum.BONUS_GAME)
			{
				returnOutcome = subOutcome;
				return true;
			}
			
			ReadOnlyCollection<SlotOutcome> subSubSubOutcomes = subOutcome.getSubOutcomesReadOnly();
			if (subSubSubOutcomes.Count <= 0)
			{
				continue;
			}
			
			foreach (SlotOutcome sub in subSubSubOutcomes)
			{
				if (sub.getOutcomeType() != SlotOutcome.OutcomeTypeEnum.BONUS_GAME && !sub.hasQueuedBonuses)
				{
					continue;
				}
				
				returnOutcome = sub;
				return true;
			}
		}

		if (reelGame.outcome.hasQueuedBonuses)
		{
			returnOutcome = reelGame.outcome;
			return true;
		}

		returnOutcome = null;
		return false;
	}
	
	private IEnumerator createBonusGame()
	{
		// ensure that we correctly set the instance to be the game we are about to show,
		// because Awake() which normally set it will only be called the first time it is shown
		challengePresenter.gameObject.SetActive(true);

		if (selectedBonusGameCriteria != null && !string.IsNullOrEmpty(selectedBonusGameCriteria.currentGameKeyOverride))
		{
			BonusGameManager.instance.currentGameKey = selectedBonusGameCriteria.currentGameKeyOverride;
		}
		
		BonusGamePresenter.instance = challengePresenter;

		if (passThroughRelativeMultiplier)
		{
			BonusGameManager.instance.currentMultiplier = ReelGame.activeGame.relativeMultiplier;
		}

		if (!keepCurrentBasegame)
		{
			BonusGameManager.currentBaseGame = null;
		}
		challengePresenter.init(isCheckingReelGameCarryOverValue:true);

		challengeGame.gameObject.SetActive(true);
		challengeGame.init();

		// wait till this challenge game feature is over before continuing
		while (challengePresenter.isGameActive)
		{
			yield return null;
		}
		
		challengeGame.reset();
		challengeGame.gameObject.SetActive(false);
		challengePresenter.gameScreen.SetActive(false);
	}
}
