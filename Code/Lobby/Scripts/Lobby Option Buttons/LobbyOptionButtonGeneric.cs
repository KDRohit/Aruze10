using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/**
Controls UI behavior of a menu option button in the main lobby.
*/

public class LobbyOptionButtonGeneric : LobbyOptionButtonLockable
{
	private const string BANNER_DEFAULT_IMAGE_PATH = "Assets/Data/HIR/Bundles/Initialization/Features/Lobby V3/Textures/LobbyBannerDefault";
	
	private const float JACKPOT_THROB_FREQUENCY = 5.0f;

	[SerializeField] private GameObject frameSparklePrefabOverride2X2;
	[SerializeField] private GameObject frameSparklePrefabOverride3X2;
	
	[SerializeField] Transform frameSizer = null;
	[SerializeField] UISprite genericFrame = null;
	[SerializeField] UISprite vipFrame = null;
	[SerializeField] GameObject vipRoomLabelBacking = null;
	[SerializeField] TextMeshPro vipRoomLabel; // to be used for masking only
	[SerializeField] VIPNewIcon vipLockObject;
	[SerializeField] GameObject vip2X2AmbientAnimationPrefab;
	[SerializeField] GameObject vip3X2AmbientAnimationPrefab;
	
	public UISprite frameVisibleLocked = null;
	public GameObject shroudLocked = null;
	
	// these are the parent anchors TBD we really only need one now that we are using overlays
	public GameObject jackpotHeader;
	public GameObject jackpotHeader1X2;
	public GameObject multiProgressiveHeader;
	public GameObject giantJackpotHeader;
	public GameObject highLimitHeader;
	public GameObject mysteryGiftHeader;
	public GameObject mysteryGiftHeader1X2;
	public GameObject bigSliceHeader;
	public GameObject bigSliceHeader1X2;
	public GameObject royalRushHeader;
	public GameObject reevaluatorJackpotHeader;
	public GameObject reevaluatorJackpotHeader1x2;
	public GameObject personalizedContentHeader;
	public GameObject extraFeatureHeader;
	public GameObject extraFeatureHeader1x2;
	public GameObject richPassHeader;
	public GameObject richPassMultiJackpotHeader1x2;
	public GameObject recommendedHeader1x2;
	public GameObject playAgainHeader1x2;
	public GameObject unlockLockParent1x2;     // lock icon and text for 1x2 sized cards
	public GameObject lockObject;
	public GameObject anchorBottomLeftShadow; // Shadow that may need to be hidden when some special frames are displayed
	public UIStretch gameImageStretch;
	public UIAnchor gameImageAnchor;
	public UIAnchor[] movingAnchors; //Anchor parents for objects that move based on being 1x1 or 1x2
	public UIStretch[] changingStretches; //UIStretches that need to change based on being 1x1 or 1x2

	public LobbyOptionDecorator buttonOverlay;

	private string localizedLimitedTime = "";	// Cache it for performance.
	private Throbamatic jackpotHeaderThrobber = null;

	private const float LOCK_1x2_OFFSET_Y = 220.0f;
	[System.NonSerialized] public UISprite frameVisible = null;

	private LobbyOptionDecorator jackpotDecorator = null;
	private LobbyOptionDecorator jackpotDecorator1X2 = null;
	private LobbyOptionDecorator multiProgressiveJackpotDecorator = null;
	private LobbyOptionDecorator giantJackpotDecorator = null;
	private LobbyOptionDecorator highLimitDecorator = null;
	private LobbyOptionDecorator bigSliceDecorator = null;
	private LobbyOptionDecorator bigSliceDecorator1X2 = null;
	private LobbyOptionDecorator mysteryGiftDecorator = null;
	private LobbyOptionDecorator mysteryGiftDecorator1x2 = null;
	private LobbyOptionDecorator personalizedContentDecorator = null;
	private LobbyOptionDecorator royalRushDecorator = null;
	private LobbyOptionDecorator reevaluatorJackpotDecorator = null;
	private LobbyOptionDecorator reevaluatorJackpotDecorator1x2 = null;
	private LobbyOptionDecorator extraFeatureDecorator = null;
	private LobbyOptionDecorator extraFeatureDecorator1x2 = null;
	private LobbyOptionDecorator richPassDecorator = null;
	private LobbyOptionDecorator richPassMultiJackpotDecorator1x2 = null;
	private LobbyOptionDecorator recommendedDecorator1x2 = null;
	private LobbyOptionDecorator playAgainDecorator1x2 = null;

