using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;

public class AlbumDialogSetView : MonoBehaviour
{
	[SerializeField] private GameObject contentParent;
	[SerializeField] private GameObject setViewContentPrefab;
	[System.NonSerialized]	public CollectablesSetViewContent content;
	[SerializeField] private UITexture setLogo;

	[SerializeField] private GameObject[] powerupsObjects;

	public PageController setViewPageController;
	public HowToGetCardsDialog howToGetCards;

	public AlbumDialogCardView cardView;
	private AlbumDialogCardView workingCardView;
	public TextMeshPro jackpotAmountText;
	public TextMeshPro progressText;
	public TextMeshPro headerText;

	public ButtonHandler howToGetCardsButton;
	public ButtonHandler closeButton;
	public ButtonHandler nextSetButton;
	public ButtonHandler previousSetButton;

	// So we can create copies of cards as needed.
	public CollectableCard cardLink;

	public TextMeshProMasker textMasker;

	private ForcedFlow ftue;

	private Dictionary<int, int> setIndexToRequiredTextures = new Dictionary<int, int>();

	private const int CARD_MOVE_OFFSCREEN_DIST = 2400;

	private List<CollectableCardData> collectableCards = new List<CollectableCardData>();
	private List<CollectableSetData> setDataList = new List<CollectableSetData>();
	private CollectableSetData setData;
	int setIndex = 0;
	int cardIndex = 0;

	private Dictionary<int, Dictionary<string, Texture2D>> inUseTextures = new Dictionary<int, Dictionary<string, Texture2D>>();
	[System.NonSerialized] public Dictionary<int, UIAtlas> cachedAtlases;
	
	//Need dynamic atlas/texture list for each page
	//On page init/setup, use the correct list
	//Recycle texture possibly on list/hide (recycle them)
	public void init(CollectableSetData setToUse, ForcedFlow dialogFTUE = null)
	{
		if (Collectables.usingDynamicAtlas)
		{
			cachedAtlases = new Dictionary<int, UIAtlas>();
		}
		
		setData = setToUse;
		ftue = dialogFTUE;

		if (setData == null)
		{
			Debug.LogError("Tried to create collectable set view without valid data");
		}

		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "hir_collection",
			phylum: "set",
			klass: setToUse.keyName,
			family: setToUse.albumName,
			genus: "view");
		
		howToGetCardsButton.registerEventDelegate(onClickHowToPlay);

		jackpotAmountText.text = CreditsEconomy.convertCredits(setData.rewardAmount);

		setDataList = Collectables.Instance.getSetsFromAlbum(setData.albumName);
		setIndex = Collectables.Instance.getSetsFromAlbum(setData.albumName).IndexOf(setData);
		collectableCards.Clear();
		
		closeButton.registerEventDelegate(onClickCloseSetView);

		setViewPageController.onPageViewed += onPageView;
		setViewPageController.onPageReset += onPageReset;
		
		if (setData.isPowerupsSet)
		{
			//if we are viewing in the powerups set then turn off the scrolling
			setViewPageController.isEnabled = false;
		}
		else
		{
			//if its not the powerups set, remove the powerups set from the setdata list so we cannot scroll to it from another set
			setViewPageController.onSwipeLeft += onClickNextSet;
			setViewPageController.onSwipeRight += onClickPreviousSet;

			int index = 0;
			for (int i = 0; i < setDataList.Count; i++)
			{
				if (setDataList[i].isPowerupsSet)
				{
					index = i;
					setDataList.RemoveAt(index);
					break;
				}
			}
		}

		setViewPageController.init(setViewContentPrefab, setDataList.Count, onPageSetup, setIndex);

		if (ftue != null)
		{
			setViewPageController.enabled = false;
		}

