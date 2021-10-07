using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BannerTypeEnum = SlotBaseGame.BannerInfo.BannerTypeEnum;
using TextDirectionEnum =  SlotBaseGame.BannerTextInfo.TextDirectionEnum;
using TextLocationEnum =  SlotBaseGame.BannerTextInfo.TextLocationEnum;
using TMPro;

public class PortalScript : TICoroutineMonoBehaviour
{
	protected const string CHALLENGE_REVEAL_AUDIO_KEY = "portal_reveal_pick_bonus_vo";
	protected const string FREESPIN_REVEAL_AUDIO_KEY = "portal_reveal_freespin_vo";
	
	[SerializeField] private float portalVOSoundDelay = 0.6f;	// Controls a delay after the bonus_portal_bg has started and before playing bonus_portal_vo
	[SerializeField] protected float BEGIN_BONUS_DELAY_TIME = 1.5f;
	[SerializeField] protected float WAIT_TO_DESTROY_PORTAL_BANNERS_DUR = 0.0f;
	[SerializeField] private bool fadeBannersBeforeDestroy = false; // fade revealed portal b anners out with custom animations

	protected Dictionary<BannerTypeEnum, SlotBaseGame.BannerInfo> _bannerMap = null;
	protected List<GameObject> bannerObjects;
	protected List<GameObject> revealedObjects;
	protected bool spinsAdded = false;
	protected bool bonusAdded = false;
	protected long _multiplier = 0;
	
	protected GameObject[] _bannerRoots;
	protected SlotBaseGame.BannerInfo[] _banners;
	protected GameObject _bannerTextOverlay;
	protected SlotOutcome _outcome;
	protected List<GameObject> bannerTextObjects;
	
	protected Vector3 bannerAdjustment;
	protected GameObject _revealVfx;
	protected Vector3[,] orginalBannerPositions;
	protected int _revealedIndex = -1;
	protected bool playingPickMeAnimation = false;

	// Our start to getting a portal to display
	public virtual void beginPortal(GameObject[] bannerRoots, SlotBaseGame.BannerInfo[] banners, GameObject bannerOverlay, SlotOutcome outcome, long multiplier)
	{
		_revealedIndex = -1;
		_banners = banners;
		_bannerRoots = bannerRoots;

		if (bannerOverlay != null)
		{
			_bannerTextOverlay = CommonGameObject.instantiate(bannerOverlay) as GameObject;
		}

		_outcome = outcome;

		if (_outcome.isPortal)
		{
			// We actually don't want the portal here, we need the bonus it contains
			// in order to handle the legacy display of what is being awarded.  So
			// we'll grab the stored information we got when we determined that the outcome contained a portal
			if (_outcome.portalChildBonusOutcome != null)
			{
				_outcome = _outcome.portalChildBonusOutcome;
			}
		}
		
		_multiplier = multiplier;
		bonusAdded = false;
		spinsAdded = false;
		
		// Populate a map for the purposes of generating the banners.
        
		_bannerMap = new Dictionary<BannerTypeEnum, SlotBaseGame.BannerInfo>();
		foreach (SlotBaseGame.BannerInfo banner in _banners)
		{
			_bannerMap.Add(banner.bannerType, banner);
 		}
		
		bannerAdjustment = new Vector3(0f, SlotBaseGame.instance.getSymbolVerticalSpacingAt(0), 0f);
		_revealVfx = _bannerMap[BannerTypeEnum.CLICKME].revealVfx;
	
		bannerObjects = new List<GameObject>();
		//Add the CLICKME banners to where they should be
		setupClickMeBanners();
		
		if (_bannerTextOverlay != null)
		{
			BonusGameManager.instance.attachTextOverlay(_bannerTextOverlay);
			bannerTextObjects = CommonGameObject.findDirectChildren(_bannerTextOverlay);

			orginalBannerPositions = new Vector3[bannerTextObjects.Count,3];
	        for (int i = 0; i < bannerTextObjects.Count; i++)
	        {
				LabelWrapper label;
				LabelWrapper footerLabel;
				LabelWrapper headerLabel;
				getBannerLabels(bannerTextObjects[i], out label, out footerLabel, out headerLabel);

	            //We need to get the position of these texts right here.
	            orginalBannerPositions[i, 0] = headerLabel.transform.localPosition;
	            orginalBannerPositions[i, 1] = label.transform.localPosition;
	            orginalBannerPositions[i, 2] = footerLabel.transform.localPosition;

	            footerLabel.text = "";
	            label.text = "";
	            headerLabel.text = "";

				BannerTypeEnum clickMeBannerType = getClickMeBannerType(i);
	            setAllBannerTextInfo(clickMeBannerType, headerLabel, label, footerLabel);
	        }
		}
        
        // There is a pickMe animation, so we should do something to play it whenever we can.
        if (_bannerMap[BannerTypeEnum.CLICKME].pickMePrefab != null ||
		   (_bannerMap.ContainsKey(BannerTypeEnum.CLICKME2) && _bannerMap[BannerTypeEnum.CLICKME2].pickMePrefab != null) ||
		   (_bannerMap.ContainsKey(BannerTypeEnum.CLICKME3) && _bannerMap[BannerTypeEnum.CLICKME3].pickMePrefab != null))
		{
			StartCoroutine("playPickMeAnimation");
		}
	
		// Audio for oz portal
		if (GameState.game.keyName.Contains("oz00"))
		{
			Audio.play("dmCantYouReadWhatTheNotice", 1f, 0f, 1f);
			Audio.switchMusicKeyImmediate("bonusportalOz00");
		}
		else if (GameState.game.keyName.Contains("lls"))
		{
			Audio.play("RevealPortalBgLLS");
		}
		else
		{
			Audio.switchMusicKey("");
			Audio.play(Audio.soundMap("bonus_portal_bg"));
			// play VO at the set delay if it is set in the sound map
			Audio.play(Audio.soundMap("bonus_portal_vo"), 1, 0, portalVOSoundDelay);
		}

		if (GameState.game.keyName.Contains("com05"))
		{
			Audio.play ("PortalVOHagar");
		}
		
	}

