using UnityEngine;
using System.Collections;
using TMPro;

public class LoadingThemeMaxVoltage : LoadingTheme
{
	public MaxVoltageLoading mvLoading;
	public UISprite meterBar;
	public UISprite meterFrame;
	public UISprite meterBackground;
	public UISprite logo;
	public UISprite smallLogo;
	public GameObject gameImageContainer;
	public Renderer gameImage;

	// token asset objects
	public GameObject tokenParent;
	public TextMeshPro tokenCount;
	public UISprite tokenDescription;

	public static bool isLoadingMiniGame = false;

	// =============================
	// CONST
	// =============================
	private const string MV_FRAME = "Loading Bar Stretchy";
	private const string MV_FILL = "loading_fill";

	public void disable()
	{
		SafeSet.gameObjectActive(gameImageContainer, false);
	}

	public void toggleLoadingImages(bool isActive = false)
	{
	}

	/// <summary>
	///   Show the max voltage screen loading effects
	/// </summary>
	public override void show()
	{
		if (GameState.game == null || !GameState.game.isMaxVoltageGame)
		{
			hide();
			return;
		}
		base.show();

		toggleLoadingImages(false);

		SafeSet.gameObjectActive(logo.gameObject, true);
		
		if (mvLoading != null)
		{
			mvLoading.gameObject.SetActive(true);
		}

		if (meterBar != null)
		{
			meterBar.spriteName = MV_FILL;
		}

		if (meterFrame != null)
		{
			meterFrame.spriteName = MV_FRAME;
			meterFrame.color = Color.white;
		}

		if (GameState.game != null && GameState.game.isMaxVoltageGame)
		{
			setupTokens();
		}
		else
		{
			SafeSet.gameObjectActive(tokenParent, false);
			SafeSet.gameObjectActive(gameImageContainer, false);
		}
	}

	public override void hide()
	{
		base.hide();
		toggleLoadingImages(true);
	}

	public void setupTokens()
	{
		SafeSet.gameObjectActive(logo.gameObject, false);
		SafeSet.gameObjectActive(smallLogo.gameObject, true);
		if (isLoadingMiniGame)
		{
			SafeSet.gameObjectActive(gameImageContainer, false);
			SafeSet.gameObjectActive(tokenParent, true);

			tokenCount.text = MaxVoltageTokenCollectionModule.currentNumberOfPicks.ToString();
			isLoadingMiniGame = false;
		}
		else
		{
			SafeSet.gameObjectActive(tokenParent, false);
			addGameImage(GameState.game);
		}
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