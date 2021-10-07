using System.Collections;
using UnityEngine;

public class LottoBlastBallRevealer : TICoroutineMonoBehaviour
{
    public LottoBlastBall lottoBall;
    [SerializeField] private AnimationListController.AnimationInformationList normalRevealAnimationList;
    [SerializeField] private AnimationListController.AnimationInformationList jackpotRevealAnimationList;

    [SerializeField] private AnimationListController.AnimationInformationList normalCelebrationAnimationList;
    [SerializeField] private AnimationListController.AnimationInformationList jackpotCelebrationAnimationList;

    public IEnumerator playReveal(bool isJackpot)
    {
        if (isJackpot)
        {
            yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotRevealAnimationList));
        }
        else
        {
            yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(normalRevealAnimationList));
        }
    }

    public IEnumerator playCelebration(bool isJackpot)
    {
        if (isJackpot)
        {
            yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotCelebrationAnimationList));
        }
        else
        {
            yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(normalCelebrationAnimationList));
        }
    }
}
