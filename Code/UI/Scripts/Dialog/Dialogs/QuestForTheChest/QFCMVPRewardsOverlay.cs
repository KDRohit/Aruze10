using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Zynga.Core.Util;

namespace QuestForTheChest
{
    
    public class QFCMVPRewardsOverlay : TICoroutineMonoBehaviour
    {
        private const string KEY_AWARD_TEXT_KEY = "qfc_key_award";
        
        [SerializeField] private GameObject playerRewardsParent;
        
        [SerializeField] private TextMeshPro mvpWinnerCoinLabel;
        [SerializeField] private QFCMVPRewardsOverlaySubView mvpWinnerView;
        [SerializeField] private QFCMVPRewardsOverlaySubView mvpTeamWinnerView;
        [SerializeField] private QFCMVPRewardsOverlaySubView mvpOpponentWinnerView;
        [SerializeField] private TextMeshPro mvpNonWinnerCoinLabel;

        [SerializeField] private AnimationListController.AnimationInformationList playerMVPAnimations;
        [SerializeField] private AnimationListController.AnimationInformationList playerMVPOutroAnimations;
        
        [SerializeField] private AnimationListController.AnimationInformationList nonMVPAnimations;
        [SerializeField] private AnimationListController.AnimationInformationList nonMVPOutroAnimations;
        
        [SerializeField] private ButtonHandler buttonMVPCollect;
        
        // Player not as the MVP view
        [SerializeField] private ButtonHandler buttonNonMVPCollect;

        [SerializeField] private GameObject coinBurstPrefab;
        [SerializeField] private Transform coinBurstEndPosition;
        
        private Transform coinBurstStartPosition;
        
        private ClickHandler.onClickDelegate onClickHandler;
        private string eventId;
		
        private const string QFC_FINAL_KEY_COLLECT_FINAL_SOUND = "QfcFinalKeyCollectFinal";

        private bool isMVP = false;
        public void initMVPView(string id, QFCPlayer player, long coinAmount, QFCReward mvpReward, ClickHandler.onClickDelegate onClick, QFCMapDialog parentDialog)
        {
            isMVP = true;
            onClickHandler = onClick;
            eventId = id;
            gameObject.SetActive(true);
            coinBurstStartPosition = buttonMVPCollect.transform;
            buttonMVPCollect.registerEventDelegate(collectMVPClicked);
            mvpWinnerCoinLabel.text = Localize.text(KEY_AWARD_TEXT_KEY, CreditsEconomy.convertCredits(coinAmount));
            mvpWinnerView.init(player, QFCMapDialog.QFCBoardPlayerIconType.CURRENT_PLAYER, mvpReward, parentDialog);
            logView();
            StartCoroutine(AnimationListController.playListOfAnimationInformation(playerMVPAnimations));
        }
        
        public void initNonMVPView(string id, long coinAmount, QFCPlayer teamMVP, QFCPlayer opponentMVP, List<QFCReward> teamMVPRewards, List<QFCReward> opponentMVPRewards, ClickHandler.onClickDelegate onClick, QFCMapDialog parentDialog)
        {     
            isMVP = false;
            onClickHandler = onClick;
            eventId = id;
            coinBurstStartPosition = buttonNonMVPCollect.transform;
            buttonNonMVPCollect.registerEventDelegate(collectNonMVPClicked);
            
            if (coinAmount == 0)
            {
                // nothing to collect. Dont do anything here.
                if (onClickHandler != null)
                {
                    onClickHandler.Invoke(Dict.create(D.EVENT_ID, eventId));
                }
                return;
            }
            gameObject.SetActive(true);
            mvpNonWinnerCoinLabel.text = CreditsEconomy.convertCredits(coinAmount);
            playerRewardsParent.SetActive(true);
            
            if (teamMVP != null && teamMVPRewards != null && !teamMVPRewards.IsEmpty())
            {
                mvpTeamWinnerView.init(teamMVP, QFCMapDialog.QFCBoardPlayerIconType.HOME ,teamMVPRewards[0], parentDialog);
            }
            else
            {
                mvpTeamWinnerView.gameObject.SetActive(false);
            }

            mvpOpponentWinnerView.init(opponentMVP, QFCMapDialog.QFCBoardPlayerIconType.AWAY, opponentMVPRewards != null && !opponentMVPRewards.IsEmpty() ? opponentMVPRewards[0] : null, parentDialog);

            logView();
            StartCoroutine(playNonMVPAnimations());
        }

        IEnumerator playNonMVPAnimations()
        {
            yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(nonMVPAnimations));
            mvpTeamWinnerView.playCoinParticles();
            mvpOpponentWinnerView.playCoinParticles();
        }

        private void logView()
        {
            StatsManager.Instance.LogCount("dialog", "ptr_v2", "dialog_mvp_reward", "", isMVP ? "mvp" : "teammate",
                "view");
        }
        
        private void logClick()
        {
            StatsManager.Instance.LogCount("dialog", "ptr_v2", "dialog_mvp_reward", "", isMVP ? "mvp" : "teammate",
                "click");
        }

        private void collectMVPClicked (Dict args)
        {
            buttonMVPCollect.enabled = false;
            StartCoroutine(handleMVPCollectClick(args));
        }

        IEnumerator handleMVPCollectClick(Dict args)
        {
            yield return StartCoroutine(coinBurst());
            StartCoroutine(AnimationListController.playListOfAnimationInformation(playerMVPOutroAnimations));
            collectClicked(args);
        }

        private void collectNonMVPClicked (Dict args)
        {
            buttonNonMVPCollect.enabled = false;
            StartCoroutine(handleNonMVPCollectClick(args));
        }

        IEnumerator handleNonMVPCollectClick(Dict args)
        {
            yield return StartCoroutine(coinBurst());
            StartCoroutine(AnimationListController.playListOfAnimationInformation(nonMVPOutroAnimations)); 
            collectClicked(args);
        }
        
        private void collectClicked(Dict args)
        {
            Audio.play(QFC_FINAL_KEY_COLLECT_FINAL_SOUND + ExperimentWrapper.QuestForTheChest.theme);
            buttonMVPCollect.unregisterEventDelegate(collectClicked);
            buttonNonMVPCollect.unregisterEventDelegate(collectClicked);
            logClick();
            if (onClickHandler != null)
            {
                onClickHandler.Invoke(Dict.create(D.EVENT_ID, eventId));
            }
        }
        
        protected IEnumerator coinBurst()
        {
            GameObject obj = NGUITools.AddChild(coinBurstStartPosition, coinBurstPrefab);
            if (obj != null)
            {
                QFCCoinBurst burst = obj.GetComponent<QFCCoinBurst>();
                if (burst != null)
                {
                    burst.setTarget(coinBurstEndPosition);
                } 
                yield return StartCoroutine(burst.playBurstAnimation());
                Destroy(obj);
            }
        }
        
    }
}