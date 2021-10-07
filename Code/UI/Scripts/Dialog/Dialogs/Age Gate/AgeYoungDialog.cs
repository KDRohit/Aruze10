using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

/**
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
**/

public class AgeYoungDialog : DialogBase
{

	public TextMeshPro messageLabel;
	public TextMeshPro underAgeLabel;
	public TextMeshPro overAgeLabel;
	
	public override void init()
	{
		//messageLabel.text = Localize.text("age_too_young_message_{0}", ExperimentWrapper.AgeGate.ageRequirment);
		//underAgeLabel.text = Localize.text("age_too_young_under_{0}", ExperimentWrapper.AgeGate.ageRequirment);
		//overAgeLabel.text = Localize.text("age_too_young_over_{0}", ExperimentWrapper.AgeGate.ageRequirment);
		messageLabel.text = Localize.text("age_too_young_message");
		underAgeLabel.text = Localize.text("age_too_young_under_21");
		overAgeLabel.text = Localize.text("age_too_young_over_21");
	}
	
	private void clickYes()
	{
		dialogArgs.merge(D.ANSWER, "underAge");
		Dialog.close();
	}
	
	private void clickNo()
	{
		dialogArgs.merge(D.ANSWER, "overAge");
		Dialog.close();
	}
	
	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup.
	}
	
	public static void showDialog(DialogBase.AnswerDelegate callback)
	{
		Scheduler.addDialog(
			"age_young", 
			Dict.create(
				D.CALLBACK, callback,
				D.STACK, true
			)
		);
	}
}