	private Vector3 defaultFrameSizerScale;
	private Vector3 defaultFramSizerPosition;
	private bool isVipLocked = false;

	public override void setup(LobbyOption option, int page, float width, float height)
	{
		reset();

		defaultFrameSizerScale = frameSizer.localScale;
		defaultFramSizerPosition = frameSizer.localPosition;

		base.setup(option, page, width, height);

		if (option != null && option.game != null && buttonOverlay != null && buttonOverlay.featuredParent != null)
		{
			buttonOverlay.featuredParent.SetActive(option.game.isUnlocked && option.game.isUnlockForAll() && !hideFeatureLabel);
		}
		
		// Enable the shadow by default and disable if a special frame doesn't want it shown
		anchorBottomLeftShadow.SetActive(false);
		
		// Hide the dynamic text by default unless it's needed.
		dynamicTextParent.gameObject.SetActive(false);

		if (width > 0 && height > 0)
		{
			Vector3 size = frameSizer.localScale;
			size.x = width;
			size.y = height;
			frameSizer.localScale = size;
	
			Vector3 pos = frameSizer.localPosition;
			pos.x = width / 2;
			pos.y = height / -2;
			frameSizer.localPosition = pos;
		}

		hideFeatureLabel = false;

		bool is1x2 = option != null && option.isPinned && option.pinned.shape == Pinned.Shape.BANNER_1X2;
		
		lockObject.transform.parent = unlockLockParent.transform;
		lockObject.transform.localScale = Vector3.one;
		frameVisible = genericFrame;
		

		// fix anchors
		if (unlockLockParent != null)
		{
			UIAnchor lockAnchor = unlockLockParent.transform.parent.GetComponent<UIAnchor>();
			if (lockAnchor != null && !lockAnchor.isActiveAndEnabled)
			{
				lockAnchor.enabled = true;
			}
		}
				
		switch (option.type)
		{
			case LobbyOption.Type.ACTION:
				setupActionOption();
				break;
			case LobbyOption.Type.GAME:
				setupGameOption(is1x2);
				break;
			
			// Maybe look for other types of options here and do something below with it.
		}

		for (int i = 0; i < movingAnchors.Length; i++)
		{
			movingAnchors[i].enabled = true;
		}

		for(int i = 0; i < changingStretches.Length; i++)
		{
			changingStretches[i].enabled = true;
		}

		refresh();
	}

	// Do not allow sparkles in generic button until the image is loaded and enabled
	protected override bool canShowSparkleAnimation()
	{
		return image.activeInHierarchy;
	}
	
