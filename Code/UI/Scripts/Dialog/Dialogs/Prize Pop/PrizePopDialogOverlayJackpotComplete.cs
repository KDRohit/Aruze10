using System.Collections;
using System.Collections.Generic;
using Com.Rewardables;
using PrizePop;
using UnityEngine;

public class PrizePopDialogOverlayJackpotComplete : PrizePopDialogOverlay
{
    [SerializeField] private LabelWrapperComponent[] jackpotLabels;
    [SerializeField] private LabelWrapperComponent grandTotalLabel;
    [SerializeField] private AnimatedParticleEffect coinParticle;
    [SerializeField] private UIAnchor coinAnchor;

    public override void init(Rewardable reward, DialogBase parent, Dict overlayArgs)
    {
        base.init(reward, parent, overlayArgs);
        long totalPrize = 0;
        for (int i = 0; i < PrizePopFeature.instance.previousJackpots.Count; i++)
        {
            jackpotLabels[i].text = CreditsEconomy.multiplyAndFormatNumberAbbreviated(PrizePopFeature.instance.previousJackpots[i], 2, false);
            totalPrize += PrizePopFeature.instance.previousJackpots[i];
        }
        
        RewardCoins coinReward = reward as RewardCoins;
        jackpotLabels[jackpotLabels.Length-1].text = CreditsEconomy.multiplyAndFormatNumberAbbreviated(coinReward.amount, 2, false);

        totalPrize += coinReward.amount;

        grandTotalLabel.text = CreditsEconomy.convertCredits(totalPrize);
        StartCoroutine(waitThenAnchorCoin());
    }

    private IEnumerator waitThenAnchorCoin()
    {
        while (coinAnchor.enabled)
        {
            yield return null;
        }
        
        coinAnchor.reposition();
    }
    
    protected override void ctaClicked(Dict args = null)
    {
        StartCoroutine(playOutro());
    }
    
    private IEnumerator playOutro()
    {
        StatsPrizePop.logOverlayClose(overlayType);
        Audio.play(COLLECT_CLICKED_AUDIO_KEY);
        yield return StartCoroutine(coinParticle.animateParticleEffect());
        Destroy(gameObject);
        PrizePopDialog.instance.onJackpotOverlayClosed();
    }
}
