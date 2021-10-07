using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
	AgeGateDialogHIR -- subclass of AgeGateDialog for HIR specific implementation.
**/

public class AgeGateDialogHIR : AgeGateDialog
{
	public UITexture imageTexture;

	public override void init()
	{
		base.init();
		downloadedTextureToUITexture(imageTexture, 0);
	}
}