	private void setupActionOption()
	{
		switch (option.action)
		{
			case "personalized_content":
				LobbyGame personalGame = LobbyGame.find(PersonalizedContentLobbyOptionDecorator1x2.gameKey);
				if (personalGame != null)
				{
					option.pinned.imageFilename = option.imageFilename = SlotResourceMap.getLobbyImagePath(personalGame.groupInfo.keyName, personalGame.keyName, "1X2");
				}

				if (personalizedContentDecorator == null)
				{
					PersonalizedContentLobbyOptionDecorator1x2.loadPrefab(personalizedContentHeader, this);
					personalizedContentDecorator = buttonOverlay;
				}
				else
				{
					buttonOverlay = jackpotDecorator;
				}

				personalizedContentHeader.SetActive(true);
				break;
			default:
				bool isRecommendedGame = option.action.StartsWith(DoSomething.RECOMMENDED_GAME_PREFIX);
				bool isFavoriteGame = option.action.StartsWith(DoSomething.FAVORITE_GAME_PREFIX);
				if (isRecommendedGame || isFavoriteGame)
				{
					SafeSet.gameObjectActive(lockObject, false);
					SafeSet.gameObjectActive(shroudLocked, false);

					// Show decorator
					LobbyOptionDecorator decorator = isFavoriteGame ? playAgainDecorator1x2 : recommendedDecorator1x2;
					if (decorator == null)
					{
						playAgainDecorator1x2 = buttonOverlay;
					}
					else
					{
						buttonOverlay = playAgainDecorator1x2;
					}

					// Get correct game image path and load header
					GameObject header = isFavoriteGame ? playAgainHeader1x2 : recommendedHeader1x2;
					int colonIndex = option.action.IndexOf(':');
					if (colonIndex != -1)
					{
						string actionIndex = option.action.Substring(colonIndex + 1).Trim();

						string gameName = "";
						if (isRecommendedGame)
						{
							gameName = ExperimentWrapper.PersonalizedContent.getRecommandedGameName(actionIndex);
							RecommendedLobbyOptionDecorator1x2.loadPrefab(header, this);
						}
						else
						{
							gameName = ExperimentWrapper.PersonalizedContent.getFavoriateGameName(actionIndex);
							FavoriteLobbyOptionDecorator1x2.loadPrefab(header, this);
						}

						LobbyGame actionGame = LobbyGame.find(gameName);
						if (actionGame != null)
						{
							option.pinned.imageFilename = option.imageFilename =
								SlotResourceMap.getLobbyImagePath(actionGame.groupInfo.keyName,
									actionGame.keyName, "1X2");
						}
					}

					SafeSet.gameObjectActive(frameVisible.gameObject, true);
					SafeSet.gameObjectActive(header, true);
				}
				else if (option.isBannerAction)
				{
					SafeSet.gameObjectActive(lockObject, false);
					SafeSet.gameObjectActive(shroudLocked, false);
					bool shouldShowVipAnimation = false;
					if (option.action.StartsWith(DoSomething.GAME_PREFIX))
					{
						string action = option.action;
						string gameId;
						// Separate the gameid from "game:gameid" we dont want to make changes to original string 
						DoSomething.splitActionString(ref action, out gameId);
						if (!string.IsNullOrWhiteSpace(gameId))
						{
							LobbyGame game = LobbyGame.find(gameId);
							
							if (game != null && SlotResourceMap.getData(gameId) != null)
							{
								if (game.isGoldPassGame)
								{
									if (richPassDecorator == null)
									{
										RichPassLobbyOptionDecorator.loadPrefab(richPassHeader, this,
											option.isPinned ? Pinned.getFilePathPostFix(option.pinned.shape): null);
										richPassDecorator = buttonOverlay;
									}
									else
									{
										buttonOverlay = richPassDecorator;
									}
									richPassHeader.SetActive(true);
								}
								else
								{
									if (game.isVIPGame && vipFrame != null)
									{
										frameVisible = vipFrame;
										SafeSet.gameObjectActive(vipRoomLabelBacking, true);
										MainLobby.hirV3.masker.addObjectToList(vipRoomLabel);

										isVipLocked = EueFeatureUnlocks.isFeatureUnlocked("vip_revamp") &&
										                game.vipLevel.levelNumber >
										                SlotsPlayer.instance.adjustedVipLevel;

										if (vipLockObject != null)
										{
											vipLockObject.gameObject.SetActive(isVipLocked);
											if (isVipLocked)
											{
												vipLockObject.setLevel(game.vipLevel.levelNumber);
											}
										}
										SafeSet.gameObjectActive(shroudLocked, isVipLocked);

										shouldShowVipAnimation = true;
									}
									else
									{

										bool unlockAllGames = UnlockAllGamesFeature.instance != null &&
										                      UnlockAllGamesFeature.instance.isEnabled;
										bool isLocked = (!game.isUnlocked && !unlockAllGames && PersonalizedContentLobbyOptionDecorator1x2.gameKey != gameId);

										if (isLocked && !game.isGoldPassGame)
										{
											// Set the unlock text to the required level.
											setUnlockLevel(game.unlockLevel);

											shroudLocked.SetActive(true);
											SafeSet.gameObjectActive(lockObject, true);
											SafeSet.gameObjectActive(shroudLocked, true);
										}
									}
								}
								// this call must be made to ensure the game's lock/unlock state is correctly set
								game.setIsUnlocked();
							}
							else
							{
								// the game is not available
								// Show the default banners asking people to update to get new games
								option.action = "appstore"; // loads appstore/playstore on click
								option.defaultBanner = true;
							}	
						}
					}

					if (option.pinned != null)
					{
						option.bannerLoadingImageName = BANNER_DEFAULT_IMAGE_PATH + Pinned.getFilePathPostFix(option.pinned.shape) +".png";
						GameObject vipAmbientAnimPrefab = null;
						switch (option.pinned.shape)
						{
							case Pinned.Shape.BANNER_3X2:
								frameSparklePrefab = frameSparklePrefabOverride3X2;
								vipAmbientAnimPrefab = vip3X2AmbientAnimationPrefab;
								break;
							case Pinned.Shape.BANNER_2X2:
								frameSparklePrefab = frameSparklePrefabOverride2X2;
								vipAmbientAnimPrefab = vip2X2AmbientAnimationPrefab;
								break;
							// dont do anything for other cases
						}

						if (shouldShowVipAnimation && vipAmbientAnimPrefab != null)
						{
							// just instantiating under the parent is sufficient
							CommonGameObject.instantiate(vipAmbientAnimPrefab, transform);
						}
					}
					frameVisible.gameObject.SetActive(true);
				}
				break;
		}
	}

