using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using TMProExtensions;

public class LobbyCarouselCard : MonoBehaviour
{
	// =============================
	// PRIVATE
	// =============================
	[SerializeField] private SwipeAnimationScrub scrubber = null;
	[SerializeField] private LobbyCarouselCardPane genericCard;
	[SerializeField] private LobbyCarouselCardPane featureCard;
	[SerializeField] private LobbyCarouselCardPane saleCard;
	[SerializeField] private LobbyCarouselCardPane specialSaleCard;
	[SerializeField] private LobbyCarouselCardPane crossPromoCard;
	[SerializeField] private LobbyCarouselCardPane beginnerSpecial;
	[SerializeField] private LobbyCarouselCardPane lifecycleSale;
	[SerializeField] private GameObject customCardAnchor;

	[SerializeField] private Animator animator;
	[SerializeField] private ClickHandler clickHandler;

	private LobbyCarouselCardPane currentCard = null;
	private GameTimer timer = null;		// Some slides use this, depending on the slide's action.
	private GameTimer textUpdateTimer = null;  // used to update text displaying the timer value, once a second
	private bool didDeactivate = false;
	private TextMeshPro prefixLabel = null;
	private TextMeshPro label = null;
	private TextMeshPro buttonLabel = null;
	private GameObject fullCustomCard = null; // A self contained card
	private bool ignoreTimerExpired;
	public int sortIndex { get; private set; }
	public CarouselData data { get; private set; }

	// =============================
	// CONST
	// =============================
	private const string SPECIAL_CAROUSEL_IMAGE_URL = "{0}/CarouselBG.jpg";	// Feature-specific carousel images.
	private const string NORMAL_CAROUSEL_IMAGE_URL = "lobby_carousel/{0}";	// General carousel images that can be chosen on the admin tool.
	private const string GENERIC = "feature";
	private const string UNIQUE_FEATURE = "unique_feature";
	private const string SALE = "sale";
	private const string SPECIAL_SALE = "special_sale";
	private const string CROSS_PROMO = "cross_promo";
	private const string BEGINNER_SPECIAL = "beginner_special";
	private const string LIFECYLCE_SALE = "lifecycle_sale";
	private const string SLOTVENTURE = "slotventure";
	private const string QUEST_FOR_THE_CHEST = "quest_for_the_chest";

	private const string DEFAULT_FONT_NAME = "OpenSans-Bold SDF";

	// card animations
	private const string LEFT_TURN_ANIM = "L turn";
	private const string RIGHT_TURN_ANIM = "R turn";
	private const string INTRO_ANIM = "Intro";
	private const string DEFAULT = "default";

	private int prevVipLevel = 0;

	void OnDestroy()
	{
		_imageGlowShader = null;
		_imageShader = null;
	}

	public void setup(CarouselData data)
	{
		prevVipLevel = SlotsPlayer.instance.vipNewLevel;
		this.data = data;
		if (data.imageData.Length <= 0)
		{
			Debug.Log(string.Format("Skipping carousel for {0}, invalid image data setup", data.actionName));
			return;
		}
		clickHandler.registerEventDelegate(onClick);
		timer = DoSomething.getTimer(data.action);
		
		SafeSet.gameObjectActive(featureCard.gameObject, false);
		SafeSet.gameObjectActive(saleCard.gameObject, false);
		SafeSet.gameObjectActive(specialSaleCard.gameObject, false);
		SafeSet.gameObjectActive(crossPromoCard.gameObject, false);
		SafeSet.gameObjectActive(genericCard.gameObject, false);
		SafeSet.gameObjectActive(beginnerSpecial.gameObject, false);

		setCardType();

		if (scrubber == null)
		{
			scrubber = gameObject.GetComponent<SwipeAnimationScrub>();
		}

		if (scrubber != null)
		{
			scrubber.onSwipeUpdate += onSwipeUpdate;
			scrubber.onSwipeReset += onSwipeReset;
		}

		// This means the card itself will handle whatever it has to from here down. 
		if (fullCustomCard != null)
		{
			return;
		}

		createImageFromData(data);
		sortIndex = data.sortIndex;

		prefixLabel = currentCard.timerPrefixLabel;
		label = currentCard.timerLabel;
		buttonLabel = currentCard.buttonLabel;

		createLabel(out ignoreTimerExpired);

		if (label != null && timer != null)
		{
			currentCard.timerContainer.SetActive(true);
			updateTimerLabelText();
			textUpdateTimer = new GameTimer(1.0f);  // update once/sec
		}
		else if (timer == null && currentCard.timerContainer != null)
		{
			currentCard.timerContainer.SetActive(false);
		}

		SafeSet.gameObjectActive(currentCard.challengesNotepad, data.isShowingChallengesNotepad);
		setupCardData();
	}

