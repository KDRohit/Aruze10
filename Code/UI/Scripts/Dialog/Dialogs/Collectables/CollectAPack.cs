using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

// Gets added to an object then play sequence gets called ideally with the JSON that contains the info about what's in the pack any anything else relevant to us
public class CollectAPack : MonoBehaviour
{
	private const string COLLECTION_ANIMATION_NORMAL = "Collections ani";
	private const string COLLECTION_ANIMATION_SPECIAL = "extra bonus";

	// I imagine most things can directly link the prefab, but in case we need this on the fly lets store the path
	public const string PACK_OPENING_PRESENTATION_PATH = "Features/Collections/Prefabs/Collections Pack Award";

	// Card pac sprite path can be stored here.
	public const string CARD_FRAME_PATH = "Features/Collections/Prefabs/Card Assets/Card Frames/Card Frame ";

	//All powerups cards share the same frame
	public const string CARD_FRAME_POWERUPS = "Features/Collections/Prefabs/Card Assets/Card Frames/Card Frame 6 PowerUps";

	// 0 is the album name, 1 is the texture name for the album symbol. (greatest hits, movie reels, idk)
	private const string THEME_PATH_BASE = "Features/Collections/Albums/{0}/{1}";

	public Renderer[] packLogos; // Already exists by default
	public UISprite packSprite; // Already exists by default
	public UISprite packSpriteLeft;
	public UISprite packSpriteRight;
	public CollectableCardAnimated baseCard; // Needs to be instaniated as needed
	public Animator packAnimator;
	public Transform firstCardTarget;
	public CollectionsDuplicateMeter starMeter;
	public GameObject cardParent;
	public GameObject animatedStarPrefab;
	public GameObject starParticlePrefab;
	public Animator bottomContentAnimator;
	public Animator topContentIntroAnimation;
	public SlideController slideController;
	
	
	[SerializeField] private Transform detailedCardViewParent;
	[SerializeField] private AlbumDialogCardView cardViewPrefab;

	// These textures need to get packed into their own atlas.
	Dictionary<string, Texture2D> loadedTexturesDict = new Dictionary<string, Texture2D>();

	private UIAtlas createdAtlas = null;

	// Used so we know when to stop loading stuff.
	int numberOfImagesToLoad = 0;

	private List<CollectableCardData> cardsToShow = new List<CollectableCardData>();
	private GameObjectCacher starCache = null;
	private GameObjectCacher bustCache = null;
	private List<string> loadedBundles = new List<string>();
	private AlbumDialogCardView createdCardView = null;

	//Animation name consts
	public const string STAR_PACK_COLLECT_ANIM = "extra bonus pack open";
	public const string NORMAL_PACK_COLLECT_ANIM = "Collection FTUE ani";

	//Pack sprite consts
	public const string DEFAULT_PACK_COLOR = "blue";
	public const string TWO_STAR_PACK_COLOR = "bronze";
	public const string THREE_STAR_PACK_COLOR = "silver";
	public const string FOUR_STAR_PACK_COLOR = "gold";
	public const string FIVE_STAR_PACK_COLOR = "crystal";
	public const string WILD_CARD_PACK_COLOR = "wild";

	private const string PACK_LEFT_SPRITE_POSTFIX = " open L";
	private const string PACK_RIGHT_SPRITE_POSTFIX = " open R";

	//Animation/Tween delay consts
	private const float STAR_PACK_OPEN_ANIM_WAIT = 0.55f;
	private const float NORMAL_PACK_OPEN_ANIM_WAIT = 0.55f;
	private const float CARD_TWEEN_STAGGER_WAIT = 0.1f;

	private const int CARD_SPACING_X = 525;
	private const int MAX_CARDS_ON_SCREEN = 4; //Don't need scrolling until we're over this amount
	private const float NO_SLIDING_CENTER_OFFSET = 282.5f;

	private int finalStarCount = 0;

	public delegate void onCardImagesLoaded(Dict args = null);
	public event onCardImagesLoaded onCardsReady;

	public delegate void onCardAnimationsFinishedDelegate(Dict args = null);
	public event onCardAnimationsFinishedDelegate onCardAnimationsFinished;

	public void setupForMOTD()
	{
		topContentIntroAnimation.Play("intro");
	}

	public void togglePowerupInfoButton(List<CollectableCardAnimated> cards, bool enabled)
	{
		for (int i = 0; i < cards.Count; ++i)
		{
			if (cards[i].loadedCard.powerupObject != null)
			{
				PowerupCardItem powerupCard = cards[i].loadedCard.powerupObject.GetComponent<PowerupCardItem>();
				if (cards[i].loadedCard.isPowerup && powerupCard != null && powerupCard)
				{
					powerupCard.setActionButton(enabled);
				}
			}
		}
	}

