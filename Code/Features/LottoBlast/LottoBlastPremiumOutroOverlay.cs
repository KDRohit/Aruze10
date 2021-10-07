using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LottoBlastPremiumOutroOverlay : TICoroutineMonoBehaviour
{
    [SerializeField] ButtonHandler collectButtonHandler;
    
    [SerializeField] private AnimationListController.AnimationInformationList outroAnimInfo;
    [SerializeField] private AnimationListController.AnimationInformationList introAnimInfo;
    [SerializeField] private TextMeshPro totalWinLabel;

    // Start is called before the first frame update
    void Start()
    {
        collectButtonHandler.registerEventDelegate(onCollectButtonClicked);
    }

    public void show()
    {
        collectButtonHandler.enabled = true;
        StartCoroutine(AnimationListController.playListOfAnimationInformation(introAnimInfo));
        totalWinLabel.text = CreditsEconomy.convertCredits(LottoBlastMinigameDialog.instance.getPremiumPayout());
    }

    public virtual void onCollectButtonClicked(Dict args = null)
    {
        collectButtonHandler.enabled = false;
        StartCoroutine(playOutro());
    }

    private IEnumerator playOutro()
    {
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(outroAnimInfo));
        LottoBlastMinigameDialog.instance.premiumGameEnded();
    }

}
