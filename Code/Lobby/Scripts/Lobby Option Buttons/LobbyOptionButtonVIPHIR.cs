using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/**
Controls UI behavior of a menu option button in the main lobby.
*/

public class LobbyOptionButtonVIPHIR : LobbyOptionButtonVIP
{
	public UISprite frame;
	public GameObject vipEarlyAccess;
	public GameObject vipComingSoon;
	public GameObject vipDoubleFreeSpins;
	
	public override void setup(LobbyOption option, int page, float width, float height)
	{
		// If the option is null, set it to COMING_SOON view.
		if (option == null)
		{
			Debug.LogErrorFormat("LobbyOptionButtonVIPHIR.cs -- setup -- the option was null, creating a VIP coming soon option for safety.");
			option = new LobbyOption();
			option.type = LobbyOption.Type.VIP_COMING_SOON;
		}

		base.setup(option, page, width, height);

		switch (option.type)
		{
			case LobbyOption.Type.GAME:
				if (vipComingSoon != null)
				{
					vipComingSoon.SetActive(false);
				}

				if (!option.game.isUnlocked)
				{
					//frame.color = Color.gray;
					UITexture tex = image.GetComponent<UITexture>();
					if (tex != null)
					{
						tex.color = Color.black;
					}
					else
					{
						image.GetComponent<Renderer>().material = new Material(getOptionShader());
						image.GetComponent<Renderer>().material.color = Color.black;
					}
				}

				if (vipEarlyAccess != null)
				{
					vipEarlyAccess.SetActive(LobbyGame.vipEarlyAccessGame == option.game);
				}

				if (vipDoubleFreeSpins != null)
				{
					vipDoubleFreeSpins.SetActive(LobbyGame.doubleFreeSpinGames.Contains(option.game) && LobbyGame.vipEarlyAccessGame != option.game);
				}
				else if (option.game.isDoubleFreeSpins)
				{
					option.imageFilename = SlotResourceMap.getLobbyImagePath
					(
						option.game.groupInfo.keyName
						, option.game.keyName
						, "1X2"
						, option.game.isDoubleFreeSpins
					);
				}
				break;
			
			case LobbyOption.Type.VIP_COMING_SOON:
				vipComingSoon.SetActive(true);
				break;

			case LobbyOption.Type.VIP_EARLY_ACCESS_COMING_SOON:
				image.gameObject.SetActive(false);
				vipComingSoon.SetActive(true);
				vipEarlyAccess.SetActive(true);
				frame.color = Color.gray;
				break;
		}
	}
}
