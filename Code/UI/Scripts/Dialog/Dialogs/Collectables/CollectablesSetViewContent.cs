using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class CollectablesSetViewContent : MonoBehaviour 
{
	public GameObject cardParent;

	public TextMeshProMasker masker;
	public UITexture backgroundRenderer;
	[System.NonSerialized] public bool isFinishedLoadedCards = false;
	[System.NonSerialized] public CollectableCard firstCard;
	[System.NonSerialized] public UIAtlas atlas;

	private Dictionary<string, CollectableCard> currentCards = new Dictionary<string, CollectableCard>();

	private const int CARD_SPACING_X = 400;
	private const int CARD_SPACING_Y = 500;
	private const int CARDS_PER_ROW = 6;

	private GameObjectCacher cardCacher;
	private List<GameObject> inUseCards = new List<GameObject>();

	public void loadCards(List<CollectableCardData> collectableCards, CollectableCard cardLink, ClickHandler.onClickDelegate onClickAction, CollectableSetData setData, TextMeshProMasker textMasker, Dictionary<string, Texture2D> nonAtlasedTextures, AlbumDialogSetView setViewParent)
	{
		if (cardCacher == null)
		{
			cardCacher = new GameObjectCacher(cardParent, cardLink.gameObject, true);
		}

		if (this == null || this.gameObject == null)
		{
			isFinishedLoadedCards = true;
			return;
		}

		int xLocationToPlaceSet = 0;
		List<TextMeshPro> cardTMProObjects = new List<TextMeshPro>();

		if (Collectables.usingDynamicAtlas)
		{
			UIAtlas setAtlas; 
			if (setViewParent.cachedAtlases.TryGetValue(setData.sortOrder, out setAtlas))
			{
				atlas = setAtlas;
			}
			else
			{
				atlas = DynamicAtlas.createAndAttachAtlas(nonAtlasedTextures.Values.ToArray(), setViewParent.gameObject, setData.keyName, 512);
				setViewParent.cachedAtlases.Add(setData.sortOrder, atlas);
				AssetBundleManager.Instance.markBundleForUnloading("collections_" + setData.keyName);
			}
		}

		CollectableCard cardHandle;
		collectableCards.Sort(cardSort);

		for (int i = 0; i < collectableCards.Count; i++)
		{
			// create a set image.
			GameObject cardGameObject = cardCacher.getInstance();
			inUseCards.Add(cardGameObject);
			cardHandle = cardGameObject.GetComponent<CollectableCard>();
			cardGameObject.SetActive(true);
			if (cardHandle != null)
			{
				// Setup should load whatever we need. But we could avoid doing a dynamic atlas and just put 
				// set images on their own atlas along with their background images and whatnot.
				cardHandle.init(collectableCards[i], CollectableCard.CardLocation.SET_VIEW, atlas, nonAtlasedTextures);
				masker.addObjectToList(cardHandle.cardIDLabel);
				masker.addObjectToList(cardHandle.titleLabel);
				if (!currentCards.ContainsKey(collectableCards[i].keyName))
				{
					currentCards.Add(collectableCards[i].keyName, cardHandle);
				}

				if (collectableCards[i].isNew && !PowerupBase.collectablesPowerupsMap.ContainsKey(collectableCards[i].keyName))
				{
					cardHandle.loadStaticNewBadge();
				}
				else
				{
					cardHandle.hideNewBadge();
				}

				if (firstCard == null && (collectableCards[i].isCollected || i == collectableCards.Count-1))
				{
					firstCard = cardHandle;	
				}

				Dict args = Dict.create(D.DATA, collectableCards[i]);
				cardHandle.onClickButton.clearAllDelegates();
				cardHandle.onClickButton.registerEventDelegate(onClickAction, args);
			}

			CommonTransform.setX(cardGameObject.transform, xLocationToPlaceSet);
			CommonTransform.setY(cardGameObject.transform, -((i / CARDS_PER_ROW) * CARD_SPACING_Y)); // negative because it decends down

			xLocationToPlaceSet += CARD_SPACING_X;

			if (xLocationToPlaceSet >= CARD_SPACING_X * CARDS_PER_ROW)
			{
				xLocationToPlaceSet = 0;
			}

			cardTMProObjects.AddRange(cardGameObject.GetComponentsInChildren<TextMeshPro>());
		}

		isFinishedLoadedCards = true;
		masker.addObjectArrayToList(cardTMProObjects.ToArray());
		AssetBundleManager.load(setData.backgroundPath, bundleLoadSuccess, bundleLoadFailure);
	}

	private void bundleLoadSuccess(string assetPath, Object obj, Dict data = null)
	{
		if (this == null || this.gameObject == null)
		{
			return;
		}
		backgroundRenderer.gameObject.SetActive(false);
		Material material = new Material(backgroundRenderer.material.shader);
		material.mainTexture = obj as Texture2D;
		backgroundRenderer.material = material;
		backgroundRenderer.gameObject.SetActive(true);
	}

	public void markCardSeen(string cardName)
	{
		CollectableCard card;
		if (currentCards.TryGetValue(cardName, out card))
		{
			card.newBadgeParent.SetActive(false);
		}
	}

	// Used by LobbyLoader to preload asset bundle.
	private void bundleLoadFailure(string assetPath, Dict data = null)
	{
		Debug.LogError("CollectableSetViewContent::bundleLoadFailure - Failed to download " + assetPath);
		// Add a missing texture?
	}

	public void releaseCardsToObjectPool()
	{
		for (int i = 0; i < inUseCards.Count; i++)
		{
			cardCacher.releaseInstance(inUseCards[i]);
		}
		
		inUseCards.Clear();

	}

	public static int cardSort(CollectableCardData a, CollectableCardData b)
	{
		// old rarity sort
		// return a.rarity.CompareTo(b.rarity);

		int aID = a.sortOrder;
		int bID = b.sortOrder;
		return aID.CompareTo(bID);
	}

	private void OnDestroy()
	{
		masker = null;
	}
}