	// Gets called once we know what cards to load.
	public IEnumerator playPackSequence(string openAnimationName, List<CollectableCardAnimated> cards, int[] newCardIndices)
	{
		togglePowerupInfoButton(cards, false);

		// play animations
		starCache = new GameObjectCacher(starMeter.starParent.gameObject, animatedStarPrefab);
		bustCache = new GameObjectCacher(starMeter.starParent.gameObject, starParticlePrefab);

		if (packAnimator != null)
		{
			packAnimator.Play(openAnimationName);
		}

		float packOpenwaitTime = 0f;
		switch (openAnimationName)
		{
		case NORMAL_PACK_COLLECT_ANIM:
			packOpenwaitTime = NORMAL_PACK_OPEN_ANIM_WAIT;
			break;

		case STAR_PACK_COLLECT_ANIM:
			packOpenwaitTime = STAR_PACK_OPEN_ANIM_WAIT;
			break;

		default:
			packOpenwaitTime = 0.0f;
			break;
		}

		yield return new WaitForSeconds(packOpenwaitTime);

		for (int i = 0; i < cards.Count; i++)
		{
			int horizontalOffset = (cards.Count - 1 - i) * CARD_SPACING_X;
			cards[i].gameObject.transform.localPosition += new Vector3(0.0f, 0.0f, -12.0f * (cards.Count - 1 - i)); //Staircasing the cards to prevent overlapping
			if (i != cards.Count-1)
			{
				StartCoroutine(cards[i].animateAndTweenCard(openAnimationName, horizontalOffset, firstCardTarget, starCache, bustCache, starMeter, finalStarCount));
				yield return new WaitForSeconds(CARD_TWEEN_STAGGER_WAIT); //Stagger the card animations
			}
			else
			{
				yield return StartCoroutine(cards[i].animateAndTweenCard(openAnimationName, horizontalOffset, firstCardTarget, starCache, bustCache, starMeter, finalStarCount));
			}
		}
			
		if (onCardAnimationsFinished != null)
		{
			if (starMeter.toolTipButton != null && starMeter.toolTipButton.gameObject != null)
			{
				starMeter.toolTipButton.gameObject.SetActive(true);
			}

			onCardAnimationsFinished();	
		}

		if (newCardIndices.Length > 0)
		{
			GameTimerRange.createWithTimeRemaining(3).registerFunction(playNewCardBounce, Dict.create(D.COLLECTABLE_CARDS, cards, D.DATA, newCardIndices));
		}

		togglePowerupInfoButton(cards, true);
	}

	private void playNewCardBounce(Dict args, GameTimerRange sender)
	{
		if (this != null)
		{
			int[] newCardIndices = (int[]) args.getWithDefault(D.DATA, null);
			List<CollectableCardAnimated> droppedCards = (List<CollectableCardAnimated>) args.getWithDefault(D.COLLECTABLE_CARDS, null);
			int cardIndex = Random.Range(0, droppedCards.Count);
			droppedCards[cardIndex].loadedCard.playNewCardBounce();
			GameTimerRange.createWithTimeRemaining(3).registerFunction(playNewCardBounce, args);
		}
	}

	// Download pack image
	public void preparePackSequence(JSON packData)
	{
		string albumKey = packData.getString("album_id", "1");

		CollectableAlbum album = Collectables.Instance.getAlbumByKey(Collectables.currentAlbum);

		Dict args = Dict.create(D.DATA, packData);

		// Download pack art then download the rest of the cards
		AssetBundleManager.load(album.logoTexturePath, packArtLoadSuccess, bundleLoadFailure, data: args, isSkippingMapping: true, fileExtension:".png");
	}

