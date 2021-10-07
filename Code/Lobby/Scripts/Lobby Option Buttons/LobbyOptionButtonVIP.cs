using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/**
Controls UI behavior of a menu option button in the main lobby.
*/

public class LobbyOptionButtonVIP : LobbyOptionButtonActive
{
	public VIPNewIcon vipIcon;
	public GameObject lockedIcon;
	public TextMeshPro vipTMPro;
	
	protected virtual Color disabledImageColor
	{
		get { return Color.gray; }
	}
	
	public override void setup(LobbyOption option, int page, float width, float height)
	{
		base.setup(option, page, width, height);
		
		if (option != null)
		{
			switch (option.type)
			{
				case LobbyOption.Type.GAME:
					refresh();
				#if RWR
					createRWR(true);
				#endif
					break;
			
				case LobbyOption.Type.VIP_COMING_SOON:
					setSelectable(false);
					SafeSet.gameObjectActive(lockedIcon, false);
					break;
				
				case LobbyOption.Type.VIP_EARLY_ACCESS_COMING_SOON:
					setSelectable(false);
					SafeSet.vipIconLevel(vipIcon, VIPLevel.earlyAccessMinLevel);
					SafeSet.labelText(vipTMPro, Localize.textUpper("{0}_members", VIPLevel.earlyAccessMinLevel.name));
					break;
			
				// Maybe look for other types of options here and do something below with it.
			}
		}
	}
	
	public override void refresh()
	{
		base.refresh();
		
		if (option != null && option.type == LobbyOption.Type.GAME)
		{
			SafeSet.vipIconLevel(vipIcon, option.game.vipLevel);
									
			if (option.game.isUnlocked)
			{
				SafeSet.gameObjectActive(lockedIcon, false);
				setSelectable(true);
			}
			else
			{
				if (option.game != null && option.game.vipLevel != null)
				{
					SafeSet.labelText(vipTMPro, Localize.textUpper("{0}_members", option.game.vipLevel.name));
				}
				setSelectable(false);
			
				imageTint = disabledImageColor;
			}
		}
	}
}