		CollectableAlbumDialog.ftueSkipped += onFtueSkipped;
	}

	private void onFtueSkipped()
	{
		setViewPageController.enabled = true;
	}

	private void bundleLoadSuccess(string assetPath, Object obj, Dict data = null)
	{
		Texture2D texture = obj as Texture2D;
		texture.name = Path.GetFileNameWithoutExtension(assetPath);
		int pageIndex = (int)data.getWithDefault(D.INDEX, 0);
		if (inUseTextures.ContainsKey(pageIndex))
		{
			if (!inUseTextures[pageIndex].ContainsKey(texture.name))
			{
				inUseTextures[pageIndex].Add(texture.name, texture);
			}
			
			if (inUseTextures[pageIndex].Count == setIndexToRequiredTextures[pageIndex])
			{
				CollectablesSetViewContent setToInit = (CollectablesSetViewContent) data.getWithDefault(D.OBJECT, null);
				setToInit.loadCards((List<CollectableCardData>)data.getWithDefault(D.DATA, null), cardLink, onClickCard, setDataList[pageIndex], textMasker,inUseTextures[pageIndex], this);
			}
		}
	}

	// Used by LobbyLoader to preload asset bundle.
	private void bundleLoadFailure(string assetPath, Dict data = null)
	{
		int pageIndex = (int)data.getWithDefault(D.INDEX, 0);
		setIndexToRequiredTextures[pageIndex]--;
		Debug.LogError("AlbumDialogSetView::bundleLoadFailure - Failed to download " + assetPath);
		// Add a missing texture?
	}

	private void onClickPreviousSet(GameObject page, int index)
	{
		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "hir_collection",
			phylum: "set",
			klass: setDataList[index].keyName,
			family: "previous",
			genus: "click");

		setDataList[index].markAllCardsAsSeen();
	}

	private void onClickNextSet(GameObject page, int index)
	{
		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "hir_collection",
			phylum: "set",
			klass: setDataList[index].keyName,
			family: "next",
			genus: "click");

		setDataList[index].markAllCardsAsSeen();
	}

	public void onClickHowToPlay(Dict args = null)
	{
		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "hir_collection",
			phylum: "how_to",
			klass: setData.keyName,
			genus: "view");
		
		Audio.play("ClickMoreInfoCollections");
		GameObject getCardDialog = NGUITools.AddChild(gameObject, howToGetCards.gameObject);
		HowToGetCardsDialog getCardDialogHandle = getCardDialog.GetComponent<HowToGetCardsDialog>();
		if (getCardDialogHandle != null)
		{
			getCardDialogHandle.currentSet = setData.keyName;
		}
	}

	// Gets dispatched from CollectablesSetViewContent. This makes handling the card view a little easier in terms
	// of placement and stuff
	public void onClickCard(Dict args = null)
	{
		CollectableCardData cardClicked = null;
		if (args != null && args.containsKey(D.DATA) && workingCardView == null)
		{
			cardClicked = args[D.DATA] as CollectableCardData;

			StatsManager.Instance.LogCount(counterName:"dialog",
				kingdom: "hir_collection",
				phylum: "card",
				family: "set",
				klass: cardClicked.keyName,
				genus: "view");
			
			cardIndex = collectableCards.IndexOf(cardClicked);
			GameObject cardGameObject = NGUITools.AddChild(gameObject, cardView.gameObject);
			AlbumDialogCardView cardViewHandle = cardGameObject.GetComponent<AlbumDialogCardView>();
			workingCardView = cardViewHandle;
			
			setViewPageController.setScrollerActive(false);
			cardViewHandle.init(cardClicked, cardIndex, collectableCards, content, ftue, content.atlas, inUseTextures[setViewPageController.currentPage], onCloseCardView);

			if (!args.containsKey(D.OBJECT))
			{
				args.Add(D.OBJECT, cardViewHandle.gameObject);
			}
		}
	}

	public void onCloseCardView()
	{
		if (setViewPageController != null)
		{
			setViewPageController.setScrollerActive(true);
		}
	}
	
	public void onClickCloseSetView(Dict args = null)
	{
		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "hir_collection",
			phylum: "set",
			klass: setData.keyName,
			family: "close",
			genus: "click");
		
		Audio.play("ClickXCollections");
		setData.markAllCardsAsSeen();
		Destroy(gameObject);
	}

	private void OnDestroy()
	{
		//remove event handlers so they don't prevent garbage collection
		setViewPageController.onPageViewed -= onPageView;
		setViewPageController.onPageReset -= onPageReset;
		setViewPageController.onSwipeLeft -= onClickNextSet;
		setViewPageController.onSwipeRight -= onClickPreviousSet;
		CollectableAlbumDialog.ftueSkipped -= onFtueSkipped;
	}

	private void onPageSetup(GameObject page, int index)
	{
		CollectablesSetViewContent pageContent = page.GetComponent<CollectablesSetViewContent>();
		pageContent.masker.centerPoint = contentParent;
		CollectableSetData pageSetData = setDataList[index];
		if (!inUseTextures.ContainsKey(index))
		{
			downloadCards(index, pageContent);
		}
		else
		{
			List<CollectableCardData> pageCardData = new List<CollectableCardData>();
			for (int i = 0; i < pageSetData.cardsInSet.Count; i++)
			{
				CollectableCardData cardData = Collectables.Instance.findCard(pageSetData.cardsInSet[i]);
				if (cardData != null)
				{
					pageCardData.Add(cardData);
				}
			}
			
			pageContent.loadCards(pageCardData, cardLink, onClickCard, setDataList[index], textMasker, inUseTextures[index], this);
		}
	}

	private void downloadCards(int pageIndex, CollectablesSetViewContent setViewContent)
	{
		List<int> borderList = new List<int>();
		List<CollectableCardData> pageCardData = new List<CollectableCardData>();
		int setCardsToLoad = 12;
		Dict data = Dict.create(D.OBJECT, setViewContent, D.INDEX, pageIndex, D.DATA, pageCardData);

		if (!inUseTextures.ContainsKey(pageIndex))
		{
			inUseTextures[pageIndex] = new Dictionary<string, Texture2D>();
		}
		
		if (!setIndexToRequiredTextures.ContainsKey(pageIndex))
		{
			setIndexToRequiredTextures.Add(pageIndex, setCardsToLoad);
		}

		CollectableSetData pageSetData = setDataList[pageIndex];
		for (int i = 0; i < pageSetData.cardsInSet.Count; i++)
		{
			CollectableCardData cardData = Collectables.Instance.findCard(pageSetData.cardsInSet[i]);
			
			if (cardData != null)
			{
				pageCardData.Add(cardData);

				if (!setData.isPowerupsSet)
				{
					if (!borderList.Contains(cardData.rarity))
					{
						if (cardData.rarity == 0)
						{
							cardData.rarity = 1;
							Debug.LogError("Bad card rarity on card " + cardData.keyName);
						}

						borderList.Add(cardData.rarity);
						setIndexToRequiredTextures[pageIndex]++;

						AssetBundleManager.load(CollectAPack.CARD_FRAME_PATH + cardData.rarity, bundleLoadSuccess,
							bundleLoadFailure, data, isSkippingMapping: true, fileExtension: ".png");
					}

					AssetBundleManager.load(cardData.texturePath, bundleLoadSuccess, bundleLoadFailure, data,
						isSkippingMapping: true, fileExtension: ".png");
				}
			}
			else
			{
				Debug.LogError("Missing card " + setData.cardsInSet[i]);
			}
		}

		if (setData.isPowerupsSet)
		{
			setIndexToRequiredTextures[pageIndex] = 1;
			AssetBundleManager.load(CollectAPack.CARD_FRAME_POWERUPS, bundleLoadSuccess,
				bundleLoadFailure, data, isSkippingMapping: true, fileExtension: ".png");
		}
	}
	
	private void onPageView(GameObject page, int index)
	{
		if (setDataList == null || index > Collectables.Instance.getSetsFromAlbum(setData.albumName).Count-1 || index < 0)
		{
			Bugsnag.LeaveBreadcrumb("Tried to view invalid page index");
			return;
		}
		
		CollectableSetData pageSetData = setDataList[index];
		content = page.GetComponent<CollectablesSetViewContent>();
		int numOfCardsCollected = 0;
		collectableCards.Clear();
		for (int i = 0; i < pageSetData.cardsInSet.Count; i++)
		{
			CollectableCardData cardData = Collectables.Instance.findCard(pageSetData.cardsInSet[i]);
			
			if (cardData != null)
			{
				collectableCards.Add(cardData);
				if (cardData.isCollected)
				{
					numOfCardsCollected++;
				}
			}
		}
		if (numOfCardsCollected == pageSetData.cardsInSet.Count)
		{
			headerText.text = "Set Complete!";
			headerText.color = new Color(0.0f, 1.0f, 0.0f);
		}
		else
		{
			headerText.text = Localize.text("collection_complete_to_win");
			headerText.color = new Color(1.0f, 1.0f, 1.0f);
		}

		progressText.text = string.Format("{0}/{1} Collected", numOfCardsCollected , pageSetData.cardsInSet.Count);

		if (setData.isPowerupsSet)
		{
			for (int i = 0; i < powerupsObjects.Length; i++)
			{
				powerupsObjects[i].SetActive(true);
			}
			howToGetCardsButton.gameObject.SetActive(false);
			setLogo.gameObject.SetActive(false);
		}
		else
		{
			AssetBundleManager.load(pageSetData.texturePath, setLogoLoadSuccess, setLogoLoadfailed);
			for (int i = 0; i < powerupsObjects.Length; i++)
			{
				powerupsObjects[i].SetActive(false);
			}
			howToGetCardsButton.gameObject.SetActive(true);
			setLogo.gameObject.SetActive(true);
		}
		
		nextSetButton.gameObject.SetActive(index != setDataList.Count - 1 && !setData.isPowerupsSet);
		previousSetButton.gameObject.SetActive(index > 0 && !setData.isPowerupsSet);
		jackpotAmountText.text = CreditsEconomy.convertCredits(pageSetData.rewardAmount);
	}

	private void onPageReset(GameObject page, int index)
	{
		CollectablesSetViewContent contentToReset = page.GetComponent<CollectablesSetViewContent>();
		contentToReset.releaseCardsToObjectPool();
	}
	
	private void setLogoLoadSuccess(string assetPath, Object obj, Dict data = null)
	{
		setLogo.gameObject.SetActive(false);
		Material material = new Material(setLogo.material.shader);
		material.mainTexture = obj as Texture2D;
		setLogo.material = material;
		setLogo.gameObject.SetActive(true);
	}
	
	private void setLogoLoadfailed(string assetPath, Dict data = null)
	{
		Debug.LogError("CollectableSetViewContent::bundleLoadFailure - Failed to download " + assetPath);
	}
}