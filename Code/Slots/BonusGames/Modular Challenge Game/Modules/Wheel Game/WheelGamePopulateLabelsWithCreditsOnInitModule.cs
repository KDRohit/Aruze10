using UnityEngine;

/**
 * A module for displaying the labelComponent of a wheel with credit values.
 */
public class WheelGamePopulateLabelsWithCreditsOnInitModule : WheelGamePopulateLabelsOnInitModule
{
    public bool shouldDisplayAbbreviatedCredits;
    public bool shouldDisplayVerticalCredits = true;

    protected override void populateLabel(LabelWrapperComponent label, ModularChallengeGameOutcomeEntry entry)
    {
        if (label == null)
        {
            return;
        }

        string creditText = "";
        //Determine how the text looks
        if (shouldDisplayAbbreviatedCredits)
        {
            creditText = CommonText.formatNumberAbbreviated(CreditsEconomy.multipliedCredits(entry.credits), shouldRoundUp: false);
        }
        else
        {
            creditText = CreditsEconomy.convertCredits(entry.credits, false);
        }

        label.text = shouldDisplayVerticalCredits ? CommonText.makeVertical(creditText) : creditText;
    }
}
