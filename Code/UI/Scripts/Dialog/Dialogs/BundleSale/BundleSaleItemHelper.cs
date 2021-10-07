using UnityEngine;

namespace Code.UI.Scripts.Dialog.Dialogs.BundleSale
{
    public class BundleSaleItemHelper : TICoroutineMonoBehaviour
    {
        [SerializeField] private LabelWrapperComponent title;
        [SerializeField] private LabelWrapperComponent duration;

        public void setText(string titleText, int timeInSeconds)
        {
            //set the title
            SafeSet.labelText(title.labelWrapper, titleText);
            
            //set duration text
            System.TimeSpan t = System.TimeSpan.FromSeconds(timeInSeconds);
            SafeSet.labelText(duration.labelWrapper, CommonText.formatTimeSpan(t));
        }
    }
}