using System.Collections;
using Com.Rewardables;
using PrizePop;
using UnityEngine;

public class PrizePopDialogOverlayJackpot : PrizePopDialogOverlay
{
    [SerializeField] private LabelWrapperComponent coinLabel;
    [SerializeField] private AnimatedParticleEffect coinParticle;
    [SerializeField] private UIAnchor coinAnchor;

    public override void init(Rewardable reward, DialogBase parent, Dict overlayArgs)
    {
        base.init(reward, parent, overlayArgs);
        RewardCoins coinReward = reward as RewardCoins;
        coinLabel.text = CreditsEconomy.convertCredits(coinReward.amount);
        PrizePopFeature.instance.previousJackpots.Add(coinReward.amount);
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

    private IEnumerator playOutro()
    {
        StatsPrizePop.logOverlayClose(overlayType);
        Audio.play(COLLECT_CLICKED_AUDIO_KEY);
        yield return StartCoroutine(coinParticle.animateParticleEffect());
        Destroy(gameObject);
        PrizePopDialog.instance.onJackpotOverlayClosed();
    }

    protected override void ctaClicked(Dict args = null)
    {
        StartCoroutine(playOutro());
    }
}