	private void onSwipeUpdate(float delta)
	{
		if (animator != null && CarouselData.active.Count > 1 && !Dialog.instance.isShowing)
		{
			string animName = getAnimationName(delta > 0 ? 1 : -1);
			animator.Play(animName, 0, Mathf.Abs(delta));
			animator.speed = 1;
		}
	}

	private void onSwipeReset(float delta)
	{
		if (animator != null)
		{
			string animName = getAnimationName(1);
			animator.Play(animName, 0, 0);
			animator.speed = 0;
		}
	}

	private void setupCardData()
	{
		if (currentCard == beginnerSpecial)
		{
			currentCard.setBeginnerSpecialFields();
		}
		else if (currentCard == lifecycleSale)
		{
			currentCard.setLifeCycleSales();
		}
	}

	private void onClick(Dict args)
	{
		string action = data.actionName;
		string param = data.actionParameter;
		if (DoSomething.isValidString(action, param))
		{
			DoSomething.now(action, param);
		}
		else
		{
			Debug.LogErrorFormat("LobbyCarouselCard.cs -- onClick -- action not valid-- {0}::{1}", action, param);
		}

		string cardType = "event";

		if (currentCard == specialSaleCard || currentCard == saleCard || currentCard == beginnerSpecial)
		{
			cardType = "sale";
		}

		StatsManager.Instance.LogCount
		(
			counterName: "lobby",
			kingdom: "carousel_card",
			phylum: cardType,
			klass: data.actionName,
		  	family: "",
			genus: "click"
		);
	}

	public void onShow()
	{
		checkIsActive();
	}

	public void onHide()
	{
		checkIsActive();
	}

	private void checkIsActive()
	{
		if (data != null && !data.getIsValid())
		{
			deactivate();
		}
	}

	void Update()
	{
		if (Glb.isResetting)
		{
			return;
		}
		if (didDeactivate && currentCard != null)
		{
			// change text due to object being disabled in between slides
			if (currentCard.timerLabel != null && currentCard.timerLabel.text.ToLower() != "ENDED")
			{
				currentCard.timerLabel.text = Localize.toUpper("ended");
			}
			// Sometimes it takes more than a frame to truly deactivate and destroy the slide,
			// so make sure no more processing happens on this slide if we deactivated it.
			// Originally I tried simply setting enabled = false, but that stops coroutines
			// from completing, causing problems for streaming textures.
			return;
		}
		
		if (timer != null)
		{				
			if (timer.isExpired && !ignoreTimerExpired)
			{
				timer = null;	// Prevent this from being called multiple times after the timer expires once.
				
				// The sale expired, so this slide must be removed from the carousel when scrolling is done.
				deactivate();
				return;
			}
		}

		if (didDeactivate)
		{
			return;
		}

		if (textUpdateTimer != null && textUpdateTimer.isExpired)
		{
			bool shouldRefresh = timer != null && timer.isExpired && ignoreTimerExpired;
			updateTimerLabelText(shouldRefresh);
			textUpdateTimer.startTimer(1.0f);  // update once/sec
			setupSpecialFeature();
		}

		if (SlotsPlayer.instance.vipNewLevel > prevVipLevel)
		{
			//Some cards need to get reset up if the VIP level changes mid-display
			prevVipLevel = SlotsPlayer.instance.vipNewLevel;
			setupCardData();
		}
	}

	private void deactivate()
	{
		data.deactivate();
		didDeactivate = true;
	}

