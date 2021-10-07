using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameImageWidget : LoadingWidget
{
	public Renderer gameImage;

	public override void show()
	{
		gameImage.gameObject.SetActive(false);
					
		if (GameState.game != null && GameState.game.eosControlledLobby == null)
		{
			string imagePath = SlotResourceMap.getLobbyImagePath(GameState.game.groupInfo.keyName, GameState.game.keyName);
			DisplayAsset.loadTextureToRenderer(gameImage, imagePath, skipBundleMapping:true, pathExtension:".png");
			base.show();
		}
		else
		{
			hide();
		}
	}

	
	public override string name
	{
		get
		{
			return "game_image";
		}
	}
}