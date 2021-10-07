using System.Collections;
using System.Collections.Generic;
using QuestForTheChest;
using TMPro;
using UnityEngine;

public class QFCMVPRewardsOverlaySubView : MonoBehaviour
{
    private const string COLLECTIBLE_POINT_STR = "qfc_collectible_packs";
    private const string ELITE_POINT_STR = "qfc_elite_points";
    private const string NO_MVP = "qfc_no_mvp";

    [SerializeField] private QFCPlayerPortrait playerPortrait;

    [SerializeField] private GameObject coinsRewardParent;
    [SerializeField] private MultiLabelWrapperComponent coinRewardLabel;

    [SerializeField] private GameObject collectionsRewardParent;
    [SerializeField] private MultiLabelWrapperComponent collectionsRewardLabel;
    [SerializeField] private Transform collectionItemAnchor;
    [SerializeField] private GameObject[] collectionStars;

    [SerializeField] private GameObject elitePointsRewardParent;
    [SerializeField] private MultiLabelWrapperComponent elitePointsRewardLabel;
    [SerializeField] private Transform eliteLogoAnchor;
    [SerializeField] private GameObject eliteLogo;
    
    [SerializeField] private GameObject noMVPParent;
    [SerializeField] private MultiLabelWrapperComponent noMVPLabel;
    
    [SerializeField] private AnimatedParticleEffect coinAnimParticles;
    
    public void init(QFCPlayer player, QFCMapDialog.QFCBoardPlayerIconType playerType, QFCReward mvpReward, QFCMapDialog parentDialog)
    {
        if (player == null)
        {
            playerPortrait.init(playerType, null, 0, false, false);
            noMVPParent.SetActive(true);
            noMVPLabel.text = Localize.text(NO_MVP);
        }
        else
        {
            //Need to grab the current amount of keys from the dialog again since they were just updated from the chest opening sequence but we have old key data from when this event was originally created
            int updatedKeysAmount = parentDialog.zidsToPortraitsDict[player.member.zId].currentKeys;

            playerPortrait.init(playerType, player.member, updatedKeysAmount, true, player.keys != 0);
            switch (mvpReward.type)
            {
                case RewardCoins.TYPE:
                    coinsRewardParent.SetActive(true);
                    coinRewardLabel.text = CreditsEconomy.convertCredits(mvpReward.value);
                    break;
                case RewardCardPack.TYPE:
                    collectionsRewardParent.SetActive(true);
                    CommonCardPack.loadCardPack(this, packLoadSuccess, packLoadFailed,
                        Dict.create(D.OBJECT, collectionItemAnchor, D.KEY, mvpReward.packName, D.OPTION,
                            collectionsRewardLabel, D.OPTION1, collectionStars));
                    break;
                case RewardElitePassPoints.TYPE:
                    elitePointsRewardParent.SetActive(true);
                    NGUITools.AddChild(eliteLogoAnchor, eliteLogo);
                    elitePointsRewardLabel.text = Localize.text(ELITE_POINT_STR, mvpReward.value);
                    break;
                default:
                    Userflows.addExtraFieldToFlow(Dialog.instance.currentDialog.userflowKey, "invalid_qfc_reward",
                        mvpReward.type);
                    Bugsnag.LeaveBreadcrumb("Invalid QFC reward type");
                    break;
            }
        }
    }

    private void packLoadFailed(string assetPath, Dict data = null)
    {
        Debug.LogWarning("Failed to load card pack: " + assetPath);
    }

    private void packLoadSuccess(string assetPath, Object obj, Dict data = null)
    {
        if (this == null || this.gameObject == null)
        {
            return;
        }
        GameObject gameObj = obj as GameObject;
        if (gameObj != null)
        {
            GameObject packObj = NGUITools.AddChild(data.getWithDefault(D.OBJECT, null) as Transform, obj as GameObject);
            string rewardPackKey = data.getWithDefault(D.KEY,  "").ToString();
            CommonCardPack cardPack = packObj.GetComponent<CommonCardPack>();
            if (cardPack != null)
            {
                cardPack.init(rewardPackKey, false, false);
                CollectablePackData packData = Collectables.Instance.findPack(rewardPackKey);
                if (packData != null)
                {
                    MultiLabelWrapperComponent collectionsLabel = (MultiLabelWrapperComponent) data.getWithDefault(D.OPTION,  null);
                    collectionsLabel.text = Localize.text(COLLECTIBLE_POINT_STR, packData.constraints[0].guaranteedPicks);
                    GameObject[] stars = (GameObject[]) data.getWithDefault(D.OPTION1,  null);
                    stars[packData.constraints[0].minRarity].SetActive(true);
                }
            }
            else
            {
                Debug.LogWarning("No card pack component");
            }
        }
    }

    public void playCoinParticles()
    {
        if (coinsRewardParent.activeInHierarchy && coinAnimParticles != null) // play only if reward was coin
        {
            StartCoroutine(coinAnimParticles.animateParticleEffect());
        }
    }
}
