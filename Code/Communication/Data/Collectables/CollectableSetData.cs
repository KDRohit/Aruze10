using System;
using UnityEngine;
using System.Collections.Generic;

// Just holds relevant info for a rusher. 
public class CollectableSetData
{
	public int id;
	public string keyName;
	public int sortOrder;
	public long rewardAmount;
	public bool isComplete;
	public bool countsTowardAlbumCompletion;
	public bool isPowerupsSet;

	public List<string> cardsInSet;
	public string albumName = "";

	// 0 = album name, 1 and 2 are the id. (set 0, and set image 0.png for example). Sets should always be named the same
	private const string TEXTURE_BASE_PATH = "Features/Collections/Albums/{0}/Sets/{1}/Set Image {2}";

	// Backgroumd for set used in the set view in the album diaog
	private const string SET_BACKGROUND_PATH = "Features/Collections/Albums/{0}/Sets/{1}/set_background_{2}";

	private string _texturePath = "";
	public string texturePath
	{
		get
		{
			return _texturePath;
		}
		protected set
		{
			_texturePath = value;
		}
	}

	private string _backgroundPath = "";
	public string backgroundPath
	{
		get
		{
			return _backgroundPath;
		}
		protected set
		{
			_backgroundPath = value;
		}
	}

	public CollectableSetData(JSON data)
	{
		cardsInSet = new List<string>(); // This doesn't depend on data. May as well init to prevent any NRE's.

		if (data != null)
		{
			id = data.getInt("id", 0);
			keyName = data.getString("key_name", "");
			sortOrder = data.getInt("sort_order", 0);
			countsTowardAlbumCompletion = data.getBool("counts_towards_album_completion", true);
			isPowerupsSet = data.getBool("is_powerup", false);
			rewardAmount = 0;
		}
	}

	public void setPath(string newAlbumName)
	{
		albumName = newAlbumName;
		texturePath = string.Format(TEXTURE_BASE_PATH, newAlbumName, keyName, sortOrder);
		backgroundPath = string.Format(SET_BACKGROUND_PATH, newAlbumName, keyName, sortOrder);
	}

	public void markAllCardsAsSeen()
	{
		bool hasSeenNewCards = false; // If we'vev seen new cards, tell the server.
		CollectableAlbum album = Collectables.Instance.getAlbumByKey(albumName);

		//Locally mark all our cards as seen
		List<CollectableCardData> cards = Collectables.Instance.getCardsFromSet(keyName);
		for (int i = 0; i < cards.Count; i++)
		{
			if (cards[i].isNew)
			{
				album.currentNewCards--;
				hasSeenNewCards = true;
			}
			cards[i].isNew = false;
		}

		if (hasSeenNewCards)
		{
			CollectablesAction.markSetSeen(albumName, keyName);
		}
	}
}
