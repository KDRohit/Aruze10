using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Controls UI behavior of a menu option button in the main lobby.
*/

public class LobbyOptionButtonPremium : LobbyOptionButtonActive
{
	public GameObject[] premiumLocks;

	public override void setup(LobbyOption option, int page, float width, float height)
	{
		base.setup(option, page, width, height);
		
		// Whether the game is an unlocked premium game that appears locked in the lobby because it has never been spun.
		// See HIR-8319 for this weirdness.
		bool isPremiumAppearsLocked =
			!option.game.xp.didUnlockInGame &&	// See HIR-8825 for explanation of this variable.
			option.game.xp.spinCount == 0;

		premiumLocks[0].SetActive(isPremiumAppearsLocked || !option.game.isUnlocked);
		premiumLocks[1].SetActive(isPremiumAppearsLocked || !option.game.isUnlocked);
	
		#if RWR
		// Create the real world rewards UI element if necessary.
		createRWR();
		#endif
	}
}
