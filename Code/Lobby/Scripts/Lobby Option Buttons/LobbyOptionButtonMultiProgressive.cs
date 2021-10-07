using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/**
Controls UI behavior of a menu option button in the main lobby.
*/

public class LobbyOptionButtonMultiProgressive : LobbyOptionButtonLockable
{
	public TextMeshPro[] jackpotLabels;
	
	public override void setup(LobbyOption option, int page, float width, float height)
	{
		base.setup(option, page, width, height);
	
		refresh();
		
		#if RWR
		// Create the real world rewards UI element if necessary.
		createRWR();
		#endif

		setFeatureUnlockLevel();
	}
}