	// Put the click me banners on the defined banner roots.
	protected virtual void setupClickMeBanners()
	{
		for (int iRoot = 0; iRoot < _bannerRoots.Length; iRoot++)
		{
			GameObject root = _bannerRoots[iRoot];
			BannerTypeEnum clickMeBannerType = getClickMeBannerType(iRoot);
	
			GameObject banner = CommonGameObject.instantiate(_bannerMap[clickMeBannerType].template) as GameObject;
			if (banner != null)
			{
				bannerObjects.Add(banner);
				banner.transform.parent = root.transform;
				banner.transform.localScale = _bannerMap[clickMeBannerType].bannerScaleAdjustment;
				banner.transform.localPosition = bannerAdjustment + _bannerMap[clickMeBannerType].bannerPosAdjustment;
				banner.transform.localRotation = Quaternion.identity;
			}
			else
			{
				Debug.LogError("The Clickme banner wasn't defined.");
			}
		}
	}
	
	protected BannerTypeEnum getClickMeBannerType(int clickMeIndex)
	{
		if (clickMeIndex == 1 && _bannerMap.ContainsKey(BannerTypeEnum.CLICKME2))
		{
			return BannerTypeEnum.CLICKME2;
		}
		else if (clickMeIndex == 2 && _bannerMap.ContainsKey(BannerTypeEnum.CLICKME3))
		{
			return BannerTypeEnum.CLICKME3;
		}
		return BannerTypeEnum.CLICKME;
	}

	protected virtual void stopPickMeAnimation(bool force = false)
	{
		for (int iBanner = 0; iBanner < bannerObjects.Count; iBanner++)
		{
			GameObject banner = bannerObjects[iBanner];
			BannerTypeEnum clickMeBannerType = getClickMeBannerType(iBanner);
			
			if (banner != null)
			{
				GameObject activePickMeAnimation =
					CommonGameObject.findDirectChild(
						banner, _bannerMap[clickMeBannerType].pickMePrefab.name+"(Clone)");
						
				if (activePickMeAnimation != null)
				{
					ParticleSystem ps = activePickMeAnimation.GetComponent<ParticleSystem>();
					
					if (ps != null && (force || !ps.IsAlive()))
					{
						Destroy(activePickMeAnimation);
						playingPickMeAnimation = false;
						break;
					}
				}
			}
		}
	}

