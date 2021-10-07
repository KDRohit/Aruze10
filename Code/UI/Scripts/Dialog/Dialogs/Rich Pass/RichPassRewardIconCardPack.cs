using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

public class RichPassRewardIconCardPack : RichPassRewardIcon
{
    [SerializeField] private Transform packParent;
    private CommonCardPack cardPack;
    public override void init(PassReward rewardToAward, RichPassCampaign.RewardTrack tier, SlideController slideController)
    {
        base.init(rewardToAward, tier, slideController);
        CommonCardPack.loadCardPack(this, packLoadSuccess, packLoadFailed);
    }

    public override IEnumerator playAnticipations()
    {
        if (cardPack != null)
        {
            cardPack.grayOutPack();
        }

        yield return base.playAnticipations();
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
            GameObject packObj = NGUITools.AddChild(packParent, obj as GameObject);
            cardPack = packObj.GetComponent<CommonCardPack>();
            if (cardPack != null)
            {
                cardPack.init(reward.cardPackKey, true, reward.claimed);    
            }
            else
            {
                Debug.LogWarning("No card back component");
            }
        }
        
    }
}
