using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SinCityPortal : LobbyOptionButtonChallengeLobby
{
	// =============================
	// PROTECTED
	// =============================
	protected int gameImageIndex = 0;
	protected List<string> gameKeys = null;
	
	[SerializeField] private Renderer currentGameImage;
	[SerializeField] private Renderer nextGameImage;
		
	public override void setup(LobbyOption option, int page, float width, float height)
	{
		base.setup(option, page, width, height);

		if (isPortal)
		{
			gameKeys = CampaignDirector.find(CampaignDirector.SIN_CITY).getGameKeys();
			gameImageIndex = 0;
			cycleGameImages();
		}
	}

	protected void cycleGameImages()
	{
		LobbyInfo sinCity = LobbyInfo.find(LobbyInfo.Type.SIN_CITY);

		if (sinCity != null && gameKeys != null)
		{
			LobbyGame game = sinCity.allLobbyOptions[gameImageIndex].game;

			if (game != null)
			{
				string filePath = SlotResourceMap.getLobbyImagePath(game.groupInfo.keyName, game.keyName);
				RoutineRunner.instance.StartCoroutine(DisplayAsset.loadTextureFromBundle(filePath, addTextureToCurrent, skipBundleMapping:true, pathExtension:".png"));

				gameImageIndex = (int)CommonMath.umod(gameImageIndex + 1, gameKeys.Count);
				game = sinCity.allLobbyOptions[gameImageIndex].game;

				if (game != null)
				{
					filePath = SlotResourceMap.getLobbyImagePath(game.groupInfo.keyName, game.keyName);
					RoutineRunner.instance.StartCoroutine(DisplayAsset.loadTextureFromBundle(filePath, addTextureToNext, skipBundleMapping:true, pathExtension:".png"));
				}
			}

			iTween.ValueTo
			(
				gameObject
				, iTween.Hash("from", 1, "to", 0, "time", 3, "delay", 3, "onupdate", "updateCurrentImage")
			);
		}
	}

	protected void addTextureToCurrent(Texture2D tex, Dict data)
	{
		if (tex == null || currentGameImage == null || currentGameImage.material == null)
		{
			return;
		}
		
		currentGameImage.material.SetTexture("_StartTex", tex);
		currentGameImage.material.SetFloat("_Fade", 0);
	}

	protected void addTextureToNext(Texture2D tex, Dict data)
	{
		if (nextGameImage == null || nextGameImage.material == null)
		{
			return;
		}
		nextGameImage.material.SetTexture("_StartTex", tex);
	}

	public void updateCurrentImage(float value)
	{
		Material mat = currentGameImage.material;
		mat.SetFloat("_Fade", 1-value);

		if (value <= 0)
		{
			cycleGameImages();
		}
	}
}