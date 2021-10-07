using System.Collections.Generic;
using UnityEngine;

public class RichPassSeasonalChallengesDisplay : MonoBehaviour
{
    [SerializeField] private UICenteredGrid challengesGrid;

    [SerializeField] private GameObject challengesIconPrefab;
    public SlideController challengesSlider;
    private List<RichPassChallengesTypeBlock> activeChallengesList;
    private List<RichPassChallengesTypeBlock> inactiveChallengesList;
    private int totalMissionsDisplaying = 0;
    private int incompleteChallengeIndex = -1;

    public void init(SortedDictionary<int, List<Mission>> unlockedMissionsToDisplay, Texture2D[] downloadedTextures, SortedDictionary<int, int> lockedSeasonMissions)
    {
        //Load the unlocked challenges
        setupUnlockedChallenges(unlockedMissionsToDisplay, downloadedTextures);

        //Load the locked challenges
        setupLockedChallenges(lockedSeasonMissions);
        
        challengesGrid.reposition();
        int totalRows = Mathf.CeilToInt((float)totalMissionsDisplaying / (float)challengesGrid.maxPerLine);
        challengesSlider.setBounds(challengesGrid.cellHeight * (totalRows - 1),  0); //-1 to prevent scrolling offscreen

        goToInProgressRow(incompleteChallengeIndex);
    }

    private void setupUnlockedChallenges(SortedDictionary<int, List<Mission>> unlockedMissionsToDisplay, Texture2D[] downloadedTextures)
    {
        activeChallengesList = new List<RichPassChallengesTypeBlock>();

        int textureOffset = 0;
        if (RichPassCampaign.goldGameKeys.Count > 0)
        {
            textureOffset++;
        }
        foreach (KeyValuePair<int, List<Mission>> kvp in unlockedMissionsToDisplay)
        {
            for (int i = 0; i < kvp.Value.Count; i++)
            {
                for (int j = 0; j < kvp.Value[i].objectives.Count; j++)
                {
                    GameObject spawnedObjectiveBlock = NGUITools.AddChild(challengesGrid.transform, challengesIconPrefab);
                    RichPassChallengesTypeBlock challengesTypeBlock = spawnedObjectiveBlock.GetComponent<RichPassChallengesTypeBlock>();
                    challengesTypeBlock.init(kvp.Value[i].objectives[j], false);
                    challengesTypeBlock.setMasks(true);
                    if (!kvp.Value[i].objectives[j].isComplete && !string.IsNullOrEmpty(kvp.Value[i].objectives[j].game) && (GameState.game == null || GameState.game.keyName != kvp.Value[i].objectives[j].game))
                    {
                        challengesTypeBlock.setViewState("main_feature");
                    }

                    Material mat = new Material(challengesTypeBlock.gameUITexture.material);

                    if (totalMissionsDisplaying + textureOffset < downloadedTextures.Length)
                    {
                        mat.mainTexture = downloadedTextures[totalMissionsDisplaying + textureOffset];
                    }

                    challengesTypeBlock.gameUITexture.material = mat;
                    challengesTypeBlock.gameUITexture.color = kvp.Value[i].objectives[j].isComplete ? Color.grey : Color.white;
                    challengesTypeBlock.newBadge.SetActive(CampaignDirector.richPass.hasNewChallenges && kvp.Key == CampaignDirector.richPass.getLatestSeasonalDate());
                    activeChallengesList.Add(challengesTypeBlock);
                    if (incompleteChallengeIndex < 0 && !kvp.Value[i].objectives[j].isComplete)
                    {
                        incompleteChallengeIndex = totalMissionsDisplaying;
                    }
                    totalMissionsDisplaying++;
                }
            }
        }
    }

    private void setupLockedChallenges(SortedDictionary<int, int> lockedSeasonMissions)
    {
        inactiveChallengesList = new List<RichPassChallengesTypeBlock>();
        foreach (KeyValuePair<int, int> kvp in lockedSeasonMissions)
        {
            System.DateTime unlockTime = Common.convertTimestampToDatetime(kvp.Key);

            for (int i = 0; i < kvp.Value; i++)
            {
                GameObject spawnedObjectiveBlock = NGUITools.AddChild(challengesGrid.transform, challengesIconPrefab);
                RichPassChallengesTypeBlock challengesTypeBlock = spawnedObjectiveBlock.GetComponent<RichPassChallengesTypeBlock>();
                challengesTypeBlock.initLocked(unlockTime, kvp.Value/2 == i);
                challengesTypeBlock.setMasks(true);
                inactiveChallengesList.Add(challengesTypeBlock);
                totalMissionsDisplaying++;
            }
        }
    }

    private void goToInProgressRow(int challengesIndex)
    {
        if (challengesIndex < 0)
        {
            return; //Stay in the default position if all the challenges are complete
        }
        int desiredRow = challengesIndex / challengesGrid.maxPerLine;

        challengesSlider.safleySetYLocation(challengesGrid.cellHeight * desiredRow);
    }

    public void upgradeToGold()
    {
        for (int i = 0; i < activeChallengesList.Count; i++)
        {
            if (activeChallengesList[i].displayObjective.type == Objective.PURCHASE_COINS)
            {
                activeChallengesList[i].init(activeChallengesList[i].displayObjective,false);
            }
            activeChallengesList[i].setBlockState("gold", true);
        }
        
        for (int i = 0; i < inactiveChallengesList.Count; i++)
        {
            inactiveChallengesList[i].setBlockState("gold", false);
        }
    }
}
