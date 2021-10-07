using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Controls UI behavior of a menu option button in the main lobby.
*/

public class LobbyOptionButtonGeneric1X2 : LobbyOptionButtonLockable
{
	private const float JACKPOT_THROB_FREQUENCY = 5.0f;

	public GameObject deluxeTopper;
	
	private string localizedLimitedTime = "";	// Cache it for performance.
	private Throbamatic jackpotHeaderThrobber = null;
	
	public override void setup(LobbyOption option, int page, float width, float height)
	{
		base.setup(option, page, width, height);
		
		// Hide the dynamic text by default unless it's needed.
		SafeSet.gameObjectActive(dynamicTextParent, false);

		SafeSet.gameObjectActive(deluxeTopper, (option.game != null && option.game.isDeluxe));

		switch (option.type)
		{
			case LobbyOption.Type.GAME:
				refresh();

			#if RWR
				// Create the real world rewards UI element if necessary.
				createRWR();
			#endif
				break;
		}
	}
	
	protected override void Update()
	{
		base.Update();
		
		if (jackpotHeaderThrobber != null)
		{
			jackpotHeaderThrobber.update();
		}
	}

	/// Force a refresh of some visible element, initially going to be used to control 
	/// lock icons on options that need to be displayed or hidden based on using the old or new wager system
	public override void refresh()
	{
		base.refresh();

		bool isLocked = (!option.game.isUnlocked &&
		                 (UnlockAllGamesFeature.instance == null || !UnlockAllGamesFeature.instance.isEnabled));

		// Since sneak preview games are flagged as unlocked, but we need to pass in the actual unlock level,
		// we must check isSneakPreview here, separate from the isLocked check.
		if (isLocked || option.game.isSneakPreview)
		{
			// Set the unlock text to the required level.
			setUnlockLevel(option.game.unlockLevel);
		}
		else
		{
			setUnlockLevel(0);
		}
	}
}