	// Plays the pickme Animation on the banner if one exsits while taking care to make sure only one is played at a time.
	protected virtual IEnumerator playPickMeAnimation()
	{
		while (true)
		{
			if (playingPickMeAnimation)
			{
				stopPickMeAnimation();
			}
			else
			{
				int bannerToAddPickmeAnimation = Random.Range(0,bannerObjects.Count);
				BannerTypeEnum clickMeBannerType = getClickMeBannerType(bannerToAddPickmeAnimation);
				
				yield return new WaitForSeconds(Random.Range(0.0f,7.5f));
				GameObject pickMeObject = CommonGameObject.instantiate(_bannerMap[clickMeBannerType].pickMePrefab) as GameObject;
				
				pickMeObject.transform.parent = bannerObjects[bannerToAddPickmeAnimation].transform;
				pickMeObject.transform.localScale = Vector3.one;
				pickMeObject.transform.localPosition = _bannerMap[clickMeBannerType].pickMePrefab.transform.localPosition;
				
				playingPickMeAnimation = true;
			}
			
			yield return null;
		}

	}

	// Utility function to show revealed banners.
	protected virtual GameObject createRevealBanner(BannerTypeEnum bannerName, GameObject parentBanner)
	{
		GameObject symbol = CommonGameObject.instantiate(_bannerMap[bannerName].template) as GameObject;
		revealedObjects.Add(symbol);
		symbol.transform.parent = parentBanner.transform.parent;
		symbol.transform.localScale = _bannerMap[bannerName].bannerScaleAdjustment;
		symbol.transform.localPosition = bannerAdjustment + _bannerMap[bannerName].bannerPosAdjustment;
		symbol.transform.localRotation = Quaternion.identity;
		
		return symbol;
		
	}
	
	protected virtual void disableAllBannerClicks()
	{
		foreach(GameObject banner in bannerObjects)
		{
			CommonGameObject.setObjectCollidersEnabled(banner, false, true);
		}
		
		foreach(GameObject textoverlay in bannerTextObjects)
		{
			CommonGameObject.setObjectCollidersEnabled(textoverlay, false, true);
		}
	}

	// get banner text for a credits banner
	protected string getCreditsBannerText(bool ignoreBannerMap = false)
	{
		if (ignoreBannerMap || _bannerMap.ContainsKey(BannerTypeEnum.CREDITS))
		{
			if (SlotBaseGame.instance != null)
			{
				return SlotBaseGame.instance.getCreditBonusValueText();
			}
			else
			{
				Debug.LogError("SlotBaseGame.instance was null!  No credit value to return.");
				return "";
			}
		}
		else
		{
			Debug.LogWarning("Trying to call getCreditsBannerText() on a game without a credits banner setup!");
			return "";
		}
	}

	// setup the credits banner text on the banner
	protected virtual void processCreditsBannerText(GameObject bannerTextObject, LabelWrapper header, LabelWrapper center, LabelWrapper footer)
	{
		string creditText = getCreditsBannerText();

		if (creditText != "" && _bannerMap.ContainsKey(BannerTypeEnum.CREDITS))
		{
			setAllBannerTextInfo(BannerTypeEnum.CREDITS, header, center, footer, creditText);
		}
		else
		{
			Debug.LogWarning("PortalScript::processCreditsBannerText() - Couldn't get creditText or a credit banner wasn't defined!");
		}
	}

	// First call done by SlotBaseGame that will hide the obj clicked, and show the appropriate reveal.
	public virtual void beginPortalReveals(GameObject objClicked)
	{
		// Audio reveal on click
		if (GameState.game.keyName.Contains("lls"))
		{
			Audio.play("LWRevealBonus");
		}
		else if (GameState.game.keyName.Contains("oz00"))
		{
			Audio.play("ECportal_knock");
			Audio.play("summarywhoosh1");
			Audio.play("dmwellthatsmorelikeit", 1f, 0f, 0.5f);
		}
		else if (GameState.game.keyName.Contains("oz02"))
		{
			Audio.play("wwportal_pick_bonus");
		}
		else if (GameState.game.keyName.Contains("com03"))
		{
			Audio.switchMusicKey("");
			Audio.play ("PickRocketFlash");
		}
		else if (GameState.game.keyName.Contains("elvira04"))
		{
			Audio.switchMusicKey("");
			if (_outcome.isChallenge)
			{
				Audio.play("RevealCreditEL04");
			}
			else
			{
				Audio.play("RevealMultiplierEL04");
			}
		}
		else
		{
			//Stop the music that was playing before so it doesn't replay in the reveal.
			Audio.switchMusicKey("");
			Audio.play(Audio.soundMap("bonus_portal_reveal_bonus"));
		}

		disableAllBannerClicks();

		this.StartCoroutine(doBeginPortalReveals(objClicked, doAnimateBannerAndFlyout));
	}

