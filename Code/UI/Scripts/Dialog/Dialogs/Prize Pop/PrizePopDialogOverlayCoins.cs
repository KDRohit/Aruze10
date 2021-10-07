using System.Collections;
using Com.Rewardables;
using UnityEngine;

public class PrizePopDialogOverlayCoins : PrizePopDialogOverlay
{
    [SerializeField] private LabelWrapperComponent coinLabel;
    [SerializeField] private AnimatedParticleEffect coinParticle;
    [SerializeField] private UIAnchor coinAnchor;

    public override void init(Rewardable reward, DialogBase parent, Dict overlayArgs)
    {
        base.init(reward, parent, overlayArgs);
        RewardCoins coinReward = reward as RewardCoins;
        coinLabel.text = CreditsEconomy.convertCredits(coinReward.amount);
        StartCoroutine(waitThenAnchorCoin());
    }
    
    
    private IEnumerator waitThenAnchorCoin()
    {
        while (coinAnchor.enabled)
        {
            yield return null;
        }

        coinAnchor.enabled = true;
    }

    private IEnumerator playOutro()
    {
        StatsPrizePop.logOverlayClose(overlayType);
        Audio.play(COLLECT_CLICKED_AUDIO_KEY);
        yield return StartCoroutine(coinParticle.animateParticleEffect());
        Dialog.close();
    }

    protected override void ctaClicked(Dict args = null)
    {
        StartCoroutine(playOutro());
    }
}