	private void setCardType()
	{
		string type = data.imageData.Length > 0 ? data.imageData[0].getString("panel_type", "") : "generic";

		switch(type)
		{
			case UNIQUE_FEATURE:
				currentCard = featureCard;
				setupSpecialFeature();
				break;

			case SALE:
				currentCard = saleCard;
				break;

			case SPECIAL_SALE:
				currentCard = specialSaleCard;
				break;

			case CROSS_PROMO:
				currentCard = crossPromoCard;
				break;

			case BEGINNER_SPECIAL:
				currentCard = beginnerSpecial;
				break;

			case LIFECYLCE_SALE:
				currentCard = lifecycleSale;
				break;

			case SLOTVENTURE:
				if (SlotventuresLobby.assetData.portalPrefab != null)
				{
					fullCustomCard = NGUITools.AddChild(customCardAnchor, SlotventuresLobby.assetData.portalPrefab);
					currentCard = null;
					SafeSet.gameObjectActive(fullCustomCard, true);
				}
				else
				{
					Debug.LogWarning("Invalid slot ventures portal prefab");
					currentCard = genericCard;
				}
				return;

			default:
				currentCard = genericCard;
				break;
		}

		SafeSet.gameObjectActive(currentCard.gameObject, true);
	}

	private void setupSpecialFeature()
	{
		if (data != null && data.isShowingJackpotMeter && data.action != null && this != null && currentCard != null)
		{
			if (data.action.Contains("ticket_tumbler") && TicketTumblerFeature.instance != null)
			{
				currentCard.setJackpotAmount(TicketTumblerFeature.instance.eventPrizeAmount);
			}

			if (data.action.Contains("jackpot_days") && ProgressiveJackpot.buyCreditsJackpot != null)
			{
				currentCard.setJackpotAmount(ProgressiveJackpot.buyCreditsJackpot.pool);
			}

			if (data.action.Contains("collectables"))
			{
				CollectableAlbum currentAlbum = Collectables.Instance.getAlbumByKey(Collectables.currentAlbum);
				if (currentAlbum != null)
				{
					currentCard.setJackpotAmount(currentAlbum.rewardAmount);
				}
			}
			if (data.action.Contains("facebook_connect"))
			{
				currentCard.setJackpotAmount(SlotsPlayer.instance.mergeBonus);
			}
		}
	}

	/// <summary>
	///   Plays the appropriate animation based on the direction
	/// direction = 1, plays the right animation
	/// direction = 1, plays the left animation
	/// direction = 0, plays the intro
	/// reverse - plays the animation in reverse
	/// </summary>
	public string playAnimation(int direction = 0, bool reverse = false)
	{
		// It's nice to know what animation we played in case we need to do something special.
		string animationWePlayed = getAnimationName(direction, reverse);

		animator.Play(animationWePlayed);
		animator.speed = 1;

		return animationWePlayed;
	}

	private string getAnimationName(int direction = 0, bool reverse = false)
	{
		string anim = INTRO_ANIM;

		if (direction != 0)
		{
			anim = direction > 0 ? RIGHT_TURN_ANIM : LEFT_TURN_ANIM;
		}
	
		if (reverse)
		{
			anim += " reverse";
		}

		return anim;
	}

	// Animator AnimationEvents depend on this function being present
	public void onReverseComplete()
	{
		gameObject.SetActive(false);
	}

	// Animator AnimationEvents depend on this function being present
	public void onOutComplete()
	{
		gameObject.SetActive(false);
	}

	public void setScrollerActive(bool active)
	{
		scrubber.setScrollerActive(active);
	}

	/*=========================================================================================
	Image Setup
	=========================================================================================*/
	
	// Creates a texture using the specifications from the json.
	// Known JSON properties for images:
	//	x					(integer)
	//	y					(integer)
	//	z					(integer)
	//	width				(integer)
	//	height				(integer)
	//	rotation			(integer)
	//	url					(explicit url)
	//	url_override		(data-driven key word like "next_unlock_icon_url")
	private void createImageFromData(CarouselData data)
	{
		if (data.imageData.Length > 0)
		{
			JSON json = data.imageData[0];

			Vector3 size = new Vector3(
				json.getInt("width", 0),
				json.getInt("height", 0),
				1.0f
			);

			Vector3 position = new Vector3(
				json.getInt("x", 0),
				json.getInt("y", 0),
				json.getInt("z", 0)
			);

			// Default to the URL chosen in the admin tool.
			string url = "";
			string urlOverride = json.getString("url_override", "");

			if (urlOverride != "")
			{
				// A URL override is provided, use it over the "url" field.
				// First see if it's a data-driven keyword.
				url = getData(urlOverride);

				if (url == "")
				{
					// Not a data-driven keyword, so use it as-is.
					url = urlOverride;
				}
			}
			else
			{
				//apply a theme if necessary
				string panelType = json.getString("panel_type", "");
				string urlPath = json.getString("url", "");
				switch (panelType.Trim())
				{
					case QUEST_FOR_THE_CHEST:
						urlPath = string.Format(urlPath, ExperimentWrapper.QuestForTheChest.themeWithoutSpaces);
						break;
				}

				// No URL override data was found, so use the normal URL as-is, plugged into the standard base path.
				url = string.Format(NORMAL_CAROUSEL_IMAGE_URL, urlPath);
			}

			createImage(url, size, position, json.getInt("rotation", 0));
		}
	}

