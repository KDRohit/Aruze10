using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using UnityEngine;

public class PickingGameRevealOnRoundStartCardsPackModule : PickingGameRevealOnStartModule
{
    [SerializeField] private string packSource = "";
    
    private const string PACK_DROP_DIALOG_KEY = "collectables_pack_dropped";

    protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
    {
        // detect pick type & whether to handle with this module
        if (pickData != null && (!string.IsNullOrEmpty(pickData.cardPackKey) || pickData.containsRewardable(RewardCardPack.TYPE)))
        {
            return true;
        }

        return false;
    }

    public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
    {
        // Play post reveal anims (like hiding the picking object)
        if (!pickItem.isPlayingPostRevalAnimsImmediatelyAfterReveal && pickItem.hasPostRevealAnims())
        {
            yield return StartCoroutine(pickItem.playPostRevealAnims());
        }
        
        yield return StartCoroutine(collectItem());
    }

    protected override IEnumerator collectItem()
    {
        if (!string.IsNullOrEmpty(packSource))
        {
            yield return StartCoroutine(showAllCardPacksCoroutine());
        }
        
        //Play card outro when we come back from pack drop dialog
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(onClaimAnimationInformationList));
    }
    
    protected override IEnumerator autoRevealPick(PickingGameBasePickItem revealedItem)
    {
        revealType = PickingGameRevealSpecificLabelRandomTextPickItem.PickItemRevealType.CardPack;
        yield return StartCoroutine(base.autoRevealPick(revealedItem));
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
