using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomFreespinBonusSummaryDialog : SlotModule 
{
	public override bool needsToCreateCustomSummaryScreenDialog ()
	{
		return true;
	}

	public override void createCustomSummaryScreenDialog (GenericDelegate answerDelegate)
	{
#if ZYNGA_KINDLE
		VIPBonusSummaryDialogKindle.showCustomDialog(
			Dict.create(
				D.CALLBACK, new DialogBase.AnswerDelegate((noArgs) => { answerDelegate(); })
			)
		);
#else
		VIPBonusSummaryDialog.showCustomDialog(
			Dict.create(
				D.CALLBACK, new DialogBase.AnswerDelegate((noArgs) => { answerDelegate(); })
			)
		);
#endif
	}
}