	protected void playFreespinRevealAudio()
	{
		if(Audio.canSoundBeMapped(FREESPIN_REVEAL_AUDIO_KEY))
		{
			Audio.play(Audio.soundMap(FREESPIN_REVEAL_AUDIO_KEY));
		}
	}
	
	protected void playChallengeRevealAudio()
	{
		if(Audio.canSoundBeMapped(CHALLENGE_REVEAL_AUDIO_KEY))
		{
			Audio.play(Audio.soundMap(CHALLENGE_REVEAL_AUDIO_KEY));
		}
	}
	
	protected virtual IEnumerator doBeginPortalReveals(GameObject objClicked, RevealDelegate revealDelegate)
	{
		if (_bannerMap[BannerTypeEnum.CLICKME].pickMePrefab != null ||
		   (_bannerMap.ContainsKey(BannerTypeEnum.CLICKME2) && _bannerMap[BannerTypeEnum.CLICKME2].pickMePrefab != null))
		{
			stopPickMeAnimation(true);
			StopCoroutine("playPickMeAnimation");		
		}
		
		GameObject revealObject;
		revealedObjects = new List<GameObject>();
		//It is possible to send the text object clicked on since clicking on the object itself may be impossible from a 
		//perspective camera, so we will check to see if we got a text object, and if so, grab the associated revealObject.
		for (int i = 0; i < bannerObjects.Count; i++)
		{
			GameObject bannerText = CommonGameObject.findDirectChild(bannerTextObjects[i], "Text");
			
			if (objClicked.transform.parent.gameObject == bannerObjects[i] || objClicked == bannerText)
			{
				_revealedIndex = i;

				LabelWrapper label;
				LabelWrapper footerLabel;
				LabelWrapper headerLabel;
				getBannerLabels(bannerTextObjects[i], out label, out footerLabel, out headerLabel);

				resetBannerPositions(i, headerLabel.gameObject, label.gameObject, footerLabel.gameObject);
								
				label.text = "";
				footerLabel.text = "";
				headerLabel.text = "";
				if (_outcome.isChallenge)
				{
					playChallengeRevealAudio();

					revealObject = createRevealBanner(BannerTypeEnum.CHALLENGE, bannerObjects[i]);

					setAllBannerTextInfo(BannerTypeEnum.CHALLENGE, headerLabel, label, footerLabel);
				}
				else if(_outcome.isGifting)
				{
					playFreespinRevealAudio();

					revealObject = createRevealBanner(BannerTypeEnum.GIFTING, bannerObjects[i]);

					setAllBannerTextInfo(BannerTypeEnum.GIFTING, headerLabel, label, footerLabel);
				}
				else
				{
					if (_bannerMap.ContainsKey(BannerTypeEnum.OTHER))
					{
						revealObject = createRevealBanner(BannerTypeEnum.OTHER, bannerObjects[i]);
						setAllBannerTextInfo(BannerTypeEnum.OTHER, headerLabel, label, footerLabel);
					}
					else if (_bannerMap.ContainsKey(BannerTypeEnum.CREDITS))
					{
						revealObject = createRevealBanner(BannerTypeEnum.CREDITS, bannerObjects[i]);
						processCreditsBannerText(bannerText, headerLabel, label, footerLabel);
					}
					else
					{
						revealObject = null;
					}
				}

				if (revealObject != null)
				{
					VisualEffectComponent.Create(_revealVfx, revealObject);
				}

				yield return this.StartCoroutine(revealDelegate(bannerObjects[i]));
				bannerObjects[i] = null;
			}
		}
		
		revealBanners();
	}
	
