using System;
using System.Collections;
using System.Collections.Generic;
using Com.HitItRich.Feature.VirtualPets;
using Com.Scheduler;
using UnityEngine;

public class VirtualPetRespinOverlayDialog : DialogBase
{
    [SerializeField] private VirtualPet pet;
    [SerializeField] private LabelWrapperComponent coinLabel;

    public static VirtualPetRespinOverlayDialog instance { get; private set; }

    public override void init()
    {
        if (instance != null)
        {
#if UNITY_EDITOR
            //Log warning just for the editor. Needs to be investigated & fixed but isn't game breaking
            Debug.LogWarning("Duplicate Pet Respin Overlays. Shouldn't happen. Verify previous overlays are closing properly");
            Debug.Break();
#endif
            
            //Track this just in splunk event.
            //Silently closing the existing overlay to prevent it from getting stuck in the dialog stack.
            Userflows.addExtraFieldToFlow(userflowKey, "dupe_respin_overlay", "true");
            Dialog.close(instance);    
        }
        
        instance = this;
        
        SpinPanel.hir.turnOnPetsSpinButton(VirtualPetSpinButton.TrickMode.RESPIN);
    }

    public IEnumerator awardCoins(long amount, bool didFakeSpin)
    {
        yield return StartCoroutine(pet.playRespinBonusAnimations(didFakeSpin));

        if (SlotBaseGame.instance != null)
        {
            SlotBaseGame.instance.addCreditsToSlotsPlayer(amount, "pet_respin_bonus", shouldPlayCreditsRollupSound:false);
            yield return StartCoroutine(SlotBaseGame.instance.rollupCredits(amount, ReelGame.activeGame.onPayoutRollup, true, allowBigWin:false, shouldSkipOnTouch:false));
        }
        else
        {
            SlotsPlayer.addNonpendingFeatureCredits(amount, "pet_respin_bonus");
        }

        winAmount = amount;
        long multipliedWinAmount = CreditsEconomy.multipliedCredits(amount);
        string gameName = GameState.game != null ? GameState.game.keyName : "";
        if (!didFakeSpin)
        {
            StatsManager.Instance.LogCount("popup", "pet", "got_respin_replacement", gameName,  multipliedWinAmount.ToString(), "view",VirtualPetsFeature.instance.currentEnergy,VirtualPetsFeature.instance.isHyper ? "hyper_on" : "hyper_off");
        }
        else
        {
            StatsManager.Instance.LogCount("popup", "pet", "got_respin", gameName,  multipliedWinAmount.ToString(), "view",VirtualPetsFeature.instance.currentEnergy,VirtualPetsFeature.instance.isHyper ? "hyper_on" : "hyper_off");    
        }
        

        yield return StartCoroutine(pet.playRespinOutroAnimations(didFakeSpin));
        
        Dialog.close();
    }

    public IEnumerator playRespinAnimations()
    {
        yield return StartCoroutine(pet.playRespinIntroAnimations());
    }

    public IEnumerator playNoRespinAnimations()
    {
        yield return StartCoroutine(pet.playNoRespinIntroAnimations());
    }
    
    public IEnumerator playPaylinesCelebration()
    {
        yield return StartCoroutine(pet.playCelebration());
    }

    public override void close()
    {
        instance = null;
        
        //Go back to the normal spin button or hyper mode
        if (VirtualPetsFeature.instance.isHyper)
        {
            SpinPanel.hir.turnOnPetsSpinButton(SlotBaseGame.instance.hasAutoSpinsRemaining ? VirtualPetSpinButton.TrickMode.HYPER_AUTO : VirtualPetSpinButton.TrickMode.HYPER);
        }
        else
        {
            SpinPanel.hir.turnOffPetsSpinButton();
        }
    }

    public static void showDialog()
    {
        Scheduler.addDialog("virtual_pets_respin_dialog", Dict.create(D.SHROUD, false),SchedulerPriority.PriorityType.IMMEDIATE);
    }

    public void OnDestroy()
    {
        instance = null;
    }
}