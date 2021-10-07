using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace FeatureOrchestrator
{
    public class GenericProgressComponentView : TICoroutineMonoBehaviour
    {
        [SerializeField] protected LabelWrapperComponent progressText;
        [SerializeField] protected ButtonHandler button;
        [SerializeField] protected UIMeterNGUI progressMeter;

        protected string originalProgressText;
        private ShowUIPrefab parentComponent;
        
        public virtual void setup(ShowUIPrefab parentComponent, Dict args)
        {
            this.parentComponent = parentComponent;
            if (args != null)
            {
                if (progressText != null)
                {
                    originalProgressText = Localize.text(args.getWithDefault(D.MESSAGE, "") as string);
                    progressText.text = originalProgressText;
                }

                if (button != null)
                {
                    button.registerEventDelegate(onButtonClicked, args);    
                }
            }

            if (progressMeter != null)
            {
                int current = (int) args.getWithDefault(D.AMOUNT, 0);
                int max = (int) args.getWithDefault(D.VALUE, 1);
                progressMeter.setState(current,max);
            }
        }

        private void onButtonClicked(Dict args = null)
        {
            if (parentComponent != null)
            {
                parentComponent.onClick("onClick");
            }
        }
    }
}