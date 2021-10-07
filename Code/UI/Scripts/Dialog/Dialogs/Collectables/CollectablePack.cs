using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CollectablePack : MonoBehaviour 
{
	[SerializeField] private UISprite packSprite;
	[SerializeField] private Renderer packLogo;
	[SerializeField] private UITexture packLogoUiTexture;
	[SerializeField] private GameObject[] stars;
	[SerializeField] private UICenteredGrid starGrid;
	[SerializeField] private LabelWrapperComponent wildCardLabel; 
	
	public TextMeshPro minCardsLabel;

	public Animator animator;
	
	private const string MIN_CARD_STRING_LOCALIZATION = "min_{0}_of_color";
	private const string WILD_CARD_STRING = "wild_card_generic_text";
	
	public const string GENERIC_LOGO_PATH = "Assets/Data/HIR/Bundles/Initialization/Features/Collections/Textures/collection_logo_generic.png";

	private const string RANDOM_NEW_CARD_PACK_KEY = "random_new_card";
	private const string TARGETED_NEW_CARD_PACK_KEY = "targeted_new_card";

	public void init(string packKey, bool useGenericLogo = false, bool grayPack = false)
	{
		if (grayPack)
		{
			grayOut();
		}
		
		CollectableAlbum currentAlbum = Collectables.Instance.getAlbumByKey(Collectables.currentAlbum);

		if (currentAlbum == null)
		{
			return;
		}

		CollectablePackData packData = Collectables.Instance.findPack(packKey);

		//Skip loading the logo if neither render is even set
		if (packLogoUiTexture != null || packLogo != null)
		{
			if (isWildCardPack(packKey))
			{
				//Turn off the logo renderers for wild cards. They have a special non themed logo baked into their pack sprites
				if (packLogoUiTexture != null)
				{
					packLogoUiTexture.gameObject.SetActive(false);
				}
				if (packLogo != null)
				{
					packLogo.gameObject.SetActive(false);
				}
			}
			else if (useGenericLogo)
			{
				SkuResources.loadFromMegaBundleWithCallbacks(this, GENERIC_LOGO_PATH, packArtLoadSuccess, bundleLoadFailure);
			}
			else
			{
				AssetBundleManager.load(this, currentAlbum.logoTexturePath, packArtLoadSuccess, bundleLoadFailure, isSkippingMapping: true, fileExtension: ".png");
			}

		}

		int minRarity = 0;
		if (packData != null)
		{
			if (wildCardLabel != null)
			{
				wildCardLabel.gameObject.SetActive(false);
			}
			minRarity = packData.constraints[0].minRarity;
			int guaranteedPicks = packData.constraints[0].guaranteedPicks;
			if (minCardsLabel != null)
			{
				minCardsLabel.text = Localize.text(MIN_CARD_STRING_LOCALIZATION, guaranteedPicks);
			}

			if (stars.Length > 0)
			{
				for (int i = 0; i < minRarity && i < stars.Length; i++)
				{
					stars[i].SetActive(true);
				}

				if (starGrid != null)
				{
					starGrid.reposition();
				}
			}
		} 
		else if (isWildCardPack(packKey))
		{
			// https://jira.corp.zynga.com/browse/HIR-89703
			// Determining rarity for wild card is not straightforward. Hence we show a generic text
			if (wildCardLabel != null)
			{
				wildCardLabel.gameObject.SetActive(true);
				wildCardLabel.text = Localize.text(WILD_CARD_STRING);
			}

			if (minCardsLabel != null)
			{
				minCardsLabel.gameObject.SetActive(false);
			}
		}

		packSprite.spriteName = getPackColor(minRarity, packKey);
	}
	
	public void initWithForcedRarity(int rarity, bool useGenericLogo = false)
	{
		CollectableAlbum currentAlbum = Collectables.Instance.getAlbumByKey(Collectables.currentAlbum);
		if (useGenericLogo)
		{
			SkuResources.loadFromMegaBundleWithCallbacks(this, GENERIC_LOGO_PATH, packArtLoadSuccess, bundleLoadFailure);
		}
		else
		{
			AssetBundleManager.load(this, currentAlbum.logoTexturePath, packArtLoadSuccess, bundleLoadFailure, isSkippingMapping:true, fileExtension:".png");
		}
		packSprite.spriteName = getPackColor(rarity, "");
	}

	public void grayOut()
	{
		packSprite.color = Color.gray;
		if (packLogo != null)
		{
			packLogo.material.color = Color.gray;
		}

		if (packLogoUiTexture != null)
		{
			packLogoUiTexture.color = Color.gray;
		}
	}

	private void packArtLoadSuccess(string assetPath, Object obj, Dict data = null)
	{
		if (this == null)
		{
			return;
		}
		
		Texture2D logoTexture = obj as Texture2D;

		if (packLogo != null)
		{
			Material material = new Material(packLogo.material.shader);
			material.mainTexture = logoTexture;
			packLogo.material = material;
		}

		if (packLogoUiTexture != null)
		{
			Material material = new Material(packLogoUiTexture.material);
			material.mainTexture = logoTexture;
			packLogoUiTexture.material = material;
			packLogoUiTexture.gameObject.SetActive(false);
			packLogoUiTexture.gameObject.SetActive(true);
		}
	}

	// Used by LobbyLoader to preload asset bundle.
	private void bundleLoadFailure(string assetPath, Dict data = null)
	{
		Debug.LogError("CollectAPack::bundleLoadFailure - Failed to download " + assetPath);
		if (assetPath != GENERIC_LOGO_PATH)
		{
			//Try to load the generic logo if our themed one failed
			SkuResources.loadFromMegaBundleWithCallbacks(this, GENERIC_LOGO_PATH, packArtLoadSuccess, bundleLoadFailure);
		}
	}

	public static string getPackColor(int minRarity, string packName)
	{
		if (isWildCardPack(packName))
		{
			return CollectAPack.WILD_CARD_PACK_COLOR;
		}
		
		switch(minRarity)
		{
		case 1:
			return CollectAPack.DEFAULT_PACK_COLOR;
		case 2:
			return CollectAPack.TWO_STAR_PACK_COLOR;
		case 3:
			return CollectAPack.THREE_STAR_PACK_COLOR;
		case 4:
			return CollectAPack.FOUR_STAR_PACK_COLOR;
		case 5:
			return CollectAPack.FIVE_STAR_PACK_COLOR;
		default:
			return CollectAPack.DEFAULT_PACK_COLOR;
		}
	}

	public static bool isWildCardPack(string packName)
	{
		return packName == RANDOM_NEW_CARD_PACK_KEY || packName == TARGETED_NEW_CARD_PACK_KEY;
	}
}