	private void setupGameOption(bool is1x2)
	{
		if (option.game == null)
		{
			return;
		}
		// only allow feature locking when using the old wager system
		bool isPersonalizedContentGame = PersonalizedContentLobbyOptionDecorator1x2.gameKey == option.game.keyName;
		bool isLocked = ((!option.game.isUnlocked && (UnlockAllGamesFeature.instance == null || !UnlockAllGamesFeature.instance.isEnabled)) && !isPersonalizedContentGame);
		
		if (isLocked && !option.isGoldPass)
		{
			// Set the unlock text to the required level.
			setUnlockLevel(option.game.unlockLevel);

			// If locked, don't show the dynamic text, since the lock occupies the same space.
			dynamicTextParent.gameObject.SetActive(false);
		}
		else if (isPersonalizedContentGame)
		{
			lockObject.SetActive(false);
		}
		else
		{
			setUnlockLevel(0);
			lockObject.SetActive(false);
		}
		
		// Change the frame to use the locked frame sprite if necessary.
		frameVisible.gameObject.SetActive(!isLocked);
		shroudLocked.SetActive(isLocked && !option.isGoldPass);
						
		// Handle the jackpot header.
		bool doShowJackpot = (option.game.isProgressive && !option.isGoldPass) || option.isProgressive1X2;
		bool haveOverlay = false;
		
		if (option.game.isBuiltInProgressive)
		{
			// Frame handling for built in progressive games like elvis03 and wicked02
			string builtInProgressiveLobbyFrameName = SlotResourceMap.getBuiltInProgressiveLobbyFrameName(option.game.keyName);
			JackpotLobbyOptionDecorator.JackpotTypeEnum builtInProvessiveLobbyFrameType = JackpotLobbyOptionDecorator.getTypeEnumFromString(builtInProgressiveLobbyFrameName);
		
			if (option.isProgressive1X2)
			{
				if (reevaluatorJackpotDecorator1x2 == null)
				{
					JackpotLobbyOptionDecorator1x2.loadPrefab(reevaluatorJackpotHeader1x2, this, builtInProvessiveLobbyFrameType);
					reevaluatorJackpotDecorator1x2 = buttonOverlay;
				}
				else
				{
					buttonOverlay = reevaluatorJackpotDecorator1x2;
				}

				reevaluatorJackpotHeader1x2.SetActive(true);
			}
			else
			{
				if (reevaluatorJackpotDecorator == null)
				{
					JackpotLobbyOptionDecorator.loadPrefab(reevaluatorJackpotHeader, this, builtInProvessiveLobbyFrameType);
					reevaluatorJackpotDecorator = buttonOverlay;
				}
				else
				{
					buttonOverlay = reevaluatorJackpotDecorator;
				}
				
				reevaluatorJackpotHeader.SetActive(true);
			}

			// Hide the shadow, since it will cause render issues
			anchorBottomLeftShadow.SetActive(false);
			haveOverlay = true;
		}
		else if (option.isGiantProgressive1X2)
		{
			if (giantJackpotDecorator == null)
			{
				GiantJackpotLobbyOptionDecorator.loadPrefab(giantJackpotHeader, this);
				giantJackpotDecorator = buttonOverlay;
			}
			else
			{
				buttonOverlay = giantJackpotDecorator;
			}

			giantJackpotHeader.SetActive(true);
			haveOverlay = true;
		}
		else if (doShowJackpot)
		{
			if (option.isProgressive1X2)
			{
				if (option.game.isMultiProgressive)
				{
					if (option.isGoldPass)
					{
						if (richPassMultiJackpotDecorator1x2 == null)
						{
							RichPassMultiJackpotLobbyOptionDecorator1x2.loadPrefab(richPassMultiJackpotHeader1x2, this);
							richPassMultiJackpotDecorator1x2 = buttonOverlay;
						}
						else
						{
							buttonOverlay = richPassMultiJackpotDecorator1x2;
						}
						richPassMultiJackpotHeader1x2.SetActive(true);
						
						//Normally done in the setup of the decorator but being done here since its being recycled
						gameImageStretch.relativeSize.y = RichPassMultiJackpotLobbyOptionDecorator1x2.TEXTURE_RELATIVE_Y;
						gameImageAnchor.pixelOffset.y = RichPassMultiJackpotLobbyOptionDecorator1x2.TEXTURE_ANCHOR_PIXEL_OFFSET_Y;
					}
					else
					{
						if (multiProgressiveJackpotDecorator == null)
						{
							MultiJackpotLobbyOptionDecorator1x2.loadPrefab(multiProgressiveHeader, this);
							multiProgressiveJackpotDecorator = buttonOverlay;
						}
						else
						{
							buttonOverlay = multiProgressiveJackpotDecorator;
						}
						multiProgressiveHeader.SetActive(true);	
						
						//Normally done in the setup of the decorator but being done here since its being recycled
						gameImageStretch.relativeSize.y = MultiJackpotLobbyOptionDecorator1x2.TEXTURE_RELATIVE_Y;
						gameImageAnchor.pixelOffset.y = MultiJackpotLobbyOptionDecorator1x2.TEXTURE_ANCHOR_PIXEL_OFFSET_Y;
					}
					
					//enable anchor and stretch to reposition
					gameImageStretch.enabled = true;
					gameImageAnchor.enabled = true;

				}
				else
				{
					if (jackpotDecorator1X2 == null)
					{
						JackpotLobbyOptionDecorator1x2.loadPrefab(jackpotHeader1X2, this, JackpotLobbyOptionDecorator.JackpotTypeEnum.Default);
						jackpotDecorator1X2 = buttonOverlay;
					}
					else
					{
						buttonOverlay = jackpotDecorator1X2;
					}
					jackpotHeader1X2.SetActive(true);
				}
			}
			else 
			{
				if (jackpotDecorator == null)
				{
					JackpotLobbyOptionDecorator.loadPrefab(jackpotHeader, this, JackpotLobbyOptionDecorator.JackpotTypeEnum.Default);
					jackpotDecorator = buttonOverlay;
				}
				else
				{
					buttonOverlay = jackpotDecorator;
				}
				jackpotHeader.SetActive(true);					
			}
			haveOverlay = true;
		}
		
		else if (option.isGoldPass)
		{
			if (richPassDecorator == null)
			{
				RichPassLobbyOptionDecorator.loadPrefab(richPassHeader, this,
					option.isPinned ? Pinned.getFilePathPostFix(option.pinned.shape) : null);

				richPassDecorator = buttonOverlay;
			}
			else
			{
				buttonOverlay = richPassDecorator;
			}
			richPassHeader.SetActive(true);
			haveOverlay = true;
		}
		else if(option.game.extraFeatureType != ExtraFeatureType.NONE)
		{
			if (is1x2)
			{
				if (extraFeatureDecorator1x2 == null)
				{
					ExtraFeatureLobbyOptionDecorator1x2.loadPrefab(extraFeatureHeader1x2, this);
					extraFeatureDecorator1x2 = buttonOverlay;
				}
				else
				{
					buttonOverlay = extraFeatureDecorator1x2;
				}

				extraFeatureHeader1x2.SetActive(true);
			}
			else
			{
				if (extraFeatureDecorator == null)
				{
					ExtraFeatureLobbyOptionDecorator.loadPrefab(extraFeatureHeader, this);
					extraFeatureDecorator = buttonOverlay;
				}
				else
				{
					buttonOverlay = extraFeatureDecorator;
				}

				extraFeatureHeader.SetActive(true);
			}
			haveOverlay = true;
		}

		//Show the locked frame if the game is locked and its a 1x1
		//If its a 1x2 then show the locked frame if it doesn't have a mystery gift type and isn't showing some other overlay
		frameVisibleLocked.gameObject.SetActive(isLocked && (!is1x2 || ((option.game.mysteryGiftType == MysteryGiftType.NONE) && !haveOverlay)));
		// Handle the High Limit card options.
		bool doShowHighLimitHeader = (option.game != null && option.game.isHighLimit && !haveOverlay);
		
		// Toggle the high Limit header if it is a high Limit game only if it is 1x1
		// Same for mystery gift and big slice games.
		// Hide all headers by default.
		if (doShowHighLimitHeader && !is1x2)
		{
			highLimitHeader.SetActive(true);
			if (highLimitDecorator == null)
			{
				HighLimitLobbyOptionDecorator.loadPrefab(highLimitHeader, this);
				highLimitDecorator = buttonOverlay;
			}
			else
			{
				buttonOverlay = highLimitDecorator;
			}
			mysteryGiftHeader.SetActive(false);
			bigSliceHeader.SetActive(false);
		}
		else
		{
			// Only show mystery gift and big slice header if not showing high limit header.
			highLimitHeader.SetActive(false);
			if (option.game.mysteryGiftType == MysteryGiftType.MYSTERY_GIFT)
			{
				if (option.isMysteryGift1X2)
				{
					if (mysteryGiftDecorator1x2 == null)
					{
						MysteryGiftLobbyOptionDecorator1x2.loadPrefab(mysteryGiftHeader1X2, this);
						mysteryGiftDecorator1x2 = buttonOverlay;
					}
					else
					{
						buttonOverlay = mysteryGiftDecorator1x2;
					}
					mysteryGiftHeader1X2.SetActive(true);
				}
				else
				{
					if (mysteryGiftDecorator == null)
					{
						MysteryGiftLobbyOptionDecorator.loadPrefab(mysteryGiftHeader, this);
						mysteryGiftDecorator = buttonOverlay;
					}
					else
					{
						buttonOverlay = mysteryGiftDecorator;
					}
					mysteryGiftHeader.SetActive(true);
				}

				if (option.button is LobbyOptionButtonMysteryGift)
				{							
					if ((option.button as LobbyOptionButtonMysteryGift).mysteryGiftIncreasedChanceIcon.activeSelf)
					{
						if (buttonOverlay != null && buttonOverlay.featuredParent != null)
						{
							buttonOverlay.featuredParent.SetActive(false);
						}
					}
				}
			}
			else if (option.game.mysteryGiftType == MysteryGiftType.BIG_SLICE)
			{
				if (option.isBigSlice1X2)
				{
					if (bigSliceDecorator1X2 == null)
					{
						BigSliceLobbyOptionDecorator1x2.loadPrefab(bigSliceHeader1X2, this);
						bigSliceDecorator1X2 = buttonOverlay;
					}
					else
					{
						buttonOverlay = bigSliceDecorator1X2;
					}
					bigSliceHeader1X2.SetActive(true);
				}
				else
				{
					if (bigSliceDecorator == null)
					{
						BigSliceLobbyOptionDecorator.loadPrefab(bigSliceHeader, this);
						bigSliceDecorator = buttonOverlay;
					}
					else
					{
						buttonOverlay = bigSliceDecorator;
					}
					bigSliceHeader.SetActive(true);
				}
			}
			
			if (option.game.isRoyalRush)
			{
				if (royalRushDecorator == null)
				{
					RoyalRushLobbyOptionDecorator.loadPrefab(royalRushHeader, this);
					royalRushDecorator = buttonOverlay;
				}
				else
				{
					buttonOverlay = royalRushDecorator;
				}
				
				royalRushHeader.SetActive(true);
				frameVisible.gameObject.SetActive(true);

				if (buttonOverlay != null && buttonOverlay.featuredParent != null)
				{
					buttonOverlay.featuredParent.SetActive(false);
				}
			}
		}

	#if RWR
		// Create the real world rewards UI element if necessary.
		createRWR();
	#endif
	}
	