	// Common function for getting labels to be used.
	protected void getBannerLabels(GameObject gameObject, out LabelWrapper label, out LabelWrapper footerLabel, out LabelWrapper headerLabel)
	{
		GameObject bannerText = CommonGameObject.findDirectChild(gameObject, "Text");
		GameObject footerText = CommonGameObject.findDirectChild(gameObject, "Footer");
		GameObject headerText = CommonGameObject.findDirectChild(gameObject, "Header");

		UILabel uiLabel = bannerText.GetComponent<UILabel>();
		UILabel footerUILabel = footerText.GetComponent<UILabel>();
		UILabel headerUILabel = headerText.GetComponent<UILabel>();

		TextMeshPro tmPro = bannerText.GetComponent<TextMeshPro>();
		TextMeshPro footerTmPro = footerText.GetComponent<TextMeshPro>();
		TextMeshPro headerTmPro = headerText.GetComponent<TextMeshPro>();
	
		label = new LabelWrapper(tmPro, uiLabel);
		footerLabel = new LabelWrapper(footerTmPro, footerUILabel);
		headerLabel = new LabelWrapper(headerTmPro, headerUILabel);
	}

	protected virtual string getBannerText(SlotBaseGame.BannerTextInfo banner)
	{
		string result = banner.localizedText;
		if (banner.textDirection == TextDirectionEnum.VERTICAL)
		{
			result = CommonText.makeVertical(result);
		}
		
		if (banner.insertNewLines)
		{
			result = CommonText.replaceSpacesWithNewLines(result);
		}
		
		// Remove extra newlines that can result from adding more newlines for spaces, when there was already wrapping.
		result = result.Replace("\n\n", "\n");
		
		return result;
	}

	//Puts the banners back to their origainal possitions.
	protected virtual void resetBannerPositions(int index,GameObject top, GameObject middle, GameObject bottom)
	{
		top.transform.localPosition = orginalBannerPositions[index,0];
		middle.transform.localPosition = orginalBannerPositions[index,1];
		bottom.transform.localPosition = orginalBannerPositions[index,2];
		
	}

	//Puts the corect text in the correct label. This changes the properties of the label and never reverts them.
	protected virtual void setCorrectLabel(SlotBaseGame.BannerTextInfo banner, LabelWrapper header, LabelWrapper center, LabelWrapper footer)
	{
		string bannerText = getBannerText(banner);
		switch (banner.textLocation)
		{
			case TextLocationEnum.HEADER:
				header.copySettings(banner.labelWrapper);
				header.text = bannerText;
				header.gameObject.transform.localPosition = header.gameObject.transform.localPosition + banner.bannerTextAdjustment;
				break;
			case TextLocationEnum.CENTER:
				center.copySettings(banner.labelWrapper);
				center.text = bannerText;
				center.gameObject.transform.localPosition = center.gameObject.transform.localPosition + banner.bannerTextAdjustment;
				break;
			case TextLocationEnum.FOOTER:
				footer.copySettings(banner.labelWrapper);
				footer.text = bannerText;
				footer.gameObject.transform.localPosition = footer.gameObject.transform.localPosition + banner.bannerTextAdjustment;
				break;
		}
		//We need to reset the localPosition Back to what they should be after this.
	}

	// Helper function to cycle through all BannerTextInfo and set it all for a banner
	protected void setAllBannerTextInfo(BannerTypeEnum bannerType, LabelWrapper headerLabel, LabelWrapper centerLabel, LabelWrapper footerLabel, string textOverride = "")
	{
		if (_bannerMap.ContainsKey(bannerType))
		{
			SlotBaseGame.BannerTextInfo[] textInfoList = _bannerMap[bannerType].textInfo;
			foreach (SlotBaseGame.BannerTextInfo bannerTextInfo in textInfoList)
			{
				setCorrectLabel(bannerTextInfo, headerLabel, centerLabel, footerLabel);

				if (textOverride != "")
				{
					switch (bannerTextInfo.textLocation)
					{
						case (TextLocationEnum.HEADER):
							headerLabel.text = textOverride;
							centerLabel.text = "";
							break;
						case (TextLocationEnum.CENTER):
							centerLabel.text = textOverride;
							break;
						case (TextLocationEnum.FOOTER):
							footerLabel.text = textOverride;
							centerLabel.text = "";
							break;
					}
				}
			}
		}
	}
	
