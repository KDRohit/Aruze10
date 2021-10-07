using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QFCBonusGamePresenter : BonusGamePresenter
{
	protected override void welcomeButtonClicked()
	{
		StatsManager.Instance.LogCount("dialog", "ptr_v2", "dialog_final_node", "", "spin", "click");
		base.welcomeButtonClicked();
	}
}
