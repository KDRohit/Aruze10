using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LobbyCarouselV3 : MonoBehaviour, IResetGame
{
	public static LobbyCarouselV3 instance { get; private set; }

	// =============================
	// PRIVATE
	// =============================
	private List<LobbyCarouselCard> cards = new List<LobbyCarouselCard>();
	private List<CarouselData> cardsData = new List<CarouselData>();
	private JSON carouselData;
	private int currentCard = 0;
	private int currentCardAnimDirection = 1; // 1 means it flips to the right, -1 to flip to the left
	private GameTimer delayTimer;
	private bool isSwiping = false;

	
	[SerializeField] private ImageButtonHandler leftButton;
	[SerializeField] private ImageButtonHandler rightButton;
	[SerializeField] private GameObject carouselCard;

	// =============================
	// CONST
	// =============================
	private const string NORMAL_CAROUSEL_IMAGE_URL = "lobby_carousel/{0}";	// General carousel images that can be chosen on the admin tool.
	// move sounds
	private const string SOUND_MOVE_PREVIOUS = "FriendsLeftArrow";
	private const string SOUND_MOVE_NEXT = "FriendsRightArrow";

	public static void cleanup()
	{
		if (instance != null)
		{
			instance.clearCards();
			Destroy(instance.gameObject);
			instance = null;
		}
	}

	void Awake()
	{
		instance = this;
		init();
	}

	void Update()
	{
		if (delayTimer != null && delayTimer.isExpired && !isSwiping)
		{
			goToCard(1);

			startAutoSlideTimer();
		}
	}
	
	public void refreshTimer(CarouselData data)
	{
		for (int i = 0; i < cards.Count; i++)
		{
			if (data == cards[i].data)
			{
				cards[i].getData("timer", true);
				return;
			}
		}
	}

	private void clearCards()
	{
		for (int i = 0; i < cards.Count; ++i)
		{
			GameObject.Destroy(cards[i].gameObject);
		}

		cards = new List<LobbyCarouselCard>();
		cardsData = new List<CarouselData>();
	}

	public void init()
	{
		if (cards.Count > 0)
		{
			clearCards();
		}

		currentCard = 0;

		if (CarouselData.active.Count > 0)
		{
			createCarouselCards();
			leftButton.registerEventDelegate(onLeftClick);
			rightButton.registerEventDelegate(onRightClick);
			leftButton.enabled = (cards.Count > 1);
			rightButton.enabled = (cards.Count > 1);

			if (cards.Count > 1)
			{
				cards[0].gameObject.SetActive(true);
			}
			else if (cards.Count > 0)
			{
				cards[currentCard].gameObject.transform.localPosition = new Vector3(0, 0, -5);
				cards[currentCard].playAnimation(); // move the current card in
				SafeSet.gameObjectActive(cards[currentCard].gameObject, true);
			}
		}

		startAutoSlideTimer();
	}

	private void createCarouselCards()
	{
		foreach (CarouselData activeCarousel in CarouselData.active)
		{
			createCard(activeCarousel);
		}

		cards.Sort(sortCards);
		resetCards();
	}

	private void createCard(CarouselData data)
	{
		if (data != null &&
			data.imageData != null &&
			data.imageData.Length > 0 &&
			!string.IsNullOrEmpty(data.imageData[0].getString("panel_type", "")))
		{
			GameObject cardObject = CommonGameObject.instantiate(carouselCard) as GameObject;

			if (cardObject == null)
			{
				Debug.LogError("Critical error, carousel card could not be created");
				return;
			}
			
			cardObject.transform.parent = gameObject.transform;
			cardObject.transform.localPosition = new Vector3(0, 0, -10);
			cardObject.transform.localScale = Vector3.one;
			LobbyCarouselCard card = cardObject.GetComponent(typeof(LobbyCarouselCard)) as LobbyCarouselCard;

			if (card == null)
			{
				Debug.LogError("Critical error, carousel card missing LobbyCarouselCard script");
			}

			if (cards == null || cardsData == null)
			{
				cards = new List<LobbyCarouselCard>();
				cardsData = new List<CarouselData>();
			}

			card.setup(data);
			cards.Add(card);
			cardsData.Add(data);

			SwipeAnimationScrub scrubber = cardObject.GetComponent<SwipeAnimationScrub>();
			if (scrubber != null)
			{
				scrubber.onSwipeLeft += onSwipeLeft;
				scrubber.onSwipeRight += onSwipeRight;
				scrubber.onSwipeUpdate += onCardSwiping;
				scrubber.onSwipeLimitMet += onSwipeLimitMet;
			}

			cardObject.SetActive(false);
		}
	}
	
	private void resetCards()
	{
		for (int i = 1; i < cards.Count; i++)
		{
			SafeSet.gameObjectActive(cards[i].gameObject, false);
		}

		currentCard = 0;
		currentCardAnimDirection = 1;
	}

	private void onCardSwiping(float delta)
	{
		isSwiping = true;
	}

	private void onSwipeLeft()
	{
		if (Dialog.instance.isShowing || cards.Count <= 1) { return; }
		
		StatsManager.Instance.LogCount
		(
			  counterName: "lobby",
			  kingdom: "carousel_scroll",
			  phylum: "left",
			  klass: "",
			  family: "",
			  genus: "swipe"
		);
		SafeSet.gameObjectActive(cards[currentCard].gameObject, false);
		goToCard(-1);
		Audio.play(SOUND_MOVE_PREVIOUS);
		isSwiping = false;
	}

	private void onSwipeRight()
	{
		if (Dialog.instance.isShowing || cards.Count <= 1) { return; }
				
		StatsManager.Instance.LogCount
		(
			  counterName: "lobby",
			  kingdom: "carousel_scroll",
			  phylum: "right",
			  klass: "",
			  family: "",
			  genus: "swipe"
		);
		SafeSet.gameObjectActive(cards[currentCard].gameObject, false);
		goToCard(-1);
		Audio.play(SOUND_MOVE_NEXT);
		isSwiping = false;
	}

	private void onSwipeLimitMet()
	{
		if (Dialog.instance.isShowing || cards.Count <= 1) { return; }

		Input.ResetInputAxes(); // stop calculations for swiping

		SwipeAnimationScrub scrubber = cards[currentCard].GetComponent<SwipeAnimationScrub>();
		if (scrubber != null)
		{
			scrubber.reset();
		}

		goToCard(1, false);
	}
	
	private void onLeftClick(Dict args)
	{
		StatsManager.Instance.LogCount
		(
			  counterName: "lobby",
			  kingdom: "carousel_scroll",
			  phylum: "left",
			  klass: "",
			  family: "",
			  genus: "click"
		);
		goToCard(-1);
		Audio.play(SOUND_MOVE_PREVIOUS);
	}

	private void onRightClick(Dict args)
	{
		StatsManager.Instance.LogCount
		(
			  counterName: "lobby",
			  kingdom: "carousel_scroll",
			  phylum: "right",
			  klass: "",
			  family: "",
			  genus: "click"
		);
		goToCard(1);
		Audio.play(SOUND_MOVE_NEXT);
	}
	
	/// <summary>
	/// Goes to the next or previous card depending on the direction value
	/// 1 = Right  -1 = Left
	/// </summary>
	/// <param name="direction"></param>
	/// <param name="playAnimation"></param>
	private void goToCard(int direction, bool playAnimation = true)
	{
		if (cards.Count > 1)
		{
			// Having a handle on the last card is nice in case we have to do something special
			// for example, not stacking the intro animations when rapidly swiping
			int lastCard = currentCard;

			cards[lastCard].onHide();

			cards[currentCard].gameObject.transform.localPosition = new Vector3(0, 0, -10);
			currentCardAnimDirection = direction;
			if (playAnimation)
			{
				cards[currentCard].playAnimation(currentCardAnimDirection); // move the current card out
			}

			if (direction == 1)
			{
				currentCard = (int)CommonMath.umod(++currentCard, cards.Count);
			}
			else if(direction == -1)
			{
				currentCard = (int)CommonMath.umod(--currentCard, cards.Count);
			}
				
			cards[currentCard].gameObject.transform.localPosition = new Vector3(0, 0, -5);

			SafeSet.gameObjectActive(cards[currentCard].gameObject, true);

			cards[currentCard].onShow();

			stopCarousel();
			startAutoSlideTimer();
		}
	}

	public void startAutoSlideTimer()
	{
		if (CarouselData.active.Count > 1)
		{
			//if we have an invalid index, reset to 0
			if (currentCard >= CarouselData.active.Count)
			{
				Debug.LogWarning("Reseting current card index on carousel.  It's out of bounds");
				currentCard = 0;
			}

			if (cardsData.Count <= currentCard || cardsData[currentCard] == null)
			{
				Debug.LogError("Invalid carousel data");
				delayTimer = null;
			}
			else
			{
				delayTimer = new GameTimer(cardsData[currentCard].seconds);	
			}
		}
		else
		{
			// If there is no need to cycle through slides, then make sure the timer is cleared.
			delayTimer = null;
		}
	}
	
	public void stopCarousel()
	{
		delayTimer = null;
	}

	public void setCarousalScrollActive(bool active)
	{
		for (int i = 0; i < cards.Count; i++)
		{
			cards[i].setScrollerActive(active);
		}
	}

	private void startPausedTimer()
	{
		if (CarouselData.active.Count > 1)
		{
			delayTimer = new GameTimer(LobbyCarousel.DISPLAY_PAUSED_SECONDS);
		}
	}

	private int sortCards(LobbyCarouselCard a, LobbyCarouselCard b)
	{
		return a.sortIndex - b.sortIndex;	
	}

	public static void resetStaticClassData()
	{
		instance = null;
	}
}
