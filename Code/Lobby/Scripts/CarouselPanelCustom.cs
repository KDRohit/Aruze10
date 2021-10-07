using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using TMProExtensions;

/*
Attached to the customizable carousel panel (v2.0).
Labels and additional images are added dynamically based on data from the admin tool.
*/

public class CarouselPanelCustom : CarouselPanelBase
{
	private const string SPECIAL_CAROUSEL_IMAGE_URL = "{0}/CarouselBG.jpg";	// Feature-specific carousel images.
	private const string NORMAL_CAROUSEL_IMAGE_URL = "lobby_carousel/{0}";	// General carousel images that can be chosen on the admin tool.
	public const string DEFAULT_FONT_NAME = "OpenSans-Bold SDF";

	public GameObject elementsParent;	// This is in the bottom left corner of the slide, so positioning is based on that corner as 0,0.
	public GameObject imageTemplate;
	public GameObject labelTemplate;
	
	private GameTimer timer = null;		// Some slides use this, depending on the slide's action.
	private GameTimer textUpdateTimer = null;  // used to update text displaying the timer value, once a second
	private bool didDeactivate = false;

	// Keep track of which labels are to show the timer. The string is the localization key to use.
	// If the string is empty, then only the time remaining is shown. If not empty and there is a token,
	// then the timer remaining is plugged into the token of the localization.
	private Dictionary<TextMeshPro, TimerLabel> timerLabels;
	
	private class TimerLabel
	{
		public string localizationKey = "";
		public bool isAllCaps = false;
		
		public TimerLabel(JSON json)
		{
			localizationKey = json.getString("localization_key", "");
			isAllCaps = json.getBool("is_all_caps", false);
		}
	}
	
	public override void init()
	{
		labelTemplate.SetActive(false);
		imageTemplate.SetActive(false);
		
		timer = DoSomething.getTimer(data.action);
		
		// Deal with labels.
		if (data.textData != null)
		{
			for (int i = 0; i < data.textData.Length; i++)
			{
				createLabel(i);
			}
		}
		
		// Deal with the automatic background image.
		createBackgroundImage();

		// Deal with admin-tool defined images.
		if (data.imageData != null)
		{
			for (int i = 0; i < data.imageData.Length; i++)
			{
				createImageFromData(i);
			}
		}
			
		if (timerLabels != null)
		{
			updateTimerLabelText();
			textUpdateTimer = new GameTimer(1.0f);  // update once/sec
		}
	}
	
	void Update()
	{
		if (didDeactivate)
		{
			// Sometimes it takes more than a frame to truly deactivate and destroy the slide,
			// so make sure no more processing happens on this slide if we deactivated it.
			// Originally I tried simply setting enabled = false, but that stops coroutines
			// from completing, causing problems for streaming textures.
			return;
		}
		
		if (timer != null)
		{				
			if (timer.isExpired)
			{
				timer = null;	// Prevent this from being called multiple times after the timer expires once.
				
				// The sale expired, so this slide must be removed from the carousel when scrolling is done.
				deactivate();
				return;
			}
		}
		
		if (data != null && !data.getIsValid())
		{
			deactivate();
			return;
		}

		if (textUpdateTimer!=null && textUpdateTimer.isExpired)
		{
			updateTimerLabelText();
			textUpdateTimer.startTimer(1.0f);  // update once/sec
		}
	}

	private void deactivate()
	{
		data.deactivate();
		didDeactivate = true;
	}
			
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
	private void createLabel(int i)
	{
		JSON json = data.textData[i];
		
		GameObject go = NGUITools.AddChild(elementsParent, labelTemplate);
		go.SetActive(true);
		
		TextMeshPro label = go.GetComponent<TextMeshPro>();
		label.makeMaterialInstance();
		
		string dataType = json.getString("data", "");
		
		if (timer != null && dataType == "timer")
		{
			if (timerLabels == null)
			{
				timerLabels = new Dictionary<TextMeshPro, TimerLabel>();
			}
			timerLabels.Add(label, new TimerLabel(json));
		}
		
		if (dataType != "")
		{
			// Localization with dynamic data plugged in.
			setLabelValue(
				label,
				json.getString("localization_key", ""),
				dataType,
				json.getBool("is_all_caps", false)
			);
		}
		else
		{
			// Standard localization without dynamic data.
			if (json.getBool("is_all_caps", false))
			{
				label.text = Localize.textUpper(json.getString("localization_key", ""));
			}
			else
			{
				label.text = Localize.text(json.getString("localization_key", ""));
			}
		}

		applyLabelDesignData(label, json);
	}