	// Creates an image with the given arguments.
	private void createImage(string url, Vector3 size, Vector3 position, int rotation)
	{
		loadTexture(currentCard.image, url, imageShader, currentCard.imageParent);
		
		if (currentCard.imageGlow != null)
		{
			// The 'Image glow' glint shader needs to be assigned the same tex as the Image renderer
			loadTexture(currentCard.imageGlow, url, imageGlowShader, currentCard.imageParent);	
		}
	}

	// Loads a texture and applies it to the given renderer.
	protected void loadTexture(Renderer imageRenderer, string url, Shader shader, GameObject rendererParent)
	{
		SafeSet.gameObjectActive(rendererParent, false);
		RoutineRunner.instance.StartCoroutine(DisplayAsset.loadTexture(url, textureCallback, Dict.create(D.OPTION, imageRenderer, D.OBJECT, rendererParent)));
	}
	
	protected void textureCallback(Texture2D tex, Dict texData)
	{
		if (this == null)
		{
			return;
		}
		
		if (tex != null)
		{
			Renderer imageRenderer = texData.getWithDefault(D.OPTION, null) as Renderer;
			GameObject parent = texData.getWithDefault(D.OBJECT, null) as GameObject;
			if (imageRenderer == null)
			{
				return;
			}

			Shader shader = imageShader;
			if (shader == null)
			{
				Debug.LogError("Could not load shader \"Unlit/GUI Texture\"  for streamed texture");
				SafeSet.gameObjectActive(parent, false);
				
				//remove slide
				if (!data.isDefault)
				{
					data.deactivate();	
				}
			}
			else
			{
				Material mat = new Material(shader);
				mat.mainTexture = tex;
				imageRenderer.material = mat;
			
				SafeSet.gameObjectActive(parent, true);	
			}
		}
		else if (!data.isDefault)
		{
			// If the texture failed to download, remove this slide from the carousel to prevent
			// repeated attempts to download the image, which may be causing high network traffic issues.
			// We never deactivate the default slide, even if the image failed,
			// because the carousel will automatically re-create it over and over and over and over...
			data.deactivate();
		}
	}

