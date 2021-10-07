using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

class LevelUpBonusMotdHIR : LevelUpBonusMotd
{
	public TextMeshPro[] numberLabels = null;

	
	public override void init()
	{
		base.init();
		List<string> numberLabelList = LevelUpBonus.getPatternList(numberLabels.Length);
		for (int i = 0; i < numberLabels.Length; i++)
		{
			numberLabels[i].text = numberLabelList[i];
		}

		patternLabel.text = Localize.textUpper(LevelUpBonus.patternKey + "_levels");
		patternShadowLabel.text = patternLabel.text;
		multiplierLabel.text = Localize.textUpper("win_{0}X", LevelUpBonus.multiplier);
		multiplierShadowlabel.text = multiplierLabel.text;
	}
}