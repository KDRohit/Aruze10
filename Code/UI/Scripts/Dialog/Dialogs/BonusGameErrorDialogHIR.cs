using UnityEngine;
using System.Collections;

public class BonusGameErrorDialogHIR : BonusGameErrorDialog
{
	public override void init()
	{
		base.init();
		
		// Force the credits label bounds to update before using it below.
		credits.ForceMeshUpdate();
		
		// Position the coin and amount label so that it's always horizontally centered.
		float width = credits.transform.localPosition.x + credits.bounds.size.x;
		CommonTransform.setX(credits.transform.parent, width * -0.5f);
	}
}