using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/**
Controls UI behavior of a menu option button in the main lobby.
*/

public abstract class LobbyOptionButtonLockable : LobbyOptionButtonActive
{
	public GameObject unlockLockParent;     // lock icon and text
	public TextMeshPro unlockLevelTMPro;
	public string unlockLevelLocalization = "";	// Optionally allows the unlock level label to be localized instead of just showing the number.
	public bool hideFeatureLabel;

	// Set the unlock level and lock visibility where linked.
	// Subclasses call this if they have lock icons and level labels to pass in.
	protected void setUnlockLevel(int level)
	{
		// In case a mass refresh goes off as this object is deleted.
		if (gameObject != null)
		{
			if (unlockLevelTMPro != null)
			{
				if (unlockLevelLocalization != null && unlockLevelLocalization != "")
				{
					unlockLevelTMPro.text = Localize.textUpper(unlockLevelLocalization, CommonText.formatNumber(level));
				}
				else
				{
					unlockLevelTMPro.text = CommonText.formatNumber(level);
				}
			}

			// If the game is sneak preview, lets show that icon. 
			if (option != null)
			{
				if (option.game != null)
				{
					SafeSet.gameObjectActive(sneakPreviewIcon, option.game.isSneakPreview);
					SafeSet.gameObjectActive(unlockLockParent, !option.game.isUnlocked && !option.game.isComingSoon);
				} 
				else if (option.action != null && option.isBannerAction)
				{
					SafeSet.gameObjectActive(unlockLockParent, true);
				}
				else
				{
					Debug.LogError(
						"LobbyOptionButtonLockable::setUnlockLevel - The game Or launch game action was null on prefab with the name " +
						gameObject.name);
				}
			}
			else
			{
				Debug.LogError("LobbyOptionButtonLockable::setUnlockLevel - The option was null " + gameObject.name);
			}
		}
	}
	
	protected void setFeatureUnlockLevel()
	{
        if (option != null)
        {
            if (option.game == null || option.game.isUnlocked)
            {
                setUnlockLevel(0);
            }
            else
            {
                setUnlockLevel(option.game.unlockLevel);			
            }
        }
	}
	
	/// Force a refresh of some visible element, initially going to be used to control 
	/// lock icons on options that need to be displayed or hidden based on using the old or new wager system
	public override void refresh()
	{
		base.refresh();

		if (option != null && option.game != null)
		{
			setFeatureUnlockLevel();
		}
	}

}
