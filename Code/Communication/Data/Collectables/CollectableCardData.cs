using System;
using UnityEngine;

public class CollectableCardData
{
	public string id; //Card ### that appears in the bottom corner
	public int sortOrder; //Order for how the cards are displayed in the set
	public string keyName; //Format ###_setname_albumname
	public int rarity;
	public bool isCollected; //Is this card in our collection?
	public bool isNew; //Has this card been seen in the dialog?
	public string setKey;
	public string albumName; // Cards should know what album they belong to in the same way that sets know what cards they have.

	// 0 = album name, 1 = set number, 2 = texture name.
	private const string TEXTURE_BASE_PATH = "Features/Collections/Albums/{0}/Sets/{1}/{2}";

	string _texturePath = "";
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

	public CollectableCardData(JSON data)
	{
		if (data != null)
		{
			keyName = data.getString("key_name", "");
			sortOrder = data.getInt("sort_order", 0);
			rarity = data.getInt("rarity", 0);
			isCollected = false;
			isNew = false;

			string[] keyNameSplit = keyName.Split('_');
			id = keyNameSplit[0]; //This assumes the card ID is the first part of the keyName
		}
	}

	public bool isPowerup
	{
		get { return !string.IsNullOrEmpty(keyName) && PowerupBase.collectablesPowerupsMap.ContainsKey(keyName); }
	}

	public void setPath(string newAlbumName, string newSetName)
	{
		setKey = newSetName;
		albumName = newAlbumName; 
		texturePath = string.Format(TEXTURE_BASE_PATH, newAlbumName, setKey, keyName);
	}

	public override string ToString()
	{
		System.Text.StringBuilder builder = new System.Text.StringBuilder();
		builder.AppendLine("Card ID: " + this.id);
		builder.AppendLine("Sort Order: " + this.sortOrder);
		builder.AppendLine("Key Name: " + this.keyName);
		builder.AppendLine("Rarity: " + this.rarity);
		builder.AppendLine("Is Collected?: " + this.isCollected);
		builder.AppendLine("Is New?: " + this.isNew);

		return builder.ToString();
	}
}