	private void updateTimerLabelText()
	{
		foreach (TextMeshPro label in timerLabels.Keys)
		{
			TimerLabel timerLabel = timerLabels[label];
			setLabelValue(
				label,
				timerLabel.localizationKey,
				"timer",
				timerLabel.isAllCaps
			);
		}
	}
	
	// Applies the design-related properties to a label from the JSON data.
	public static void applyLabelDesignData(TextMeshPro label, JSON json)
	{
		if (label == null || json == null)
		{
			return;
		}
		
		label.textContainer.width = json.getInt("width", 0);
		label.textContainer.height = json.getInt("height", 0);
		label.lineSpacing = json.getInt("line_spacing", 0);
		
		setLabelPivot(label, json);

		Transform labelTransform = applyLabelSize(
			label,
			json.getFloat("scale_x", 1.0f),
			json.getFloat("scale_y", 1.0f)
		);
		
		Shader shader = label.fontSharedMaterial.shader;	// Remember the shader to use on the new material instance below.
		
		string fontName = json.getString("font", DEFAULT_FONT_NAME);
		label.font = TMProFontLoader.getFont(fontName);
				
		label.makeMaterialInstance(shader);	// Must do this whenever the font is changed since doing that assigns the default material for that font.
		
		string fontFaceTextureName = json.getString("font_face_texture", "");
		Texture tex = null;
		if (fontFaceTextureName != "")
		{
			tex = getFontFaceTexture(fontFaceTextureName);
		}
		if (fontFaceTextureName != "" && tex == null)
		{
			Debug.LogWarning("Carousel Slide: Could not find font_face_texture: " + fontFaceTextureName);
		}
		label.setFaceTexture(tex);
		
		label.color = CommonColor.colorFromHex(json.getString("color", "FF000000"));
		label.fontStyle = (FontStyles)json.getInt("style", 0);
		label.enableAutoSizing = true;
		label.fontSizeMin = 18;
		label.fontSizeMax = json.getInt("max_font_size", 72);
		
		setExtraPadding(label);

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
		
		labelTransform.localPosition = new Vector3(
			json.getInt("x", 0),
			json.getInt("y", 0),
			-10.0f
		);
		
		labelTransform.localEulerAngles = new Vector3(0, 0, json.getInt("rotation", 0));
		
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
	
	// Depending on the font, we want "Extra Padding" on or off.
	public static void setExtraPadding(TextMeshPro label)
	{
		switch (label.font.name)
		{
			case "Teko-Bold SDF":
			case "PollerOne SDF":
				label.extraPadding = true;
				break;
			
			default:
				label.extraPadding = false;
				break;
		}
	}
	
	private static void setLabelPivot(TextMeshPro label, JSON json)
	{
		// TODO:UNITY2018:obsoleteTextContainer:confirm
		switch (json.getString("alignment", "center"))
		{
			case "left":
				label.alignment = TextAlignmentOptions.Left;
				break;
			case "right":
				label.alignment = TextAlignmentOptions.Right;
				break;
			case "top":
				label.alignment = TextAlignmentOptions.Top;
				break;
			case "bottom":
				label.alignment = TextAlignmentOptions.Bottom;
				break;
			case "center":
				label.alignment = TextAlignmentOptions.Center;
				break;
			case "top_left":
				label.alignment = TextAlignmentOptions.TopLeft;
				break;
			case "bottom_left":
				label.alignment = TextAlignmentOptions.BottomLeft;
				break;
			case "top_right":
				label.alignment = TextAlignmentOptions.TopRight;
				break;
			case "bottom_right":
				label.alignment = TextAlignmentOptions.BottomRight;
				break;
		}
		
		enforceAlignment(label);
	}

	// Make sure the alignment option matches the label's pivot position.
	public static void enforceAlignment(TextMeshPro label)
	{
		// TODO:UNITY2018:obsoleteTextContainer:confirm
		// Enforce label.pivot matches alignment as per old TextContainer.
		TMProExtensions.TMProExtensionFunctions.SetPivotAndAlignmentFromTextContainer(label);
	}

	// Applies the sizing information and returns the effective Transform for the label.
	// Now we only allow scaling on the X, but Y is still here for backwards compatibility with older data.
	public static Transform applyLabelSize(TextMeshPro label, float scaleX, float scaleY)
	{
		Transform labelTransform = label.transform;

		scaleX = Mathf.Clamp(scaleX, 0.5f, 2.0f);
		scaleY = Mathf.Clamp(scaleY, 0.5f, 1.0f);
		
		labelTransform.localScale = new Vector3(scaleX, scaleY, 1.0f);
		
		return labelTransform;
	}
	
	// Creates a texture for the background image for actions that automatically determine the URL.
	private void createBackgroundImage()
	{
		string url = "";

		if (data.action.FastStartsWith("xpromo"))
		{
			// Special case where we need to know the parameter of the action.
			url = MobileXpromo.getCarouselImagePath();
		}
		else
		{
			switch (data.action)
			{
				case "happy_hour_sale":
				case "payer_reactivation_sale":
				case "popcorn_sale":
				case "vip_sale":
					// All of these are different STUD sales.
					STUDSale sale = STUDSale.getSaleByAction(data.action);
					if (sale != null)
					{
						if (sale.saleType == SaleType.POPCORN && ExperimentWrapper.PopcornVariantTest.isInExperiment)
						{
							url = PopcornVariant.currentCarouselPath;
						}
						else
						{
							url = string.Format(SPECIAL_CAROUSEL_IMAGE_URL, sale.featureData.imageFolderPath);
						}
					}
					break;
				case "multiplier_sale":
				case "percentage_sale":
				case "buycoins_new_sale":
					url = BuyCreditsDialog.carouselImagePath;
					break;
			}
		}
		
		if (url != "")
		{
			// The image template is sized at the background size by default.
			Vector3 size = imageTemplate.transform.localScale;
			// Center the background image, since 0,0 is the lower left corner.
			Vector3 position = new Vector3(size.x * 0.5f, size.y * 0.5f, 10.0f);
			createImage(url, size, position, 0);
		}
	}

	
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
	private void createImageFromData(int i)
	{
		JSON json = data.imageData[i];
		
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
			// No URL override data was found, so use the normal URL as-is, plugged into the standard base path.
			url = string.Format(NORMAL_CAROUSEL_IMAGE_URL, json.getString("url", ""));
		}
		
		createImage(url, size, position, json.getInt("rotation", 0));
	}
	
