using UnityEngine;
using System.Collections;
using TMPro;

public class PaytableBonus : TICoroutineMonoBehaviour
{
	public UITexture icon;
	public TextMeshPro title;
	public TextMeshPro description;
	private string basicImagePath;
	private string commonImagePath;
	private const string PAYTABLE_IMAGE_POSTFIX = "_paytable";

	public void init(string gameKey, string bonusGameKey)
	{
		BonusGame game = BonusGame.find(bonusGameKey);
		if (game == null)
		{
			Debug.LogError("Could not find BonusGame " + bonusGameKey + " in PaytableBonus.");
			return;
		}

		// Set the icon for this glyph:
		this.icon.gameObject.SetActive(false);	// Hide until we have a valid texture.

		// Correct the image path from a URL designed for the web version of the game:
		string imageBaseName = PaytableBonus.getPaytableBonusImageBasename(game);
		SlotResourceMap.createPaytableImage(gameKey, imageBaseName, gamePicLoaded, gamePicFailed);

		// Text:
		this.title.text = game.name;
		this.description.text = game.description;
	}

	// Callback for the bonus icon:
//	private void gamePicLoaded(Texture2D tex, Dict data)
	private void gamePicLoaded(string filename, Object texObject, Dict data)
	{
		if (texObject == null)
		{
			return;
		}

		Texture tex = texObject as Texture;
		if (tex == null)
		{
			return;
		}

		NGUIExt.applyUITexture(this.icon, tex);
		this.icon.alpha = 0.0f;
		this.icon.gameObject.SetActive(true);
		TweenAlpha.Begin(this.icon.gameObject, 0.5f, 1.0f);
	}

	private void gamePicFailed(string filename, Dict data)
	{
		if (data != null)
		{
			string basicImagePath = data[D.OPTION1] as string;
			string commonImagePath = data[D.IMAGE_PATH] as string;
			Debug.LogWarning("Failed to load: " + basicImagePath + " and " + commonImagePath);
		}
		else
		{
			Debug.LogError("Failed to find paytable image.");
		}
	}

	public void hide()
	{
		this.gameObject.SetActive(false);
	}

	public static string getPaytableBonusImageBasename(BonusGame bonusGameData)
	{
		string imageBaseName = System.IO.Path.GetFileName(bonusGameData.paytableImage);
		if (imageBaseName.Contains('.'))
		{
			imageBaseName = imageBaseName.Substring(0, imageBaseName.IndexOf('.')); // Remove the .whatever from the end.
		}
		if (!string.IsNullOrEmpty(imageBaseName))
		{
			imageBaseName = imageBaseName + PAYTABLE_IMAGE_POSTFIX; // Add the postfix to the end.
		}
		return imageBaseName;
	}
}
