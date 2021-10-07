using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Com.Rewardables;

public class PrizePopDialogOverlayExtraPicks : PrizePopDialogOverlay
{
    [SerializeField] private LabelWrapperComponent extraPicksLabel;

    public override void init(Rewardable reward, DialogBase parent, Dict overlayArgs = null)
    {
        base.init(reward, parent, overlayArgs);
        RewardPrizePopPicks extraPicksReward = reward as RewardPrizePopPicks;
        extraPicksLabel.text = string.Format("You Found {0} Extra Chances", CommonText.formatNumber(extraPicksReward.extraPicks));
    }

    protected override void ctaClicked(Dict args = null)
    {
        Audio.play(COLLECT_CLICKED_AUDIO_KEY);
        PrizePopDialog.instance.StartCoroutine(PrizePopDialog.instance.addPicksAndContinueBonusGame());
        base.ctaClicked(args);
    }
}