	public void preparePackSequence(List<CollectableCardData> collectedCards, string packColor = DEFAULT_PACK_COLOR)
	{

		if (collectedCards == null || collectedCards.Count == 0)
		{
			Debug.LogError("No cards in pack");
			bundleLoadFailure(null);
			return;
		}

		if (packColor != DEFAULT_PACK_COLOR)
		{
			packSprite.spriteName = packColor;
			packSpriteLeft.spriteName = packColor + PACK_LEFT_SPRITE_POSTFIX;
			packSpriteRight.spriteName = packColor + PACK_RIGHT_SPRITE_POSTFIX;
		}

		string albumKey = collectedCards[0].albumName;
		if(starMeter.toolTipButton != null)
		{
			starMeter.toolTipButton.gameObject.SetActive(false);
		}
		CollectableAlbum album = Collectables.Instance.getAlbumByKey(albumKey);
		if (Collectables.showStarMeterToolTip)
		{
			bool hasDuplicate = false;
			for (int i = 0; i < collectedCards.Count; i++)
			{
				if (collectedCards[i].isCollected)
				{
					hasDuplicate = true;
					break;
				}
			}

			if (!hasDuplicate)
			{
				starMeter.gameObject.SetActive(false);
			}
		}
		Dict args = Dict.create(D.COLLECTABLE_CARDS, collectedCards);
		AssetBundleManager.load(album.logoTexturePath, packArtLoadSuccess, bundleLoadFailure, data: args, isSkippingMapping:true, fileExtension:".png");
	}

	// once we have the pack image, start grabbing cards
	private void packArtLoadSuccess(string assetPath, Object obj, Dict data = null)
	{
		if (this == null || this.gameObject == null)
		{
			return;
		}

		if (packLogos != null && packLogos.Length > 0)
		{
			for (int i = 0; i < packLogos.Length; i++)
			{
				Material material = new Material(packLogos[i].material.shader);
				material.mainTexture = obj as Texture2D;
				packLogos[i].material = material;
			}
		}

		string[] assetPathSplitStrings = assetPath.Split('.');
		if (assetPathSplitStrings != null)
		{
			string filePath = assetPath.Split('.')[0];
			string bundleName = AssetBundleManager.getBundleNameForResource(filePath);
			if (!bundleName.IsNullOrWhiteSpace() && !loadedBundles.Contains(bundleName))
			{
				loadedBundles.Add(bundleName);
			}
		}

		JSON packData = data.getWithDefault(D.DATA, null) as JSON;
		List<int> packTypes = new List<int>();
		if (packData == null)
		{
			cardsToShow = (List<CollectableCardData>)data.getWithDefault(D.COLLECTABLE_CARDS, null);
			if (cardsToShow != null)
			{
				numberOfImagesToLoad = cardsToShow.Count;
				if (numberOfImagesToLoad > 0)
				{
					for (int i = 0; i < cardsToShow.Count; i++)
					{
						if (!packTypes.Contains(cardsToShow[i].rarity))
						{
							if (cardsToShow[i].rarity == 0)
							{
								cardsToShow[i].rarity = 1;
								Debug.LogError("Bad card rarity on card " + cardsToShow[i].keyName);
							}

							numberOfImagesToLoad++;
							packTypes.Add(cardsToShow[i].rarity);

							AssetBundleManager.load(CARD_FRAME_PATH + cardsToShow[i].rarity, bundleLoadSuccess, cardLoadFailure, isSkippingMapping:true, fileExtension:".png");
						}

						//if there is a powerup card, load the powerups card frame
						if (PowerupBase.collectablesPowerupsMap.ContainsKey(cardsToShow[i].keyName))
						{
							AssetBundleManager.load(CARD_FRAME_POWERUPS, bundleLoadSuccess, cardLoadFailure, isSkippingMapping:true, fileExtension:".png");
						}
						else
						{
							AssetBundleManager.load(cardsToShow[i].texturePath, bundleLoadSuccess, cardLoadFailure, isSkippingMapping:true, fileExtension:".png");
						}
					}
				}
				else
				{
					if (onCardsReady != null)
					{
						onCardsReady();
					}
				}

			}
			else
			{
				Debug.LogError("Cards to show is null");

				if (onCardsReady != null)
				{
					onCardsReady();
				}
			}
		}
		else
		{
			string[] cards = packData.getStringArray("cards");
			numberOfImagesToLoad = cards.Length;
			if (numberOfImagesToLoad > 0)
			{
				string cardKey = "";
				for (int i = 0; i < cards.Length; i++)
				{
					cardKey = cards[i];
					CollectableCardData cardData = Collectables.Instance.findCard(cardKey);

					if (cardData != null)
					{
						if (cardData.rarity == 0)
						{
							cardData.rarity = 1;
							Debug.LogError("Bad card rarity on card " + cardData.keyName);
						}

						if (!packTypes.Contains(cardData.rarity))
						{
							numberOfImagesToLoad++;
							packTypes.Add(cardData.rarity);
							AssetBundleManager.load(CARD_FRAME_PATH + cardData.rarity, bundleLoadSuccess, cardLoadFailure, isSkippingMapping:true, fileExtension:".png");
						}

						cardsToShow.Add(cardData);
						if (PowerupBase.collectablesPowerupsMap.ContainsKey(cardsToShow[i].keyName))
						{
							AssetBundleManager.load(CARD_FRAME_POWERUPS, bundleLoadSuccess, cardLoadFailure);
						}
						else
						{
							AssetBundleManager.load(cardsToShow[i].texturePath, bundleLoadSuccess, cardLoadFailure, isSkippingMapping: true, fileExtension: ".png");
						}
					}
					else
					{
						cardLoadFailure(null);
					}
				}
			}
			else
			{
				if (onCardsReady != null)
				{
					onCardsReady();
				}
			}
		}
	}

