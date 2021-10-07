using UnityEngine;
using System.Collections;

public class DoSomethingZade : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		StatsManager.Instance.LogCount("dialog", "xPromo", "", "", "");
		//TODO: Girish
		/*ZADEAdManager.Instance.RequestAd(ZADEAdManager.ZADE_IXPROMO_SLOT_NAME,
			(Zap.Ad ad, Texture2D tex) =>
				{
					if (ad != null)
					{
						ad.Delegate = ZADEAdManager.Instance;
						if (ad.CanOpenMRAID)
						{
							ad.openMRAID(true);
						}
						else
						{
							Debug.LogError("cannot open MRAID Ad -- Lobby");
						}
					}
				},
			null
		);*/
	}
	
	public override bool getIsValidToSurface(string parameter)
	{
		// Zade slides are always inactive until an actual ad has successfully downloaded.
		return false;
	}
}
