
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VirtalPetFeedTreatAnimation : TICoroutineMonoBehaviour
{
    [SerializeField] private AnimationListController.AnimationInformationList treatIntro;
    [SerializeField] private AnimationListController.AnimationInformationList treatOutro;
    [SerializeField] private AnimationListController.AnimationInformationList treatUsed;
    [SerializeField] private AnimationListController.AnimationInformationList treatOff;
    [SerializeField] private AnimationListController.AnimationInformationList treatNotAvailable;
    
    [SerializeField] private string treatTextIntroLocKey = "";
    [SerializeField] private string treatTextOutroLocKey = "";
    [SerializeField] private LabelWrapperComponent treatTextField;

    public string textIntroLocKey
    {
        get {return treatTextIntroLocKey;}
        set { treatTextIntroLocKey = value; }

    }
    public string textOutroLocKey
    {
        get { return treatTextOutroLocKey; }
        set { treatTextOutroLocKey = value; }
    }
    
    public IEnumerator treatIntroAnimation()
    {
        if (treatTextIntroLocKey != "" && treatTextField != null)
        {
            SafeSet.labelText(treatTextField.labelWrapper, Localize.text(treatTextIntroLocKey));
          
        }
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(treatIntro));
    }
    public IEnumerator treatOutroAnimation()
    {
        if (treatTextOutroLocKey != "" && treatTextField != null)
        {
            SafeSet.labelText( treatTextField.labelWrapper, Localize.text(treatTextOutroLocKey));
        }
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(treatOutro));
    }

    public IEnumerator treatUsedAnimation()
    {
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(treatUsed));
    }
    
    public IEnumerator treatAvailableAnimation()
    {
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(treatOff));
    }
    public IEnumerator treatNotAvailableAnimation()
    {
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(treatNotAvailable));
    }
}
