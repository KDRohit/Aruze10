using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LottoBlastFreeOutroOverlay : TICoroutineMonoBehaviour
{
    [SerializeField] ButtonHandler collectButtonHandler;
    
    [SerializeField] private TextMeshPro totalWinLabel;
    [SerializeField] private AnimationListController.AnimationInformationList outroAnimInfo;
    [SerializeField] private AnimationListController.AnimationInformationList introAnimInfo;

    void Start()
    {
        collectButtonHandler.registerEventDelegate(onCollectButtonClicked);
    }

    public void show()
    {
        StartCoroutine(AnimationListController.playListOfAnimationInformation(introAnimInfo));
        collectButtonHandler.enabled = true;
        totalWinLabel.text = CreditsEconomy.convertCredits(LottoBlastMinigameDialog.instance.getFreePayout());
    }

    public virtual void onCollectButtonClicked(Dict args = null)
    {
        collectButtonHandler.enabled = false;
        StartCoroutine(playOutro());
    }

    private IEnumerator playOutro()
    {
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(outroAnimInfo));
        LottoBlastMinigameDialog.instance.freeCollectButtonPressed();
    }

}