	// Returns data based on the "data" or "url_override" field in text and image JSON blocks respectively.
	// Note: If adding or removing strings, please also update the wiki appropriately:
	// https://wiki.corp.zynga.com/display/hititrich/Carousel+Slides+2.0
	public string getData(string dataType, bool refresh = false)
	{
		string dataValue = "";
		LobbyGame game = null;
		
		switch (dataType)
		{
			case "timer":
				if (timer == null)
				{
					// This shouldn't happen unless in test view mode.
					if (CarouselData.isTestViewing)
					{
						dataValue = CommonText.secondsFormatted(50000);
					}
					else
					{
						Debug.LogWarningFormat("CarouselPanelCustom: Trying to get timer data but there is no timer: action: '{0}'",  data.action);
					}
				}
				else
				{
					if (refresh)
					{
						timer = DoSomething.getTimer(data.action);
						if (timer == null)
						{
							dataValue = "";
						}
						else
						{
							dataValue = timer.timeRemainingFormatted;
						}
					}
					else
					{
						dataValue = timer.timeRemainingFormatted;
					}


				}
				break;
				
			case "next_unlock_level":
				game = LobbyGame.getNextUnlocked(SlotsPlayer.instance.socialMember.experienceLevel);
				if (game != null)
				{
					dataValue = CommonText.formatNumber(game.unlockLevel);
				}
				break;
				
			case "next_unlock_icon_url":
				game = LobbyGame.getNextUnlocked(SlotsPlayer.instance.socialMember.experienceLevel);
				if (game != null)
				{
					dataValue = SlotResourceMap.getLobbyImagePath(game.groupInfo.keyName, game.keyName);
				}
				break;
			case "reactivate_friend_reward":
				dataValue = CreditsEconomy.convertCredits(ReactivateFriend.rewardAmount);
				break;
				
			case "sneak_preview_icon_url":
				if (LobbyGame.sneakPreviewGame != null)
				{
					dataValue = SlotResourceMap.getLobbyImagePath(LobbyGame.sneakPreviewGame.groupInfo.keyName, LobbyGame.sneakPreviewGame.keyName);
				}
				break;

			case "quest_collectibles":
				dataValue = CommonText.formatNumber(SlotsPlayer.instance.questCollectibles);
				break;
				
			case "w2e_credits":
				dataValue = CreditsEconomy.convertCredits(WatchToEarn.rewardAmount);
				break;
			case "credit_sale_multiplier":
				dataValue = CommonText.formatNumber(PurchaseFeatureData.findBuyCreditsMultiplier());
				break;
			case "credit_sale_percentage":
				dataValue = CommonText.formatNumber(PurchaseFeatureData.findBuyCreditsSalePercentage());
				break;
			case "credit_sale_bonus":
				// Adding 100 because of how this is shown to the client on the page vs how it is calculated in purchasablepackage.
				int bonus = PurchaseFeatureData.findBuyCreditsSalePercentage();
				if (bonus % 100 == 0)
				{
					// If this is a multiple of 100, then we want to show it as {0}X.
					int multiplier = (bonus / 100) + 1;
					dataValue = Localize.textUpper("{0}X", CommonText.formatNumber(multiplier));
				}
				else
				{
					// Otherwise we want to show it as {0}%
					dataValue = Localize.textUpper("{0}_percent", CommonText.formatNumber(bonus));
				}
				break;
			case "credit_sale_title":
				dataValue = BuyCreditsDialog.currentSaleTitle;
				break;
			case "credit_sweepstakes_payout":
				dataValue = CreditsEconomy.convertCredits(CreditSweepstakes.payout);
				break;
			case "linked_vip_connect_bonus":
				dataValue = CreditsEconomy.convertCredits(LinkedVipProgram.instance.incentiveCredits);
				break;
			case "level_up_bonus_multiplier":
				dataValue = CommonText.formatNumber(LevelUpBonus.multiplier);
				break;
			case "ticket_tumbler_prize":
				if (TicketTumblerFeature.instance.eventData != null)
				{
					dataValue = CreditsEconomy.convertCredits(TicketTumblerFeature.instance.eventPrizeAmount);
				}
				break;

			case "qfc_video_slide_url":
				dataValue = string.Format(NORMAL_CAROUSEL_IMAGE_URL, string.Format("V3", ""));
				break;

			case "xpromo_url":
				MobileXpromo.logView();
				dataValue = MobileXpromo.getCarouselImagePath();
				break;
					
			case "collectables_award":
				// This needs real data from collectables
				dataValue = "0";
				break;

				// Note: If adding or removing strings, please also update the wiki appropriately:
				// https://wiki.corp.zynga.com/display/hititrich/Carousel+Slides+2.0
		}
		
		return dataValue;
	}

	/*=========================================================================================
	Label Setup
	=========================================================================================*/

	// Creates a label using the specifications from the json.
	// Known JSON properties for labels:
	//	x					(integer)
	//	y					(integer)
	//	width				(integer)
	//	height				(integer)
	//	rotation			(integer)
	//	scale_x				(float 0.5 - 2.0)
	//	line_spacing		(integer)
	//	data				("timer", "next_unlock_level", etc.)
	//	localization_key	(localization key string)
	//	is_all_caps			(true/false)
	//	alignment			("center", "left", "right", "top", "bottom", "top_left", "top_right", "bottom_left", "bottom_right")
	//	font				("OpenSans-Bold SDF", "monofonto numbers SDF")
	//	color				(hex string)
	//	is_gradient			(true/false)
	//	gradient_steps		(array of objects containing "location" float and "color" hex string)
	//	end_gradient_color	(hex string)
	//	effect				("shadow" or "outline")
	//	effect_distance		(integer)
	//	effect_color		(hex string)
	//	effect_softness		(float)
	private void createLabel(out bool ignoreTimer)
	{
		ignoreTimer = false;

		if (data.textData.Length >= 1)
		{
			foreach (JSON json in data.textData)
			{
				string dataType = json.getString("data", "");

				if (dataType != "")
				{

					updateTimerLabelText();
					ignoreTimer = json.getBool("timer_always_valid", false);
					applyLabelDesignData(label, json);

					if (prefixLabel != null)
					{
						updateTimerPrefixLabelText(json);
					}

				}
				else if (!string.IsNullOrEmpty(json.getString("button_label_override", "")) && ExperimentWrapper.MobileToMobileXPromo.isInExperiment)
				{
					switch (json.getString("button_label_override", ""))
					{
						case "xpromo":
							if (MobileXpromo.isGameInstalled())
							{
								buttonLabel.text = Localize.text("xpromo_play_now");
							}
							else
							{
								buttonLabel.text = Localize.text("xpromo_download_now");
							}
							break;

						default:
							break;
					}
				}
				else if (!string.IsNullOrEmpty(json.getString("button_text", "")) && buttonLabel != null)
				{
					buttonLabel.text = Localize.text(json.getString("button_text", ""));
				}
			}
		}
	}

