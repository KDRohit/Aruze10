using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlotBasicProperty : SlotModule
{
    // A slot module that sets slot basic properties data.
    //
    // ( Migrating these data from previously SCAT to client as
    // this shortens the feedback loop of finding the ideal values of these slot properties.
    // And gives artists control over correctly setting these values early on during games assembly ) 
    //
    // Author : Xueer Zhu <xzhu@zynga.com>
    // Date : Jan 21st, 2021
    //

    [Tooltip("Reel delay > 0")]
    [SerializeField] private float reelDelay = 0f;
    [Tooltip("(symbols/second), > 0")]
    [SerializeField] private float spinSpeed = 10f;  
    [Tooltip("(symbols/second), > 0")]
    [SerializeField] private float autoSpinSpeed = 13f;  
    [Tooltip("(relative to symbol height)")]
    [Range(0,1)]
    [SerializeField] private float reelStopHeight = 0.416f;  
    [Tooltip("(relative to symbol height)")]
    [Range(0,1)]
    [SerializeField] private float rollbackHeight = 0.333f;  
    [Tooltip("(symbols/second)")]
    [Range(0,1)]
    [SerializeField] private float beginRollbackSpeed = 0.15f;   
    [Tooltip("(symbols/second)")]
    [Range(0,1)]
    [SerializeField] private float endRollbackSpeed = 0.75f;  
    
    public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
    {
        return true;
    }
    
    public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
    {
        setSlotBasicProperty();
    }
    
    private void setSlotBasicProperty()
    {
        SlotGameData currentSlotGameData = SlotBaseGame.instance.slotGameData;
        currentSlotGameData.reelDelay = reelDelay;
        currentSlotGameData.beginRollbackSpeed = beginRollbackSpeed;
        currentSlotGameData.endRollbackSpeed = endRollbackSpeed;
        
        currentSlotGameData.spinMovementNormal = spinMovementNormal(spinSpeed);
        currentSlotGameData.spinMovementAutospin = spinMovementAutospin(autoSpinSpeed);
        currentSlotGameData.reelStopAmount = reelStopAmount(reelStopHeight);
        currentSlotGameData.rollbackAmount = rollbackAmount(rollbackHeight);
    }
    
    private float spinMovementNormal(float spinSpeed)
    {
        return spinSpeed;
    }

    private float spinMovementAutospin(float autoSpinSpeed)
    {
        return autoSpinSpeed;
    }

    private float reelStopAmount(float reelStopHeight)
    {
        return reelStopHeight;
    }
    private float rollbackAmount(float rollbackHeight)
    {
        return rollbackHeight;
    }

    // Forces SlotGameData related properties to update, used to test the values in runtime
    public void forceUpdate()
    {
        setSlotBasicProperty();
    }
}
