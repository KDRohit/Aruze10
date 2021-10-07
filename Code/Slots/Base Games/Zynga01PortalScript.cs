using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using BannerEnum = SlotBaseGame.BannerInfo.BannerTypeEnum;
using TextDirectionEnum =  SlotBaseGame.BannerTextInfo.TextDirectionEnum;
using TextLocationEnum =  SlotBaseGame.BannerTextInfo.TextLocationEnum;

public class Zynga01PortalScript : PortalScript
{
	// Our start to getting a portal to display
	public override void beginPortal(GameObject[] bannerRoots, SlotBaseGame.BannerInfo[] banners, GameObject bannerOverlay, SlotOutcome outcome, long multiplier)
	{	
		StartCoroutine(clearBoardThenBeginPortal(bannerRoots, banners, bannerOverlay, outcome, multiplier));
	}

	public IEnumerator clearBoardThenBeginPortal(GameObject[] bannerRoots, SlotBaseGame.BannerInfo[] banners, GameObject bannerOverlay, SlotOutcome outcome, long multiplier)
	{
		yield return StartCoroutine((PlopSlotBaseGame.instance as PlopSlotBaseGame).clearSymbolsForBonusGame());
		_revealedIndex = -1;
		_banners = banners;
		_bannerRoots = bannerRoots;
		_bannerTextOverlay = CommonGameObject.instantiate(bannerOverlay) as GameObject;
		_outcome = outcome;
		_multiplier = multiplier;
		bonusAdded = false;
		spinsAdded = false;
		
		// Populate a map for the purposes of generating the banners.
		
		_bannerMap = new Dictionary<BannerEnum, SlotBaseGame.BannerInfo>();
		foreach (SlotBaseGame.BannerInfo banner in _banners)
		{
			_bannerMap.Add(banner.bannerType, banner);
		}
		
		bannerAdjustment = new Vector3(0f, SlotBaseGame.instance.getSymbolVerticalSpacingAt(0), 0f);
		_revealVfx = _bannerMap[BannerEnum.CLICKME].revealVfx;
		
		bannerObjects = new List<GameObject>();
		//Add the CLICKME banners to where they should be
		foreach (GameObject root in _bannerRoots)
		{
			
			GameObject symbol = CommonGameObject.instantiate(_bannerMap[BannerEnum.CLICKME].template) as GameObject; //TODO: Null check
			bannerObjects.Add(symbol);
			symbol.transform.parent = root.transform;
			symbol.transform.localScale = Vector3.one;
			symbol.transform.localPosition = bannerAdjustment + _bannerMap[BannerEnum.CLICKME].bannerPosAdjustment;
			symbol.transform.localRotation = Quaternion.identity;
		}
		
		BonusGameManager.instance.attachTextOverlay(_bannerTextOverlay);
		bannerTextObjects = CommonGameObject.findDirectChildren(_bannerTextOverlay);
		orginalBannerPositions = new Vector3[bannerTextObjects.Count,3];
		for (int i = 0; i < bannerTextObjects.Count; i++)
		{
			GameObject banner = bannerTextObjects[i];
			GameObject bannerText = CommonGameObject.findDirectChild(banner, "Text");
			GameObject headerText = CommonGameObject.findDirectChild(banner, "Header");
			GameObject footerText = CommonGameObject.findDirectChild(banner, "Footer");
			
			//We need to get the position of these texts right here.
			orginalBannerPositions[i, 0] = headerText.transform.localPosition;
			orginalBannerPositions[i, 1] = bannerText.transform.localPosition;
			orginalBannerPositions[i, 2] = footerText.transform.localPosition;
		}

		StartCoroutine("playPickMeAnimation");

		

		Audio.switchMusicKeyImmediate(Audio.soundMap("bonus_portal_bg"));
	}

	// Plays the pickme Animation on the banner if one exsits while taking care to make sure only one is played at a time.
	protected override IEnumerator playPickMeAnimation()
	{
		while (true)
		{
			int bannerToAddPickmeAnimation = Random.Range(0,bannerObjects.Count);
			yield return new WaitForSeconds(Random.Range(1.0f,3.5f));
			GameObject pickMeObject = bannerObjects[bannerToAddPickmeAnimation];
			yield return StartCoroutine(pickMeObject.GetComponentInChildren<Zynga01BannerScript>().doSignSpin());
		}
		
	}

