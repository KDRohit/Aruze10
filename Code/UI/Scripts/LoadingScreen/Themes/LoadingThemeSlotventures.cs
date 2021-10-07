using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class LoadingThemeSlotventures : LoadingTheme
{
	public UISprite meterBar;
	public UISprite meterFrame;
	public UISprite meterBackground;
	public UISprite logo;
	public UISprite smallLogo;
	public GameObject gameImageContainer;
	public Renderer gameImage;
	public Renderer background;
	private const string BACKGROUND_IMAGE = "Features/Slotventures/{0}/Textures/slotventures_{0}_V3_background";

	public void disable()
	{
		SafeSet.gameObjectActive(gameImageContainer, false);
	}

	public void toggleLoadingImages(bool isActive = false)
	{
		if (Loading.hirV3 != null && Loading.hirV3.backgroundRenderer != null)
		{
			Loading.hirV3.backgroundRenderer.gameObject.SetActive(isActive);
		}
	}

	/// <summary>
	///   Show the max voltage screen loading effects
	/// </summary>
	public override void show()
	{
		ChallengeCampaign slotventureCampaign = CampaignDirector.find(SlotventuresChallengeCampaign.CAMPAIGN_ID);

		// If the campaign is around
		if (slotventureCampaign != null)
		{
			if (LobbyLoader.lastLobby != LobbyInfo.Type.SLOTVENTURE && (GameState.game == null || slotventureCampaign.currentMission == null || !slotventureCampaign.currentMission.containsGame(GameState.game.keyName)))
			{
				hide();
				return;
			}
		}
		else
		{
			hide();
			return;
		}

		AssetBundleManager.load(string.Format(BACKGROUND_IMAGE, SlotventuresLobby.assetData.themeName), onLoadTexture, onLoadTextureFail);
	}

	private void onLoadTexture(string assetPath, Object obj, Dict data = null)
	{
		Texture2D tex = obj as Texture2D;
		background.material.mainTexture = tex;
		base.show();
		aspectRatioCorrection();
		toggleLoadingImages(false);
	}

	private void aspectRatioCorrection()
	{
		//texture gets scaled to size of background object

		//get size of background renderer
		Vector3 size = background.gameObject.transform.localScale;

		//test if we have an invalid scale
		if (size.x <= 0 || size.y <= 0)
		{
			return;
		}
		
		//check if screen is too wide
		if (Screen.width > size.x)  //values less than one will be an error case
		{
			
			float adjust = Screen.width / size.x;
			size *= adjust;
			background.gameObject.transform.localScale = size;
		}
		else if (((float)Screen.width / Screen.height) > (size.x / size.y))
		{
			//screen is a wider aspect, but smaller than the the texture
			//this happens frequently when testing in the unity editor
			float ratio = ((float) Screen.width / Screen.height) / (size.x / size.y);
			size *= ratio;
			background.gameObject.transform.localScale = size;

		}

		//check if screen is too taller than our background object
		if (Screen.height > size.y)
		{
			float adjust = Screen.height / size.y;
			size *= adjust;
			background.gameObject.transform.localScale = size;
		}
	}

	private void onLoadTextureFail(string assetPath, Dict data = null)
	{
		hide();
		Debug.LogError("LobbyOptionButtonSlotventurePortal::onLoadTextureFail - Could not load themed texture");
	}
	public override void hide()
	{
		base.hide();
		toggleLoadingImages(true);
	}

	/// <summary>
	///   if the user is loading up a game, add the game image to the image container
	/// </summary>
	public void addGameImage(LobbyGame game)
	{
		if (gameImage != null && game != null)
		{
			SafeSet.gameObjectActive(gameImageContainer, true);
			string imagePath = SlotResourceMap.getLobbyImagePath(game.groupInfo.keyName, game.keyName);
			DisplayAsset.loadTextureToRenderer(gameImage, imagePath, "", true, skipBundleMapping:true, pathExtension:".png");
		}
	}

	public void removeGameImage()
	{
		SafeSet.gameObjectActive(gameImageContainer, false);
	}
}