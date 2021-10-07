using UnityEngine;
using System.Collections;

/*
HIR version of progressive jackpot toaster.
*/

public class ProgressiveJackpotToasterHIR : ProgressiveJackpotToaster 
{
	public UITexture gameIcon;
	
	// Callback for loading a game icon texture.
	private void optionTextureLoaded(Texture2D tex, Dict data)
	{
		if (tex != null && gameIcon != null)
		{
			NGUIExt.applyUITexture(gameIcon, tex);
		}
	}
}
