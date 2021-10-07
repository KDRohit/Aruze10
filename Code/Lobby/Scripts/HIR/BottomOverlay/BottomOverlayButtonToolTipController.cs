using System.Collections;
using Com.Scheduler;
using UnityEngine;

public class BottomOverlayButtonToolTipController : TICoroutineMonoBehaviour
{
    [SerializeField] private GameObject locksParent;

    //Level Lock Components
    [SerializeField] private GameObject levelLockObject;
    [SerializeField] private LabelWrapperComponent levelLockLabel;

    //VIP Lock Components
    [SerializeField] private GameObject vipLockParent;
    [SerializeField] private VIPIconHandler lockedGemIcon;

    //Locked Tooltip Components
    [SerializeField] private AnimationListController.AnimationInformationList lockedToolTipAnimationList;
    [SerializeField] private LabelWrapperComponent lockedToolTipLabel;

    [SerializeField] private AnimationListController.AnimationInformationList unlockAnimationList;

    [SerializeField] private GameObject loadingParent;

    [SerializeField] private GameObject newBadge;

    private bool animationInProgress;
    private FeatureUnlockTask unlockTask;

    public const string COMING_SOON_LOC_KEY = "coming_soon";
    public const string SPIN_TO_UNLOCK = "spin_to_unlock";

    public IEnumerator playLockedTooltip()
    {
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(lockedToolTipAnimationList));
    }
    
    public void playLoadingTooltip()
    {
        loadingParent.SetActive(true);
        locksParent.SetActive(true);
        SafeSet.gameObjectActive(levelLockObject, false);
        SafeSet.gameObjectActive(vipLockParent, false);
    }
    
    public void stopLoadingTooltip()
    {
        SafeSet.gameObjectActive(locksParent, false);
        SafeSet.gameObjectActive(loadingParent, false);
    }

    public void initLevelLock(int level, string lockedLocalization, params object[] textArgs)
    {
        locksParent.SetActive(true);
        levelLockObject.SetActive(true);
        levelLockLabel.text = CommonText.formatNumber(level);
        setLockedText(lockedLocalization, textArgs);
    }

    public void initMysteryLock(string lockedLocalization, params object[] textArgs)
    {
        locksParent.SetActive(true);
        levelLockObject.SetActive(true);
        levelLockLabel.text = "?";
        setLockedText(lockedLocalization, textArgs);
    }

    public void initVipLock(int minVip, string lockedLocalization, params object[] textArgs)
    {
        locksParent.SetActive(true);
        vipLockParent.SetActive(true);
        lockedGemIcon.setLevel(minVip);
        setLockedText(lockedLocalization, textArgs);
    }

    public void setLockedText(string lockedLocalization, params object[] textArgs)
    {
        lockedToolTipLabel.text = Localize.text(lockedLocalization, textArgs);
    }

    public void startFeatureUnlockedPresentation(DialogBase.AnswerDelegate animFinishedDelegate, FeatureUnlockTask task, Dict args = null)
    {
        unlockTask = task;
        animationInProgress = true;
        StartCoroutine(playUnlockedAnimation(animFinishedDelegate, true, args));
    }

    private IEnumerator playUnlockedAnimation(DialogBase.AnswerDelegate animFinishedDelegate, bool turnOnNewBadge, Dict args = null)
    {
        while (Loading.isLoading)
        {
            yield return null;
        }
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(unlockAnimationList));
        if (turnOnNewBadge)
        {
            toggleNewBadge(true);
        }

        animationInProgress = false;
        animFinishedDelegate(args);
    }

    public void toggleNewBadge(bool enabled)
    {
        SafeSet.gameObjectActive(newBadge, enabled);
    }

    
    //Remove the task from the scheduler if it get cutoff mid-animation
    public void OnDestroy()
    {
        if (animationInProgress)
        {
            Scheduler.removeTask(unlockTask);
        }
    }

    public void OnDisabled()
    {
        if (animationInProgress)
        {
            Scheduler.removeTask(unlockTask);
        }
    }
}