	private void bundleLoadSuccess(string assetPath, Object obj, Dict data = null)
	{
		Texture2D loadedTexture = obj as Texture2D;
		string filePath = assetPath.Split('.')[0];
		string bundleName = AssetBundleManager.getBundleNameForResource(filePath);
		if (!bundleName.IsNullOrWhiteSpace() && !loadedBundles.Contains(bundleName))
		{
			loadedBundles.Add(bundleName);
		}

		// Should grab the file name to make life easier?

		loadedTexture.name = Path.GetFileNameWithoutExtension(assetPath);

		if (!loadedTexturesDict.ContainsKey(loadedTexture.name))
		{
			loadedTexturesDict.Add(loadedTexture.name, loadedTexture);
		}
		else
		{
			numberOfImagesToLoad--; //If we get multiple versions of the same card in the same pack drop
		}

		// -1 on textures to pack since we have the card pack sprite in there.
		if (loadedTexturesDict.Count == numberOfImagesToLoad)
		{
			// We copy the base card for our images so use its atlas, since the copies will reference it
			if (Collectables.usingDynamicAtlas)
			{
				generateDynamicAtlas();
			}

			//Once we've loaded the necessary cards onto our dynamic atlas we're unloading the associated bundles
			for (int i = 0; i < loadedBundles.Count; i++)
			{
				AssetBundleManager.unloadBundle(loadedBundles[i], false);
			}

			if (onCardsReady != null)
			{
				onCardsReady();
			}
		}
	}

	private void generateDynamicAtlas()
	{
		DynamicAtlas atlas = new DynamicAtlas();
		List<Texture2D> texturesToPack = new List<Texture2D>();
		foreach(Texture2D tex in loadedTexturesDict.Values)
		{
			if (tex != null)
			{
				texturesToPack.Add(tex);
			}
		}
		UIAtlas newAtlas = atlas.createAtlas(texturesToPack.ToArray(), gameObject, 1024, "Pack Drop Atlas");
		createdAtlas = newAtlas;
	}

