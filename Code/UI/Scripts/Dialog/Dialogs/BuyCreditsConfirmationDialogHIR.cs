using UnityEngine;
using System.Collections;
using TMPro;
/**
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
**/

public class BuyCreditsConfirmationDialogHIR : BuyCreditsConfirmationDialog
{
	public TextMeshPro messageLabel;
	public GameObject creditsBonusName;	// Description text that is next to the bonus label.
	public TextMeshPro creditsBonusLabel;	// Bonus Credits Label.
	public GameObject okButton;
	public GameObject creditSweepstakesElements;

	/// Initialization
	public override void init()
	{
		base.init();
		
		creditsLabel.text = CreditsEconomy.convertCredits(SlotsPlayer.creditAmount);
		messageLabel.text = Localize.text("purchased_credits_{0}", CreditsEconomy.convertCredits(totalCredits));

		if (creditsBonusLabel != null)
		{
			if (bonusCredits == 0)
			{
				// Hide bonus credits label if this is empty:
				creditsBonusLabel.gameObject.SetActive(false);
				creditsBonusName.SetActive(false);
			}
			else
			{
				creditsBonusLabel.text = CreditsEconomy.convertCredits(bonusCredits);
			}
		}

		if (vipPointsLabel != null)
		{
			vipPointsLabel.text = CommonText.formatNumber(vipPoints);
		}
		
		okButton.SetActive(!CreditSweepstakes.isActive);
		creditSweepstakesElements.SetActive(CreditSweepstakes.isActive);
	}
}