	// Creates an image with the given arguments.
	private void createImage(string url, Vector3 size, Vector3 position, int rotation)
	{
		GameObject go = NGUITools.AddChild(elementsParent, imageTemplate);
		go.SetActive(true);
		
		go.transform.localScale = size;
		go.transform.localPosition = position;
		go.transform.localEulerAngles = new Vector3(0, 0, rotation);
		
		loadTexture(go.GetComponent<Renderer>(), url);		
	}
	
	public static void applyImageDesignData(Transform imageTransform, JSON json)
	{
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
		
		imageTransform.localScale = size;
		imageTransform.localPosition = position;
		imageTransform.localEulerAngles = new Vector3(0, 0, json.getInt("rotation", 0));
	}
	
	// Intelligently sets a label's value, possibly as part of a localization and possibly with dynamic data.
	private void setLabelValue(TextMeshPro label, string locKey, string dataType, bool isAllCaps)
	{
		string dataValue = getData(dataType);
		
		if (string.IsNullOrEmpty(locKey))
		{
			// No localization key, so just use the dynamic value.
			label.text = dataValue;
		}
		else if (locKey.Contains("{0}"))
		{
			if (isAllCaps)
			{
				label.text = Localize.textUpper(locKey, dataValue);
			}
			else
			{
				label.text = Localize.text(locKey, dataValue);
			}
		}
	}

	// Returns data based on the "data" or "url_override" field in text and image JSON blocks respectively.
	// Note: If adding or removing strings, please also update the wiki appropriately:
	// https://wiki.corp.zynga.com/display/hititrich/Carousel+Slides+2.0
	public string getData(string dataType)
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
					dataValue = timer.timeRemainingFormatted;
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

			case "xpromo_url":
				dataValue = MobileXpromo.getCarouselImagePath();
				break;
					
				// Note: If adding or removing strings, please also update the wiki appropriately:
				// https://wiki.corp.zynga.com/display/hititrich/Carousel+Slides+2.0
		}
		
		return dataValue;
	}
	
	// Returns the font texture that is associated with the given texture name.
	public static Texture2D getFontFaceTexture(string name)
	{
		return SkuResources.loadSkuSpecificResourcePNG(string.Format("initialization/tmpro textures/{0}", name));
	}
}
