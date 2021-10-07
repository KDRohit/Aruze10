using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;
using UnityEngine;

public class RichPassNewSeasonalChallengesDialog : DialogBase
{
    [SerializeField] private ObjectSwapper tierSwapper;
    [SerializeField] private GameObject challengesIconPrefab;
    [SerializeField] private UICenteredGrid challengesGrid;
    [SerializeField] private TextMeshPro endsInLabel;
    [SerializeField] private ButtonHandler ctaButton;

    public override void init()
    {
        Audio.play("MOTDRichPass");
        int totalMissionsDisplaying = 0;
        tierSwapper.setState(CampaignDirector.richPass.passType);
        int latestUnlockTimestamp = (int)dialogArgs.getWithDefault(D.DATA, 0);
        List<Mission> newMissions = CampaignDirector.richPass.seasonMissions[latestUnlockTimestamp];
        
        endsInLabel.text = Localize.text("ends_in");
        CampaignDirector.richPass.timerRange.registerLabel(endsInLabel, keepCurrentText: true);
        CampaignDirector.richPass.timerRange.registerFunction(onFeatureExpire);
        for (int i = 0; i < newMissions.Count; i++)
        {
            for (int j = 0; j < newMissions[i].objectives.Count; j++)
            {
                GameObject spawnedObjectiveBlock = NGUITools.AddChild(challengesGrid.transform, challengesIconPrefab);
                RichPassChallengesTypeBlock challengesTypeBlock = spawnedObjectiveBlock.GetComponent<RichPassChallengesTypeBlock>();
                challengesTypeBlock.init(newMissions[i].objectives[j], false);
                challengesTypeBlock.setMasks(false);
                Material mat = new Material(challengesTypeBlock.gameUITexture.material);
                mat.mainTexture = downloadedTextures[totalMissionsDisplaying];
                challengesTypeBlock.gameUITexture.material = mat;

                totalMissionsDisplaying++;
            }
        }

        challengesGrid.reposition();
        
        ctaButton.registerEventDelegate(ctaClicked);
        StatsRichPass.logNewSeasonalChallenges("view");
    }

    private void onFeatureExpire(Dict arg = null, GameTimerRange caller = null)
    {
        Dialog.close();
    }

    private void ctaClicked(Dict args = null)
    {
        Audio.play("ButtonConfirm");
        StatsRichPass.logNewSeasonalChallenges("close");
        RichPassFeatureDialog.showDialog(CampaignDirector.richPass, SchedulerPriority.PriorityType.HIGH);
        Dialog.close();
    }
    
    public override void close()
    {
        if (CampaignDirector.richPass != null)
        {
            CampaignDirector.richPass.timerRange.removeFunction(onFeatureExpire);
        }
    }

    public static bool showDialog(int challengesUnlockTimestamp, string motdKey = "")
    {
        if (challengesUnlockTimestamp < 0)
        {
            challengesUnlockTimestamp = CampaignDirector.richPass.getLatestSeasonalDate();
        }
        
        List<Mission> newMissions = CampaignDirector.richPass.seasonMissions[challengesUnlockTimestamp];
        List<string> gameOptionImages = new List<string>();
        for (int i = 0; i < newMissions.Count; i++)
        {
            for (int j = 0; j < newMissions[i].objectives.Count; j++)
            {
                Objective objective = newMissions[i].objectives[j];
                string gameKey = objective.game;
                if (!string.IsNullOrEmpty(objective.game))
                {
                    LobbyGame gameInfo = null;
                    gameInfo = LobbyGame.find(gameKey);
                    if (gameInfo != null)
                    {
                        gameOptionImages.Add(SlotResourceMap.getLobbyImagePath(gameInfo.groupInfo.keyName, gameInfo.keyName));
                    }
                }
                else if (objective.type == Objective.PACKS_COLLECTED || objective.type == Objective.CARDS_COLLECTED)
                { 
                    gameOptionImages.Add("robust_challenges/collections pack challenge card 1x1");
                }
                else if (Objective.addGameOptionImage(objective, gameOptionImages))
                {
                    //Objective.addGameOptionImage will load a new path into gameOptionImages if a match was found, otherwise it will return false.
                }
                else
                {
                    gameOptionImages.Add("robust_challenges/generic hir win challenge card 1x1");
                }
            }
        }
        
        Dialog.instance.showDialogAfterDownloadingTextures("rich_pass_new_seasonal_challenges_dialog", null, Dict.create(D.DATA, challengesUnlockTimestamp, D.MOTD_KEY, motdKey), nonMappedBundledTextures:gameOptionImages.ToArray());
        return true;
    }
    
    public override void onCloseButtonClicked(Dict args = null)
    {
        base.onCloseButtonClicked(args);
        Audio.play("ButtonConfirm");
        StatsRichPass.logNewSeasonalChallenges("close");
    }
}
