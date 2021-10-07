using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LottoBlastFreeIntroOverlay : MonoBehaviour
{
    [SerializeField] ButtonHandler playButtonHandler;
    
    [SerializeField] private TextMeshPro freeJackpotAmountLabel;

    [SerializeField] private AnimationListController.AnimationInformationList introAnimInfo;
    [SerializeField] private AnimationListController.AnimationInformationList outroAnimInfo;

    // Start is called before the first frame update
    void Start()
    {
        playButtonHandler.registerEventDelegate(onPlayButtonClicked);
    }

    public void show()
    {
        playButtonHandler.enabled = true;
        StartCoroutine(setFreeJackpotLabel());
        StartCoroutine(AnimationListController.playListOfAnimationInformation(introAnimInfo));
    }

    private IEnumerator setFreeJackpotLabel()
    {
        freeJackpotAmountLabel.text = "";
        yield return null; //We need to make sure the start method of the minigame dialog has run.
        yield return null;
        freeJackpotAmountLabel.text = CreditsEconomy.convertCredits(LottoBlastMinigameDialog.instance.getFreePotentialJackpotAmount());
    }

    private void onPlayButtonClicked(Dict args = null)
    {
        playButtonHandler.enabled = false;
        StartCoroutine(AnimationListController.playListOfAnimationInformation(outroAnimInfo));
        LottoBlastMinigameDialog.instance.freeStartButtonPressed();
    }

}
