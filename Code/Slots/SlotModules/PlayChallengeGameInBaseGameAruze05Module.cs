//
// This module is used to activate a challenge game that is inside the basegame by overriding
// executeOnPreBonusGameCreated and blocking until it is completed.
//
// This is used in Aruze05 to trigger a wheel game that is played in the base game

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayChallengeGameInBaseGameAruze05Module : SlotModule
{
    [Tooltip("List of bonus game names that can be playing in the base game.")]
    [SerializeField] private List<string> bonusGameNames;

    [Tooltip("The BonusGamePresenter that should be present.")]
    [SerializeField] private BonusGamePresenter challengePresenter;

    [Tooltip("The ChallengeGame that will be played before the bonus game.")]
    [SerializeField] private ModularChallengeGame challengeGame;

    private BonusGamePresenter _prevPresenterInstance;

    public override bool needsToExecuteOnPreBonusGameCreated()
    {
        if (challengeGame == null || challengePresenter == null)
        {
            return false;
        }

        SlotOutcome slotOutcome = reelGame.outcome.getBonusGameOutcome(bonusGameNames[0]);
        if (slotOutcome != null)
        {
            return true;
        }

        return false;
    }

    public override bool needsToLetModuleCreateBonusGame()
    {
        if (challengeGame == null || challengePresenter == null)
        {
            return false;
        }

        SlotOutcome slotOutcome = reelGame.outcome.getBonusGameOutcome(bonusGameNames[0]);
        if (slotOutcome != null)
        {
            return true;
        }

        return false;
    }

    public override IEnumerator executeOnPreBonusGameCreated()
    {
        // ensure that we correctly set the instance to be the game we are about to show,
        // because Awake() which normally set it will only be called the first time it is shown
        InGameFeatureContainer.showFeatureUI(false);
        challengePresenter.gameObject.SetActive(true);
        _prevPresenterInstance = BonusGamePresenter.instance;
        BonusGamePresenter.instance = challengePresenter;
        BonusGameManager.instance.currentGameKey = GameState.game.keyName;
        BonusGameManager.instance.summaryScreenGameName = bonusGameNames[0];
        BonusGameManager.instance.currentMultiplier = ReelGame.activeGame.relativeMultiplier;
        challengePresenter.init(isCheckingReelGameCarryOverValue:true);

        challengeGame.gameObject.SetActive(true);
        challengeGame.init();

        // wait till this challenge game feature is over before continuing
        while (challengePresenter.isGameActive)
        {
            yield return null;
        }
        challengePresenter.finalCleanup();
        yield return null;
        challengeGame.reset();
        challengeGame.gameObject.SetActive(false);
        challengePresenter.gameScreen.SetActive(false);
        SlotBaseGame.activeGame.outcome.isChallenge = false;
        BonusGamePresenter.instance = _prevPresenterInstance;
        InGameFeatureContainer.showFeatureUI(true);
    }
}
