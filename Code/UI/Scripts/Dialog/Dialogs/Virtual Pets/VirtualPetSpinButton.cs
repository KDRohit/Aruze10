using System.Collections;
using System.Collections.Generic;
using Com.HitItRich.Feature.VirtualPets;
using UnityEngine;

public class VirtualPetSpinButton : TICoroutineMonoBehaviour
{
    public LocalPositionSwap[] positionSwaps;
    
    [SerializeField] private LabelWrapperComponent hyperBonusLabel;
    [SerializeField] private GameObjectCycler hyperModeCycler;
    [SerializeField] private GameObjectCycler autoSpinhyperModeCycler;

    [SerializeField] private GameObject hyperModeParent;
    [SerializeField] private GameObject respinModeParent;
    [SerializeField] private MultiClickHandler clickHandler;
    private TrickMode currentMode = TrickMode.NONE;


    public const string PREFAB_PATH = "Features/Virtual Pets/Prefabs/Instanced Prefabs/Pets Spin Panel Button";
    
    public enum TrickMode
    {
        RESPIN,
        HYPER,
        HYPER_AUTO,
        NONE
    }

    public void init()
    {
        if (SpinPanel.instance == null)
        {
            //Self destroy if for some reason this is created when the spin panel doesn't exist
            Destroy(gameObject);
            return;
        }
        
        if (VirtualPetsFeature.instance == null)
        {
            if (Data.debugMode)
            {
                Debug.LogWarning("Trying to turn on pets button while feature is inactive. Turning off automatically");
            }
            SpinPanel.hir.turnOffPetsSpinButton();
            return;
        }
        
        if (VirtualPetRespinOverlayDialog.instance != null)
        {
            setTrickMode(TrickMode.RESPIN);
        }
        else if (VirtualPetsFeature.instance.isHyper)
        {
            setTrickMode(SlotBaseGame.instance != null && SlotBaseGame.instance.hasAutoSpinsRemaining ? TrickMode.HYPER_AUTO : TrickMode.HYPER);
        }
        else
        {
            //Turn off if neither mode should be active
            SpinPanel.hir.turnOffPetsSpinButton();
            return;
        }

        string spinPanelState = SpinPanel.instance.normalSpinPanelSwapper.getCurrentState();
        for (int i = 0; i < positionSwaps.Length; i++)
        {
            positionSwaps[i].swap(spinPanelState);
        }
    }

    private void setToHyperMode(bool autoSpinning)
    {
        respinModeParent.SetActive(false);
        hyperModeParent.SetActive(true);

        VirtualPetsFeature.instance.hyperTimer.registerLabel(hyperBonusLabel.tmProLabel, GameTimerRange.TimeFormat.REMAINING);

        //When not in auto-spin this acts as a SPIN button
        //When auto-spinning this is a STOP button
        if (!autoSpinning)
        {
            hyperModeCycler.startCycling(true);
            clickHandler.registerEventDelegate(ClickHandler.MouseEvent.OnHold, SpinPanel.hir.onSpinHold);
            clickHandler.registerEventDelegate(ClickHandler.MouseEvent.OnClick, SpinPanel.hir.clickSpinDelegate);
            clickHandler.holdTime = ExperimentWrapper.SpinPanelV2.autoSpinHoldDuration;

            if (LevelUpUserExperienceFeature.instance.isEnabled)
            {
                clickHandler.registerEventDelegate(ClickHandler.MouseEvent.OnClick, SpinPanel.hir.showLevelPercentOnClickSpin);
                clickHandler.registerEventDelegate(ClickHandler.MouseEvent.OnHold, SpinPanel.hir.showLevelPercentOnClickSpin);
            }
        }
        else
        {
            autoSpinhyperModeCycler.startCycling(true);
            clickHandler.registerEventDelegate(ClickHandler.MouseEvent.OnClick, SpinPanel.hir.clickStopDelegate);
        }
    }
    
    private void setToRespinMode()
    {
        hyperModeParent.SetActive(false);
        respinModeParent.SetActive(true);
        
        clickHandler.registerEventDelegate(ClickHandler.MouseEvent.OnClick, SpinPanel.hir.clickStopDelegate);
    }

    private void resetButton()
    {
        //Clear out any previoussly registered delegates when swapping from one mode to another
        switch (currentMode)
        {
            case TrickMode.HYPER:
                clickHandler.clearAllDelegates(ClickHandler.MouseEvent.OnHold);
                clickHandler.clearAllDelegates(ClickHandler.MouseEvent.OnClick);
                hyperModeCycler.stopCyclingImmediate();
                break;
            case TrickMode.RESPIN:
                clickHandler.clearAllDelegates(ClickHandler.MouseEvent.OnClick);
                break;
            case TrickMode.HYPER_AUTO:
                autoSpinhyperModeCycler.stopCyclingImmediate();
                clickHandler.clearAllDelegates(ClickHandler.MouseEvent.OnClick);
                break;
            default:
                break;
        }
    }

    public void setTrickMode(TrickMode newMode)
    {
        //Don't need to rerun setup code if mode isn't actually changing
        if (currentMode != newMode)
        {
            resetButton();
            switch (newMode)
            {
                case TrickMode.HYPER:
                    setToHyperMode(false);
                    break;
                case TrickMode.RESPIN:
                    setToRespinMode();
                    break;
                case TrickMode.HYPER_AUTO:
                    setToHyperMode(true);
                    break;
                default:
                    break;
            }
            currentMode = newMode;
        }
    }
}
