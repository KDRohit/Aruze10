using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class GenericLobbyPortalV3 : GenericLobbyPortal
{
	

	[HideInInspector] public  Texture2D tabIcon;

	[SerializeField] private Renderer roomImage;
	[SerializeField] private Renderer currentGameImage;
	[SerializeField] private Renderer nextGameImage;
	private static Texture2D fallbackImage;
		
	public override void setup(bool isLocked, bool loadBGImage = true, bool loadTabImage = true)
	{
		base.setup(isLocked, loadBGImage, loadTabImage);

		if (loadBGImage)
		{
			SkuResources.loadFromMegaBundleWithCallbacks(filePath, onDownloadBackground, onBackGroundLoadFail);
		}
		else
		{
			roomImage.gameObject.SetActive(false);
		}

		if (loadTabImage)
		{
			SkuResources.loadFromMegaBundleWithCallbacks(fileTabPath, onDownloadTabIcon, onTabIconLoadFail);
		}

		if (!string.IsNullOrEmpty(staticArtPath))
		{
			SkuResources.loadFromMegaBundleWithCallbacks(staticArtPath, onStaticArtLoad, onStaticArtLoadFail);
		}

		if (fallbackImage == null)
		{
			SkuResources.loadFromMegaBundleWithCallbacks(FALLBACK_ART_PATH, onDownloadFallbackArt, onTabIconLoadFail);
		}

		//cycle games
		if (gameKeys != null)
		{
			cycleGameImages();
		}
	}

	protected override void setupFallback()
	{
		currentGameImage.material.SetTexture("_StartTex", fallbackImage);
		nextGameImage.gameObject.SetActive(false);
	}

	private void onStaticArtLoad(string assetPath, Object obj, Dict data = null)
	{
		currentGameImage.material.SetTexture("_StartTex", obj as Texture2D);
		nextGameImage.gameObject.SetActive(false);
	}

	private void onStaticArtLoadFail(string assetPath, Dict data = null)
	{
		Debug.LogError("Failed to load " + assetPath);
	}

	private void onDownloadTabIcon(string assetPath, Object obj, Dict data = null)
	{
		tabIcon = obj as Texture2D;
	}

	private void onDownloadFallbackArt(string assetPath, Object obj, Dict data = null)
	{
		fallbackImage = obj as Texture2D;	
	}

	private  void onTabIconLoadFail(string assetPath, Dict data = null)
	{
		Debug.LogError("Failed to load " + assetPath);
	}

	private void onDownloadBackground(string assetPath, Object obj, Dict data = null)
	{
		Texture2D tex = obj as Texture2D;

		isWaitingForTextures = false;

		if (tex != null)
		{
			if (roomImage == null || roomImage.material == null)
			{
				return;
			}
			roomImage.material.SetTexture("_StartTex", tex);
		}
	}

	private  void onBackGroundLoadFail(string assetPath, Dict data = null)
	{
		Debug.LogError("Failed to load " + assetPath);
	}	

	private void addTextureToCurrent(Texture2D tex, Dict data)
	{

		if (tex == null || currentGameImage == null || currentGameImage.material == null)
		{
			return;
		}

		currentGameImage.material.SetTexture("_StartTex", tex);
		currentGameImage.material.SetFloat("_Fade", 0);
	}

	private void addTextureToNext(Texture2D tex, Dict data)
	{
		if (nextGameImage == null || nextGameImage.material == null)
		{
			return;
		}
		nextGameImage.material.SetTexture("_StartTex", tex);
	}

	private void cycleGameImages()
	{
		LobbyInfo info = LobbyInfo.find(lobbyType);
		LobbyGame game = null;
		if (info != null && gameKeys != null && gameKeys.Count > 0)
		{
			if (gameImageIndex < info.allLobbyOptions.Count)
			{
				game = info.allLobbyOptions[gameImageIndex].game;
			}
			if (game != null)
			{
				string filePath = SlotResourceMap.getLobbyImagePath(game.groupInfo.keyName, game.keyName);
				RoutineRunner.instance.StartCoroutine(DisplayAsset.loadTextureFromBundle(filePath, addTextureToCurrent, skipBundleMapping:true, pathExtension:".png"));

				gameImageIndex = (int)CommonMath.umod(gameImageIndex + 1, gameKeys.Count);

				if (gameImageIndex < info.allLobbyOptions.Count)
				{
					game = info.allLobbyOptions[gameImageIndex].game;
				}
				else
				{
					Debug.LogErrorFormat
					(
						"GenericLobbyPortalV3: gameKeys index {0} does not align with lobby options count {1}",
						gameImageIndex.ToString(),
						info.allLobbyOptions.Count
					);
				}

				if (game != null)
				{
					filePath = SlotResourceMap.getLobbyImagePath(game.groupInfo.keyName, game.keyName);
					RoutineRunner.instance.StartCoroutine(DisplayAsset.loadTextureFromBundle(filePath, addTextureToNext,skipBundleMapping:true, pathExtension:".png"));

				}
			}

			iTween.ValueTo
			(
				gameObject
				, iTween.Hash("from", 1, "to", 0, "time", 3, "delay", 3, "onupdate", "updateCurrentImage")
			);	
		}
		else if (gameKeys != null && info != null)
		{
			Debug.Log("stop here");
		}
	}

	public void updateCurrentImage(float value)
	{
		Material mat = currentGameImage.material;
		mat.SetFloat("_Fade", 1-value);

		if (value <= 0)
		{
			setJackpotLabel();
			cycleGameImages();
		}

	}
}
