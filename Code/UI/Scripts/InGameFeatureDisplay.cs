using UnityEngine;

public class InGameFeatureDisplay : MonoBehaviour
{
    // Start is called before the first frame update
    public virtual void onStartNextAutoSpin()
    {
    }
    public virtual void onStartNextSpin(long wager)
    {
    }

    public virtual void setButtonsEnabled(bool isEnabled)
    {
    }

    public virtual void onBetChanged(long newWager)
    {
    }

    public virtual void onHide()
    {
    }

    public virtual void onShow()
    {
    }
    
    public virtual void onSpinComplete()
    {
    }

    public virtual void refresh(Dict args)
    {
    }

    public virtual void init(Dict args = null)
    {
    }
}
