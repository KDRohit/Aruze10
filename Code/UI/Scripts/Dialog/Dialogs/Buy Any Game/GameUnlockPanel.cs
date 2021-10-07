using UnityEngine;
using System.Collections;

public class GameUnlockPanel : MonoBehaviour
{
	public UILabel unlockLevel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent unlockLevelWrapperComponent;

	public LabelWrapper unlockLevelWrapper
	{
		get
		{
			if (_unlockLevelWrapper == null)
			{
				if (unlockLevelWrapperComponent != null)
				{
					_unlockLevelWrapper = unlockLevelWrapperComponent.labelWrapper;
				}
				else
				{
					_unlockLevelWrapper = new LabelWrapper(unlockLevel);
				}
			}
			return _unlockLevelWrapper;
		}
	}
	private LabelWrapper _unlockLevelWrapper = null;
	
	public UILabel priceLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent priceLabelWrapperComponent;

	public LabelWrapper priceLabelWrapper
	{
		get
		{
			if (_priceLabelWrapper == null)
			{
				if (priceLabelWrapperComponent != null)
				{
					_priceLabelWrapper = priceLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_priceLabelWrapper = new LabelWrapper(priceLabel);
				}
			}
			return _priceLabelWrapper;
		}
	}
	private LabelWrapper _priceLabelWrapper = null;
	
	public Renderer image;
	public UILabel gameNameLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent gameNameLabelWrapperComponent;

	public LabelWrapper gameNameLabelWrapper
	{
		get
		{
			if (_gameNameLabelWrapper == null)
			{
				if (gameNameLabelWrapperComponent != null)
				{
					_gameNameLabelWrapper = gameNameLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_gameNameLabelWrapper = new LabelWrapper(gameNameLabel);
				}
			}
			return _gameNameLabelWrapper;
		}
	}
	private LabelWrapper _gameNameLabelWrapper = null;
	
	
	private LobbyGame game;
	
	public bool initPage(LobbyGame game)
	{
		this.game = game;

		image.material = new Material(LobbyOptionButtonActive.getOptionShader());
		image.material.color = Color.black;
		
		SafeSet.gameObjectActive(gameNameLabelWrapper.gameObject, false);

		if (game != null)
		{
			string imagePath = SlotResourceMap.getLobbyImagePath(game.groupInfo.keyName, game.keyName);
			StartCoroutine(DisplayAsset.loadTextureFromBundle(imagePath, textureLoaded, Dict.create(D.IMAGE_TRANSFORM, image.transform), skipBundleMapping:true, pathExtension:".png"));
			
			if (unlockLevelWrapper != null)
			{
				unlockLevelWrapper.text = game.unlockLevel.ToString();
			}
			return true;
		}
		else
		{
			// Return false so we can remove the object
			SafeSet.gameObjectActive(gameNameLabelWrapper.gameObject, false);
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
			SafeSet.labelText(gameNameLabelWrapper, game.name);
			SafeSet.gameObjectActive(gameNameLabelWrapper.gameObject, true);
			Debug.LogError("BuyAnyGamePanel -- could not find texure");
		}
	}
}

