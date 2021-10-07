using System.Collections;

/*
 * Does final freespins value multiplication for total win amount at freespins end
 * 
 * Games: orig012
 *
 * Original Author: Shaun Peoples <speoples@zynga.com>
 */
public class FreeSpinsTotalWinMultiplierRollupModule : SlotModule
{
    //mutation to look for
    private ReevaluationRetriggerAndMultiplyFromPick mutator;

    private long multiplier = 1;
    
    private class ReevaluationRetriggerAndMultiplyFromPick :  ReevaluationBase
    {
        public long addMultiplier;
        public long newMultiplier;
        public bool active;

        public ReevaluationRetriggerAndMultiplyFromPick(JSON reevalJSON) : base(reevalJSON)
        {
            addMultiplier = reevalJSON.getLong("add_multiplier", 0);
            newMultiplier = reevalJSON.getLong("new_multiplier", -1);
            active = reevalJSON.getBool("active", false) && newMultiplier > 0;
        }
    }

    private ReevaluationRetriggerAndMultiplyFromPick getReevaluator()
    {
        JSON[] arrayReevaluations = ReelGame.activeGame.outcome.getArrayReevaluations();
        foreach (JSON reeval in arrayReevaluations)
        {
            string reevalType = reeval.getString("type", "");
            if (reevalType == "retrigger_and_multiply_from_pick_game_reevaluator")
            {
                mutator = new ReevaluationRetriggerAndMultiplyFromPick(reeval);
                if (mutator.addMultiplier > 0 && multiplier < mutator.newMultiplier)
                {
                    multiplier = mutator.newMultiplier;
                }
                return mutator;
            }
        }

        return null;
    }

    public override bool needsToExecuteAfterPaylines()
    {
        getReevaluator();
        return multiplier > 1 && reelGame.numberOfFreespinsRemaining < 1;
    }

    public override IEnumerator executeAfterPaylinesCallback(bool winsShown)
    {
        //check to see if there's a valid add, otherwise, it might be just the add spins data
        if (!reelGame.hasFreespinsSpinsRemaining && multiplier > 1)
        {
            long payout = BonusGamePresenter.instance.currentPayout;

            yield return StartCoroutine(SlotUtils.rollup(
                start: payout,
                end: payout * multiplier, 
                tmPro: BonusSpinPanel.instance.winningsAmountLabel,
                playSound: true, 
                shouldSkipOnTouch: true,
                shouldBigWin: false,
                isCredit: true));

            BonusGamePresenter.instance.currentPayout = payout * multiplier;
        }
    }
}