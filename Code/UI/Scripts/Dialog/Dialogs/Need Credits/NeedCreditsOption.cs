using UnityEngine;
using System.Collections;
using TMPro;

public class NeedCreditsOption : MonoBehaviour
{
	public TextMeshPro creditsLabel;
	public TextMeshPro vipPointsLabel;
	public TextMeshPro priceLabel;

	private CreditPackage creditPackage;
	private NeedCreditsMultiDialog dialog;

	public void init(CreditPackage creditPackage, NeedCreditsMultiDialog dialog)
	{
		this.dialog = dialog;
		this.creditPackage = creditPackage;
		if (creditPackage != null)
		{
			priceLabel.text = creditPackage.purchasePackage.getLocalizedPrice();
			vipPointsLabel.text = Localize.text("plus_{0}", CommonText.formatNumber(creditPackage.purchasePackage.vipPoints()));
			creditsLabel.text = CreditsEconomy.convertCredits(creditPackage.purchasePackage.totalCredits(creditPackage.bonus));
		}
		else
		{
			Debug.LogWarning("NeedCreditsOption -- creditPackage is null");
		}
	}

	public void buyButtonClicked()
	{
		dialog.makePurchase(creditPackage);
	}

}