	private void updateTimerLabelText(bool refresh = false)
	{
		string dataValue = getData("timer", refresh);
		label.text = dataValue;
	}

	private void updateTimerPrefixLabelText(JSON textData )
	{
		string locKey = textData.getString("localization_key", "");
		if (!string.IsNullOrEmpty(locKey))
		{
			prefixLabel.text = Localize.text(locKey);
		}

	}

	// Applies the design-related properties to a label from the JSON data.
	private static void applyLabelDesignData(TextMeshPro label, JSON json)
	{
		if (label == null || json == null)
		{
			return;
		}
		
		// TODO:UNITY2018:obsoleteTextContainer:confirm
		label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, json.getInt("width", 0));
		label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, json.getInt("height", 0));
		label.lineSpacing = json.getInt("line_spacing", 0);
		
		Shader shader = label.fontSharedMaterial.shader;	// Remember the shader to use on the new material instance below.
		
		string fontName = json.getString("font", DEFAULT_FONT_NAME);
		label.font = TMProFontLoader.getFont(fontName);
				
		label.makeMaterialInstance(shader);	// Must do this whenever the font is changed since doing that assigns the default material for that font.
				
		label.color = CommonColor.colorFromHex(json.getString("color", "FF000000"));
		label.fontStyle = (FontStyles)json.getInt("style", 0);
		label.enableAutoSizing = true;
		label.fontSizeMin = 18;
		label.fontSizeMax = json.getInt("max_font_size", 72);

		if (json.getBool("is_gradient", false))
		{
			label.enableVertexGradient = true;
			Color endGradientColor = CommonColor.colorFromHex(json.getString("end_gradient_color", "FF000000"));
			label.colorGradient = new VertexGradient(label.color, label.color, endGradientColor, endGradientColor);
		}
		else
		{
			label.enableVertexGradient = false;	
		}
		
		string effect = json.getString("effect", "");

		switch (effect)
		{
			case "outline":
				label.enableUnderlay(true);
				label.setUnderlayOffset(Vector2.zero);
				float thickness = json.getFloat("effect_distance", 0.0f);
				if (thickness > 1.0f)
				{
					// This is an older definition before TextMeshPro. Convert to something sensible.
					thickness = 0.35f;
				}
				label.setUnderlayDilate(thickness);
				break;
			case "shadow":
				label.enableUnderlay(true);
				label.setUnderlayDilate(0.0f);
				float distance = json.getFloat("effect_distance", 0.0f);
				if (distance > 1.0f)
				{
					// This is an older definition before TextMeshPro. Convert to something sensible.
					distance = -0.5f;
				}
				label.setUnderlayOffset(new Vector2(0.0f, distance));
				break;
			default:
				label.enableUnderlay(false);
				break;
		}
	
		if (effect != "")
		{
			label.setUnderlaySoftness(json.getFloat("effect_softness", 0.0f));
			label.setUnderlayColor(CommonColor.colorFromHex(json.getString("effect_color", "FF000000")));
		}
	}

	/*=========================================================================================
	GETTERS
	=========================================================================================*/
	protected static Shader imageShader
	{
		get
		{
			if (_imageShader == null)
			{
				_imageShader = ShaderCache.find("Unlit/GUI Texture");
			}
			return _imageShader;
		}
	}
	private static Shader _imageShader = null;
	
	protected static Shader imageGlowShader
	{
		get
		{
			if (_imageGlowShader == null)
			{
				_imageGlowShader = ShaderCache.find("Unlit/Glint");
			}
			return _imageGlowShader;
		}
	}
	private static Shader _imageGlowShader = null;
	
}