	protected override void Update()
	{
		base.Update();
		
		if (jackpotHeaderThrobber != null)
		{
			jackpotHeaderThrobber.update();
		}
	}

	protected override void OnClick()
	{
		if (option.isBannerAction)
		{
			StatsManager.Instance.LogCount(counterName:"lobby", kingdom:"select_banner", phylum:option.action, klass: option.defaultBanner? "default" : option.imageFilename, genus:"click");
		}
		if (!isVipLocked) // ignore clicks for locked vip game banners
		{
			base.OnClick();
		}
	}
	
	public void hideFrames(bool hideFeatured, bool hideLocked)
	{
		if (frameVisible != null)
		{
			SafeSet.gameObjectActive(frameVisible.gameObject, false);
		}

		if (frameVisibleLocked != null)
		{
			SafeSet.gameObjectActive(frameVisibleLocked.gameObject, false);
		}
	
		if (hideLocked)
		{
			SafeSet.gameObjectActive(lockObject, false);
			SafeSet.gameObjectActive(shroudLocked, false);
		}

		if (hideFeatured && buttonOverlay != null && buttonOverlay.featuredParent != null)
		{
			buttonOverlay.featuredParent.SetActive(false);
		}
	}

	/// Force a refresh of some visible element, initially going to be used to control 
	/// lock icons on options that need to be displayed or hidden based on using the old or new wager system
	public override void refresh()
	{
		base.refresh();
		if (option != null && option.game != null)
		{
			// only allow feature locking when using the old wager system
			bool isLocked = (!option.game.isUnlocked && (UnlockAllGamesFeature.instance == null || !UnlockAllGamesFeature.instance.isEnabled));
			if (isLocked)
			{
				// Set the unlock text to the required level.
				setUnlockLevel(option.game.unlockLevel);

				// If locked, don't show the dynamic text, since the lock occupies the same space.
				if (dynamicTextParent != null)
				{
					dynamicTextParent.gameObject.SetActive(false);
				}
			}
			else
			{
				setUnlockLevel(0);
			}
			SafeSet.gameObjectActive(shroudLocked, isLocked && !option.isGoldPass);

			//Royal Rush, recommmend and play again decorators have a special overlay but also needs to use the standard frame
			if (buttonOverlay != null && buttonOverlay != royalRushDecorator && 
			    buttonOverlay != recommendedDecorator1x2 && 
			    buttonOverlay != playAgainDecorator1x2)
			{
				hideFrames(buttonOverlay.hideFeatured, buttonOverlay.hideLock);
			}
			else
			{
				// Change the frame to use the locked frame sprite if necessary.
				if (frameVisible != null)
				{
					frameVisible.gameObject.SetActive(!isLocked);
				}
				if (frameVisibleLocked != null)
				{	
					frameVisibleLocked.gameObject.SetActive(isLocked);
				}
			}

			if (!gameImageAnchor.isActiveAndEnabled)
			{
				gameImageAnchor.enabled = true;
			}

			if (!gameImageStretch.isActiveAndEnabled)
			{
				gameImageStretch.enabled = true;
			}

			// fix anchors
			if (unlockLockParent != null)
			{
				UIAnchor lockAnchor = unlockLockParent.transform.parent.GetComponent<UIAnchor>();
				if (lockAnchor != null && !lockAnchor.isActiveAndEnabled)
				{
					lockAnchor.enabled = true;
				}
			}
		}
	}

