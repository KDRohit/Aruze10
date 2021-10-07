using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Data handling class for sets of collecables. Ideally the 10-14 cards. 
public class CollectableAlbum
{
	public int currentDuplicateStars;
	public int maxStars;
	public int numCompleted;
	public long rewardAmount;
	public string keyName;
	public List<string> setsInAlbum;
	public string starPackName = "";
	public int currentNewCards;
	public string completionAchievement { get; private set; }

	private List<Texture2D> textureList;
	private int assetsToLoad = 0;
	private int assetsLoaded = 0;

	// 0 = album name, 1 = texture
	private const string TEXTURE_BASE_PATH = "Features/Collections/Albums/{0}/Collection Textures/{1}";
	private const string CARD_PACK_ICON_TEXTURE = "Card Packs Icon";
	private const string BACKGROUND_TEXTURE = "collection_background";
	private const string LOGO_TEXTURE = "collection_logo";
	private const string SET_CONTAINER_TEXTURE = "Card Set Container";
	private const string ALBUM_REWARD_PATH = "Features/Collections/Albums/{0}/Prefabs/Collection Complete Content";

	private string _logoTexturePath = "";
	private string _backgroundTexturePath = "";
	private string _cardPackIconTexturePath = "";
	private string _albumRewardPrefabPath = "";
	private string _setContainerTexturePath = "";


	public string logoTexturePath
	{
		get
		{
			return _logoTexturePath;
		}
		protected set
		{
			_logoTexturePath = value;
		}
	}

	public string setContainerTexturePath
	{
		get
		{
			return _setContainerTexturePath;
		}
		protected set
		{
			_setContainerTexturePath = value;
		}
	}

	public string backgroundTexturePath
	{
		get
		{
			return _backgroundTexturePath;
		}
		protected set
		{
			_backgroundTexturePath = value;
		}
	}

	public string cardPackIconTexturePath
	{
		get
		{
			return _cardPackIconTexturePath;
		}
		protected set
		{
			_cardPackIconTexturePath = value;
		}
	}

	public string albumRewardPrefabPath
	{
		get
		{
			return _albumRewardPrefabPath;
		}
		protected set
		{
			_albumRewardPrefabPath = value;
		}
	}

	public CollectableAlbum(JSON data)
	{
		if (data != null)
		{
			keyName = data.getString("key_name", "");
			completionAchievement = data.getString("completion_achievement", "");
			currentDuplicateStars = 0;
			rewardAmount = 0;
			numCompleted = 0;
			setsInAlbum = new List<string>();
			logoTexturePath = string.Format(TEXTURE_BASE_PATH, keyName, LOGO_TEXTURE);
			cardPackIconTexturePath = string.Format(TEXTURE_BASE_PATH, keyName, CARD_PACK_ICON_TEXTURE);
			backgroundTexturePath = string.Format(TEXTURE_BASE_PATH, keyName, BACKGROUND_TEXTURE);
			albumRewardPrefabPath = string.Format(ALBUM_REWARD_PATH, keyName);
			setContainerTexturePath = string.Format(TEXTURE_BASE_PATH, keyName, SET_CONTAINER_TEXTURE);
			currentNewCards = 0;
		}
	}

	private bool hasLoadedAllAssets()
	{
		return assetsLoaded == assetsToLoad;
	}
}
