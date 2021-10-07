using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LottoBlastAreYouSureOverlay : MonoBehaviour
{
    [SerializeField] ButtonHandler closeButtonHandler;
    [SerializeField] ButtonHandler playButtonHandler;
    
    [SerializeField] private AnimationListController.AnimationInformationList animInfo;
    [SerializeField] private AudioListController.AudioInformationList audioInfo;
    [SerializeField] private AnimationListController.AnimationInformationList introAnimInfo;


    [SerializeField] private TextMeshPro coinAmountLabel;
    [SerializeField] private TextMeshPro footerLabel;

    void Start()
    {
        closeButtonHandler.registerEventDelegate(onCloseButtonClicked);
        playButtonHandler.registerEventDelegate(onPlayButtonClicked);
        StatsLottoBlast.logConfirmationDialogView();
    }

    public void show()
    {
        playButtonHandler.enabled = true;
        closeButtonHandler.enabled = true;
        coinAmountLabel.text = LottoBlastMinigameDialog.instance.potentialJackpotAmount;
        footerLabel.text = "Play for only " + LottoBlastMinigameDialog.instance.premiumBuyButtonText;
        StartCoroutine(AnimationListController.playListOfAnimationInformation(introAnimInfo));
    }
    public void hide()
    {
        StartCoroutine(AnimationListController.playListOfAnimationInformation(animInfo));
    }

    private void onCloseButtonClicked(Dict args = null)
    {
        playButtonHandler.enabled = false;
        closeButtonHandler.enabled = false;
        StatsLottoBlast.logConfirmationDialogClose();
        StartCoroutine(AudioListController.playListOfAudioInformation(audioInfo));
        Dialog.close(LottoBlastMinigameDialog.instance);
    }
    private void onPlayButtonClicked(Dict args = null)
    {
        playButtonHandler.enabled = false;
        closeButtonHandler.enabled = false;
        StartCoroutine(playOutro());
    }

    private IEnumerator playOutro()
    {
        StartCoroutine(AnimationListController.playListOfAnimationInformation(animInfo));
        
        LottoBlastMinigameDialog.instance.attemptPremiumGamePurchase();
        StatsLottoBlast.logBuyPremiumGame("lotto_blastare_you_sure_upgrade");

        yield return new WaitForSeconds(0.5f);
        gameObject.SetActive(false);
    }

}
