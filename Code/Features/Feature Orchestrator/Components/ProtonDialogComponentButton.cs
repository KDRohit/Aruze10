using UnityEngine;

namespace FeatureOrchestrator
{
    public class ProtonDialogComponentButton : MonoBehaviour
    {
        [SerializeField] private ClickHandler button;
        [SerializeField] private TriggerEvent trigger;

        private BaseComponent parentComponent;
        private enum TriggerEvent
        {
            onCtaClick,
            onCloseClick,
            onCta2Click
        }

        public void registerToParentComponent(BaseComponent parent, bool shouldHandleClicks = true)
        {
            parentComponent = parent;
            if (shouldHandleClicks)
            {
	            button.registerEventDelegate(onClick);
            }
        }

        public void onClick(Dict args = null)
        {
            if (parentComponent != null)
            {
                parentComponent.onClick(trigger.ToString());
            }
        }
    }
}