	public void cardViewCloseClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "hir_collection",
			phylum: "card",
			klass: createdCardView.currentData.keyName,
			family: "close",
			genus: "click");

		Audio.play("ClickXCollections");
		createdCardView.gameObject.SetActive(false);
		slideController.enableScrolling();
	}

	private void showDetailedCard(Dict args = null)
	{
		if (createdCardView == null)
		{
			GameObject cardGameObject = NGUITools.AddChild(detailedCardViewParent, cardViewPrefab.gameObject);
			createdCardView = cardGameObject.GetComponent<AlbumDialogCardView>();
			createdCardView.closeButton.registerEventDelegate(cardViewCloseClicked);
		}

		CollectableCardData cardToShow = (CollectableCardData) args.getWithDefault(D.DATA, "");
		List<CollectableCardData> detailedCardList = new List<CollectableCardData>(cardsToShow);
		detailedCardList.Reverse();
		if (createdCardView != null && cardToShow != null)
		{
			int cardIndex = detailedCardList.IndexOf(cardToShow);
			createdCardView.init(cardToShow, cardIndex, detailedCardList,null, null, createdAtlas, loadedTexturesDict);
			createdCardView.gameObject.SetActive(true);
			slideController.preventScrolling();
			StatsManager.Instance.LogCount(counterName:"dialog",
				kingdom: "hir_collection",
				phylum: "card",
				klass: createdCardView.currentData.keyName,
				family: "pack",
				genus: "view");
		}
	}

	public void openAndRevealPack(string openAnimationName)
	{
		List<int> newCardIndices = new List<int>();
		List<CollectableCardAnimated> cardObjects = new List<CollectableCardAnimated>();
		CollectableAlbum album = Collectables.Instance.getAlbumByKey(Collectables.currentAlbum);
		if (album != null)
		{
			finalStarCount = album.currentDuplicateStars;
			if (cardsToShow != null)
			{
				for (int i = 0; i < cardsToShow.Count; i++)
				{
					if (cardsToShow[i] == null)
					{
						continue;
					}

					GameObject createdObject = NGUITools.AddChild(cardParent, baseCard.gameObject);
					CollectableCardAnimated newCard = createdObject.GetComponent<CollectableCardAnimated>();
					if (newCard != null)
					{
						CollectableCard cardHandle = newCard.createAnimatedCard();

						if (cardHandle != null)
						{
							cardHandle.gameObject.SetActive(true);
							cardHandle.init(cardsToShow[i], CollectableCard.CardLocation.PACK_DROP, createdAtlas,
								loadedTexturesDict);
							if (cardViewPrefab != null)
							{
								cardHandle.onClickButton.registerEventDelegate(showDetailedCard,
									Dict.create(D.DATA, cardsToShow[i]));
							}

							//Only want to show the new badge if the card isn't already collected
							bool isNew = !cardsToShow[i].isCollected;
							if (isNew)
							{
								cardsToShow[i].isNew = true; //Only new if we haven't collected it already
								//hide new badge for powerup cards
								if (!PowerupBase.collectablesPowerupsMap.ContainsKey(cardsToShow[i].keyName))
								{
									album.currentNewCards++;
									cardHandle.loadAnimatedNewBadge();
									onCardAnimationsFinished += cardHandle.playNewBadgeAnimation;

									if (cardHandle.powerupObject != null)
									{
										cardHandle.powerupObject.transform.localPosition = new Vector3(0, 0, 11);
									}
								}

								newCardIndices.Add(i);
							}
							else
							{
								finalStarCount += cardsToShow[i].rarity;
							}

							cardsToShow[i].isCollected = true;
							cardObjects.Add(newCard);
						}
					}
				}
			}
		}

		if (cardObjects.Count <= MAX_CARDS_ON_SCREEN)
		{
			slideController.enabled = false; //No scrolling when less than 5 cards
			int spacesToOffsetFor = MAX_CARDS_ON_SCREEN - cardObjects.Count + 1;
			CommonTransform.addX(firstCardTarget, NO_SLIDING_CENTER_OFFSET * spacesToOffsetFor);
		}
		else
		{
			//Do the stuff here to dynamically expand the slide area based on # of cards being dropped
			int cardsNeededToSlide = cardObjects.Count - MAX_CARDS_ON_SCREEN;
			slideController.leftBound = cardsNeededToSlide * -CARD_SPACING_X;
		}

		StartCoroutine(playPackSequence(openAnimationName, cardObjects, newCardIndices.ToArray()));
	}

	// Used by LobbyLoader to preload asset bundle.
	private void bundleLoadFailure(string assetPath, Dict data = null)
	{
		if (string.IsNullOrEmpty(assetPath))
		{
			Debug.LogError("CollectAPack::bundleLoadFailure - No bundle to download");
		}
		else
		{
			Debug.LogError("CollectAPack::bundleLoadFailure - Failed to download " + assetPath);
		}


		if (onCardsReady != null)
		{
			onCardsReady();
		}
	}

	private void cardLoadFailure(string assetPath, Dict data = null)
	{
		if (!string.IsNullOrEmpty(assetPath))
		{
			Debug.LogError("CollectAPack::cardLoadFailure - Failed to download " + assetPath);
		}
		else
		{
			Debug.LogError("CollectAPack::cardLoadFailure - Invalid card data");
		}

		numberOfImagesToLoad--;
		if (numberOfImagesToLoad == 0 || loadedTexturesDict.Count == numberOfImagesToLoad)
		{
			//Do the packing in here if we loaded some images successfully
			if (Collectables.usingDynamicAtlas && loadedTexturesDict.Count > 0)
			{
				generateDynamicAtlas();
			}

			//Once we've loaded the necessary cards onto our dynamic atlas we're unloading the associated bundles
			for (int i = 0; i < loadedBundles.Count; i++)
			{
				AssetBundleManager.unloadBundle(loadedBundles[i], false);
			}

			if (onCardsReady != null)
			{
				onCardsReady();
			}
		}
	}

	public void updateDuplicateStarCount(CollectableAlbum currentAlbum)
	{
		currentAlbum.currentDuplicateStars = finalStarCount;
		if (currentAlbum.currentDuplicateStars >= currentAlbum.maxStars)
		{
			currentAlbum.currentDuplicateStars -= currentAlbum.maxStars;
		}
	}
}