	// Invoked twice to make sure revealed banners display the appropriate banners.
	// If 3 objects have been added, we start the game process, otherwise, we show the last banner.
	protected virtual void revealBanners()
	{
		Color disabledColor = Color.gray;
		GameObject revealObject = null;
		for (int i = 0; i < bannerObjects.Count; i++)
		{
			if (bannerObjects[i] != null && i != _revealedIndex)
			{
				LabelWrapper label;
				LabelWrapper footerLabel;
				LabelWrapper headerLabel;
				getBannerLabels(bannerTextObjects[i], out label, out footerLabel, out headerLabel);
				
				resetBannerPositions(i, headerLabel.gameObject, label.gameObject, footerLabel.gameObject);

				label.text = "";
				footerLabel.text = "";
				headerLabel.text = "";
				
				if (_outcome.isChallenge)
				{
					if (!spinsAdded)
					{
						revealObject = createRevealBanner(BannerTypeEnum.GIFTING, bannerObjects[i]);

						setAllBannerTextInfo(BannerTypeEnum.GIFTING, headerLabel, label, footerLabel);
						spinsAdded = true;
					}
					else
					{
						if (_bannerMap.ContainsKey(BannerTypeEnum.OTHER))
						{
							revealObject = createRevealBanner(BannerTypeEnum.OTHER, bannerObjects[i]);
							setAllBannerTextInfo(BannerTypeEnum.OTHER, headerLabel, label, footerLabel);
						}
						else if (_bannerMap.ContainsKey(BannerTypeEnum.CREDITS))
						{
							revealObject = createRevealBanner(BannerTypeEnum.CREDITS, bannerObjects[i]);
							processCreditsBannerText(label.gameObject, headerLabel, label, footerLabel);
						}

						label.color = disabledColor;
					}
				}
				else if (_outcome.isGifting)
				{
					if (!bonusAdded)
					{
						revealObject = createRevealBanner(BannerTypeEnum.CHALLENGE, bannerObjects[i]);
						bonusAdded = true;
						setAllBannerTextInfo(BannerTypeEnum.CHALLENGE, headerLabel, label, footerLabel);
					}
					else
					{
						if (_bannerMap.ContainsKey(BannerTypeEnum.OTHER))
						{
							revealObject = createRevealBanner(BannerTypeEnum.OTHER, bannerObjects[i]);
							setAllBannerTextInfo(BannerTypeEnum.OTHER, headerLabel, label, footerLabel);
						}
						else if (_bannerMap.ContainsKey(BannerTypeEnum.CREDITS))
						{
							revealObject = createRevealBanner(BannerTypeEnum.CREDITS, bannerObjects[i]);
							processCreditsBannerText(label.gameObject, headerLabel, label, footerLabel);
						}
						label.color = disabledColor;
					}
				}
				else if (_outcome.isCredit)
				{
					if (!bonusAdded)
					{
						revealObject = createRevealBanner(BannerTypeEnum.CHALLENGE, bannerObjects[i]);
						bonusAdded = true;
						setAllBannerTextInfo(BannerTypeEnum.CHALLENGE, headerLabel, label, footerLabel);
					}
					else
					{
						revealObject = createRevealBanner(BannerTypeEnum.GIFTING, bannerObjects[i]);
						setAllBannerTextInfo(BannerTypeEnum.GIFTING, headerLabel, label, footerLabel);
					}
				}
				
				// Since these are definitely not the active panels, let's disable all text flourishes and set the color to disabled.
                label.color = disabledColor;
                footerLabel.color = disabledColor;
                headerLabel.color = disabledColor;

                label.effectColor = Color.black;
                footerLabel.effectColor = Color.black;
                headerLabel.effectColor = Color.black;

                label.isGradient = false;
                footerLabel.isGradient = false;
                headerLabel.isGradient = false;
				
				VisualEffectComponent.Create(_revealVfx, revealObject);
				AnimateBannerFlyout(bannerObjects[i]);
				bannerObjects[i] = null;
				break;
			}
		}
		
		// Get the child banner where the material is stored.
		CommonGameObject.colorGameObject(revealObject, disabledColor);
		// Color any NGUI opjects on the banner just to be safe.
		CommonGameObject.colorUIGameObject(revealObject, disabledColor);
		
		if (revealedObjects.Count == bannerObjects.Count)
		{
			Invoke("beginBonus", BEGIN_BONUS_DELAY_TIME);
		}
		else
		{
			Invoke("revealBanners", 0.75f);
		}
		
		// Audio reveal
		if (GameState.game.keyName.Contains("lls"))
		{
			Audio.play("RevealBonusOthersLLS");
		}
		else if (GameState.game.keyName.Contains("oz00"))
		{
			Audio.play("clickheelsparkly0");
		}
		else
		{
			Audio.play(Audio.soundMap("bonus_portal_reveal_others"));
		}
	}
	
