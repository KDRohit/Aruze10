using UnityEngine;
using System.Collections;
using TMPro;

/*
  Attached to the parent dialog to handle setting up the dialog and handling clicks.
*/

public class BuyCreditsDialogV4HIR : BuyCreditsDialogNewHIR
{
	protected override void setVIPBonusLabel (VIPLevel level)
	{
		double mult = ((double)level.purchaseBonusPct + 100) / 100;
		mult = System.Math.Truncate(mult * 10f) / 10f;
		vipPercentBonusLabel.text = Localize.textUpper("{0}x_bonus", mult);
	}
		
	public static new void resetStaticClassData()
	{
		hasSeenSale = false;
	}
}
