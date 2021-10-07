using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Controls the display of a panel for a VIP game on the VIP dialog.
*/

public class VIPDialogGamePanel : TICoroutineMonoBehaviour
{
	public TextMeshPro nameLabel;
	public UISprite vipSprite;
	public Renderer gameImageRenderer;
	public TextMeshPro gameNameLabel;
	public GameObject gameParent;
	public GameObject comingSoonParent;
	
	private LobbyGame game = null;
	
	public void setGame(LobbyGame game)
	{
		this.game = game;
		
		if (game != null)
		{
			gameImageRenderer.material = new Material(LobbyOptionButtonActive.getOptionShader());
			gameImageRenderer.material.color = Color.black;

			gameNameLabel.gameObject.SetActive(false);
			
			setLevelElements(game.vipLevel);

			string filename = SlotResourceMap.getLobbyImagePath(game.groupInfo.keyName, game.keyName, "");
		
			StartCoroutine(DisplayAsset.loadTextureFromBundle(filename, optionTextureLoaded, Dict.create(D.IMAGE_TRANSFORM, gameImageRenderer.transform), skipBundleMapping:true, pathExtension:".png"));
		}
		else
		{
			// If null, then show the COMING SOON stuff instead.
			// This is only available for the EARLY ACCESS version of the panel, so nullcheck just in case.
			if (comingSoonParent != null)
			{
				gameParent.SetActive(false);
				comingSoonParent.SetActive(true);
			}
			
			// Set the vip level to the first one that allows early access.
			setLevelElements(VIPLevel.earlyAccessMinLevel);
		}
	}
	
	private void setLevelElements(VIPLevel level)
	{
		nameLabel.text = Localize.toUpper(level.name);
		vipSprite.spriteName = string.Format("VIP Games Gem Inset {0}", level.levelNumber);
	}
	
	// Callback for loading a texture.
	private void optionTextureLoaded(Texture2D tex, Dict data)
	{
		if (tex != null)
		{
			gameImageRenderer.material.mainTexture = tex;
			gameImageRenderer.material.color = Color.white;
		}
		else
		{
			gameNameLabel.text = game.name;
			gameNameLabel.gameObject.SetActive(true);
		}
	}

}
