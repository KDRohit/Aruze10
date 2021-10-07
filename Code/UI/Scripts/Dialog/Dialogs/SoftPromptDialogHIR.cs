using UnityEngine;
using System.Collections;
using System;
using TMPro;

/*
HIR specific treatment for this dialog.
*/

public class SoftPromptDialogHIR : SoftPromptDialog
{
	public TextMeshPro mainLabel;
	
	// Initialization
	public override void init()
	{
		base.init();
		if (Localize.keyExists("soft_prompt_" + theme))
		{
			mainLabel.text = Localize.text("soft_prompt_" + theme);
		}
	}
}
