using UnityEngine;
using System.Collections;
using TMPro;

public class SelectGameUnlockPanel : MonoBehaviour
{
	public TextMeshPro unlockLevel;
	public Renderer image;
	public TextMeshPro gameNameLabel;
	
	private LobbyGame game;
	private SelectGameUnlockDialog dialog;
	
	public bool initPage(LobbyGame game, SelectGameUnlockDialog dialog)
	{
		this.game = game;
		this.dialog = dialog;
				
		image.material = new Material(LobbyOptionButtonActive.getOptionShader());
		image.material.color = Color.black;

		gameNameLabel.gameObject.SetActive(false);

		if (game != null)
		{
			string imagePath = SlotResourceMap.getLobbyImagePath(game.groupInfo.keyName, game.keyName);
			StartCoroutine(DisplayAsset.loadTextureFromBundle(imagePath, textureLoaded, Dict.create(D.IMAGE_TRANSFORM, image.transform), skipBundleMapping:true, pathExtension:".png"));
			
			if (unlockLevel != null)
			{
				unlockLevel.text = game.unlockLevel.ToString();
			}
			return true;
		}
		else
		{
			// Return false so we can remove the object.
			gameNameLabel.gameObject.SetActive(false);
			return false;
		}
	}
	
	// Callback for the DisplayAsset.LoadTexture call.
	private void textureLoaded(Texture2D tex, Dict args)
	{
		if (tex != null)
		{
			image.material.mainTexture = tex;
			image.material.color = Color.white;
		}
		else
		{
			gameNameLabel.text = game.name;
			gameNameLabel.gameObject.SetActive(true);
			Debug.LogWarning("SelectGameUnlockPanel -- could not find texure");
		}
	}
	
	
	// Callback for the Buy Button being clicked.
	private void unlockButtonClicked()
	{
		dialog.unlockGame(game);
	}
	
}
