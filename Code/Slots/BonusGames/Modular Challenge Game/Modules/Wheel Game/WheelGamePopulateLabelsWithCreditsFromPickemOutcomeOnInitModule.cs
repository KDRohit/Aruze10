using System.Collections.Generic;
using UnityEngine;

/*
 *  Used to get the "picks" JSON from a named outcome and use the credit values in to set label values
 *  Ensures the credit values from the picks are multiplied by GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers; 
 *
 *  Used By: billions02
 */
[AddComponentMenu("WheelGame/Init Modules/Wheel Labels/Populate Credits From Picks Outcome")]
public class WheelGamePopulateLabelsWithCreditsFromPickemOutcomeOnInitModule : WheelGameModule
{
    [SerializeField] protected string namedOutcome;
    [SerializeField] protected List<LabelWrapperComponent> wheelLabels;
    [SerializeField] protected bool makeLabelFontSizesEqual = true;
    public bool shouldDisplayAbbreviatedCredits;
    public bool shouldDisplayVerticalCredits = true;
    private JSON[] pickemPicks;
    private int labelEntryIndex = 0;
    
    public override void executeOnRoundInit(ModularWheelGameVariant roundParent, ModularWheel wheel)
    {
        labelEntryIndex = 0;
        SlotOutcome bonusGameOutcome = BonusGameManager.currentBonusGameOutcome;
        SlotOutcome slotOutcome = SlotOutcome.getBonusGameOutcome(bonusGameOutcome, namedOutcome);
			
        slotOutcome = slotOutcome.getSubOutcomesReadOnly()[0];
        pickemPicks = slotOutcome.getOutcomeJsonValue(JSON.getJsonArrayStatic, "picks");
        
        wheelRoundVariantParent = roundParent;
        wheelParent = wheel;

        if (roundParent == null)
        {
            Debug.LogError("WheelGameModule.executeOnRoundInit() - round was null, this is needed in order for modules to function!  Destroying this moudle.");
            Destroy(this);
        }

        if (wheel == null)
        {
            Debug.LogError("WheelGameModule.executeOnRoundInit() - wheel was null, this is needed in order for modules to function!  Destroying this moudle.");
            Destroy(this);
        }
        
        // generate an ordered outcome list from the wins & leftovers
        for(int i = 0, j = 0; i < wheelLabels.Count; j++)
        {
            LabelWrapperComponent wheelLabel = wheelLabels[i];
            if (wheelLabel == null)
            {
                continue;
            }
            
            JSON pickemPick = pickemPicks[j];
            long credits = pickemPick.getLong("credits", -1);

            // if we found a zero credit value, we won't put it on the wheel and use up a  
            // a slice label for it. This is used because we have things like BOOST_1 and BOOST_2 
            // in pick that have no slice label 
            if (credits == 0)
            {
                continue;
            }
        
            string creditText = "";
            
            credits = credits * GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;

            //Determine how the text looks
            creditText = shouldDisplayAbbreviatedCredits ? CommonText.formatNumberAbbreviated(CreditsEconomy.multipliedCredits(credits), shouldRoundUp: false) : CreditsEconomy.convertCredits(credits, false);
            wheelLabel.text = shouldDisplayVerticalCredits ? CommonText.makeVertical(creditText) : creditText;
            i++;
        }

        if (makeLabelFontSizesEqual)
        {
            CommonText.makeLabelFontSizesEqual(wheelLabels);
        }
    }
}