	public override void reset()
	{
		option = null;
		page = 0;
		jackpotHeader.SetActive(false);
		multiProgressiveHeader.SetActive(false);
		giantJackpotHeader.SetActive(false);
		highLimitHeader.SetActive(false);
		mysteryGiftHeader.SetActive(false);
		bigSliceHeader.SetActive(false);
		bigSliceHeader1X2.SetActive(false);
		mysteryGiftHeader1X2.SetActive(false);
		jackpotHeader1X2.SetActive(false);
		personalizedContentHeader.SetActive(false);
		reevaluatorJackpotHeader.SetActive(false);
		reevaluatorJackpotHeader1x2.SetActive(false);
		extraFeatureHeader.SetActive(false);
		extraFeatureHeader1x2.SetActive(false);
		royalRushHeader.SetActive(false);
		richPassHeader.SetActive(false);
		richPassMultiJackpotHeader1x2.SetActive(false);
		frameVisible = null;
		buttonOverlay = null;
		gameImageAnchor.pixelOffset = Vector2.zero;
		gameImageStretch.relativeSize = Vector2.one;
		frameSizer.localPosition = defaultFramSizerPosition;
		frameSizer.localScale = defaultFrameSizerScale;
		lockObject.SetActive(true);

		if (!gameImageAnchor.isActiveAndEnabled)
		{
			gameImageAnchor.enabled = true;
		}

		if (!gameImageStretch.isActiveAndEnabled)
		{
			gameImageStretch.enabled = true;
		}

		image.SetActive(false);
	}
}
