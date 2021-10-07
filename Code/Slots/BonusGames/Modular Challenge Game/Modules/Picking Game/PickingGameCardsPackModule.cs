using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Com.Scheduler;

public class PickingGameCardsPackModule : PickingGameRevealModule
{
    [SerializeField] protected string REVEAL_ANIMATION_NAME = "revealCardPack";
    [SerializeField] protected float REVEAL_ANIMATION_DURATION_OVERRIDE = 0.0f;
    [SerializeField] protected string REVEAL_AUDIO = "pickem_cards_pick";
    [SerializeField] protected float REVEAL_AUDIO_DELAY = 0.0f; // delay before playing the reveal clip

    [SerializeField] private string packSource = "";
    private const string PACK_DROP_DIALOG_KEY = "collectables_pack_dropped";
    
    protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
    {
        // detect pick type & whether to handle with this module
        
        if (pickData != null && (!string.IsNullOrEmpty(pickData.cardPackKey) || pickData.meterAction == "bg_card_pack"))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    
    public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
    {
        ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentPickOutcome();
        Audio.playSoundMapOrSoundKeyWithDelay(REVEAL_AUDIO, REVEAL_AUDIO_DELAY);
        pickItem.setRevealAnim(REVEAL_ANIMATION_NAME, REVEAL_ANIMATION_DURATION_OVERRIDE);
        
        PickingGameCardPackPickItem cardPackPickItem = pickItem.GetComponent<PickingGameCardPackPickItem>();
        if (cardPackPickItem != null)
        {
            cardPackPickItem.setCardPack(currentPick.cardPackKey);
        }

        yield return StartCoroutine(base.executeOnItemClick(pickItem));
        
        if (!string.IsNullOrEmpty(packSource))
        {
            yield return StartCoroutine(showAllCardPacksCoroutine());
        }
    }

    private IEnumerator showAllCardPacksCoroutine()
    {
        List<string> packsToShow = new List<string>();
        ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentPickOutcome();

        if (!string.IsNullOrEmpty(currentPick.cardPackKey))
        {
            packsToShow.Add(currentPick.cardPackKey);
        }

        //Go through every possible part of the pick that can have a card pack attached to it and get all the packKeys to look for
        if (currentPick.rewardables != null)
        {
            for (int i = 0; i < currentPick.rewardables.Count; i++)
            {
                if (currentPick.rewardables[i] is RewardCardPack packReward)
                {
                    packsToShow.Add(packReward.packKey);
                }
                else if (currentPick.rewardables[i] is RewardableRewardBundle rewardBundle)
                {
                    for (int j = 0; j < rewardBundle.rewardables.Count; j++)
                    {
                        if (rewardBundle.rewardables[j] is RewardCardPack bundeldPackReward)
                        {
                            packsToShow.Add(bundeldPackReward.packKey);
                        }
                    }
                }
            }
        }

        List<SchedulerTask> currentTasks = Scheduler.findAllTasksWith(PACK_DROP_DIALOG_KEY);
        List<SchedulerTask> packDialogTasks = new List<SchedulerTask>();

        //Find any queued packs that match up with the card packs expected to be awarded from this pick
        for (int i = 0; i < currentTasks.Count; i++)
        {
            string taskPackKey = (string) currentTasks[i].args.getWithDefault(D.PACKAGE_KEY, "");
            if (!string.IsNullOrEmpty(taskPackKey) && packsToShow.Contains(taskPackKey))
            {
                packDialogTasks.Add(currentTasks[i]);
                packsToShow.Remove(taskPackKey);
            }
        }

        //Now actually show the card packs
        for (int i = 0; i < packDialogTasks.Count; i++)
        {
            packDialogTasks[i].execute();
            while (!packDialogTasks[i].removedFromScheduler)
            {
                yield return null;
            }
        }
    }
}