	// First call done by SlotBaseGame that will hide the obj clicked, and show the appropriate reveal.
	public override void beginPortalReveals(GameObject objClicked)
	{

		//Stop the music that was playing before so it doesn't replay in the reveal.
		Audio.switchMusicKey("");
		Audio.play(Audio.soundMap("bonus_portal_reveal_bonus"));

		destroySigns();
		disableAllBannerClicks();
		
		this.StartCoroutine(doBeginPortalReveals(objClicked, eatGrassAndReveal));
	}

	protected void destroySigns()
	{
		StopCoroutine("playPickMeAnimation");
		for (int i=0; i < bannerObjects.Count; i++)
		{
			GameObject pickMeObject = bannerObjects[i];
			pickMeObject.GetComponentInChildren<Zynga01BannerScript>().destroySigns();
		}
	}

	// "Other" banner can be the progressive game, or a credit game. Who knows what else in the future.
	protected override void processCreditsBannerText(GameObject bannerTextObject, LabelWrapper header, LabelWrapper center, LabelWrapper footer)
	{
		GameObject creditTextGameObject = GameObject.Find("CreditsLabel");
		LabelWrapperComponent creditLabel = creditTextGameObject.GetComponent<LabelWrapperComponent>();
		creditLabel.text = SlotBaseGame.instance.getCreditBonusValueText();
	}

	// Invoked twice to make sure revealed banners display the appropriate banners.
	// If 3 objects have been added, we start the game process, otherwise, we show the last banner.
	protected override void revealBanners()
	{
		Color disabledColor = new Color(0.25f, 0.25f, 0.25f);
		GameObject revealObject = null;
		for (int i = 0; i < bannerObjects.Count; i++)
		{
			if (bannerObjects[i] != null && i != _revealedIndex)
			{
				GameObject bannerText = CommonGameObject.findDirectChild(bannerTextObjects[i], "Text");
				GameObject footerText = CommonGameObject.findDirectChild(bannerTextObjects[i], "Footer");
				GameObject headerText = CommonGameObject.findDirectChild(bannerTextObjects[i], "Header");
				resetBannerPositions(i,headerText,bannerText,footerText);

				
				if (_outcome.isChallenge)
				{
					if (!spinsAdded)
					{
						revealObject = createRevealBanner(BannerEnum.GIFTING, bannerObjects[i]);
						spinsAdded = true;
					}
					else
					{
						revealObject = createRevealBanner(BannerEnum.CREDITS, bannerObjects[i]);
						processCreditsBannerText(null, null, null, null);
						GameObject creditTextGameObject = GameObject.Find("CreditsLabel");
						UILabel creditLabel = creditTextGameObject.GetComponent<UILabel>();
						creditLabel.color = disabledColor;
					}
				}
				else if (_outcome.isGifting)
				{
					if (!bonusAdded)
					{
						revealObject = createRevealBanner(BannerEnum.CHALLENGE, bannerObjects[i]);
						bonusAdded = true;
					}
					else
					{
						revealObject = createRevealBanner(BannerEnum.CREDITS, bannerObjects[i]);
						processCreditsBannerText(null, null, null, null);
						GameObject creditTextGameObject = GameObject.Find("CreditsLabel");
						UILabel creditLabel = creditTextGameObject.GetComponent<UILabel>();
						creditLabel.color = disabledColor;
					}
				}
				else if (_outcome.isCredit)
				{
					if (!bonusAdded)
					{
						revealObject = createRevealBanner(BannerEnum.CHALLENGE, bannerObjects[i]);
						bonusAdded = true;
					}
					else
					{
						revealObject = createRevealBanner(BannerEnum.GIFTING, bannerObjects[i]);
					}
				}

				VisualEffectComponent.Create(_revealVfx, revealObject);
				AnimateBannerFlyout(bannerObjects[i]);
				bannerObjects[i] = null;
				// Get the child banner where the material is stored.
				List<GameObject> children = CommonGameObject.findAllChildren(revealObject);
				foreach (GameObject child in children)
				{
					if (child != null && child.GetComponent<Renderer>() != null && child.GetComponent<Renderer>().material != null && child.GetComponent<Renderer>().material.HasProperty("_Color"))
					{
						// Set the Color to Disabled
						child.GetComponent<Renderer>().material.SetColor("_Color", disabledColor);
					}
				}
				break;
			}
		}


		
		if (revealedObjects.Count == 3)
		{
			Invoke("beginBonus", 1.5f);
		}
		else
		{
			Invoke("revealBanners", 0.75f);
		}

		Audio.play(Audio.soundMap("bonus_portal_reveal_others"));

	}
	
	protected IEnumerator eatGrassAndReveal(GameObject target)
	{
		yield return StartCoroutine(target.GetComponentInChildren<Zynga01BannerScript>().doAnimationAndReveal());
	}
}
