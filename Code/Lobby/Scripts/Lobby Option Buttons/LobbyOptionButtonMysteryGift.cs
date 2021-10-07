using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Controls UI behavior of a menu option button in the main lobby.
*/

public class LobbyOptionButtonMysteryGift : LobbyOptionButtonLockable
{
	public GameObject mysteryGiftIncreasedChanceIcon;
	public GameObject mysteryGiftIncreasedChanceIconGlowSizer;
	
	private Throbamatic flameThrobber = null;
	
	public override void setup(LobbyOption option, int page, float width, float height)
	{
		base.setup(option, page, width, height);
		setFeatureUnlockLevel();

		// Default should be disable.
		mysteryGiftIncreasedChanceIcon.SetActive(false);

		#if RWR
		// Create the real world rewards UI element if necessary.
		createRWR();
		#endif
		
		if (mysteryGiftIncreasedChanceIconGlowSizer != null &&
			MysteryGift.isIncreasedMysteryGiftChance
			)
		{
			flameThrobber = new Throbamatic(this, mysteryGiftIncreasedChanceIconGlowSizer, 1.0f, 1.5f, 0.5f);
		}

		refresh();
	}

	protected override void Update()
	{
		base.Update();
		if (flameThrobber != null)
		{
			flameThrobber.update();
		}

		updateFooterIcons();			
	}

	public override void refresh()
	{
		base.refresh();
		updateFooterIcons();
	}

	private void updateFooterIcons()
	{
		if (mysteryGiftIncreasedChanceIcon != null && option != null && option.game != null)
		{
			switch (option.game.mysteryGiftType)
			{
				case MysteryGiftType.MYSTERY_GIFT:
					mysteryGiftIncreasedChanceIcon.SetActive(MysteryGift.isIncreasedMysteryGiftChance);
					break;

				case MysteryGiftType.BIG_SLICE:
				 	mysteryGiftIncreasedChanceIcon.SetActive(MysteryGift.isIncreasedBigSliceChance);
				 	break;
			}
		}
	}
}
