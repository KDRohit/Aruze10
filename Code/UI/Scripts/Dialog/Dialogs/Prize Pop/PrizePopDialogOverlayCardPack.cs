using System.Collections;
using System.Collections.Generic;
using Com.Rewardables;
using UnityEngine;

public class PrizePopDialogOverlayCardPack : PrizePopDialogOverlay
{
    [SerializeField] private Transform packParent;
    
    private CommonCardPack cardPack;
    private RewardCardPack cardPackReward;
    public override void init(Rewardable reward, DialogBase parent, Dict overlayArgs)
    {
        base.init(reward, parent, overlayArgs);
        cardPackReward = reward as RewardCardPack;
        CommonCardPack.loadCardPack(this, packLoadSuccess, packLoadFailed);
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
            GameObject packObj = NGUITools.AddChild(packParent, gameObj);
            cardPack = packObj.GetComponent<CommonCardPack>();
            if (cardPack != null)
            {
                cardPack.init(cardPackReward.packKey, true);    
            }
            else
            {
                Debug.LogWarning("No card back component");
            }
        }
    }
    
    protected override void ctaClicked(Dict args = null)
    {
        StatsPrizePop.logOverlayClose(overlayType);
        Audio.play(COLLECT_CLICKED_AUDIO_KEY);
        Dialog.close();
    }
}
