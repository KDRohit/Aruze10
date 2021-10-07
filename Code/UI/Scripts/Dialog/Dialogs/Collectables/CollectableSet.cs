using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;

// These are the set images used in the album dialog. All the sprites are loaded on the fly.
public class CollectableSet : MonoBehaviour, IResetGame
{
	public CollectableSetData data;

	public ButtonHandler setButton;

	// Holds the notif bubble and text. Makes it easier to turn stuff on and off
	public GameObject notifParent;
	public GameObject completeParent;
	public GameObject inProgressParent;

	public UISprite mainImage;
	public UISprite setContainerSprite;
	public UITexture setImageUITexture;
	public UITexture setContainerTexture;

	public TextMeshPro newCardsLabel;
	public TextMeshPro cardCount;

	public static bool setButtonEnabled = true;

	[System.NonSerialized] public int count = 0;

	TextMeshPro notifCount;

	public static CollectableAlbumDialog dialogHandle = null;

	private const string BACKROUND_IMAGE_NAME = "setbackground";
	private const string NOTIF_IMAGE_NAME = "notif_background";
	private const string TAG_IMAGE_NAME = "tag_image";
	private const string SET_CONTAINER_PATH = "Card Set Container";

	public virtual void setup(CollectableSetData dataToUse, UIAtlas atlas = null,
		Dictionary<string, Texture2D> setTextures = null)
	{
		if (dataToUse == null)
		{
			Debug.LogError("CollectableSet::setup - CollectableSetData was null");
			return;
		}
		else
		{
			data = dataToUse;
		}
		// These might be irrelevant. It might be nice if we could just have a small atlas with everything on it that we use here.
		// then dynamically set the sprite names.
		if (atlas != null)
		{
			mainImage.atlas = atlas;
			mainImage.spriteName = Path.GetFileName(data.texturePath);
			mainImage.depth = 0;
	
			setContainerSprite.atlas = atlas;
			setContainerSprite.spriteName = "Card Set Container";
			setContainerSprite.depth = -1;
		}
		else if (setTextures != null)
		{
			mainImage.gameObject.SetActive(false);
			setContainerSprite.gameObject.SetActive(false);
	
			string path = Path.GetFileName(data.texturePath);
			if (!string.IsNullOrEmpty(path) && setTextures.ContainsKey(path))
			{
				Material setMaterial = new Material(setImageUITexture.material.shader);
				setMaterial.mainTexture = setTextures[path];
				setImageUITexture.material = setMaterial;
				setImageUITexture.gameObject.SetActive(true);
			}

			if (setTextures.ContainsKey(SET_CONTAINER_PATH))
			{
				Material setContainerMaterial = new Material(setContainerTexture.material.shader);
				setContainerMaterial.mainTexture = setTextures[SET_CONTAINER_PATH];
				setContainerTexture.material = setContainerMaterial;
				setContainerTexture.gameObject.SetActive(true);
			}
		}
		setCardsCount();
		setButton.registerEventDelegate(onClickSet);
	}

	protected virtual void setCardsCount()
	{
		int newCount = 0;
		CollectableCardData cardData;
		int cardsOwned = 0;
		for (int i = 0; i < data.cardsInSet.Count; i++)
		{
			cardData = Collectables.Instance.findCard(data.cardsInSet[i]);
			if (cardData.isNew)
			{
				newCount++;
			}
			if (cardData.isCollected)
			{
				cardsOwned++;
			}
		}

		count = cardsOwned + newCount;

		if (notifCount != null)
		{
			notifCount.text = CommonText.formatNumber(newCount);
		}

		if (completeParent != null && inProgressParent != null)
		{
			completeParent.SetActive(cardsOwned == data.cardsInSet.Count);
			inProgressParent.SetActive(cardsOwned < data.cardsInSet.Count);
			
			if (newCount > 0 && !completeParent.activeSelf
			) //Turn on the new badge if we have new cards but haven't completed the set
			{
				notifParent.SetActive(true);
				newCardsLabel.text = newCount.ToString();
			}
		}

		cardCount.text = string.Format("{0}/{1}", cardsOwned, data.cardsInSet.Count);
	}

	protected void onClickSet(Dict args = null)
	{
		if(!setButtonEnabled || dialogHandle.manualFtueStart)
		{
			return;
		}
		
		if (dialogHandle != null && data != null)
		{
			Audio.play("ClickToOpenCollections");
			dialogHandle.loadAndShowSetCards(data);
		}
		else if (data == null)
		{
			Debug.LogError("Clicked on a set with null data");
		}
		else
		{
			Debug.LogError("Dialog reference was null");
		}

		if (notifParent != null)
		{
			notifParent.gameObject.SetActive(false);
		}

		// Transition to card view. We may need to do this at the dialog level for the sake of loading things. Though even a 
		// reference to the open dialog might be fine.
	}

	public void updateSetCounts()
	{
		CollectableCardData cardData;
		int newCardCount = 0;
		for (int i = 0; i < data.cardsInSet.Count; i++)
		{
			cardData = Collectables.Instance.findCard(data.cardsInSet[i]);
			if (cardData.isNew)
			{
				newCardCount++;	
			}
		}
		notifParent.SetActive(newCardCount > 0 && !completeParent.activeSelf);
		newCardsLabel.text = CommonText.formatNumber(newCardCount);
	}

	public static void resetStaticClassData()
	{
		setButtonEnabled = true;
	}
}
