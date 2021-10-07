using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

/**
* SuperFreespinsModule.cs
* Joel Gallant - 2017-09-06
* Originally created for ainsworth10 - Enchanted Island (Super Free Spins)
* When reaching a spin threshold, switch to the Super Free Spins skin & effects
*/
public class SuperFreespinsModule: SlotModule
{
    [SerializeField] private string superFreespinsAnimateAudio = "freespin_add_one_spin_animate";
    
    [Tooltip("Indexed list of animiations / audio to play when the target symbol lands on a specific reel stop (top down).")]
    [SerializeField] private AnimationListController.AnimationInformationList[] trailPositionAnimations;
    
    [SerializeField] private LabelWrapperComponent superSpinsLabel;
    [SerializeField] private AnimationListController.AnimationInformationList superFreeSpinsActivationAnimations;
    [SerializeField] private GameObject[] freeSpinsHierarchyList;
    [SerializeField] private GameObject[] superFreeSpinsHierarchyList;
	[SerializeField] private AnimatedParticleEffect superFreespinsAwardTrail; // particle trail for adding spins to the main panel
    [SerializeField] private float delayBeforeSpinPanelUpdate = 0.0f; // optional delay to synchronize spin panel update with animation / particle
    [SerializeField] private float delayBeforeSuperSpinStart = 0.0f; // optional delay to allow player to perceive updated spin count
    [SerializeField] private string superFreespinsPaytable = "";

    private bool superSpinsActive = false;
    private int _superSpinsRemaining = 0; // also serves as spins collected counter
    public int SuperSpinsRemaining
    {
        get { return _superSpinsRemaining; }
        set
        {
            _superSpinsRemaining = value;
            superSpinsLabel.text = _superSpinsRemaining.ToString();
        }
    }

    protected override void OnEnable()
    {
        SuperSpinsRemaining = 0;
        superSpinsActive = false;
        base.OnEnable();
    }
    
    // increase super spins counter if an outcome is found
    public override bool needsToExecuteOnReelsStoppedCallback()
    {
        return getSuperSpinIncreaseOutcome() != null;
    }

    public override IEnumerator executeOnReelsStoppedCallback()
    {
        yield return StartCoroutine(executeSuperSpinsIncreaseLogic());
    }
    
    // after the paylines, trigger the final super free spins bonus
    public override bool needsToExecuteAfterPaylines()
    {
        return shouldActivateSuperSpins() || superSpinsActive;
    }

    public override IEnumerator executeAfterPaylinesCallback(bool winsShown)
    {
        yield return StartCoroutine(executeSuperSpinsLogic());
    }

    private IEnumerator executeSuperSpinsLogic()
    {
        // if this is our first super spin, activate the features
        if (shouldActivateSuperSpins())
        {
            yield return StartCoroutine(activateSuperSpins());
        }
        
        // process each super spin
        if (SuperSpinsRemaining > 0)
        {
            _superSpinsRemaining--; // decrement private field to avoid label update            
        }
    }
    
    private bool shouldActivateSuperSpins()
    {        
        // when only super spins remain, activate the feature.
        if (reelGame.numberOfFreespinsRemaining == 0 && !superSpinsActive && _superSpinsRemaining > 0)
        {
            return true;
        }
        
        return false;
    }
    
    private IEnumerator activateSuperSpins()
    {
        // swap objects for new skin
        for (int i = 0; i < freeSpinsHierarchyList.Length; i++)
        {
            freeSpinsHierarchyList[i].SetActive(false);
        }
        for (int i = 0; i < superFreeSpinsHierarchyList.Length; i++)
        {
            superFreeSpinsHierarchyList[i].SetActive(true);
        }

        // play defined intro animations for super spins
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(superFreeSpinsActivationAnimations));

        yield return StartCoroutine(superFreespinsAwardTrail.animateParticleEffect());

        if (delayBeforeSpinPanelUpdate > 0.0f)
        {
            yield return new WaitForSeconds(delayBeforeSpinPanelUpdate);
        }
        
        reelGame.numberOfFreespinsRemaining = SuperSpinsRemaining;
        
        if (delayBeforeSuperSpinStart > 0.0f)
        {
            yield return new WaitForSeconds(delayBeforeSuperSpinStart);
        }
        
        superSpinsActive = true;
    }
    
    private SlotOutcome getSuperSpinIncreaseOutcome()
    {
        List<SlotOutcome> reevaluationsAsSlotOutcomes = reelGame.outcome.getReevaluationsAsSlotOutcomes();
        for (int i = 0; i < reevaluationsAsSlotOutcomes.Count; i++)
        {
            if (reevaluationsAsSlotOutcomes[i].getType() == "bonus_symbol_accumulation")
            {
                return reevaluationsAsSlotOutcomes[i];
            }
        }

        return null;
    }
    
    private IEnumerator executeSuperSpinsIncreaseLogic()
    {
        // Get the outcome data to determine the proper symbol to animate.
        SlotOutcome superSpinIncreaseOutcome = getSuperSpinIncreaseOutcome();
        Dictionary<int, string> superSpinIncreaseSymbols = superSpinIncreaseOutcome.getAnticipationSymbols();

        string targetSymbolName = "";
        
        // Iterate through symbols based on reevaluation data
        foreach (int reelID in superSpinIncreaseSymbols.Keys)
        {
            bool found = false;
            SlotReel reel = reelGame.engine.getSlotReelAt(reelID - 1);
            targetSymbolName = reel.getReplacedSymbolName(superSpinIncreaseSymbols[reel.reelID]);
            
            // TODO: extend this to support more than a single symbol.
        }

        // play the audio key associated with the symbol animation
        Audio.playSoundMapOrSoundKey(superFreespinsAnimateAudio);
        
        // Find, & store the super symbol
        SlotSymbol foundSymbol = null;
        List<SlotSymbol> visibleSymbols = ReelGame.activeGame.engine.getAllVisibleSymbols(); 
        for (int i = 0; i < visibleSymbols.Count; i++)
        {
            SlotSymbol symbol = visibleSymbols[i];
            if (symbol.serverName == targetSymbolName)
            {
                foundSymbol = symbol;
            }
        }
        
        // Fly to the meter with an appropriate animation based on symbol position
        if (trailPositionAnimations.Length > foundSymbol.visibleSymbolIndex)
        {
            yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(trailPositionAnimations[foundSymbol.visibleSymbolIndex]));
        }
        else
        {
            Debug.LogError("Visible symbol index: " + foundSymbol.visibleSymbolIndex + " is out of range (" + trailPositionAnimations.Length + ")");
        }

        SuperSpinsRemaining += 1;
        
        // play the ship animation on the symbol after the presentation (HIR-51525)
        yield return StartCoroutine(foundSymbol.playAndWaitForAnimateOutcome());   
    }
}
