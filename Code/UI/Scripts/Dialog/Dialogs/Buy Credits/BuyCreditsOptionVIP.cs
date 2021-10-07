using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
/**
Controls one of the options for buying credits.
This dialog also supports STUD-driven credits packages.
*/

public abstract class BuyCreditsOptionVIP : MonoBehaviour
{
	public int index;				// Which option of the six, indexed 0-5.
	public TextMeshPro totalCredits; // Base + Bonus + vipBonus
	public TextMeshPro cost; // Real $$$$
	public TextMeshPro vipBonus; // Amount Added percent because of vip level

	public TextMeshPro baseSaleCredits; // Base + Bonus + vipBonus when sale active
	public TextMeshPro vipSaleBonus; // Amount Added percent because of vip level when sale acgtive

	public UISprite background;   // Dynamically changed to indicate special offers.

	public CreditPackage creditPackage = null; // Used in the new payments system.
	
	public virtual void setIndex(int newIndex, BuyCreditsDialog dialog)
	{

	}

	public virtual TextMeshPro[] getTextMeshPros()
	{
		return new TextMeshPro[] {totalCredits, cost, vipBonus, baseSaleCredits, vipSaleBonus};
	}
}

