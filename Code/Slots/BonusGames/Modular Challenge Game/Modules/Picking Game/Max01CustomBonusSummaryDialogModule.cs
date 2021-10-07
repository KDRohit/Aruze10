using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Max01CustomBonusSummaryDialogModule : ChallengeGameModule
{
	public override bool needsToShowCustomBonusSummaryDialog ()
	{
		return true;
	}

	public override void createCustomSummaryScreenDialog (GenericDelegate answerDelegate)
	{
		MaxVoltageBonusSummaryDialog.showCustomDialog(
			Dict.create(
				D.CALLBACK, new DialogBase.AnswerDelegate((noArgs) => { answerDelegate(); })
			)
		);
	}
}
