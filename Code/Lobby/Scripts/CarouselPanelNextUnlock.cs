using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Attached to the Next Unlock carousel panel since it has to determine the game icon dynamically.
Any necessary UI elements are linked to this to get for setting up.
*/

public class CarouselPanelNextUnlock : CarouselPanelBase
{
	public TextMeshPro label;
	public Renderer background;
	public Renderer gameIcon;
		
	public override void init()
	{
		LobbyGame game = LobbyGame.getNextUnlocked(SlotsPlayer.instance.socialMember.experienceLevel);
		
		if (game != null)
		{
			if (data.imageUrls[0] != "")
			{
				loadTexture(background, "lobby_carousel/" + data.imageUrls[0]);
			}

			string filename = SlotResourceMap.getLobbyImagePath(game.groupInfo.keyName, game.keyName);
			loadTexture(gameIcon, filename);
		
			label.text = Localize.textUpper("next_unlock_at_level_{0}", game.unlockLevel);

			// Prepare to be touched.
			data.action = string.Format("game:{0}", game.keyName);
		}
		else
		{
			// If no next game, make sure nothing happens if touched.
			data.action = "";
		}
	}
	
	void Update()
	{
		if (data.action == "")
		{
			// This slide shouldn't be in the carousel if the player is already at the max level for unlocking games.
			// This could happen if the player leveled up to the highest level during the session.
			// Remove it when the page scroller is finished scrolling, to avoid weirdness removing while scrolling.
			data.deactivate();
		}
	}
}
