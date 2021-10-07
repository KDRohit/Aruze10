using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AlbumDialogCardView : MonoBehaviour 
{
	public GameObject contentParent;
	public ButtonHandler closeButton;
	public ButtonHandler nextCard;
	public ButtonHandler previousCard;
	public GameObject cardViewPrefab;

	public TextMeshProMasker masker; // Needed?
	public TextMeshPro legalText;

	private ForcedFlow ftue;

	public CollectableCardData currentData;

	[SerializeField] private PageController cardPageController;
	private CardViewContent currentContent;

	private List<CollectableCardData> cardDataList;
	// so we can flip between them easily.
	private int currentCardIndex = -1;

	private UIAtlas setAtlas = null;
	private Dictionary<string, Texture2D> nonAtlasedTextureDict = null;
	private CollectablesSetViewContent parentSetViewContent;
	private System.Action closeCardViewAction = null;

	public void init
	(
		CollectableCardData cardData,
		 int startingIndex,
		List<CollectableCardData> collectableCards,
		CollectablesSetViewContent parentContent,
		ForcedFlow dialogFTUE = null,
		UIAtlas setAtlasToShow = null,
		Dictionary<string, Texture2D> nonAtlasedTextures = null,
		System.Action onCloseCardView = null
	)
	{
		if (onCloseCardView != null)
		{
			closeCardViewAction = onCloseCardView;
		}

		if (closeButton != null)
		{
			closeButton.registerEventDelegate(onClickCloseCardView);
		}

		cardDataList = collectableCards;
		parentSetViewContent = parentContent;
		if (cardData != null)
		{
			currentData = cardData;
			if (Localize.keyExists(cardData.setKey + "_legal"))
			{
				legalText.text = Localize.text(cardData.setKey + "_legal");
			}
			else
			{
				legalText.gameObject.SetActive(false);
			}
		}
		else
		{
			Debug.LogError("Trying to view card details with NULL card data");
		}

		setAtlas = setAtlasToShow;
		
		nonAtlasedTextureDict = nonAtlasedTextures;

		if (startingIndex >= 0 && collectableCards != null) //Only set this up if we actually can swipe through cards
		{
			cardPageController.onPageViewed += onPageView;

			if (nextCard != null && previousCard != null)
			{
				cardPageController.onSwipeLeft += onClickNextCard;
				cardPageController.onSwipeRight += onClickPreviousCard;
			}

			cardPageController.init(cardViewPrefab, cardDataList.Count, onPageSetup, startingIndex);
		}
	}


	public void togglePageController(bool setActive)
	{
		cardPageController.enabled = setActive;
	}
	
	private void onPageSetup(GameObject page, int index)
	{
		CardViewContent	content = page.GetComponent<CardViewContent>();
		CollectableCardData cardToShow = cardDataList[index];

		bool isPowerupCard = PowerupBase.collectablesPowerupsMap.ContainsKey(cardToShow.keyName);
		content.cardOnDisplay.setup(cardToShow, setAtlas, nonAtlasedTextureDict);
		content.earnedStateObjects.SetActive(cardToShow.isCollected || isPowerupCard);
		content.unearnedStateObjects.SetActive(!(cardToShow.isCollected || isPowerupCard));
	}

	private void onPageView(GameObject page, int index)
	{
		currentContent = page.GetComponent<CardViewContent>();
		currentData = cardDataList[index];

		if (nextCard != null)
		{
			nextCard.gameObject.SetActive(index != cardDataList.Count - 1);
		}

		if (previousCard != null)
		{
			previousCard.gameObject.SetActive(index != 0);
		}
		if (parentSetViewContent != null)
		{
			parentSetViewContent.markCardSeen(cardDataList[index].keyName);
		}
	}

	public void onClickCloseCardView(Dict args = null)
	{
		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "hir_collection",
			phylum: "card",
			klass: currentData.keyName,
			family: "close",
			genus: "click");

		Audio.play("ClickXCollections");

		if (closeCardViewAction != null)
		{
			closeCardViewAction();
		}
		
		Destroy(gameObject);		
	}

	private void onClickNextCard(GameObject page, int index)
	{
		Audio.play("ClickRightCollections");
		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "hir_collection",
			phylum: "card",
			klass: cardDataList[index].keyName,
			family: "next",
			genus: "click");
	}

	private void onClickPreviousCard(GameObject page, int index)
	{
		Audio.play("ClickLeftCollections");
		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "hir_collection",
			phylum: "card",
			klass: cardDataList[index].keyName,
			family: "previous",
			genus: "click");
	}

	public void resetCard()
	{
		currentContent.cardOnDisplay.reset();
	}
}