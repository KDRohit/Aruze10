using UnityEngine;
using System.Collections;

using TMPro;

public class WeeklyRaceZoneListItem : MonoBehaviour
{
	[SerializeField] public GameObject promotion;
	[SerializeField] public GameObject relegation;
	[SerializeField] public ButtonHandler zoneInfoButton;
	[SerializeField] public UISprite narrowBackground;
	[SerializeField] public UISprite background;
	[SerializeField] public UISprite buttonSprite;

	[SerializeField] public GameObject dropZoneLeftArrow;
	[SerializeField] public GameObject dropZoneRightArrow;
	
	[SerializeField] public TextMeshPro promotionText;
	[SerializeField] public TextMeshPro relegationText;

	private WeeklyRaceLeaderboard leaderboard;
	private bool isPromotion = false;

	private const string DROP_ZONE_BG = "Player Panel Bar Orange Stretchy";
	private const string PROMOTION_ZONE_BG = "Player Panel Bar Green Stretchy";
	private const string BUTTON_PROMO_BG = "Button Info Green";
	private const string BUTTON_DROP_BG = "Button Info Orange";

	void Awake()
	{
		init();
	}

	public void init()
	{
		zoneInfoButton.registerEventDelegate(onInfoClick);
	}

	/// <summary>
	///   Called from the leaderboard dialog, passes in a reference to the leader for handling callbacks
	/// </summary>
	public void setup(WeeklyRaceLeaderboard leaderboard, bool isPromotion)
	{
		this.leaderboard = leaderboard;
		this.isPromotion = isPromotion;

		promotion.SetActive(isPromotion);
		relegation.SetActive(!isPromotion);
		buttonSprite.spriteName = isPromotion ? BUTTON_PROMO_BG : BUTTON_DROP_BG;
		background.spriteName = isPromotion ? PROMOTION_ZONE_BG : DROP_ZONE_BG;
		narrowBackground.spriteName = isPromotion ? PROMOTION_ZONE_BG : DROP_ZONE_BG;

		if (leaderboard == null)
		{
			background.gameObject.SetActive(false);
			narrowBackground.gameObject.SetActive(true);
			zoneInfoButton.gameObject.SetActive(false);
		}
	}

	public void setText(string text)
	{
		// you can just set both, only one is ever visible
		if (promotionText != null)
		{
			promotionText.text = text;
		}

		if (relegationText != null)
		{			
			relegationText.text = text;

			// if we are setting up text, and see that something isn't for the drop zone. we are going to disable the drop zone arrows
			if (!text.Contains("Drop"))
			{
				dropZoneLeftArrow.SetActive(false);
				dropZoneRightArrow.SetActive(false);
			}
		}
	}

	public void onInfoClick(Dict args = null)
	{
		leaderboard.showZoneInfo(isPromotion);
	}
}
