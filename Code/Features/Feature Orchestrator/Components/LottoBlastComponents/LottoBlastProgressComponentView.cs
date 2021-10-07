using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FeatureOrchestrator
{
    public class LottoBlastProgressComponentView : GenericProgressComponentView
    {
        [SerializeField] protected LabelWrapperComponent durationText;
        [SerializeField] private AnimationListController.AnimationInformationList completeAnimInfo;

        private XPProgressCounter progressData;
        private string singleLevelLeftText;
        
        public override void setup(ShowUIPrefab parentComponent, Dict args)
        {
            base.setup(parentComponent, args);
            
            singleLevelLeftText = args.getWithDefault(D.OPTION, "") as string;
            progressData = args.getWithDefault(D.OPTION1, null) as XPProgressCounter;
            GameTimerRange timerRange = args.getWithDefault(D.TIME, null) as GameTimerRange;
            timerRange.registerLabel(durationText.tmProLabel, GameTimerRange.TimeFormat.REMAINING_HMS_FORMAT);
            progressText.text = getLocalizedProgressText();
            progressData.valueUpdated += onProgressDataValueUpdated;
        }

        private void onProgressDataValueUpdated()
        {
            progressText.text = getLocalizedProgressText();
        }

        private string getLocalizedProgressText()
        {
            if (progressData.levelsLeftToTarget == 1)
            {
                return Localize.text(singleLevelLeftText);
            }
            
            return Localize.text(originalProgressText, progressData.levelsLeftToTarget);
        }

        public void complete(bool playAnim)
        {
            if (playAnim)
            {
                StartCoroutine(completeRoutine());    
            }
            else
            {
                Destroy(this.gameObject);
            }
             
        }

        private IEnumerator completeRoutine()
        {
            yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(completeAnimInfo));
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            progressData.valueUpdated -= onProgressDataValueUpdated;
        }
    }
}