	protected virtual void AnimateBannerFlyout(GameObject banner)
	{
		CommonTransform.setZ(banner.transform, -0.1f, Space.Self);
		TweenScale.Begin(banner, 0.5f, Vector3.zero);
		Destroy(banner, 0.5f);
	}

	protected virtual IEnumerator doAnimateBannerOnly(GameObject banner)
	{
		//Check if that banner has an animation to play prior to the flyout
		BannerScript script = banner.GetComponentInChildren<BannerScript>();
		if (script != null && script.onClickAnimation != null)
		{
			//Play the animation prior to the flyout
			yield return new TIAnimationYieldInstruction(script.onClickAnimation, script.onClickAnimation.clip);
		}
		Destroy(banner);
	}

	protected virtual IEnumerator doAnimateBannerAndFlyout(GameObject banner)
	{
		//Check if that banner has an animation to play prior to the flyout
		BannerScript script = banner.GetComponentInChildren<BannerScript>();
		if (script != null && script.onClickAnimation != null)
		{
			//Play the animation prior to the flyout
			yield return new TIAnimationYieldInstruction(script.onClickAnimation, script.onClickAnimation.clip);
		}

		CommonTransform.setZ(banner.transform, -0.1f, Space.Self);
		yield return new TITweenYieldInstruction(iTween.ScaleTo(banner, Vector3.zero, 0.5f));
		Destroy(banner);
	}
	
	// We destroy our banners and get into the game already.
	protected virtual void beginBonus()
	{
		if (WAIT_TO_DESTROY_PORTAL_BANNERS_DUR == 0.0f)
		{
			destroyPortalBanners();
		}
		else
		{
			// fade out banners before destroy
			if (fadeBannersBeforeDestroy)
			{
				StartCoroutine(fadePortalBanners());
			}
			StartCoroutine(waitToDestroyPortalBanners());
		}
		
		spinsAdded = false;
		bonusAdded = false;
		
		// Determine if we need to create a bonus here (including if this is a credits outcome which is actually a bonus game)
		bool createBonus = true;
		if (!_outcome.isCredit || (_outcome.isCredit && _outcome.winAmount == 0))
		{
			BonusGameManager.instance.currentMultiplier = _multiplier;
			BonusGameManager.currentBaseGame = SlotBaseGame.instance;

			//Checking slot modules to see if we want to create the bonus game in the module instead. Used for transitions.
			foreach (SlotModule module in BonusGameManager.currentBaseGame.cachedAttachedSlotModules)
			{
				// handle the pre bonus created modules, needed for some transitions
				if (module.needsToExecuteOnPreBonusGameCreated())
				{
					StartCoroutine(module.executeOnPreBonusGameCreated());
				}

				if (module.needsToLetModuleCreateBonusGame())
				{
					//Don't create the bonus here if we're going to do that in the module
					createBonus = false;
				}
			}

			if (createBonus)
			{
				SlotBaseGame.instance.createBonus(_outcome, isIgnoringPortal:true);
			}
		}

		if (_outcome.isCredit)
		{
			if (SlotBaseGame.instance != null)
			{
				SlotBaseGame.instance.goIntoBonus();
			}
			else
			{
				Debug.LogError("There is no SlotBaseGame instance, can't start bonus game...");
			}
		}
		else
		{
			if (createBonus)
			{
				BonusGameManager.instance.show();
			}
		}
	}

	// override for custom portal fade animations
	protected virtual IEnumerator fadePortalBanners()
	{
		yield break;
	}

	protected IEnumerator waitToDestroyPortalBanners()
	{
		yield return new WaitForSeconds(WAIT_TO_DESTROY_PORTAL_BANNERS_DUR);
		destroyPortalBanners();
	}

	protected virtual void destroyPortalBanners()
	{
		foreach (GameObject bannerObj in revealedObjects)
		{
			GameObject.Destroy(bannerObj);
		}
		
		if (_bannerTextOverlay != null)
		{
			Object.Destroy(_bannerTextOverlay);
		}
	}
}

public delegate IEnumerator RevealDelegate(GameObject target);
